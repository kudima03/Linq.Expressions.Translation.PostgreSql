#pragma warning disable xUnit1004 // skipped tests record SQL the translator does not produce yet
#pragma warning disable IDE0100 // the comparison with a boolean literal is under test

namespace Linq.Expressions.Translation.PostgreSql.Tests.Operators;

public sealed record FilteredLeftJoinTests
{
    [Fact(Skip = "A filter on the GroupJoin matches is not moved into ON (#48).")]
    public void LeftJoinsWithFilterOnMatches()
    {
        Expression query = Orders
            .GroupJoin(Customers, o => o.CustomerId, c => c.Id, (o, cs) => new { o, cs })
            .SelectMany(
                m => m.cs.Where(c => c.IsActive == true).DefaultIfEmpty(),
                (m, c) => new { m.o.Id, Name = c == null ? null : c.Name }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", CASE WHEN t1."Id" IS NULL THEN NULL ELSE t1."Name" END AS "Name"
                FROM "Order" AS t0
                LEFT JOIN "Customer" AS t1 ON (t0."CustomerId" = t1."Id") AND (t1."IsActive" = TRUE)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "Chained filters on the GroupJoin matches are not moved into ON (#48).")]
    public void LeftJoinsWithChainedFiltersOnMatches()
    {
        Expression query = Customers
            .GroupJoin(Orders, c => c.Id, o => o.CustomerId, (c, os) => new { c, os })
            .SelectMany(
                x => x.os.Where(o => o.IsPaid).Where(o => o.Total > 100).DefaultIfEmpty(),
                (x, o) => new { x.c.Name, OrderId = (int?)o!.Id }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t1."Id" AS "OrderId"
                FROM "Customer" AS t0
                LEFT JOIN "Order" AS t1 ON (t0."Id" = t1."CustomerId") AND t1."IsPaid" AND (t1."Total" > 100)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "A filter on the join into group is not moved into ON (#48).")]
    public void LeftJoinsWithFilterOnMatchesInQuerySyntax()
    {
        Expression query = (
            from o in Orders
            join c in Customers on o.CustomerId equals c.Id into cs
            from c in cs.Where(x => x.IsActive).DefaultIfEmpty()
            select new { o.Id, c.Name }
        ).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t1."Name"
                FROM "Order" AS t0
                LEFT JOIN "Customer" AS t1 ON (t0."CustomerId" = t1."Id") AND t1."IsActive"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "A correlated filter on the matches is not moved into ON (#48).")]
    public void LeftJoinsWithFilterCorrelatedToOuterRow()
    {
        Expression query = (
            from l in OrderLines
            join o in Orders on l.OrderId equals o.Id into os
            from o in os.Where(x => x.Quantity >= l.Quantity).DefaultIfEmpty()
            select new { l.ProductId, o.Number }
        ).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."ProductId", t1."Number"
                FROM "OrderLine" AS t0
                LEFT JOIN "Order" AS t1 ON (t0."OrderId" = t1."Id") AND (t1."Quantity" >= t0."Quantity")
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "A nullable equality on the matches is not moved into ON (#48).")]
    public void LeftJoinsWithNullableEqualityFilterOnMatches()
    {
        Expression query = Customers
            .GroupJoin(Orders, c => c.Id, o => o.CustomerId, (c, os) => new { c, os })
            .SelectMany(
                x => x.os.Where(o => o.IsGift == true).DefaultIfEmpty(),
                (x, o) => new { x.c.Name, OrderId = (int?)o!.Id }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t1."Id" AS "OrderId"
                FROM "Customer" AS t0
                LEFT JOIN "Order" AS t1 ON (t0."Id" = t1."CustomerId") AND (t1."IsGift" IS NOT DISTINCT FROM TRUE)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "A nullable inequality on the matches is not moved into ON (#48).")]
    public void LeftJoinsWithNullableInequalityFilterOnMatches()
    {
        Expression query = Customers
            .GroupJoin(Orders, c => c.Id, o => o.CustomerId, (c, os) => new { c, os })
            .SelectMany(
                x => x.os.Where(o => o.Discount != 5).DefaultIfEmpty(),
                (x, o) => new { x.c.Name, OrderId = (int?)o!.Id }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t1."Id" AS "OrderId"
                FROM "Customer" AS t0
                LEFT JOIN "Order" AS t1 ON (t0."Id" = t1."CustomerId") AND (t1."Discount" IS DISTINCT FROM 5)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
