#pragma warning disable CA1829 // Count() on the array column is the call under test
namespace Linq.Expressions.Translation.PostgreSql.Tests.Combinations;

public sealed record CollectionQuantifierTests
{
    [Fact]
    public void FiltersByCapturedListThroughReversedAny()
    {
        List<int> ids = [1, 2, 3];
        Expression query = Orders
            .Where(o => ids.Any(i => o.Id == i))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Id" IN (1, 2, 3)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByArrayColumnThroughAny()
    {
        Expression query = Orders
            .Where(o => o.Tags.Any(t => t == "vip"))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE 'vip' = ANY (t0."Tags")
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByArrayColumnThroughAnyOfOtherColumn()
    {
        Expression query = Orders
            .Where(o => o.Tags.Any(t => t == o.Note))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."Note" = ANY (t0."Tags")) OR ((t0."Note" IS NULL) AND (ARRAY_POSITION(t0."Tags", NULL) IS NOT NULL))
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void NegatesNullableMembershipThroughAny()
    {
        List<int> ids = [1, 2];
        Expression query = Orders
            .Where(o => !ids.Any(i => i == o.Priority))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE NOT COALESCE(t0."Priority" IN (1, 2), FALSE)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByEmptyListThroughAny()
    {
        List<int> none = [];
        Expression query = Orders
            .Where(o => none.Any(i => i == o.Id))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE FALSE
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByListWithNullThroughAny()
    {
        int?[] priorities = [2, null];
        Expression query = Orders
            .Where(o => priorities.Any(i => i == o.Priority))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."Priority" IN (2)) OR (t0."Priority" IS NULL)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RejectsAnyComparingElementWithItself()
    {
        int[] ids = [1, 2];
        Expression query = Orders.Where(o => ids.Any(i => i == i * o.Id)).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Any", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsAnyWithOrderingPredicate()
    {
        List<int> ids = [1, 2];
        Expression query = Orders.Where(o => ids.Any(i => i > o.Id)).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Any", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsAnyWithEqualityIgnoringElement()
    {
        List<int> ids = [1, 2];
        Expression query = Orders.Where(o => ids.Any(i => o.Id == o.Quantity)).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Any", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsAnyOverStringCharacters()
    {
        Expression query = Orders.Where(o => o.Number.Any(c => c == 'x')).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Any", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CountsArrayElementsAsLong()
    {
        Expression query = Orders.Select(o => o.Tags.LongCount()).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CARDINALITY(t0."Tags")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByArrayElementCount()
    {
        Expression query = Orders
            .Where(o => o.Tags.Count() > 2)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE CARDINALITY(t0."Tags") > 2
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RejectsCountingStringCharacters()
    {
        Expression query = Orders.Select(o => o.Number.Count()).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Count", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsCountingArrayElementsWithPredicate()
    {
        Expression query = Orders.Select(o => o.Tags.Count(t => t == "x")).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Count", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsTakingMaximumOfArrayElements()
    {
        Expression query = Orders.Select(o => o.Tags.Max()).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Max", exception.Message, StringComparison.Ordinal);
    }
}
