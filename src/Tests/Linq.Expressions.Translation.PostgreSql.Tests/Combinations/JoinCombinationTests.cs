namespace Linq.Expressions.Translation.PostgreSql.Tests.Combinations;

public sealed record JoinCombinationTests
{
    [Fact]
    public void JoinsFiveTables()
    {
        Expression query = Customers
            .Join(
                Orders,
                c => c.Id,
                o => o.CustomerId,
                (c, o) => new { c.Name, OrderId = o.Id }
            )
            .Join(
                OrderLines,
                x => x.OrderId,
                l => l.OrderId,
                (x, l) => new { x.Name, l.ProductId }
            )
            .Join(
                Products,
                x => x.ProductId,
                p => p.Id,
                (x, p) =>
                    new
                    {
                        x.Name,
                        Product = p.Name,
                        p.CategoryId,
                    }
            )
            .Join(
                Products,
                x => x.CategoryId,
                g => g.Id,
                (x, g) =>
                    new
                    {
                        x.Name,
                        x.Product,
                        Category = g.Name,
                    }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t3."Name" AS "Product", t4."Name" AS "Category"
                FROM "Customer" AS t0
                INNER JOIN "Order" AS t1 ON t0."Id" = t1."CustomerId"
                INNER JOIN "OrderLine" AS t2 ON t1."Id" = t2."OrderId"
                INNER JOIN "Product" AS t3 ON t2."ProductId" = t3."Id"
                INNER JOIN "Product" AS t4 ON t3."CategoryId" = t4."Id"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsFourTablesWithFiltersOnEachSource()
    {
        Expression query = Customers
            .Where(c => c.IsActive)
            .Join(
                Orders.Where(o => o.IsPaid),
                c => c.Id,
                o => o.CustomerId,
                (c, o) => new { c.Name, OrderId = o.Id }
            )
            .Join(
                OrderLines.Where(l => l.Quantity > 1),
                x => x.OrderId,
                l => l.OrderId,
                (x, l) =>
                    new
                    {
                        x.Name,
                        l.ProductId,
                        l.Quantity,
                    }
            )
            .Join(
                Products.Where(p => p.Price > 5m),
                x => x.ProductId,
                p => p.Id,
                (x, p) =>
                    new
                    {
                        x.Name,
                        p.Price,
                        x.Quantity,
                    }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t3."Price", t2."Quantity"
                FROM "Customer" AS t0
                INNER JOIN "Order" AS t1 ON t0."Id" = t1."CustomerId"
                INNER JOIN "OrderLine" AS t2 ON t1."Id" = t2."OrderId"
                INNER JOIN "Product" AS t3 ON t2."ProductId" = t3."Id"
                WHERE t0."IsActive" AND t1."IsPaid" AND (t2."Quantity" > 1) AND (t3."Price" > 5)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void MixesInnerAndLeftJoinsWithFilters()
    {
        Expression query = Customers
            .Join(Orders, c => c.Id, o => o.CustomerId, (c, o) => new { c, o })
            .GroupJoin(OrderLines, x => x.o.Id, l => l.OrderId, (x, ls) => new { x, ls })
            .SelectMany(
                y => y.ls.DefaultIfEmpty(),
                (y, l) =>
                    new
                    {
                        y.x.c.Name,
                        OrderId = y.x.o.Id,
                        Quantity = (int?)l!.Quantity,
                    }
            )
            .Where(z => z.Name != "x")
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t1."Id" AS "OrderId", t2."Quantity"
                FROM "Customer" AS t0
                INNER JOIN "Order" AS t1 ON t0."Id" = t1."CustomerId"
                LEFT JOIN "OrderLine" AS t2 ON t1."Id" = t2."OrderId"
                WHERE t0."Name" <> 'x'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsManagerChainOfThreeLevels()
    {
        Expression query = Employees
            .Join(
                Employees,
                e => e.ManagerId,
                m => m.Id,
                (e, m) =>
                    new
                    {
                        e.Name,
                        Manager = m.Name,
                        m.ManagerId,
                    }
            )
            .Join(
                Employees,
                x => x.ManagerId,
                g => g.Id,
                (x, g) =>
                    new
                    {
                        x.Name,
                        x.Manager,
                        Director = g.Name,
                    }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t1."Name" AS "Manager", t2."Name" AS "Director"
                FROM "Employee" AS t0
                INNER JOIN "Employee" AS t1 ON t0."ManagerId" = t1."Id"
                INNER JOIN "Employee" AS t2 ON t1."ManagerId" = t2."Id"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void LeftJoinsManagerChainOfTwoLevels()
    {
        Expression query = Employees
            .GroupJoin(Employees, e => e.ManagerId, m => m.Id, (e, ms) => new { e, ms })
            .SelectMany(
                x => x.ms.DefaultIfEmpty(),
                (x, m) =>
                    new
                    {
                        x.e.Name,
                        Manager = m!.Name,
                        ManagerOfManager = m!.ManagerId,
                    }
            )
            .GroupJoin(
                Employees,
                y => y.ManagerOfManager,
                g => g.Id,
                (y, gs) => new { y, gs }
            )
            .SelectMany(
                z => z.gs.DefaultIfEmpty(),
                (z, g) =>
                    new
                    {
                        z.y.Name,
                        z.y.Manager,
                        Director = g!.Name,
                    }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t1."Name" AS "Manager", t2."Name" AS "Director"
                FROM "Employee" AS t0
                LEFT JOIN "Employee" AS t1 ON t0."ManagerId" = t1."Id"
                LEFT JOIN "Employee" AS t2 ON t1."ManagerId" = t2."Id"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsOnCompositeKeyWithNullableMember()
    {
        Expression query = Products
            .Join(
                Customers,
                p => new { p.CategoryId, p.Name },
                c => new { CategoryId = (int?)c.Id, c.Name },
                (p, c) => new { p.Id, c.Region }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t1."Region"
                FROM "Product" AS t0
                INNER JOIN "Customer" AS t1 ON (t0."CategoryId" = t1."Id") AND (t0."Name" = t1."Name")
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsOnCompositeKeyOfThreeMembers()
    {
        Expression query = OrderLines
            .Join(
                Orders,
                l => new
                {
                    l.OrderId,
                    l.Quantity,
                    Total = l.UnitPrice,
                },
                o => new
                {
                    OrderId = o.Id,
                    o.Quantity,
                    o.Total,
                },
                (l, o) => o.Number
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Number"
                FROM "OrderLine" AS t0
                INNER JOIN "Order" AS t1 ON (t0."OrderId" = t1."Id") AND (t0."Quantity" = t1."Quantity") AND (t0."UnitPrice" = t1."Total")
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsOnConditionalKey()
    {
        Expression query = Customers
            .Join(
                Orders,
                c => c.IsActive ? c.Id : 0,
                o => o.CustomerId,
                (c, o) => new { c.Name, o.Id }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t1."Id"
                FROM "Customer" AS t0
                INNER JOIN "Order" AS t1 ON CASE WHEN t0."IsActive" THEN t0."Id" ELSE 0 END = t1."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsOnStringFunctionKey()
    {
        Expression query = Customers
            .Join(
                Employees,
                c => c.Name.Trim().ToLower(),
                e => e.Name.Trim().ToLower(),
                (c, e) => new { c.Id, EmployeeId = e.Id }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t1."Id" AS "EmployeeId"
                FROM "Customer" AS t0
                INNER JOIN "Employee" AS t1 ON LOWER(BTRIM(t0."Name")) = LOWER(BTRIM(t1."Name"))
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void GroupJoinsOnCompositeKeyWithAggregate()
    {
        Expression query = OrderLines
            .GroupJoin(
                Orders,
                l => new { l.OrderId, l.Quantity },
                o => new { OrderId = o.Id, o.Quantity },
                (l, os) => new { l.ProductId, Matches = os.Count() }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."ProductId", (
                    SELECT CAST(COUNT(*) AS integer)
                    FROM "Order" AS t1
                    WHERE (t1."Id" = t0."OrderId") AND (t1."Quantity" = t0."Quantity")
                ) AS "Matches"
                FROM "OrderLine" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsThenGroupsAndOrders()
    {
        Expression query = Customers
            .Join(
                Orders,
                c => c.Id,
                o => o.CustomerId,
                (c, o) => new { c.Region, o.Total }
            )
            .GroupBy(x => x.Region)
            .Select(g => new
            {
                Region = g.Key,
                Revenue = g.Sum(x => x.Total),
                Orders = g.Count(),
            })
            .OrderByDescending(x => x.Revenue)
            .Take(3)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Region", COALESCE(SUM(t1."Total"), 0) AS "Revenue",
                CAST(COUNT(*) AS integer) AS "Orders"
                FROM "Customer" AS t0
                INNER JOIN "Order" AS t1 ON t0."Id" = t1."CustomerId"
                GROUP BY t0."Region"
                ORDER BY COALESCE(SUM(t1."Total"), 0) DESC
                LIMIT 3
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsThenFiltersGroupsWithHaving()
    {
        Expression query = Customers
            .Join(
                Orders,
                c => c.Id,
                o => o.CustomerId,
                (c, o) =>
                    new
                    {
                        c.Id,
                        c.Name,
                        o.Total,
                    }
            )
            .GroupBy(x => new { x.Id, x.Name })
            .Where(g => g.Sum(x => x.Total) > 1000m)
            .Select(g => new { g.Key.Name, Revenue = g.Sum(x => x.Total) })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", COALESCE(SUM(t1."Total"), 0) AS "Revenue"
                FROM "Customer" AS t0
                INNER JOIN "Order" AS t1 ON t0."Id" = t1."CustomerId"
                GROUP BY t0."Id", t0."Name"
                HAVING COALESCE(SUM(t1."Total"), 0) > 1000
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void JoinsUnionedSources()
    {
        Expression query = Customers
            .Select(c => c.Id)
            .Union(Employees.Select(e => e.Id))
            .Join(Orders, id => id, o => o.CustomerId, (id, o) => o.Total)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t3."Total"
                FROM (
                    SELECT t0."Id"
                    FROM "Customer" AS t0
                    UNION
                    SELECT t1."Id"
                    FROM "Employee" AS t1
                ) AS t2
                INNER JOIN "Order" AS t3 ON t2."Id" = t3."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void JoinsThenUnionsWithOtherJoin()
    {
        Expression query = Customers
            .Join(Orders, c => c.Id, o => o.CustomerId, (c, o) => o.Total)
            .Concat(
                Products.Join(
                    OrderLines,
                    p => p.Id,
                    l => l.ProductId,
                    (p, l) => l.UnitPrice
                )
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Total"
                FROM "Customer" AS t0
                INNER JOIN "Order" AS t1 ON t0."Id" = t1."CustomerId"
                UNION ALL
                SELECT t3."UnitPrice"
                FROM "Product" AS t2
                INNER JOIN "OrderLine" AS t3 ON t2."Id" = t3."ProductId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsThenChecksExistence()
    {
        Expression query = Customers
            .Join(Orders, c => c.Id, o => o.CustomerId, (c, o) => new { c.Name, o.Total })
            .Where(x => Employees.Any(e => e.Name == x.Name))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t1."Total"
                FROM "Customer" AS t0
                INNER JOIN "Order" AS t1 ON t0."Id" = t1."CustomerId"
                WHERE EXISTS (
                    SELECT 1
                    FROM "Employee" AS t2
                    WHERE t2."Name" = t0."Name"
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
