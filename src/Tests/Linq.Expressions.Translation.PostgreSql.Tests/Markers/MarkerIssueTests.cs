namespace Linq.Expressions.Translation.PostgreSql.Tests.Markers;

// Steps of PostgreSqlRecursion.WithRecursive whose equalities read the level
// parameter itself, in a step PostgreSQL rejects: grouped, or reading the level
// again in a subquery. Each is refused with NotSupportedException, as the same steps
// with a relational comparison are.
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
}
