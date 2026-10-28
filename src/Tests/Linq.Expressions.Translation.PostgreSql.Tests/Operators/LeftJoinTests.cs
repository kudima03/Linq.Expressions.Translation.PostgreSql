namespace Linq.Expressions.Translation.PostgreSql.Tests.Operators;

public sealed record LeftJoinTests
{
    public sealed class Sticker
    {
        public int? OrderId { get; init; }

        public string? Text { get; init; }
    }

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
    public void LeftJoinsWithQuerySyntax()
    {
        Expression query = (
            from c in Customers
            join o in Orders on c.Id equals o.CustomerId into g
            from o in g.DefaultIfEmpty()
            select new { c.Name, OrderId = (int?)o.Id }
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
    public void LeftJoinsOnTwoNullableKeysWithPlainEquality()
    {
        Expression query = Employees
            .GroupJoin(
                Employees,
                e => e.ManagerId,
                m => m.ManagerId,
                (e, ms) => new { e, ms }
            )
            .SelectMany(
                x => x.ms.DefaultIfEmpty(),
                (x, m) => new { x.e.Name, Peer = m!.Name }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t1."Name" AS "Peer"
                FROM "Employee" AS t0
                LEFT JOIN "Employee" AS t1 ON t0."ManagerId" = t1."ManagerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
    public void ProjectsComputedValueOfLeftJoinedRows()
    {
        Expression query = Customers
            .SelectMany(
                c =>
                    Orders
                        .Where(o => o.CustomerId == c.Id)
                        .Select(o => o.Total * 2)
                        .DefaultIfEmpty(),
                (c, total) => new { c.Name, Total = total }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", COALESCE(t2."Value", 0) AS "Total"
                FROM "Customer" AS t0
                LEFT JOIN LATERAL (
                    SELECT t1."Total" * 2 AS "Value"
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                ) AS t2 ON TRUE
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesDefaultedComputedColumnOfLeftJoinedSource()
    {
        Expression query = Customers
            .SelectMany(
                c =>
                    Orders
                        .Where(o => o.CustomerId == c.Id)
                        .Select(o => o.Quantity + 1)
                        .DefaultIfEmpty(),
                (c, q) => new { c.Name, Quantity = q }
            )
            .Where(x => x.Quantity != 5)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", COALESCE(t2."Value", 0) AS "Quantity"
                FROM "Customer" AS t0
                LEFT JOIN LATERAL (
                    SELECT t1."Quantity" + 1 AS "Value"
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                ) AS t2 ON TRUE
                WHERE COALESCE(t2."Value", 0) <> 5
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OrdersByConstantColumnOfLeftJoinedSource()
    {
        Expression query = Customers
            .SelectMany(
                c =>
                    Orders
                        .Where(o => o.CustomerId == c.Id)
                        .Select(o => "order")
                        .DefaultIfEmpty(),
                (c, s) => new { c.Name, Kind = s }
            )
            .OrderBy(x => x.Kind)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t2."Value" AS "Kind"
                FROM "Customer" AS t0
                LEFT JOIN LATERAL (
                    SELECT 'order' AS "Value"
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                ) AS t2 ON TRUE
                ORDER BY t2."Value" NULLS FIRST
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void LeftJoinsProjectionWithComputedColumnAsDerivedTable()
    {
        Expression query = Customers
            .GroupJoin(
                Orders.Select(o => new ValueTuple<int?, string?>(
                    o.CustomerId,
                    o.Note ?? "none"
                )),
                c => c.Id,
                o => o.Item1,
                (c, matches) => new { c, matches }
            )
            .SelectMany(
                x => x.matches.DefaultIfEmpty(),
                (x, o) => new ValueTuple<int, int?, string?>(x.c.Id, o.Item1, o.Item2)
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id" AS "item1", t2."item1" AS "item2", t2."item2" AS "item3"
                FROM "Customer" AS t0
                LEFT JOIN (
                    SELECT t1."CustomerId" AS "item1", COALESCE(t1."Note", 'none') AS "item2"
                    FROM "Order" AS t1
                ) AS t2 ON t0."Id" = t2."item1"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void DefaultsUnmatchedValueOfLeftJoinedScalar()
    {
        Expression query = Customers
            .SelectMany(
                c =>
                    Orders
                        .Where(o => o.CustomerId == c.Id)
                        .Select(o => o.Quantity)
                        .DefaultIfEmpty(),
                (c, q) => new { c.Id, Quantity = q }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", COALESCE(t1."Quantity", 0) AS "Quantity"
                FROM "Customer" AS t0
                LEFT JOIN "Order" AS t1 ON t1."CustomerId" = t0."Id"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void DefaultsUnmatchedValueMembersOfLeftJoinedTupleButNullsReferences()
    {
        Expression query = Customers
            .GroupJoin(
                Orders.Select(o => new ValueTuple<int?, ValueTuple<int>, string?>(
                    o.CustomerId,
                    new ValueTuple<int>(o.Quantity),
                    o.Note
                )),
                c => c.Id,
                o => o.Item1,
                (c, matches) => new { c, matches }
            )
            .SelectMany(
                x => x.matches.DefaultIfEmpty(),
                (x, o) =>
                    new ValueTuple<int, int, string?>(x.c.Id, o.Item2.Item1, o.Item3)
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id" AS "item1", COALESCE(t1."Quantity", 0) AS "item2", t1."Note" AS "item3"
                FROM "Customer" AS t0
                LEFT JOIN "Order" AS t1 ON t0."Id" = t1."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void KeepsNullableMembersOfLeftJoinedTupleNull()
    {
        Expression query = Customers
            .GroupJoin(
                Orders.Select(o => new ValueTuple<int?, int?>(
                    o.CustomerId,
                    o.Quantity * 10
                )),
                c => c.Id,
                o => o.Item1,
                (c, matches) => new { c, matches }
            )
            .SelectMany(
                x => x.matches.DefaultIfEmpty(),
                (x, o) => new ValueTuple<int, int?>(x.c.Id, o.Item2)
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id" AS "item1", t2."item2"
                FROM "Customer" AS t0
                LEFT JOIN (
                    SELECT t1."CustomerId" AS "item1", t1."Quantity" * 10 AS "item2"
                    FROM "Order" AS t1
                ) AS t2 ON t0."Id" = t2."item1"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesLeftJoinedProjectionWithNullByRequiredColumn()
    {
        Expression query = Customers
            .GroupJoin(
                Orders.Select(o => new
                {
                    o.CustomerId,
                    o.Quantity,
                    Note = o.Note ?? "none",
                }),
                c => c.Id,
                o => o.CustomerId,
                (c, matches) => new { c, matches }
            )
            .SelectMany(
                x => x.matches.DefaultIfEmpty(),
                (x, o) =>
                    new
                    {
                        x.c.Id,
                        Quantity = o == null ? -1 : o.Quantity,
                        Missing = o == null,
                    }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", CASE WHEN t2."CustomerId" IS NULL THEN -1 ELSE t2."Quantity" END AS "Quantity", t2."CustomerId" IS NULL AS "Missing"
                FROM "Customer" AS t0
                LEFT JOIN (
                    SELECT t1."CustomerId", t1."Quantity", COALESCE(t1."Note", 'none') AS "Note"
                    FROM "Order" AS t1
                ) AS t2 ON t0."Id" = t2."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesLeftJoinedProjectionOfNullableColumnsWithNullByEveryColumn()
    {
        Expression query = Customers
            .GroupJoin(
                Orders.Select(o => new { o.Note, o.Priority }),
                c => c.Name,
                o => o.Note,
                (c, matches) => new { c, matches }
            )
            .SelectMany(
                x => x.matches.DefaultIfEmpty(),
                (x, o) => new { x.c.Id, Missing = o == null }
            )
            .Where(r => !r.Missing)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", (t1."Note" IS NULL) AND (t1."Priority" IS NULL) AS "Missing"
                FROM "Customer" AS t0
                LEFT JOIN "Order" AS t1 ON t0."Name" = t1."Note"
                WHERE NOT ((t1."Note" IS NULL) AND (t1."Priority" IS NULL))
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesLeftJoinedProjectionOfNullableColumnsWithNotNullByAnyColumn()
    {
        Expression query = Customers
            .GroupJoin(
                Orders.Select(o => new { o.Note, o.Priority }),
                c => c.Name,
                o => o.Note,
                (c, matches) => new { c, matches }
            )
            .SelectMany(
                x => x.matches.DefaultIfEmpty(),
                (x, o) => new { x.c.Id, Priority = o != null ? o.Priority : -1 }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", CASE WHEN (t1."Note" IS NOT NULL) OR (t1."Priority" IS NOT NULL) THEN t1."Priority" ELSE -1 END AS "Priority"
                FROM "Customer" AS t0
                LEFT JOIN "Order" AS t1 ON t0."Name" = t1."Note"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesProjectionThatIsNotLeftJoinedWithNullAsConstant()
    {
        Expression query = Customers
            .Select(c => new { c.Id })
            .Where(x => x != null)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Customer" AS t0
                WHERE TRUE
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SubstitutesExplicitDefaultOfLeftJoinedScalar()
    {
        Expression query = Customers
            .SelectMany(
                c =>
                    Orders
                        .Where(o => o.CustomerId == c.Id)
                        .Select(o => o.Quantity)
                        .DefaultIfEmpty(c.Id * 100),
                (c, q) => new { c.Id, Quantity = q }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", COALESCE(t1."Quantity", t0."Id" * 100) AS "Quantity"
                FROM "Customer" AS t0
                LEFT JOIN "Order" AS t1 ON t1."CustomerId" = t0."Id"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SubstitutesExplicitDefaultOfLeftJoinedComputedValue()
    {
        Expression query = Customers
            .SelectMany(
                c =>
                    Orders
                        .Where(o => o.CustomerId == c.Id)
                        .Select(o => o.Note ?? "noted")
                        .DefaultIfEmpty("none"),
                (c, n) => new { c.Id, Note = n }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", COALESCE(t2."Value", 'none') AS "Note"
                FROM "Customer" AS t0
                LEFT JOIN LATERAL (
                    SELECT COALESCE(t1."Note", 'noted') AS "Value"
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                ) AS t2 ON TRUE
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RejectsExplicitDefaultOfLeftJoinedNullableValue()
    {
        Expression query = Customers
            .SelectMany(
                c =>
                    Orders
                        .Where(o => o.CustomerId == c.Id)
                        .Select(o => o.Note)
                        .DefaultIfEmpty("none"),
                (c, n) => new { c.Id, Note = n }
            )
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Equal(
            "A default of DefaultIfEmpty is only translated for a single value that is never NULL in a matching row, as NULL cannot be told from the absence of a row.",
            exception.Message
        );
    }

    [Fact]
    public void RejectsExplicitDefaultOfLeftJoinedProjection()
    {
        Expression query = Customers
            .SelectMany(
                c =>
                    Orders
                        .Where(o => o.CustomerId == c.Id)
                        .Select(o => new ValueTuple<int>(o.Quantity))
                        .DefaultIfEmpty(new ValueTuple<int>(-1)),
                (c, o) => new { c.Id, o.Item1 }
            )
            .Expression;

        _ = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void LeftJoinsPagedProjectionWithoutHiddenOrderingKey()
    {
        Expression query = Customers
            .GroupJoin(
                Orders
                    .OrderBy(o => o.CreatedAt)
                    .Select(o => new { o.CustomerId, o.Total })
                    .Take(5),
                c => c.Id,
                x => x.CustomerId,
                (c, xs) => new { c, xs }
            )
            .SelectMany(
                y => y.xs.DefaultIfEmpty(),
                (y, x) => new { y.c.Name, Total = (decimal?)x!.Total }
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
                    ORDER BY t1."CreatedAt"
                    LIMIT 5
                ) AS t2 ON t0."Id" = t2."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ChecksLeftJoinedEntityForNullByRequiredColumn()
    {
        Expression query = Customers
            .GroupJoin(Orders, c => c.Id, o => o.CustomerId, (c, os) => new { c, os })
            .SelectMany(x => x.os.DefaultIfEmpty(), (x, o) => new { x.c.Name, o })
            .Where(y => y.o == null)
            .Select(y => y.Name)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name"
                FROM "Customer" AS t0
                LEFT JOIN "Order" AS t1 ON t0."Id" = t1."CustomerId"
                WHERE t1."Id" IS NULL
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CountsMatchedLeftJoinedEntitiesPerGroup()
    {
        Expression query = Customers
            .GroupJoin(Orders, c => c.Id, o => o.CustomerId, (c, os) => new { c, os })
            .SelectMany(x => x.os.DefaultIfEmpty(), (x, o) => new { x.c.Region, o })
            .GroupBy(y => y.Region)
            .Select(g => new { g.Key, Orders = g.Count(y => y.o != null) })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Region" AS "Key", CAST(COUNT(*) FILTER (WHERE t1."Id" IS NOT NULL) AS integer) AS "Orders"
                FROM "Customer" AS t0
                LEFT JOIN "Order" AS t1 ON t0."Id" = t1."CustomerId"
                GROUP BY t0."Region"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ChecksLeftJoinedEntityWithoutRequiredColumnByEveryColumn()
    {
        Expression query = Orders
            .SelectMany(
                o =>
                    new CapturingQueryable<Sticker>()
                        .Where(s => s.OrderId == o.Id)
                        .DefaultIfEmpty(),
                (o, s) => new { o.Number, s }
            )
            .Select(x => new
            {
                x.Number,
                Missing = x.s == null,
                Present = x.s != null,
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Number", (t1."OrderId" IS NULL) AND (t1."Text" IS NULL) AS "Missing", (t1."OrderId" IS NOT NULL) OR (t1."Text" IS NOT NULL) AS "Present"
                FROM "Order" AS t0
                LEFT JOIN "Sticker" AS t1 ON t1."OrderId" = t0."Id"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void KnowsAnInnerJoinedEntityIsNeverNull()
    {
        Expression query = Customers
            .Join(Orders, c => c.Id, o => o.CustomerId, (c, o) => new { c, o })
            .Select(x => new
            {
                x.c.Name,
                Missing = x.o == null,
                Present = x.o != null,
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", FALSE AS "Missing", TRUE AS "Present"
                FROM "Customer" AS t0
                INNER JOIN "Order" AS t1 ON t0."Id" = t1."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
