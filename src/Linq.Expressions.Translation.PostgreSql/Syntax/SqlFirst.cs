namespace Linq.Expressions.Translation.PostgreSql.Syntax;

// The value of the first row of a group in an order: the first element of the array
// of the values aggregated in that order, of the rows a filter keeps. Without an
// order, it is any of them, as the first row of a query without ORDER BY is.
internal sealed class SqlFirst(
    SqlExpression value,
    IReadOnlyList<Ordering> order,
    SqlExpression? filter = null
) : SqlExpression(value.Type)
{
    public override IEnumerable<SqlExpression> Operands =>
        [
            value,
            .. order.Select(ordering => ordering.Key),
            .. new[] { filter }.OfType<SqlExpression>(),
        ];

    public override bool IsAggregate => true;

    public override bool Nullable => value.Nullable || filter is not null;

    public override SqlKind TimeKind => value.TimeKind;

    public override void Render(SqlWriter writer, SqlMode mode)
    {
        _ = writer.Append("(ARRAY_AGG(").Render(value, SqlMode.Value);
        if (order.Count > 0)
        {
            _ = writer
                .Append(" ORDER BY ")
                .Join(order, ordering => ordering.Render(writer));
        }
        _ = writer.Append(")");
        if (filter is not null)
        {
            _ = writer
                .Append(" FILTER (WHERE ")
                .Render(filter, SqlMode.Predicate)
                .Append(")");
        }
        _ = writer.Append(")[1]");
    }
}
