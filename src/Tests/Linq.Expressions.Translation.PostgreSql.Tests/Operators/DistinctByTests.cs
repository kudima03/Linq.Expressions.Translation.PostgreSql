namespace Linq.Expressions.Translation.PostgreSql.Tests.Operators;

public sealed record DistinctByTests
{
    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
    public void TakesRowsKeptPerKey()
    {
        Expression query = Customers.DistinctBy(c => c.Region).Take(5).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT DISTINCT ON (t0."Region") t0."Id", t0."Name", t0."Email",
                t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                LIMIT 5
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsFirstRowKeptPerKey()
    {
        Expression query = Customers
            .DistinctBy(c => c.Region)
            .Select(c => c.Name)
            .Executed(q => q.First());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT DISTINCT ON (t0."Region") t0."Name"
                FROM "Customer" AS t0
                LIMIT 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CountsRowsKeptPerKeyInDerivedTable()
    {
        Expression query = Customers.DistinctBy(c => c.Region).Executed(q => q.Count());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COUNT(*) AS integer)
                FROM (
                    SELECT DISTINCT ON (t0."Region") t0."Id", t0."Name", t0."Email",
                    t0."Region", t0."IsActive"
                    FROM "Customer" AS t0
                ) AS t1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OrdersRowsKeptPerKeyInDerivedTable()
    {
        Expression query = Customers
            .DistinctBy(c => c.Region)
            .OrderBy(c => c.Name)
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
                ORDER BY t1."Name"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void MakesProjectedRowsKeptPerKeyDistinctInDerivedTable()
    {
        Expression query = Customers
            .DistinctBy(c => c.Region)
            .Select(c => c.Name)
            .Distinct()
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT DISTINCT t1."Name"
                FROM (
                    SELECT DISTINCT ON (t0."Region") t0."Name"
                    FROM "Customer" AS t0
                ) AS t1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void KeepsOneRowPerKeyOfDistinctRowsInDerivedTable()
    {
        Expression query = Customers
            .Select(c => new { c.Region, c.IsActive })
            .Distinct()
            .DistinctBy(x => x.Region)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT DISTINCT ON (t1."Region") t1."Region", t1."IsActive"
                FROM (
                    SELECT DISTINCT t0."Region", t0."IsActive"
                    FROM "Customer" AS t0
                ) AS t1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void KeepsOneRowPerKeyOfPageInDerivedTable()
    {
        Expression query = Customers.Take(10).DistinctBy(c => c.Region).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT DISTINCT ON (t1."Region") t1."Id", t1."Name", t1."Email",
                t1."Region", t1."IsActive"
                FROM (
                    SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                    FROM "Customer" AS t0
                    LIMIT 10
                ) AS t1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void KeepsOneRowPerConstantKey()
    {
        Expression query = Customers.DistinctBy(c => 1).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT DISTINCT ON (CAST(1 AS integer)) t0."Id", t0."Name", t0."Email",
                t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void KeepsOneRowPerKeyOfGroups()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new { g.Key, Total = g.Sum(o => o.Total) })
            .DistinctBy(x => x.Total)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT DISTINCT ON (COALESCE(SUM(t0."Total"), 0)) t0."CustomerId" AS "Key",
                COALESCE(SUM(t0."Total"), 0) AS "Total"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void KeepsFirstRowPerCompositeKeyOfOrderLedByKeys()
    {
        Expression query = Orders
            .OrderBy(o => o.Priority)
            .ThenBy(o => o.Note)
            .DistinctBy(o => new { o.Note, o.Priority })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT DISTINCT ON (t0."Note", t0."Priority") t0."Id", t0."CustomerId",
                t0."Number", t0."Note", t0."Total", t0."Discount", t0."Quantity",
                t0."Priority", t0."Points", t0."Weight", t0."IsPaid", t0."IsGift",
                t0."Status", t0."Options", t0."CreatedAt", t0."PaidAt", t0."ShippedOn",
                t0."DeliveryTime", t0."TrackingId", t0."Tags"
                FROM "Order" AS t0
                ORDER BY t0."Priority" NULLS FIRST, t0."Note" NULLS FIRST
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RestoresOrderLedByPartOfCompositeKey()
    {
        Expression query = Orders
            .OrderBy(o => o.CustomerId)
            .DistinctBy(o => new { o.CustomerId, o.Priority })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Id", t1."CustomerId", t1."Number", t1."Note", t1."Total",
                t1."Discount", t1."Quantity", t1."Priority", t1."Points", t1."Weight",
                t1."IsPaid", t1."IsGift", t1."Status", t1."Options", t1."CreatedAt",
                t1."PaidAt", t1."ShippedOn", t1."DeliveryTime", t1."TrackingId", t1."Tags"
                FROM (
                    SELECT DISTINCT ON (t0."CustomerId", t0."Priority") t0."Id", t0."CustomerId",
                t0."Number", t0."Note", t0."Total", t0."Discount", t0."Quantity",
                t0."Priority", t0."Points", t0."Weight", t0."IsPaid", t0."IsGift",
                t0."Status", t0."Options", t0."CreatedAt", t0."PaidAt", t0."ShippedOn",
                t0."DeliveryTime", t0."TrackingId", t0."Tags"
                    FROM "Order" AS t0
                    ORDER BY t0."CustomerId", t0."Priority" NULLS FIRST, t0."CustomerId"
                ) AS t1
                ORDER BY t1."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersRowsKeptPerKeyOfOrderedSourceInDerivedTable()
    {
        Expression query = Orders
            .OrderBy(o => o.CustomerId)
            .DistinctBy(o => o.CustomerId)
            .Where(o => o.IsPaid)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Id"
                FROM (
                    SELECT DISTINCT ON (t0."CustomerId") t0."Id", t0."CustomerId",
                t0."Number", t0."Note", t0."Total", t0."Discount", t0."Quantity",
                t0."Priority", t0."Points", t0."Weight", t0."IsPaid", t0."IsGift",
                t0."Status", t0."Options", t0."CreatedAt", t0."PaidAt", t0."ShippedOn",
                t0."DeliveryTime", t0."TrackingId", t0."Tags"
                    FROM "Order" AS t0
                    ORDER BY t0."CustomerId"
                ) AS t1
                WHERE t1."IsPaid"
                ORDER BY t1."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ChecksAnyRowKeptPerKeyWithItsOrder()
    {
        Expression query = Orders
            .OrderBy(o => o.CustomerId)
            .DistinctBy(o => o.CustomerId)
            .Executed(q => q.Any());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT EXISTS (
                    SELECT DISTINCT ON (t0."CustomerId") 1
                    FROM "Order" AS t0
                    ORDER BY t0."CustomerId"
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsRowsKeptPerKeyAsDerivedTable()
    {
        Expression query = Customers
            .Join(
                Orders.DistinctBy(o => o.CustomerId),
                c => c.Id,
                o => o.CustomerId,
                (c, o) => new { c.Name, o.Number }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t2."Number"
                FROM "Customer" AS t0
                INNER JOIN (
                    SELECT DISTINCT ON (t1."CustomerId") t1."Id", t1."CustomerId",
                    t1."Number", t1."Note", t1."Total", t1."Discount", t1."Quantity",
                    t1."Priority", t1."Points", t1."Weight", t1."IsPaid", t1."IsGift",
                    t1."Status", t1."Options", t1."CreatedAt", t1."PaidAt",
                    t1."ShippedOn", t1."DeliveryTime", t1."TrackingId", t1."Tags"
                    FROM "Order" AS t1
                ) AS t2 ON t0."Id" = t2."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ParenthesizesOrderedRowsKeptPerKeyInSetOperation()
    {
        Expression query = Orders
            .OrderBy(o => o.CustomerId)
            .DistinctBy(o => o.CustomerId)
            .Select(o => o.Id)
            .Concat(Orders.Select(o => o.Id))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                (
                    SELECT DISTINCT ON (t0."CustomerId") t0."Id"
                    FROM "Order" AS t0
                    ORDER BY t0."CustomerId"
                )
                UNION ALL
                SELECT t1."Id"
                FROM "Order" AS t1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RejectsDistinctByWithComparer()
    {
        Expression query = Customers
            .DistinctBy(c => c.Region, StringComparer.OrdinalIgnoreCase)
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("DistinctBy", exception.Message, StringComparison.Ordinal);
    }
}
