using System.Linq.Expressions;
using System.Reflection;
using Linq.Expressions.Translation.PostgreSql.Mapping;
using Linq.Expressions.Translation.PostgreSql.Shapes;
using Linq.Expressions.Translation.PostgreSql.Syntax;

namespace Linq.Expressions.Translation.PostgreSql.Translation;

internal sealed partial class Translator
{
    private static readonly HashSet<string> Terminals =
    [
        nameof(Queryable.Count),
        nameof(Queryable.LongCount),
        nameof(Queryable.Sum),
        nameof(Queryable.Average),
        nameof(Queryable.Min),
        nameof(Queryable.Max),
        nameof(Queryable.MinBy),
        nameof(Queryable.MaxBy),
        nameof(Queryable.Any),
        nameof(Queryable.All),
        nameof(Queryable.Contains),
        nameof(Queryable.First),
        nameof(Queryable.FirstOrDefault),
        nameof(Queryable.Single),
        nameof(Queryable.SingleOrDefault),
        nameof(Queryable.Last),
        nameof(Queryable.LastOrDefault),
        nameof(Queryable.ElementAt),
        nameof(Queryable.ElementAtOrDefault),
    ];

    private static readonly HashSet<Type> Integral = [typeof(int), typeof(long)];

    private static readonly HashSet<string> Countable =
    [
        nameof(Queryable.Count),
        nameof(Queryable.LongCount),
        nameof(Queryable.Any),
        nameof(Queryable.All),
    ];

    private static SelectQuery Statement(SqlExpression result)
    {
        return result is SqlScalarSubquery subquery
            ? subquery.Query
            : new SelectQuery(null, new ScalarShape(result));
    }

    private SqlExpression Subquery(MethodCallExpression call)
    {
        return Window(call) ?? Valued(Terminal(call), call);
    }

    // In a value position, an ...OrDefault of a non-nullable value falls back to its
    // default where the subquery yields no row.
    private static SqlExpression Valued(SqlExpression result, MethodCallExpression call)
    {
        SqlExpression value = Single(result);
        return call.Method.Name.EndsWith("OrDefault", StringComparison.Ordinal)
            ? Substituted(value, call.Type)
            : value;
    }

    // A subquery in a value position must produce one column; the shape of an element
    // query is the shape of its source, which may be an entity or a projection.
    private static SqlExpression Single(SqlExpression result)
    {
        return result is SqlScalarSubquery { Query.Shape: not ScalarShape }
            ? throw new NotSupportedException(
                "A subquery used as a value must select a single scalar value."
            )
            : result;
    }

    private SqlExpression Terminal(MethodCallExpression call)
    {
        MethodCallExpression? padding = call.Arguments[0] as MethodCallExpression;
        return
            padding
                is { Method.Name: nameof(Queryable.DefaultIfEmpty), Arguments.Count: 1 }
            ? Padded(Operand(padding), call, padding.Method)
            : Terminal(Operand(call), call);
    }

    private SqlExpression Padded(
        SqlQuery source,
        MethodCallExpression call,
        MethodInfo padding
    )
    {
        string name = call.Method.Name;
        return call.Arguments.Count > 1 ? throw Unsupported.Method(padding)
            : name == nameof(Queryable.Any) ? new SqlConstant(true, typeof(bool))
            : name is nameof(Queryable.Count) or nameof(Queryable.LongCount)
                ? Sized(
                    name,
                    new SqlFunction(
                        "GREATEST",
                        [
                            Counted(source, nameof(Queryable.LongCount), typeof(long)),
                            new SqlConstant(1, typeof(int)),
                        ],
                        typeof(long)
                    )
                )
            : name == nameof(Queryable.Sum) ? Aggregated(source, call)
            : name
                is nameof(Queryable.Average)
                    or nameof(Queryable.Min)
                    or nameof(Queryable.Max)
                ? Substituted(Aggregated(source, call), call.Type)
            : throw Unsupported.Method(padding);
    }

    private static SqlExpression Substituted(SqlExpression value, Type type)
    {
        return type.IsValueType && Nullable.GetUnderlyingType(type) is null
            ? new SqlFunction(
                "COALESCE",
                [value, new SqlConstant(ScalarTypes.Default(type), type)],
                type
            )
            : value;
    }

    private SqlQuery Operand(MethodCallExpression call)
    {
        return
            call.Method.GetParameters().Any(IsRejected)
            && !IsOrdinal(call)
            && !IsOrdinallyCompared(call)
            ? throw Unsupported.Method(call.Method)
            : Query(call.Arguments[0]);
    }

