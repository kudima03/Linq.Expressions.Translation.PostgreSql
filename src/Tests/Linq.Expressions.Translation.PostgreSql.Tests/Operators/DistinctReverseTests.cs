namespace Linq.Expressions.Translation.PostgreSql.Tests.Operators;

public sealed record DistinctReverseTests
{
    [Fact]
    public void SelectsDistinctEntities()
    {
        Expression query = Customers.Distinct().Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT DISTINCT t0."Id", t0."Name", t0."Email", t0."Region",
                t0."IsActive"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsDistinctScalar()
    {
        Expression query = Customers.Select(c => c.Region).Distinct().Expression;

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
    public void SelectsDistinctAnonymousProjection()
    {
        Expression query = Customers
            .Select(c => new { c.Region, c.IsActive })
            .Distinct()
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT DISTINCT t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsDistinctAfterFilter()
    {
        Expression query = Customers
            .Where(c => c.IsActive)
            .Select(c => c.Region)
            .Distinct()
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT DISTINCT t0."Region"
                FROM "Customer" AS t0
                WHERE t0."IsActive"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void KeepsSingleDistinctWhenRepeated()
    {
        Expression query = Customers
            .Select(c => c.Region)
            .Distinct()
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
    public void PagesDistinctRows()
    {
        Expression query = Customers
            .Select(c => c.Region)
            .Distinct()
            .Skip(1)
            .Take(2)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT DISTINCT t0."Region"
                FROM "Customer" AS t0
                LIMIT 2 OFFSET 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void WrapsFilterAfterDistinct()
    {
        Expression query = Customers
            .Select(c => c.Region)
            .Distinct()
            .Where(r => r != "EU")
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Region"
                FROM (
                    SELECT DISTINCT t0."Region"
                    FROM "Customer" AS t0
                ) AS t1
                WHERE t1."Region" <> 'EU'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void WrapsOrderingAfterDistinct()
    {
        Expression query = Customers
            .Select(c => c.Region)
            .Distinct()
            .OrderBy(r => r)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Region"
                FROM (
                    SELECT DISTINCT t0."Region"
                    FROM "Customer" AS t0
                ) AS t1
                ORDER BY t1."Region"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void WrapsProjectionAfterDistinct()
    {
        Expression query = Customers
            .Select(c => new { c.Region, c.IsActive })
            .Distinct()
            .Select(x => x.Region)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Region"
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
    public void ReversesAscendingOrdering()
    {
        Expression query = Customers.OrderBy(c => c.Name).Reverse().Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                ORDER BY t0."Name" DESC
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ReversesDescendingOrdering()
    {
        Expression query = Customers.OrderByDescending(c => c.Name).Reverse().Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                ORDER BY t0."Name"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ReversesEveryOrderingKey()
    {
        Expression query = Customers
            .OrderBy(c => c.Region)
            .ThenByDescending(c => c.Name)
            .Reverse()
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                ORDER BY t0."Region" DESC, t0."Name"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ReversesNullableKeyWithMirroredNullPlacement()
    {
        Expression query = Customers.OrderBy(c => c.Email).Reverse().Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                ORDER BY t0."Email" DESC NULLS LAST
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RestoresOrderingWhenReversedTwice()
    {
        Expression query = Customers.OrderBy(c => c.Name).Reverse().Reverse().Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                ORDER BY t0."Name"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesFromReversedOrdering()
    {
        Expression query = Customers.OrderBy(c => c.Id).Reverse().Take(3).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                ORDER BY t0."Id" DESC
                LIMIT 3
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ReversesAfterFilter()
    {
        Expression query = Customers
            .OrderBy(c => c.Id)
            .Where(c => c.IsActive)
            .Reverse()
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                WHERE t0."IsActive"
                ORDER BY t0."Id" DESC
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ReversesPagedRows()
    {
        Expression query = Customers.OrderBy(c => c.Id).Take(3).Reverse().Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Id", t1."Name", t1."Email", t1."Region", t1."IsActive"
                FROM (
                    SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                    FROM "Customer" AS t0
                    ORDER BY t0."Id"
                    LIMIT 3
                ) AS t1
                ORDER BY t1."Id" DESC
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
