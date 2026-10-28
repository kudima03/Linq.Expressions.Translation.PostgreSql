using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Linq.Expressions.Translation.PostgreSql.Mapping;
using Linq.Expressions.Translation.PostgreSql.Shapes;
using Linq.Expressions.Translation.PostgreSql.Syntax;

namespace Linq.Expressions.Translation.PostgreSql.Translation;

internal sealed partial class Translator
{
    private static readonly Dictionary<ExpressionType, string> Arithmetic = new()
    {
        [ExpressionType.Add] = "+",
        [ExpressionType.AddChecked] = "+",
        [ExpressionType.Subtract] = "-",
        [ExpressionType.SubtractChecked] = "-",
        [ExpressionType.Multiply] = "*",
        [ExpressionType.MultiplyChecked] = "*",
        [ExpressionType.Divide] = "/",
        [ExpressionType.Modulo] = "%",
        [ExpressionType.And] = "&",
        [ExpressionType.Or] = "|",
        [ExpressionType.ExclusiveOr] = "#",
        [ExpressionType.LeftShift] = "<<",
        [ExpressionType.RightShift] = ">>",
    };

    private static readonly Dictionary<ExpressionType, string> Comparisons = new()
    {
        [ExpressionType.LessThan] = "<",
        [ExpressionType.LessThanOrEqual] = "<=",
        [ExpressionType.GreaterThan] = ">",
        [ExpressionType.GreaterThanOrEqual] = ">=",
    };

    private static readonly Dictionary<
        ExpressionType,
        (string Symbol, SqlBinaryKind Kind)
    > Connectives = new()
    {
        [ExpressionType.And] = ("AND", SqlBinaryKind.Logical),
        [ExpressionType.AndAlso] = ("AND", SqlBinaryKind.Logical),
        [ExpressionType.Or] = ("OR", SqlBinaryKind.Logical),
        [ExpressionType.OrElse] = ("OR", SqlBinaryKind.Logical),
        [ExpressionType.ExclusiveOr] = ("<>", SqlBinaryKind.Comparison),
    };

    private static readonly HashSet<Type> Fractional =
    [
        typeof(float),
        typeof(double),
        typeof(decimal),
    ];

    private static bool IsBoolean(Type type)
    {
        return ScalarTypes.Underlying(type) == typeof(bool);
    }

    private static SqlExpression Scalar(Shape shape)
    {
        return shape is ScalarShape scalar
            ? scalar.Value
            : throw new NotSupportedException(
                "Entities and projections cannot be used where a single value is required."
            );
    }

    private SqlExpression Sql(Expression expression)
    {
        return Scalar(Shape(expression));
    }

    private Shape Shape(Expression expression)
    {
        return expression switch
        {
            ParameterExpression parameter => Resolved(parameter),
            MemberExpression member => Member(member),
            ConstantExpression { Value: ITuple tuple } => Tupled(tuple, expression.Type),
            NewExpression creation => Created(creation),
            MemberInitExpression initialization => Initialization(initialization),
            MethodCallExpression call
                when call.Method.DeclaringType == typeof(Enumerable)
                    && call.Method.Name != nameof(Enumerable.Contains) => Enumerated(
                call
            ),
            UnaryExpression
            {
                NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked
            } conversion => Conversion(conversion),
            _ => new ScalarShape(Node(expression)),
        };
    }

    private Shape Resolved(ParameterExpression parameter)
    {
        Binding binding = _bindings[parameter];
        binding.Uses++;
        return binding.Shape is RowShape row ? throw row.Rejected() : binding.Shape;
    }

