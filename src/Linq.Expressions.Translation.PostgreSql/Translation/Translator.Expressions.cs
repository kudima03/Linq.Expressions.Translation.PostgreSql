using System.Linq.Expressions;
using System.Reflection;
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
        [ExpressionType.Multiply] = "*",
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
        return ((ScalarShape)shape).Value;
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
            NewExpression creation => New(creation),
            MemberInitExpression initialization => Initialization(initialization),
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
        return binding.Shape;
    }

    private SqlExpression Node(Expression expression)
    {
        return expression switch
        {
            ConstantExpression constant => new SqlConstant(constant.Value, constant.Type),
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
        ParameterInfo[] parameters = creation.Constructor!.GetParameters();
        return new ObjectShape([
            .. creation.Arguments.Select(
                (argument, index) =>
                    new ObjectMember(parameters[index].Name!, Shape(argument))
            ),
        ]);
    }

    private ObjectShape Initialization(MemberInitExpression initialization)
    {
        return new ObjectShape([
            .. New(initialization.NewExpression).Members,
            .. initialization
                .Bindings.Cast<MemberAssignment>()
                .Select(binding => new ObjectMember(
                    binding.Member.Name,
                    Shape(binding.Expression)
                )),
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
        (string symbol, SqlBinaryKind kind) = Operator(binary);
        SqlExpression left = Sql(binary.Left);
        SqlExpression right = Sql(binary.Right);
        return new SqlBinary(
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

    private SqlExpression Equality(Expression left, Expression right, bool negated)
    {
        if (left is ConstantExpression { Value: null })
        {
            return new SqlIsNull(Sql(right), negated);
        }
        if (right is ConstantExpression { Value: null })
        {
            return new SqlIsNull(Sql(left), negated);
        }
        Shape first = Shape(left);
        Shape second = Shape(right);
        if (first is not ScalarShape || second is not ScalarShape)
        {
            throw new NotSupportedException(
                "Comparing entities or projections for equality is not supported."
            );
        }
        SqlExpression one = Scalar(first);
        SqlExpression other = Scalar(second);
        bool nullable = (one.Nullable || other.Nullable) && !IsCorrelation(left, right);
        string symbol =
            nullable ? (negated ? "IS DISTINCT FROM" : "IS NOT DISTINCT FROM")
            : negated ? "<>"
            : "=";
        return new SqlBinary(
            symbol,
            SqlBinaryKind.Comparison,
            one,
            other,
            typeof(bool),
            false
        );
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
        return parts.Aggregate(
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
                Concatenated(argument, parts);
            }
            return;
        }
        SqlExpression part = Sql(expression);
        parts.Add(
            part.Nullable
                ? new SqlFunction(
                    "COALESCE",
                    [part, new SqlConstant(string.Empty, typeof(string))],
                    typeof(string)
                )
                : part
        );
    }

    private SqlUnary Unary(UnaryExpression unary)
    {
        string symbol =
            unary.NodeType != ExpressionType.Not ? "-"
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
