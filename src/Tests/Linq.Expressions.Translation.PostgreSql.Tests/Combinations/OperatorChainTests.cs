namespace Linq.Expressions.Translation.PostgreSql.Tests.Combinations;

public sealed record OperatorChainTests
{
    [Fact]
    public void FiltersOrdersPagesAndProjects()
    {
        Expression query = Customers
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Skip(10)
            .Take(5)
            .Select(c => c.Name)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name"
                FROM "Customer" AS t0
                WHERE t0."IsActive"
                ORDER BY t0."Name"
                LIMIT 5 OFFSET 10
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void ProjectsFiltersAndOrdersByComputedMember()
    {
        Expression query = Customers
            .Select(c => new { c.Id, Upper = c.Name.ToUpper() })
            .Where(x => x.Upper != "X")
            .OrderBy(x => x.Upper)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", UPPER(t0."Name") AS "Upper"
                FROM "Customer" AS t0
                WHERE UPPER(t0."Name") <> 'X'
                ORDER BY UPPER(t0."Name")
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OrdersFiltersAndProjects()
    {
        Expression query = Customers
            .OrderBy(c => c.Name)
            .Where(c => c.IsActive)
            .Select(c => c.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Customer" AS t0
                WHERE t0."IsActive"
                ORDER BY t0."Name"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersAcrossSeveralSelectsInOneLevel()
    {
        Expression query = Customers
            .Where(c => c.Id > 1)
            .Select(c => new { c.Id, c.Name })
            .Where(x => x.Id < 9)
            .Select(x => new { x.Name })
            .Where(y => y.Name != "x")
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name"
                FROM "Customer" AS t0
                WHERE (t0."Id" > 1) AND (t0."Id" < 9) AND (t0."Name" <> 'x')
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OrdersDistinctRowsAndTakes()
    {
        Expression query = Customers
            .Select(c => c.Region)
            .Distinct()
            .OrderBy(r => r)
            .Take(3)
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
                LIMIT 3
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void DistinctAfterTakeWrapsTakenRows()
    {
        Expression query = Customers.Select(c => c.Region).Take(5).Distinct().Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT DISTINCT t1."Region"
                FROM (
                    SELECT t0."Region"
                    FROM "Customer" AS t0
                    LIMIT 5
                ) AS t1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersOrdersAndTakesAfterTake()
    {
        Expression query = Customers
            .OrderBy(c => c.Id)
            .Take(10)
            .Where(c => c.IsActive)
            .OrderByDescending(c => c.Name)
            .Take(3)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Id", t1."Name", t1."Email", t1."Region", t1."IsActive"
                FROM (
                    SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                    FROM "Customer" AS t0
                    ORDER BY t0."Id"
                    LIMIT 10
                ) AS t1
                WHERE t1."IsActive"
                ORDER BY t1."Name" DESC, t1."Id"
                LIMIT 3
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void WrapsTwiceWhenFilteringAfterEachTake()
    {
        Expression query = Customers
            .OrderBy(c => c.Id)
            .Take(10)
            .Where(c => c.IsActive)
            .OrderByDescending(c => c.Name)
            .Take(3)
            .Where(c => c.Region == "EU")
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t2."Id", t2."Name", t2."Email", t2."Region", t2."IsActive"
                FROM (
                    SELECT t1."Id", t1."Name", t1."Email", t1."Region", t1."IsActive"
                    FROM (
                        SELECT t0."Id", t0."Name", t0."Email", t0."Region",
                        t0."IsActive"
                        FROM "Customer" AS t0
                        ORDER BY t0."Id"
                        LIMIT 10
                    ) AS t1
                    WHERE t1."IsActive"
                    ORDER BY t1."Name" DESC, t1."Id"
                    LIMIT 3
                ) AS t2
                WHERE t2."Region" = 'EU'
                ORDER BY t2."Name" DESC, t2."Id"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SkipsAfterFilterThenFilters()
    {
        Expression query = Customers
            .Where(c => c.IsActive)
            .OrderBy(c => c.Id)
            .Skip(5)
            .Where(c => c.Region == "EU")
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Id", t1."Name", t1."Email", t1."Region", t1."IsActive"
                FROM (
                    SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                    FROM "Customer" AS t0
                    WHERE t0."IsActive"
                    ORDER BY t0."Id"
                    OFFSET 5
                ) AS t1
                WHERE t1."Region" = 'EU'
                ORDER BY t1."Id"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void PagesFilteredPagedRows()
    {
        Expression query = Customers
            .OrderBy(c => c.Id)
            .Take(20)
            .Where(c => c.IsActive)
            .Skip(2)
            .Take(5)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Id", t1."Name", t1."Email", t1."Region", t1."IsActive"
                FROM (
                    SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                    FROM "Customer" AS t0
                    ORDER BY t0."Id"
                    LIMIT 20
                ) AS t1
                WHERE t1."IsActive"
                ORDER BY t1."Id"
                LIMIT 5 OFFSET 2
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ProjectsAfterPagedFilter()
    {
        Expression query = Customers
            .OrderBy(c => c.Id)
            .Take(20)
            .Where(c => c.IsActive)
            .Select(c => c.Name)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Name"
                FROM (
                    SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                    FROM "Customer" AS t0
                    ORDER BY t0."Id"
                    LIMIT 20
                ) AS t1
                WHERE t1."IsActive"
                ORDER BY t1."Id"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FiltersUnionThenOrdersAndTakes()
    {
        Expression query = Customers
            .Select(c => c.Name)
            .Union(Products.Select(p => p.Name))
            .Where(n => n != "x")
            .OrderBy(n => n)
            .Take(5)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t2."Name"
                FROM (
                    SELECT t0."Name"
                    FROM "Customer" AS t0
                    UNION
                    SELECT t1."Name"
                    FROM "Product" AS t1
                ) AS t2
                WHERE t2."Name" <> 'x'
                ORDER BY t2."Name"
                LIMIT 5
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void GroupsConcatenatedRows()
    {
        Expression query = Orders
            .Select(o => o.CustomerId)
            .Concat(OrderLines.Select(l => l.OrderId))
            .GroupBy(id => id)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t2."CustomerId" AS "Id", CAST(COUNT(*) AS integer) AS "Count"
                FROM (
                    SELECT t0."CustomerId"
                    FROM "Order" AS t0
                    UNION ALL
                    SELECT t1."OrderId"
                    FROM "OrderLine" AS t1
                ) AS t2
                GROUP BY t2."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FiltersExistsOrdersAndTakes()
    {
        Expression query = Customers
            .Where(c => Orders.Any(o => o.CustomerId == c.Id))
            .OrderBy(c => c.Name)
            .Take(5)
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
                ORDER BY t0."Name"
                LIMIT 5
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CastsFiltersAndProjects()
    {
        Expression query = Customers
            .Cast<INamed>()
            .Where(n => n.Name != "x")
            .OrderBy(n => n.Name)
            .Select(n => n.Name)
            .Take(2)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name"
                FROM "Customer" AS t0
                WHERE t0."Name" <> 'x'
                ORDER BY t0."Name"
                LIMIT 2
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ReversesFilteredPagedRows()
    {
        Expression query = Customers
            .OrderBy(c => c.Id)
            .Where(c => c.IsActive)
            .Reverse()
            .Take(3)
            .Select(c => c.Name)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name"
                FROM "Customer" AS t0
                WHERE t0."IsActive"
                ORDER BY t0."Id" DESC
                LIMIT 3
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FiltersProjectsAndOrdersAcrossJoin()
    {
        Expression query = Customers
            .Where(c => c.IsActive)
            .Join(
                Orders.Where(o => o.IsPaid),
                c => c.Id,
                o => o.CustomerId,
                (c, o) => new { c.Name, o.Total }
            )
            .Where(x => x.Total > 10m)
            .OrderByDescending(x => x.Total)
            .Skip(1)
            .Take(2)
            .Select(x => x.Name)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name"
                FROM "Customer" AS t0
                INNER JOIN "Order" AS t1 ON t0."Id" = t1."CustomerId"
                WHERE t0."IsActive" AND t1."IsPaid" AND (t1."Total" > 10)
                ORDER BY t1."Total" DESC
                LIMIT 2 OFFSET 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void CountsPagedFilteredRows()
    {
        Expression query = Customers
            .Where(c => c.IsActive)
            .OrderBy(c => c.Id)
            .Skip(5)
            .Take(10)
            .Executed(q => q.Count());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COUNT(*) AS integer)
                FROM (
                    SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                    FROM "Customer" AS t0
                    WHERE t0."IsActive"
                    ORDER BY t0."Id"
                    LIMIT 10 OFFSET 5
                ) AS t1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void SumsPagedProjection()
    {
        Expression query = Orders
            .OrderBy(o => o.Id)
            .Take(5)
            .Select(o => o.Total)
            .Executed(q => q.Sum());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(SUM(t1."Total"), 0)
                FROM (
                    SELECT t0."Total"
                    FROM "Order" AS t0
                    ORDER BY t0."Id"
                    LIMIT 5
                ) AS t1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void TakesFirstOfFilteredDistinctOrderedRows()
    {
        Expression query = Customers
            .Where(c => c.IsActive)
            .Select(c => c.Region)
            .Distinct()
            .OrderBy(r => r)
            .Executed(q => q.First());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Region"
                FROM (
                    SELECT DISTINCT t0."Region"
                    FROM "Customer" AS t0
                    WHERE t0."IsActive"
                ) AS t1
                ORDER BY t1."Region"
                LIMIT 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void ChecksAnyAfterLongChain()
    {
        Expression query = Customers
            .Where(c => c.IsActive)
            .Select(c => new { c.Id, c.Region })
            .Where(x => x.Region == "EU")
            .Executed(q => q.Any(x => x.Id > 100));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT EXISTS (
                    SELECT 1
                    FROM "Customer" AS t0
                    WHERE t0."IsActive" AND (t0."Region" = 'EU') AND (t0."Id" > 100)
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void TakesLastAfterFilterAndProjection()
    {
        Expression query = Customers
            .OrderBy(c => c.Id)
            .Where(c => c.IsActive)
            .Select(c => new { c.Id, c.Name })
            .Executed(q => q.Last());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name"
                FROM "Customer" AS t0
                WHERE t0."IsActive"
                ORDER BY t0."Id" DESC
                LIMIT 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
