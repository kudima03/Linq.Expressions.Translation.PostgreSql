namespace Linq.Expressions.Translation.PostgreSql.Tests.Markers;

// Steps of PostgreSqlRecursion.WithRecursive whose equalities read the level
// parameter itself, in a step PostgreSQL rejects: grouped, or reading the level
// again in a subquery. Each is refused with NotSupportedException, as the same steps
// with a relational comparison are. A recursion that reads the outer row of
// SelectMany is joined laterally.
public sealed record MarkerIssueTests
{
    public static TheoryData<Func<Expression>> Refused =>
        [
            // A grouped step, its key compared in a conditional with an aggregate of
            // the level.
            () =>
                Trees
                    .WithRecursive(
                        Employees.Select(e => new { e.Id, Depth = 0 }),
                        walk =>
                            walk.Where(a => a.Depth < 2)
                                .GroupBy(a => a.Id)
                                .Select(g => new
                                {
                                    Id = g.Key,
                                    Depth = g.Key == walk.Max(b => b.Id) ? 1 : 2,
                                })
                    )
                    .Expression,
            // A grouped step, its count compared in a conditional with the count of
            // the level.
            () =>
                Trees
                    .WithRecursive(
                        Employees.Select(e => new { e.Id, Depth = 0 }),
                        walk =>
                            walk.Where(a => a.Depth < 2)
                                .GroupBy(a => a.Id)
                                .Select(g => new
                                {
                                    Id = g.Key,
                                    Depth = g.Count() == walk.Count() ? 1 : 2,
                                })
                    )
                    .Expression,
            // The level read again in a scalar subquery of an equality in a filter.
            () =>
                Trees
                    .WithRecursive(
                        Employees,
                        walk => walk.Where(a => a.Id == walk.Max(b => b.Id))
                    )
                    .Expression,
        ];

    // #124: Observed: KeyNotFoundException: The given key 'walk' was not present in
    // the dictionary (IsCorrelation looks the level parameter up in the bindings).
    [Theory]
    [MemberData(nameof(Refused))]
    public void RefusesAnEqualityReadingTheLevelInAStepPostgreSqlRejects(
        Func<Expression> query
    )
    {
        _ = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query()).TextValue
        );
    }

    // #143: a recursion whose seed reads the outer row is a derived table that reads
    // it inside, which only LATERAL lets it do.
    [Fact]
    public void JoinsARecursionWhoseSeedReadsTheOuterRowLaterally()
    {
        Expression query = Customers
            .SelectMany(
                row =>
                    PostgreSqlRecursion.WithRecursive(
                        Orders
                            .Where(x => x.CustomerId == row.Id)
                            .Select(x => new { x.Id, Depth = 0 }),
                        level =>
                            level
                                .Where(x => x.Depth < 2)
                                .Select(x => new { Id = x.Id + 10, Depth = x.Depth + 1 })
                    ),
                (row, hop) =>
                    new
                    {
                        Customer = row.Id,
                        hop.Id,
                        hop.Depth,
                    }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id" AS "Customer", t4."Id", t4."Depth"
                FROM "Customer" AS t0
                INNER JOIN LATERAL (
                    WITH RECURSIVE t2 AS (
                        SELECT t1."Id", CAST(0 AS integer) AS "Depth"
                        FROM "Order" AS t1
                        WHERE t1."CustomerId" = t0."Id"
                        UNION ALL
                        SELECT t3."Id" + 10 AS "Id", t3."Depth" + 1 AS "Depth"
                        FROM t2 AS t3
                        WHERE t3."Depth" < 2
                    )
                    SELECT t2."Id", t2."Depth"
                    FROM t2
                ) AS t4 ON TRUE
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
