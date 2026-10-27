namespace Linq.Expressions.Translation.PostgreSql.Tests.Operators;

public sealed record MinByMaxByTests
{
    [Fact]
    public void TakesRowWithSmallestKey()
    {
        Expression query = Orders.Executed(q => q.MinBy(o => o.Total));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."CustomerId", t0."Number", t0."Note", t0."Total",
                t0."Discount", t0."Quantity", t0."Priority", t0."Points", t0."Weight",
                t0."IsPaid", t0."IsGift", t0."Status", t0."Options", t0."CreatedAt",
                t0."PaidAt", t0."ShippedOn", t0."DeliveryTime", t0."TrackingId", t0."Tags"
                FROM "Order" AS t0
                ORDER BY t0."Total"
                LIMIT 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesRowWithLargestKey()
    {
        Expression query = Orders.Executed(q => q.MaxBy(o => o.Total));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."CustomerId", t0."Number", t0."Note", t0."Total",
                t0."Discount", t0."Quantity", t0."Priority", t0."Points", t0."Weight",
                t0."IsPaid", t0."IsGift", t0."Status", t0."Options", t0."CreatedAt",
                t0."PaidAt", t0."ShippedOn", t0."DeliveryTime", t0."TrackingId", t0."Tags"
                FROM "Order" AS t0
                ORDER BY t0."Total" DESC
                LIMIT 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SkipsNullKeysForSmallest()
    {
        Expression query = Orders.Executed(q => q.MinBy(o => o.Discount));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."CustomerId", t0."Number", t0."Note", t0."Total",
                t0."Discount", t0."Quantity", t0."Priority", t0."Points", t0."Weight",
                t0."IsPaid", t0."IsGift", t0."Status", t0."Options", t0."CreatedAt",
                t0."PaidAt", t0."ShippedOn", t0."DeliveryTime", t0."TrackingId", t0."Tags"
                FROM "Order" AS t0
                ORDER BY t0."Discount" NULLS LAST
                LIMIT 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SkipsNullKeysForLargest()
    {
        Expression query = Orders.Executed(q => q.MaxBy(o => o.Discount));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."CustomerId", t0."Number", t0."Note", t0."Total",
                t0."Discount", t0."Quantity", t0."Priority", t0."Points", t0."Weight",
                t0."IsPaid", t0."IsGift", t0."Status", t0."Options", t0."CreatedAt",
                t0."PaidAt", t0."ShippedOn", t0."DeliveryTime", t0."TrackingId", t0."Tags"
                FROM "Order" AS t0
                ORDER BY t0."Discount" DESC NULLS LAST
                LIMIT 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void BreaksTiesBySourceOrder()
    {
        Expression query = Orders
            .Where(o => o.CustomerId == 1)
            .OrderBy(o => o.Id)
            .Executed(q => q.MaxBy(o => o.CreatedAt));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."CustomerId", t0."Number", t0."Note", t0."Total",
                t0."Discount", t0."Quantity", t0."Priority", t0."Points", t0."Weight",
                t0."IsPaid", t0."IsGift", t0."Status", t0."Options", t0."CreatedAt",
                t0."PaidAt", t0."ShippedOn", t0."DeliveryTime", t0."TrackingId", t0."Tags"
                FROM "Order" AS t0
                WHERE t0."CustomerId" = 1
                ORDER BY t0."CreatedAt" DESC, t0."Id"
                LIMIT 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void BreaksTiesByReversedSourceOrder()
    {
        Expression query = Orders
            .OrderBy(o => o.Id)
            .Reverse()
            .Executed(q => q.MinBy(o => o.Discount));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."CustomerId", t0."Number", t0."Note", t0."Total",
                t0."Discount", t0."Quantity", t0."Priority", t0."Points", t0."Weight",
                t0."IsPaid", t0."IsGift", t0."Status", t0."Options", t0."CreatedAt",
                t0."PaidAt", t0."ShippedOn", t0."DeliveryTime", t0."TrackingId", t0."Tags"
                FROM "Order" AS t0
                ORDER BY t0."Discount" NULLS LAST, t0."Id" DESC
                LIMIT 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ProjectsRowWithSmallestKey()
    {
        Expression query = Orders
            .Select(o => new { o.Id, o.Total })
            .Executed(q => q.MinBy(x => x.Total));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Total"
                FROM "Order" AS t0
                ORDER BY t0."Total"
                LIMIT 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesScalarWithSmallestComputedKey()
    {
        Expression query = Customers
            .Select(c => c.Name)
            .Executed(q => q.MinBy(n => n.Length));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name"
                FROM "Customer" AS t0
                ORDER BY LENGTH(t0."Name")
                LIMIT 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesGroupWithLargestAggregate()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new { g.Key, Total = g.Sum(o => o.Total) })
            .Executed(q => q.MaxBy(x => x.Total));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId" AS "Key", COALESCE(SUM(t0."Total"), 0) AS "Total"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                ORDER BY COALESCE(SUM(t0."Total"), 0) DESC
                LIMIT 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesRowOfDistinctSourceInDerivedTable()
    {
        Expression query = Customers
            .Select(c => c.Region)
            .Distinct()
            .Executed(q => q.MinBy(r => r.Length));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Region"
                FROM (
                    SELECT DISTINCT t0."Region"
                    FROM "Customer" AS t0
                ) AS t1
                ORDER BY LENGTH(t1."Region")
                LIMIT 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesLargestValueAsSubquery()
    {
        Expression query = Customers
            .Select(c =>
                Orders.Where(o => o.CustomerId == c.Id).Select(o => o.Total).MaxBy(t => t)
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT (
                    SELECT t1."Total"
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                    ORDER BY t1."Total" DESC
                    LIMIT 1
                )
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OrdersTupleKeyMemberwiseWithNullsFirst()
    {
        Expression query = Orders.Executed(q =>
            q.MinBy(o => new ValueTuple<int?, decimal>(o.Priority, o.Total))
        );

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."CustomerId", t0."Number", t0."Note", t0."Total",
                t0."Discount", t0."Quantity", t0."Priority", t0."Points", t0."Weight",
                t0."IsPaid", t0."IsGift", t0."Status", t0."Options", t0."CreatedAt",
                t0."PaidAt", t0."ShippedOn", t0."DeliveryTime", t0."TrackingId", t0."Tags"
                FROM "Order" AS t0
                ORDER BY t0."Priority" NULLS FIRST, t0."Total"
                LIMIT 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OrdersTupleKeyMemberwiseDescending()
    {
        Expression query = Orders.Executed(q =>
            q.MaxBy(o => new ValueTuple<int?, decimal>(o.Priority, o.Total))
        );

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."CustomerId", t0."Number", t0."Note", t0."Total",
                t0."Discount", t0."Quantity", t0."Priority", t0."Points", t0."Weight",
                t0."IsPaid", t0."IsGift", t0."Status", t0."Options", t0."CreatedAt",
                t0."PaidAt", t0."ShippedOn", t0."DeliveryTime", t0."TrackingId", t0."Tags"
                FROM "Order" AS t0
                ORDER BY t0."Priority" DESC NULLS LAST, t0."Total" DESC
                LIMIT 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OrdersStringKeyOrdinally()
    {
        Expression query = Customers.Executed(q =>
            q.MinBy(c => c.Name, StringComparer.Ordinal)
        );

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                ORDER BY t0."Name" COLLATE "C"
                LIMIT 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RejectsOtherComparer()
    {
        Expression query = Customers.Executed(q =>
            q.MaxBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
        );

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("MaxBy", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsTupleKeyWithGuidMember()
    {
        Expression query = Orders.Executed(q =>
            q.MaxBy(o => new ValueTuple<int, Guid>(o.Id, o.TrackingId))
        );

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains(
            "'MaxBy' of Guid values",
            exception.Message,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void RejectsGuidKey()
    {
        Expression query = Orders.Executed(q => q.MinBy(o => o.TrackingId));

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains(
            "'MinBy' of Guid values",
            exception.Message,
            StringComparison.Ordinal
        );
    }
}
