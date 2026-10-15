namespace Linq.Expressions.Translation.PostgreSql.Tests;

public sealed record PostgreSqlQueryTests
{
    [Fact]
    public void SelectsEveryMappedColumnOfRoot()
    {
        Expression query = Customers.Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsColumnsInPropertyDeclarationOrder()
    {
        Expression query = Orders.Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."CustomerId", t0."Number", t0."Note", t0."Total",
                t0."Discount", t0."Quantity", t0."Priority", t0."Points", t0."Weight",
                t0."IsPaid", t0."IsGift", t0."Status", t0."Options", t0."CreatedAt",
                t0."PaidAt", t0."ShippedOn", t0."DeliveryTime", t0."TrackingId", t0."Tags"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsSingleColumnWithoutAlias()
    {
        Expression query = Customers.Select(c => c.Name).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void TranslatesTerminalOperatorCapturedFromProvider()
    {
        Expression query = Customers.Executed(q => q.Count());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COUNT(*) AS integer)
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