    // Counting and testing the rows of a one-row root leave its element unread,
    // unless their predicate reads it.
    private SqlExpression Terminal(SqlQuery source, MethodCallExpression call)
    {
        bool filtered = call.Arguments.Count > 1;
        if (!Countable.Contains(call.Method.Name))
        {
            _ = Sourced(source);
        }
        return call.Method.Name switch
        {
            nameof(Queryable.Count) or nameof(Queryable.LongCount) => Counted(
                filtered ? Where(source, Lambda(call, 1), false) : source,
                call.Method.Name,
                call.Type
            ),
            nameof(Queryable.Sum)
            or nameof(Queryable.Average)
            or nameof(Queryable.Min)
            or nameof(Queryable.Max) => Aggregated(source, call),
            nameof(Queryable.Any) => new SqlExists(
                Existing(
                    filtered
                        ? Where(source, Lambda(call, 1), false)
                        : Open(source, Barrier.None)
                )
            ),
            nameof(Queryable.All) => new SqlUnary(
                "NOT ",
                new SqlExists(Unmatched(source, call)),
                typeof(bool)
            ),
            nameof(Queryable.Contains) => Membership(source, call),
            nameof(Queryable.Last) or nameof(Queryable.LastOrDefault) => Element(
                Reverse(filtered ? Where(source, Lambda(call, 1)) : source, call.Method),
                1,
                call
            ),
            nameof(Queryable.ElementAt) or nameof(Queryable.ElementAtOrDefault) =>
                Element(Positioned(source, Count(call.Arguments[1])), 1, call),
            nameof(Queryable.First) or nameof(Queryable.FirstOrDefault) => Element(
                filtered ? Where(source, Lambda(call, 1)) : source,
                1,
                call
            ),
            nameof(Queryable.Single) or nameof(Queryable.SingleOrDefault) => Element(
                filtered ? Where(source, Lambda(call, 1)) : source,
                2,
                call
            ),
            nameof(Queryable.MinBy) or nameof(Queryable.MaxBy) => Element(
                Extremal(source, call),
                1,
                call
            ),
            _ => throw Unsupported.Method(call.Method),
        };
    }

    // MinBy and MaxBy take the first element in the order of the key, then of the
    // source: an element whose key is null is skipped unless every key is null (nulls
    // last), while a tuple key is never null itself and orders its members as .NET does.
    private SelectQuery Extremal(SqlQuery source, MethodCallExpression call)
    {
        SelectQuery query = Open(source, Barrier.Paging | Barrier.Distinct);
        Shape key = Bind(Lambda(call, 1), query.Shape);
        return key.Columns().Any(column => IsUuid(column.Value.Type))
            ? throw Unsupported.Uuids(call.Method.Name)
            : Ordered(
                query,
                key,
                call.Method.Name == nameof(Queryable.MaxBy),
                false,
                IsOrdinal(call),
                key is ScalarShape
            );
    }

    private static bool IsUuid(Type type)
    {
        return ScalarTypes.Underlying(type) == typeof(Guid);
    }

    private SqlExpression Membership(SqlQuery source, MethodCallExpression call)
    {
        Shape item = Shape(call.Arguments[1]);
        return item is ObjectShape composite
            ? Contained(source, composite)
            : Membership(source, call.Arguments[0], Scalar(item));
    }

    // Contains of a tuple or an object of values among the rows of a sequence of
    // them: a row whose members equal its members, compared as the default comparer
    // compares them, where null equals null.
    private SqlExists Contained(SqlQuery source, ObjectShape item)
    {
        SelectQuery query = Restricted(source, false);
        Restrict(
            query,
            query.Shape is ObjectShape row && row.Members.Count == item.Members.Count
                ? Members(item, row)
                : throw new NotSupportedException(
                    "Contains of a tuple or an object is translated over a sequence of tuples or objects of the same members."
                )
        );
        return new SqlExists(Existing(query));
    }

    // Contains matches a null item with a NULL of the sequence, which IN never does.
    private SqlExpression Membership(
        SqlQuery source,
        Expression sequence,
        SqlExpression item
    )
    {
        _ = Scalar(source.Shape);
        bool nullable = source.Nullability()[0];
        SqlExpression membership = new SqlInQuery(
            item,
            Unordered(Open(source, Barrier.None)),
            nullable
        );
        if (!item.Nullable || !nullable)
        {
            return membership;
        }
        SelectQuery nulls = Restricted(Query(sequence), false);
        Restrict(nulls, new SqlIsNull(Scalar(nulls.Shape), false));
        return Logical(
            "OR",
            membership,
            Logical("AND", new SqlIsNull(item, false), new SqlExists(Existing(nulls)))
        );
    }

    private SqlScalarSubquery Counted(SqlQuery source, string name, Type type)
    {
        SelectQuery query = Unordered(Open(source, Barrier.Aggregation, false));
        query.Shape = new ScalarShape(Counting(name, null, null));
        return new SqlScalarSubquery(query, type);
    }

    private SqlScalarSubquery Aggregated(SqlQuery source, MethodCallExpression call)
    {
        SelectQuery query = Unordered(Open(source, Barrier.Aggregation, false));
        if (IsOrdinallyCompared(call))
        {
            query.Shape = new ScalarShape(Ordinal(Scalar(query.Shape), null, call));
            return new SqlScalarSubquery(query, call.Type);
        }
        SqlExpression value =
            call.Arguments.Count > 1
                ? Scalar(Bind(Lambda(call, 1), query.Shape))
                : Scalar(query.Shape);
        query.Shape = new ScalarShape(
            Aggregate(call.Method.Name, value, call.Type, false, null)
        );
        return new SqlScalarSubquery(query, call.Type);
    }

