using System.Collections;
using System.Linq.Expressions;
using Linq.Expressions.Translation.PostgreSql.Shapes;
using Linq.Expressions.Translation.PostgreSql.Syntax;

namespace Linq.Expressions.Translation.PostgreSql.Translation;

internal sealed partial class Translator
{
    private static readonly Dictionary<string, string> MathFunctions = new()
    {
        [nameof(Math.Abs)] = "ABS",
        [nameof(Math.Ceiling)] = "CEILING",
        [nameof(Math.Floor)] = "FLOOR",
        [nameof(Math.Truncate)] = "TRUNC",
        [nameof(Math.Sqrt)] = "SQRT",
        [nameof(Math.Pow)] = "POWER",
        [nameof(Math.Exp)] = "EXP",
        [nameof(Math.Log10)] = "LOG",
        [nameof(Math.Max)] = "GREATEST",
        [nameof(Math.Min)] = "LEAST",
    };

    private static readonly Dictionary<string, string> Intervals = new()
    {
        [nameof(DateTime.AddYears)] = "year",
        [nameof(DateTime.AddMonths)] = "month",
        [nameof(DateTime.AddDays)] = "day",
        [nameof(DateTime.AddHours)] = "hour",
        [nameof(DateTime.AddMinutes)] = "minute",
        [nameof(DateTime.AddSeconds)] = "second",
    };

    private static readonly HashSet<Type> Printable =
    [
        typeof(int),
        typeof(long),
        typeof(decimal),
        typeof(double),
        typeof(Guid),
    ];

    private SqlExpression Method(MethodCallExpression call)
    {
        Type owner = call.Method.DeclaringType!;
        string name = call.Method.Name;
        return owner == typeof(Queryable) ? Subquery(call)
            : name == nameof(Equals) && call.Method.GetParameters().All(IsComparable)
                ? Equated(call)
            : name == nameof(ToString) && call.Arguments.Count == 0 ? Stringified(call)
            : name == nameof(Enumerable.Contains) && owner != typeof(string)
                ? Membership(call)
            : owner == typeof(string) ? StringMethod(call)
            : owner == typeof(Math) ? MathMethod(call)
            : Temporal.Contains(owner) ? TemporalMethod(call)
            : name == nameof(Nullable<>.GetValueOrDefault) ? Defaulted(call)
            : name == nameof(Enum.HasFlag) ? Flagged(call)
            : name == nameof(Guid.NewGuid)
                ? new SqlFunction("GEN_RANDOM_UUID", [], typeof(Guid))
            : throw Unsupported.Method(call.Method);
    }

    private Shape Enumerated(MethodCallExpression call)
    {
        Shape source = Shape(call.Arguments[0]);
        return source is GroupingShape grouping ? Grouped(grouping, call)
            : source is GroupJoinShape group
                ? new ScalarShape(Terminal(Correlated(group), call))
            : throw Unsupported.Method(call.Method);
    }

    private SqlExpression Stringified(MethodCallExpression call)
    {
        Type type = call.Object!.Type;
        return type == typeof(string) ? Sql(call.Object)
            : Printable.Contains(type)
                ? new SqlCast(Sql(call.Object), "text", typeof(string))
            : throw Unsupported.Method(call.Method);
    }

    private SqlExpression Equated(MethodCallExpression call)
    {
        return call.Object is null
            ? Equality(Typed(call.Arguments[0]), Typed(call.Arguments[1]), false)
            : Equality(call.Object, Typed(call.Arguments[0]), false);
    }

    private static bool IsComparable(System.Reflection.ParameterInfo parameter)
    {
        return parameter.ParameterType != typeof(StringComparison);
    }

    private static Expression Typed(Expression argument)
    {
        return argument is ConstantExpression { Value: object value }
            ? Expression.Constant(value)
            : argument;
    }

