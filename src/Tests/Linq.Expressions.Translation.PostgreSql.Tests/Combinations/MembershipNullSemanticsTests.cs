namespace Linq.Expressions.Translation.PostgreSql.Tests.Combinations;

public sealed record MembershipNullSemanticsTests
{
    [Fact]
    public void ProjectsArrayMembershipOfNullableValue()
    {
        Expression query = Orders.Select(o => o.Tags.Contains(o.Note)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(t0."Note" = ANY (t0."Tags"), FALSE)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ProjectsArrayMembershipOfValue()
    {
        Expression query = Orders.Select(o => o.Tags.Contains(o.Number)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Number" = ANY (t0."Tags")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ProjectsListMembershipOfValue()
    {
        int[] quantities = [1, 2];
        Expression query = Orders.Select(o => quantities.Contains(o.Quantity)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Quantity" IN (1, 2)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ProjectsMembershipIncludingNull()
    {
        int?[] priorities = [1, null];
        Expression query = Orders.Select(o => priorities.Contains(o.Priority)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(t0."Priority" IN (1), FALSE) OR (t0."Priority" IS NULL)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void NegatesMembershipIncludingNull()
    {
        int?[] priorities = [1, null];
        Expression query = Orders
            .Where(o => !priorities.Contains(o.Priority))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE NOT (COALESCE(t0."Priority" IN (1), FALSE) OR (t0."Priority" IS NULL))
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ProjectsSubqueryMembershipOfNullableValue()
    {
        Expression query = Orders
            .Select(o => Customers.Select(c => (int?)c.Id).Contains(o.Priority))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(t0."Priority" IN (SELECT t1."Id" FROM "Customer" AS t1), FALSE)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void NegatesSubqueryMembershipOfNullableValue()
    {
        Expression query = Orders
            .Where(o => !Customers.Select(c => (int?)c.Id).Contains(o.Priority))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE NOT COALESCE(t0."Priority" IN (SELECT t1."Id" FROM "Customer" AS t1), FALSE)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ProjectsSubqueryMembershipOfValues()
    {
        Expression query = Customers
            .Select(c => Orders.Select(o => o.CustomerId).Contains(c.Id))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id" IN (SELECT t1."CustomerId" FROM "Order" AS t1)
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
