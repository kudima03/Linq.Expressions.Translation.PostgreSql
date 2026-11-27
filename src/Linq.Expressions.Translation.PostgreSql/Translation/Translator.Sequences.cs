using System.Linq.Expressions;
using System.Text;
using Linq.Expressions.Translation.PostgreSql.Mapping;
using Linq.Expressions.Translation.PostgreSql.Shapes;
using Linq.Expressions.Translation.PostgreSql.Syntax;

namespace Linq.Expressions.Translation.PostgreSql.Translation;

// Sequences read whole by a BCL member: the elements of a group in an order, their
// first or last one, string.Join over a group, a subquery or an array, and the Max or
// Min of a few values.
internal sealed partial class Translator
{
    // The types whose Max and Min order as PostgreSQL's GREATEST and LEAST do. A
    // double or a float is not one: Max skips NaN where GREATEST takes it as the
    // greatest value, and Min returns it where LEAST skips it.
    private static readonly HashSet<Type> GreatestTypes =
    [
        typeof(bool),
        typeof(short),
        typeof(int),
        typeof(long),
        typeof(decimal),
        typeof(DateOnly),
        typeof(TimeOnly),
        typeof(DateTime),
        typeof(DateTimeOffset),
        typeof(TimeSpan),
    ];

    private static readonly Dictionary<NormalizationForm, string> Forms = new()
    {
        [NormalizationForm.FormC] = "NFC",
        [NormalizationForm.FormD] = "NFD",
        [NormalizationForm.FormKC] = "NFKC",
        [NormalizationForm.FormKD] = "NFKD",
    };

    private Shape Sequenced(GroupingShape group, Shape element, MethodCallExpression call)
    {
        return call.Method.Name switch
        {
            nameof(Enumerable.OrderBy)
            or nameof(Enumerable.OrderByDescending)
            or nameof(Enumerable.ThenBy)
            or nameof(Enumerable.ThenByDescending) => Sorted(group, element, call),
            nameof(Enumerable.First)
            or nameof(Enumerable.FirstOrDefault)
            or nameof(Enumerable.Last)
            or nameof(Enumerable.LastOrDefault) => new ScalarShape(
                Earliest(group, element, call)
            ),
            _ => throw Unsupported.Method(call.Method),
        };
    }

    // The elements of a group sorted by keys, nulls first ascending as in .NET.
    private GroupingShape Sorted(
        GroupingShape group,
        Shape element,
        MethodCallExpression call
    )
    {
        string name = call.Method.Name;
        bool then = name.StartsWith(nameof(Enumerable.ThenBy), StringComparison.Ordinal);
        if (!then && group.Order is not null)
        {
            throw new NotSupportedException(
                $"'{name}' of a group already ordered is not supported; order it once with OrderBy and ThenBy."
            );
        }
        bool ordinal = IsOrdinal(call);
        if (call.Arguments.Count > 2 && !ordinal)
        {
            throw Unsupported.Method(call.Method);
        }
        bool descending = name.EndsWith("Descending", StringComparison.Ordinal);
        List<Ordering> keys =
        [
            .. group.Order ?? [],
            .. Bind(Lambda(call, 1), element)
                .Columns()
                .Where(column => column.Value is not SqlConstant)
                .SelectMany(column =>
                    Keys(
                        ordinal ? CodeUnits(column.Value) : column.Value,
                        descending,
                        descending
                    )
                ),
        ];
        return new GroupingShape(
            group.Key,
            element,
            group.Filter,
            group.Distinct,
            keys,
            group.Source
        );
    }

