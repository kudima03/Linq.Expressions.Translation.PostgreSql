using Xunit.Sdk;

namespace Linq.Expressions.Translation.PostgreSql.ExecutionTests;

// WITH RECURSIVE returns the rows of every level, from the seed to the first empty
// level, as the in-memory side computes them level by level: hierarchies of several
// levels, walks over a cyclic graph stopped by a predicate, and a depth limit that
// fails the statement instead of cutting the walk off, also when the query around
// it reads only some rows.
[Collection(SharedDatabase.Name)]
public sealed class RecursionSemanticsTests(Database database)
{
    private static IQueryable<Walk> Reports(long? maxDepth = null)
    {
        IQueryable<Walk> seed = Employees
            .Where(e => e.ManagerId == null)
            .Select(e => new Walk
            {
                Id = e.Id,
                Depth = 0,
                Path = e.Name,
            });
        Expression<Func<IQueryable<Walk>, IQueryable<Walk>>> step = level =>
            level.Join(
                Employees,
                w => w.Id,
                e => e.ManagerId,
                (w, e) =>
                    new Walk
                    {
                        Id = e.Id,
                        Depth = w.Depth + 1,
                        Path = w.Path + "/" + e.Name,
                    }
            );
        return maxDepth is long depth
            ? Trees.WithRecursive(seed, step, depth)
            : Trees.WithRecursive(seed, step);
    }

    // The walks over the graph that never visit a node twice.
    private static IQueryable<Walk> Paths(long? maxDepth = null, bool stopped = true)
    {
        IQueryable<Walk> seed = MappedTables
            .Edges.Where(e => e.From == 1 || e.From == 5)
            .Select(e => new Walk
            {
                Id = e.From,
                Depth = 0,
                Path = "/" + e.From + "/",
            });
        Expression<Func<IQueryable<Walk>, IQueryable<Walk>>> step = stopped
            ? level =>
                level
                    .Join(
                        MappedTables.Edges,
                        w => w.Id,
                        e => e.From,
                        (w, e) =>
                            new Walk
                            {
                                Id = e.To,
                                Depth = w.Depth + 1,
                                Path = w.Path + e.To + "/",
                            }
                    )
                    .Where(w =>
                        !w.Path.Substring(0, w.Path.Length - 1).Contains("/" + w.Id + "/")
                    )
            : level =>
                level.Join(
                    MappedTables.Edges,
                    w => w.Id,
                    e => e.From,
                    (w, e) =>
                        new Walk
                        {
                            Id = e.To,
                            Depth = w.Depth + 1,
                            Path = w.Path + e.To + "/",
                        }
                );
        return maxDepth is long depth
            ? Trees.WithRecursive(seed, step, depth)
            : Trees.WithRecursive(seed, step);
    }

    [Fact]
    public void WalksAHierarchyOfSeveralLevels()
    {
        database.AssertSameRows(Reports());
        database.AssertSameRows(Reports(2));
        database.AssertSameRows(Reports(100));
    }

    [Fact]
    public void ReadsTheEntitiesOfEveryLevel()
    {
        database.AssertSameRows(
            Trees.WithRecursive(
                Employees.Where(e => e.Id == 1),
                level => level.Join(Employees, m => m.Id, e => e.ManagerId, (m, e) => e)
            )
        );
        database.AssertSameRows(
            Trees.WithRecursive(
                Employees.Where(e => e.Id == 1),
                level => level.SelectMany(m => Employees.Where(e => e.ManagerId == m.Id))
            )
        );
        database.AssertSameRows(
            Trees.WithRecursive(
                Employees.Where(e => e.ManagerId == null),
                level => Employees.Join(level, e => e.ManagerId, m => m.Id, (e, m) => e)
            )
        );
    }

    [Fact]
    public void CountsUpWithScalarRows()
    {
        database.AssertSameRows(
            Trees.WithRecursive(
                Orders.Where(o => o.Id == 1).Select(o => o.Quantity),
                level => level.Where(n => n < 10).Select(n => n + 1)
            )
        );
        database.AssertSameRows(
            Trees.WithRecursive(
                Orders.Where(o => o.Id <= 2).Select(o => (long)o.Id),
                level => level.Where(n => n < 1000).Select(n => n * 3)
            )
        );
    }

    [Fact]
    public void AccumulatesColumnsTypedByTheSeed()
    {
        database.AssertSameRows(
            Trees.WithRecursive(
                Employees
                    .Where(e => e.ManagerId == null)
                    .Select(e => new Tally
                    {
                        Id = e.Id,
                        Sum = 0m,
                        Big = 1L,
                        Note = null,
                    }),
                level =>
                    level.Join(
                        Employees,
                        t => t.Id,
                        e => e.ManagerId,
                        (t, e) =>
                            new Tally
                            {
                                Id = e.Id,
                                Sum = t.Sum + (e.Id / 2m),
                                Big = t.Big * 1_000_000_000L,
                                Note = e.Name,
                            }
                    )
            )
        );
    }

