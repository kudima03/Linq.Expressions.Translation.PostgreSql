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
