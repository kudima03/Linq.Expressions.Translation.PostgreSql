#pragma warning disable CA1861 // the constant arrays are the sources under test

using Xunit.Sdk;

namespace Linq.Expressions.Translation.PostgreSql.ExecutionTests;

// Edge cases of WITH RECURSIVE against LINQ to Objects: steps that PostgreSQL allows
// in the recursive term (DISTINCT, LIMIT, joins with VALUES), column types that the
// seed decides and the step has to keep, shapes of every kind, the query around the
// recursion, and the depth limit wherever the rows are read.
[Collection(SharedDatabase.Name)]
public sealed class RecursionEdgeCaseSemanticsTests(Database database)
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

    // The walks over the cyclic graph that never stop.
    private static IQueryable<Walk> Cycle(long maxDepth)
    {
        return Trees.WithRecursive(
            MappedTables
                .Edges.Where(e => e.From == 1)
                .Select(e => new Walk
                {
                    Id = e.From,
                    Depth = 0,
                    Path = "/",
                }),
            level =>
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
                ),
            maxDepth
        );
    }

    // The walks over the cyclic graph, stopped at a depth of 5.
    private static IQueryable<Walk> Loops()
    {
        return Trees.WithRecursive(
            MappedTables
                .Edges.Where(e => e.From == 1)
                .Select(e => new Walk
                {
                    Id = e.From,
                    Depth = 0,
                    Path = "/",
                }),
            level =>
                level
                    .Where(w => w.Depth < 5)
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
                    ),
            10
        );
    }

    [Fact]
    public void DeduplicatesEachLevelWithDistinct()
    {
        database.AssertSameRows(
            Trees.WithRecursive(
                MappedTables.Edges.Where(e => e.From != 5).Select(e => e.From),
                level => level.Where(n => n < 6).Select(n => n + 1).Distinct()
            )
        );
    }

    [Fact]
    public void TakesTheFirstRowsOfEachLevel()
    {
        database.AssertSameRows(
            Trees.WithRecursive(
                MappedTables.Edges.Select(e => e.From),
                level =>
                    level.Where(n => n < 6).Select(n => n + 1).OrderBy(n => n).Take(2)
            )
        );
        database.AssertSameRows(
            Trees.WithRecursive(
                Employees
                    .Where(e => e.ManagerId == null)
                    .Select(e => new Walk
                    {
                        Id = e.Id,
                        Depth = 0,
                        Path = e.Name,
                    }),
                level =>
                    level
                        .Join(
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
                        )
                        .OrderByDescending(w => w.Id)
                        .Take(1)
            )
        );
    }

    [Fact]
    public void SkipsTheFirstRowsOfEachLevel()
    {
        database.AssertSameRows(
            Trees.WithRecursive(
                MappedTables.Edges.Select(e => e.From),
                level =>
                    level.Where(n => n < 6).Select(n => n + 1).OrderBy(n => n).Skip(1)
            )
        );
    }

    [Fact]
    public void KeepsTheSeedTypesForConstantsOfTheStep()
    {
        database.AssertSameRows(
            Trees.WithRecursive(
                Employees
                    .Where(e => e.ManagerId == null)
                    .Select(e => new Tally
                    {
                        Id = e.Id,
                        Big = e.Id,
                        Sum = e.Id,
                        Ratio = e.Id,
                        Note = e.Name,
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
                                Big = 0L,
                                Sum = 1.5m,
                                Ratio = 0.25,
                                Note = null,
                            }
                    )
            )
        );
    }

    [Fact]
    public void WidensASeedColumnToItsDotNetType()
    {
        // The seed column is an int widened to long: multiplying it in the step must
        // not overflow a 32-bit integer.
        database.AssertSameRows(
            Trees.WithRecursive(
                Employees
                    .Where(e => e.ManagerId == null)
                    .Select(e => new Tally
                    {
                        Id = e.Id,
                        Big = e.Id,
                        Sum = e.Id,
                        Ratio = e.Id,
                        Note = e.Name,
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
                                Big = (t.Big + 3) * 1_000_000_000L,
                                Sum = (t.Sum * 3) + 0.125m,
                                Ratio = t.Ratio / 4,
                                Note = t.Note + e.Id,
                            }
                    )
            )
        );
    }

    [Fact]
    public void CountsInSmallintsTypedBySmallintColumns()
    {
        database.AssertSameRows(
            Trees.WithRecursive(
                MappedTables.OrderEvents.Select(x => new Severity
                {
                    Id = x.Id,
                    Level = x.Severity,
                }),
                level =>
                    level
                        .Where(s => s.Level < 6)
                        .Select(s => new Severity
                        {
                            Id = s.Id,
                            Level = (short)(s.Level + 1),
                        })
            )
        );
        database.AssertSameRows(
            Trees.WithRecursive(
                MappedTables.OrderEvents.Select(x => new Severity
                {
                    Id = x.Id,
                    Level = 0,
                }),
                level =>
                    level
                        .Where(s => s.Level < 3)
                        .Select(s => new Severity
                        {
                            Id = s.Id,
                            Level = (short)(s.Level + 1),
                        })
            )
        );
    }

    // Observed: 42804: recursive query "t1" column 2 has type real in
    // non-recursive term but type double precision overall (t2."Value" * 2).
    [Fact(
        Skip = "#131: float arithmetic of the step is double precision, not the seed's real"
    )]
    public void ScalesRealsTypedByARealColumn()
    {
        database.AssertSameRows(
            Trees.WithRecursive(
                MappedTables.OrderEvents.Select(x => new Score
                {
                    Id = x.Id,
                    Value = x.Score,
                }),
                level =>
                    level
                        .Where(s => s.Value > 0 && s.Value < 100)
                        .Select(s => new Score { Id = s.Id, Value = s.Value * 2 })
            )
        );
    }

    // Observed: 42804: recursive query "t1" column 2 has type real in
    // non-recursive term but type double precision overall (t2."Value" + 0.5).
    [Fact(
        Skip = "#131: float arithmetic of the step is double precision, not the seed's real"
    )]
    public void AddsRealConstantsInTheStep()
    {
        database.AssertSameRows(
            Trees.WithRecursive(
                MappedTables.OrderEvents.Select(x => new Score { Id = x.Id, Value = 0f }),
                level =>
                    level
                        .Where(s => s.Value < 2)
                        .Select(s => new Score { Id = s.Id, Value = s.Value + 0.5f })
            )
        );
    }

    // Observed: 42804: recursive query "t1" column 3 has type text in
    // non-recursive term but type text[] overall (the seed writes NULL AS "Tags").
    [Fact(Skip = "#128: a null array constant of the seed is not cast to its array type")]
    public void ReadsArraysFromANullSeedColumn()
    {
        database.AssertSameRows(
            Trees.WithRecursive(
                Orders
                    .Where(o => o.Id == 1)
                    .Select(o => new Tagged
                    {
                        Id = o.Id,
                        Depth = 0,
                        Tags = null,
                    }),
                level =>
                    level
                        .Where(t => t.Depth < 1)
                        .Join(
                            Orders,
                            t => t.Id,
                            o => o.Id,
                            (t, o) =>
                                new Tagged
                                {
                                    Id = o.Id,
                                    Depth = t.Depth + 1,
                                    Tags = o.Tags,
                                }
                        )
            )
        );
    }

    // Observed: 42804: recursive query "t1" column 3 has type text in
    // non-recursive term but type text[] overall (the seed writes '{}' AS "Tags").
    [Fact(
        Skip = "#128: an empty array constant of the seed is not cast to its array type"
    )]
    public void ReadsArraysFromAnEmptyArraySeedColumn()
    {
        database.AssertSameRows(
            Trees.WithRecursive(
                Orders
                    .Where(o => o.Id == 1)
                    .Select(o => new Tagged
                    {
                        Id = o.Id,
                        Depth = 0,
                        Tags = new string[0],
                    }),
                level =>
                    level
                        .Where(t => t.Depth < 1)
                        .Join(
                            Orders,
                            t => t.Id,
                            o => o.Id,
                            (t, o) =>
                                new Tagged
                                {
                                    Id = o.Id,
                                    Depth = t.Depth + 1,
                                    Tags = o.Tags,
                                }
                        )
            )
        );
    }

    [Fact]
    public void StepsDatesAndTimesFromConstants()
    {
        database.AssertSameRows(
            Trees.WithRecursive(
                Employees
                    .Where(e => e.Id == 1)
                    .Select(e => new Moments
                    {
                        Step = 0,
                        Day = new DateOnly(2024, 2, 28),
                        At = new DateTime(2024, 12, 31, 23, 0, 0),
                        Time = new TimeOnly(23, 30),
                        Span = TimeSpan.FromHours(1),
                        Key = new Guid("0f8fad5b-d9cb-469f-a165-70867728950e"),
                    }),
                level =>
                    level
                        .Where(m => m.Step < 3)
                        .Select(m => new Moments
                        {
                            Step = m.Step + 1,
                            Day = m.Day.AddDays(1),
                            At = m.At.AddHours(1),
                            Time = m.Time.AddMinutes(20),
                            Span = m.Span + TimeSpan.FromMinutes(90),
                            Key = m.Key,
                        })
            )
        );
    }

    [Fact]
    public void StepsDatesFromColumns()
    {
        database.AssertSameRows(
            Trees.WithRecursive(
                Orders
                    .Where(o => o.Id == 1)
                    .Select(o => new Moments
                    {
                        Step = 0,
                        Day = o.ShippedOn!.Value,
                        At = o.CreatedAt,
                        Time = o.DeliveryTime,
                        Span = TimeSpan.Zero,
                        Key = o.TrackingId,
                    }),
                level =>
                    level
                        .Where(m => m.Step < 2)
                        .Select(m => new Moments
                        {
                            Step = m.Step + 1,
                            Day = m.Day.AddMonths(1),
                            At = m.At.AddDays(-1),
                            Time = m.Time.AddHours(-10),
                            Span = m.Span - TimeSpan.FromSeconds(1),
                            Key = m.Key,
                        })
            )
        );
    }

    [Fact]
    public void StepsNullableFlagsEnumsAndCharacters()
    {
        database.AssertSameRows(
            Trees.WithRecursive(
                Orders
                    .Where(o => o.Id == 1)
                    .Select(o => new Marks
                    {
                        Step = 0,
                        Gift = null,
                        Status = null,
                        Letter = 'a',
                        Flag = true,
                    }),
                level =>
                    level
                        .Where(m => m.Step < 3)
                        .Select(m => new Marks
                        {
                            Step = m.Step + 1,
                            Gift = m.Step > 0,
                            Status = OrderStatus.Paid,
                            Letter = 'b',
                            Flag = !m.Flag,
                        })
            )
        );
    }

    [Fact]
    public void RecursesOverStrings()
    {
        database.AssertSameRows(
            Trees.WithRecursive(
                Customers.Where(c => c.Id == 1).Select(c => "a"),
                level => level.Where(s => s.Length < 4).Select(s => s + "b")
            )
        );
        database.AssertSameRows(
            Trees.WithRecursive(
                Customers.Where(c => c.Id <= 3).Select(c => c.Name),
                level => level.Where(s => s.Length < 6).Select(s => s + "'")
            )
        );
    }

    [Fact]
    public void RecursesFromANullScalar()
    {
        database.AssertSameRows(
            Trees.WithRecursive(
                Customers.Where(c => c.Id == 1).Select(c => (int?)null),
                level =>
                    level.Where(n => n == null || n < 3).Select(n => (int?)((n ?? 0) + 1))
            )
        );
        database.AssertSameRows(
            Trees.WithRecursive(
                Customers.Where(c => c.Id == 1).Select(c => (string?)null),
                level =>
                    level
                        .Where(s => s == null || s.Length < 3)
                        .Select(s => (string?)(s + "x"))
            )
        );
    }

    [Fact]
    public void SeedsFromAnInMemorySequence()
    {
        database.AssertSameRows(
            Trees.WithRecursive(
                new[] { 1L, 5L }.AsQueryable(),
                level => level.Where(n => n < 3_000_000_000L).Select(n => n * 100)
            )
        );
        database.AssertSameRows(
            Trees.WithRecursive(
                new[] { (1, "a"), (2, "b") }.AsQueryable(),
                level =>
                    level
                        .Where(t => t.Item1 < 4)
                        .Select(t => new ValueTuple<int, string>(
                            t.Item1 + 1,
                            t.Item2 + "+"
                        ))
            )
        );
    }

    [Fact]
    public void JoinsTheLevelWithAnInMemorySequence()
    {
        (int From, int To)[] links = [(1, 2), (2, 3), (2, 4), (4, 5)];

        database.AssertSameRows(
            Trees.WithRecursive(
                Employees.Where(e => e.Id == 1).Select(e => e.Id),
                level => level.Join(links, n => n, l => l.From, (n, l) => l.To)
            )
        );
    }

    [Fact]
    public void WalksEntitiesWithMappedNames()
    {
        database.AssertSameRows(
            Trees.WithRecursive(
                MappedTables.OrderEvents.Where(x => x.OrderId == null),
                level =>
                    level.Join(
                        MappedTables.OrderEvents,
                        p => p.Id,
                        x => (long?)x.OrderId,
                        (p, x) => x
                    )
            )
        );
        database.AssertSameRows(
            Trees.WithRecursive(
                MappedTables.OrderEvents.Select(x => new Mapped
                {
                    Id = x.Id,
                    Kind = x.Kind,
                }),
                level =>
                    level
                        .Where(m => m.Kind.Length < 12)
                        .Select(m => new Mapped { Kind = m.Kind + "!", Id = m.Id })
            )
        );
    }

    [Fact]
    public void WalksNestedObjects()
    {
        database.AssertSameRows(
            Trees.WithRecursive(
                Employees
                    .Where(e => e.ManagerId == null)
                    .Select(e => new Node
                    {
                        Id = e.Id,
                        Info = new Info { Depth = 0, Name = e.Name },
                    }),
                level =>
                    level.Join(
                        Employees,
                        n => n.Id,
                        e => e.ManagerId,
                        (n, e) =>
                            new Node
                            {
                                Id = e.Id,
                                Info = new Info
                                {
                                    Depth = n.Info.Depth + 1,
                                    Name = n.Info.Name + ">" + e.Name,
                                },
                            }
                    )
            )
        );
    }

    [Fact]
    public void WalksAnonymousRows()
    {
        database.AssertSameRows(
            Trees.WithRecursive(
                Employees
                    .Where(e => e.ManagerId == null)
                    .Select(e => new
                    {
                        e.Id,
                        Depth = 0,
                        Top = e.Name,
                    }),
                level =>
                    level.Join(
                        Employees,
                        w => w.Id,
                        e => e.ManagerId,
                        (w, e) =>
                            new
                            {
                                e.Id,
                                Depth = w.Depth + 1,
                                w.Top,
                            }
                    )
            )
        );
    }

    // Observed: ArgumentException: An item with the same key has already been
    // added. Key: Id (the step's columns are matched ignoring case). The result is
    // renamed after the recursion, as the materializer matches names ignoring case.
    [Fact]
    public void KeepsColumnsWhoseNamesDifferInCaseOnly()
    {
        database.AssertSameRows(
            Trees
                .WithRecursive(
                    Employees.Where(e => e.Id == 1).Select(e => new { id = 0, e.Id }),
                    level =>
                        level
                            .Where(r => r.id < 2)
                            .Select(r => new { id = r.id + 1, r.Id })
                )
                .Select(r => new { Depth = r.id, Employee = r.Id })
        );
    }

    [Fact]
    public void ReadsTheLevelThroughALeftJoinOfItsOwn()
    {
        // The level is the preserved side: PostgreSQL allows it.
        database.AssertSameRows(
            Trees.WithRecursive(
                Employees
                    .Where(e => e.Id == 2 || e.Id == 3)
                    .Select(e => new Chain { Id = e.Id, Manager = e.ManagerId }),
                level =>
                    level
                        .Where(c => c.Id < 4)
                        .GroupJoin(
                            Employees,
                            c => c.Id,
                            e => e.ManagerId,
                            (c, es) => new { c, es }
                        )
                        .SelectMany(
                            x => x.es.DefaultIfEmpty(),
                            (x, e) =>
                                new Chain
                                {
                                    Id = x.c.Id + 10,
                                    Manager = e == null ? null : e.Id,
                                }
                        )
            )
        );
    }

    [Fact]
    public void FiltersTheLevelWithAnUncorrelatedSubquery()
    {
        database.AssertSameRows(
            Trees.WithRecursive(
                Employees.Where(e => e.ManagerId == null),
                level =>
                    level
                        .Where(m => Customers.Any(c => c.Id == m.Id))
                        .Join(Employees, m => m.Id, e => e.ManagerId, (m, e) => e)
            )
        );
        database.AssertSameRows(
            Trees.WithRecursive(
                Employees.Where(e => e.ManagerId == null),
                level =>
                    level.Join(
                        Employees.Where(e => !Orders.Any(o => o.Id == e.Id * 100)),
                        m => m.Id,
                        e => e.ManagerId,
                        (m, e) => e
                    )
            )
        );
    }

    [Fact]
    public void CountsEachLevelAsAWindowOverIt()
    {
        database.AssertSameRows(
            Trees.WithRecursive(
                MappedTables.Edges.Select(e => new Counted { Id = e.From, Rows = 0 }),
                level =>
                    level
                        .Where(c => c.Id < 4)
                        .Select(c => new Counted
                        {
                            Id = c.Id + 1,
                            Rows = level.Where(d => d.Id < 4).Count(),
                        })
            )
        );
    }

    [Fact]
    public void GroupsTheRowsOfEveryLevel()
    {
        database.AssertSameRows(
            Reports().GroupBy(w => w.Depth).Select(g => new { g.Key, Count = g.Count() })
        );
        database.AssertSameRows(
            Loops()
                .GroupBy(w => w.Id)
                .Select(g => new { g.Key, Max = g.Max(w => w.Depth) })
        );
    }

    [Fact]
    public void DeduplicatesTheRowsOfEveryLevel()
    {
        database.AssertSameRows(Loops().Select(w => w.Id).Distinct());
        database.AssertSameResult(Loops().Select(w => w.Id), q => q.Distinct().Count());
    }

    [Fact]
    public void PagesTheOrderedRows()
    {
        database.AssertSameSequence(
            Reports(10).OrderBy(w => w.Path).Skip(1).Take(3).Select(w => w.Path)
        );
        database.AssertSameResult(
            Reports(),
            q => q.OrderByDescending(w => w.Path).First()
        );
        database.AssertSameResult(Reports(), q => q.Any(w => w.Depth == 2));
        database.AssertSameResult(Reports(), q => q.All(w => w.Depth < 2));
        database.AssertSameResult(Reports(), q => q.Sum(w => w.Depth));
    }

    [Fact]
    public void CombinesTwoRecursions()
    {
        IQueryable<Walk> down = Reports();
        IQueryable<Walk> cycle = Loops();

        database.AssertSameRows(down.Select(w => w.Id).Union(cycle.Select(w => w.Id)));
        database.AssertSameRows(
            down.Select(w => w.Id).Intersect(cycle.Select(w => w.Id))
        );
        database.AssertSameRows(down.Select(w => w.Id).Except(cycle.Select(w => w.Id)));
        database.AssertSameRows(
            down.Join(cycle, d => d.Id, c => c.Id, (d, c) => new { d.Path, c.Depth })
        );
        database.AssertSameRows(
            down.Join(
                down,
                a => a.Depth,
                b => b.Depth,
                (a, b) => new { A = a.Id, B = b.Id }
            )
        );
        database.AssertSameRows(down.Concat(down));
    }

    [Fact]
    public void ReadsARecursionInTheStepOfAnother()
    {
        IQueryable<int> ids = Trees.WithRecursive(
            Employees.Where(e => e.Id == 1).Select(e => e.Id),
            level => level.Join(Employees, n => n, e => e.ManagerId, (n, e) => e.Id)
        );

        database.AssertSameRows(
            Trees.WithRecursive(
                Customers.Where(c => c.Id == 1).Select(c => 0),
                level => level.Join(ids, n => n + 1, id => id, (n, id) => id)
            )
        );
    }

    [Fact]
    public void ReadsTheRecursionInACorrelatedSubquery()
    {
        IQueryable<Walk> reports = Reports(3);

        database.AssertSameRows(
            Employees.Where(e => reports.Any(w => w.Id == e.Id && w.Depth > 0))
        );
        database.AssertSameRows(
            Employees.SelectMany(
                e => reports.Where(w => w.Path.EndsWith(e.Name)),
                (e, w) => new { e.Id, w.Depth }
            )
        );
        database.AssertSameRows(
            Employees.Select(e => new
            {
                e.Id,
                Deepest = reports.Where(w => w.Id == e.Id).Max(w => (int?)w.Depth),
            })
        );
    }

    [Fact]
    public void RecursesFromACorrelatedSeed()
    {
        // The in-memory reference evaluates only recursions over closed sources, so the
        // expected rows are computed here: each employee with the size of its subtree.
        IQueryable<Subtree> query = Employees.Select(e => new Subtree
        {
            Id = e.Id,
            Size = PostgreSqlRecursion
                .WithRecursive(
                    Employees.Where(x => x.Id == e.Id),
                    level =>
                        level.Join(Employees, m => m.Id, x => x.ManagerId, (m, x) => x)
                )
                .Count(),
        });
        List<string> expected =
        [
            .. Seed
                .Employees.Select(e => new Subtree
                {
                    Id = e.Id,
                    Size = SubtreeSize(e.Id),
                })
                .Select(Canonical.Describe)
                .Order(StringComparer.Ordinal),
        ];

        ResultSet result = database.Query(
            new PostgreSqlQuery(query.Expression).TextValue
        );

        Assert.Equal(
            expected,
            result
                .Rows.Select(row =>
                    Canonical.Describe(Materializer.Element(typeof(Subtree), result, row))
                )
                .Order(StringComparer.Ordinal)
        );
    }

    [Fact]
    public void RecursesWithACorrelatedStep()
    {
        // Each employee with the number of employees reachable from the root without
        // passing through it.
        IQueryable<Subtree> query = Employees.Select(e => new Subtree
        {
            Id = e.Id,
            Size = PostgreSqlRecursion
                .WithRecursive(
                    Employees.Where(x => x.ManagerId == null && x.Id != e.Id),
                    level =>
                        level.Join(
                            Employees.Where(x => x.Id != e.Id),
                            m => m.Id,
                            x => x.ManagerId,
                            (m, x) => x
                        )
                )
                .Count(),
        });
        List<string> expected =
        [
            .. Seed
                .Employees.Select(e => new Subtree
                {
                    Id = e.Id,
                    Size =
                        Seed.Employees.Count(x => x.ManagerId == null && x.Id != e.Id)
                        + Seed.Employees.Count(x =>
                            x.ManagerId is int manager
                            && x.Id != e.Id
                            && Reachable(manager, e.Id)
                        ),
                })
                .Select(Canonical.Describe)
                .Order(StringComparer.Ordinal),
        ];

        ResultSet result = database.Query(
            new PostgreSqlQuery(query.Expression).TextValue
        );

        Assert.Equal(
            expected,
            result
                .Rows.Select(row =>
                    Canonical.Describe(Materializer.Element(typeof(Subtree), result, row))
                )
                .Order(StringComparer.Ordinal)
        );
    }

    [Fact]
    public void SeedsFromGroupedDistinctPagedAndJoinedQueries()
    {
        database.AssertSameRows(
            Trees.WithRecursive(
                Employees
                    .GroupBy(e => e.ManagerId)
                    .Select(g => new Chain { Id = g.Count(), Manager = g.Key }),
                level =>
                    level
                        .Where(c => c.Id < 4)
                        .Select(c => new Chain { Id = c.Id + 1, Manager = c.Manager }),
                5
            )
        );
        database.AssertSameRows(
            Trees.WithRecursive(
                Employees.Select(e => e.ManagerId).Distinct(),
                level => level.Where(m => m < 3).Select(m => m + 1),
                5
            )
        );
        database.AssertSameRows(
            Trees.WithRecursive(
                Employees.OrderBy(e => e.Id).Skip(5).Select(e => e.Id),
                level => level.Where(n => n < 9).Select(n => n + 1)
            )
        );
        database.AssertSameRows(
            Trees.WithRecursive(
                Employees.Join(
                    Employees,
                    e => e.ManagerId,
                    m => m.Id,
                    (e, m) =>
                        new Walk
                        {
                            Id = e.Id,
                            Depth = 0,
                            Path = m.Name + "/" + e.Name,
                        }
                ),
                level =>
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
                    ),
                3
            )
        );
    }

    [Fact]
    public void SeedsFromAnInMemorySequenceStartingWithNull()
    {
        database.AssertSameRows(
            Trees.WithRecursive(
                new[] { (long?)null, 3L }.AsQueryable(),
                level => level.Where(n => n < 4).Select(n => n * 3_000_000_000L)
            )
        );
        database.AssertSameRows(
            Trees.WithRecursive(
                new[] { (1, null), (2, "b") }.AsQueryable(),
                level =>
                    level
                        .Where(t => t.Item1 < 4)
                        .Select(t => new ValueTuple<int, string?>(
                            t.Item1 + 1,
                            t.Item2 + "+"
                        ))
            )
        );
    }

    [Fact]
    public void CarriesNullsOfEveryTypeThroughTheLevels()
    {
        database.AssertSameRows(
            Trees.WithRecursive(
                Employees
                    .Where(e => e.ManagerId == null)
                    .Select(e => new Carried
                    {
                        Id = e.Id,
                        Text = null,
                        Amount = null,
                        Key = null,
                        Moment = null,
                        Span = null,
                        Status = null,
                        Flag = null,
                        Bytes = null,
                    }),
                level =>
                    level
                        .Where(c => c.Id < 10)
                        .Select(c => new Carried
                        {
                            Id = c.Id + 3,
                            Text = c.Text,
                            Amount = c.Amount,
                            Key = c.Key,
                            Moment = c.Moment,
                            Span = c.Span,
                            Status = c.Status,
                            Flag = c.Flag,
                            Bytes = c.Bytes,
                        })
            )
        );
    }

    [Fact]
    public void CarriesConstantsOfEveryTypeThroughTheLevels()
    {
        database.AssertSameRows(
            Trees.WithRecursive(
                Employees
                    .Where(e => e.ManagerId == null)
                    .Select(e => new Carried
                    {
                        Id = e.Id,
                        Text = "x",
                        Amount = 2.5m,
                        Key = Guid.Empty,
                        Moment = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
                        Span = TimeSpan.FromMinutes(5),
                        Status = OrderStatus.Paid,
                        Flag = false,
                        Bytes = new byte[] { 1, 2 },
                    }),
                level =>
                    level
                        .Where(c => c.Id < 10)
                        .Select(c => new Carried
                        {
                            Id = c.Id + 3,
                            Text = c.Text + "y",
                            Amount = c.Amount * 2,
                            Key = c.Key,
                            Moment = c.Moment,
                            Span = c.Span,
                            Status = c.Status,
                            Flag = c.Flag,
                            Bytes = c.Bytes,
                        })
            )
        );
    }

    [Fact]
    public void ReadsBytesFromTheTableInTheStep()
    {
        database.AssertSameRows(
            Trees.WithRecursive(
                Attachments
                    .Where(a => a.Id == 1)
                    .Select(a => new Blob { Id = a.Id, Checksum = null }),
                level =>
                    level
                        .Where(b => b.Id < 4)
                        .Join(
                            Attachments,
                            b => b.Id + 1,
                            a => a.Id,
                            (b, a) => new Blob { Id = a.Id, Checksum = a.Checksum }
                        )
            )
        );
    }

    [Fact]
    public void MakesAColumnNullableWhereALaterLevelYieldsNull()
    {
        database.AssertSameRows(
            Trees
                .WithRecursive(
                    Employees
                        .Where(e => e.Id <= 3)
                        .Select(e => new Pair
                        {
                            Id = e.Id,
                            A = e.Name,
                            B = e.Name,
                        }),
                    level =>
                        level
                            .Where(p => p.Id < 6 && p.A != "Root")
                            .Select(p => new Pair
                            {
                                Id = p.Id + 1,
                                A = p.B,
                                B = null,
                            })
                )
                .Where(p => p.A != "Lead A")
        );
    }

    [Fact]
    public void NumbersTheRowsOfEachLevelWithAWindow()
    {
        database.AssertSameRows(
            Trees.WithRecursive(
                MappedTables.Edges.Select(e => new Counted { Id = e.Id, Rows = 0 }),
                level =>
                    level
                        .Where(c => c.Id < 5)
                        .Over(
                            null,
                            rows => rows.OrderBy(c => c.Id),
                            null,
                            (c, window) =>
                                new Counted
                                {
                                    Id = c.Id + 1,
                                    Rows = (int)window.RowNumber(),
                                }
                        )
            )
        );
    }

    [Fact]
    public void AddsAnUncorrelatedScalarInTheStep()
    {
        database.AssertSameRows(
            Trees.WithRecursive(
                Employees.Select(e => e.Id),
                level =>
                    level
                        .Where(n => n < 9)
                        .Select(n =>
                            n + Customers.OrderBy(c => c.Id).Select(c => c.Id).First()
                        )
            )
        );
    }

    [Fact]
    public void ReadsTheRecursionAsTheInnerOfAGroupJoin()
    {
        IQueryable<Walk> reports = Reports(3);

        database.AssertSameRows(
            Employees.GroupJoin(
                reports,
                e => e.Id,
                w => w.Id,
                (e, ws) => new { e.Id, Count = ws.Count() }
            )
        );
        database.AssertSameRows(
            Employees
                .GroupJoin(reports, e => e.Id, w => w.Id, (e, ws) => new { e, ws })
                .SelectMany(
                    x => x.ws.DefaultIfEmpty(),
                    (x, w) => new { x.e.Id, Path = w == null ? null : w.Path }
                )
        );
        IQueryable<int> ids = reports.Select(w => w.Id);
        database.AssertSameRows(Employees.Where(e => !ids.Contains(e.Id)));
        database.AssertSameRows(ids.Union(Employees.Select(e => e.Id * 2)));
    }

    // Observed: 42P01: invalid reference to FROM-clause entry for table "t0":
    // INNER JOIN (WITH RECURSIVE t2 AS (... WHERE t1."Id" = t0."Id" ...)) AS t5
    // ON TRUE, without LATERAL.
    [Fact]
    public void FlattensARecursionFromACorrelatedSeed()
    {
        IQueryable<Below> query = Employees.SelectMany(
            e =>
                PostgreSqlRecursion.WithRecursive(
                    Employees.Where(x => x.Id == e.Id),
                    level =>
                        level.Join(Employees, m => m.Id, x => x.ManagerId, (m, x) => x)
                ),
            (e, x) => new Below { Id = e.Id, Member = x.Id }
        );
        List<string> expected =
        [
            .. Seed
                .Employees.SelectMany(
                    e => Members(e.Id),
                    (e, member) => new Below { Id = e.Id, Member = member }
                )
                .Select(Canonical.Describe)
                .Order(StringComparer.Ordinal),
        ];

        ResultSet result = database.Query(
            new PostgreSqlQuery(query.Expression).TextValue
        );

        Assert.Equal(
            expected,
            result
                .Rows.Select(row =>
                    Canonical.Describe(Materializer.Element(typeof(Below), result, row))
                )
                .Order(StringComparer.Ordinal)
        );
    }

    // Observed: 42P01: invalid reference to FROM-clause entry for table "t2":
    // FROM t1 AS t2 INNER JOIN (WITH RECURSIVE t4 AS (... = t2."Value" ...)) AS t6
    // ON TRUE, without LATERAL.
    [Fact]
    public void FlattensARecursionCorrelatedWithTheLevelInTheStep()
    {
        IQueryable<int> query = Trees.WithRecursive(
            Employees.Where(e => e.ManagerId == null).Select(e => e.Id),
            level =>
                level.SelectMany(
                    id =>
                        PostgreSqlRecursion.WithRecursive(
                            Employees.Where(x => x.ManagerId == id).Select(x => x.Id),
                            inner => inner.Where(x => x < 0)
                        ),
                    (id, below) => below
                )
        );
        // Every employee below a root, and the roots.
        List<string> expected =
        [
            .. Seed
                .Employees.Where(e => Members(1).Contains(e.Id) || e.ManagerId == null)
                .Select(e => Canonical.Describe(e.Id))
                .Order(StringComparer.Ordinal),
        ];

        ResultSet result = database.Query(
            new PostgreSqlQuery(query.Expression).TextValue
        );

        Assert.Equal(
            expected,
            result
                .Rows.Select(row =>
                    Canonical.Describe(Materializer.Element(typeof(int), result, row))
                )
                .Order(StringComparer.Ordinal)
        );
    }

    [Fact]
    public void AllowsTheLargestMaximumDepth()
    {
        database.AssertSameRows(Reports(long.MaxValue));
        database.AssertSameRows(Reports(int.MaxValue + 1L));
    }

    [Fact]
    public void ReachesTheMaximumDepthExactly()
    {
        database.AssertSameRows(Reports(2));
        AssertTooDeep(Reports(1), 1);
        database.AssertSameRows(
            Trees.WithRecursive(
                Employees.Where(e => e.ManagerId == null),
                level => level.Where(e => e.Id > 100),
                0
            )
        );
    }

    [Fact]
    public void FailsBeyondTheMaximumDepthInEveryContext()
    {
        IQueryable<Walk> cycle = Cycle(4);

        AssertTooDeep(Cycle(4).Where(w => w.Id == 4), 4);
        AssertTooDeep(Cycle(4).OrderBy(w => w.Depth).Take(1), 4);
        AssertTooDeep(Cycle(4).GroupBy(w => w.Id).Select(g => g.Key), 4);
        AssertTooDeep(Employees.Where(e => cycle.Any(w => w.Id == e.Id)), 4);
        AssertTooDeep(
            Employees.Select(e => new { e.Id, Visits = cycle.Count(w => w.Id == e.Id) }),
            4
        );
    }

    // Observed: the statement returns no row, as WHERE FALSE around the derived
    // table lets PostgreSQL skip the recursion; LINQ to Objects fails.
    [Fact(
        Skip = "#128: the depth limit does not fail when the query around it filters out every row"
    )]
    public void FailsBeyondTheMaximumDepthUnderAFalseFilter()
    {
        AssertTooDeep(Cycle(4).Where(w => false), 4);
    }

    // Observed: the statement returns no row, as the join with an empty side lets
    // PostgreSQL skip the recursion; LINQ to Objects reads the outer rows and fails.
    [Fact(Skip = "#128: the depth limit does not fail when joined with an empty side")]
    public void FailsBeyondTheMaximumDepthWhenJoinedWithNoRows()
    {
        AssertTooDeep(
            Cycle(4)
                .Join(
                    Employees.Where(e => e.Id > 1000),
                    w => w.Id,
                    e => e.Id,
                    (w, e) => e
                ),
            4
        );
    }

    [Fact]
    public void FailsBeyondTheMaximumDepthOfAnInnerRecursion()
    {
        IQueryable<Walk> inner = Cycle(2);

        AssertTooDeep(
            Trees.WithRecursive(
                inner.Select(w => w.Id),
                level => level.Where(n => n < 0),
                100
            ),
            2
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

    private static IEnumerable<int> Members(int id)
    {
        return Seed
            .Employees.Where(e => e.ManagerId == id)
            .SelectMany(e => Members(e.Id))
            .Prepend(id);
    }

    private static int SubtreeSize(int id)
    {
        return 1
            + Seed.Employees.Where(e => e.ManagerId == id).Sum(e => SubtreeSize(e.Id));
    }

    // Whether the employee is reachable from a root without passing through the
    // excluded one.
    private static bool Reachable(int id, int excluded)
    {
        Employee? employee = Seed.Employees.FirstOrDefault(e => e.Id == id);
        return employee is not null
            && employee.Id != excluded
            && (employee.ManagerId is not int manager || Reachable(manager, excluded));
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

        public long Big { get; init; }

        public decimal Sum { get; init; }

        public double Ratio { get; init; }

        public string? Note { get; init; }
    }

    public sealed record Severity
    {
        public long Id { get; init; }

        public short Level { get; init; }
    }

    public sealed record Score
    {
        public long Id { get; init; }

        public float Value { get; init; }
    }

    public sealed record Tagged
    {
        public int Id { get; init; }

        public int Depth { get; init; }

        public string[]? Tags { get; init; }
    }

    public sealed record Moments
    {
        public int Step { get; init; }

        public DateOnly Day { get; init; }

        public DateTime At { get; init; }

        public TimeOnly Time { get; init; }

        public TimeSpan Span { get; init; }

        public Guid Key { get; init; }
    }

    public sealed record Marks
    {
        public int Step { get; init; }

        public bool? Gift { get; init; }

        public OrderStatus? Status { get; init; }

        public char Letter { get; init; }

        public bool Flag { get; init; }
    }

    public sealed record Mapped
    {
        public long Id { get; init; }

        public string Kind { get; init; } = string.Empty;
    }

    public sealed record Node
    {
        public int Id { get; init; }

        public Info Info { get; init; } = new();
    }

    public sealed record Info
    {
        public int Depth { get; init; }

        public string Name { get; init; } = string.Empty;
    }

    public sealed record Chain
    {
        public int Id { get; init; }

        public int? Manager { get; init; }
    }

    public sealed record Counted
    {
        public int Id { get; init; }

        public int Rows { get; init; }
    }

    public sealed record Below
    {
        public int Id { get; init; }

        public int Member { get; init; }
    }

    public sealed record Carried
    {
        public int Id { get; init; }

        public string? Text { get; init; }

        public decimal? Amount { get; init; }

        public Guid? Key { get; init; }

        public DateTimeOffset? Moment { get; init; }

        public TimeSpan? Span { get; init; }

        public OrderStatus? Status { get; init; }

        public bool? Flag { get; init; }

        public byte[]? Bytes { get; init; }
    }

    public sealed record Blob
    {
        public int Id { get; init; }

        public byte[]? Checksum { get; init; }
    }

    public sealed record Pair
    {
        public int Id { get; init; }

        public string? A { get; init; }

        public string? B { get; init; }
    }

    public sealed record Subtree
    {
        public int Id { get; init; }

        public int Size { get; init; }
    }
}
