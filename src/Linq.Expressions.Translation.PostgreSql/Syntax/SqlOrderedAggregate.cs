namespace Linq.Expressions.Translation.PostgreSql.Syntax;

// An aggregate over its rows in an order, STRING_AGG(value, separator ORDER BY ...),
// of the rows a filter keeps. It is NULL where it keeps none, so it is only read
// through COALESCE.
internal sealed class SqlOrderedAggregate(
    string name,
    IReadOnlyList<SqlExpression> arguments,
    bool distinct,
    IReadOnlyList<Ordering> order,
    SqlExpression? filter,
    Type type
) : SqlExpression(type)
{
    public override IEnumerable<SqlExpression> Operands =>
        [
            .. arguments,
            .. order.Select(ordering => ordering.Key),
            .. new[] { filter }.OfType<SqlExpression>(),
        ];

    public override bool IsAggregate => true;

    public override void Render(SqlWriter writer, SqlMode mode)
    {
        _ = writer
            .Append(name + "(" + (distinct ? "DISTINCT " : string.Empty))
            .Join(arguments, argument => writer.Render(argument, SqlMode.Value));
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
    }
}
