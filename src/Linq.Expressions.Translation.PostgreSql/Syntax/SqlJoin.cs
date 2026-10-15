namespace Linq.Expressions.Translation.PostgreSql.Syntax;

internal sealed class SqlJoin(string kind, SqlSource source, SqlExpression? condition)
{
    public void Render(SqlWriter writer)
    {
        _ = writer.Append(" " + kind + " ");
        source.Render(writer);
        if (condition is not null)
        {
            _ = writer.Append(" ON ").Render(condition, SqlMode.Predicate);
        }
    }
}
