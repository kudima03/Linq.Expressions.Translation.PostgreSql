namespace Linq.Expressions.Translation.PostgreSql.Syntax;

// The distinct values of a group in the order of their first occurrences, which
// DISTINCT in an aggregate cannot give, as it sorts by the value: the values of the
// group aggregated into an array in their order, unnested with their positions and
// grouped again, each value at its least position. As an array, or the last of them.
internal sealed class SqlOccurrences(
    SqlExpression value,
    IReadOnlyList<Ordering> order,
    SqlExpression? filter,
    bool last,
    Type type
) : SqlExpression(type)
{
    public override IEnumerable<SqlExpression> Operands =>
        [
            value,
            .. order.Select(ordering => ordering.Key),
            .. new[] { filter }.OfType<SqlExpression>(),
        ];

    public override bool IsAggregate => true;

    public override bool Nullable => last;

    public override void Render(SqlWriter writer, SqlMode mode)
    {
        _ = writer
            .Append(last ? "(SELECT u.v FROM UNNEST(" : "ARRAY(SELECT u.v FROM UNNEST(")
            .Append("ARRAY_AGG(")
            .Render(value, SqlMode.Value)
            .Append(" ORDER BY ")
            .Join(order, ordering => ordering.Render(writer))
            .Append(")");
        if (filter is not null)
        {
            _ = writer
                .Append(" FILTER (WHERE ")
                .Render(filter, SqlMode.Predicate)
                .Append(")");
        }
        _ = writer
            .Append(") WITH ORDINALITY AS u (v, i) GROUP BY u.v ORDER BY MIN(u.i)")
            .Append(last ? " DESC LIMIT 1)" : ")");
    }
}