    private SqlFunction Defaulted(MethodCallExpression call)
    {
        SqlExpression fallback =
            call.Arguments.Count == 0
                ? new SqlConstant(Activator.CreateInstance(call.Type), call.Type)
                : Sql(call.Arguments[0]);
        return new SqlFunction("COALESCE", [Sql(call.Object!), fallback], call.Type);
    }

    private SqlExpression StringMethod(MethodCallExpression call)
    {
        IReadOnlyList<Expression> arguments = call.Arguments;
        return (call.Method.Name, arguments.Count) switch
        {
            (nameof(string.ToUpper) or nameof(string.ToUpperInvariant), 0) => Function(
                "UPPER",
                call
            ),
            (nameof(string.ToLower) or nameof(string.ToLowerInvariant), 0) => Function(
                "LOWER",
                call
            ),
            (nameof(string.Trim), _) => Function("BTRIM", call),
            (nameof(string.TrimStart), _) => Function("LTRIM", call),
            (nameof(string.TrimEnd), _) => Function("RTRIM", call),
            (nameof(string.Replace), 2) => Function("REPLACE", call),
            (nameof(string.Substring), _) => Substring(call),
            (nameof(string.IndexOf), 1) => Calculate(
                "-",
                Function("STRPOS", call),
                new SqlConstant(1, typeof(int))
            ),
            (nameof(string.Contains), 1) => Contains(call),
            (nameof(string.StartsWith), 1) => StartsWith(call),
            (nameof(string.EndsWith), 1) => EndsWith(call),
            (nameof(string.IsNullOrEmpty), 1) => IsNullOrEmpty(Sql(arguments[0])),
            (nameof(string.Concat), _) => Concatenation(call),
            _ => throw Unsupported.Method(call.Method),
        };
    }

    private SqlFunction Function(string name, MethodCallExpression call)
    {
        return new SqlFunction(
            name,
            [.. call.Arguments.Prepend(call.Object).OfType<Expression>().Select(Sql)],
            call.Type
        );
    }

    private SqlExpression Substring(MethodCallExpression call)
    {
        SqlExpression start = Sql(call.Arguments[0]);
        SqlExpression position = start is SqlConstant { Value: int index }
            ? new SqlConstant(index + 1, typeof(int))
            : Calculate("+", start, new SqlConstant(1, typeof(int)));
        return new SqlFunction(
            "SUBSTRING",
            [Sql(call.Object!), position, .. call.Arguments.Skip(1).Select(Sql)],
            typeof(string)
        );
    }

    private SqlExpression Contains(MethodCallExpression call)
    {
        SqlExpression text = Sql(call.Object!);
        SqlExpression part = Sql(call.Arguments[0]);
        return part is SqlConstant { Value: object value }
            ? Like(text, "%" + Escaped(value) + "%")
            : Compare(
                ">",
                new SqlFunction("STRPOS", [text, part], typeof(int)),
                new SqlConstant(0, typeof(int))
            );
    }

    private SqlExpression StartsWith(MethodCallExpression call)
    {
        SqlExpression text = Sql(call.Object!);
        SqlExpression part = Sql(call.Arguments[0]);
        return part is SqlConstant { Value: object value }
            ? Like(text, Escaped(value) + "%")
            : new SqlFunction("STARTS_WITH", [text, part], typeof(bool));
    }

    private SqlExpression EndsWith(MethodCallExpression call)
    {
        SqlExpression text = Sql(call.Object!);
        SqlExpression part = Sql(call.Arguments[0]);
        return part is SqlConstant { Value: object value }
            ? Like(text, "%" + Escaped(value))
            : Compare(
                "=",
                new SqlFunction(
                    "RIGHT",
                    [text, new SqlFunction("LENGTH", [part], typeof(int))],
                    typeof(string)
                ),
                part
            );
    }

    private static string Escaped(object value)
    {
        return value
            .ToString()!
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
    }

    private static SqlBinary Like(SqlExpression text, string pattern)
    {
        return Compare("LIKE", text, Text(pattern));
    }

