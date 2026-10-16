namespace Linq.Expressions.Translation.PostgreSql.Tests.Combinations;

public sealed record AggregateCombinationTests
{
    [Fact]
    public void SumsLongAsBigint()
    {
        Expression query = Orders.Executed(q => q.Sum(o => o.Points));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COALESCE(SUM(t0."Points"), 0) AS bigint)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SumsDoubleWithoutCast()
    {
        Expression query = Orders.Executed(q => q.Sum(o => o.Weight));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(SUM(t0."Weight"), 0)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AveragesLongAsDouble()
    {
        Expression query = Orders.Executed(q => q.Average(o => o.Points));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT AVG(CAST(t0."Points" AS double precision))
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AveragesDecimalWithoutCast()
    {
        Expression query = Orders.Executed(q => q.Average(o => o.Total));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT AVG(t0."Total")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SumsProductOfDecimalAndInteger()
    {
        Expression query = Orders.Executed(q => q.Sum(o => o.Total * o.Quantity));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(SUM(t0."Total" * CAST(t0."Quantity" AS numeric)), 0)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SumsProjectedValuesOfFilteredRows()
    {
        Expression query = Orders
            .Where(o => o.IsPaid)
            .Select(o => o.Total)
            .Executed(q => q.Sum());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(SUM(t0."Total"), 0)
                FROM "Order" AS t0
                WHERE t0."IsPaid"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CountsDistinctComputedValues()
    {
        Expression query = Orders
            .Select(o => o.Quantity * 2)
            .Distinct()
            .Executed(q => q.Count());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COUNT(*) AS integer)
                FROM (
                    SELECT DISTINCT t0."Quantity" * 2 AS "Value"
                    FROM "Order" AS t0
                ) AS t1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CountsTakenRows()
    {
        Expression query = Orders
            .OrderBy(o => o.Id)
            .Select(o => o.Id)
            .Take(10)
            .Executed(q => q.Count());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COUNT(*) AS integer)
                FROM (
                    SELECT t0."Id"
                    FROM "Order" AS t0
                    ORDER BY t0."Id"
                    LIMIT 10
                ) AS t1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesMaximumOfGroupCounts()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => g.Count())
            .Executed(q => q.Max());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT MAX(t1."Value")
                FROM (
                    SELECT CAST(COUNT(*) AS integer) AS "Value"
                    FROM "Order" AS t0
                    GROUP BY t0."CustomerId"
                ) AS t1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersComputedValuesAfterTake()
    {
        Expression query = Orders
            .Select(o => o.Quantity + 1)
            .Take(3)
            .Where(x => x > 2)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Value"
                FROM (
                    SELECT t0."Quantity" + 1 AS "Value"
                    FROM "Order" AS t0
                    LIMIT 3
                ) AS t1
                WHERE t1."Value" > 2
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersNamedComputedValuesAfterTake()
    {
        Expression query = Orders
            .Select(o => new { Next = o.Quantity + 1 })
            .Take(3)
            .Where(x => x.Next > 2)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Next"
                FROM (
                    SELECT t0."Quantity" + 1 AS "Next"
                    FROM "Order" AS t0
                    LIMIT 3
                ) AS t1
                WHERE t1."Next" > 2
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesFirstAndLastMomentPerGroup()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new
            {
                g.Key,
                First = g.Min(o => o.CreatedAt),
                Last = g.Max(o => o.CreatedAt),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId" AS "Key", MIN(t0."CreatedAt") AS "First", MAX(t0."CreatedAt") AS "Last"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void GroupsByYearAndMonth()
    {
        Expression query = Orders
            .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Revenue = g.Sum(o => o.Total),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(EXTRACT(YEAR FROM t0."CreatedAt") AS integer) AS "Year",
                CAST(EXTRACT(MONTH FROM t0."CreatedAt") AS integer) AS "Month",
                COALESCE(SUM(t0."Total"), 0) AS "Revenue"
                FROM "Order" AS t0
                GROUP BY CAST(EXTRACT(YEAR FROM t0."CreatedAt") AS integer), CAST(EXTRACT(MONTH FROM t0."CreatedAt") AS integer)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RanksFilteredGroupsByRevenue()
    {
        Expression query = Orders
            .Where(o => o.IsPaid)
            .GroupBy(o => o.CustomerId)
            .Where(g => g.Key > 10 && g.Count() > 2)
            .OrderByDescending(g => g.Sum(o => o.Total))
            .Select(g => g.Key)
            .Take(5)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId"
                FROM "Order" AS t0
                WHERE t0."IsPaid"
                GROUP BY t0."CustomerId"
                HAVING (t0."CustomerId" > 10) AND (CAST(COUNT(*) AS integer) > 2)
                ORDER BY COALESCE(SUM(t0."Total"), 0) DESC
                LIMIT 5
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CountsDistinctCustomersPerStatus()
    {
        Expression query = Orders
            .GroupBy(o => o.Status)
            .Select(g => new
            {
                Status = g.Key,
                Customers = g.Select(o => o.CustomerId).Distinct().Count(),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Status", CAST(COUNT(DISTINCT t0."CustomerId") AS integer) AS "Customers"
                FROM "Order" AS t0
                GROUP BY t0."Status"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AggregatesSelectedElementsSeveralWays()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId, o => o.Total)
            .Select(g => new
            {
                g.Key,
                Sum = g.Sum(),
                Max = g.Max(),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId" AS "Key", COALESCE(SUM(t0."Total"), 0) AS "Sum", MAX(t0."Total") AS "Max"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CountsMatchingRowsInResultSelector()
    {
        Expression query = Orders
            .GroupBy(
                o => o.CustomerId,
                (k, os) => new { k, Paid = os.Count(o => o.IsPaid) }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId" AS "k", CAST(COUNT(*) FILTER (WHERE t0."IsPaid") AS integer) AS "Paid"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void GroupsByBooleanColumn()
    {
        Expression query = Orders
            .GroupBy(o => o.IsPaid)
            .Select(g => new { Paid = g.Key, Mean = g.Average(o => o.Total) })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."IsPaid" AS "Paid", AVG(t0."Total") AS "Mean"
                FROM "Order" AS t0
                GROUP BY t0."IsPaid"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void GroupsByConditionalBucket()
    {
        Expression query = Orders
            .GroupBy(o => o.Total > 100 ? "big" : "small")
            .Select(g => new { Size = g.Key, Count = g.Count() })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CASE WHEN t0."Total" > 100 THEN 'big' ELSE 'small' END AS "Size",
                CAST(COUNT(*) AS integer) AS "Count"
                FROM "Order" AS t0
                GROUP BY CASE WHEN t0."Total" > 100 THEN 'big' ELSE 'small' END
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AveragesIntegerPerGroupAsDouble()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new { g.Key, Mean = g.Average(o => o.Quantity) })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId" AS "Key", AVG(CAST(t0."Quantity" AS double precision)) AS "Mean"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SumsCoalescedNullablePerGroup()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new { g.Key, Discounts = g.Sum(o => o.Discount ?? 0m) })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId" AS "Key", COALESCE(SUM(COALESCE(t0."Discount", 0)), 0) AS "Discounts"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComputesPercentageOfPaidOrdersPerGroup()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new { g.Key, Share = g.Count(o => o.IsPaid) * 100 / g.Count() })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId" AS "Key",
                (CAST(COUNT(*) FILTER (WHERE t0."IsPaid") AS integer) * 100) / CAST(COUNT(*) AS integer) AS "Share"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void GroupsProjectedRowsWithoutWrapping()
    {
        Expression query = Orders
            .Select(o => new { o.CustomerId, Value = o.Total * 2 })
            .GroupBy(x => x.CustomerId)
            .Select(g => new { g.Key, Sum = g.Sum(x => x.Value) })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId" AS "Key", COALESCE(SUM(t0."Total" * 2), 0) AS "Sum"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void GroupsTakenRowsInDerivedTable()
    {
        Expression query = Orders
            .OrderBy(o => o.Id)
            .Select(o => new { o.CustomerId, o.Total })
            .Take(100)
            .GroupBy(x => x.CustomerId)
            .Select(g => new { g.Key, Sum = g.Sum(x => x.Total) })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."CustomerId" AS "Key", COALESCE(SUM(t1."Total"), 0) AS "Sum"
                FROM (
                    SELECT t0."CustomerId", t0."Total"
                    FROM "Order" AS t0
                    ORDER BY t0."Id"
                    LIMIT 100
                ) AS t1
                GROUP BY t1."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CountsGroups()
    {
        Expression query = Orders.GroupBy(o => o.CustomerId).Executed(q => q.Count());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COUNT(*) AS integer)
                FROM (
                    SELECT t0."CustomerId"
                    FROM "Order" AS t0
                    GROUP BY t0."CustomerId"
                ) AS t1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ChecksAnyGroupMatchesAggregate()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Executed(q => q.Any(g => g.Count() > 5));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT EXISTS (
                    SELECT 1
                    FROM "Order" AS t0
                    GROUP BY t0."CustomerId"
                    HAVING CAST(COUNT(*) AS integer) > 5
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void GroupsJoinedRowsByColumnOfInnerSource()
    {
        Expression query = Orders
            .Join(
                Customers,
                o => o.CustomerId,
                c => c.Id,
                (o, c) => new { c.Region, o.Total }
            )
            .GroupBy(x => x.Region)
            .Select(g => new { Region = g.Key, Revenue = g.Sum(x => x.Total) })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Region", COALESCE(SUM(t0."Total"), 0) AS "Revenue"
                FROM "Order" AS t0
                INNER JOIN "Customer" AS t1 ON t0."CustomerId" = t1."Id"
                GROUP BY t1."Region"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OrdersGroupsByKeyThenAggregate()
    {
        Expression query = Orders
            .GroupBy(o => o.Status)
            .OrderBy(g => g.Key)
            .ThenByDescending(g => g.Count())
            .Select(g => new { Status = g.Key, Count = g.LongCount() })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Status", COUNT(*) AS "Count"
                FROM "Order" AS t0
                GROUP BY t0."Status"
                ORDER BY t0."Status", CAST(COUNT(*) AS integer) DESC
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void DropsOrderingInsideCorrelatedSum()
    {
        Expression query = Customers
            .Select(c => new
            {
                c.Name,
                Spent = Orders
                    .Where(o => o.CustomerId == c.Id)
                    .OrderBy(o => o.Id)
                    .Sum(o => o.Total),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", (
                    SELECT COALESCE(SUM(t1."Total"), 0)
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                ) AS "Spent"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByCorrelatedCountWithPredicate()
    {
        Expression query = Customers
            .Where(c => Orders.Count(o => o.CustomerId == c.Id && o.IsPaid) >= 2)
            .Select(c => c.Name)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name"
                FROM "Customer" AS t0
                WHERE (
                    SELECT CAST(COUNT(*) AS integer)
                    FROM "Order" AS t1
                    WHERE (t1."CustomerId" = t0."Id") AND t1."IsPaid"
                ) >= 2
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersGroupsByFractionalAverage()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Where(g => g.Average(o => o.Total) > 50.5m)
            .Select(g => g.Key)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                HAVING AVG(t0."Total") > 50.5
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
