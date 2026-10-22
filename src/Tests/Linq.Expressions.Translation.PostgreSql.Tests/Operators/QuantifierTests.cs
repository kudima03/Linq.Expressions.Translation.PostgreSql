namespace Linq.Expressions.Translation.PostgreSql.Tests.Operators;

public sealed record QuantifierTests
{
    [Fact]
    public void ChecksAnyRowExists()
    {
        Expression query = Customers.Executed(q => q.Any());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT EXISTS (
                    SELECT 1
                    FROM "Customer" AS t0
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ChecksAnyRowMatches()
    {
        Expression query = Customers.Executed(q => q.Any(c => c.IsActive));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT EXISTS (
                    SELECT 1
                    FROM "Customer" AS t0
                    WHERE t0."IsActive"
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ChecksAnyRowMatchesComparison()
    {
        Expression query = Customers.Executed(q => q.Any(c => c.Id > 100));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT EXISTS (
                    SELECT 1
                    FROM "Customer" AS t0
                    WHERE t0."Id" > 100
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ChecksAnyFilteredRowExists()
    {
        Expression query = Customers.Where(c => c.Region == "EU").Executed(q => q.Any());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT EXISTS (
                    SELECT 1
                    FROM "Customer" AS t0
                    WHERE t0."Region" = 'EU'
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ChecksAnyFilteredRowMatches()
    {
        Expression query = Customers
            .Where(c => c.Region == "EU")
            .Executed(q => q.Any(c => c.IsActive));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT EXISTS (
                    SELECT 1
                    FROM "Customer" AS t0
                    WHERE (t0."Region" = 'EU') AND t0."IsActive"
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ChecksAnyRowAfterPaging()
    {
        Expression query = Customers
            .OrderBy(c => c.Id)
            .Take(5)
            .Executed(q => q.Any(c => c.IsActive));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT EXISTS (
                    SELECT 1
                    FROM (
                        SELECT t0."Id", t0."Name", t0."Email", t0."Region",
                        t0."IsActive"
                        FROM "Customer" AS t0
                        ORDER BY t0."Id"
                        LIMIT 5
                    ) AS t1
                    WHERE t1."IsActive"
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ChecksAnyJoinedRowExists()
    {
        Expression query = Customers
            .Join(Orders, c => c.Id, o => o.CustomerId, (c, o) => new { c.Name, o.Total })
            .Executed(q => q.Any());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT EXISTS (
                    SELECT 1
                    FROM "Customer" AS t0
                    INNER JOIN "Order" AS t1 ON t0."Id" = t1."CustomerId"
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ChecksAllRowsMatch()
    {
        Expression query = Customers.Executed(q => q.All(c => c.IsActive));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT NOT EXISTS (
                    SELECT 1
                    FROM "Customer" AS t0
                    WHERE NOT t0."IsActive"
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ChecksAllRowsMatchComparison()
    {
        Expression query = Customers.Executed(q => q.All(c => c.Id > 5));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT NOT EXISTS (
                    SELECT 1
                    FROM "Customer" AS t0
                    WHERE NOT (t0."Id" > 5)
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ChecksAllRowsMatchConjunction()
    {
        Expression query = Customers.Executed(q => q.All(c => c.IsActive && (c.Id > 5)));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT NOT EXISTS (
                    SELECT 1
                    FROM "Customer" AS t0
                    WHERE NOT (t0."IsActive" AND (t0."Id" > 5))
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ChecksAllFilteredRowsMatch()
    {
        Expression query = Customers
            .Where(c => c.Region == "EU")
            .Executed(q => q.All(c => c.IsActive));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT NOT EXISTS (
                    SELECT 1
                    FROM "Customer" AS t0
                    WHERE (t0."Region" = 'EU') AND NOT t0."IsActive"
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ChecksAllNullableRowsMatchWithNullAsMismatch()
    {
        Expression query = Customers.Executed(q => q.All(c => c.Email == null));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT NOT EXISTS (
                    SELECT 1
                    FROM "Customer" AS t0
                    WHERE NOT (t0."Email" IS NULL)
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ChecksContainsOfScalar()
    {
        Expression query = Customers.Select(c => c.Id).Executed(q => q.Contains(5));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT 5 IN (
                    SELECT t0."Id"
                    FROM "Customer" AS t0
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ChecksContainsOfCapturedValue()
    {
        int wanted = 42;

        Expression query = Customers.Select(c => c.Id).Executed(q => q.Contains(wanted));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT 42 IN (
                    SELECT t0."Id"
                    FROM "Customer" AS t0
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ChecksContainsOfString()
    {
        Expression query = Customers.Select(c => c.Name).Executed(q => q.Contains("Ann"));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT 'Ann' IN (
                    SELECT t0."Name"
                    FROM "Customer" AS t0
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ChecksContainsInFilteredRows()
    {
        Expression query = Customers
            .Where(c => c.IsActive)
            .Select(c => c.Id)
            .Executed(q => q.Contains(5));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT 5 IN (
                    SELECT t0."Id"
                    FROM "Customer" AS t0
                    WHERE t0."IsActive"
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ChecksContainsOfGuid()
    {
        Expression query = Orders
            .Select(o => o.TrackingId)
            .Executed(q =>
                q.Contains(Guid.Parse("11111111-2222-3333-4444-555555555555"))
            );

        Assert.Equal(
            Sql.Valid(
                """
                SELECT UUID '11111111-2222-3333-4444-555555555555' IN (
                    SELECT t0."TrackingId"
                    FROM "Order" AS t0
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ChecksContainsOfPagedRows()
    {
        Expression query = Customers
            .OrderBy(c => c.Id)
            .Take(5)
            .Select(c => c.Id)
            .Executed(q => q.Contains(5));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT 5 IN (
                    SELECT t0."Id"
                    FROM "Customer" AS t0
                    ORDER BY t0."Id"
                    LIMIT 5
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsMembershipInNullableSubqueryValuesWithCoalesce()
    {
        Expression query = Customers
            .Select(c => Orders.Select(o => o.Priority).Contains(c.Id))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(t0."Id" IN (
                    SELECT t1."Priority"
                    FROM "Order" AS t1
                ), FALSE)
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsMembershipOfNullableColumnInSubqueryWithCoalesce()
    {
        Expression query = Orders
            .Select(o => Orders.Select(x => (int?)x.Id).Contains(o.Priority))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(t0."Priority" IN (
                    SELECT t1."Id"
                    FROM "Order" AS t1
                ), FALSE)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ChecksMembershipInNullableColumnWithCoalesce()
    {
        Expression query = Orders.Select(o => o.Priority).Executed(q => q.Contains(5));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(5 IN (
                    SELECT t0."Priority"
                    FROM "Order" AS t0
                ), FALSE)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
