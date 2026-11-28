namespace Linq.Expressions.Translation.PostgreSql.Tests.Operators;

// ElementAt and ElementAtOrDefault of an Index counted from the end: the element at
// OFFSET n - 1 of the inverted order.
public sealed record ElementFromEndTests
{
    // #141: ElementAt(^2) is the second row of the inverted order.
    [Fact]
    public void TakesElementAtFromEnd()
    {
        Expression query = Customers.OrderBy(c => c.Id).Executed(q => q.ElementAt(^2));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                ORDER BY t0."Id" DESC
                LIMIT 1 OFFSET 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    // #141: ElementAtOrDefault(^1) is the first row of the inverted order.
    [Fact]
    public void TakesElementAtOrDefaultFromEnd()
    {
        Expression query = Customers
            .OrderByDescending(c => c.Name)
            .Executed(q => q.ElementAtOrDefault(^1));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                ORDER BY t0."Name"
                LIMIT 1 OFFSET 0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
