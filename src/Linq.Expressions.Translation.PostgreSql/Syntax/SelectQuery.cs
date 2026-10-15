using Linq.Expressions.Translation.PostgreSql.Shapes;

namespace Linq.Expressions.Translation.PostgreSql.Syntax;

internal sealed class SelectQuery(SqlSource? from, Shape shape) : SqlQuery(shape)
{
    public SqlSource? From { get; } = from;

    public List<SqlJoin> Joins { get; } = [];

    public SqlExpression? Where { get; set; }

    public List<Ordering> Orderings { get; set; } = [];

    public long? Limit { get; set; }

    public long? Offset { get; set; }

    public bool Distinct { get; set; }

    public bool IsPaged => Limit is not null || Offset is not null;

    public override void Render(SqlWriter writer)
    {
        _ = writer
            .Append(Distinct ? "SELECT DISTINCT " : "SELECT ")
            .Join(Shape.Columns(), column => column.Render(writer));
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
        if (Offset is not null)
        {
            _ = writer.Append(" OFFSET " + Offset);
        }
    }
}
