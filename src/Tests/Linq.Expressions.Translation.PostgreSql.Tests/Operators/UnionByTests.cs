namespace Linq.Expressions.Translation.PostgreSql.Tests.Operators;

public sealed record UnionByTests
{
    [Fact]
    public void KeepsRowOfFirstSequencePerKey()
    {
        Expression query = Customers
            .Where(c => c.IsActive)
            .UnionBy(Customers.Where(c => c.Name == "Bob"), c => c.Email)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT DISTINCT ON (t2."Email") t2."Id", t2."Name", t2."Email",
                t2."Region", t2."IsActive"
                FROM (
                    SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive",
                    0 AS "$side"
                    FROM "Customer" AS t0
                    WHERE t0."IsActive"
                    UNION ALL
                    SELECT t1."Id", t1."Name", t1."Email", t1."Region", t1."IsActive",
                    1 AS "$side"
                    FROM "Customer" AS t1
                    WHERE t1."Name" = 'Bob'
                ) AS t2
                ORDER BY t2."Email" NULLS FIRST, t2."$side"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void UnionsProjections()
    {
        Expression query = Customers
            .Select(c => new { c.Name, Key = c.Id })
            .UnionBy(Products.Select(p => new { p.Name, Key = p.Id }), x => x.Name)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT DISTINCT ON (t2."Name") t2."Name", t2."Key"
                FROM (
                    SELECT t0."Name", t0."Id" AS "Key", 0 AS "$side"
                    FROM "Customer" AS t0
                    UNION ALL
                    SELECT t1."Name", t1."Id" AS "Key", 1 AS "$side"
                    FROM "Product" AS t1
                ) AS t2
                ORDER BY t2."Name", t2."$side"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void UnionsScalarSequencesByComputedKey()
    {
        Expression query = Customers
            .Select(c => c.Name)
            .UnionBy(Products.Select(p => p.Name), n => n.Length)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT DISTINCT ON (LENGTH(t2."Name")) t2."Name"
                FROM (
                    SELECT t0."Name", 0 AS "$side"
                    FROM "Customer" AS t0
                    UNION ALL
                    SELECT t1."Name", 1 AS "$side"
                    FROM "Product" AS t1
                ) AS t2
                ORDER BY LENGTH(t2."Name"), t2."$side"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ProjectsRowsKeptPerKey()
    {
        Expression query = Customers
            .UnionBy(Customers, c => c.Region)
            .Select(c => c.Name)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT DISTINCT ON (t2."Region") t2."Name"
                FROM (
                    SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive",
                    0 AS "$side"
                    FROM "Customer" AS t0
                    UNION ALL
                    SELECT t1."Id", t1."Name", t1."Email", t1."Region", t1."IsActive",
                    1 AS "$side"
                    FROM "Customer" AS t1
                ) AS t2
                ORDER BY t2."Region", t2."$side"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void PagesRowsKeptPerKey()
    {
        Expression query = Customers.UnionBy(Customers, c => c.Region).Take(5).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT DISTINCT ON (t2."Region") t2."Id", t2."Name", t2."Email",
                t2."Region", t2."IsActive"
                FROM (
                    SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive",
                    0 AS "$side"
                    FROM "Customer" AS t0
                    UNION ALL
                    SELECT t1."Id", t1."Name", t1."Email", t1."Region", t1."IsActive",
                    1 AS "$side"
                    FROM "Customer" AS t1
                ) AS t2
                ORDER BY t2."Region", t2."$side"
                LIMIT 5
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersRowsKeptPerKeyInDerivedTableWithoutOrder()
    {
        Expression query = Customers
            .UnionBy(Customers, c => c.Region)
            .Where(c => c.IsActive)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t3."Id", t3."Name", t3."Email", t3."Region", t3."IsActive"
                FROM (
                    SELECT DISTINCT ON (t2."Region") t2."Id", t2."Name", t2."Email",
                    t2."Region", t2."IsActive"
                    FROM (
                        SELECT t0."Id", t0."Name", t0."Email", t0."Region",
                        t0."IsActive", 0 AS "$side"
                        FROM "Customer" AS t0
                        UNION ALL
                        SELECT t1."Id", t1."Name", t1."Email", t1."Region",
                        t1."IsActive", 1 AS "$side"
                        FROM "Customer" AS t1
                    ) AS t2
                    ORDER BY t2."Region", t2."$side"
                ) AS t3
                WHERE t3."IsActive"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OrdersRowsKeptPerKey()
    {
        Expression query = Customers
            .UnionBy(Customers, c => c.Region)
            .OrderBy(c => c.Name)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t3."Id", t3."Name", t3."Email", t3."Region", t3."IsActive"
                FROM (
                    SELECT DISTINCT ON (t2."Region") t2."Id", t2."Name", t2."Email",
                    t2."Region", t2."IsActive"
                    FROM (
                        SELECT t0."Id", t0."Name", t0."Email", t0."Region",
                        t0."IsActive", 0 AS "$side"
                        FROM "Customer" AS t0
                        UNION ALL
                        SELECT t1."Id", t1."Name", t1."Email", t1."Region",
                        t1."IsActive", 1 AS "$side"
                        FROM "Customer" AS t1
                    ) AS t2
                    ORDER BY t2."Region", t2."$side"
                ) AS t3
                ORDER BY t3."Name"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CountsRowsKeptPerKey()
    {
        Expression query = Customers
            .UnionBy(Customers, c => c.Region)
            .Executed(q => q.Count());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COUNT(*) AS integer)
                FROM (
                    SELECT DISTINCT ON (t2."Region") t2."Id", t2."Name", t2."Email",
                    t2."Region", t2."IsActive"
                    FROM (
                        SELECT t0."Id", t0."Name", t0."Email", t0."Region",
                        t0."IsActive", 0 AS "$side"
                        FROM "Customer" AS t0
                        UNION ALL
                        SELECT t1."Id", t1."Name", t1."Email", t1."Region",
                        t1."IsActive", 1 AS "$side"
                        FROM "Customer" AS t1
                    ) AS t2
                    ORDER BY t2."Region", t2."$side"
                ) AS t3
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void WrapsSetOperationSide()
    {
        Expression query = Customers
            .Concat(Customers)
            .UnionBy(Customers, c => c.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT DISTINCT ON (t4."Id") t4."Id", t4."Name", t4."Email", t4."Region",
                t4."IsActive"
                FROM (
                    SELECT t2."Id", t2."Name", t2."Email", t2."Region", t2."IsActive",
                    0 AS "$side"
                    FROM (
                        SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                        FROM "Customer" AS t0
                        UNION ALL
                        SELECT t1."Id", t1."Name", t1."Email", t1."Region", t1."IsActive"
                        FROM "Customer" AS t1
                    ) AS t2
                    UNION ALL
                    SELECT t3."Id", t3."Name", t3."Email", t3."Region", t3."IsActive",
                    1 AS "$side"
                    FROM "Customer" AS t3
                ) AS t4
                ORDER BY t4."Id", t4."$side"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ParenthesizesPagedSide()
    {
        Expression query = Customers
            .OrderBy(c => c.Name)
            .Take(3)
            .UnionBy(Customers, c => c.Region)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT DISTINCT ON (t2."Region") t2."Id", t2."Name", t2."Email",
                t2."Region", t2."IsActive"
                FROM ((
                    SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive",
                    0 AS "$side"
                    FROM "Customer" AS t0
                    ORDER BY t0."Name"
                    LIMIT 3
                ) UNION ALL
                SELECT t1."Id", t1."Name", t1."Email", t1."Region", t1."IsActive",
                1 AS "$side"
                FROM "Customer" AS t1
                ) AS t2
                ORDER BY t2."Region", t2."$side"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RejectsReverseOfUnorderedRows()
    {
        Expression query = Customers
            .UnionBy(Customers, c => c.Region)
            .Reverse()
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Reverse", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsInMemorySecondSequence()
    {
        IEnumerable<Customer> none = [];
        Expression query = Customers.UnionBy(none, c => c.Region).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("UnionBy", exception.Message, StringComparison.Ordinal);
    }
}
