namespace Linq.Expressions.Translation.PostgreSql.Tests.Operators;

public sealed record OrderByTests
{
    [Fact]
    public void OrdersAscending()
    {
        Expression query = Customers.OrderBy(c => c.Name).Expression;

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
    public void OrdersDescending()
    {
        Expression query = Customers.OrderByDescending(c => c.Name).Expression;

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
    public void AddsThenByKey()
    {
        Expression query = Customers
            .OrderBy(c => c.Region)
            .ThenBy(c => c.Name)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                ORDER BY t0."Region", t0."Name"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AddsThenByDescendingKey()
    {
        Expression query = Customers
            .OrderBy(c => c.Region)
            .ThenByDescending(c => c.Name)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                ORDER BY t0."Region", t0."Name" DESC
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AddsThenByAfterDescendingKey()
    {
        Expression query = Customers
            .OrderByDescending(c => c.Region)
            .ThenBy(c => c.Name)
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
    public void OrdersByTwoDescendingKeys()
    {
        Expression query = Customers
            .OrderByDescending(c => c.Region)
            .ThenByDescending(c => c.Name)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                ORDER BY t0."Region" DESC, t0."Name" DESC
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OrdersByThreeKeys()
    {
        Expression query = Customers
            .OrderBy(c => c.Region)
            .ThenByDescending(c => c.IsActive)
            .ThenBy(c => c.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                ORDER BY t0."Region", t0."IsActive" DESC, t0."Id"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void PutsNullsFirstWhenAscendingByNullableColumn()
    {
        Expression query = Customers.OrderBy(c => c.Email).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                ORDER BY t0."Email" NULLS FIRST
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void PutsNullsLastWhenDescendingByNullableColumn()
    {
        Expression query = Customers.OrderByDescending(c => c.Email).Expression;

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
    public void OrdersByNullableKeyInThenBy()
    {
        Expression query = Customers
            .OrderBy(c => c.Region)
            .ThenBy(c => c.Email)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                ORDER BY t0."Region", t0."Email" NULLS FIRST
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OrdersByNullableNumber()
    {
        Expression query = Orders
            .OrderByDescending(o => o.Priority)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                ORDER BY t0."Priority" DESC NULLS LAST
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OrdersByComputedKey()
    {
        Expression query = Customers.OrderBy(c => c.Name.Length).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                ORDER BY LENGTH(t0."Name")
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OrdersByArithmeticKey()
    {
        Expression query = Orders
            .OrderByDescending(o => o.Total * o.Quantity)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                ORDER BY t0."Total" * CAST(t0."Quantity" AS numeric) DESC
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OrdersByCompositeKey()
    {
        Expression query = Customers.OrderBy(c => new { c.Region, c.Name }).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                ORDER BY t0."Region", t0."Name"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OrdersByBooleanColumn()
    {
        Expression query = Customers.OrderBy(c => c.IsActive).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                ORDER BY t0."IsActive"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void KeepsEarlierKeysAsTieBreakersOfLaterOrderBy()
    {
        Expression query = Customers
            .OrderBy(c => c.Region)
            .OrderBy(c => c.Name)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                ORDER BY t0."Name", t0."Region"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void KeepsEarlierKeysWhenOrderingByAgainDescending()
    {
        Expression query = Customers
            .OrderBy(c => c.Region)
            .ThenBy(c => c.Id)
            .OrderByDescending(c => c.Name)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                ORDER BY t0."Name" DESC, t0."Region", t0."Id"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void KeepsOrderingWhenFilteringAfterwards()
    {
        Expression query = Customers.OrderBy(c => c.Name).Where(c => c.Id > 1).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                WHERE t0."Id" > 1
                ORDER BY t0."Name"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void KeepsOrderingWhenProjectingAfterwards()
    {
        Expression query = Customers.OrderBy(c => c.Name).Select(c => c.Id).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Customer" AS t0
                ORDER BY t0."Name"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OrdersByProjectedMember()
    {
        Expression query = Customers
            .Select(c => new { c.Id, Upper = c.Name.ToUpper() })
            .OrderBy(x => x.Upper)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", UPPER(t0."Name") AS "Upper"
                FROM "Customer" AS t0
                ORDER BY UPPER(t0."Name")
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OrdersByDecimalAndDateColumns()
    {
        Expression query = Orders
            .OrderBy(o => o.Total)
            .ThenByDescending(o => o.CreatedAt)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                ORDER BY t0."Total", t0."CreatedAt" DESC
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OrdersByGuidAndEnumColumns()
    {
        Expression query = Orders
            .OrderBy(o => o.TrackingId)
            .ThenBy(o => o.Status)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                ORDER BY t0."TrackingId", t0."Status"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OrdersByNullableDateColumn()
    {
        Expression query = Orders.OrderBy(o => o.PaidAt).Select(o => o.Id).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                ORDER BY t0."PaidAt" NULLS FIRST
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OrdersByNullableOrderingWithoutNullsFirst()
    {
        Expression query = Orders
            .OrderBy(o => o.Priority > 5)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                ORDER BY COALESCE(t0."Priority" > 5, FALSE)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OmitsConstantOrderingKey()
    {
        Expression query = Orders
            .Select(o => new { o.Id, Kind = "x" })
            .OrderBy(x => x.Kind)
            .ThenBy(x => x.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", 'x' AS "Kind"
                FROM "Order" AS t0
                ORDER BY t0."Id"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OmitsConstantMemberOfCompositeOrderingKey()
    {
        Expression query = Orders
            .OrderByDescending(o => new { First = 1, o.Total })
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                ORDER BY t0."Total" DESC
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void LeavesRowsUnorderedByConstantKey()
    {
        Expression query = Orders.OrderBy(o => 0).Select(o => o.Id).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OrdersScalarsByThemselves()
    {
        Expression query = Customers.Select(c => c.Name).Order().Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name"
                FROM "Customer" AS t0
                ORDER BY t0."Name"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OrdersPagedScalarsByThemselvesBeforeEarlierKeys()
    {
        Expression query = Customers
            .OrderBy(c => c.Id)
            .Select(c => c.Name)
            .Take(5)
            .Order()
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Name"
                FROM (
                    SELECT t0."Name", t0."Id" AS "$0"
                    FROM "Customer" AS t0
                    ORDER BY t0."Id"
                    LIMIT 5
                ) AS t1
                ORDER BY t1."Name", t1."$0"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OrdersNullableScalarsByThemselvesDescending()
    {
        Expression query = Customers.Select(c => c.Email).OrderDescending().Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Email"
                FROM "Customer" AS t0
                ORDER BY t0."Email" DESC NULLS LAST
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