    private static SqlBinary IsNullOrEmpty(SqlExpression text)
    {
        return Logical(
            "OR",
            new SqlIsNull(text, false),
            new SqlBinary(
                "=",
                SqlBinaryKind.Comparison,
                text,
                Text(string.Empty),
                typeof(bool),
                false
            )
        );
    }

    private static SqlBinary Compare(
        string symbol,
        SqlExpression left,
        SqlExpression right
    )
    {
        return new SqlBinary(
            symbol,
            SqlBinaryKind.Comparison,
            left,
            right,
            typeof(bool),
            left.Nullable
        );
    }

    private static SqlBinary Calculate(
        string symbol,
        SqlExpression left,
        SqlExpression right
    )
    {
        return new SqlBinary(
            symbol,
            SqlBinaryKind.Arithmetic,
            left,
            right,
            left.Type,
            left.Nullable
        );
    }

    private SqlExpression MathMethod(MethodCallExpression call)
    {
        string name = call.Method.Name;
        List<SqlExpression> arguments = [.. call.Arguments.Select(Sql)];
        return name switch
        {
            nameof(Math.Log) when arguments.Count == 1 => new SqlFunction(
                "LN",
                arguments,
                call.Type
            ),
            nameof(Math.Sign) => new SqlCast(
                new SqlFunction("SIGN", arguments, call.Type),
                "integer",
                typeof(int)
            ),
            nameof(Math.Round)
                when call.Method.GetParameters()[0].ParameterType == typeof(decimal)
                    && call.Arguments[^1]
                        is ConstantExpression { Value: MidpointRounding.AwayFromZero } =>
                new SqlFunction("ROUND", arguments[..^1], call.Type),
            _ when MathFunctions.TryGetValue(name, out string? function) =>
                new SqlFunction(function, arguments, call.Type),
            _ => throw Unsupported.Method(call.Method),
        };
    }

    private SqlExpression TemporalMethod(MethodCallExpression call)
    {
        string name = call.Method.Name;
        return name == nameof(DateOnly.FromDateTime)
                ? new SqlCast(Sql(call.Arguments[0]), "date", typeof(DateOnly))
            : call.Method.DeclaringType == typeof(DateOnly)
            && name == nameof(DateOnly.AddDays)
                ? Calculate("+", Sql(call.Object!), Sql(call.Arguments[0]))
            : Intervals.TryGetValue(name, out string? unit)
                ? Calculate(
                    "+",
                    Sql(call.Object!),
                    Calculate(
                        "*",
                        Sql(call.Arguments[0]),
                        new SqlFragment($"INTERVAL '1 {unit}'", typeof(TimeSpan))
                    )
                )
            : throw Unsupported.Method(call.Method);
    }

    private SqlBinary Flagged(MethodCallExpression call)
    {
        SqlExpression flag = Sql(call.Arguments[0]);
        return Compare("=", Calculate("&", Sql(call.Object!), flag), flag);
    }

    private SqlExpression Membership(MethodCallExpression call)
    {
        (Expression source, Expression item) = call.Object is null
            ? (Unspanned(call.Arguments[0]), call.Arguments[1])
            : (call.Object, call.Arguments[0]);
        SqlExpression value = Sql(item);
        if (source is not ConstantExpression { Value: IEnumerable values })
        {
            return new SqlAny(value, Sql(source));
        }
        List<SqlExpression> present =
        [
            .. values
                .Cast<object?>()
                .OfType<object>()
                .Select(element => new SqlConstant(element, element.GetType())),
        ];
        SqlExpression membership =
            present.Count == 0
                ? new SqlConstant(false, typeof(bool))
                : new SqlIn(value, present);
        return values.Cast<object?>().Contains(null)
            ? Logical("OR", membership, new SqlIsNull(value, false))
            : membership;
    }

    private static Expression Unspanned(Expression source)
    {
        Expression array = source
            is MethodCallExpression { Method.Name: "op_Implicit" } span
            ? span.Arguments[0]
            : source;
        return array is UnaryExpression { NodeType: ExpressionType.Convert } conversion
            ? conversion.Operand
            : array;
    }
}