    [Fact]
    public void StopsACycleByAPredicate()
    {
        database.AssertSameRows(Paths());
        database.AssertSameRows(Paths(10));
    }

    [Fact]
    public void FailsBeyondTheMaximumDepth()
    {
        AssertTooDeep(Reports(1), 1);
        AssertTooDeep(Reports(0), 0);
        AssertTooDeep(Paths(5, false), 5);
    }

    [Fact]
    public void FailsBeyondTheMaximumDepthWhateverIsRead()
    {
        AssertTooDeep(Paths(3, false).Where(w => w.Depth == 0).Take(1), 3);
        AssertTooDeep(Paths(3, false).Select(w => w.Id).Distinct(), 3);
    }

    [Fact]
    public void ComposesWithTheQueryAroundIt()
    {
        database.AssertSameRows(Reports(5).Where(w => w.Depth > 0).Select(w => w.Path));
        database.AssertSameRows(
            Reports()
                .Join(Employees, w => w.Id, e => e.Id, (w, e) => new { e.Name, w.Depth })
        );
        database.AssertSameSequence(Reports(5).OrderBy(w => w.Depth).ThenBy(w => w.Id));
        database.AssertSameResult(Reports(5), q => q.Count());
        database.AssertSameResult(Reports(5), q => q.Max(w => w.Depth));
        database.AssertSameRows(
            Trees.Over(
                Reports(),
                w => w.Depth,
                rows => rows.OrderBy(w => w.Id),
                null,
                (w, window) => new { w.Id, Number = window.RowNumber() }
            )
        );
        IQueryable<Walk> reports = Reports();
        database.AssertSameRows(
            Employees.Select(e => new
            {
                e.Id,
                Below = reports.Count(w => w.Path.StartsWith(e.Name + "/")),
            })
        );
    }

    [Fact]
    public void SeedsFromACombinedQuery()
    {
        database.AssertSameRows(
            Trees.WithRecursive(
                Employees
                    .Where(e => e.Id == 2)
                    .Select(e => e.Id)
                    .Concat(Employees.Where(e => e.Id == 3).Select(e => e.Id)),
                level =>
                    level.Join(Employees, id => id, e => e.ManagerId, (id, e) => e.Id)
            )
        );
        database.AssertSameRows(
            Trees.WithRecursive(
                Employees.OrderBy(e => e.Id).Take(1).Select(e => e.Id),
                level =>
                    level.Join(Employees, id => id, e => e.ManagerId, (id, e) => e.Id)
            )
        );
    }

    [Fact]
    public void ReadsLevelsWithANullableColumn()
    {
        database.AssertSameRows(
            Trees.WithRecursive(
                Employees
                    .Where(e => e.Id == 4)
                    .Select(e => new Chain { Id = e.Id, Manager = e.ManagerId }),
                level =>
                    level
                        .Where(c => c.Manager != null)
                        .Join(
                            Employees,
                            c => c.Manager,
                            e => e.Id,
                            (c, e) => new Chain { Id = e.Id, Manager = e.ManagerId }
                        )
            )
        );
    }

    [Fact]
    public void ReturnsNoRowForAnEmptySeed()
    {
        database.AssertSameResult(
            Trees.WithRecursive(
                Employees.Where(e => e.Id > 1000),
                level => level.Join(Employees, m => m.Id, e => e.ManagerId, (m, e) => e),
                0L
            ),
            q => q.Count()
        );
    }

    private void AssertTooDeep<T>(IQueryable<T> query, long depth)
    {
        string message = $"a row deeper than the maximum depth of {depth}";
        InvalidOperationException memory = Assert.Throws<InvalidOperationException>(() =>
            new EnumerableQuery<T>(InMemory.Rewrite(query.Expression)).ToList()
        );
        Assert.Contains(message, memory.Message, StringComparison.Ordinal);
        XunitException sql = Assert.Throws<XunitException>(() =>
            database.Query(new PostgreSqlQuery(query.Expression).TextValue)
        );
        Assert.Contains(message, sql.Message, StringComparison.Ordinal);
    }

    public sealed record Walk
    {
        public int Id { get; init; }

        public int Depth { get; init; }

        public string Path { get; init; } = string.Empty;
    }

    public sealed record Tally
    {
        public int Id { get; init; }

        public decimal Sum { get; init; }

        public long Big { get; init; }

        public string? Note { get; init; }
    }

    public sealed record Chain
    {
        public int Id { get; init; }

        public int? Manager { get; init; }
    }
}