    // The first or last element of a group in its order is the first of the array of
    // its values in that order, or in the inverted one; without an order, First is any
    // element, as for a query. A filtered group may hold no element: First and Last
    // throw there, the OrDefault forms yield the default.
    private SqlExpression Earliest(
        GroupingShape group,
        Shape element,
        MethodCallExpression call
    )
    {
        string name = call.Method.Name;
        bool last = name.StartsWith(nameof(Enumerable.Last), StringComparison.Ordinal);
        IReadOnlyList<Ordering> order = group.Sequence;
        if (last && order.Count == 0)
        {
            throw new NotSupportedException(
                $"'{name}' of a group is translated only once its elements are sorted with OrderBy, or come from an ordered source: they have no order in SQL."
            );
        }
        SqlExpression? filter =
            call.Arguments.Count > 1
                ? Conjoined(Scalar(Bind(Lambda(call, 1), element)), group.Filter)
                : group.Filter;
        bool defaulted = name.EndsWith("OrDefault", StringComparison.Ordinal);
        if (filter is not null && !defaulted)
        {
            throw new NotSupportedException(
                $"'{name}' of a filtered group is not supported: where no element is left, .NET throws; use {name}OrDefault."
            );
        }
        SqlExpression value = element is ScalarShape scalar
            ? scalar.Value
            : throw new NotSupportedException(
                $"'{name}' of a group is translated for a single value only: select the value before {name}."
            );
        // The first of the distinct values is the first value; the last of them is
        // the one that occurs first the latest, not the last value.
        SqlExpression first =
            last && group.Distinct && !IsSortedByItself(group, value)
                ? new SqlOccurrences(value, order, filter, true, value.Type)
                : new SqlFirst(
                    value,
                    [.. last ? order.Select(ordering => ordering.Inverted()) : order],
                    filter
                );
        return filter is null ? first : Substituted(first, call.Type);
    }

    // A predicate value => value != null leaves the values of a group that are never
    // null.
    private static Shape Kept(LambdaExpression predicate, Shape element)
    {
        return
            element is ScalarShape { Value.Nullable: true } scalar
            && IsNullTest(predicate)
            ? new ScalarShape(new SqlRequired(scalar.Value, scalar.Value.Type))
            : element;
    }

    private static bool IsNullTest(LambdaExpression predicate)
    {
        BinaryExpression? test = predicate.Body as BinaryExpression;
        return test?.NodeType == ExpressionType.NotEqual
            && test.Right is ConstantExpression { Value: null }
            && Unlifted(test.Left) == predicate.Parameters[0];
    }

    private static bool IsCompared(MethodCallExpression call)
    {
        Type last = call.Method.GetParameters()[^1].ParameterType;
        return call.Arguments.Count == 2
            && last.IsGenericType
            && last.GetGenericTypeDefinition() == typeof(IComparer<>);
    }

    private static bool IsOrdinalComparer(Expression comparer)
    {
        return comparer is ConstantExpression { Value: object value }
            && ReferenceEquals(value, StringComparer.Ordinal);
    }

    // Min(comparer) or Max(comparer) with StringComparer.Ordinal.
    private static bool IsOrdinallyCompared(MethodCallExpression call)
    {
        return call.Method.Name is nameof(Enumerable.Min) or nameof(Enumerable.Max)
            && IsCompared(call)
            && IsOrdinalComparer(call.Arguments[1]);
    }

    private static SqlExpression Extreme(
        GroupingShape group,
        Shape element,
        MethodCallExpression call
    )
    {
        return IsOrdinallyCompared(call)
            ? Ordinal(Scalar(element), group.Filter, call)
            : throw Unsupported.Method(call.Method);
    }

    // Min and Max of strings with StringComparer.Ordinal are MIN and MAX of the keys
    // of the ordinal order, read back as text in the collation of the database.
    private static SqlCollation Ordinal(
        SqlExpression value,
        SqlExpression? filter,
        MethodCallExpression call
    )
    {
        if (value.Type != typeof(string))
        {
            throw Unsupported.Method(call.Method);
        }
        SqlAggregate extreme = new(
            call.Method.Name.ToUpperInvariant(),
            CodeUnits(value),
            false,
            filter,
            typeof(string)
        );
        return Restored(extreme, extreme.Nullable);
    }

