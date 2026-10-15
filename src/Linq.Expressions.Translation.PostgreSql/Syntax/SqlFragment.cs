namespace Linq.Expressions.Translation.PostgreSql.Syntax;

internal sealed class SqlFragment(string text, Type type) : SqlExpression(type)
{
    public override void Render(SqlWriter writer, SqlMode mode)
    {
        _ = writer.Append(text);
    }
}
