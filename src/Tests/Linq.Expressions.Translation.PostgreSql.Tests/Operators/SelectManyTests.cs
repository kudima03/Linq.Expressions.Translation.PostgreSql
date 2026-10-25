namespace Linq.Expressions.Translation.PostgreSql.Tests.Operators;

public sealed record SelectManyTests
{
    [Fact]
    public void CrossJoinsUncorrelatedSource()
    {
        Expression query = Customers
            .SelectMany(c => Orders, (c, o) => new { c.Name, OrderId = o.Id })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t1."Id" AS "OrderId"
                FROM "Customer" AS t0
                CROSS JOIN "Order" AS t1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CrossJoinsWithQuerySyntax()
    {
        Expression query = (
            from c in Customers
            from o in Orders
            select new { c.Name, OrderId = o.Id }
        ).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t1."Id" AS "OrderId"
                FROM "Customer" AS t0
                CROSS JOIN "Order" AS t1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersCrossJoinWithQuerySyntax()
    {
        Expression query = (
            from c in Customers
            from o in Orders
            where o.CustomerId == c.Id
            select new { c.Name, OrderId = o.Id }
        ).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t1."Id" AS "OrderId"
                FROM "Customer" AS t0
                CROSS JOIN "Order" AS t1
                WHERE t1."CustomerId" = t0."Id"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void InnerJoinsSourceCorrelatedByWhere()
    {
        Expression query = Customers
            .SelectMany(
                c => Orders.Where(o => o.CustomerId == c.Id),
                (c, o) => new { c.Name, o.Total }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t1."Total"
                FROM "Customer" AS t0
                INNER JOIN "Order" AS t1 ON t1."CustomerId" = t0."Id"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void InnerJoinsCorrelatedSourceWithExtraPredicate()
    {
        Expression query = Customers
            .SelectMany(
                c => Orders.Where(o => (o.CustomerId == c.Id) && o.IsPaid),
                (c, o) => new { c.Name, o.Total }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t1."Total"
                FROM "Customer" AS t0
                INNER JOIN "Order" AS t1 ON (t1."CustomerId" = t0."Id") AND t1."IsPaid"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void InnerJoinsCorrelatedSourceWithSeparateWhereClauses()
    {
        Expression query = Customers
            .SelectMany(
                c => Orders.Where(o => o.CustomerId == c.Id).Where(o => o.IsPaid),
                (c, o) => new { c.Name, o.Total }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t1."Total"
                FROM "Customer" AS t0
                INNER JOIN "Order" AS t1 ON (t1."CustomerId" = t0."Id") AND t1."IsPaid"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FlattensCorrelatedSourceWithoutResultSelector()
    {
        Expression query = Customers
            .SelectMany(c => Orders.Where(o => o.CustomerId == c.Id))
            .Select(o => o.Total)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Total"
                FROM "Customer" AS t0
                INNER JOIN "Order" AS t1 ON t1."CustomerId" = t0."Id"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void LeftJoinsCorrelatedSourceWithDefaultIfEmpty()
    {
        Expression query = Customers
            .SelectMany(
                c => Orders.Where(o => o.CustomerId == c.Id).DefaultIfEmpty(),
                (c, o) => new { c.Name, Total = (decimal?)o!.Total }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t1."Total"
                FROM "Customer" AS t0
                LEFT JOIN "Order" AS t1 ON t1."CustomerId" = t0."Id"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void LeftJoinsCorrelatedSourceWithQuerySyntax()
    {
        Expression query = (
            from c in Customers
            from o in Orders.Where(o => o.CustomerId == c.Id).DefaultIfEmpty()
            select new { c.Name, Total = (decimal?)o.Total }
        ).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t1."Total"
                FROM "Customer" AS t0
                LEFT JOIN "Order" AS t1 ON t1."CustomerId" = t0."Id"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void LateralJoinsCorrelatedSourceWithTake()
    {
        Expression query = Customers
            .SelectMany(
                c =>
                    Orders
                        .Where(o => o.CustomerId == c.Id)
                        .OrderByDescending(o => o.Total)
                        .Take(1)
                        .Select(o => new { o.Total }),
                (c, o) => new { c.Name, o.Total }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t2."Total"
                FROM "Customer" AS t0
                CROSS JOIN LATERAL (
                    SELECT t1."Total"
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                    ORDER BY t1."Total" DESC
                    LIMIT 1
                ) AS t2
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void LeftJoinsLateralCorrelatedSourceWithTake()
    {
        Expression query = Customers
            .SelectMany(
                c =>
                    Orders
                        .Where(o => o.CustomerId == c.Id)
                        .OrderByDescending(o => o.Total)
                        .Take(1)
                        .Select(o => new { o.Total })
                        .DefaultIfEmpty(),
                (c, o) => new { c.Name, Total = (decimal?)o!.Total }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t2."Total"
                FROM "Customer" AS t0
                LEFT JOIN LATERAL (
                    SELECT t1."Total"
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                    ORDER BY t1."Total" DESC
                    LIMIT 1
                ) AS t2 ON TRUE
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CrossJoinsThreeSources()
    {
        Expression query = (
            from c in Customers
            from o in Orders
            from p in Products
            select new
            {
                c.Name,
                o.Number,
                ProductName = p.Name,
            }
        ).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t1."Number", t2."Name" AS "ProductName"
                FROM "Customer" AS t0
                CROSS JOIN "Order" AS t1
                CROSS JOIN "Product" AS t2
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CrossJoinsFilteredSources()
    {
        Expression query = Customers
            .Where(c => c.IsActive)
            .SelectMany(
                c => Products.Where(p => p.Price > 10),
                (c, p) => new { c.Name, ProductName = p.Name }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t1."Name" AS "ProductName"
                FROM "Customer" AS t0
                CROSS JOIN "Product" AS t1
                WHERE t0."IsActive" AND (t1."Price" > 10)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OrdersAndPagesAfterSelectMany()
    {
        Expression query = Customers
            .SelectMany(
                c => Orders.Where(o => o.CustomerId == c.Id),
                (c, o) => new { c.Name, o.Total }
            )
            .OrderBy(x => x.Name)
            .Skip(5)
            .Take(5)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t1."Total"
                FROM "Customer" AS t0
                INNER JOIN "Order" AS t1 ON t1."CustomerId" = t0."Id"
                ORDER BY t0."Name"
                LIMIT 5 OFFSET 5
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsEveryColumnOfFlattenedEntity()
    {
        Expression query = Customers
            .SelectMany(c => Products.Where(p => p.CategoryId == c.Id))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Id", t1."Name", t1."Price", t1."CategoryId"
                FROM "Customer" AS t0
                INNER JOIN "Product" AS t1 ON t1."CategoryId" = t0."Id"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsLateralPagedProjectionWithoutHiddenOrderingKey()
    {
        Expression query = Customers
            .SelectMany(
                c =>
                    Orders
                        .Where(o => o.CustomerId == c.Id)
                        .OrderByDescending(o => o.CreatedAt)
                        .Take(1)
                        .Select(o => o.Total),
                (c, t) => new { c.Name, Total = t }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t2."Total"
                FROM "Customer" AS t0
                CROSS JOIN LATERAL (
                    SELECT t1."Total"
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                    ORDER BY t1."CreatedAt" DESC
                    LIMIT 1
                ) AS t2
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
