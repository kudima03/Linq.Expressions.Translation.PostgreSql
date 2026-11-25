namespace Linq.Expressions.Translation.PostgreSql.Syntax;

internal sealed class SqlJoin(string kind, SqlSource source, SqlExpression? condition)
{
    public string Kind { get; } = kind;

    public SqlSource Source { get; } = source;

    public void Render(SqlWriter writer)
    {
        _ = writer.Append(" " + Kind + " ");
        Source.Render(writer);
        if (condition is not null)
        {
            _ = writer.Append(" ON ").Render(condition, SqlMode.Predicate);
        }
    }
}
