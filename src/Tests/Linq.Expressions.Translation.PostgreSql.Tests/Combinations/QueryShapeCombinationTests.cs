#pragma warning disable IDE0120 // the LINQ calls are the subject

namespace Linq.Expressions.Translation.PostgreSql.Tests.Combinations;

public sealed record QueryShapeCombinationTests
{
    [Fact]
    public void DropsOrderingWhenDistinctProjectionHidesKey()
    {
        Expression query = Customers
            .OrderBy(c => c.Id)
            .Select(c => c.Region)
            .Distinct()
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT DISTINCT t0."Region"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void KeepsOrderingWhenDistinctProjectionContainsKey()
    {
        Expression query = Customers
            .OrderBy(c => c.Region)
            .Select(c => new { c.Region })
            .Distinct()
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT DISTINCT t0."Region"
                FROM "Customer" AS t0
                ORDER BY t0."Region"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OrdersDistinctValuesInDerivedTable()
    {
        Expression query = Customers
            .Select(c => c.Region)
            .Distinct()
            .OrderBy(r => r)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Region"
                FROM (
                    SELECT DISTINCT t0."Region"
                    FROM "Customer" AS t0
                ) AS t1
                ORDER BY t1."Region"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ReordersTakenRowsKeepingPreviousOrderAsTieBreaker()
    {
        Expression query = Customers
            .OrderBy(c => c.Name)
            .Take(10)
            .OrderBy(c => c.Id)
            .Select(c => c.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Id"
                FROM (
                    SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                    FROM "Customer" AS t0
                    ORDER BY t0."Name"
                    LIMIT 10
                ) AS t1
                ORDER BY t1."Id", t1."Name"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void MakesSkippedRowsDistinct()
    {
        Expression query = Customers
            .OrderBy(c => c.Id)
            .Skip(2)
            .Select(c => c.Name)
            .Distinct()
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT DISTINCT t1."Name"
                FROM (
                    SELECT t0."Name"
                    FROM "Customer" AS t0
                    ORDER BY t0."Id"
                    OFFSET 2
                ) AS t1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesLastRowsOrderedByTwoKeys()
    {
        Expression query = Customers
            .OrderBy(c => c.Region)
            .ThenBy(c => c.Id)
            .TakeLast(3)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Id", t1."Name", t1."Email", t1."Region", t1."IsActive"
                FROM (
                    SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                    FROM "Customer" AS t0
                    ORDER BY t0."Region" DESC, t0."Id" DESC
                    LIMIT 3
                ) AS t1
                ORDER BY t1."Region", t1."Id"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ProjectsAfterTakingLastRows()
    {
        Expression query = Customers
            .OrderBy(c => c.Id)
            .TakeLast(2)
            .Select(c => c.Name)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Name"
                FROM (
                    SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                    FROM "Customer" AS t0
                    ORDER BY t0."Id" DESC
                    LIMIT 2
                ) AS t1
                ORDER BY t1."Id"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesLastRowsByHiddenOrderingKey()
    {
        Expression query = Customers
            .Where(c => c.IsActive)
            .OrderBy(c => c.Id)
            .Select(c => c.Name)
            .TakeLast(2)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Name"
                FROM (
                    SELECT t0."Name", t0."Id" AS "$0"
                    FROM "Customer" AS t0
                    WHERE t0."IsActive"
                    ORDER BY t0."Id" DESC
                    LIMIT 2
                ) AS t1
                ORDER BY t1."$0"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesLastFilteredRows()
    {
        Expression query = Customers
            .Where(c => c.IsActive)
            .OrderBy(c => c.Id)
            .TakeLast(2)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Id", t1."Name", t1."Email", t1."Region", t1."IsActive"
                FROM (
                    SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                    FROM "Customer" AS t0
                    WHERE t0."IsActive"
                    ORDER BY t0."Id" DESC
                    LIMIT 2
                ) AS t1
                ORDER BY t1."Id"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesLastRowsByNullableKey()
    {
        Expression query = Orders
            .OrderBy(o => o.Priority)
            .Select(o => new { o.Id, o.Priority })
            .TakeLast(2)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Id", t1."Priority"
                FROM (
                    SELECT t0."Id", t0."Priority"
                    FROM "Order" AS t0
                    ORDER BY t0."Priority" DESC NULLS LAST
                    LIMIT 2
                ) AS t1
                ORDER BY t1."Priority" NULLS FIRST
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SkipsLastFilteredRowsCountingTheSameFilter()
    {
        Expression query = Customers
            .Where(c => c.IsActive)
            .OrderBy(c => c.Id)
            .Select(c => c.Id)
            .SkipLast(1)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Customer" AS t0
                WHERE t0."IsActive"
                ORDER BY t0."Id"
                LIMIT (SELECT GREATEST(COUNT(*) - 1, 0) FROM "Customer" AS t1 WHERE t1."IsActive")
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesLastRowOfNullableOrdering()
    {
        Expression query = Orders
            .OrderBy(o => o.Priority)
            .Select(o => o.Id)
            .Executed(q => q.Last());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                ORDER BY t0."Priority" DESC NULLS LAST
                LIMIT 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ReversesTwoKeyOrdering()
    {
        Expression query = Customers
            .OrderBy(c => c.Region)
            .ThenByDescending(c => c.Id)
            .Reverse()
            .Select(c => c.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Customer" AS t0
                ORDER BY t0."Region" DESC, t0."Id"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SumsConcatenatedValues()
    {
        Expression query = Customers
            .Select(c => c.Id)
            .Concat(Products.Select(p => p.Id))
            .Executed(q => q.Sum());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COALESCE(SUM(t2."Id"), 0) AS integer)
                FROM (
                    SELECT t0."Id"
                    FROM "Customer" AS t0
                    UNION ALL SELECT t1."Id"
                    FROM "Product" AS t1
                ) AS t2
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OrdersAndTakesUnion()
    {
        Expression query = Customers
            .Select(c => c.Name)
            .Union(Products.Select(p => p.Name))
            .OrderBy(n => n)
            .Take(3)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t2."Name"
                FROM (
                    SELECT t0."Name"
                    FROM "Customer" AS t0
                    UNION SELECT t1."Name"
                    FROM "Product" AS t1
                ) AS t2
                ORDER BY t2."Name"
                LIMIT 3
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CountsExceptedIds()
    {
        Expression query = Customers
            .Select(c => c.Id)
            .Except(Orders.Select(o => o.CustomerId))
            .Executed(q => q.Count());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COUNT(*) AS integer)
                FROM (
                    SELECT t0."Id"
                    FROM "Customer" AS t0
                    EXCEPT SELECT t1."CustomerId"
                    FROM "Order" AS t1
                ) AS t2
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersIntersection()
    {
        Expression query = Customers
            .Select(c => c.Id)
            .Intersect(Orders.Select(o => o.CustomerId))
            .Where(id => id > 10)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t2."Id"
                FROM (
                    SELECT t0."Id"
                    FROM "Customer" AS t0
                    INTERSECT SELECT t1."CustomerId"
                    FROM "Order" AS t1
                ) AS t2
                WHERE t2."Id" > 10
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void UnitesFilteredOperands()
    {
        Expression query = Customers
            .Where(c => c.IsActive)
            .Select(c => c.Name)
            .Union(Products.Where(p => p.Price > 10).Select(p => p.Name))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name"
                FROM "Customer" AS t0
                WHERE t0."IsActive"
                UNION SELECT t1."Name"
                FROM "Product" AS t1
                WHERE t1."Price" > 10
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsThreeTables()
    {
        Expression query = Orders
            .Join(Customers, o => o.CustomerId, c => c.Id, (o, c) => new { o, c })
            .Join(
                OrderLines,
                x => x.o.Id,
                l => l.OrderId,
                (x, l) => new { x.c.Name, l.Quantity }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Name", t2."Quantity"
                FROM "Order" AS t0
                INNER JOIN "Customer" AS t1 ON t0."CustomerId" = t1."Id"
                INNER JOIN "OrderLine" AS t2 ON t0."Id" = t2."OrderId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsFilteredInnerSource()
    {
        Expression query = Orders
            .Join(
                Customers.Where(c => c.IsActive),
                o => o.CustomerId,
                c => c.Id,
                (o, c) => new { o.Id, c.Name }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t1."Name"
                FROM "Order" AS t0
                INNER JOIN "Customer" AS t1 ON t0."CustomerId" = t1."Id"
                WHERE t1."IsActive"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsPagedInnerSourceAsDerivedTable()
    {
        Expression query = Orders
            .Join(
                Customers.OrderBy(c => c.Id).Take(10),
                o => o.CustomerId,
                c => c.Id,
                (o, c) => new { o.Id, c.Name }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t2."Name"
                FROM "Order" AS t0
                INNER JOIN (
                    SELECT t1."Id", t1."Name", t1."Email", t1."Region", t1."IsActive"
                    FROM "Customer" AS t1
                    ORDER BY t1."Id"
                    LIMIT 10
                ) AS t2 ON t0."CustomerId" = t2."Id"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsOnCompositeKey()
    {
        Expression query = Orders
            .Join(
                OrderLines,
                o => new { o.Id, o.Quantity },
                l => new { Id = l.OrderId, l.Quantity },
                (o, l) => l.UnitPrice
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."UnitPrice"
                FROM "Order" AS t0
                INNER JOIN "OrderLine" AS t1 ON (t0."Id" = t1."OrderId") AND (t0."Quantity" = t1."Quantity")
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OrdersAndPagesJoinedRows()
    {
        Expression query = Orders
            .Join(Customers, o => o.CustomerId, c => c.Id, (o, c) => new { o.Id, c.Name })
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Id)
            .Skip(10)
            .Take(5)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t1."Name"
                FROM "Order" AS t0
                INNER JOIN "Customer" AS t1 ON t0."CustomerId" = t1."Id"
                ORDER BY t1."Name", t0."Id"
                LIMIT 5
                OFFSET 10
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void LeftJoinsFilteredInnerSourceInOnClause()
    {
        Expression query = (
            from c in Customers
            join o in Orders.Where(o => o.IsPaid) on c.Id equals o.CustomerId into os
            from o in os.DefaultIfEmpty()
            select new { c.Name, Total = (decimal?)o.Total }
        ).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t1."Total"
                FROM "Customer" AS t0
                LEFT JOIN "Order" AS t1 ON (t0."Id" = t1."CustomerId") AND t1."IsPaid"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersOuterRowsAfterLeftJoin()
    {
        Expression query = (
            from c in Customers
            join o in Orders on c.Id equals o.CustomerId into os
            from o in os.DefaultIfEmpty()
            where c.IsActive
            select new { c.Name, Total = (decimal?)o.Total }
        ).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t1."Total"
                FROM "Customer" AS t0
                LEFT JOIN "Order" AS t1 ON t0."Id" = t1."CustomerId"
                WHERE t0."IsActive"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AggregatesGroupJoinTwoWays()
    {
        Expression query = Customers
            .GroupJoin(
                Orders,
                c => c.Id,
                o => o.CustomerId,
                (c, os) =>
                    new
                    {
                        c.Name,
                        Count = os.Count(o => o.IsPaid),
                        Revenue = os.Sum(o => o.Total),
                    }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", (
                    SELECT CAST(COUNT(*) AS integer)
                    FROM "Order" AS t1
                    WHERE (t1."CustomerId" = t0."Id") AND t1."IsPaid"
                ) AS "Count", (
                    SELECT COALESCE(SUM(t2."Total"), 0)
                    FROM "Order" AS t2
                    WHERE t2."CustomerId" = t0."Id"
                ) AS "Revenue"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SumsFilteredGroupJoinRows()
    {
        Expression query = Customers
            .GroupJoin(
                Orders,
                c => c.Id,
                o => o.CustomerId,
                (c, os) =>
                    new { c.Name, Paid = os.Where(o => o.IsPaid).Sum(o => o.Total) }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", (
                    SELECT COALESCE(SUM(t1."Total"), 0)
                    FROM "Order" AS t1
                    WHERE (t1."CustomerId" = t0."Id") AND t1."IsPaid"
                ) AS "Paid"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesMaximumOfProjectedGroupJoinRows()
    {
        Expression query = Customers
            .GroupJoin(
                Orders,
                c => c.Id,
                o => o.CustomerId,
                (c, os) => new { c.Name, Last = os.Select(o => o.Id).Max() }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", (
                    SELECT MAX(t1."Id")
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                ) AS "Last"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ExcludesIdsFoundInNullableSubquery()
    {
        Expression query = Customers
            .Where(c => !Products.Select(p => p.CategoryId).Contains(c.Id))
            .Select(c => c.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Customer" AS t0
                WHERE NOT COALESCE(t0."Id" IN (SELECT t1."CategoryId" FROM "Product" AS t1), FALSE)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByMembershipInFilteredSubquery()
    {
        Expression query = Customers
            .Where(c =>
                Orders.Where(o => o.IsPaid).Select(o => o.CustomerId).Contains(c.Id)
            )
            .Select(c => c.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Customer" AS t0
                WHERE t0."Id" IN (SELECT t1."CustomerId" FROM "Order" AS t1 WHERE t1."IsPaid")
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ChecksNestedCorrelatedExistence()
    {
        Expression query = Customers
            .Where(c =>
                Orders.Any(o =>
                    o.CustomerId == c.Id
                    && OrderLines.Any(l => l.OrderId == o.Id && l.Quantity > 5)
                )
            )
            .Select(c => c.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Customer" AS t0
                WHERE EXISTS (
                    SELECT 1
                    FROM "Order" AS t1
                    WHERE (t1."CustomerId" = t0."Id") AND EXISTS (
                        SELECT 1
                        FROM "OrderLine" AS t2
                        WHERE (t2."OrderId" = t1."Id") AND (t2."Quantity" > 5)
                    )
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OrdersByCorrelatedCount()
    {
        Expression query = Customers
            .OrderByDescending(c => Orders.Count(o => o.CustomerId == c.Id))
            .Select(c => c.Name)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name"
                FROM "Customer" AS t0
                ORDER BY (
                    SELECT CAST(COUNT(*) AS integer)
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                ) DESC
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ProjectsLatestCorrelatedNumber()
    {
        Expression query = Customers
            .Select(c => new
            {
                c.Name,
                Latest = Orders
                    .Where(o => o.CustomerId == c.Id)
                    .OrderByDescending(o => o.CreatedAt)
                    .Select(o => o.Number)
                    .FirstOrDefault(),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", (
                    SELECT t1."Number"
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                    ORDER BY t1."CreatedAt" DESC
                    LIMIT 1
                ) AS "Latest"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CountsDistinctCorrelatedStatuses()
    {
        Expression query = Customers
            .Select(c => new
            {
                c.Name,
                Statuses = Orders
                    .Where(o => o.CustomerId == c.Id)
                    .Select(o => o.Status)
                    .Distinct()
                    .Count(),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", (
                    SELECT CAST(COUNT(*) AS integer)
                    FROM (
                        SELECT DISTINCT t1."Status"
                        FROM "Order" AS t1
                        WHERE t1."CustomerId" = t0."Id"
                    ) AS t2
                ) AS "Statuses"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ChecksAnyWithinTakenCorrelatedRows()
    {
        Expression query = Customers
            .Where(c =>
                Orders
                    .Where(o => o.CustomerId == c.Id)
                    .OrderBy(o => o.Id)
                    .Select(o => o.IsPaid)
                    .Take(3)
                    .Any(paid => paid)
            )
            .Select(c => c.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Customer" AS t0
                WHERE EXISTS (
                    SELECT 1
                    FROM (
                        SELECT t1."IsPaid"
                        FROM "Order" AS t1
                        WHERE t1."CustomerId" = t0."Id"
                        ORDER BY t1."Id"
                        LIMIT 3
                    ) AS t2
                    WHERE t2."IsPaid"
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersAboveUncorrelatedAverage()
    {
        Expression query = Orders
            .Where(o => o.Total > Orders.Average(x => x.Total))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Total" > (SELECT AVG(t1."Total") FROM "Order" AS t1)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesTotalWithCorrelatedSumOfLines()
    {
        Expression query = Orders
            .Where(o =>
                o.Total
                != OrderLines
                    .Where(l => l.OrderId == o.Id)
                    .Sum(l => l.UnitPrice * l.Quantity)
            )
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Total" <> (
                    SELECT COALESCE(SUM(t1."UnitPrice" * CAST(t1."Quantity" AS numeric)), 0)
                    FROM "OrderLine" AS t1
                    WHERE t1."OrderId" = t0."Id"
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ProjectsWhetherAllCorrelatedRowsMatch()
    {
        Expression query = Customers
            .Select(c => new
            {
                c.Name,
                AllPaid = Orders.Where(o => o.CustomerId == c.Id).All(o => o.IsPaid),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", NOT EXISTS (
                    SELECT 1
                    FROM "Order" AS t1
                    WHERE (t1."CustomerId" = t0."Id") AND NOT t1."IsPaid"
                ) AS "AllPaid"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
