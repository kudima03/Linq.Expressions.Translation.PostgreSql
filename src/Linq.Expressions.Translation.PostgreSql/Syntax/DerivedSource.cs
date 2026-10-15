namespace Linq.Expressions.Translation.PostgreSql.Syntax;

internal sealed class DerivedSource(SqlQuery query, string alias) : SqlSource(alias)
{
    public override void Render(SqlWriter writer)
    {
        _ = writer.Append("(");
        query.Render(writer);
        _ = writer.Append(") AS " + Alias);
    }
}
