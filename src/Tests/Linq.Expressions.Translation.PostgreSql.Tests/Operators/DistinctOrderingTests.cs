namespace Linq.Expressions.Translation.PostgreSql.Tests.Operators;

public sealed record DistinctOrderingTests
{
    [Fact]
    public void DropsOrderingAfterHiddenLeadingKey()
    {
        Expression query = Customers
            .OrderBy(c => c.Id)
            .ThenBy(c => c.Region)
            .Select(c => c.Region)
            .Distinct()
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT DISTINCT t0."Region"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void KeepsSelectedLeadingOrderingKey()
    {
        Expression query = Customers
            .OrderBy(c => c.Region)
            .ThenBy(c => c.Id)
            .Select(c => c.Region)
            .Distinct()
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT DISTINCT t0."Region"
                FROM "Customer" AS t0
                ORDER BY t0."Region"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void KeepsSelectedComputedOrderingKey()
    {
        Expression query = Orders
            .OrderBy(o => o.Quantity * 2)
            .Select(o => o.Quantity * 2)
            .Distinct()
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT DISTINCT t0."Quantity" * 2
                FROM "Order" AS t0
                ORDER BY t0."Quantity" * 2
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void DropsHiddenDescendingKeyBeforeTake()
    {
        Expression query = Customers
            .OrderByDescending(c => c.Id)
            .Select(c => c.Region)
            .Distinct()
            .Take(5)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT DISTINCT t0."Region"
                FROM "Customer" AS t0
                LIMIT 5
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
