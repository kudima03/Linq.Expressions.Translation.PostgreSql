namespace Linq.Expressions.Translation.PostgreSql.Tests.Combinations;

public sealed record SetOperationAndLeftJoinCombinationTests
{
    [Fact]
    public void EscapesWildcardInsideEscapedStringPattern()
    {
        string fragment = "x_\n";
        Expression query = Orders
            .Where(o => o.Number.Contains(fragment))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Number" LIKE E'%x\\_\n%'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void MatchesEveryTextForEmptyPrefix()
    {
        string prefix = "";
        Expression query = Orders
            .Where(o => o.Number.StartsWith(prefix))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Number" LIKE '%'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ParenthesizesUnionBeforeConcatenation()
    {
        Expression query = Customers
            .Select(c => c.Name)
            .Union(Products.Select(p => p.Name))
            .Concat(Employees.Select(e => e.Name))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                (SELECT t0."Name" FROM "Customer" AS t0 UNION SELECT t1."Name" FROM "Product" AS t1)
                UNION ALL SELECT t2."Name"
                FROM "Employee" AS t2
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ParenthesizesUnionOnRightOfExcept()
    {
        Expression query = Customers
            .Select(c => c.Id)
            .Except(Orders.Select(o => o.CustomerId).Union(Employees.Select(e => e.Id)))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Customer" AS t0
                EXCEPT (SELECT t1."CustomerId" FROM "Order" AS t1 UNION SELECT t2."Id" FROM "Employee" AS t2)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByMembershipInUnion()
    {
        Expression query = Customers
            .Where(c =>
                Orders
                    .Select(o => o.CustomerId)
                    .Union(Employees.Select(e => e.Id))
                    .Contains(c.Id)
            )
            .Select(c => c.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Customer" AS t0
                WHERE t0."Id" IN (
                    SELECT t3."CustomerId"
                    FROM (
                        SELECT t1."CustomerId"
                        FROM "Order" AS t1
                        UNION SELECT t2."Id"
                        FROM "Employee" AS t2
                    ) AS t3
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ChecksUnionHasAnyRow()
    {
        Expression query = Customers
            .Select(c => c.Id)
            .Union(Products.Select(p => p.Id))
            .Executed(q => q.Any());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT EXISTS (
                    SELECT 1
                    FROM (
                        SELECT t0."Id"
                        FROM "Customer" AS t0
                        UNION SELECT t1."Id"
                        FROM "Product" AS t1
                    ) AS t2
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void UnitesFilteredEntitiesBeforeProjecting()
    {
        Expression query = Customers
            .Where(c => c.IsActive)
            .Union(Customers.Where(c => c.Region == "EU"))
            .Select(c => c.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t2."Id"
                FROM (
                    SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                    FROM "Customer" AS t0
                    WHERE t0."IsActive"
                    UNION SELECT t1."Id", t1."Name", t1."Email", t1."Region", t1."IsActive"
                    FROM "Customer" AS t1
                    WHERE t1."Region" = 'EU'
                ) AS t2
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ConcatenatesPagedOperand()
    {
        Expression query = Customers
            .OrderBy(c => c.Id)
            .Select(c => c.Name)
            .Take(2)
            .Concat(Products.Select(p => p.Name))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                (SELECT t0."Name" FROM "Customer" AS t0 ORDER BY t0."Id" LIMIT 2)
                UNION ALL SELECT t1."Name"
                FROM "Product" AS t1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesFirstOfOrderedUnion()
    {
        Expression query = Customers
            .Select(c => c.Name)
            .Union(Products.Select(p => p.Name))
            .OrderBy(n => n)
            .Executed(q => q.First());

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
                LIMIT 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CountsLeftJoinedRowsThroughNullableInnerKey()
    {
        Expression query = (
            from c in Customers
            join o in Orders on c.Id equals o.CustomerId into os
            from o in os.DefaultIfEmpty()
            select new
            {
                c.Name,
                OrderId = (int?)o.Id,
                Total = (decimal?)o.Total,
            }
        )
            .GroupBy(x => x.Name)
            .Select(g => new
            {
                g.Key,
                Orders = g.Count(x => x.OrderId != null),
                Revenue = g.Sum(x => x.Total ?? 0),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name" AS "Key", CAST(COUNT(*) FILTER (WHERE t1."Id" IS NOT NULL) AS integer) AS "Orders",
                COALESCE(SUM(COALESCE(t1."Total", 0)), 0) AS "Revenue"
                FROM "Customer" AS t0
                LEFT JOIN "Order" AS t1 ON t0."Id" = t1."CustomerId"
                GROUP BY t0."Name"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersRowsOfCorrelatedSelectMany()
    {
        Expression query = Customers
            .SelectMany(
                c => Orders.Where(o => o.CustomerId == c.Id),
                (c, o) => new { c.Name, o.Total }
            )
            .Where(x => x.Total > 100)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t1."Total"
                FROM "Customer" AS t0
                INNER JOIN "Order" AS t1 ON t1."CustomerId" = t0."Id"
                WHERE t1."Total" > 100
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