    // new[] { a, b, ... }.Max() is GREATEST(a, b, ...) and Min() is LEAST(...): both
    // skip NULLs and are NULL when every value is, as Max and Min of a nullable or a
    // reference type are.
    private SqlExpression Extremes(NewArrayExpression array, MethodCallExpression call)
    {
        string name = call.Method.Name;
        Type element = ScalarTypes.Underlying(array.Type.GetElementType()!);
        bool compared = call.Arguments.Count == 2 && IsCompared(call);
        bool ordinal = compared && IsOrdinalComparer(call.Arguments[1]);
        if (array.Expressions.Count == 0 || (call.Arguments.Count > 1 && !compared))
        {
            throw Unsupported.Method(call.Method);
        }
        if (element == typeof(Guid))
        {
            throw Unsupported.Uuids(name);
        }
        if (
            compared
                ? !ordinal || element != typeof(string)
                : !GreatestTypes.Contains(element)
        )
        {
            throw new NotSupportedException(
                $"'{name}' of an array of '{element.Name}' values is not supported: PostgreSQL orders them differently from .NET (strings are compared with StringComparer.Ordinal only, doubles and floats not at all, as NaN orders otherwise)."
            );
        }
        List<SqlExpression> values =
        [
            .. array
                .Expressions.Select(Sql)
                .Select(value => ordinal ? CodeUnits(value) : value),
        ];
        SqlFunction extreme = new(
            name == nameof(Enumerable.Max) ? "GREATEST" : "LEAST",
            values,
            call.Type,
            values.All(value => value.Nullable)
        );
        return ordinal ? Restored(extreme, extreme.Nullable) : extreme;
    }

    // string.Normalize() is in form C.
    private SqlFunction Normalized(MethodCallExpression call)
    {
        NormalizationForm form =
            call.Arguments.Count == 0 ? NormalizationForm.FormC
            : call.Arguments[0]
                is ConstantExpression { Value: NormalizationForm constant }
                ? constant
            : throw Unsupported.Method(call.Method);
        return Forms.TryGetValue(form, out string? keyword)
            ? new SqlFunction(
                "NORMALIZE",
                [Sql(call.Object!), new SqlFragment(keyword, typeof(string))],
                typeof(string)
            )
            : throw Unsupported.Method(call.Method);
    }

    // string.Join(separator, values): a null separator or value is the empty string.
    // Values of another type are formatted differently by PostgreSQL.
    private SqlExpression Separated(MethodCallExpression call)
    {
        Type values = call.Method.GetParameters()[1].ParameterType;
        if (values != typeof(string[]) && values != typeof(IEnumerable<string>))
        {
            throw new NotSupportedException(
                "string.Join is translated for string values only: PostgreSQL formats other values differently from .NET."
            );
        }
        SqlExpression separator = Separator(call.Arguments[0]);
        Expression argument = call.Arguments[1];
        return argument is NewArrayExpression or ConstantExpression { Value: not null }
                ? Interleaved(argument, separator)
            : argument.Type.IsAssignableTo(typeof(IQueryable))
                ? Aggregated(Query(argument), separator)
            : Shape(argument) switch
            {
                GroupingShape group => Aggregated(group, separator),
                GroupJoinShape group => Aggregated(Correlated(group), separator),
                ScalarShape { Value.Type.IsArray: true } array => new SqlFunction(
                    "ARRAY_TO_STRING",
                    [array.Value, separator, Text(string.Empty)],
                    typeof(string)
                ),
                _ => throw Unsupported.Method(call.Method),
            };
    }

    private SqlExpression Separator(Expression separator)
    {
        SqlExpression value = Sql(separator);
        return value switch
        {
            SqlConstant { Value: null } => Text(string.Empty),
            SqlConstant { Value: char character } => Text(character.ToString()),
            _ when separator.Type == typeof(char) => new SqlCast(
                value,
                "text",
                typeof(string)
            ),
            { Nullable: true } => new SqlFunction(
                "COALESCE",
                [value, Text(string.Empty)],
                typeof(string)
            ),
            _ => value,
        };
    }

    // The values of an array, each followed by the separator but the last.
    private SqlExpression Interleaved(Expression argument, SqlExpression separator)
    {
        List<SqlExpression> parts = [];
        if (argument is NewArrayExpression array)
        {
            foreach (Expression item in array.Expressions)
            {
                Separate(parts, separator);
                Appended(item, parts);
            }
        }
        else
        {
            foreach (
                string? item in (IEnumerable<string?>)
                    ((ConstantExpression)argument).Value!
            )
            {
                Separate(parts, separator);
                parts.Add(Text(item ?? string.Empty));
            }
        }
        return Joined([.. parts.Where(part => part is not SqlConstant { Value: "" })]);
    }

