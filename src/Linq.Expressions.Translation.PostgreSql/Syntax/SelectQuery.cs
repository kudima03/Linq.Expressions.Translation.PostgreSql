using Linq.Expressions.Translation.PostgreSql.Shapes;

namespace Linq.Expressions.Translation.PostgreSql.Syntax;

internal sealed class SelectQuery(SqlSource? from, Shape shape) : SqlQuery(shape)
{
    public SqlSource? From { get; } = from;

    public List<SqlJoin> Joins { get; } = [];

    // Ordering keys a derived table has to expose for the outer query to keep its order.
    public List<ProjectionColumn> Hidden { get; } = [];

    public SqlExpression? Where { get; set; }

    public IReadOnlyList<SqlExpression>? GroupBy { get; set; }

    public SqlExpression? Having { get; set; }

    public List<Ordering> Orderings { get; set; } = [];

    public long? Limit { get; set; }

    public long? Offset { get; set; }

    public SqlExpression? Bound { get; set; }

    public bool Distinct { get; set; }

    // The keys of DISTINCT ON, which keeps the first row of each key in ORDER BY order.
    public IReadOnlyList<SqlExpression>? DistinctOn { get; set; }

    // Whether ORDER BY orders the result, rather than only choosing the rows DISTINCT ON
    // keeps, as for UnionBy, whose result has no order.
    public bool IsOrdered { get; set; } = true;

    public bool IsPaged => Limit is not null || Offset is not null || Bound is not null;

    // The order is part of the result: it decides which rows are paged or kept per key.
    public bool KeepsOrder => IsPaged || DistinctOn is not null;

    public override void Render(SqlWriter writer)
    {
        _ = writer.Append(Distinct ? "SELECT DISTINCT " : "SELECT ");
        if (DistinctOn is not null)
        {
            _ = writer
                .Append("DISTINCT ON (")
                .Join(DistinctOn, key => writer.Render(key, SqlMode.Value))
                .Append(") ");
        }
        _ = writer.Join([.. Shape.Columns(), .. Hidden], column => column.Render(writer));
        if (From is not null)
        {
            _ = writer.Append(" FROM ");
            From.Render(writer);
        }
        foreach (SqlJoin join in Joins)
        {
            join.Render(writer);
        }
        if (Where is not null)
        {
            _ = writer.Append(" WHERE ").Render(Where, SqlMode.Predicate);
        }
        if (GroupBy is not null)
        {
            _ = writer
                .Append(" GROUP BY ")
                .Join(GroupBy, key => writer.Render(key, SqlMode.Value));
        }
        if (Having is not null)
        {
            _ = writer.Append(" HAVING ").Render(Having, SqlMode.Predicate);
        }
        if (Orderings.Count > 0)
        {
            _ = writer
                .Append(" ORDER BY ")
                .Join(Orderings, ordering => ordering.Render(writer));
        }
        if (Limit is not null)
        {
            _ = writer.Append(" LIMIT " + Limit);
        }
        if (Bound is not null)
        {
            _ = writer.Append(" LIMIT ").Render(Bound, SqlMode.Value);
        }
        if (Offset is not null)
        {
            _ = writer.Append(" OFFSET " + Offset);
        }
    }
}
