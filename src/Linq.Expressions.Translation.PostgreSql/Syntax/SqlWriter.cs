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

    public SqlWriter Identifier(string name)
    {
        return Append("\"" + name.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"");
    }

    public SqlWriter Render(SqlExpression expression, SqlMode mode)
    {
        expression.Render(this, mode);
        return this;
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