    // A constant tuple is projected member by member, as a constructed one is: each
    // element is a literal, NULL for a default. The eighth and later elements live
    // in a nested tuple, which ITuple flattens away.
    private static ObjectShape Tupled(ITuple tuple, Type type)
    {
        Type[] elements = type.GetGenericArguments();
        if (elements.Length > 7)
        {
            throw new NotSupportedException(
                $"Constants of type '{type.Name}' cannot be rendered as SQL literals: tuples of more than seven elements are not supported."
            );
        }
        List<ObjectMember> members = [];
        for (int index = 0; index < elements.Length; index++)
        {
            Shape element = tuple[index] is ITuple nested
                ? Tupled(nested, elements[index])
                : new ScalarShape(new SqlConstant(tuple[index], elements[index]));
            members.Add(new ObjectMember("item" + (index + 1), element, elements[index]));
        }
        return new ObjectShape(members);
    }

    private Shape Created(NewExpression creation)
    {
        return
            creation.Type == typeof(DateTime)
            && creation.Arguments is [Expression { Type: Type first }, ..]
            && first == typeof(long)
            ? new ScalarShape(Moment(creation))
            : New(creation);
    }

    private SqlExpression Node(Expression expression)
    {
        return expression switch
        {
            ConstantExpression constant => new SqlConstant(constant.Value, constant.Type),
            DeferredConstant deferred => new SqlDeferred(Node(deferred.Operand)),
            BinaryExpression binary => Binary(binary),
            UnaryExpression { NodeType: ExpressionType.ArrayLength } length =>
                new SqlFunction("CARDINALITY", [Sql(length.Operand)], typeof(int)),
            UnaryExpression unary => Unary(unary),
            ConditionalExpression conditional => Conditional(conditional),
            MethodCallExpression call => Method(call),
            _ => throw Unsupported.Node(expression),
        };
    }

    private Shape Member(MemberExpression member)
    {
        if (member.Expression is null)
        {
            return new ScalarShape(StaticMember(member.Member));
        }
        Shape owner = Shape(member.Expression);
        return owner is ScalarShape scalar
                ? new ScalarShape(ScalarMember(scalar.Value, member.Member))
            : owner is EntityShape entity ? entity.Member(member.Member)
            : owner is GroupingShape grouping ? grouping.Key
            : ((ObjectShape)owner).Member(member.Member);
    }

    private ObjectShape New(NewExpression creation)
    {
        if (ScalarTypes.IsScalar(creation.Type))
        {
            throw new NotSupportedException(
                $"Constructing '{creation.Type.Name}' from SQL values is not supported."
            );
        }
        // A struct created without arguments has no constructor, so names are read
        // only for arguments.
        return new ObjectShape([
            .. creation.Arguments.Select(
                (argument, index) =>
                    new ObjectMember(
                        creation.Constructor!.GetParameters()[index].Name!,
                        Ungrouped(Shape(argument)),
                        argument.Type
                    )
            ),
        ]);
    }

    // A nested group has no SQL value; projected, it would silently become its key.
    private static Shape Ungrouped(Shape shape)
    {
        return shape is GroupingShape
            ? throw new NotSupportedException(
                "Returning or projecting IGrouping results of GroupBy is not supported; aggregate the groups or select their keys."
            )
            : shape;
    }

    private ObjectShape Initialization(MemberInitExpression initialization)
    {
        List<ObjectMember> assigned =
        [
            .. initialization.Bindings.Select(binding =>
                binding is MemberAssignment assignment
                    ? new ObjectMember(
                        assignment.Member.Name,
                        Ungrouped(Shape(assignment.Expression)),
                        assignment.Expression.Type
                    )
                    : throw Unsupported.Binding(binding)
            ),
        ];
        // An initializer runs after the constructor, so it replaces the argument.
        return new ObjectShape([
            .. New(initialization.NewExpression)
                .Members.Where(argument =>
                    !assigned.Any(member => member.Matches(argument.Name))
                ),
            .. assigned,
        ]);
    }

    private Shape Conversion(UnaryExpression conversion)
    {
        Shape operand = Shape(conversion.Operand);
        return operand is ScalarShape scalar && ScalarTypes.IsScalar(conversion.Type)
            ? new ScalarShape(
                Convert(scalar.Value, conversion.Operand.Type, conversion.Type)
            )
            : operand;
    }

