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
                SELECT COALESCE(t0."Note" = ANY (t0."Tags"), FALSE) OR ((t0."Note" IS NULL) AND (ARRAY_POSITION(t0."Tags", NULL) IS NOT NULL))
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

    [Fact]
    public void MatchesNullItemWithNullOfSubquery()
    {
        Expression query = Employees
            .Where(e => Employees.Select(m => m.ManagerId).Contains(e.ManagerId))
            .Select(e => e.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Employee" AS t0
                WHERE (t0."ManagerId" IN (
                    SELECT t1."ManagerId"
                    FROM "Employee" AS t1
                )) OR ((t0."ManagerId" IS NULL) AND EXISTS (
                    SELECT 1
                    FROM "Employee" AS t2
                    WHERE t2."ManagerId" IS NULL
                ))
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void MatchesNullItemWithNullOfPagedSubqueryWithoutHiddenKey()
    {
        Expression query = Employees
            .Where(e =>
                Employees
                    .OrderBy(m => m.Name)
                    .Select(m => m.ManagerId)
                    .Take(5)
                    .Contains(e.ManagerId)
            )
            .Select(e => e.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Employee" AS t0
                WHERE (t0."ManagerId" IN (
                    SELECT t1."ManagerId"
                    FROM "Employee" AS t1
                    ORDER BY t1."Name"
                    LIMIT 5
                )) OR ((t0."ManagerId" IS NULL) AND EXISTS (
                    SELECT 1
                    FROM (
                        SELECT t2."ManagerId"
                        FROM "Employee" AS t2
                        ORDER BY t2."Name"
                        LIMIT 5
                    ) AS t3
                    WHERE t3."ManagerId" IS NULL
                ))
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void KeepsPlainInForNonNullableItem()
    {
        Expression query = Employees
            .Where(e => Employees.Select(m => m.ManagerId).Contains(e.Id))
            .Select(e => e.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Employee" AS t0
                WHERE t0."Id" IN (
                    SELECT t1."ManagerId"
                    FROM "Employee" AS t1
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void KeepsPlainInForNonNullableSequence()
    {
        Expression query = Employees
            .Where(e => Employees.Select(m => (int?)m.Id).Contains(e.ManagerId))
            .Select(e => e.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Employee" AS t0
                WHERE t0."ManagerId" IN (
                    SELECT t1."Id"
                    FROM "Employee" AS t1
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void NeverMatchesNullKeysOfGroupJoin()
    {
        Expression query = Employees
            .GroupJoin(
                Employees,
                e => e.ManagerId,
                m => m.ManagerId,
                (e, ms) => ms.Count()
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT (
                    SELECT CAST(COUNT(*) AS integer)
                    FROM "Employee" AS t1
                    WHERE t1."ManagerId" = t0."ManagerId"
                )
                FROM "Employee" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
