namespace Linq.Expressions.Translation.PostgreSql.Tests.Operators;

public sealed record ExceptByTests
{
    [Fact]
    public void ExcludesKeysOfSubquery()
    {
        Expression query = Customers
            .ExceptBy(Orders.Select(o => o.CustomerId), c => c.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT DISTINCT ON (t0."Id") t0."Id", t0."Name", t0."Email", t0."Region",
                t0."IsActive"
                FROM "Customer" AS t0
                WHERE NOT (t0."Id" IN (
                    SELECT t1."CustomerId"
                    FROM "Order" AS t1
                ))
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ExcludesNullKeyMatchingNullOfSubquery()
    {
        IQueryable<string?> blocked = Customers
            .Where(c => !c.IsActive)
            .Select(c => c.Email);
        Expression query = Orders.ExceptBy(blocked, o => o.Note).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT DISTINCT ON (t0."Note") t0."Id", t0."CustomerId", t0."Number",
                t0."Note", t0."Total", t0."Discount", t0."Quantity", t0."Priority",
                t0."Points", t0."Weight", t0."IsPaid", t0."IsGift", t0."Status",
                t0."Options", t0."CreatedAt", t0."PaidAt", t0."ShippedOn",
                t0."DeliveryTime", t0."TrackingId", t0."Tags"
                FROM "Order" AS t0
                WHERE NOT (COALESCE(t0."Note" IN (
                    SELECT t1."Email"
                    FROM "Customer" AS t1
                    WHERE NOT t1."IsActive"
                ), FALSE) OR ((t0."Note" IS NULL) AND EXISTS (
                    SELECT 1
                    FROM "Customer" AS t2
                    WHERE NOT t2."IsActive" AND (t2."Email" IS NULL)
                )))
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ExcludesKeysOfCollection()
    {
        string[] regions = ["EU", "US"];
        Expression query = Customers.ExceptBy(regions, c => c.Region).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT DISTINCT ON (t0."Region") t0."Id", t0."Name", t0."Email",
                t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                WHERE NOT (t0."Region" IN ('EU', 'US'))
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ExcludesNullKeyMatchingNullOfCollection()
    {
        string?[] emails = ["a@x.io", null];
        Expression query = Customers.ExceptBy(emails, c => c.Email).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT DISTINCT ON (t0."Email") t0."Id", t0."Name", t0."Email",
                t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                WHERE NOT (COALESCE(t0."Email" IN ('a@x.io'), FALSE) OR (t0."Email" IS NULL))
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ExcludesNothingForEmptyCollection()
    {
        string[] none = [];
        Expression query = Customers.ExceptBy(none, c => c.Region).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT DISTINCT ON (t0."Region") t0."Id", t0."Name", t0."Email",
                t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                WHERE NOT FALSE
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ProjectsRowKeptPerKey()
    {
        string[] regions = ["EU", "US"];
        Expression query = Customers
            .Select(c => new { c.Region, c.Name })
            .ExceptBy(regions, x => x.Region)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT DISTINCT ON (t0."Region") t0."Region", t0."Name"
                FROM "Customer" AS t0
                WHERE NOT (t0."Region" IN ('EU', 'US'))
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void KeepsFirstRowPerKeyInSourceOrder()
    {
        Expression query = Orders
            .OrderByDescending(o => o.CreatedAt)
            .ExceptBy(Customers.Select(c => c.Id), o => o.CustomerId)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t2."Id", t2."CustomerId", t2."Number", t2."Note", t2."Total",
                t2."Discount", t2."Quantity", t2."Priority", t2."Points", t2."Weight",
                t2."IsPaid", t2."IsGift", t2."Status", t2."Options", t2."CreatedAt",
                t2."PaidAt", t2."ShippedOn", t2."DeliveryTime", t2."TrackingId", t2."Tags"
                FROM (
                    SELECT DISTINCT ON (t0."CustomerId") t0."Id", t0."CustomerId",
                    t0."Number", t0."Note", t0."Total", t0."Discount", t0."Quantity",
                    t0."Priority", t0."Points", t0."Weight", t0."IsPaid", t0."IsGift",
                    t0."Status", t0."Options", t0."CreatedAt", t0."PaidAt",
                    t0."ShippedOn", t0."DeliveryTime", t0."TrackingId", t0."Tags"
                    FROM "Order" AS t0
                    WHERE NOT (t0."CustomerId" IN (
                        SELECT t1."Id"
                        FROM "Customer" AS t1
                    ))
                    ORDER BY t0."CustomerId", t0."CreatedAt" DESC
                ) AS t2
                ORDER BY t2."CreatedAt" DESC
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersPagedSourceInDerivedTable()
    {
        string[] regions = ["EU", "US"];
        Expression query = Customers.Take(5).ExceptBy(regions, c => c.Region).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT DISTINCT ON (t1."Region") t1."Id", t1."Name", t1."Email",
                t1."Region", t1."IsActive"
                FROM (
                    SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                    FROM "Customer" AS t0
                    LIMIT 5
                ) AS t1
                WHERE NOT (t1."Region" IN ('EU', 'US'))
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersRowsKeptPerKeyInDerivedTable()
    {
        string[] regions = ["EU", "US"];
        Expression query = Customers
            .ExceptBy(regions, c => c.Region)
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
                    WHERE NOT (t0."Region" IN ('EU', 'US'))
                ) AS t1
                WHERE t1."IsActive"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
