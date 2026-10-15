namespace Linq.Expressions.Translation.PostgreSql.Tests.Expressions;

public sealed record GuidAndEnumTests
{
    [Fact]
    public void SelectsGuidColumn()
    {
        Expression query = Orders.Select(o => o.TrackingId).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."TrackingId"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsNewGuid()
    {
        Expression query = Orders.Select(o => Guid.NewGuid()).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT GEN_RANDOM_UUID()
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsNewGuidNextToColumn()
    {
        Expression query = Orders
            .Select(o => new { o.Id, Token = Guid.NewGuid() })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", GEN_RANDOM_UUID() AS "Token"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsEmptyGuid()
    {
        Expression query = Orders.Select(o => Guid.Empty).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT UUID '00000000-0000-0000-0000-000000000000'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsParsedGuid()
    {
        Expression query = Orders
            .Select(o => Guid.Parse("11111111-2222-3333-4444-555555555555"))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT UUID '11111111-2222-3333-4444-555555555555'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsConstructedGuid()
    {
        Expression query = Orders
            .Select(o => new Guid("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT UUID 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsCapturedGuid()
    {
        Guid id = Guid.Parse("11111111-2222-3333-4444-555555555555");

        Expression query = Orders.Select(o => id).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT UUID '11111111-2222-3333-4444-555555555555'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByGuidEquality()
    {
        Guid id = Guid.Parse("11111111-2222-3333-4444-555555555555");

        Expression query = Orders
            .Where(o => o.TrackingId == id)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."TrackingId" = UUID '11111111-2222-3333-4444-555555555555'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByGuidInequality()
    {
        Guid id = Guid.Parse("11111111-2222-3333-4444-555555555555");

        Expression query = Orders
            .Where(o => o.TrackingId != id)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."TrackingId" <> UUID '11111111-2222-3333-4444-555555555555'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByGuidEqualsMethod()
    {
        Guid id = Guid.Parse("11111111-2222-3333-4444-555555555555");

        Expression query = Orders
            .Where(o => o.TrackingId.Equals(id))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."TrackingId" = UUID '11111111-2222-3333-4444-555555555555'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByNewGuidComparison()
    {
        Expression query = Orders
            .Where(o => o.TrackingId == Guid.NewGuid())
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."TrackingId" = GEN_RANDOM_UUID()
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByGuidText()
    {
        Expression query = Orders
            .Where(o => o.TrackingId.ToString().StartsWith("ab"))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE CAST(t0."TrackingId" AS text) LIKE 'ab%'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OrdersByGuid()
    {
        Expression query = Orders.OrderBy(o => o.TrackingId).Select(o => o.Id).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                ORDER BY t0."TrackingId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void GroupsByGuid()
    {
        Expression query = Orders
            .GroupBy(o => o.TrackingId)
            .Select(g => new { g.Key, Count = g.Count() })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."TrackingId" AS "Key", CAST(COUNT(*) AS integer) AS "Count"
                FROM "Order" AS t0
                GROUP BY t0."TrackingId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsDistinctGuids()
    {
        Expression query = Orders.Select(o => o.TrackingId).Distinct().Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT DISTINCT t0."TrackingId"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsEnumColumn()
    {
        Expression query = Orders.Select(o => o.Status).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Status"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsEnumConstant()
    {
        Expression query = Orders.Select(o => OrderStatus.Cancelled).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT 3
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsDefaultEnum()
    {
        Expression query = Orders.Select(o => default(OrderStatus)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT 0
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsCapturedEnum()
    {
        OrderStatus status = OrderStatus.Shipped;

        Expression query = Orders.Select(o => status).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT 2
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsFlagsConstant()
    {
        Expression query = Orders
            .Select(o => ShippingOptions.Express | ShippingOptions.Fragile)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT 5
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsEnumNextToColumn()
    {
        Expression query = Orders.Select(o => new { o.Id, o.Status }).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Status"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsEnumComparisonAsValue()
    {
        Expression query = Orders.Select(o => o.Status == OrderStatus.Paid).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Status" = 1
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsEnumArithmetic()
    {
        Expression query = Orders.Select(o => (int)o.Status + 1).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Status" + 1
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsLabelOfEnumWithConditional()
    {
        Expression query = Orders
            .Select(o => o.Status == OrderStatus.Paid ? "paid" : "other")
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CASE WHEN t0."Status" = 1 THEN 'paid' ELSE 'other' END
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsLabelOfEnumWithChainedConditionals()
    {
        Expression query = Orders
            .Select(o =>
                o.Status == OrderStatus.Created
                    ? "new"
                    : (o.Status == OrderStatus.Paid ? "paid" : "later")
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CASE WHEN t0."Status" = 0 THEN 'new' WHEN t0."Status" = 1 THEN 'paid' ELSE 'later' END
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByCapturedEnum()
    {
        OrderStatus status = OrderStatus.Shipped;

        Expression query = Orders
            .Where(o => o.Status == status)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Status" = 2
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByEnumDisjunction()
    {
        Expression query = Orders
            .Where(o =>
                (o.Status == OrderStatus.Paid) || (o.Status == OrderStatus.Shipped)
            )
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."Status" = 1) OR (t0."Status" = 2)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByEnumRange()
    {
        Expression query = Orders
            .Where(o =>
                (o.Status >= OrderStatus.Paid) && (o.Status < OrderStatus.Cancelled)
            )
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."Status" >= 1) AND (t0."Status" < 3)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByEnumEqualsMethod()
    {
        Expression query = Orders
            .Where(o => o.Status.Equals(OrderStatus.Paid))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Status" = 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByEnumInArray()
    {
        Expression query = Orders
            .Where(o =>
                new[] { OrderStatus.Paid, OrderStatus.Shipped }.Contains(o.Status)
            )
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Status" IN (1, 2)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByEnumNotInCapturedList()
    {
        OrderStatus[] statuses = [OrderStatus.Created, OrderStatus.Cancelled];

        Expression query = Orders
            .Where(o => !statuses.Contains(o.Status))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE NOT (t0."Status" IN (0, 3))
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OrdersByEnum()
    {
        Expression query = Orders
            .OrderBy(o => o.Status)
            .ThenBy(o => o.Id)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                ORDER BY t0."Status", t0."Id"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void GroupsByEnum()
    {
        Expression query = Orders
            .GroupBy(o => o.Status)
            .Select(g => new { g.Key, Revenue = g.Sum(o => o.Total) })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Status" AS "Key", COALESCE(SUM(t0."Total"), 0) AS "Revenue"
                FROM "Order" AS t0
                GROUP BY t0."Status"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsDistinctEnums()
    {
        Expression query = Orders.Select(o => o.Status).Distinct().Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT DISTINCT t0."Status"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