    private static SqlExpression Convert(SqlExpression value, Type from, Type to)
    {
        Type source = ScalarTypes.Underlying(from);
        Type target = ScalarTypes.Underlying(to);
        if (source == target)
        {
            return value;
        }
        // Only a DateTime converts to a DateTimeOffset: a timestamp compares with a
        // timestamptz through the session time zone, as the conversion uses the local one.
        if (target == typeof(DateTimeOffset))
        {
            return value;
        }
        if (!ScalarTypes.IsNumber(target))
        {
            throw new NotSupportedException(
                $"Converting '{from.Name}' to '{to.Name}' is not supported."
            );
        }
        SqlExpression operand =
            Fractional.Contains(source) && !Fractional.Contains(target)
                ? new SqlFunction("TRUNC", [value], value.Type)
                : value;
        return new SqlCast(operand, ScalarTypes.Number(target), to);
    }

    private SqlExpression Binary(BinaryExpression binary)
    {
        if (binary.NodeType is ExpressionType.Equal or ExpressionType.NotEqual)
        {
            return Equality(
                binary.Left,
                binary.Right,
                binary.NodeType == ExpressionType.NotEqual
            );
        }
        if (binary.NodeType == ExpressionType.Coalesce)
        {
            List<SqlExpression> arguments = [];
            Coalesced(binary, arguments);
            return new SqlFunction(
                "COALESCE",
                arguments,
                binary.Type,
                arguments[^1].Nullable
            );
        }
        if (binary.NodeType == ExpressionType.Add && binary.Type == typeof(string))
        {
            return Concatenation(binary);
        }
        if (
            Ordinal(binary.Left, binary.Right) is MethodCallExpression ordinal
            && Comparisons.TryGetValue(binary.NodeType, out string? ranking)
        )
        {
            return Ranked(ordinal, ranking);
        }
        (string symbol, SqlBinaryKind kind) = Operator(binary);
        SqlExpression left = Sql(binary.Left);
        SqlExpression right = binary.NodeType
            is ExpressionType.LeftShift
                or ExpressionType.RightShift
            ? Masked(binary)
            : Sql(binary.Right);
        return binary.NodeType == ExpressionType.Modulo && IsFloating(binary.Type)
            ? Remainder(left, right, binary.Type)
            : new SqlBinary(
                symbol,
                kind,
                left,
                right,
                binary.Type,
                left.Nullable || right.Nullable
            );
    }

    private static (string Symbol, SqlBinaryKind Kind) Operator(BinaryExpression binary)
    {
        return IsBoolean(binary.Left.Type)
            && Connectives.TryGetValue(
                binary.NodeType,
                out (string Symbol, SqlBinaryKind Kind) connective
            )
                ? connective
            : Comparisons.TryGetValue(binary.NodeType, out string? comparison)
                ? (comparison, SqlBinaryKind.Comparison)
            : Arithmetic.TryGetValue(binary.NodeType, out string? arithmetic)
                ? (arithmetic, SqlBinaryKind.Arithmetic)
            : throw Unsupported.Node(binary);
    }

    private static bool IsFloating(Type type)
    {
        Type value = ScalarTypes.Underlying(type);
        return value == typeof(float) || value == typeof(double);
    }

    // PostgreSQL has no % for real or double precision: the remainder is taken as
    // numeric and converted back. A literal is already numeric.
    private static SqlCast Remainder(SqlExpression left, SqlExpression right, Type type)
    {
        SqlBinary remainder = new(
            "%",
            SqlBinaryKind.Arithmetic,
            Exact(left),
            Exact(right),
            typeof(decimal),
            left.Nullable || right.Nullable
        );
        return new SqlCast(
            remainder,
            ScalarTypes.Number(ScalarTypes.Underlying(type)),
            type
        );
    }

    private static SqlExpression Exact(SqlExpression operand)
    {
        return operand is SqlConstant
            ? operand
            : new SqlCast(operand, "numeric", typeof(decimal));
    }

