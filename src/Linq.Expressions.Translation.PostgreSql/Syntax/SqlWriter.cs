using System.Text;

namespace Linq.Expressions.Translation.PostgreSql.Syntax;

internal sealed class SqlWriter
{
    private readonly StringBuilder _text = new();

    public static string Text(SqlExpression expression)
    {
        SqlWriter writer = new();
        expression.Render(writer, SqlMode.Value);
        return writer.ToString();
    }

    public SqlWriter Append(string text)
    {
        _ = _text.Append(text);
        return this;
    }

    // PostgreSQL keeps the first 63 bytes of an identifier, so two column names that
    // share them are one name to it, an ambiguous one.
    public static void Distinguish(IEnumerable<string> names)
    {
        Dictionary<string, string> kept = [];
        foreach (string name in names.Distinct())
        {
            string prefix = Kept(name);
            if (!kept.TryAdd(prefix, name))
            {
                throw new NotSupportedException(
                    $"The columns '{kept[prefix]}' and '{name}' share their first {IdentifierLength} bytes, which is all PostgreSQL keeps of a name: project fewer nested members."
                );
            }
        }
    }

    private const int IdentifierLength = 63;

    private static string Kept(string name)
    {
        int bytes = 0;
        int length = 0;
        foreach (Rune rune in name.EnumerateRunes())
        {
            bytes += rune.Utf8SequenceLength;
            if (bytes > IdentifierLength)
            {
                break;
            }
            length += rune.Utf16SequenceLength;
        }
        return name[..length];
    }

    public SqlWriter Identifier(string name)
    {
        return Append("\"" + name.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"");
    }

    public SqlWriter Render(SqlExpression expression, SqlMode mode)
    {
        expression.Render(this, mode);
        return this;
    }

    // A constant of a fractional type is written so that PostgreSQL takes it for
    // one: a whole number with its point, a float in a cast. Among operands it
    // resolves to one type (an operator, a function, CASE, IN), a numeric or double
    // precision one beside it types it, as does an interval it multiplies, and it
    // is written as its digits alone.
    public static bool IsNumeric(IEnumerable<SqlExpression> operands)
    {
        return operands.Any(operand =>
            operand is not SqlConstant
            && (Nullable.GetUnderlyingType(operand.Type) ?? operand.Type) is Type type
            && (
                type == typeof(decimal)
                || type == typeof(double)
                || type == typeof(TimeSpan)
            )
        );
    }

    public SqlWriter Unified(SqlExpression operand, bool numeric)
    {
        return
            numeric
            && operand is SqlConstant constant
            && SqlLiteral.Digits(constant.Value) is string digits
            ? Append(digits)
            : Render(operand, SqlMode.Value);
    }

    // The operand of a cast is typed by it: its digits, or the text of a number
    // that has none.
    public SqlWriter Cast(SqlExpression operand)
    {
        return
            operand is SqlConstant { Value: double or float } constant
            && SqlLiteral.Digits(constant.Value) is null
            ? Append(SqlLiteral.Quoted(System.Convert.ToDouble(constant.Value, null)))
            : Unified(operand, true);
    }

    public SqlWriter Operand(SqlExpression operand, SqlMode mode)
    {
        return operand.IsCompound(mode)
            ? Append("(").Render(operand, mode).Append(")")
            : Render(operand, mode);
    }

    public SqlWriter Join<T>(IEnumerable<T> items, Action<T> render)
    {
        string separator = string.Empty;
        foreach (T item in items)
        {
            _ = Append(separator);
            render(item);
            separator = ", ";
        }
        return this;
    }

    public override string ToString()
    {
        return _text.ToString();
    }
}
