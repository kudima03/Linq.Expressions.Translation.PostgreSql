using Linq.Expressions.Translation.PostgreSql.Mapping;

namespace Linq.Expressions.Translation.PostgreSql.Syntax;

internal sealed class TableSource(TableMapping table, string alias) : SqlSource(alias)
{
    public override void Render(SqlWriter writer)
    {
        if (table.Schema is not null)
        {
            _ = writer.Identifier(table.Schema).Append(".");
        }
        _ = writer.Identifier(table.Name).Append(" AS " + Alias);
    }
}
