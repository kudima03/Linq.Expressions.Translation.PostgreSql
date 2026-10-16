namespace Linq.Expressions.Translation.PostgreSql.Tests.Operators;

public sealed record WrappedComputedValueTests
{
    [Fact]
    public void FiltersComputedUnionValues()
    {
        Expression query = Orders
            .Select(o => o.Quantity + 1)
            .Union(Orders.Select(o => o.Id))
            .Where(x => x > 2)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t2."Value"
                FROM (
                    SELECT t0."Quantity" + 1 AS "Value" FROM "Order" AS t0
                    UNION
                    SELECT t1."Id" FROM "Order" AS t1
                ) AS t2
                WHERE t2."Value" > 2
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CountsComputedValuesOfNestedSetOperations()
    {
        Expression query = Orders
            .Select(o => o.Quantity * 2)
            .Union(Orders.Select(o => o.Id))
            .Concat(Orders.Select(o => o.CustomerId))
            .Executed(q => q.Count());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COUNT(*) AS integer)
                FROM (
                    (SELECT t0."Quantity" * 2 AS "Value" FROM "Order" AS t0 UNION SELECT t1."Id" FROM "Order" AS t1)
                    UNION ALL
                    SELECT t2."CustomerId" FROM "Order" AS t2
                ) AS t3
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AveragesTakenComputedValues()
    {
        Expression query = Orders
            .Select(o => o.Total * 2)
            .Take(10)
            .Executed(q => q.Average());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT AVG(t1."Value")
                FROM (
                    SELECT t0."Total" * 2 AS "Value"
                    FROM "Order" AS t0
                    LIMIT 10
                ) AS t1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void KeepsColumnNameWhenWrappingColumn()
    {
        Expression query = Orders
            .Select(o => o.Quantity)
            .Take(3)
            .Where(x => x > 2)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Quantity"
                FROM (
                    SELECT t0."Quantity"
                    FROM "Order" AS t0
                    LIMIT 3
                ) AS t1
                WHERE t1."Quantity" > 2
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
