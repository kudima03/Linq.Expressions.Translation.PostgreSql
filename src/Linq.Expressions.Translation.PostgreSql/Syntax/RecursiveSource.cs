namespace Linq.Expressions.Translation.PostgreSql.Syntax;

// WITH RECURSIVE as a derived table, so that it composes like any source:
// (WITH RECURSIVE name AS (seed UNION ALL step) SELECT columns FROM name) AS alias.
// With a depth limit the step fails on a row deeper than it, and the rows are read
// only once every level has been computed (a one-time filter that counts them), so
// that a query reading some of them fails as one reading all of them does.
internal sealed class RecursiveSource(
    string name,
    SelectQuery seed,
    SelectQuery step,
    IReadOnlyList<string> columns,
    bool limited,
    string alias
) : SqlSource(alias)
{
    public override void Render(SqlWriter writer)
    {
        _ = writer.Append("(WITH RECURSIVE " + name + " AS (");
        Term(writer, seed);
        _ = writer.Append(" UNION ALL ");
        Term(writer, step);
        _ = writer
            .Append(") SELECT ")
            .Join(columns, column => writer.Append(name + ".").Identifier(column))
            .Append(" FROM " + name);
        if (limited)
        {
            _ = writer.Append(" WHERE (SELECT COUNT(*) FROM " + name + ") > 0");
        }
        _ = writer.Append(") AS " + Alias);
    }

    private static void Term(SqlWriter writer, SelectQuery query)
    {
        bool parenthesized = query.KeepsOrder;
        _ = writer.Append(parenthesized ? "(" : string.Empty);
        query.Render(writer);
        _ = writer.Append(parenthesized ? ")" : string.Empty);
    }
}
