namespace Linq.Expressions.Translation.PostgreSql.Tests.Operators;

public sealed record JoinTests
{
    [Fact]
    public void JoinsTwoTablesIntoAnonymousType()
    {
        Expression query = Customers
            .Join(Orders, c => c.Id, o => o.CustomerId, (c, o) => new { c.Name, o.Total })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t1."Total"
                FROM "Customer" AS t0
                INNER JOIN "Order" AS t1 ON t0."Id" = t1."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsIntoRecord()
    {
        Expression query = Customers
            .Join(
                Orders,
                c => c.Id,
                o => o.CustomerId,
                (c, o) => new CustomerSummary(o.Id, c.Name)
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Id", t0."Name"
                FROM "Customer" AS t0
                INNER JOIN "Order" AS t1 ON t0."Id" = t1."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AliasesJoinedColumnsWithSameName()
    {
        Expression query = Customers
            .Join(
                Orders,
                c => c.Id,
                o => o.CustomerId,
                (c, o) => new { CustomerId = c.Id, OrderId = o.Id }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id" AS "CustomerId", t1."Id" AS "OrderId"
                FROM "Customer" AS t0
                INNER JOIN "Order" AS t1 ON t0."Id" = t1."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsIntoScalar()
    {
        Expression query = Customers
            .Join(Orders, c => c.Id, o => o.CustomerId, (c, o) => o.Total)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Total"
                FROM "Customer" AS t0
                INNER JOIN "Order" AS t1 ON t0."Id" = t1."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsIntoWholeEntities()
    {
        Expression query = Customers
            .Join(
                Orders,
                c => c.Id,
                o => o.CustomerId,
                (c, o) => new { Customer = c, Order = o }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id" AS "Customer.Id", t0."Name" AS "Customer.Name",
                t0."Email" AS "Customer.Email", t0."Region" AS "Customer.Region",
                t0."IsActive" AS "Customer.IsActive", t1."Id" AS "Order.Id",
                t1."CustomerId" AS "Order.CustomerId", t1."Number" AS "Order.Number",
                t1."Note" AS "Order.Note", t1."Total" AS "Order.Total",
                t1."Discount" AS "Order.Discount", t1."Quantity" AS "Order.Quantity",
                t1."Priority" AS "Order.Priority", t1."Points" AS "Order.Points",
                t1."Weight" AS "Order.Weight", t1."IsPaid" AS "Order.IsPaid",
                t1."IsGift" AS "Order.IsGift", t1."Status" AS "Order.Status",
                t1."Options" AS "Order.Options", t1."CreatedAt" AS "Order.CreatedAt",
                t1."PaidAt" AS "Order.PaidAt", t1."ShippedOn" AS "Order.ShippedOn",
                t1."DeliveryTime" AS "Order.DeliveryTime",
                t1."TrackingId" AS "Order.TrackingId", t1."Tags" AS "Order.Tags"
                FROM "Customer" AS t0
                INNER JOIN "Order" AS t1 ON t0."Id" = t1."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsOnStringKeys()
    {
        Expression query = Customers
            .Join(
                Products,
                c => c.Name,
                p => p.Name,
                (c, p) => new { c.Id, ProductId = p.Id }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t1."Id" AS "ProductId"
                FROM "Customer" AS t0
                INNER JOIN "Product" AS t1 ON t0."Name" = t1."Name"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsOnCompositeKey()
    {
        Expression query = OrderLines
            .Join(
                Orders,
                l => new { l.OrderId, l.Quantity },
                o => new { OrderId = o.Id, o.Quantity },
                (l, o) => new { l.UnitPrice, o.Number }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."UnitPrice", t1."Number"
                FROM "OrderLine" AS t0
                INNER JOIN "Order" AS t1 ON (t0."OrderId" = t1."Id") AND (t0."Quantity" = t1."Quantity")
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsOnComputedKey()
    {
        Expression query = Customers
            .Join(
                Orders,
                c => c.Id * 2,
                o => o.CustomerId,
                (c, o) => new { c.Name, o.Id }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t1."Id"
                FROM "Customer" AS t0
                INNER JOIN "Order" AS t1 ON (t0."Id" * 2) = t1."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsOnNullableKeyWithPlainEquality()
    {
        Expression query = Products
            .Join(
                Customers,
                p => p.CategoryId,
                c => c.Id,
                (p, c) => new { p.Name, c.Region }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t1."Region"
                FROM "Product" AS t0
                INNER JOIN "Customer" AS t1 ON t0."CategoryId" = t1."Id"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsOnTwoNullableKeysWithPlainEquality()
    {
        Expression query = Employees
            .Join(
                Employees,
                e => e.ManagerId,
                m => m.ManagerId,
                (e, m) => new { e.Id, ManagerOf = m.Id }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t1."Id" AS "ManagerOf"
                FROM "Employee" AS t0
                INNER JOIN "Employee" AS t1 ON t0."ManagerId" = t1."ManagerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    // An anonymous key is never null itself; its members compare with the default
    // comparer, where null equals null.
    [Fact]
    public void JoinsOnCompositeKeyWithTwoNullableMembers()
    {
        Expression query = Employees
            .Join(
                Employees,
                e => new { e.ManagerId, e.Name },
                m => new { m.ManagerId, m.Name },
                (e, m) => new { Employee = e.Id, Peer = m.Id }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id" AS "Employee", t1."Id" AS "Peer"
                FROM "Employee" AS t0
                INNER JOIN "Employee" AS t1 ON (t0."ManagerId" IS NOT DISTINCT FROM t1."ManagerId") AND (t0."Name" = t1."Name")
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsOnCompositeKeyWithOneNullableMember()
    {
        Expression query = Employees
            .Join(
                Employees,
                e => new { Key = e.ManagerId, e.Name },
                m => new { Key = (int?)m.Id, m.Name },
                (e, m) => new { Employee = e.Id, Manager = m.Id }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id" AS "Employee", t1."Id" AS "Manager"
                FROM "Employee" AS t0
                INNER JOIN "Employee" AS t1 ON (t0."ManagerId" = t1."Id") AND (t0."Name" = t1."Name")
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsOnNestedCompositeKeyWithNullableMembers()
    {
        Expression query = Employees
            .Join(
                Employees,
                e => new { Manager = new { e.ManagerId }, e.Name },
                m => new { Manager = new { m.ManagerId }, m.Name },
                (e, m) => new { Employee = e.Id, Peer = m.Id }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id" AS "Employee", t1."Id" AS "Peer"
                FROM "Employee" AS t0
                INNER JOIN "Employee" AS t1 ON (t0."ManagerId" IS NOT DISTINCT FROM t1."ManagerId") AND (t0."Name" = t1."Name")
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsTableWithItself()
    {
        Expression query = Employees
            .Join(
                Employees,
                e => e.ManagerId,
                m => m.Id,
                (e, m) => new { Employee = e.Name, Manager = m.Name }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name" AS "Employee", t1."Name" AS "Manager"
                FROM "Employee" AS t0
                INNER JOIN "Employee" AS t1 ON t0."ManagerId" = t1."Id"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersBeforeJoin()
    {
        Expression query = Customers
            .Where(c => c.IsActive)
            .Join(Orders, c => c.Id, o => o.CustomerId, (c, o) => new { c.Name, o.Total })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t1."Total"
                FROM "Customer" AS t0
                INNER JOIN "Order" AS t1 ON t0."Id" = t1."CustomerId"
                WHERE t0."IsActive"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersInnerSourceBeforeJoin()
    {
        Expression query = Customers
            .Join(
                Orders.Where(o => o.IsPaid),
                c => c.Id,
                o => o.CustomerId,
                (c, o) => new { c.Name, o.Total }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t1."Total"
                FROM "Customer" AS t0
                INNER JOIN "Order" AS t1 ON t0."Id" = t1."CustomerId"
                WHERE t1."IsPaid"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersAfterJoinOnBothSides()
    {
        Expression query = Customers
            .Join(Orders, c => c.Id, o => o.CustomerId, (c, o) => new { c, o })
            .Where(x => x.c.IsActive && (x.o.Total > 100))
            .Select(x => new { x.c.Name, x.o.Total })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t1."Total"
                FROM "Customer" AS t0
                INNER JOIN "Order" AS t1 ON t0."Id" = t1."CustomerId"
                WHERE t0."IsActive" AND (t1."Total" > 100)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OrdersAfterJoin()
    {
        Expression query = Customers
            .Join(Orders, c => c.Id, o => o.CustomerId, (c, o) => new { c.Name, o.Total })
            .OrderByDescending(x => x.Total)
            .ThenBy(x => x.Name)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t1."Total"
                FROM "Customer" AS t0
                INNER JOIN "Order" AS t1 ON t0."Id" = t1."CustomerId"
                ORDER BY t1."Total" DESC, t0."Name"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void PagesAfterJoin()
    {
        Expression query = Customers
            .Join(Orders, c => c.Id, o => o.CustomerId, (c, o) => new { c.Name, o.Total })
            .OrderBy(x => x.Name)
            .Skip(10)
            .Take(5)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t1."Total"
                FROM "Customer" AS t0
                INNER JOIN "Order" AS t1 ON t0."Id" = t1."CustomerId"
                ORDER BY t0."Name"
                LIMIT 5 OFFSET 10
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsThreeTables()
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
                (x, l) => new { x.Name, l.Quantity }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t2."Quantity"
                FROM "Customer" AS t0
                INNER JOIN "Order" AS t1 ON t0."Id" = t1."CustomerId"
                INNER JOIN "OrderLine" AS t2 ON t1."Id" = t2."OrderId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsFourTables()
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
                        p.Price,
                    }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t3."Name" AS "Product", t3."Price"
                FROM "Customer" AS t0
                INNER JOIN "Order" AS t1 ON t0."Id" = t1."CustomerId"
                INNER JOIN "OrderLine" AS t2 ON t1."Id" = t2."OrderId"
                INNER JOIN "Product" AS t3 ON t2."ProductId" = t3."Id"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsWithQuerySyntax()
    {
        Expression query = (
            from c in Customers
            join o in Orders on c.Id equals o.CustomerId
            select new { c.Name, o.Total }
        ).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t1."Total"
                FROM "Customer" AS t0
                INNER JOIN "Order" AS t1 ON t0."Id" = t1."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsWithQuerySyntaxAndCompositeKey()
    {
        Expression query = (
            from l in OrderLines
            join o in Orders
                on new { l.OrderId, l.Quantity } equals new { OrderId = o.Id, o.Quantity }
            select new { l.UnitPrice, o.Number }
        ).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."UnitPrice", t1."Number"
                FROM "OrderLine" AS t0
                INNER JOIN "Order" AS t1 ON (t0."OrderId" = t1."Id") AND (t0."Quantity" = t1."Quantity")
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsWithQuerySyntaxFilterAndOrder()
    {
        Expression query = (
            from c in Customers
            join o in Orders on c.Id equals o.CustomerId
            where o.Total > 100
            orderby o.Total descending
            select new { c.Name, o.Total }
        ).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t1."Total"
                FROM "Customer" AS t0
                INNER JOIN "Order" AS t1 ON t0."Id" = t1."CustomerId"
                WHERE t1."Total" > 100
                ORDER BY t1."Total" DESC
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsOnPagedOuterSourceAsDerivedTable()
    {
        Expression query = Customers
            .OrderBy(c => c.Id)
            .Take(5)
            .Join(Orders, c => c.Id, o => o.CustomerId, (c, o) => new { c.Name, o.Total })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Name", t2."Total"
                FROM (
                    SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                    FROM "Customer" AS t0
                    ORDER BY t0."Id"
                    LIMIT 5
                ) AS t1
                INNER JOIN "Order" AS t2 ON t1."Id" = t2."CustomerId"
                ORDER BY t1."Id"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsPagedInnerSourceAsDerivedTable()
    {
        Expression query = Customers
            .Join(
                Orders.OrderByDescending(o => o.Total).Take(3),
                c => c.Id,
                o => o.CustomerId,
                (c, o) => new { c.Name, o.Total }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t2."Total"
                FROM "Customer" AS t0
                INNER JOIN (
                    SELECT t1."Id", t1."CustomerId", t1."Number", t1."Note",
                    t1."Total", t1."Discount", t1."Quantity", t1."Priority",
                    t1."Points", t1."Weight", t1."IsPaid", t1."IsGift", t1."Status",
                    t1."Options", t1."CreatedAt", t1."PaidAt", t1."ShippedOn",
                    t1."DeliveryTime", t1."TrackingId", t1."Tags"
                    FROM "Order" AS t1
                    ORDER BY t1."Total" DESC
                    LIMIT 3
                ) AS t2 ON t0."Id" = t2."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsProjectedInnerSource()
    {
        Expression query = Customers
            .Join(
                Orders.Select(o => new { o.CustomerId, Double = o.Total * 2 }),
                c => c.Id,
                o => o.CustomerId,
                (c, o) => new { c.Name, o.Double }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t1."Total" * 2 AS "Double"
                FROM "Customer" AS t0
                INNER JOIN "Order" AS t1 ON t0."Id" = t1."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CountsJoinedRows()
    {
        Expression query = Customers
            .Join(Orders, c => c.Id, o => o.CustomerId, (c, o) => new { c.Name, o.Total })
            .Executed(q => q.Count());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COUNT(*) AS integer)
                FROM "Customer" AS t0
                INNER JOIN "Order" AS t1 ON t0."Id" = t1."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SumsJoinedColumn()
    {
        Expression query = Customers
            .Join(
                Orders,
                c => c.Id,
                o => o.CustomerId,
                (c, o) => new { c.Region, o.Total }
            )
            .Where(x => x.Region == "EU")
            .Executed(q => q.Sum(x => x.Total));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(SUM(t1."Total"), 0)
                FROM "Customer" AS t0
                INNER JOIN "Order" AS t1 ON t0."Id" = t1."CustomerId"
                WHERE t0."Region" = 'EU'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsPagedProjectionWithoutHiddenOrderingKey()
    {
        Expression query = Customers
            .Join(
                Orders
                    .OrderBy(o => o.CreatedAt)
                    .Select(o => new { o.CustomerId, o.Total })
                    .Take(5),
                c => c.Id,
                x => x.CustomerId,
                (c, x) => new { c.Name, x.Total }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t2."Total"
                FROM "Customer" AS t0
                INNER JOIN (
                    SELECT t1."CustomerId", t1."Total"
                    FROM "Order" AS t1
                    ORDER BY t1."CreatedAt"
                    LIMIT 5
                ) AS t2 ON t0."Id" = t2."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
