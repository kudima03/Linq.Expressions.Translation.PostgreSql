namespace Linq.Expressions.Translation.PostgreSql.Tests.Operators;

public sealed record CastOfTypeTests
{
    [Fact(Skip = Skipped.NotImplemented)]
    public void IgnoresCastToElementType()
    {
        Expression query = Customers.Cast<Customer>().Expression;

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

    [Fact(Skip = Skipped.NotImplemented)]
    public void IgnoresOfTypeOfElementType()
    {
        Expression query = Customers.OfType<Customer>().Expression;

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

    [Fact(Skip = Skipped.NotImplemented)]
    public void ProjectsThroughInterfaceCast()
    {
        Expression query = Customers.Cast<INamed>().Select(n => n.Name).Expression;

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
    public void ProjectsThroughInterfaceOfType()
    {
        Expression query = Customers.OfType<INamed>().Select(n => n.Name).Expression;

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
    public void FiltersThroughInterfaceCast()
    {
        Expression query = Customers
            .Cast<INamed>()
            .Where(n => n.Name == "Ann")
            .Select(n => n.Name)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name"
                FROM "Customer" AS t0
                WHERE t0."Name" = 'Ann'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FiltersThroughInterfaceCastOfProduct()
    {
        Expression query = Products
            .Cast<INamed>()
            .OrderBy(n => n.Name)
            .Select(n => n.Name)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name"
                FROM "Product" AS t0
                ORDER BY t0."Name"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void KeepsNonNullValuesOfNullableElementOnOfType()
    {
        Expression query = Orders.Select(o => o.Priority).OfType<int>().Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Priority"
                FROM "Order" AS t0
                WHERE t0."Priority" IS NOT NULL
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void IgnoresCastOfScalarToSameType()
    {
        Expression query = Customers.Select(c => c.Id).Cast<int>().Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
