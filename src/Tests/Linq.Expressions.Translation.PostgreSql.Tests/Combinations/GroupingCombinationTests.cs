namespace Linq.Expressions.Translation.PostgreSql.Tests.Combinations;

public sealed record GroupingCombinationTests
{
    [Fact]
    public void FiltersGroupsByCountAndRevenue()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                Count = g.Count(),
                Revenue = g.Sum(o => o.Total),
            })
            .Where(x => (x.Count > 1) && (x.Revenue > 100m))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId", CAST(COUNT(*) AS integer) AS "Count",
                COALESCE(SUM(t0."Total"), 0) AS "Revenue"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                HAVING (CAST(COUNT(*) AS integer) > 1) AND (COALESCE(SUM(t0."Total"),
                0) > 100)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersOrdersAndPagesGroups()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                Count = g.Count(),
                Revenue = g.Sum(o => o.Total),
            })
            .Where(x => x.Count > 1)
            .OrderByDescending(x => x.Revenue)
            .ThenBy(x => x.CustomerId)
            .Skip(5)
            .Take(5)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId", CAST(COUNT(*) AS integer) AS "Count",
                COALESCE(SUM(t0."Total"), 0) AS "Revenue"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                HAVING CAST(COUNT(*) AS integer) > 1
                ORDER BY COALESCE(SUM(t0."Total"), 0) DESC, t0."CustomerId"
                LIMIT 5 OFFSET 5
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void WrapsFilterAfterPagedGroups()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                Count = g.Count(),
                Revenue = g.Sum(o => o.Total),
            })
            .OrderBy(x => x.CustomerId)
            .Take(5)
            .Where(x => x.Count > 1)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."CustomerId", t1."Count", t1."Revenue"
                FROM (
                    SELECT t0."CustomerId", CAST(COUNT(*) AS integer) AS "Count",
                    COALESCE(SUM(t0."Total"), 0) AS "Revenue"
                    FROM "Order" AS t0
                    GROUP BY t0."CustomerId"
                    ORDER BY t0."CustomerId"
                    LIMIT 5
                ) AS t1
                WHERE t1."Count" > 1
                ORDER BY t1."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void GroupsGroupedRows()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                Count = g.Count(),
                Revenue = g.Sum(o => o.Total),
            })
            .GroupBy(x => x.Count)
            .Select(g => new { Count = g.Key, Customers = g.Count() })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Count", CAST(COUNT(*) AS integer) AS "Customers"
                FROM (
                    SELECT t0."CustomerId", CAST(COUNT(*) AS integer) AS "Count",
                    COALESCE(SUM(t0."Total"), 0) AS "Revenue"
                    FROM "Order" AS t0
                    GROUP BY t0."CustomerId"
                ) AS t1
                GROUP BY t1."Count"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void GroupsGroupedRowsAndAggregatesGroupedColumn()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                Count = g.Count(),
                Revenue = g.Sum(o => o.Total),
            })
            .GroupBy(x => x.Count)
            .Select(g => new
            {
                Count = g.Key,
                Revenue = g.Sum(x => x.Revenue),
                Largest = g.Max(x => x.Revenue),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Count", COALESCE(SUM(t1."Revenue"), 0) AS "Revenue",
                MAX(t1."Revenue") AS "Largest"
                FROM (
                    SELECT t0."CustomerId", CAST(COUNT(*) AS integer) AS "Count",
                    COALESCE(SUM(t0."Total"), 0) AS "Revenue"
                    FROM "Order" AS t0
                    GROUP BY t0."CustomerId"
                ) AS t1
                GROUP BY t1."Count"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsDistinctGroupedValues()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                Count = g.Count(),
                Revenue = g.Sum(o => o.Total),
            })
            .Select(x => x.Count)
            .Distinct()
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT DISTINCT t1."Count"
                FROM (
                    SELECT t0."CustomerId", CAST(COUNT(*) AS integer) AS "Count",
                    COALESCE(SUM(t0."Total"), 0) AS "Revenue"
                    FROM "Order" AS t0
                    GROUP BY t0."CustomerId"
                ) AS t1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void UnionsGroupedRowsWithOtherRows()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new { Id = g.Key, Amount = g.Sum(o => o.Total) })
            .Union(
                OrderLines
                    .GroupBy(l => l.OrderId)
                    .Select(g => new { Id = g.Key, Amount = g.Sum(l => l.UnitPrice) })
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId" AS "Id", COALESCE(SUM(t0."Total"), 0) AS "Amount"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                UNION
                SELECT t1."OrderId" AS "Id", COALESCE(SUM(t1."UnitPrice"),
                0) AS "Amount"
                FROM "OrderLine" AS t1
                GROUP BY t1."OrderId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsGroupedRowsWithEntity()
    {
        Expression query = Customers
            .Join(
                Orders
                    .GroupBy(o => o.CustomerId)
                    .Select(g => new { CustomerId = g.Key, Count = g.Count() }),
                c => c.Id,
                x => x.CustomerId,
                (c, x) => new { c.Name, x.Count }
            )
            .OrderByDescending(y => y.Count)
            .Take(5)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t2."Count"
                FROM "Customer" AS t0
                INNER JOIN (
                    SELECT t1."CustomerId", CAST(COUNT(*) AS integer) AS "Count"
                    FROM "Order" AS t1
                    GROUP BY t1."CustomerId"
                ) AS t2 ON t0."Id" = t2."CustomerId"
                ORDER BY t2."Count" DESC
                LIMIT 5
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void GroupsAfterFilteringJoiningAndProjecting()
    {
        Expression query = Orders
            .Where(o => o.IsPaid)
            .Select(o => new
            {
                o.CustomerId,
                o.Total,
                o.CreatedAt.Month,
            })
            .GroupBy(x => new { x.CustomerId, x.Month })
            .Select(g => new
            {
                g.Key.CustomerId,
                g.Key.Month,
                Revenue = g.Sum(x => x.Total),
            })
            .Where(x => x.Revenue > 50m)
            .OrderBy(x => x.CustomerId)
            .ThenBy(x => x.Month)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId",
                CAST(EXTRACT(MONTH FROM t0."CreatedAt") AS integer) AS "Month",
                COALESCE(SUM(t0."Total"), 0) AS "Revenue"
                FROM "Order" AS t0
                WHERE t0."IsPaid"
                GROUP BY t0."CustomerId",
                CAST(EXTRACT(MONTH FROM t0."CreatedAt") AS integer)
                HAVING COALESCE(SUM(t0."Total"), 0) > 50
                ORDER BY t0."CustomerId",
                CAST(EXTRACT(MONTH FROM t0."CreatedAt") AS integer)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void GroupsByKeyThenFiltersOnKey()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                Count = g.Count(),
                Revenue = g.Sum(o => o.Total),
            })
            .Where(x => x.CustomerId > 10)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId", CAST(COUNT(*) AS integer) AS "Count",
                COALESCE(SUM(t0."Total"), 0) AS "Revenue"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                HAVING t0."CustomerId" > 10
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AveragesGroupedAggregateInProjection()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                Ratio = g.Sum(o => o.Total) / g.Count(),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId", COALESCE(SUM(t0."Total"),
                0) / CAST(CAST(COUNT(*) AS integer) AS numeric) AS "Ratio"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesTwoAggregatesOfSameGroup()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Where(g => g.Max(o => o.Total) > g.Average(o => o.Total) * 2)
            .Select(g => g.Key)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                HAVING MAX(t0."Total") > (AVG(t0."Total") * 2)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CountsGroupsAboveThreshold()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Where(g => g.Count() > 2)
            .Executed(q => q.Count());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COUNT(*) AS integer)
                FROM (
                    SELECT t0."CustomerId"
                    FROM "Order" AS t0
                    GROUP BY t0."CustomerId"
                    HAVING CAST(COUNT(*) AS integer) > 2
                ) AS t1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ChecksAnyGroupAboveThreshold()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Executed(q => q.Any(g => g.Count() > 2));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT EXISTS (
                    SELECT 1
                    FROM "Order" AS t0
                    GROUP BY t0."CustomerId"
                    HAVING CAST(COUNT(*) AS integer) > 2
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesTopGroup()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                Count = g.Count(),
                Revenue = g.Sum(o => o.Total),
            })
            .OrderByDescending(x => x.Revenue)
            .Executed(q => q.First());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId", CAST(COUNT(*) AS integer) AS "Count",
                COALESCE(SUM(t0."Total"), 0) AS "Revenue"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                ORDER BY COALESCE(SUM(t0."Total"), 0) DESC
                LIMIT 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesLastGroupKey()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => g.Key)
            .OrderBy(k => k)
            .Executed(q => q.Last());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                ORDER BY t0."CustomerId" DESC
                LIMIT 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AveragesRevenuePerGroup()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                Count = g.Count(),
                Revenue = g.Sum(o => o.Total),
            })
            .Executed(q => q.Average(x => x.Revenue));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT AVG(t1."Revenue")
                FROM (
                    SELECT t0."CustomerId", CAST(COUNT(*) AS integer) AS "Count",
                    COALESCE(SUM(t0."Total"), 0) AS "Revenue"
                    FROM "Order" AS t0
                    GROUP BY t0."CustomerId"
                ) AS t1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SumsCountsPerGroup()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                Count = g.Count(),
                Revenue = g.Sum(o => o.Total),
            })
            .Executed(q => q.Sum(x => x.Count));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COALESCE(SUM(t1."Count"), 0) AS integer)
                FROM (
                    SELECT t0."CustomerId", CAST(COUNT(*) AS integer) AS "Count",
                    COALESCE(SUM(t0."Total"), 0) AS "Revenue"
                    FROM "Order" AS t0
                    GROUP BY t0."CustomerId"
                ) AS t1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