    private static void Separate(List<SqlExpression> parts, SqlExpression separator)
    {
        if (parts.Count > 0)
        {
            parts.Add(separator);
        }
    }

    // The values a subquery yields, in its order, joined in one row.
    private SqlExpression Aggregated(SqlQuery source, SqlExpression separator)
    {
        SelectQuery query = Open(source, Barrier.Aggregation);
        List<Ordering> order = query.IsOrdered ? query.Orderings : [];
        query.Orderings = [];
        query.Shape = new ScalarShape(
            Aggregated(Scalar(query.Shape), separator, false, order, null)
        );
        return new SqlRequired(
            new SqlScalarSubquery(query, typeof(string)),
            typeof(string)
        );
    }

    // Whether the elements of a group are sorted by their value alone, in the order
    // of the database or the ordinal one: their distinct values then come in that
    // order whichever of equal values came first.
    private static bool IsSortedByItself(GroupingShape group, SqlExpression value)
    {
        HashSet<string> keys =
        [
            .. (group.Order ?? []).Select(ordering => SqlWriter.Text(ordering.Key)),
        ];
        return keys.Count == 1
            && (
                keys.Contains(SqlWriter.Text(value))
                || keys.Contains(SqlWriter.Text(CodeUnits(value)))
            );
    }

    // The values of a group joined in the order they come in: the keys they are
    // sorted by, then the order of the source. STRING_AGG(DISTINCT ...) orders by its
    // value only, and would take a NULL for the empty string, which .NET joins as a
    // value of its own: it joins the distinct values that are sorted by themselves,
    // or come in no order at all, and are never null. In any other order the distinct
    // values come by their first occurrences.
    private static SqlExpression Aggregated(GroupingShape group, SqlExpression separator)
    {
        SqlExpression value = Scalar(
            group.Element
                ?? throw new NotSupportedException(
                    "The elements of a group are not available once the groups have been paged, made distinct or combined; aggregate them before that."
                )
        );
        if (!group.Distinct)
        {
            return Aggregated(value, separator, false, group.Sequence, group.Filter);
        }
        IReadOnlyList<Ordering> sorted = group.Order ?? [];
        bool ordinal = sorted.Any(ordering =>
            SqlWriter.Text(ordering.Key) == SqlWriter.Text(CodeUnits(value))
        );
        SqlExpression argument = ordinal ? CodeUnits(value) : value;
        bool byValue =
            sorted.Count == 0 ? group.Source.Count == 0 : IsSortedByItself(group, value);
        if (!byValue || (value.Nullable && sorted.Count > 0))
        {
            return new SqlFunction(
                "ARRAY_TO_STRING",
                [
                    new SqlOccurrences(
                        value,
                        group.Sequence,
                        group.Filter,
                        false,
                        typeof(string[])
                    ),
                    separator,
                    Text(string.Empty),
                ],
                typeof(string)
            );
        }
        if (value.Nullable)
        {
            throw new NotSupportedException(
                "string.Join of the distinct values of a group that may be null is translated only once they come in an order, their own or the source's: STRING_AGG(DISTINCT ...) would take a NULL for the empty string."
            );
        }
        SqlExpression joined = Aggregated(
            argument,
            separator,
            true,
            [
                .. sorted
                    .Take(1)
                    .Select(ordering => new Ordering(argument, ordering.Descending)),
            ],
            group.Filter
        );
        return ordinal ? Restored(joined, false) : joined;
    }

    private static SqlFunction Aggregated(
        SqlExpression value,
        SqlExpression separator,
        bool distinct,
        IReadOnlyList<Ordering> order,
        SqlExpression? filter
    )
    {
        SqlExpression text = value.Nullable
            ? new SqlFunction("COALESCE", [value, Text(string.Empty)], typeof(string))
            : value;
        return new SqlFunction(
            "COALESCE",
            [
                new SqlOrderedAggregate(
                    "STRING_AGG",
                    [text, separator],
                    distinct,
                    order,
                    filter,
                    typeof(string)
                ),
                Text(string.Empty),
            ],
            typeof(string)
        );
    }
}
