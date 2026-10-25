namespace Linq.Expressions.Translation.PostgreSql.Tests.Operators;

public sealed record SubqueryTests
{
    [Fact]
    public void FiltersByExistingRelatedRows()
    {
        Expression query = Customers
            .Where(c => Orders.Any(o => o.CustomerId == c.Id))
            .Select(c => c.Name)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name"
                FROM "Customer" AS t0
                WHERE EXISTS (
                    SELECT 1
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByMissingRelatedRows()
    {
        Expression query = Customers
            .Where(c => !Orders.Any(o => o.CustomerId == c.Id))
            .Select(c => c.Name)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name"
                FROM "Customer" AS t0
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByRelatedRowsWithoutPredicate()
    {
        Expression query = Customers
            .Where(c => Orders.Where(o => o.CustomerId == c.Id).Any())
            .Select(c => c.Name)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name"
                FROM "Customer" AS t0
                WHERE EXISTS (
                    SELECT 1
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByRelatedRowsMatchingExtraPredicate()
    {
        Expression query = Customers
            .Where(c => Orders.Any(o => (o.CustomerId == c.Id) && o.IsPaid))
            .Select(c => c.Name)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name"
                FROM "Customer" AS t0
                WHERE EXISTS (
                    SELECT 1
                    FROM "Order" AS t1
                    WHERE (t1."CustomerId" = t0."Id") AND t1."IsPaid"
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByAllRelatedRowsMatching()
    {
        Expression query = Customers
            .Where(c => Orders.Where(o => o.CustomerId == c.Id).All(o => o.IsPaid))
            .Select(c => c.Name)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name"
                FROM "Customer" AS t0
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM "Order" AS t1
                    WHERE (t1."CustomerId" = t0."Id") AND NOT t1."IsPaid"
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CombinesExistsWithOtherPredicate()
    {
        Expression query = Customers
            .Where(c => c.IsActive && Orders.Any(o => o.CustomerId == c.Id))
            .Select(c => c.Name)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name"
                FROM "Customer" AS t0
                WHERE t0."IsActive" AND EXISTS (
                    SELECT 1
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CombinesTwoExistsWithDisjunction()
    {
        Expression query = Customers
            .Where(c =>
                Orders.Any(o => o.CustomerId == c.Id)
                || Employees.Any(e => e.Name == c.Name)
            )
            .Select(c => c.Name)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name"
                FROM "Customer" AS t0
                WHERE EXISTS (
                    SELECT 1
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                ) OR EXISTS (
                    SELECT 1
                    FROM "Employee" AS t2
                    WHERE t2."Name" = t0."Name"
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByNestedExists()
    {
        Expression query = Customers
            .Where(c =>
                Orders.Any(o =>
                    (o.CustomerId == c.Id) && OrderLines.Any(l => l.OrderId == o.Id)
                )
            )
            .Select(c => c.Name)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name"
                FROM "Customer" AS t0
                WHERE EXISTS (
                    SELECT 1
                    FROM "Order" AS t1
                    WHERE (t1."CustomerId" = t0."Id") AND EXISTS (
                        SELECT 1
                        FROM "OrderLine" AS t2
                        WHERE t2."OrderId" = t1."Id"
                    )
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByCorrelatedCount()
    {
        Expression query = Customers
            .Where(c => Orders.Count(o => o.CustomerId == c.Id) > 2)
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
                    WHERE t1."CustomerId" = t0."Id"
                ) > 2
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByCorrelatedSum()
    {
        Expression query = Customers
            .Where(c => Orders.Where(o => o.CustomerId == c.Id).Sum(o => o.Total) > 1000m)
            .Select(c => c.Name)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name"
                FROM "Customer" AS t0
                WHERE (
                    SELECT COALESCE(SUM(t1."Total"), 0)
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                ) > 1000
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByCorrelatedMaximumEqualToColumn()
    {
        Expression query = Orders
            .Where(o =>
                o.Total
                == Orders.Where(x => x.CustomerId == o.CustomerId).Max(x => x.Total)
            )
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Total" = (
                    SELECT MAX(t1."Total")
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."CustomerId"
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByUncorrelatedAverage()
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
                WHERE t0."Total" > (
                    SELECT AVG(t1."Total")
                    FROM "Order" AS t1
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByUncorrelatedMaximumOfFilteredRows()
    {
        Expression query = Orders
            .Where(o => o.Total >= Orders.Where(x => x.IsPaid).Max(x => x.Total))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Total" >= (
                    SELECT MAX(t1."Total")
                    FROM "Order" AS t1
                    WHERE t1."IsPaid"
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByDistinctCountOfRelatedRows()
    {
        Expression query = Customers
            .Where(c =>
                Orders
                    .Where(o => o.CustomerId == c.Id)
                    .Select(o => o.Status)
                    .Distinct()
                    .Count() > 1
            )
            .Select(c => c.Name)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name"
                FROM "Customer" AS t0
                WHERE (
                    SELECT CAST(COUNT(*) AS integer)
                    FROM (
                        SELECT DISTINCT t1."Status"
                        FROM "Order" AS t1
                        WHERE t1."CustomerId" = t0."Id"
                    ) AS t2
                ) > 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByMembershipInSubquery()
    {
        Expression query = Customers
            .Where(c => Orders.Select(o => o.CustomerId).Contains(c.Id))
            .Select(c => c.Name)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name"
                FROM "Customer" AS t0
                WHERE t0."Id" IN (
                    SELECT t1."CustomerId"
                    FROM "Order" AS t1
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByNonMembershipInSubquery()
    {
        Expression query = Customers
            .Where(c => !Orders.Select(o => o.CustomerId).Contains(c.Id))
            .Select(c => c.Name)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name"
                FROM "Customer" AS t0
                WHERE NOT (t0."Id" IN (
                    SELECT t1."CustomerId"
                    FROM "Order" AS t1
                ))
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByMembershipInFilteredSubquery()
    {
        Expression query = Orders
            .Where(o =>
                Customers.Where(c => c.IsActive).Select(c => c.Id).Contains(o.CustomerId)
            )
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."CustomerId" IN (
                    SELECT t1."Id"
                    FROM "Customer" AS t1
                    WHERE t1."IsActive"
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByMembershipInCapturedQuery()
    {
        IQueryable<int> active = Customers.Where(c => c.IsActive).Select(c => c.Id);

        Expression query = Orders
            .Where(o => active.Contains(o.CustomerId))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."CustomerId" IN (
                    SELECT t1."Id"
                    FROM "Customer" AS t1
                    WHERE t1."IsActive"
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByMembershipInPagedSubquery()
    {
        Expression query = Orders
            .Where(o =>
                Customers
                    .OrderBy(c => c.Id)
                    .Take(3)
                    .Select(c => c.Id)
                    .Contains(o.CustomerId)
            )
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."CustomerId" IN (
                    SELECT t1."Id"
                    FROM "Customer" AS t1
                    ORDER BY t1."Id"
                    LIMIT 3
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByMembershipInGroupedSubquery()
    {
        Expression query = Customers
            .Where(c =>
                Orders
                    .GroupBy(o => o.CustomerId)
                    .Where(g => g.Count() > 2)
                    .Select(g => g.Key)
                    .Contains(c.Id)
            )
            .Select(c => c.Name)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name"
                FROM "Customer" AS t0
                WHERE t0."Id" IN (
                    SELECT t1."CustomerId"
                    FROM "Order" AS t1
                    GROUP BY t1."CustomerId"
                    HAVING CAST(COUNT(*) AS integer) > 2
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersOrderLinesByMembershipInOrders()
    {
        Expression query = OrderLines
            .Where(l => Orders.Where(o => o.IsPaid).Select(o => o.Id).Contains(l.OrderId))
            .Select(l => l.ProductId)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."ProductId"
                FROM "OrderLine" AS t0
                WHERE t0."OrderId" IN (
                    SELECT t1."Id"
                    FROM "Order" AS t1
                    WHERE t1."IsPaid"
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersEmployeesByManagerCondition()
    {
        Expression query = Employees
            .Where(e =>
                Employees.Any(m => (m.Id == e.ManagerId) && (m.ManagerId == null))
            )
            .Select(e => e.Name)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name"
                FROM "Employee" AS t0
                WHERE EXISTS (
                    SELECT 1
                    FROM "Employee" AS t1
                    WHERE (t1."Id" = t0."ManagerId") AND (t1."ManagerId" IS NULL)
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsCorrelatedCount()
    {
        Expression query = Customers
            .Select(c => new { c.Name, Orders = Orders.Count(o => o.CustomerId == c.Id) })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", (
                    SELECT CAST(COUNT(*) AS integer)
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                ) AS "Orders"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsCorrelatedSum()
    {
        Expression query = Customers
            .Select(c => new
            {
                c.Name,
                Revenue = Orders.Where(o => o.CustomerId == c.Id).Sum(o => o.Total),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", (
                    SELECT COALESCE(SUM(t1."Total"), 0)
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                ) AS "Revenue"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsCorrelatedMaximum()
    {
        Expression query = Customers
            .Select(c => new
            {
                c.Name,
                Largest = Orders
                    .Where(o => o.CustomerId == c.Id)
                    .Max(o => (decimal?)o.Total),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", (
                    SELECT MAX(t1."Total")
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                ) AS "Largest"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsCorrelatedExistence()
    {
        Expression query = Customers
            .Select(c => new
            {
                c.Name,
                HasOrders = Orders.Any(o => o.CustomerId == c.Id),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", EXISTS (
                    SELECT 1
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                ) AS "HasOrders"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsFirstValueOfCorrelatedRows()
    {
        Expression query = Customers
            .Select(c => new
            {
                c.Name,
                LastOrder = Orders
                    .Where(o => o.CustomerId == c.Id)
                    .OrderByDescending(o => o.CreatedAt)
                    .Select(o => (int?)o.Id)
                    .FirstOrDefault(),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", (
                    SELECT t1."Id"
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                    ORDER BY t1."CreatedAt" DESC
                    LIMIT 1
                ) AS "LastOrder"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsSeveralCorrelatedValues()
    {
        Expression query = Customers
            .Select(c => new
            {
                c.Name,
                Orders = Orders.Count(o => o.CustomerId == c.Id),
                Lines = OrderLines.Count(l => l.OrderId == c.Id),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", (
                    SELECT CAST(COUNT(*) AS integer)
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                ) AS "Orders", (
                    SELECT CAST(COUNT(*) AS integer)
                    FROM "OrderLine" AS t2
                    WHERE t2."OrderId" = t0."Id"
                ) AS "Lines"
                FROM "Customer" AS t0
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
    public void GroupsBySubqueryFreeKeyAfterCorrelatedFilter()
    {
        Expression query = Customers
            .Where(c => Orders.Any(o => o.CustomerId == c.Id))
            .GroupBy(c => c.Region)
            .Select(g => new { g.Key, Count = g.Count() })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Region" AS "Key", CAST(COUNT(*) AS integer) AS "Count"
                FROM "Customer" AS t0
                WHERE EXISTS (
                    SELECT 1
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                )
                GROUP BY t0."Region"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsAggregatedSubquery()
    {
        Expression query = Customers
            .Join(
                Orders
                    .GroupBy(o => o.CustomerId)
                    .Select(g => new
                    {
                        CustomerId = g.Key,
                        Revenue = g.Sum(o => o.Total),
                    }),
                c => c.Id,
                x => x.CustomerId,
                (c, x) => new { c.Name, x.Revenue }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t2."Revenue"
                FROM "Customer" AS t0
                INNER JOIN (
                    SELECT t1."CustomerId", COALESCE(SUM(t1."Total"), 0) AS "Revenue"
                    FROM "Order" AS t1
                    GROUP BY t1."CustomerId"
                ) AS t2 ON t0."Id" = t2."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void LeftJoinsAggregatedSubquery()
    {
        Expression query = Customers
            .GroupJoin(
                Orders
                    .GroupBy(o => o.CustomerId)
                    .Select(g => new
                    {
                        CustomerId = g.Key,
                        Revenue = g.Sum(o => o.Total),
                    }),
                c => c.Id,
                x => x.CustomerId,
                (c, xs) => new { c, xs }
            )
            .SelectMany(
                y => y.xs.DefaultIfEmpty(),
                (y, x) => new { y.c.Name, Revenue = (decimal?)x!.Revenue }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t2."Revenue"
                FROM "Customer" AS t0
                LEFT JOIN (
                    SELECT t1."CustomerId", COALESCE(SUM(t1."Total"), 0) AS "Revenue"
                    FROM "Order" AS t1
                    GROUP BY t1."CustomerId"
                ) AS t2 ON t0."Id" = t2."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersJoinedSubqueryResult()
    {
        Expression query = Customers
            .Join(
                Orders
                    .GroupBy(o => o.CustomerId)
                    .Select(g => new
                    {
                        CustomerId = g.Key,
                        Revenue = g.Sum(o => o.Total),
                    }),
                c => c.Id,
                x => x.CustomerId,
                (c, x) => new { c.Name, x.Revenue }
            )
            .Where(y => y.Revenue > 500m)
            .OrderByDescending(y => y.Revenue)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t2."Revenue"
                FROM "Customer" AS t0
                INNER JOIN (
                    SELECT t1."CustomerId", COALESCE(SUM(t1."Total"), 0) AS "Revenue"
                    FROM "Order" AS t1
                    GROUP BY t1."CustomerId"
                ) AS t2 ON t0."Id" = t2."CustomerId"
                WHERE t2."Revenue" > 500
                ORDER BY t2."Revenue" DESC
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsFromFilteredPagedSubquery()
    {
        Expression query = Customers
            .Where(c => c.IsActive)
            .OrderBy(c => c.Id)
            .Take(10)
            .Where(c => c.Region == "EU")
            .Select(c => c.Name)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Name"
                FROM (
                    SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                    FROM "Customer" AS t0
                    WHERE t0."IsActive"
                    ORDER BY t0."Id"
                    LIMIT 10
                ) AS t1
                WHERE t1."Region" = 'EU'
                ORDER BY t1."Id"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AveragesCorrelatedCounts()
    {
        Expression query = Customers
            .Select(c => Orders.Count(o => o.CustomerId == c.Id))
            .Executed(q => q.Average());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT AVG(CAST((
                    SELECT CAST(COUNT(*) AS integer)
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                ) AS double precision))
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CountsRowsWithRelatedRows()
    {
        Expression query = Customers.Executed(q =>
            q.Count(c => Orders.Any(o => o.CustomerId == c.Id))
        );

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COUNT(*) AS integer)
                FROM "Customer" AS t0
                WHERE EXISTS (
                    SELECT 1
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ChecksAnyRowWithRelatedRows()
    {
        Expression query = Customers.Executed(q =>
            q.Any(c => Orders.Any(o => o.CustomerId == c.Id))
        );

        Assert.Equal(
            Sql.Valid(
                """
                SELECT EXISTS (
                    SELECT 1
                    FROM "Customer" AS t0
                    WHERE EXISTS (
                        SELECT 1
                        FROM "Order" AS t1
                        WHERE t1."CustomerId" = t0."Id"
                    )
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesTextOfFirstOrDefaultNullSafe()
    {
        Expression query = Customers
            .Select(c =>
                Orders
                    .Where(o => o.CustomerId == c.Id)
                    .Select(o => o.Number)
                    .FirstOrDefault()
            )
            .Where(n => n != "a")
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT (
                    SELECT t1."Number"
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                    LIMIT 1
                )
                FROM "Customer" AS t0
                WHERE (
                    SELECT t1."Number"
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                    LIMIT 1
                ) IS DISTINCT FROM 'a'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesNullableValueOfFirstOrDefaultNullSafe()
    {
        Expression query = Customers
            .Select(c =>
                Orders
                    .Where(o => o.CustomerId == c.Id)
                    .Select(o => o.Discount)
                    .FirstOrDefault()
            )
            .Where(d => d != 1)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT (
                    SELECT t1."Discount"
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                    LIMIT 1
                )
                FROM "Customer" AS t0
                WHERE (
                    SELECT t1."Discount"
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                    LIMIT 1
                ) IS DISTINCT FROM 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesTextMaximumOfSubqueryNullSafe()
    {
        Expression query = Customers
            .Select(c => Orders.Where(o => o.CustomerId == c.Id).Max(o => o.Number))
            .Where(n => n != "a")
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT (
                    SELECT MAX(t1."Number")
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                )
                FROM "Customer" AS t0
                WHERE (
                    SELECT MAX(t1."Number")
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                ) IS DISTINCT FROM 'a'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesValueMaximumOfSubqueryDirectly()
    {
        Expression query = Customers
            .Select(c => Orders.Where(o => o.CustomerId == c.Id).Max(o => o.Total))
            .Where(t => t != 1)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT (
                    SELECT MAX(t1."Total")
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                )
                FROM "Customer" AS t0
                WHERE (
                    SELECT MAX(t1."Total")
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                ) <> 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ConcatenatesTextOfFirstOrDefaultAsEmpty()
    {
        Expression query = Customers
            .Select(c =>
                c.Name
                + Orders
                    .Where(o => o.CustomerId == c.Id)
                    .Select(o => o.Number)
                    .FirstOrDefault()
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name" || COALESCE((
                    SELECT t1."Number"
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                    LIMIT 1
                ), '')
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OrdersNullsFirstByTextOfFirstOrDefault()
    {
        Expression query = Customers
            .OrderBy(c =>
                Orders
                    .Where(o => o.CustomerId == c.Id)
                    .Select(o => o.Number)
                    .FirstOrDefault()
            )
            .Select(c => c.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Customer" AS t0
                ORDER BY (
                    SELECT t1."Number"
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                    LIMIT 1
                ) NULLS FIRST
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
