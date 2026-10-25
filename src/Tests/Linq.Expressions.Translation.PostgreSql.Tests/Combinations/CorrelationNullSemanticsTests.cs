namespace Linq.Expressions.Translation.PostgreSql.Tests.Combinations;

public sealed record CorrelationNullSemanticsTests
{
    [Fact]
    public void ExcludesNullCorrelationKeysLikeCSharp()
    {
        Expression query = Products
            .Where(p => Products.Any(q => q.CategoryId != p.CategoryId))
            .Select(p => p.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Product" AS t0
                WHERE EXISTS (
                    SELECT 1
                    FROM "Product" AS t1
                    WHERE t1."CategoryId" IS DISTINCT FROM t0."CategoryId"
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void KeepsEqualsForCorrelationWithOneNullableKey()
    {
        Expression query = Customers
            .Where(c => Orders.Any(o => o.Priority == c.Id))
            .Select(c => c.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Customer" AS t0
                WHERE EXISTS (
                    SELECT 1
                    FROM "Order" AS t1
                    WHERE t1."Priority" = t0."Id"
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ProjectsCorrelationWithOneNullableKey()
    {
        Expression query = Customers
            .Select(c => Orders.Select(o => o.Priority == c.Id).FirstOrDefault())
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE((
                    SELECT COALESCE(t1."Priority" = t0."Id", FALSE)
                    FROM "Order" AS t1
                    LIMIT 1
                ), FALSE)
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ProjectsCorrelationWithBothKeysNullable()
    {
        Expression query = Products
            .Select(p =>
                Products.Select(q => q.CategoryId == p.CategoryId).FirstOrDefault()
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE((
                    SELECT t1."CategoryId" IS NOT DISTINCT FROM t0."CategoryId"
                    FROM "Product" AS t1
                    LIMIT 1
                ), FALSE)
                FROM "Product" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void KeepsNullCorrelationKeysForNotEqualLikeCSharp()
    {
        Expression query = Employees
            .Where(e => Employees.Any(m => m.Id != e.ManagerId))
            .Select(e => e.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Employee" AS t0
                WHERE EXISTS (
                    SELECT 1
                    FROM "Employee" AS t1
                    WHERE t1."Id" IS DISTINCT FROM t0."ManagerId"
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void KeepsNullCorrelationKeysForNotEqualNullableBoolean()
    {
        Expression query = Orders
            .Where(o => Orders.Any(p => p.IsGift != o.IsPaid))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE EXISTS (
                    SELECT 1
                    FROM "Order" AS t1
                    WHERE t1."IsGift" IS DISTINCT FROM t0."IsPaid"
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ProjectsNotEqualCorrelationWithOneNullableKey()
    {
        Expression query = Employees
            .Select(e => Employees.Select(m => m.Id != e.ManagerId).FirstOrDefault())
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE((
                    SELECT t1."Id" IS DISTINCT FROM t0."ManagerId"
                    FROM "Employee" AS t1
                    LIMIT 1
                ), FALSE)
                FROM "Employee" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
