namespace Linq.Expressions.Translation.PostgreSql.Syntax;

// The rows of an in-memory sequence as a VALUES list, its columns named after the
// alias as a derived table's are.
internal sealed class ValuesSource(
    IReadOnlyList<IReadOnlyList<SqlExpression>> rows,
    IReadOnlyList<string> names,
    string alias
) : SqlSource(alias)
{
    public override void Render(SqlWriter writer)
    {
        _ = writer
            .Append("(VALUES ")
            .Join(
                rows,
                row =>
                    writer
                        .Append("(")
                        .Join(row, cell => writer.Unified(cell, true))
                        .Append(")")
            )
            .Append(") AS " + Alias + " (")
            .Join(names, name => writer.Identifier(name))
            .Append(")");
    }
}
