namespace Linq.Expressions.Translation.PostgreSql.ExecutionTests;

// The markers against LINQ to Objects where the SQL they give is rejected or wrong: a
// fractional RANGE offset over an integer key, an equality in the step of a
// recursion that compares with a window aggregate of the level, columns of the step
// whose names differ in case only, a correlated recursion flattened by SelectMany,
// and derived tables whose column paths share their first 63 bytes.
[Collection(SharedDatabase.Name)]
public sealed class MarkerIssueSemanticsTests(Database database)
{
    // #123: the issue's frame, within a partition over a bigint key; over an integer
    // key, Range(-1.5, 0.5) holds the keys from 1 before to the current one.
    [Fact]
    public void SumsAFractionalRangeWithinAPartitionOverABigintKey()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders,
                o => o.CustomerId,
                rows => rows.OrderBy(o => o.Points),
                PostgreSqlFrame.Range(-1.5m, 0.5m),
                (o, w) => new { o.Id, Total = w.Rows.Sum(r => r.Total) }
            )
        );
    }

    // #124: the aggregate of the level's filtered rows in the step's projection is a
    // window aggregate of the step, as with a relational comparison.
    [Fact]
    public void ComparesWithAWindowAggregateOfTheLevelForEquality()
    {
        database.AssertSameRows(
            Trees.WithRecursive(
                Employees.Select(e => new Walk { Id = e.Id, Depth = 0 }),
                walk =>
                    walk.Where(a => a.Depth < 2)
                        .Select(a => new Walk
                        {
                            Id = a.Id,
                            Depth =
                                a.Id == walk.Where(b => b.Depth < 2).Max(b => b.Id)
                                    ? a.Depth + 1
                                    : a.Depth + 2,
                        })
            )
        );
    }

    // #124: the count of the level's filtered rows as the test of a conditional.
    [Fact]
    public void TestsACountOfTheLevelForEqualityInAConditional()
    {
        database.AssertSameRows(
            Trees.WithRecursive(
                Employees.Select(e => new Walk { Id = e.Id, Depth = 0 }),
                walk =>
                    walk.Where(a => a.Depth < 2)
                        .Select(a => new Walk
                        {
                            Id = a.Id,
                            Depth =
                                walk.Where(b => b.Depth < 2).Count() == 1
                                    ? a.Depth + 1
                                    : a.Depth + 2,
                        })
            )
        );
    }

    // The relational comparison of the same step is translated already.
    [Fact]
    public void ComparesWithAWindowAggregateOfTheLevel()
    {
        database.AssertSameRows(
            Trees.WithRecursive(
                Employees.Select(e => new Walk { Id = e.Id, Depth = 0 }),
                walk =>
                    walk.Where(a => a.Depth < 2)
                        .Select(a => new Walk
                        {
                            Id = a.Id,
                            Depth =
                                a.Id < walk.Where(b => b.Depth < 2).Max(b => b.Id)
                                    ? a.Depth + 1
                                    : a.Depth + 2,
                        })
            )
        );
    }

    // #142: the issue's record, whose members Id and ID are two columns. The rows are
    // read through other names, as the materializer matches names ignoring case.
    [Fact]
    public void KeepsAPositionalRecordWhoseMembersDifferInCaseOnly()
    {
        database.AssertSameRows(
            Trees
                .WithRecursive(
                    Employees.Where(e => e.Id < 3).Select(e => new CaseHop(e.Id, 0)),
                    level =>
                        level
                            .Where(h => h.ID < 2)
                            .Select(h => new CaseHop(h.Id, h.ID + 1))
                )
                .Select(h => new { Lower = h.Id, Upper = h.ID })
        );
    }

    // #143: the issue's walk, whose seed reads the outer row, flattened with a result
    // selector of a value tuple.
    [Fact(
        Skip = "#143: a correlated recursion flattened by SelectMany is joined without LATERAL"
    )]
    public void FlattensAWalkWhoseSeedReadsTheOuterRow()
    {
        IQueryable<ValueTuple<int, int, int>> query = Customers.SelectMany(
            row =>
                PostgreSqlRecursion.WithRecursive(
                    Orders
                        .Where(x => x.CustomerId == row.Id)
                        .Select(x => new Hop(x.Id, 0)),
                    level =>
                        level
                            .Where(x => x.Depth < 2)
                            .Select(x => new Hop(x.Id + 10, x.Depth + 1))
                ),
            (row, hop) => new ValueTuple<int, int, int>(row.Id, hop.Id, hop.Depth)
        );
        List<string> expected =
        [
            .. Seed
                .Customers.SelectMany(
                    c => Seed.Orders.Where(o => o.CustomerId == c.Id),
                    (c, o) => (c, o)
                )
                .SelectMany(
                    x => Enumerable.Range(0, 3),
                    (x, depth) =>
                        new ValueTuple<int, int, int>(
                            x.c.Id,
                            x.o.Id + (10 * depth),
                            depth
                        )
                )
                .Select(row => Canonical.Describe(row))
                .Order(StringComparer.Ordinal),
        ];
        Assert.NotEmpty(expected);

        ResultSet result = database.Query(
            new PostgreSqlQuery(query.Expression).TextValue
        );

        Assert.Equal(
            expected,
            result
                .Rows.Select(row =>
                    Canonical.Describe(
                        Materializer.Element(
                            typeof(ValueTuple<int, int, int>),
                            result,
                            row
                        )
                    )
                )
                .Order(StringComparer.Ordinal)
        );
    }

    // #125: twelve nested value tuples under DISTINCT; the paths of the innermost
    // members share their first 63 bytes ("item2." eleven times and more).
    [Fact(
        Skip = "#125: derived-table column paths past 63 bytes collide in PostgreSQL (a refusal is an acceptable fix)"
    )]
    public void ReadsNestedValueTuplesWhosePathsExceedTheIdentifierLength()
    {
        database.AssertSameRows(
            Nest(
                    Nest(
                        Nest(
                            Nest(
                                Nest(
                                    Nest(
                                        Nest(
                                            Nest(
                                                Nest(
                                                    Nest(
                                                        Nest(
                                                            Nest(
                                                                Orders.Select(
                                                                    o => new ValueTuple<
                                                                        int,
                                                                        int
                                                                    >(o.Id, o.Quantity)
                                                                )
                                                            )
                                                        )
                                                    )
                                                )
                                            )
                                        )
                                    )
                                )
                            )
                        )
                    )
                )
                .Distinct()
                .Where(t => t.Item1 > 0)
        );
    }

    // #125: eleven windows over one another; every Over nests the row once more, so
    // the paths of the first row's columns share their first 63 bytes.
    [Fact(
        Skip = "#125: derived-table column paths past 63 bytes collide in PostgreSQL (a refusal is an acceptable fix)"
    )]
    public void StacksWindowsUntilTheirPathsExceedTheIdentifierLength()
    {
        database.AssertSameRows(
            Counted(
                    Counted(
                        Counted(
                            Counted(
                                Counted(
                                    Counted(
                                        Counted(
                                            Counted(
                                                Counted(
                                                    Counted(
                                                        Counted(
                                                            Orders.Select(o => new Pair
                                                            {
                                                                Id = o.Id,
                                                                Quantity = o.Quantity,
                                                            })
                                                        )
                                                    )
                                                )
                                            )
                                        )
                                    )
                                )
                            )
                        )
                    )
                )
                .Where(t => t.Item2 > 0)
        );
    }

    // Under the identifier length, the same windows are read by their paths.
    [Fact]
    public void StacksWindowsWhosePathsFitTheIdentifierLength()
    {
        database.AssertSameRows(
            Counted(
                    Counted(
                        Counted(
                            Orders.Select(o => new Pair
                            {
                                Id = o.Id,
                                Quantity = o.Quantity,
                            })
                        )
                    )
                )
                .Where(t => t.Item2 > 0)
        );
    }

    private static IQueryable<ValueTuple<int, T>> Nest<T>(IQueryable<T> source)
    {
        return source.Select(x => new ValueTuple<int, T>(1, x));
    }

    private static IQueryable<ValueTuple<T, int>> Counted<T>(IQueryable<T> source)
    {
        return Trees.Over(
            source,
            null,
            null,
            null,
            (row, w) => new ValueTuple<T, int>(row, w.Rows.Count())
        );
    }

    public sealed record Walk
    {
        public int Id { get; init; }

        public int Depth { get; init; }
    }

    public sealed record Pair
    {
        public int Id { get; init; }

        public int Quantity { get; init; }
    }

#pragma warning disable CA1708 // the names differing in case only are under test
    public sealed record CaseHop(long Id, long ID);
#pragma warning restore CA1708

    public sealed record Hop(int Id, int Depth);
}
