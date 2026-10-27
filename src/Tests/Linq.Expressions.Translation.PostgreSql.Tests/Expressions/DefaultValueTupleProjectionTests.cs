#pragma warning disable xUnit1004 // skipped tests record SQL the translator does not produce yet

namespace Linq.Expressions.Translation.PostgreSql.Tests.Expressions;

public sealed record DefaultValueTupleProjectionTests
{
    [Fact(Skip = "A default value tuple cannot be projected (#49).")]
    public void ProjectsDefaultValueTupleNestedInConstructedTuple()
    {
        Expression query = Orders
            .Select(o => new ValueTuple<ValueTuple<int?>, string?>(default, o.Note))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT NULL AS "item1.item1", t0."Note" AS "item2"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "A default value tuple cannot be projected (#49).")]
    public void ProjectsDefaultValueTupleAsAnonymousMember()
    {
        Expression query = Orders
            .Select(o => new { Pair = default(ValueTuple<int?, string?>), o.Id })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT NULL AS "Pair.item1", NULL AS "Pair.item2", t0."Id"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "A default value tuple cannot be projected (#49).")]
    public void ProjectsDefaultValueTupleAsResult()
    {
        Expression query = Orders
            .Select(o => default(ValueTuple<int?, string?>))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT NULL AS "item1", NULL AS "item2"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "A captured value tuple cannot be projected (#49).")]
    public void ProjectsCapturedDefaultValueTuple()
    {
        ValueTuple<int?, string?> empty = default;
        Expression query = Orders.Select(o => new { Pair = empty, o.Id }).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT NULL AS "Pair.item1", NULL AS "Pair.item2", t0."Id"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "A captured value tuple cannot be projected (#49).")]
    public void ProjectsCapturedValueTupleOfValuesAsLiterals()
    {
        ValueTuple<int?, string?> pair = (1, "a");
        Expression query = Orders.Select(o => new { Pair = pair, o.Id }).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT 1 AS "Pair.item1", 'a' AS "Pair.item2", t0."Id"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "A default value tuple cannot be projected (#49).")]
    public void ConcatenatesUnmatchedRowsWithDefaultValueTupleIntoFullOuterJoin()
    {
        Expression query = (
            from o in Orders
            join c in Customers on o.CustomerId equals c.Id into cs
            from c in cs.DefaultIfEmpty()
            select new ValueTuple<ValueTuple<int?>, string?>(
                new ValueTuple<int?>(o.Priority),
                c.Name
            )
        )
            .Concat(
                Customers
                    .Where(c => !Orders.Any(o => o.CustomerId == c.Id))
                    .Select(c => new ValueTuple<ValueTuple<int?>, string?>(
                        default,
                        c.Name
                    ))
            )
            .Select(r => new { r.Item1.Item1, r.Item2 })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t4."item1.item1" AS "Item1", t4."item2" AS "Item2"
                FROM (
                    SELECT t0."Priority" AS "item1.item1", t1."Name" AS "item2"
                    FROM "Order" AS t0
                    LEFT JOIN "Customer" AS t1 ON t0."CustomerId" = t1."Id"
                    UNION ALL
                    SELECT NULL AS "item1.item1", t2."Name" AS "item2"
                    FROM "Customer" AS t2
                    WHERE NOT EXISTS (
                        SELECT 1
                        FROM "Order" AS t3
                        WHERE t3."CustomerId" = t2."Id"
                    )
                ) AS t4
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
