using System.Collections;
using System.Globalization;
using System.Linq.Expressions;
using System.Text;
using Linq.Expressions.Translation.PostgreSql.Mapping;
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
        [nameof(DateTime.AddMilliseconds)] = "millisecond",
    };

    private static readonly HashSet<Type> Numeric =
    [
        typeof(int),
        typeof(long),
        typeof(decimal),
        typeof(double),
    ];

    private static readonly string WhiteSpace = string.Concat(
        Enumerable
            .Range(char.MinValue, char.MaxValue)
            .Select(code => (char)code)
            .Where(char.IsWhiteSpace)
    );

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
            : name == nameof(long.Parse) && call.Object is null ? Parsed(call)
            : name == nameof(TimeSpan.FromTicks) && owner == typeof(TimeSpan)
                ? Microseconds(Sql(call.Arguments[0]))
            : owner == typeof(string) ? StringMethod(call)
            : owner == typeof(Math) || Numeric.Contains(owner) ? MathMethod(call)
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
                ? Terminals.Contains(call.Method.Name)
                        ? new ScalarShape(Valued(Terminal(Correlated(group), call), call))
                    : group.Then(call)
            : source is ScalarShape { Value: SqlConstant or { Type.IsArray: true } } items
            && Matched(call) is Expression item
                ? new ScalarShape(Membership(items.Value, Sql(item)))
            : source is ScalarShape { Value.Type.IsArray: true } array && IsCounted(call)
                ? new ScalarShape(
                    new SqlFunction("CARDINALITY", [array.Value], call.Type)
                )
            : source is ScalarShape { Value.Type.IsArray: true } elements
            && call.Method.Name == nameof(Enumerable.Any)
            && call.Arguments.Count == 1
                ? new ScalarShape(
                    Compare(
                        ">",
                        new SqlFunction("CARDINALITY", [elements.Value], typeof(int)),
                        Integer(0)
                    )
                )
            : throw Unsupported.Method(call.Method);
    }

    private static bool IsCounted(MethodCallExpression call)
    {
        return call.Arguments.Count == 1
            && call.Method.Name
                is nameof(Enumerable.Count)
                    or nameof(Enumerable.LongCount);
    }

    // xs.Any(x => x == item) is xs.Contains(item) when item does not depend on x.
    private static Expression? Matched(MethodCallExpression call)
    {
        LambdaExpression? lambda =
            call.Method.Name == nameof(Enumerable.Any)
                ? call.Arguments[^1] as LambdaExpression
                : null;
        if (lambda?.Body is not BinaryExpression { NodeType: ExpressionType.Equal } equal)
        {
            return null;
        }
        ParameterExpression element = lambda.Parameters[0];
        Expression? item =
            Unlifted(equal.Left) == element ? equal.Right
            : Unlifted(equal.Right) == element ? equal.Left
            : null;
        Parameters parameters = new();
        _ = parameters.Visit(item);
        return parameters.Found.Contains(element) ? null : item;
    }

    private static Expression Unlifted(Expression expression)
    {
        return
            expression is UnaryExpression { NodeType: ExpressionType.Convert } conversion
            ? conversion.Operand
            : expression;
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
                ? new SqlConstant(ScalarTypes.Default(call.Type), call.Type)
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
            (nameof(string.Trim), _) => Trimmed("BTRIM", call),
            (nameof(string.TrimStart), _) => Trimmed("LTRIM", call),
            (nameof(string.TrimEnd), _) => Trimmed("RTRIM", call),
            (nameof(string.Replace), 2) => Function("REPLACE", call),
            (nameof(string.Substring), _) => Substring(
                Sql(call.Object!),
                Sql(arguments[0]),
                [.. arguments.Skip(1).Select(Sql)]
            ),
            (nameof(string.Remove), _) => Removed(call),
            (nameof(string.IndexOf), 1) => Calculate(
                "-",
                Function("STRPOS", call),
                Integer(1)
            ),
            (nameof(string.IndexOf), _)
                when arguments.Skip(1).All(argument => argument.Type == typeof(int)) =>
                Located(call),
            (nameof(string.LastIndexOf), 1) => LastLocated(call),
            (nameof(string.IndexOfAny), 1) => AnyLocated(call),
            (nameof(string.Contains), _) when IsOrdinal(arguments) => Contains(call),
            (nameof(string.StartsWith), _) when IsOrdinal(arguments) => StartsWith(call),
            (nameof(string.EndsWith), _) when IsOrdinal(arguments) => EndsWith(call),
            (nameof(string.IsNullOrEmpty), 1) => IsNullOrEmpty(Sql(arguments[0])),
            (nameof(string.Concat), _) => Concatenation(call),
            (nameof(string.Format), _) => Formatted(call),
            _ => throw Unsupported.Method(call.Method),
        };
    }

    private SqlExpression Formatted(MethodCallExpression call)
    {
        if (call.Arguments[0] is not ConstantExpression { Value: string format })
        {
            throw Unsupported.Method(call.Method);
        }
        IReadOnlyList<Expression> arguments = call.Arguments[1]
            is NewArrayExpression array
            ? array.Expressions
            : call.Arguments.Skip(1).ToList();
        List<SqlExpression> parts = [];
        StringBuilder literal = new();
        for (int index = 0; index < format.Length; index++)
        {
            char current = format[index];
            if (current is '{' or '}' && format.ElementAtOrDefault(index + 1) == current)
            {
                _ = literal.Append(current);
                index++;
            }
            else if (current == '{')
            {
                int end = format.IndexOf('}', index);
                Flushed(literal, parts);
                Concatenated(arguments[FormatIndex(format[index..(end + 1)])], parts);
                index = end;
            }
            else
            {
                _ = literal.Append(current);
            }
        }
        Flushed(literal, parts);
        return Joined(parts);
    }

    private static void Flushed(StringBuilder literal, List<SqlExpression> parts)
    {
        if (literal.Length > 0)
        {
            parts.Add(Text(literal.ToString()));
            _ = literal.Clear();
        }
    }

    private static int FormatIndex(string item)
    {
        return int.TryParse(
            item[1..^1],
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out int index
        )
            ? index
            : throw new NotSupportedException(
                $"The format item '{item}' is not supported: alignment and format strings have no PostgreSQL equivalent."
            );
    }

    private SqlFunction Function(string name, MethodCallExpression call)
    {
        return new SqlFunction(
            name,
            [.. call.Arguments.Prepend(call.Object).OfType<Expression>().Select(Sql)],
            call.Type
        );
    }

    // Without characters, Trim removes every white-space character, BTRIM only spaces.
    private SqlFunction Trimmed(string name, MethodCallExpression call)
    {
        List<SqlExpression> arguments =
        [
            Sql(call.Object!),
            .. call
                .Arguments.Where(argument =>
                    argument is not ConstantExpression { Value: char[] { Length: 0 } }
                )
                .Select(argument =>
                    argument is ConstantExpression { Value: char[] characters }
                        ? Text(new string(characters))
                        : Sql(argument)
                ),
        ];
        if (arguments.Count == 1)
        {
            arguments.Add(Text(WhiteSpace));
        }
        return new SqlFunction(name, arguments, call.Type);
    }

    private static SqlFunction Substring(
        SqlExpression text,
        SqlExpression start,
        IReadOnlyList<SqlExpression> length
    )
    {
        return new SqlFunction(
            "SUBSTRING",
            [text, Added(start, Integer(1)), .. length],
            typeof(string)
        );
    }

    private static SqlExpression Added(SqlExpression left, SqlExpression right)
    {
        return
            left is SqlConstant { Value: int first }
            && right is SqlConstant { Value: int second }
            ? Integer(first + second)
            : Calculate("+", left, right);
    }

    private SqlExpression Removed(MethodCallExpression call)
    {
        SqlExpression text = Sql(call.Object!);
        SqlExpression start = Sql(call.Arguments[0]);
        SqlExpression head = new SqlFunction(
            "SUBSTRING",
            [text, Integer(1), start],
            typeof(string)
        );
        return call.Arguments.Count == 1
            ? head
            : new SqlBinary(
                "||",
                SqlBinaryKind.Concatenation,
                head,
                Substring(text, Added(start, Sql(call.Arguments[1])), []),
                typeof(string),
                false
            );
    }

    private SqlExpression Located(MethodCallExpression call)
    {
        SqlExpression start = Sql(call.Arguments[1]);
        SqlExpression position = new SqlFunction(
            "STRPOS",
            [
                Substring(
                    Sql(call.Object!),
                    start,
                    [.. call.Arguments.Skip(2).Select(Sql)]
                ),
                Sql(call.Arguments[0]),
            ],
            typeof(int)
        );
        return Found(
            position,
            Calculate("+", Calculate("-", position, Integer(1)), start)
        );
    }

    private SqlExpression LastLocated(MethodCallExpression call)
    {
        SqlExpression text = Sql(call.Object!);
        SqlExpression part = Sql(call.Arguments[0]);
        bool character = call.Arguments[0].Type == typeof(char);
        SqlExpression reversed =
            character ? part
            : part is SqlConstant { Value: string value }
                ? Text(string.Concat(value.Reverse()))
            : new SqlFunction("REVERSE", [part], typeof(string));
        SqlExpression tail = new SqlFunction(
            "STRPOS",
            [new SqlFunction("REVERSE", [text], typeof(string)), reversed],
            typeof(int)
        );
        SqlExpression length = new SqlFunction("LENGTH", [text], typeof(int));
        return Found(
            new SqlFunction("STRPOS", [text, part], typeof(int)),
            character
                ? Calculate("-", length, tail)
                : Calculate(
                    "-",
                    Calculate(
                        "+",
                        Calculate(
                            "-",
                            length,
                            new SqlFunction("LENGTH", [part], typeof(int))
                        ),
                        Integer(1)
                    ),
                    tail
                )
        );
    }

    private SqlExpression AnyLocated(MethodCallExpression call)
    {
        if (
            call.Arguments[0]
            is not ConstantExpression { Value: char[] { Length: > 0 } characters }
        )
        {
            throw Unsupported.Method(call.Method);
        }
        SqlExpression text = Sql(call.Object!);
        return new SqlFunction(
            "COALESCE",
            [
                new SqlFunction(
                    "LEAST",
                    [
                        .. characters.Select(character => new SqlFunction(
                            "NULLIF",
                            [
                                Calculate(
                                    "-",
                                    new SqlFunction(
                                        "STRPOS",
                                        [text, new SqlConstant(character, typeof(char))],
                                        typeof(int)
                                    ),
                                    Integer(1)
                                ),
                                Integer(-1),
                            ],
                            typeof(int?)
                        )),
                    ],
                    typeof(int?)
                ),
                Integer(-1),
            ],
            typeof(int)
        );
    }

    private static SqlCase Found(SqlExpression position, SqlExpression index)
    {
        return new SqlCase(
            [new SqlWhen(Compare("=", position, Integer(0)), Integer(-1))],
            index,
            typeof(int)
        );
    }

    private static bool IsOrdinal(IReadOnlyList<Expression> arguments)
    {
        return arguments.Count == 1
            || arguments[1] is ConstantExpression { Value: StringComparison.Ordinal };
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
            nameof(Math.Clamp) => new SqlFunction(
                "LEAST",
                [new SqlFunction("GREATEST", arguments[..2], call.Type), arguments[2]],
                call.Type
            ),
            nameof(Math.BigMul) when call.Arguments[0].Type == typeof(int) => Calculate(
                "*",
                Convert(arguments[0], typeof(int), typeof(long)),
                Convert(arguments[1], typeof(int), typeof(long))
            ),
            nameof(double.IsNaN) => Compare("=", arguments[0], Floating(double.NaN)),
            nameof(double.IsInfinity) => Logical(
                "OR",
                Compare("=", arguments[0], Floating(double.PositiveInfinity)),
                Compare("=", arguments[0], Floating(double.NegativeInfinity))
            ),
            nameof(double.IsFinite) => new SqlIn(
                arguments[0],
                [
                    Floating(double.NaN),
                    Floating(double.PositiveInfinity),
                    Floating(double.NegativeInfinity),
                ],
                true
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
            nameof(Math.Round) => Rounded(call, arguments[0]),
            _ when MathFunctions.TryGetValue(name, out string? function) =>
                new SqlFunction(function, arguments, call.Type),
            _ => throw Unsupported.Method(call.Method),
        };
    }

    private static SqlConstant Floating(double value)
    {
        return new SqlConstant(value, typeof(double));
    }

    private SqlExpression TemporalMethod(MethodCallExpression call)
    {
        string name = call.Method.Name;
        bool day = call.Method.DeclaringType == typeof(DateOnly);
        return name == nameof(DateOnly.FromDateTime)
                ? new SqlCast(Sql(call.Arguments[0]), day ? "date" : "time", call.Type)
            : day && name == nameof(DateOnly.AddDays)
                ? Calculate("+", Sql(call.Object!), Sql(call.Arguments[0]))
            : name == nameof(DateTime.Add)
                ? Calculate("+", Sql(call.Object!), Sql(call.Arguments[0]))
            : name == nameof(DateTime.AddTicks) ? Ticked(call)
            : Intervals.TryGetValue(name, out string? unit)
                ? Dated(
                    Shifted(Sql(call.Object!), Sql(call.Arguments[0]), unit),
                    day,
                    call.Type
                )
            : throw Unsupported.Method(call.Method);
    }

    // A constant count of ticks below a microsecond is seen to be lost; a computed one
    // is truncated to the microsecond.
    private SqlExpression Ticked(MethodCallExpression call)
    {
        return call.Arguments[0] is not ConstantExpression { Value: long ticks }
                ? Calculate("+", Sql(call.Object!), Microseconds(Sql(call.Arguments[0])))
            : ticks % TimeSpan.TicksPerMicrosecond == 0
                ? Shifted(
                    Sql(call.Object!),
                    new SqlConstant(ticks / TimeSpan.TicksPerMicrosecond, typeof(long)),
                    "microsecond"
                )
            : throw Unsupported.Method(call.Method);
    }

    private static SqlBinary Shifted(
        SqlExpression moment,
        SqlExpression amount,
        string unit
    )
    {
        return Calculate("+", moment, Interval(amount, unit));
    }

    private static SqlExpression Dated(SqlExpression moment, bool day, Type type)
    {
        return day ? new SqlCast(moment, "date", type) : moment;
    }

    private SqlBinary Flagged(MethodCallExpression call)
    {
        SqlExpression flag = Sql(call.Arguments[0]);
        return Compare("=", Calculate("&", Sql(call.Object!), flag), flag);
    }

    private static readonly Dictionary<Type, string> Parsable = new()
    {
        [typeof(bool)] = "boolean",
        [typeof(DateOnly)] = "date",
        [typeof(TimeOnly)] = "time",
        [typeof(DateTime)] = "timestamp",
        [typeof(Guid)] = "uuid",
    };

    private static readonly HashSet<Type> CultureFree = [typeof(bool), typeof(Guid)];

    private static readonly HashSet<Type> DateStyled =
    [
        typeof(DateOnly),
        typeof(DateTime),
    ];

    // The invariant short date: a month, a day and a four-digit year.
    private const string MonthFirst = @"^\s*(\d{1,2})[/.-](\d{1,2})[/.-](\d{4})";

    // Parsing is a cast from text in PostgreSQL's input syntax, which the invariant
    // culture reads as well; the current culture may not.
    private SqlExpression Parsed(MethodCallExpression call)
    {
        Type type = call.Type;
        string? target =
            ScalarTypes.IsNumber(type) && !type.IsEnum
                ? ScalarTypes.Number(type)
                : Parsable.GetValueOrDefault(type);
        IEnumerable<Expression> options = call.Arguments.Skip(1);
        bool cultured =
            CultureFree.Contains(type)
            || options.Any(option => option is ConstantExpression { Value: CultureInfo });
        if (target is null || !cultured || !options.All(IsInvariant))
        {
            throw Unsupported.Method(call.Method);
        }
        SqlExpression text = Sql(call.Arguments[0]);
        return new SqlCast(
            DateStyled.Contains(type) ? MonthFirstAsIso(text) : text,
            target,
            type
        );
    }

    // The server reads a numeric date in its DateStyle, day first on a DMY server,
    // where the invariant culture reads the month first; the date is rewritten as
    // ISO 8601, which is read the same whatever the DateStyle.
    private static SqlFunction MonthFirstAsIso(SqlExpression text)
    {
        return new SqlFunction(
            "REGEXP_REPLACE",
            [
                text,
                new SqlConstant(MonthFirst, typeof(string)),
                new SqlConstant(@"\3-\1-\2", typeof(string)),
            ],
            typeof(string),
            text.Nullable
        );
    }

    // A style is a constant set of flags; a culture has to be the invariant one.
    private static bool IsInvariant(Expression option)
    {
        return option is ConstantExpression { Value: object value }
            && (
                value is NumberStyles or DateTimeStyles
                || value.Equals(CultureInfo.InvariantCulture)
            );
    }

    private SqlExpression Membership(MethodCallExpression call)
    {
        (Expression source, Expression item) = call.Object is null
            ? (Unspanned(call.Arguments[0]), call.Arguments[1])
            : (call.Object, call.Arguments[0]);
        SqlExpression value = Sql(item);
        return Membership(Sql(source), value);
    }

    private static SqlExpression Membership(SqlExpression source, SqlExpression value)
    {
        if (source is not SqlConstant { Value: IEnumerable values })
        {
            SqlExpression any = new SqlAny(value, source);
            // A null item is contained when the array holds a NULL, which = ANY misses.
            return value.Nullable
                ? Logical(
                    "OR",
                    any,
                    Logical(
                        "AND",
                        new SqlIsNull(value, false),
                        new SqlIsNull(
                            new SqlFunction(
                                "ARRAY_POSITION",
                                [source, new SqlConstant(null, value.Type)],
                                typeof(int?)
                            ),
                            true
                        )
                    )
                )
                : any;
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
                : new SqlIn(value, present, false);
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
