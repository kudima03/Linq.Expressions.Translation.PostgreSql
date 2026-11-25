namespace Linq.Expressions.Translation.PostgreSql.Syntax;

// The rows of the level before, which the step of WITH RECURSIVE reads by the name
// of the recursive query.
internal sealed class LevelSource(string name, string alias) : SqlSource(alias)
{
    public override void Render(SqlWriter writer)
    {
        _ = writer.Append(name + " AS " + Alias);
    }
}
