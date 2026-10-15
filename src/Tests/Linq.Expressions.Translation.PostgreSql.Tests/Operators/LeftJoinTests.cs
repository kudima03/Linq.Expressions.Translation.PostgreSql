namespace Linq.Expressions.Translation.PostgreSql.Tests.Operators;

public sealed record LeftJoinTests
{
    [Fact(Skip = Skipped.NotImplemented)]
    public void LeftJoinsWithGroupJoinAndDefaultIfEmpty()
    {
        Expression query = Customers
            .GroupJoin(Orders, c => c.Id, o => o.CustomerId, (c, os) => new { c, os })
            .SelectMany(
                x => x.os.DefaultIfEmpty(),
                (x, o) => new { x.c.Name, OrderId = (int?)o!.Id }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t1."Id" AS "OrderId"
                FROM "Customer" AS t0
                LEFT JOIN "Order" AS t1 ON t0."Id" = t1."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void LeftJoinsProjectingSeveralNullableColumns()
    {
        Expression query = Customers
            .GroupJoin(Orders, c => c.Id, o => o.CustomerId, (c, os) => new { c, os })
            .SelectMany(
                x => x.os.DefaultIfEmpty(),
                (x, o) =>
                    new
                    {
                        x.c.Name,
                        OrderId = (int?)o!.Id,
                        Total = (decimal?)o!.Total,
                        Paid = (bool?)o!.IsPaid,
                    }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t1."Id" AS "OrderId", t1."Total",
                t1."IsPaid" AS "Paid"
                FROM "Customer" AS t0
                LEFT JOIN "Order" AS t1 ON t0."Id" = t1."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void LeftJoinsProjectingNullableReferenceColumn()
    {
        Expression query = Customers
            .GroupJoin(Orders, c => c.Id, o => o.CustomerId, (c, os) => new { c, os })
            .SelectMany(x => x.os.DefaultIfEmpty(), (x, o) => new { x.c.Name, o!.Number })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t1."Number"
                FROM "Customer" AS t0
                LEFT JOIN "Order" AS t1 ON t0."Id" = t1."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void LeftJoinsWithQuerySyntax()
    {
        Expression query = (
            from c in Customers
            join o in Orders on c.Id equals o.CustomerId into g
            from o in g.DefaultIfEmpty()
            select new { c.Name, OrderId = (int?)o!.Id }
        ).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t1."Id" AS "OrderId"
                FROM "Customer" AS t0
                LEFT JOIN "Order" AS t1 ON t0."Id" = t1."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FiltersCustomersWithoutOrders()
    {
        Expression query = Customers
            .GroupJoin(Orders, c => c.Id, o => o.CustomerId, (c, os) => new { c, os })
            .SelectMany(
                x => x.os.DefaultIfEmpty(),
                (x, o) => new { x.c.Name, OrderId = (int?)o!.Id }
            )
            .Where(x => x.OrderId == null)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t1."Id" AS "OrderId"
                FROM "Customer" AS t0
                LEFT JOIN "Order" AS t1 ON t0."Id" = t1."CustomerId"
                WHERE t1."Id" IS NULL
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FiltersCustomersWithOrders()
    {
        Expression query = Customers
            .GroupJoin(Orders, c => c.Id, o => o.CustomerId, (c, os) => new { c, os })
            .SelectMany(
                x => x.os.DefaultIfEmpty(),
                (x, o) => new { x.c.Name, OrderId = (int?)o!.Id }
            )
            .Where(x => x.OrderId != null)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t1."Id" AS "OrderId"
                FROM "Customer" AS t0
                LEFT JOIN "Order" AS t1 ON t0."Id" = t1."CustomerId"
                WHERE t1."Id" IS NOT NULL
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FiltersLeftJoinedColumnWithNullableComparison()
    {
        Expression query = Customers
            .GroupJoin(Orders, c => c.Id, o => o.CustomerId, (c, os) => new { c, os })
            .SelectMany(
                x => x.os.DefaultIfEmpty(),
                (x, o) => new { x.c.Name, Total = (decimal?)o!.Total }
            )
            .Where(x => x.Total > 100)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t1."Total"
                FROM "Customer" AS t0
                LEFT JOIN "Order" AS t1 ON t0."Id" = t1."CustomerId"
                WHERE t1."Total" > 100
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void KeepsInnerSourceFilterInJoinCondition()
    {
        Expression query = Customers
            .GroupJoin(
                Orders.Where(o => o.IsPaid),
                c => c.Id,
                o => o.CustomerId,
                (c, os) => new { c, os }
            )
            .SelectMany(
                x => x.os.DefaultIfEmpty(),
                (x, o) => new { x.c.Name, OrderId = (int?)o!.Id }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t1."Id" AS "OrderId"
                FROM "Customer" AS t0
                LEFT JOIN "Order" AS t1 ON (t0."Id" = t1."CustomerId") AND t1."IsPaid"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FiltersOuterSourceBeforeLeftJoin()
    {
        Expression query = Customers
            .Where(c => c.IsActive)
            .GroupJoin(Orders, c => c.Id, o => o.CustomerId, (c, os) => new { c, os })
            .SelectMany(
                x => x.os.DefaultIfEmpty(),
                (x, o) => new { x.c.Name, OrderId = (int?)o!.Id }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t1."Id" AS "OrderId"
                FROM "Customer" AS t0
                LEFT JOIN "Order" AS t1 ON t0."Id" = t1."CustomerId"
                WHERE t0."IsActive"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void LeftJoinsOnCompositeKey()
    {
        Expression query = OrderLines
            .GroupJoin(
                Orders,
                l => new { l.OrderId, l.Quantity },
                o => new { OrderId = o.Id, o.Quantity },
                (l, os) => new { l, os }
            )
            .SelectMany(
                x => x.os.DefaultIfEmpty(),
                (x, o) => new { x.l.UnitPrice, o!.Number }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."UnitPrice", t1."Number"
                FROM "OrderLine" AS t0
                LEFT JOIN "Order" AS t1 ON (t0."OrderId" = t1."Id") AND (t0."Quantity" = t1."Quantity")
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void LeftJoinsTableWithItself()
    {
        Expression query = Employees
            .GroupJoin(Employees, e => e.ManagerId, m => m.Id, (e, ms) => new { e, ms })
            .SelectMany(
                x => x.ms.DefaultIfEmpty(),
                (x, m) => new { x.e.Name, Manager = m!.Name }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t1."Name" AS "Manager"
                FROM "Employee" AS t0
                LEFT JOIN "Employee" AS t1 ON t0."ManagerId" = t1."Id"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void ChainsTwoLeftJoins()
    {
        Expression query = Customers
            .GroupJoin(Orders, c => c.Id, o => o.CustomerId, (c, os) => new { c, os })
            .SelectMany(x => x.os.DefaultIfEmpty(), (x, o) => new { x.c, o })
            .GroupJoin(OrderLines, x => x.o!.Id, l => l.OrderId, (x, ls) => new { x, ls })
            .SelectMany(
                y => y.ls.DefaultIfEmpty(),
                (y, l) =>
                    new
                    {
                        y.x.c.Name,
                        OrderId = (int?)y.x.o!.Id,
                        Quantity = (int?)l!.Quantity,
                    }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t1."Id" AS "OrderId", t2."Quantity"
                FROM "Customer" AS t0
                LEFT JOIN "Order" AS t1 ON t0."Id" = t1."CustomerId"
                LEFT JOIN "OrderLine" AS t2 ON t1."Id" = t2."OrderId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void MixesInnerJoinAndLeftJoin()
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
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t1."Id" AS "OrderId", t2."Quantity"
                FROM "Customer" AS t0
                INNER JOIN "Order" AS t1 ON t0."Id" = t1."CustomerId"
                LEFT JOIN "OrderLine" AS t2 ON t1."Id" = t2."OrderId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void OrdersAndPagesAfterLeftJoin()
    {
        Expression query = Customers
            .GroupJoin(Orders, c => c.Id, o => o.CustomerId, (c, os) => new { c, os })
            .SelectMany(
                x => x.os.DefaultIfEmpty(),
                (x, o) => new { x.c.Name, Total = (decimal?)o!.Total }
            )
            .OrderByDescending(x => x.Total)
            .Take(10)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t1."Total"
                FROM "Customer" AS t0
                LEFT JOIN "Order" AS t1 ON t0."Id" = t1."CustomerId"
                ORDER BY t1."Total" DESC NULLS LAST
                LIMIT 10
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void JoinsPagedInnerSourceAsDerivedTableOnTheLeft()
    {
        Expression query = Customers
            .GroupJoin(
                Orders
                    .OrderByDescending(o => o.Total)
                    .Take(3)
                    .Select(o => new { o.CustomerId, o.Total }),
                c => c.Id,
                o => o.CustomerId,
                (c, os) => new { c, os }
            )
            .SelectMany(
                x => x.os.DefaultIfEmpty(),
                (x, o) => new { x.c.Name, Total = (decimal?)o!.Total }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t2."Total"
                FROM "Customer" AS t0
                LEFT JOIN (
                    SELECT t1."CustomerId", t1."Total"
                    FROM "Order" AS t1
                    ORDER BY t1."Total" DESC
                    LIMIT 3
                ) AS t2 ON t0."Id" = t2."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void CountsLeftJoinedRows()
    {
        Expression query = Customers
            .GroupJoin(Orders, c => c.Id, o => o.CustomerId, (c, os) => new { c, os })
            .SelectMany(
                x => x.os.DefaultIfEmpty(),
                (x, o) => new { x.c.Name, OrderId = (int?)o!.Id }
            )
            .Executed(q => q.Count());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COUNT(*) AS integer)
                FROM "Customer" AS t0
                LEFT JOIN "Order" AS t1 ON t0."Id" = t1."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
