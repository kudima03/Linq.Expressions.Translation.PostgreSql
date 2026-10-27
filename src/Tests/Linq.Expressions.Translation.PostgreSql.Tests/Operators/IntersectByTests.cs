namespace Linq.Expressions.Translation.PostgreSql.Tests.Operators;

public sealed record IntersectByTests
{
    [Fact]
    public void KeepsKeysOfSubquery()
    {
        Expression query = Customers
            .IntersectBy(Orders.Select(o => o.CustomerId), c => c.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT DISTINCT ON (t0."Id") t0."Id", t0."Name", t0."Email", t0."Region",
                t0."IsActive"
                FROM "Customer" AS t0
                WHERE t0."Id" IN (
                    SELECT t1."CustomerId"
                    FROM "Order" AS t1
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void MatchesNullKeyWithNullOfSubquery()
    {
        Expression query = Orders
            .IntersectBy(Customers.Select(c => c.Email), o => o.Note)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT DISTINCT ON (t0."Note") t0."Id", t0."CustomerId", t0."Number",
                t0."Note", t0."Total", t0."Discount", t0."Quantity", t0."Priority",
                t0."Points", t0."Weight", t0."IsPaid", t0."IsGift", t0."Status",
                t0."Options", t0."CreatedAt", t0."PaidAt", t0."ShippedOn",
                t0."DeliveryTime", t0."TrackingId", t0."Tags"
                FROM "Order" AS t0
                WHERE (t0."Note" IN (
                    SELECT t1."Email"
                    FROM "Customer" AS t1
                )) OR ((t0."Note" IS NULL) AND EXISTS (
                    SELECT 1
                    FROM "Customer" AS t2
                    WHERE t2."Email" IS NULL
                ))
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void KeepsKeysOfCollection()
    {
        string[] regions = ["EU", "US"];
        Expression query = Customers.IntersectBy(regions, c => c.Region).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT DISTINCT ON (t0."Region") t0."Id", t0."Name", t0."Email",
                t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                WHERE t0."Region" IN ('EU', 'US')
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void MatchesNullKeyWithNullOfCollection()
    {
        int?[] priorities = [1, null];
        Expression query = Orders.IntersectBy(priorities, o => o.Priority).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT DISTINCT ON (t0."Priority") t0."Id", t0."CustomerId", t0."Number",
                t0."Note", t0."Total", t0."Discount", t0."Quantity", t0."Priority",
                t0."Points", t0."Weight", t0."IsPaid", t0."IsGift", t0."Status",
                t0."Options", t0."CreatedAt", t0."PaidAt", t0."ShippedOn",
                t0."DeliveryTime", t0."TrackingId", t0."Tags"
                FROM "Order" AS t0
                WHERE (t0."Priority" IN (1)) OR (t0."Priority" IS NULL)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ProjectsRowKeptPerKey()
    {
        IQueryable<string?> blocked = Customers
            .Where(c => !c.IsActive)
            .Select(c => c.Email);
        Expression query = Customers
            .Select(c => new { c.Email, c.Name })
            .IntersectBy(blocked, x => x.Email)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT DISTINCT ON (t0."Email") t0."Email", t0."Name"
                FROM "Customer" AS t0
                WHERE (t0."Email" IN (
                    SELECT t1."Email"
                    FROM "Customer" AS t1
                    WHERE NOT t1."IsActive"
                )) OR ((t0."Email" IS NULL) AND EXISTS (
                    SELECT 1
                    FROM "Customer" AS t2
                    WHERE NOT t2."IsActive" AND (t2."Email" IS NULL)
                ))
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CountsRowsKeptPerKey()
    {
        string[] regions = ["EU", "US"];
        Expression query = Customers
            .IntersectBy(regions, c => c.Region)
            .Executed(q => q.Count());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COUNT(*) AS integer)
                FROM (
                    SELECT DISTINCT ON (t0."Region") t0."Id", t0."Name", t0."Email",
                    t0."Region", t0."IsActive"
                    FROM "Customer" AS t0
                    WHERE t0."Region" IN ('EU', 'US')
                ) AS t1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RejectsCompositeKey()
    {
        Expression query = Customers
            .IntersectBy(
                Customers.Select(c => new { c.Region, c.IsActive }),
                c => new { c.Region, c.IsActive }
            )
            .Expression;

        _ = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
    }
}
