namespace Linq.Expressions.Translation.PostgreSql.Syntax;

// The value of the first row of a group in an order: the first element of the array
// of the values aggregated in that order.
internal sealed class SqlFirst(SqlExpression value, IReadOnlyList<Ordering> order)
    : SqlExpression(value.Type)
{
    public override bool Nullable => value.Nullable;

    public override void Render(SqlWriter writer, SqlMode mode)
    {
        _ = writer
            .Append("(ARRAY_AGG(")
            .Render(value, SqlMode.Value)
            .Append(" ORDER BY ")
            .Join(order, ordering => ordering.Render(writer))
            .Append("))[1]");
    }
}
