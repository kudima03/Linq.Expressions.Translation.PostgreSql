#pragma warning disable xUnit1004 // skipped tests record SQL the translator does not produce yet

namespace Linq.Expressions.Translation.PostgreSql.Tests.Operators;

public sealed record DistinctByTests
{
    [Fact(Skip = "DistinctBy is not translated (#46).")]
    public void KeepsOneRowPerKey()
    {
        Expression query = Customers.DistinctBy(c => c.Region).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT DISTINCT ON (t0."Region") t0."Id", t0."Name", t0."Email",
                t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "DistinctBy is not translated (#46).")]
    public void KeepsOneRowPerValueTupleKey()
    {
        Expression query = Orders
            .DistinctBy(o => new ValueTuple<int?, string?>(o.Priority, o.Note))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT DISTINCT ON (t0."Priority", t0."Note") t0."Id", t0."CustomerId",
                t0."Number", t0."Note", t0."Total", t0."Discount", t0."Quantity",
                t0."Priority", t0."Points", t0."Weight", t0."IsPaid", t0."IsGift",
                t0."Status", t0."Options", t0."CreatedAt", t0."PaidAt", t0."ShippedOn",
                t0."DeliveryTime", t0."TrackingId", t0."Tags"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "DistinctBy is not translated (#46).")]
    public void KeepsOneRowPerAnonymousKey()
    {
        Expression query = Customers
            .DistinctBy(c => new { c.Region, c.IsActive })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT DISTINCT ON (t0."Region", t0."IsActive") t0."Id", t0."Name",
                t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "DistinctBy is not translated (#46).")]
    public void KeepsFirstRowPerKeyOfOrderLedByKey()
    {
        Expression query = Orders
            .OrderBy(o => o.CustomerId)
            .ThenByDescending(o => o.CreatedAt)
            .DistinctBy(o => o.CustomerId)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT DISTINCT ON (t0."CustomerId") t0."Id", t0."CustomerId",
                t0."Number", t0."Note", t0."Total", t0."Discount", t0."Quantity",
                t0."Priority", t0."Points", t0."Weight", t0."IsPaid", t0."IsGift",
                t0."Status", t0."Options", t0."CreatedAt", t0."PaidAt", t0."ShippedOn",
                t0."DeliveryTime", t0."TrackingId", t0."Tags"
                FROM "Order" AS t0
                ORDER BY t0."CustomerId", t0."CreatedAt" DESC
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "DistinctBy is not translated (#46).")]
    public void KeepsLatestOrderPerCustomerInSourceOrder()
    {
        Expression query = Orders
            .OrderByDescending(o => o.CreatedAt)
            .DistinctBy(o => o.CustomerId)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Id", t1."CustomerId", t1."Number", t1."Note", t1."Total",
                t1."Discount", t1."Quantity", t1."Priority", t1."Points", t1."Weight",
                t1."IsPaid", t1."IsGift", t1."Status", t1."Options", t1."CreatedAt",
                t1."PaidAt", t1."ShippedOn", t1."DeliveryTime", t1."TrackingId", t1."Tags"
                FROM (
                    SELECT DISTINCT ON (t0."CustomerId") t0."Id", t0."CustomerId",
                    t0."Number", t0."Note", t0."Total", t0."Discount", t0."Quantity",
                    t0."Priority", t0."Points", t0."Weight", t0."IsPaid", t0."IsGift",
                    t0."Status", t0."Options", t0."CreatedAt", t0."PaidAt",
                    t0."ShippedOn", t0."DeliveryTime", t0."TrackingId", t0."Tags"
                    FROM "Order" AS t0
                    ORDER BY t0."CustomerId", t0."CreatedAt" DESC
                ) AS t1
                ORDER BY t1."CreatedAt" DESC
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "DistinctBy is not translated (#46).")]
    public void ProjectsLatestOrderPerCustomerInSourceOrder()
    {
        Expression query = Orders
            .OrderByDescending(o => o.CreatedAt)
            .DistinctBy(o => o.CustomerId)
            .Select(o => new { o.CustomerId, o.Id })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."CustomerId", t1."Id"
                FROM (
                    SELECT DISTINCT ON (t0."CustomerId") t0."Id", t0."CustomerId",
                    t0."Number", t0."Note", t0."Total", t0."Discount", t0."Quantity",
                    t0."Priority", t0."Points", t0."Weight", t0."IsPaid", t0."IsGift",
                    t0."Status", t0."Options", t0."CreatedAt", t0."PaidAt",
                    t0."ShippedOn", t0."DeliveryTime", t0."TrackingId", t0."Tags"
                    FROM "Order" AS t0
                    ORDER BY t0."CustomerId", t0."CreatedAt" DESC
                ) AS t1
                ORDER BY t1."CreatedAt" DESC
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "DistinctBy is not translated (#46).")]
    public void ProjectsRowKeptPerKey()
    {
        Expression query = Customers
            .DistinctBy(c => c.Region)
            .Select(c => c.Name)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT DISTINCT ON (t0."Region") t0."Name"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "DistinctBy is not translated (#46).")]
    public void FiltersRowsKeptPerKeyInDerivedTable()
    {
        Expression query = Customers
            .DistinctBy(c => c.Region)
            .Where(c => c.IsActive)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Id", t1."Name", t1."Email", t1."Region", t1."IsActive"
                FROM (
                    SELECT DISTINCT ON (t0."Region") t0."Id", t0."Name", t0."Email",
                    t0."Region", t0."IsActive"
                    FROM "Customer" AS t0
                ) AS t1
                WHERE t1."IsActive"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
