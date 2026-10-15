namespace Linq.Expressions.Translation.PostgreSql.Syntax;

internal abstract class SqlSource(string alias)
{
    public string Alias { get; } = alias;

    public abstract void Render(SqlWriter writer);
}
