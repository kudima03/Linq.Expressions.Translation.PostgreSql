namespace Linq.Expressions.Translation.PostgreSql.Tests.Operators;

public sealed record GroupByTests
{
    [Fact(Skip = Skipped.NotImplemented)]
    public void GroupsAndCountsRows()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new { CustomerId = g.Key, Count = g.Count() })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId", CAST(COUNT(*) AS integer) AS "Count"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void AliasesRenamedGroupKey()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new { Customer = g.Key, Count = g.Count() })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId" AS "Customer",
                CAST(COUNT(*) AS integer) AS "Count"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void ProjectsOnlyGroupKey()
    {
        Expression query = Customers.GroupBy(c => c.Region).Select(g => g.Key).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Region"
                FROM "Customer" AS t0
                GROUP BY t0."Region"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void LongCountsGroupedRows()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new { CustomerId = g.Key, Count = g.LongCount() })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId", COUNT(*) AS "Count"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void SumsGroupedDecimal()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new { CustomerId = g.Key, Revenue = g.Sum(o => o.Total) })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId", COALESCE(SUM(t0."Total"), 0) AS "Revenue"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void SumsGroupedInteger()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new { CustomerId = g.Key, Units = g.Sum(o => o.Quantity) })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId", CAST(COALESCE(SUM(t0."Quantity"),
                0) AS integer) AS "Units"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void AveragesGroupedDecimal()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new { CustomerId = g.Key, Mean = g.Average(o => o.Total) })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId", AVG(t0."Total") AS "Mean"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void AveragesGroupedInteger()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new { CustomerId = g.Key, Mean = g.Average(o => o.Quantity) })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId",
                AVG(CAST(t0."Quantity" AS double precision)) AS "Mean"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FindsGroupedMinimumAndMaximum()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                First = g.Min(o => o.CreatedAt),
                Last = g.Max(o => o.CreatedAt),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId", MIN(t0."CreatedAt") AS "First",
                MAX(t0."CreatedAt") AS "Last"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FindsGroupedStringExtremes()
    {
        Expression query = Customers
            .GroupBy(c => c.Region)
            .Select(g => new
            {
                g.Key,
                Shortest = g.Min(c => c.Name),
                Longest = g.Max(c => c.Name),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Region" AS "Key", MIN(t0."Name") AS "Shortest",
                MAX(t0."Name") AS "Longest"
                FROM "Customer" AS t0
                GROUP BY t0."Region"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void ComputesSeveralAggregates()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                Count = g.Count(),
                Revenue = g.Sum(o => o.Total),
                Largest = g.Max(o => o.Total),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId", CAST(COUNT(*) AS integer) AS "Count",
                COALESCE(SUM(t0."Total"), 0) AS "Revenue", MAX(t0."Total") AS "Largest"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void GroupsByCompositeKey()
    {
        Expression query = Orders
            .GroupBy(o => new { o.CustomerId, o.Status })
            .Select(g => new
            {
                g.Key.CustomerId,
                g.Key.Status,
                Count = g.Count(),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId", t0."Status",
                CAST(COUNT(*) AS integer) AS "Count"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId", t0."Status"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void GroupsByComputedKey()
    {
        Expression query = Customers
            .GroupBy(c => c.Name.Length)
            .Select(g => new { Length = g.Key, Count = g.Count() })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT LENGTH(t0."Name") AS "Length",
                CAST(COUNT(*) AS integer) AS "Count"
                FROM "Customer" AS t0
                GROUP BY LENGTH(t0."Name")
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void GroupsByBooleanKey()
    {
        Expression query = Customers
            .GroupBy(c => c.IsActive)
            .Select(g => new { g.Key, Count = g.Count() })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."IsActive" AS "Key", CAST(COUNT(*) AS integer) AS "Count"
                FROM "Customer" AS t0
                GROUP BY t0."IsActive"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void GroupsByNullableKey()
    {
        Expression query = Customers
            .GroupBy(c => c.Email)
            .Select(g => new { g.Key, Count = g.Count() })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Email" AS "Key", CAST(COUNT(*) AS integer) AS "Count"
                FROM "Customer" AS t0
                GROUP BY t0."Email"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void GroupsByEnumKey()
    {
        Expression query = Orders
            .GroupBy(o => o.Status)
            .Select(g => new { g.Key, Count = g.Count() })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Status" AS "Key", CAST(COUNT(*) AS integer) AS "Count"
                FROM "Order" AS t0
                GROUP BY t0."Status"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void GroupsByDatePartKey()
    {
        Expression query = Orders
            .GroupBy(o => o.CreatedAt.Year)
            .Select(g => new { Year = g.Key, Revenue = g.Sum(o => o.Total) })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(EXTRACT(YEAR FROM t0."CreatedAt") AS integer) AS "Year",
                COALESCE(SUM(t0."Total"), 0) AS "Revenue"
                FROM "Order" AS t0
                GROUP BY CAST(EXTRACT(YEAR FROM t0."CreatedAt") AS integer)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void GroupsWithElementSelector()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId, o => o.Total)
            .Select(g => new { CustomerId = g.Key, Revenue = g.Sum() })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId", COALESCE(SUM(t0."Total"), 0) AS "Revenue"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void CountsGroupsWithElementSelector()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId, o => o.Total)
            .Select(g => new { CustomerId = g.Key, Count = g.Count() })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId", CAST(COUNT(*) AS integer) AS "Count"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void GroupsWithResultSelector()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId, (key, os) => new { key, Count = os.Count() })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId" AS "key", CAST(COUNT(*) AS integer) AS "Count"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void GroupsWithElementAndResultSelectors()
    {
        Expression query = Orders
            .GroupBy(
                o => o.CustomerId,
                o => o.Total,
                (key, totals) => new { key, Revenue = totals.Sum() }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId" AS "key", COALESCE(SUM(t0."Total"),
                0) AS "Revenue"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void GroupsAfterFiltering()
    {
        Expression query = Orders
            .Where(o => o.IsPaid)
            .GroupBy(o => o.CustomerId)
            .Select(g => new { CustomerId = g.Key, Count = g.Count() })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId", CAST(COUNT(*) AS integer) AS "Count"
                FROM "Order" AS t0
                WHERE t0."IsPaid"
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void GroupsAfterProjection()
    {
        Expression query = Orders
            .Select(o => new { o.CustomerId, Net = o.Total - o.Discount })
            .GroupBy(x => x.CustomerId)
            .Select(g => new { CustomerId = g.Key, Net = g.Sum(x => x.Net) })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId", COALESCE(SUM(t0."Total" - t0."Discount"),
                0) AS "Net"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void GroupsJoinedRows()
    {
        Expression query = Customers
            .Join(
                Orders,
                c => c.Id,
                o => o.CustomerId,
                (c, o) => new { c.Region, o.Total }
            )
            .GroupBy(x => x.Region)
            .Select(g => new { Region = g.Key, Revenue = g.Sum(x => x.Total) })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Region", COALESCE(SUM(t1."Total"), 0) AS "Revenue"
                FROM "Customer" AS t0
                INNER JOIN "Order" AS t1 ON t0."Id" = t1."CustomerId"
                GROUP BY t0."Region"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FiltersGroupsByAggregateAsHaving()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Where(g => g.Count() > 1)
            .Select(g => new { CustomerId = g.Key, Count = g.Count() })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId", CAST(COUNT(*) AS integer) AS "Count"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                HAVING CAST(COUNT(*) AS integer) > 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FiltersGroupsByProjectedAggregateAsHaving()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new { CustomerId = g.Key, Count = g.Count() })
            .Where(x => x.Count > 1)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId", CAST(COUNT(*) AS integer) AS "Count"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                HAVING CAST(COUNT(*) AS integer) > 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FiltersGroupsBySum()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Where(g => g.Sum(o => o.Total) > 1000)
            .Select(g => g.Key)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                HAVING COALESCE(SUM(t0."Total"), 0) > 1000
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FiltersGroupsByKeyAsHaving()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Where(g => g.Key > 5)
            .Select(g => new { CustomerId = g.Key, Count = g.Count() })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId", CAST(COUNT(*) AS integer) AS "Count"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                HAVING t0."CustomerId" > 5
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void CombinesHavingConditions()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Where(g => (g.Count() > 1) && (g.Sum(o => o.Total) > 100))
            .Select(g => g.Key)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                HAVING (CAST(COUNT(*) AS integer) > 1) AND (COALESCE(SUM(t0."Total"),
                0) > 100)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void CombinesSeveralHavingClauses()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Where(g => g.Count() > 1)
            .Where(g => g.Max(o => o.Total) < 500)
            .Select(g => g.Key)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                HAVING (CAST(COUNT(*) AS integer) > 1) AND (MAX(t0."Total") < 500)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void CountsGroupedRowsMatchingPredicate()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new { CustomerId = g.Key, Paid = g.Count(o => o.IsPaid) })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId",
                CAST(COUNT(*) FILTER (WHERE t0."IsPaid") AS integer) AS "Paid"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void SumsGroupedRowsMatchingPredicate()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                Paid = g.Where(o => o.IsPaid).Sum(o => o.Total),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId",
                COALESCE(SUM(t0."Total") FILTER (WHERE t0."IsPaid"), 0) AS "Paid"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void CountsDistinctGroupedValues()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                Statuses = g.Select(o => o.Status).Distinct().Count(),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId",
                CAST(COUNT(DISTINCT t0."Status") AS integer) AS "Statuses"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void ChecksAnyGroupedRowMatches()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new { CustomerId = g.Key, HasLarge = g.Any(o => o.Total > 100) })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId", BOOL_OR(t0."Total" > 100) AS "HasLarge"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void ChecksAllGroupedRowsMatch()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new { CustomerId = g.Key, AllPaid = g.All(o => o.IsPaid) })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId", BOOL_AND(t0."IsPaid") AS "AllPaid"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void OrdersGroupsByAggregate()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new { CustomerId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId", CAST(COUNT(*) AS integer) AS "Count"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                ORDER BY CAST(COUNT(*) AS integer) DESC
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void OrdersGroupsByKeyAndAggregate()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new { CustomerId = g.Key, Revenue = g.Sum(o => o.Total) })
            .OrderByDescending(x => x.Revenue)
            .ThenBy(x => x.CustomerId)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId", COALESCE(SUM(t0."Total"), 0) AS "Revenue"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                ORDER BY COALESCE(SUM(t0."Total"), 0) DESC, t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void PagesGroups()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new { CustomerId = g.Key, Count = g.Count() })
            .OrderBy(x => x.CustomerId)
            .Skip(10)
            .Take(5)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId", CAST(COUNT(*) AS integer) AS "Count"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                ORDER BY t0."CustomerId"
                LIMIT 5 OFFSET 10
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FiltersOrdersAndPagesGroups()
    {
        Expression query = Orders
            .Where(o => o.IsPaid)
            .GroupBy(o => o.CustomerId)
            .Where(g => g.Count() > 1)
            .Select(g => new { CustomerId = g.Key, Revenue = g.Sum(o => o.Total) })
            .OrderByDescending(x => x.Revenue)
            .Take(3)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId", COALESCE(SUM(t0."Total"), 0) AS "Revenue"
                FROM "Order" AS t0
                WHERE t0."IsPaid"
                GROUP BY t0."CustomerId"
                HAVING CAST(COUNT(*) AS integer) > 1
                ORDER BY COALESCE(SUM(t0."Total"), 0) DESC
                LIMIT 3
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
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

    [Fact(Skip = Skipped.NotImplemented)]
    public void CountsProjectedGroups()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new { CustomerId = g.Key, Revenue = g.Sum(o => o.Total) })
            .Executed(q => q.Count());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COUNT(*) AS integer)
                FROM (
                    SELECT t0."CustomerId", COALESCE(SUM(t0."Total"), 0) AS "Revenue"
                    FROM "Order" AS t0
                    GROUP BY t0."CustomerId"
                ) AS t1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void AveragesAggregateOfGroups()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new { CustomerId = g.Key, Revenue = g.Sum(o => o.Total) })
            .Executed(q => q.Average(x => x.Revenue));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT AVG(t1."Revenue")
                FROM (
                    SELECT t0."CustomerId", COALESCE(SUM(t0."Total"), 0) AS "Revenue"
                    FROM "Order" AS t0
                    GROUP BY t0."CustomerId"
                ) AS t1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FiltersProjectedGroupsAfterPaging()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new { CustomerId = g.Key, Count = g.Count() })
            .OrderBy(x => x.CustomerId)
            .Take(5)
            .Where(x => x.Count > 1)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."CustomerId", t1."Count"
                FROM (
                    SELECT t0."CustomerId", CAST(COUNT(*) AS integer) AS "Count"
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
}
