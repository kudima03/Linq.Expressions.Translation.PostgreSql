namespace Linq.Expressions.Translation.PostgreSql.Tests.Markers;

// The aggregates of LINQ over PostgreSqlWindow.Rows, the rows of the frame, are
// window aggregates with .NET's semantics, as over a group: Sum is 0 and Any false
// over no rows, Count is an int, Where is FILTER (WHERE ...) and Select the value
// aggregated.
public sealed record FrameAggregateTests
{
    public static TheoryData<
        Expression<Func<Order, PostgreSqlWindow<Order>, object?>>,
        string
    > Aggregates =>
        new()
        {
            {
                (o, w) => w.Rows.Count(),
                "CAST(COUNT(*) OVER (ORDER BY t0.\"Id\") AS integer)"
            },
            { (o, w) => w.Rows.LongCount(), "COUNT(*) OVER (ORDER BY t0.\"Id\")" },
            {
                (o, w) => w.Rows.Count(x => x.IsPaid),
                "CAST(COUNT(*) FILTER (WHERE t0.\"IsPaid\") OVER (ORDER BY t0.\"Id\") AS integer)"
            },
            {
                (o, w) => w.Rows.LongCount(x => x.Priority > 1),
                "COUNT(*) FILTER (WHERE t0.\"Priority\" > 1) OVER (ORDER BY t0.\"Id\")"
            },
            {
                (o, w) => w.Rows.Sum(x => x.Total),
                "COALESCE(SUM(t0.\"Total\") OVER (ORDER BY t0.\"Id\"), 0)"
            },
            {
                (o, w) => w.Rows.Sum(x => x.Quantity),
                "CAST(COALESCE(SUM(t0.\"Quantity\") OVER (ORDER BY t0.\"Id\"), 0) AS integer)"
            },
            {
                (o, w) => w.Rows.Sum(x => x.Points),
                "CAST(COALESCE(SUM(t0.\"Points\") OVER (ORDER BY t0.\"Id\"), 0) AS bigint)"
            },
            {
                (o, w) => w.Rows.Sum(x => x.Discount),
                "COALESCE(SUM(t0.\"Discount\") OVER (ORDER BY t0.\"Id\"), 0)"
            },
            {
                (o, w) => w.Rows.Sum(x => x.Weight),
                "COALESCE(SUM(t0.\"Weight\") OVER (ORDER BY t0.\"Id\"), 0)"
            },
            {
                (o, w) => w.Rows.Average(x => x.Total),
                "AVG(t0.\"Total\") OVER (ORDER BY t0.\"Id\")"
            },
            {
                (o, w) => w.Rows.Average(x => x.Quantity),
                "AVG(CAST(t0.\"Quantity\" AS double precision)) OVER (ORDER BY t0.\"Id\")"
            },
            {
                (o, w) => w.Rows.Min(x => x.CreatedAt),
                "MIN(t0.\"CreatedAt\") OVER (ORDER BY t0.\"Id\")"
            },
            {
                (o, w) => w.Rows.Max(x => x.Note),
                "MAX(t0.\"Note\") OVER (ORDER BY t0.\"Id\")"
            },
            {
                (o, w) => w.Rows.Max(x => x.IsPaid),
                "BOOL_OR(t0.\"IsPaid\") OVER (ORDER BY t0.\"Id\")"
            },
            {
                (o, w) => w.Rows.Min(x => x.IsPaid),
                "BOOL_AND(t0.\"IsPaid\") OVER (ORDER BY t0.\"Id\")"
            },
            {
                (o, w) => w.Rows.Any(),
                "COALESCE(BOOL_OR(TRUE) OVER (ORDER BY t0.\"Id\"), FALSE)"
            },
            {
                (o, w) => w.Rows.Any(x => x.IsGift == true),
                "COALESCE(BOOL_OR(t0.\"IsGift\" IS NOT DISTINCT FROM TRUE) OVER (ORDER BY t0.\"Id\"), FALSE)"
            },
            {
                (o, w) => w.Rows.All(x => x.IsPaid),
                "COALESCE(BOOL_AND(t0.\"IsPaid\") OVER (ORDER BY t0.\"Id\"), TRUE)"
            },
            {
                (o, w) => w.Rows.Where(x => x.IsPaid).Sum(x => x.Total),
                "COALESCE(SUM(t0.\"Total\") FILTER (WHERE t0.\"IsPaid\") OVER (ORDER BY t0.\"Id\"), 0)"
            },
            {
                (o, w) => w.Rows.Where(x => x.IsPaid).Count(x => x.Quantity > 1),
                "CAST(COUNT(*) FILTER (WHERE t0.\"IsPaid\" AND (t0.\"Quantity\" > 1)) OVER (ORDER BY t0.\"Id\") AS integer)"
            },
            {
                (o, w) =>
                    w
                        .Rows.Where(x => x.IsPaid)
                        .Where(x => x.Quantity > 1)
                        .Sum(x => x.Points),
                "CAST(COALESCE(SUM(t0.\"Points\") FILTER (WHERE t0.\"IsPaid\" AND (t0.\"Quantity\" > 1)) OVER (ORDER BY t0.\"Id\"), 0) AS bigint)"
            },
            {
                (o, w) => w.Rows.Where(x => x.IsPaid).Select(x => x.Note).Any(),
                "COALESCE(BOOL_OR(TRUE) FILTER (WHERE t0.\"IsPaid\") OVER (ORDER BY t0.\"Id\"), FALSE)"
            },
            {
                (o, w) => w.Rows.Where(x => x.Note != null).All(x => x.IsPaid),
                "COALESCE(BOOL_AND(t0.\"IsPaid\") FILTER (WHERE t0.\"Note\" IS NOT NULL) OVER (ORDER BY t0.\"Id\"), TRUE)"
            },
            {
                (o, w) => w.Rows.Max(x => x.Total * x.Quantity),
                "MAX(t0.\"Total\" * CAST(t0.\"Quantity\" AS numeric)) OVER (ORDER BY t0.\"Id\")"
            },
            {
                (o, w) => w.Rows.Select(x => x.Quantity).Where(q => q > 1).Sum(),
                "CAST(COALESCE(SUM(t0.\"Quantity\") FILTER (WHERE t0.\"Quantity\" > 1) OVER (ORDER BY t0.\"Id\"), 0) AS integer)"
            },
            {
                (o, w) =>
                    w.Rows.Select(x => new { x.Total, x.Quantity }).Sum(x => x.Total),
                "COALESCE(SUM(t0.\"Total\") OVER (ORDER BY t0.\"Id\"), 0)"
            },
            {
                (o, w) =>
                    w.Rows.Sum(x => Orders.Count(y => y.CustomerId == x.CustomerId)),
                "CAST(COALESCE(SUM((SELECT CAST(COUNT(*) AS integer) FROM \"Order\" AS t1 WHERE t1.\"CustomerId\" = t0.\"CustomerId\")) OVER (ORDER BY t0.\"Id\"), 0) AS integer)"
            },
        };

    [Theory]
    [MemberData(nameof(Aggregates))]
    public void AggregatesTheRowsOfTheFrame(
        Expression<Func<Order, PostgreSqlWindow<Order>, object?>> selector,
        string aggregate
    )
    {
        Expression query = Trees
            .Over(Orders, null, rows => rows.OrderBy(o => o.Id), null, selector)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                $"""
                SELECT {aggregate}
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