    // COUNT(DISTINCT x) skips NULL, which Distinct() keeps as one of the values.
    private static SqlExpression Counting(
        string name,
        SqlExpression? distinct,
        SqlExpression? filter,
        bool windowed = false,
        SqlWindow? window = null
    )
    {
        SqlExpression count = new SqlAggregate(
            "COUNT",
            distinct,
            distinct is not null,
            filter,
            typeof(long),
            windowed,
            window
        );
        if (distinct is { Nullable: true })
        {
            SqlAggregate missing = new(
                "BOOL_OR",
                new SqlIsNull(distinct, false),
                false,
                filter,
                typeof(bool)
            );
            count = Calculate(
                "+",
                count,
                new SqlCase([new SqlWhen(missing, Integer(1))], Integer(0), typeof(int))
            );
        }
        return Sized(name, count);
    }

    private static SqlExpression Sized(string name, SqlExpression count)
    {
        return name == nameof(Queryable.Count)
            ? new SqlCast(count, "integer", typeof(int))
            : count;
    }

    private static SqlExpression Aggregate(
        string name,
        SqlExpression value,
        Type type,
        bool distinct,
        SqlExpression? filter,
        bool windowed = false,
        SqlWindow? window = null
    )
    {
        Type result = ScalarTypes.Underlying(type);
        if (name == nameof(Queryable.Sum))
        {
            SqlExpression sum = new SqlFunction(
                "COALESCE",
                [
                    new SqlAggregate(
                        "SUM",
                        value,
                        distinct,
                        filter,
                        type,
                        windowed,
                        window
                    ),
                    new SqlConstant(0, typeof(int)),
                ],
                result
            );
            return Integral.Contains(result)
                ? new SqlCast(sum, ScalarTypes.Number(result), result)
                : sum;
        }
        if (name != nameof(Queryable.Average))
        {
            return new SqlAggregate(
                Extreme(name, value.Type),
                value,
                distinct,
                filter,
                type,
                windowed,
                window
            );
        }
        SqlAggregate Mean(SqlExpression argument)
        {
            return new SqlAggregate(
                "AVG",
                argument,
                distinct,
                filter,
                type,
                windowed,
                window
            );
        }
        // The mean of integers is the exact sum divided once, as .NET computes it:
        // AVG of an integer is a numeric, where the mean of the values as doubles
        // would round each of them first. The mean of decimals has their places.
        return Integral.Contains(ScalarTypes.Underlying(value.Type))
                ? new SqlCast(Mean(value), "double precision", type)
            : IsDecimal(value.Type) ? Fitted(Mean(Padded(value)))
            : Mean(value);
    }

    // PostgreSQL has no MIN or MAX of boolean or uuid: booleans have BOOL_AND and
    // BOOL_OR, and uuids order differently in .NET.
    private static string Extreme(string name, Type type)
    {
        return ScalarTypes.Underlying(type) == typeof(bool)
                ? (name == nameof(Queryable.Max) ? "BOOL_OR" : "BOOL_AND")
            : IsUuid(type) ? throw Unsupported.Uuids(name)
            : name.ToUpperInvariant();
    }

    private SelectQuery Unmatched(SqlQuery source, MethodCallExpression call)
    {
        SelectQuery query = Restricted(source, false);
        Restrict(
            query,
            new SqlUnary("NOT ", Scalar(Bind(Lambda(call, 1), query.Shape)), typeof(bool))
        );
        return Existing(query);
    }

    private static SelectQuery Existing(SelectQuery query)
    {
        query.Shape = new ScalarShape(new SqlConstant(1, typeof(int)));
        return Unordered(query);
    }

    private static SelectQuery Unordered(SelectQuery query)
    {
        if (!query.KeepsOrder)
        {
            query.Orderings = [];
        }
        return query;
    }

    private SelectQuery Positioned(SqlQuery source, int index)
    {
        SelectQuery query = Open(Skip(source, index), Barrier.None);
        query.Offset ??= 0;
        // No element precedes the first one.
        if (index < 0)
        {
            query.Limit = 0;
        }
        return query;
    }

    private SqlScalarSubquery Element(
        SqlQuery source,
        int count,
        MethodCallExpression call
    )
    {
        return new SqlScalarSubquery(Take(source, count), call.Type);
    }

    // The level of a recursion is a source, not a row: it is of no scope.
    private bool IsCorrelation(params Expression[] operands)
    {
        Parameters parameters = new();
        foreach (Expression operand in operands)
        {
            _ = parameters.Visit(operand);
        }
        return parameters
                .Found.Where(_bindings.ContainsKey)
                .Select(parameter => _bindings[parameter].Scope)
                .Distinct()
                .Count() > 1;
    }

    private sealed class Parameters : ExpressionVisitor
    {
        public List<ParameterExpression> Found { get; } = [];

        protected override Expression VisitParameter(ParameterExpression node)
        {
            Found.Add(node);
            return node;
        }
    }
}
