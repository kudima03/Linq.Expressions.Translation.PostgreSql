namespace Linq.Expressions.Translation.PostgreSql.Syntax;

// The window of a window function: OVER (PARTITION BY ... ORDER BY ... frame).
internal sealed class SqlWindow(
    IReadOnlyList<SqlExpression> partition,
    IReadOnlyList<Ordering> order,
    string? frame
)
{
    public void Render(SqlWriter writer)
    {
        _ = writer.Append(" OVER (");
        string separator = string.Empty;
        if (partition.Count > 0)
        {
            _ = writer
                .Append("PARTITION BY ")
                .Join(partition, key => writer.Render(key, SqlMode.Value));
            separator = " ";
        }
        if (order.Count > 0)
        {
            _ = writer
                .Append(separator + "ORDER BY ")
                .Join(order, ordering => ordering.Render(writer));
            separator = " ";
        }
        if (frame is not null)
        {
            _ = writer.Append(separator + frame);
        }
        _ = writer.Append(")");
    }
}