    private SqlExpression Masked(BinaryExpression shift)
    {
        int mask = ScalarTypes.Underlying(shift.Left.Type) == typeof(long) ? 63 : 31;
        SqlExpression count = Sql(shift.Right);
        return count is SqlConstant { Value: int value }
            ? Integer(value & mask)
            : Calculate("&", count, Integer(mask));
    }

    private SqlExpression Equality(Expression left, Expression right, bool negated)
    {
        if (left is ConstantExpression { Value: null })
        {
            return Nullity(right, negated);
        }
        if (right is ConstantExpression { Value: null })
        {
            return Nullity(left, negated);
        }
        if (Ordinal(left, right) is MethodCallExpression ordinal)
        {
            return Equated(
                new SqlCollation(Sql(ordinal.Arguments[0])),
                Sql(ordinal.Arguments[1]),
                negated,
                IsCorrelation(ordinal.Arguments[0], ordinal.Arguments[1])
            );
        }
        Shape first = Shape(left);
        Shape second = Shape(right);
        return first is not ScalarShape || second is not ScalarShape
            ? throw new NotSupportedException(
                "Comparing entities or projections for equality is not supported."
            )
            : Equated(Scalar(first), Scalar(second), negated, IsCorrelation(left, right));
    }

    // A correlation key equal to NULL is false in both languages, but not equal to it
    // is true in C# and NULL in SQL.
    private static SqlExpression Equated(
        SqlExpression one,
        SqlExpression other,
        bool negated,
        bool correlation
    )
    {
        bool nullable = one.Nullable || other.Nullable;
        bool distinct =
            (one.Nullable && other.Nullable) || (nullable && (negated || !correlation));
        return distinct
            ? Distinct(one, other, negated)
            : new SqlBinary(
                negated ? "<>" : "=",
                SqlBinaryKind.Comparison,
                one,
                other,
                typeof(bool),
                nullable
            );
    }

    // IS [NOT] DISTINCT FROM is never NULL.
    private static SqlBinary Distinct(
        SqlExpression left,
        SqlExpression right,
        bool negated
    )
    {
        return new SqlBinary(
            negated ? "IS DISTINCT FROM" : "IS NOT DISTINCT FROM",
            SqlBinaryKind.Comparison,
            left,
            right,
            typeof(bool),
            false
        );
    }

    private SqlExpression Nullity(Expression expression, bool negated)
    {
        Shape shape = Shape(expression);
        return shape is EntityShape entity ? Nullity(entity, negated)
            : shape is ObjectShape composite ? Nullity(composite, negated)
            : new SqlIsNull(Scalar(shape), negated);
    }

    // A constructed object is never null, unless a left join yields it for an outer row
    // without a match, which leaves its columns of presence NULL.
    private static SqlExpression Nullity(ObjectShape composite, bool negated)
    {
        if (composite.Presence is null)
        {
            return new SqlConstant(negated, typeof(bool));
        }
        Dictionary<string, SqlExpression> columns = composite
            .Columns()
            .ToDictionary(column => column.Name!, column => column.Value);
        return composite
            .Presence.Select(path => (SqlExpression)new SqlIsNull(columns[path], negated))
            .Aggregate((first, second) => Logical(negated ? "OR" : "AND", first, second));
    }

    // An entity is null only where a left join found no row, which leaves every column
    // NULL; a column the table requires tells, and without one all columns have to.
    private static SqlExpression Nullity(EntityShape entity, bool negated)
    {
        EntityColumn? required = entity.Fields.FirstOrDefault(column => column.Required);
        return required is null
                ? entity
                    .Fields.Select(column =>
                        (SqlExpression)new SqlIsNull(column.Value, negated)
                    )
                    .Aggregate(
                        (first, second) => Logical(negated ? "OR" : "AND", first, second)
                    )
            : required.Value.Nullable ? new SqlIsNull(required.Value, negated)
            : new SqlConstant(negated, typeof(bool));
    }

