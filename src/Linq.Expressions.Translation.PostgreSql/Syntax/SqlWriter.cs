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

    // A whole decimal constant is written with its point, as PostgreSQL takes a
    // number without one for an integer. Among operands it resolves to one type (an
    // operator, COALESCE, CASE, IN, VALUES, a cast), a numeric one beside it types
    // it, and it is written as its digits alone.
    public static bool IsNumeric(IEnumerable<SqlExpression> operands)
    {
        return operands.Any(operand =>
            operand is not SqlConstant
            && (Nullable.GetUnderlyingType(operand.Type) ?? operand.Type)
                == typeof(decimal)
        );
    }

    public SqlWriter Unified(SqlExpression operand, bool numeric)
    {
        return numeric && operand is SqlConstant { Value: decimal number }
            ? Append(SqlLiteral.Number(number))
            : Render(operand, SqlMode.Value);
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
