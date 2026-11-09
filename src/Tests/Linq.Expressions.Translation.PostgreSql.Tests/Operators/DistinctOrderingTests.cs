namespace Linq.Expressions.Translation.PostgreSql.Tests.Operators;

public sealed record DistinctOrderingTests
{
    // Distinct yields the first occurrence of each value in the order of the source:
    // DISTINCT ON the values keeps, of each, the row that comes first in the order of
    // the source, which is restored outside from the ordering keys it exposes.
    [Fact]
    public void KeepsOrderOfFirstOccurrencesByHiddenKey()
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
                SELECT t1."Region"
                FROM (
                    SELECT DISTINCT ON (t0."Region") t0."Region", t0."Id" AS "$0"
                    FROM "Customer" AS t0
                    ORDER BY t0."Region", t0."Id", t0."Region"
                ) AS t1
                ORDER BY t1."$0", t1."Region"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void KeepsOrderOfFirstOccurrencesOfPagedSource()
    {
        Expression query = Customers
            .OrderBy(c => c.Id)
            .Skip(2)
            .Select(c => c.Region)
            .Distinct()
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t2."Region"
                FROM (
                    SELECT DISTINCT ON (t1."Region") t1."Region", t1."$0"
                    FROM (
                        SELECT t0."Region", t0."Id" AS "$0"
                        FROM "Customer" AS t0
                        ORDER BY t0."Id"
                        OFFSET 2
                    ) AS t1
                    ORDER BY t1."Region", t1."$0"
                ) AS t2
                ORDER BY t2."$0"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void KeepsOrderOfFirstOccurrencesOfProjection()
    {
        Expression query = Orders
            .OrderByDescending(o => o.Id)
            .Select(o => new { o.CustomerId, o.IsPaid })
            .Distinct()
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."CustomerId", t1."IsPaid"
                FROM (
                    SELECT DISTINCT ON (t0."CustomerId", t0."IsPaid") t0."CustomerId", t0."IsPaid", t0."Id" AS "$0"
                    FROM "Order" AS t0
                    ORDER BY t0."CustomerId", t0."IsPaid", t0."Id" DESC
                ) AS t1
                ORDER BY t1."$0" DESC
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    // Values equal in the selected leading key still come in the order of a hidden key.
    [Fact]
    public void KeepsOrderOfFirstOccurrencesOfProjectionBeyondSelectedKey()
    {
        Expression query = Customers
            .OrderBy(c => c.Region)
            .ThenBy(c => c.Id)
            .Select(c => new { c.Region, c.Name })
            .Distinct()
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Region", t1."Name"
                FROM (
                    SELECT DISTINCT ON (t0."Region", t0."Name") t0."Region", t0."Name", t0."Id" AS "$0"
                    FROM "Customer" AS t0
                    ORDER BY t0."Region", t0."Name", t0."Region", t0."Id"
                ) AS t1
                ORDER BY t1."Region", t1."$0"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OrdersConstantValueByNothing()
    {
        Expression query = Customers
            .OrderBy(c => c.Id)
            .Select(c => 1)
            .Distinct()
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT DISTINCT 1
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
    public void KeepsHiddenDescendingKeyBeforeTake()
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
                SELECT t1."Region"
                FROM (
                    SELECT DISTINCT ON (t0."Region") t0."Region", t0."Id" AS "$0"
                    FROM "Customer" AS t0
                    ORDER BY t0."Region", t0."Id" DESC
                ) AS t1
                ORDER BY t1."$0" DESC
                LIMIT 5
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    // The ORDER BY of UnionBy only chooses the rows DISTINCT ON keeps; the result has
    // no order for Distinct to keep.
    [Fact]
    public void OrdersDistinctValuesOfUnionByByNothing()
    {
        Expression query = Customers
            .Select(c => new { c.Region, c.Id })
            .UnionBy(Employees.Select(e => new { Region = e.Name, e.Id }), x => x.Id)
            .Select(x => x.Region)
            .Distinct()
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT DISTINCT t3."Region"
                FROM (
                    SELECT DISTINCT ON (t2."Id") t2."Region"
                    FROM (
                        SELECT t0."Region", t0."Id", 0 AS "$side"
                        FROM "Customer" AS t0
                        UNION ALL
                        SELECT t1."Name" AS "Region", t1."Id", 1 AS "$side"
                        FROM "Employee" AS t1
                    ) AS t2
                    ORDER BY t2."Id", t2."$side"
                ) AS t3
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    // A derived table does not order its rows: the order moves to the outer query.
    [Fact]
    public void RestrictsDistinctRowsOutsideInTheirOrder()
    {
        Expression query = Customers
            .OrderBy(c => c.Region)
            .Distinct()
            .Where(c => c.IsActive)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Id", t1."Name", t1."Email", t1."Region", t1."IsActive"
                FROM (
                    SELECT DISTINCT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                    FROM "Customer" AS t0
                ) AS t1
                WHERE t1."IsActive"
                ORDER BY t1."Region"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
