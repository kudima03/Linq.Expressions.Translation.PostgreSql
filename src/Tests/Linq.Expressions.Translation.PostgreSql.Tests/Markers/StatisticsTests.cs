namespace Linq.Expressions.Translation.PostgreSql.Tests.Markers;

// The statistical aggregates of PostgreSqlFunctions are PostgreSQL's aggregates of
// the same name over a group, the frame of a window or a query, filtered by the
// Where of their rows.
public sealed record StatisticsTests
{
    private static readonly int[] One = [0];

    public static TheoryData<
        Expression<Func<IGrouping<int, Order>, object?>>,
        string
    > Functions =>
        new()
        {
            {
                g => PostgreSqlFunctions.StddevPop(g, o => o.Weight),
                "STDDEV_POP(t0.\"Weight\")"
            },
            {
                g => PostgreSqlFunctions.StddevPop(g, o => o.Discount),
                "STDDEV_POP(t0.\"Discount\")"
            },
            {
                g => PostgreSqlFunctions.StddevSamp(g, o => o.Weight),
                "STDDEV_SAMP(t0.\"Weight\")"
            },
            {
                g => PostgreSqlFunctions.StddevSamp(g, o => o.Total),
                "STDDEV_SAMP(t0.\"Total\")"
            },
            {
                g => PostgreSqlFunctions.VarPop(g, o => (double?)o.Quantity),
                "VAR_POP(CAST(t0.\"Quantity\" AS double precision))"
            },
            {
                g => PostgreSqlFunctions.VarPop(g, o => (decimal?)o.Quantity),
                "VAR_POP(CAST(t0.\"Quantity\" AS numeric))"
            },
            {
                g => PostgreSqlFunctions.VarSamp(g, o => o.Weight),
                "VAR_SAMP(t0.\"Weight\")"
            },
            {
                g => PostgreSqlFunctions.VarSamp(g, o => o.Total),
                "VAR_SAMP(t0.\"Total\")"
            },
            {
                g => PostgreSqlFunctions.Corr(g, o => o.Weight, o => o.Points),
                "CORR(t0.\"Weight\", CAST(t0.\"Points\" AS double precision))"
            },
            {
                g => PostgreSqlFunctions.CovarPop(g, o => o.Weight, o => o.Weight),
                "COVAR_POP(t0.\"Weight\", t0.\"Weight\")"
            },
            {
                g => PostgreSqlFunctions.CovarSamp(g, o => o.Weight, o => o.Weight),
                "COVAR_SAMP(t0.\"Weight\", t0.\"Weight\")"
            },
            {
                g => PostgreSqlFunctions.RegrAvgX(g, o => o.Weight, o => o.Weight),
                "REGR_AVGX(t0.\"Weight\", t0.\"Weight\")"
            },
            {
                g => PostgreSqlFunctions.RegrAvgY(g, o => o.Weight, o => o.Weight),
                "REGR_AVGY(t0.\"Weight\", t0.\"Weight\")"
            },
            {
                g => PostgreSqlFunctions.RegrCount(g, o => o.Weight, o => o.Priority),
                "REGR_COUNT(t0.\"Weight\", CAST(t0.\"Priority\" AS double precision))"
            },
            {
                g => PostgreSqlFunctions.RegrIntercept(g, o => o.Weight, o => o.Weight),
                "REGR_INTERCEPT(t0.\"Weight\", t0.\"Weight\")"
            },
            {
                g => PostgreSqlFunctions.RegrR2(g, o => o.Weight, o => o.Weight),
                "REGR_R2(t0.\"Weight\", t0.\"Weight\")"
            },
            {
                g => PostgreSqlFunctions.RegrSlope(g, o => o.Weight, o => o.Weight),
                "REGR_SLOPE(t0.\"Weight\", t0.\"Weight\")"
            },
            {
                g => PostgreSqlFunctions.RegrSxx(g, o => o.Weight, o => o.Weight),
                "REGR_SXX(t0.\"Weight\", t0.\"Weight\")"
            },
            {
                g => PostgreSqlFunctions.RegrSxy(g, o => o.Weight, o => o.Weight),
                "REGR_SXY(t0.\"Weight\", t0.\"Weight\")"
            },
            {
                g => PostgreSqlFunctions.RegrSyy(g, o => o.Weight, o => o.Weight),
                "REGR_SYY(t0.\"Weight\", t0.\"Weight\")"
            },
            {
                g => PostgreSqlFunctions.VarPop(g.Where(o => o.IsPaid), o => o.Weight),
                "VAR_POP(t0.\"Weight\") FILTER (WHERE t0.\"IsPaid\")"
            },
            {
                g =>
                    PostgreSqlFunctions.Corr(
                        g.Where(o => o.IsPaid).Where(o => o.Quantity > 0),
                        o => o.Weight,
                        o => o.Weight
                    ),
                "CORR(t0.\"Weight\", t0.\"Weight\") FILTER (WHERE (t0.\"Quantity\" > 0) AND t0.\"IsPaid\")"
            },
            {
                g =>
                    PostgreSqlFunctions.VarPop(
                        g.Select(o => new { o.Weight }),
                        x => x.Weight
                    ),
                "VAR_POP(t0.\"Weight\")"
            },
        };

