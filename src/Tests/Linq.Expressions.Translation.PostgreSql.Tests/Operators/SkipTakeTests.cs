namespace Linq.Expressions.Translation.PostgreSql.Tests.Operators;

public sealed record SkipTakeTests
{
    [Fact]
    public void TakesFirstRows()
    {
        Expression query = Customers.Take(5).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                LIMIT 5
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SkipsFirstRows()
    {
        Expression query = Customers.Skip(5).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                OFFSET 5
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SkipsThenTakes()
    {
        Expression query = Customers.Skip(5).Take(10).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                LIMIT 10 OFFSET 5
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesThenSkipsWithinTakenRows()
    {
        Expression query = Customers.Take(10).Skip(4).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                LIMIT 6 OFFSET 4
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AddsConsecutiveSkips()
    {
        Expression query = Customers.Skip(2).Skip(3).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                OFFSET 5
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void KeepsSmallerOfConsecutiveTakes()
    {
        Expression query = Customers.Take(10).Take(3).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                LIMIT 3
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void KeepsSmallerTakeWhenSecondIsLarger()
    {
        Expression query = Customers.Take(3).Take(10).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                LIMIT 3
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CombinesSkipTakeSkip()
    {
        Expression query = Customers.Skip(5).Take(10).Skip(2).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                LIMIT 8 OFFSET 7
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesZeroRows()
    {
        Expression query = Customers.Take(0).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                LIMIT 0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void EvaluatesPagingArithmeticOfCapturedVariables()
    {
        int page = 3;
        int size = 20;

        Expression query = Customers
            .OrderBy(c => c.Id)
            .Skip((page - 1) * size)
            .Take(size)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                ORDER BY t0."Id"
                LIMIT 20 OFFSET 40
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void PagesAfterFilteringAndOrdering()
    {
        Expression query = Customers
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Skip(10)
            .Take(5)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                WHERE t0."IsActive"
                ORDER BY t0."Name"
                LIMIT 5 OFFSET 10
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ProjectsAfterPaging()
    {
        Expression query = Customers
            .OrderBy(c => c.Id)
            .Take(5)
            .Select(c => c.Name)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name"
                FROM "Customer" AS t0
                ORDER BY t0."Id"
                LIMIT 5
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ProjectsAfterSkip()
    {
        Expression query = Customers
            .OrderBy(c => c.Id)
            .Skip(5)
            .Select(c => c.Name)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name"
                FROM "Customer" AS t0
                ORDER BY t0."Id"
                OFFSET 5
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void PagesAfterProjection()
    {
        Expression query = Customers.Select(c => c.Name).Skip(1).Take(2).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name"
                FROM "Customer" AS t0
                LIMIT 2 OFFSET 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void WrapsFilterAfterTake()
    {
        Expression query = Customers.Take(5).Where(c => c.IsActive).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Id", t1."Name", t1."Email", t1."Region", t1."IsActive"
                FROM (
                    SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                    FROM "Customer" AS t0
                    LIMIT 5
                ) AS t1
                WHERE t1."IsActive"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void WrapsFilterAfterSkip()
    {
        Expression query = Customers.Skip(5).Where(c => c.IsActive).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Id", t1."Name", t1."Email", t1."Region", t1."IsActive"
                FROM (
                    SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                    FROM "Customer" AS t0
                    OFFSET 5
                ) AS t1
                WHERE t1."IsActive"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void KeepsOrderingWhenFilteringAfterTake()
    {
        Expression query = Customers
            .OrderBy(c => c.Name)
            .Take(5)
            .Where(c => c.IsActive)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Id", t1."Name", t1."Email", t1."Region", t1."IsActive"
                FROM (
                    SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                    FROM "Customer" AS t0
                    ORDER BY t0."Name"
                    LIMIT 5
                ) AS t1
                WHERE t1."IsActive"
                ORDER BY t1."Name"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void WrapsOrderingAfterTake()
    {
        Expression query = Customers.Take(5).OrderBy(c => c.Name).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Id", t1."Name", t1."Email", t1."Region", t1."IsActive"
                FROM (
                    SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                    FROM "Customer" AS t0
                    LIMIT 5
                ) AS t1
                ORDER BY t1."Name"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void WrapsFilterAfterTakeOnProjection()
    {
        Expression query = Customers
            .Select(c => c.Id)
            .Take(5)
            .Where(id => id > 2)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Id"
                FROM (
                    SELECT t0."Id"
                    FROM "Customer" AS t0
                    LIMIT 5
                ) AS t1
                WHERE t1."Id" > 2
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void PagesTwiceAcrossFilter()
    {
        Expression query = Customers.Take(5).Where(c => c.IsActive).Take(2).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Id", t1."Name", t1."Email", t1."Region", t1."IsActive"
                FROM (
                    SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                    FROM "Customer" AS t0
                    LIMIT 5
                ) AS t1
                WHERE t1."IsActive"
                LIMIT 2
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Theory]
    [InlineData(5, 10)]
    [InlineData(20, 20)]
    [InlineData(100, 1)]
    public void PagesWithSkipAndTake(int skip, int take)
    {
        Expression query = Customers.OrderBy(c => c.Id).Skip(skip).Take(take).Expression;

        Assert.Equal(
            Sql.Valid(
                $"""
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                ORDER BY t0."Id"
                LIMIT {take} OFFSET {skip}
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SkipsNothing()
    {
        Expression query = Customers.Skip(0).Expression;

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

    [Fact]
    public void FiltersComputedValuesAfterSkip()
    {
        Expression query = Orders
            .Select(o => o.Total * 2)
            .Skip(1)
            .Where(x => x > 5)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Value"
                FROM (
                    SELECT t0."Total" * 2 AS "Value"
                    FROM "Order" AS t0
                    OFFSET 1
                ) AS t1
                WHERE t1."Value" > 5
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void KeepsOrderingOfComputedValuesAcrossWrap()
    {
        Expression query = Orders
            .OrderBy(o => o.Total * 2)
            .Select(o => o.Total * 2)
            .Take(3)
            .Where(x => x > 5)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Value"
                FROM (
                    SELECT t0."Total" * 2 AS "Value"
                    FROM "Order" AS t0
                    ORDER BY t0."Total" * 2
                    LIMIT 3
                ) AS t1
                WHERE t1."Value" > 5
                ORDER BY t1."Value"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CountsPagedComputedValues()
    {
        Expression query = Orders
            .Select(o => o.Total * 2)
            .Take(3)
            .Executed(q => q.Count());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COUNT(*) AS integer)
                FROM (
                    SELECT t0."Total" * 2 AS "Value"
                    FROM "Order" AS t0
                    LIMIT 3
                ) AS t1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void KeepsHiddenOrderingKeyWhenFilteringTakenRows()
    {
        Expression query = Customers
            .OrderBy(c => c.Region)
            .Select(c => c.Name)
            .Take(5)
            .Where(n => n != "a")
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Name"
                FROM (
                    SELECT t0."Name", t0."Region" AS "$0"
                    FROM "Customer" AS t0
                    ORDER BY t0."Region"
                    LIMIT 5
                ) AS t1
                WHERE t1."Name" <> 'a'
                ORDER BY t1."$0"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ExposesEachHiddenOrderingKeyOnce()
    {
        Expression query = Customers
            .OrderBy(c => c.Region)
            .ThenByDescending(c => c.Id)
            .Select(c => c.Name)
            .Take(5)
            .Where(n => n != "a")
            .Skip(1)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Name"
                FROM (
                    SELECT t0."Name", t0."Region" AS "$0", t0."Id" AS "$1"
                    FROM "Customer" AS t0
                    ORDER BY t0."Region", t0."Id" DESC
                    LIMIT 5
                ) AS t1
                WHERE t1."Name" <> 'a'
                ORDER BY t1."$0", t1."$1" DESC
                OFFSET 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CountsSkippedLastRowsWithoutHiddenOrderingKey()
    {
        Expression query = Customers
            .OrderBy(c => c.Region)
            .Select(c => c.Name)
            .Take(5)
            .SkipLast(1)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Name"
                FROM (
                    SELECT t0."Name", t0."Region" AS "$0"
                    FROM "Customer" AS t0
                    ORDER BY t0."Region"
                    LIMIT 5
                ) AS t1
                ORDER BY t1."$0"
                LIMIT (
                    SELECT GREATEST(COUNT(*) - 1, 0)
                    FROM (
                        SELECT t2."Name"
                        FROM "Customer" AS t2
                        ORDER BY t2."Region"
                        LIMIT 5
                    ) AS t3
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
