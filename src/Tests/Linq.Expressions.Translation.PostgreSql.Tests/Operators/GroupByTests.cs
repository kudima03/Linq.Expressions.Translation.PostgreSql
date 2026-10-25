namespace Linq.Expressions.Translation.PostgreSql.Tests.Operators;

public sealed record GroupByTests
{
    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
    public void CountsGroupsOfComputedKey()
    {
        Expression query = Orders.GroupBy(o => o.Total * 2).Executed(q => q.Count());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COUNT(*) AS integer)
                FROM (
                    SELECT t0."Total" * 2 AS "Value"
                    FROM "Order" AS t0
                    GROUP BY t0."Total" * 2
                ) AS t1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersPagedComputedKeys()
    {
        Expression query = Orders
            .GroupBy(o => o.Total * 2)
            .Select(g => g.Key)
            .Skip(1)
            .Where(k => k > 1)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Value"
                FROM (
                    SELECT t0."Total" * 2 AS "Value"
                    FROM "Order" AS t0
                    GROUP BY t0."Total" * 2
                    OFFSET 1
                ) AS t1
                WHERE t1."Value" > 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SumsDistinctGroupedValues()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => g.Select(o => o.Total).Distinct().Sum())
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(SUM(DISTINCT t0."Total"), 0)
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AveragesDistinctGroupedValues()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => g.Select(o => o.Quantity).Distinct().Average())
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT AVG(DISTINCT CAST(t0."Quantity" AS double precision))
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SumsDistinctGroupElements()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId, o => o.Quantity)
            .Select(g => g.Distinct().Sum())
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COALESCE(SUM(DISTINCT t0."Quantity"), 0) AS integer)
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SumsDistinctFilteredGroupedValues()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => g.Where(o => o.IsPaid).Select(o => o.Total).Distinct().Sum())
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(SUM(DISTINCT t0."Total") FILTER (WHERE t0."IsPaid"), 0)
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void IgnoresDistinctForGroupedMaximum()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => g.Select(o => o.Total).Distinct().Max())
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT MAX(t0."Total")
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void IgnoresDistinctForGroupedMinimumWithSelector()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => g.Distinct().Min(o => o.Total))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT MIN(t0."Total")
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void GroupsAllRowsByConstantKey()
    {
        Expression query = Orders
            .GroupBy(o => 1)
            .Select(g => new { Count = g.Count(), Total = g.Sum(o => o.Total) })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COUNT(*) AS integer) AS "Count",
                COALESCE(SUM(t0."Total"), 0) AS "Total"
                FROM "Order" AS t0
                GROUP BY CAST(1 AS integer)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ProjectsConstantGroupKey()
    {
        Expression query = Orders
            .GroupBy(o => "all")
            .Select(g => new { g.Key, Count = g.Count() })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT 'all' AS "Key", CAST(COUNT(*) AS integer) AS "Count"
                FROM "Order" AS t0
                GROUP BY CAST(1 AS integer)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OmitsConstantMemberOfCompositeGroupKey()
    {
        Expression query = Orders
            .GroupBy(o => new { o.CustomerId, All = true })
            .Select(g => new { g.Key, Count = g.Count() })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId" AS "Key.CustomerId", TRUE AS "Key.All",
                CAST(COUNT(*) AS integer) AS "Count"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OmitsConstantGroupKeyFromOrdering()
    {
        Expression query = Orders
            .GroupBy(o => 1)
            .OrderBy(g => g.Key)
            .Select(g => g.Count())
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COUNT(*) AS integer)
                FROM "Order" AS t0
                GROUP BY CAST(1 AS integer)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesMaximumOfNullableTextNullSafe()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => g.Max(o => o.Note))
            .Where(n => n != "a")
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT MAX(t0."Note")
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                HAVING MAX(t0."Note") IS DISTINCT FROM 'a'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesFilteredMaximumOfTextNullSafe()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => g.Where(o => o.IsPaid).Max(o => o.Number))
            .Where(n => n != "a")
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT MAX(t0."Number") FILTER (WHERE t0."IsPaid")
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                HAVING MAX(t0."Number") FILTER (WHERE t0."IsPaid") IS DISTINCT FROM 'a'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesMaximumOfTextDirectly()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => g.Max(o => o.Number))
            .Where(n => n != "a")
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT MAX(t0."Number")
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                HAVING MAX(t0."Number") <> 'a'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesFilteredMaximumOfValueDirectly()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => g.Where(o => o.IsPaid).Max(o => o.Total))
            .Where(t => t != 1)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT MAX(t0."Total") FILTER (WHERE t0."IsPaid")
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                HAVING MAX(t0."Total") FILTER (WHERE t0."IsPaid") <> 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CountsNullAmongDistinctNullableValues()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => g.Select(o => o.Note).Distinct().Count())
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COUNT(DISTINCT t0."Note") + CASE WHEN BOOL_OR(t0."Note" IS NULL) THEN 1 ELSE 0 END AS integer)
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CountsNullAmongDistinctFilteredNullableValues()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g =>
                g.Where(o => o.IsPaid).Select(o => o.Note).Distinct().LongCount()
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COUNT(DISTINCT t0."Note") FILTER (WHERE t0."IsPaid") + CASE WHEN BOOL_OR(t0."Note" IS NULL) FILTER (WHERE t0."IsPaid") THEN 1 ELSE 0 END
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AggregatesBooleansWithBoolOrAndBoolAnd()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new
            {
                AnyPaid = g.Max(o => o.IsPaid),
                AllPaid = g.Min(o => o.IsPaid),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT BOOL_OR(t0."IsPaid") AS "AnyPaid", BOOL_AND(t0."IsPaid") AS "AllPaid"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsKeyOfPagedGroupsAfterDistinct()
    {
        Expression query = Customers
            .GroupBy(c => c.Region)
            .Take(5)
            .Distinct()
            .Select(g => g.Key)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t2."Region"
                FROM (
                    SELECT DISTINCT t1."Region"
                    FROM (
                        SELECT t0."Region"
                        FROM "Customer" AS t0
                        GROUP BY t0."Region"
                        LIMIT 5
                    ) AS t1
                ) AS t2
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OrdersPagedGroupsByKey()
    {
        Expression query = Customers
            .GroupBy(c => c.Region)
            .Take(5)
            .OrderBy(g => g.Key)
            .Select(g => g.Key)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Region"
                FROM (
                    SELECT t0."Region"
                    FROM "Customer" AS t0
                    GROUP BY t0."Region"
                    LIMIT 5
                ) AS t1
                ORDER BY t1."Region"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void NamesComputedKeyOfPagedGroups()
    {
        Expression query = Customers
            .GroupBy(c => c.Name.ToUpper())
            .Take(5)
            .Where(g => g.Key != "A")
            .Select(g => g.Key)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Value"
                FROM (
                    SELECT UPPER(t0."Name") AS "Value"
                    FROM "Customer" AS t0
                    GROUP BY UPPER(t0."Name")
                    LIMIT 5
                ) AS t1
                WHERE t1."Value" <> 'A'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ChecksAnyPagedGroupByKey()
    {
        Expression query = Customers
            .GroupBy(c => c.Region)
            .Take(5)
            .Executed(q => q.Any(g => g.Key == "EU"));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT EXISTS (
                    SELECT 1
                    FROM (
                        SELECT t0."Region"
                        FROM "Customer" AS t0
                        GROUP BY t0."Region"
                        LIMIT 5
                    ) AS t1
                    WHERE t1."Region" = 'EU'
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsKeysOfCombinedGroups()
    {
        Expression query = Customers
            .GroupBy(c => c.Region)
            .Concat(Customers.GroupBy(c => c.Name))
            .Select(g => g.Key)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t2."Region"
                FROM (
                    SELECT t0."Region"
                    FROM "Customer" AS t0
                    GROUP BY t0."Region"
                    UNION ALL
                    SELECT t1."Name"
                    FROM "Customer" AS t1
                    GROUP BY t1."Name"
                ) AS t2
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