    [Theory]
    [MemberData(nameof(Functions))]
    public void AggregatesTheRowsOfAGroup(
        Expression<Func<IGrouping<int, Order>, object?>> selector,
        string function
    )
    {
        Expression query = Orders.GroupBy(o => o.CustomerId).Select(selector).Expression;

        Assert.Equal(
            Sql.Valid(
                $"""
                SELECT {function}
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersGroupsByAStatistic()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Where(g => PostgreSqlFunctions.StddevPop(g, o => o.Weight) > 1)
            .Select(g => g.Key)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                HAVING STDDEV_POP(t0."Weight") > 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AggregatesTheRowsOfAFrame()
    {
        Expression query = Trees
            .Over(
                Orders,
                o => o.CustomerId,
                rows => rows.OrderBy(o => o.Id),
                PostgreSqlFrame.Rows(-4, 0),
                (o, w) =>
                    new
                    {
                        o.Id,
                        Deviation = PostgreSqlFunctions.StddevSamp(
                            w.Rows,
                            x => (decimal?)x.Total
                        ),
                        Slope = PostgreSqlFunctions.RegrSlope(
                            w.Rows.Where(x => x.IsPaid),
                            x => x.Weight,
                            x => (double?)x.Id
                        ),
                    }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", STDDEV_SAMP(t0."Total") OVER (PARTITION BY t0."CustomerId" ORDER BY t0."Id" ROWS BETWEEN 4 PRECEDING AND CURRENT ROW) AS "Deviation", REGR_SLOPE(t0."Weight", CAST(t0."Id" AS double precision)) FILTER (WHERE t0."IsPaid") OVER (PARTITION BY t0."CustomerId" ORDER BY t0."Id" ROWS BETWEEN 4 PRECEDING AND CURRENT ROW) AS "Slope"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AggregatesTheRowsOfAQuery()
    {
        Expression total = One.AsQueryable()
            .Select(_ => new
            {
                Variance = PostgreSqlFunctions.VarPop(Orders, o => (decimal?)o.Total),
                Paid = PostgreSqlFunctions.Corr(
                    Orders.Where(o => o.IsPaid),
                    o => o.Weight,
                    o => (double?)o.Quantity
                ),
            })
            .Expression;
        Expression correlated = Customers
            .Select(c => new
            {
                c.Id,
                Deviation = PostgreSqlFunctions.StddevPop(
                    Orders.Where(o => o.CustomerId == c.Id),
                    o => (double?)o.Quantity
                ),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT (SELECT VAR_POP(t0."Total") FROM "Order" AS t0) AS "Variance", (
                    SELECT CORR(t1."Weight", CAST(t1."Quantity" AS double precision))
                    FROM "Order" AS t1
                    WHERE t1."IsPaid"
                ) AS "Paid"
                """
            ),
            new PostgreSqlQuery(total).TextValue
        );
        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", (
                    SELECT STDDEV_POP(CAST(t1."Quantity" AS double precision))
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                ) AS "Deviation"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(correlated).TextValue
        );
    }

    [Fact]
    public void AggregatesThePagedRowsOfAQueryInADerivedTable()
    {
        Expression query = Customers
            .Select(c =>
                PostgreSqlFunctions.VarSamp(
                    Orders.OrderBy(o => o.Total).Take(5),
                    o => o.Weight
                )
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT (
                    SELECT VAR_SAMP(t2."Weight")
                    FROM (
                        SELECT t1."Id", t1."CustomerId", t1."Number", t1."Note", t1."Total", t1."Discount", t1."Quantity", t1."Priority", t1."Points", t1."Weight", t1."IsPaid", t1."IsGift", t1."Status", t1."Options", t1."CreatedAt", t1."PaidAt", t1."ShippedOn", t1."DeliveryTime", t1."TrackingId", t1."Tags"
                        FROM "Order" AS t1
                        ORDER BY t1."Total"
                        LIMIT 5
                    ) AS t2
                )
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