    private void Coalesced(Expression expression, List<SqlExpression> arguments)
    {
        if (expression is BinaryExpression { NodeType: ExpressionType.Coalesce } coalesce)
        {
            Coalesced(coalesce.Left, arguments);
            Coalesced(coalesce.Right, arguments);
            return;
        }
        arguments.Add(Sql(expression));
    }

    private SqlExpression Concatenation(Expression expression)
    {
        List<SqlExpression> parts = [];
        Concatenated(expression, parts);
        return Joined(parts);
    }

    private static SqlExpression Joined(List<SqlExpression> parts)
    {
        return parts.Count == 0
            ? Text(string.Empty)
            : Textual(parts)
                .Aggregate(
                    (left, right) =>
                        new SqlBinary(
                            "||",
                            SqlBinaryKind.Concatenation,
                            left,
                            right,
                            typeof(string),
                            false
                        )
                );
    }

    // || needs a text operand: a lone value is cast, and so is the first of two leading
    // values that are not text; from then on the left operand is text.
    private static IEnumerable<SqlExpression> Textual(List<SqlExpression> parts)
    {
        return !IsText(parts[0]) && (parts.Count == 1 || !IsText(parts[1]))
            ? parts.Skip(1).Prepend(new SqlCast(parts[0], "text", typeof(string)))
            : parts;
    }

    private static bool IsText(SqlExpression part)
    {
        return part.Type == typeof(string);
    }

    private void Concatenated(Expression expression, List<SqlExpression> parts)
    {
        if (expression is BinaryExpression { NodeType: ExpressionType.Add } addition)
        {
            Concatenated(addition.Left, parts);
            Concatenated(addition.Right, parts);
            return;
        }
        if (
            expression is MethodCallExpression { Method.Name: nameof(string.Concat) } call
        )
        {
            foreach (Expression argument in call.Arguments)
            {
                Listed(argument, parts);
            }
            return;
        }
        Appended(expression, parts);
    }

    private void Listed(Expression argument, List<SqlExpression> parts)
    {
        if (argument is NewArrayExpression array)
        {
            foreach (Expression element in array.Expressions)
            {
                Concatenated(element, parts);
            }
            return;
        }
        Concatenated(argument, parts);
    }

    private void Appended(Expression expression, List<SqlExpression> parts)
    {
        Expression value =
            expression is UnaryExpression { NodeType: ExpressionType.Convert } boxing
            && boxing.Type == typeof(object)
                ? boxing.Operand
                : expression;
        Type type = value is ConstantExpression { Value: object constant }
            ? constant.GetType()
            : Nullable.GetUnderlyingType(value.Type) ?? value.Type;
        if (type != typeof(string) && type != typeof(char) && !Printable.Contains(type))
        {
            throw new NotSupportedException(
                $"Concatenating '{type.Name}' values is not supported: PostgreSQL formats them differently from .NET."
            );
        }
        SqlExpression part = Sql(value);
        parts.Add(
            part.Nullable
                ? new SqlFunction(
                    "COALESCE",
                    [
                        type == typeof(string)
                            ? part
                            : new SqlCast(part, "text", typeof(string)),
                        Text(string.Empty),
                    ],
                    typeof(string)
                )
                : part
        );
    }

    private SqlUnary Unary(UnaryExpression unary)
    {
        string symbol =
            unary.NodeType is ExpressionType.Negate or ExpressionType.NegateChecked ? "-"
            : unary.NodeType != ExpressionType.Not ? throw Unsupported.Node(unary)
            : IsBoolean(unary.Type) ? "NOT "
            : "~";
        return new SqlUnary(symbol, Sql(unary.Operand), unary.Type);
    }

    private SqlCase Conditional(ConditionalExpression conditional)
    {
        List<SqlWhen> whens =
        [
            new SqlWhen(Sql(conditional.Test), Sql(conditional.IfTrue)),
        ];
        SqlExpression otherwise = Sql(conditional.IfFalse);
        if (otherwise is SqlCase nested)
        {
            whens.AddRange(nested.Whens);
            otherwise = nested.Otherwise;
        }
        return new SqlCase(whens, otherwise, conditional.Type);
    }
}
