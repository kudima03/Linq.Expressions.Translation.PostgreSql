namespace Linq.Expressions.Translation.PostgreSql.Tests.Expressions;

public sealed record ProjectionShapeTests
{
    public sealed record Summary(int Id, string Number);

    public sealed record Wrapper(Summary Summary, decimal Total);

    public sealed record Detail
    {
        public Summary Summary { get; init; } = new(0, string.Empty);

        public decimal Total { get; init; }
    }

    [Fact]
    public void ProjectsRecordThroughConstructor()
    {
        Expression query = Orders.Select(o => new Summary(o.Id, o.Number)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Number"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AliasesComputedRecordArguments()
    {
        Expression query = Orders
            .Select(o => new Summary(o.Id + 1, o.Number.ToUpper()))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id" + 1 AS "Id", UPPER(t0."Number") AS "Number"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FlattensNestedRecord()
    {
        Expression query = Orders
            .Select(o => new Wrapper(new Summary(o.Id, o.Number), o.Total))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id" AS "Summary.Id", t0."Number" AS "Summary.Number",
                t0."Total"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FlattensNestedMemberInitializer()
    {
        Expression query = Orders
            .Select(o => new Detail
            {
                Summary = new Summary(o.Id, o.Number),
                Total = o.Total,
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id" AS "Summary.Id", t0."Number" AS "Summary.Number",
                t0."Total"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FlattensDeeplyNestedAnonymousTypes()
    {
        Expression query = Orders
            .Select(o => new { o.Id, A = new { B = new { o.Number, o.Total } } })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Number" AS "A.B.Number", t0."Total" AS "A.B.Total"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ProjectsEntityInsideAnonymousType()
    {
        Expression query = Orders
            .Select(o => new { Order = o, Doubled = o.Quantity * 2 })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id" AS "Order.Id", t0."CustomerId" AS "Order.CustomerId",
                t0."Number" AS "Order.Number", t0."Note" AS "Order.Note",
                t0."Total" AS "Order.Total", t0."Discount" AS "Order.Discount",
                t0."Quantity" AS "Order.Quantity", t0."Priority" AS "Order.Priority",
                t0."Points" AS "Order.Points", t0."Weight" AS "Order.Weight",
                t0."IsPaid" AS "Order.IsPaid", t0."IsGift" AS "Order.IsGift",
                t0."Status" AS "Order.Status", t0."Options" AS "Order.Options",
                t0."CreatedAt" AS "Order.CreatedAt", t0."PaidAt" AS "Order.PaidAt",
                t0."ShippedOn" AS "Order.ShippedOn",
                t0."DeliveryTime" AS "Order.DeliveryTime",
                t0."TrackingId" AS "Order.TrackingId", t0."Tags" AS "Order.Tags",
                t0."Quantity" * 2 AS "Doubled"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ProjectsMemberOfAnonymousMember()
    {
        Expression query = Orders
            .Select(o => new { o.Id, Info = new { o.Number } }.Info.Number)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Number"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersOnMemberOfEntityInsideAnonymousType()
    {
        Expression query = Orders
            .Select(o => new { Order = o, Doubled = o.Quantity * 2 })
            .Where(x => x.Order.IsPaid)
            .OrderBy(x => x.Doubled)
            .Select(x => x.Order.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."IsPaid"
                ORDER BY t0."Quantity" * 2
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersOnNestedAnonymousMember()
    {
        Expression query = Orders
            .Select(o => new { o.Id, Info = new { o.Number, o.Total } })
            .Where(x => x.Info.Total > 5m)
            .Select(x => x.Info.Number)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Number"
                FROM "Order" AS t0
                WHERE t0."Total" > 5
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OrdersByNestedRecordMember()
    {
        Expression query = Orders
            .Select(o => new Wrapper(new Summary(o.Id, o.Number), o.Total))
            .OrderBy(w => w.Summary.Number)
            .Select(w => w.Summary.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                ORDER BY t0."Number"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ReusesProjectedMemberInSecondSelect()
    {
        Expression query = Orders
            .Select(o => new { o.Id, Net = o.Total - o.Discount })
            .Select(x => new { x.Id, Gross = x.Net * 1.2m })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", (t0."Total" - t0."Discount") * 1.2 AS "Gross"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ReusesProjectedMemberInSecondSelectAndFilter()
    {
        Expression query = Orders
            .Select(o => new { o.Id, Net = o.Total - o.Discount })
            .Where(x => x.Net > 10m)
            .Select(x => x.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."Total" - t0."Discount") > 10
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ChainsThreeProjections()
    {
        Expression query = Orders
            .Select(o => new { o.Id, o.Quantity })
            .Select(x => new { x.Id, Double = x.Quantity * 2 })
            .Select(y => y.Double + y.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT (t0."Quantity" * 2) + t0."Id"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ChainsMembersOfDateParts()
    {
        Expression query = Orders.Select(o => o.CreatedAt.Date.Year).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(EXTRACT(YEAR FROM DATE_TRUNC('day',
                t0."CreatedAt")) AS integer)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ChainsMembersOfStringFunctions()
    {
        Expression query = Orders
            .Select(o => o.Number.Trim().ToUpper().Length)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                $"""
                SELECT LENGTH(UPPER(BTRIM(t0."Number", {Sql.WhiteSpace})))
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ChainsMembersOfNullableValue()
    {
        Expression query = Orders.Select(o => o.PaidAt!.Value.Year).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(EXTRACT(YEAR FROM t0."PaidAt") AS integer)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ChainsMembersOfDateOnlyConversion()
    {
        Expression query = Orders
            .Select(o => DateOnly.FromDateTime(o.CreatedAt).Month)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(EXTRACT(MONTH FROM CAST(t0."CreatedAt" AS date)) AS integer)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ProjectsLiteralsOfEveryKind()
    {
        Expression query = Orders
            .Select(o => new
            {
                Int = 1,
                Long = 2L,
                Dec = 3.5m,
                Dbl = 4.5,
                Str = "s",
                Flag = true,
                Nothing = (string?)null,
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT 1 AS "Int", 2 AS "Long", 3.5 AS "Dec", 4.5 AS "Dbl",
                's' AS "Str", TRUE AS "Flag", NULL AS "Nothing"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ProjectsFromJoinIntoNestedRecord()
    {
        Expression query = Customers
            .Join(
                Orders,
                c => c.Id,
                o => o.CustomerId,
                (c, o) => new Wrapper(new Summary(c.Id, o.Number), o.Total)
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id" AS "Summary.Id", t1."Number" AS "Summary.Number",
                t1."Total"
                FROM "Customer" AS t0
                INNER JOIN "Order" AS t1 ON t0."Id" = t1."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ProjectsFromGroupIntoRecord()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new Summary(g.Key, g.Max(o => o.Number)!))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId" AS "Id", MAX(t0."Number") AS "Number"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
