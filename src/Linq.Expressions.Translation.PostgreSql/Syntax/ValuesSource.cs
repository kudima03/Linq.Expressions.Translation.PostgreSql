namespace Linq.Expressions.Translation.PostgreSql.Syntax;

// The rows of an in-memory sequence as a VALUES list, its columns named after the
// alias as a derived table's are. A sequence has an order, which the rows of VALUES
// keep only while they are read as they are: once something sorts or groups them,
// their positions are a column of their own, written when the statement reads it.
internal sealed class ValuesSource(
    IReadOnlyList<IReadOnlyList<SqlExpression>> rows,
    IReadOnlyList<string> names,
    string alias
) : SqlSource(alias)
{
    // The column of the positions; a member never starts with $.
    public const string Position = "$position";

    // Whether the statement reads the positions of the rows.
    public bool Positioned { get; set; }

    public override void Render(SqlWriter writer)
    {
        int position = 0;
        _ = writer
            .Append("(VALUES ")
            .Join(
                rows,
                row =>
                    writer
                        .Append("(")
                        .Join(row, cell => writer.Unified(cell, true))
                        .Append(Positioned ? ", " + position++ + ")" : ")")
            )
            .Append(") AS " + Alias + " (")
            .Join(
                Positioned ? [.. names, Position] : names,
                name => writer.Identifier(name)
            )
            .Append(")");
    }
}
