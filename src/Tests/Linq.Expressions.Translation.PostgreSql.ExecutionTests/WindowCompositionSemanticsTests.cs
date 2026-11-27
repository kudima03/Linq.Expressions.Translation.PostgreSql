#pragma warning disable CA1861 // the constant arrays are the sources under test

namespace Linq.Expressions.Translation.PostgreSql.ExecutionTests;

// PostgreSqlWindows.Over composed with the other operators gives every row what
// PostgreSQL's window functions give it over the rows of the source (MemoryWindow on
// the in-memory side): an operator applied to the result never changes the rows the
// window was computed over, and a window over a filtered, paged, distinct, grouped,
// joined, combined or in-memory source sees exactly those rows.
[Collection(SharedDatabase.Name)]
public sealed class WindowCompositionSemanticsTests(Database database)
{
    private static IQueryable<RankedOrder> Ranked =>
        Trees.Over(
            Orders,
            o => o.CustomerId,
            rows => rows.OrderByDescending(o => o.Total).ThenBy(o => o.Id),
            null,
            (o, w) =>
                new RankedOrder(o.Id, o.CustomerId, o.IsPaid, o.Total, w.RowNumber())
        );

    // ------------------------------------------------------------ over a source

    [Fact]
    public void FiltersAfterTheWindowWithoutChangingIt()
    {
        database.AssertSameRows(
            Trees
                .Over(
                    Orders,
                    null,
                    rows => rows.OrderBy(o => o.Id),
                    null,
                    (o, w) =>
                        new
                        {
                            o.Id,
                            o.IsPaid,
                            Number = w.RowNumber(),
                            Count = w.Rows.Count(),
                        }
                )
                .Where(x => x.IsPaid)
        );
    }

    [Fact]
    public void ComputesTheWindowOverAFilteredSourceThenFiltersAgain()
    {
        database.AssertSameRows(
            Trees
                .Over(
                    Orders.Where(o => o.Quantity > 1),
                    o => o.IsPaid,
                    rows => rows.OrderBy(o => o.Id),
                    null,
                    (o, w) =>
                        new
                        {
                            o.Id,
                            Number = w.RowNumber(),
                            Previous = w.Lag(x => (int?)x.Id, 1, null),
                        }
                )
                .Where(x => x.Number > 1)
        );
    }

    [Fact]
    public void KeepsTheOrderOfTheSource()
    {
        database.AssertSameSequence(
            Trees.Over(
                Orders.OrderBy(o => o.Total).ThenBy(o => o.Id),
                null,
                rows => rows.OrderByDescending(o => o.Id),
                null,
                (o, w) => new { o.Id, Number = w.RowNumber() }
            )
        );
    }

    [Fact]
    public void KeepsTheOrderOfTheSourceThroughAFilter()
    {
        database.AssertSameSequence(
            Trees
                .Over(
                    Orders.OrderBy(o => o.Total).ThenBy(o => o.Id),
                    null,
                    rows => rows.OrderBy(o => o.Id),
                    null,
                    (o, w) => new { o.Id, Number = w.RowNumber() }
                )
                .Where(x => x.Number % 2 == 0)
        );
    }

    [Fact]
    public void KeepsTheOrderOfAPagedSource()
    {
        database.AssertSameSequence(
            Trees.Over(
                Orders.OrderByDescending(o => o.Total).ThenBy(o => o.Id).Skip(2).Take(6),
                o => o.IsPaid,
                rows => rows.OrderBy(o => o.Id),
                null,
                (o, w) =>
                    new
                    {
                        o.Id,
                        Number = w.RowNumber(),
                        Count = w.Rows.Count(),
                    }
            )
        );
    }

    [Fact]
    public void ComputesTheWindowOverASourceSkippedPastItsEnd()
    {
        database.AssertSameResult(
            Trees.Over(
                Orders.OrderBy(o => o.Id).Skip(100),
                null,
                rows => rows.OrderBy(o => o.Id),
                null,
                (o, w) => w.RowNumber()
            ),
            q => q.Count()
        );
    }

    [Fact]
    public void ComputesTheWindowOverDistinctValues()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders.Select(o => o.Priority).Distinct(),
                null,
                rows => rows.Order(),
                null,
                (p, w) =>
                    new
                    {
                        Priority = p,
                        Number = w.RowNumber(),
                        Next = w.Lead(x => x, 1, null),
                    }
            )
        );
    }

    [Fact]
    public void ComputesTheWindowOverDistinctPairs()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders.Select(o => new { o.CustomerId, o.IsPaid }).Distinct(),
                x => x.CustomerId,
                rows => rows.OrderBy(x => x.IsPaid),
                null,
                (x, w) =>
                    new
                    {
                        x.CustomerId,
                        x.IsPaid,
                        Number = w.RowNumber(),
                        Count = w.Rows.Count(),
                    }
            )
        );
    }

    [Fact]
    public void ComputesTheWindowOverAProjection()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders.Select(o => new
                {
                    o.Id,
                    Net = o.Total - (o.Discount ?? 0),
                    Bucket = o.Quantity % 3,
                }),
                x => x.Bucket,
                rows => rows.OrderBy(x => x.Net).ThenBy(x => x.Id),
                null,
                (x, w) =>
                    new
                    {
                        x.Id,
                        Rank = w.Rank(),
                        First = w.FirstValue(r => r.Id),
                        Running = w.Rows.Sum(r => r.Net),
                    }
            )
        );
    }

    [Fact]
    public void ComputesTheWindowOverInMemoryValues()
    {
        database.AssertSameRows(
            Trees.Over(
                new[] { 5, 3, 3, 9, 1, 3 }.AsQueryable(),
                null,
                rows => rows.Order(),
                null,
                (v, w) =>
                    new
                    {
                        Value = v,
                        Rank = w.Rank(),
                        Dense = w.DenseRank(),
                        Percent = w.PercentRank(),
                        Cumulative = w.CumeDist(),
                        Next = w.Lead(x => x, 1, -1),
                        Count = w.Rows.Count(),
                    }
            )
        );
    }

    [Fact]
    public void ComputesTheWindowOverInMemoryTuples()
    {
        (int Id, string? Label)[] labels =
        [
            (1, "b"),
            (2, null),
            (3, "a"),
            (4, "b"),
            (5, null),
            (6, "it's"),
        ];

        database.AssertSameRows(
            Trees.Over(
                labels.AsQueryable(),
                r => r.Label,
                rows => rows.OrderByDescending(r => r.Id),
                null,
                (r, w) =>
                    new
                    {
                        r.Id,
                        Number = w.RowNumber(),
                        Previous = w.Lag(x => x.Label, 1, "none"),
                        Last = w.LastValue(x => x.Id),
                    }
            )
        );
    }

    [Fact]
    public void FiltersTheWindowOverInMemoryValues()
    {
        database.AssertSameRows(
            Trees
                .Over(
                    new[] { 10L, 20L, 30L, 40L }.AsQueryable(),
                    null,
                    rows => rows.OrderDescending(),
                    null,
                    (v, w) => new { Value = v, Number = w.RowNumber() }
                )
                .Where(x => x.Number <= 2)
        );
    }

    [Fact]
    public void ComputesTheWindowOverAConcatenation()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders.Select(o => o.CustomerId).Concat(Customers.Select(c => c.Id)),
                null,
                rows => rows.Order(),
                null,
                (id, w) =>
                    new
                    {
                        Id = id,
                        Rank = w.Rank(),
                        Dense = w.DenseRank(),
                        Count = w.Rows.Count(),
                    }
            )
        );
    }

    [Fact]
    public void ComputesTheWindowOverAUnionAndAnExcept()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders.Select(o => o.CustomerId).Union(Customers.Select(c => c.Id)),
                null,
                rows => rows.OrderDescending(),
                null,
                (id, w) => new { Id = id, Number = w.RowNumber() }
            )
        );
        database.AssertSameRows(
            Trees.Over(
                Customers.Select(c => c.Id).Except(Orders.Select(o => o.CustomerId)),
                null,
                rows => rows.Order(),
                null,
                (id, w) => new { Id = id, Number = w.RowNumber() }
            )
        );
    }

    [Fact]
    public void ComputesTheWindowOverAConcatenationOfInMemoryValues()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders.Select(o => o.Quantity).Concat([0, 100, 7]),
                null,
                rows => rows.Order(),
                null,
                (q, w) => new { Quantity = q, Rank = w.Rank() }
            )
        );
    }

    [Fact]
    public void ComputesTheWindowOverALeftJoin()
    {
        database.AssertSameRows(
            Trees.Over(
                Customers
                    .GroupJoin(
                        Orders,
                        c => c.Id,
                        o => o.CustomerId,
                        (c, os) => new { c, os }
                    )
                    .SelectMany(
                        x => x.os.DefaultIfEmpty(),
                        (x, o) =>
                            new
                            {
                                Customer = x.c.Id,
                                Order = o == null ? (int?)null : o.Id,
                                Total = o == null ? (decimal?)null : o.Total,
                            }
                    ),
                x => x.Order == null,
                rows => rows.OrderBy(x => x.Customer).ThenBy(x => x.Order),
                null,
                (x, w) =>
                    new
                    {
                        x.Customer,
                        x.Order,
                        Number = w.RowNumber(),
                        Running = w.Rows.Sum(r => r.Total),
                    }
            )
        );
    }

    [Fact]
    public void PartitionsByANullableKeyOfALeftJoin()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders
                    .GroupJoin(
                        Customers,
                        o => o.CustomerId,
                        c => c.Id,
                        (o, cs) => new { o, cs }
                    )
                    .SelectMany(
                        x => x.cs.DefaultIfEmpty(),
                        (x, c) => new { x.o.Id, Region = c == null ? null : c.Region }
                    ),
                x => x.Region,
                rows => rows.OrderBy(x => x.Id),
                null,
                (x, w) =>
                    new
                    {
                        x.Id,
                        x.Region,
                        Number = w.RowNumber(),
                        Count = w.Rows.Count(),
                    }
            )
        );
    }

    [Fact]
    public void ComputesTheWindowOverACrossJoin()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders
                    .Where(o => o.Id <= 3)
                    .SelectMany(
                        o => Customers.Where(c => c.Id <= 3),
                        (o, c) => new { Order = o.Id, Customer = c.Id }
                    ),
                x => x.Customer,
                rows => rows.OrderByDescending(x => x.Order),
                null,
                (x, w) =>
                    new
                    {
                        x.Order,
                        x.Customer,
                        Number = w.RowNumber(),
                    }
            )
        );
    }

    [Fact]
    public void ComputesTheWindowOverACorrelatedSelectMany()
    {
        database.AssertSameRows(
            Trees.Over(
                Customers.SelectMany(
                    c => Orders.Where(o => o.CustomerId == c.Id),
                    (c, o) =>
                        new
                        {
                            c.Region,
                            o.Id,
                            o.Total,
                        }
                ),
                x => x.Region,
                rows => rows.OrderBy(x => x.Total).ThenBy(x => x.Id),
                null,
                (x, w) =>
                    new
                    {
                        x.Id,
                        Rank = w.Rank(),
                        Share = x.Total / w.Rows.Sum(r => r.Total),
                    }
            )
        );
    }

    // ------------------------------------------------------------- over groups

    [Fact]
    public void RanksTheGroupsLeftByHaving()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders.GroupBy(o => o.CustomerId).Where(g => g.Count() > 1),
                null,
                rows =>
                    rows.OrderByDescending(g => g.Sum(o => o.Total)).ThenBy(g => g.Key),
                null,
                (g, w) =>
                    new
                    {
                        g.Key,
                        Rank = w.Rank(),
                        Groups = w.Rows.Count(),
                    }
            )
        );
    }

    [Fact]
    public void PartitionsGroupsByAnAggregate()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders.GroupBy(o => o.CustomerId),
                g => g.Count(),
                rows => rows.OrderBy(g => g.Key),
                null,
                (g, w) =>
                    new
                    {
                        g.Key,
                        Size = g.Count(),
                        Number = w.RowNumber(),
                        Previous = w.Lag(x => (int?)x.Key, 1, null),
                    }
            )
        );
    }

    [Fact]
    public void ReadsGroupAggregatesBesideTheWindow()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders.GroupBy(o => new { o.CustomerId, o.IsPaid }),
                g => g.Key.IsPaid,
                rows => rows.OrderBy(g => g.Key.CustomerId),
                null,
                (g, w) =>
                    new
                    {
                        g.Key.CustomerId,
                        g.Key.IsPaid,
                        Paid = g.Count(o => o.Quantity > 1),
                        Largest = g.Max(o => o.Total),
                        Numbers = string.Join(
                            ",",
                            g.OrderBy(o => o.Id).Select(o => o.Number)
                        ),
                        Previous = w.Lag(x => (decimal?)x.Sum(o => o.Total), 1, null),
                        Dense = w.DenseRank(),
                    }
            )
        );
    }

    [Fact]
    public void RanksGroupsOfAnElementSelector()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders.GroupBy(o => o.IsPaid, o => o.Quantity),
                null,
                rows => rows.OrderBy(g => g.Sum()),
                null,
                (g, w) =>
                    new
                    {
                        g.Key,
                        Sum = g.Sum(),
                        Rank = w.Rank(),
                        Total = w.Rows.Sum(x => x.Sum()),
                    }
            )
        );
    }

    [Fact]
    public void CountsTheGroupsOfTheSameSelectBesideTheWindow()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders.GroupBy(o => o.CustomerId),
                null,
                rows => rows.OrderBy(g => g.Key),
                null,
                (g, w) =>
                    new
                    {
                        g.Key,
                        Number = w.RowNumber(),
                        Groups = Orders.GroupBy(o => o.CustomerId).Count(),
                        Orders = Orders.GroupBy(o => o.CustomerId).Sum(x => x.Count()),
                    }
            )
        );
    }

    // ----------------------------------------------------------- after the window

    [Fact]
    public void PagesTheWindowInItsOwnOrder()
    {
        database.AssertSameSequence(
            Ranked.OrderBy(x => x.Rank).ThenBy(x => x.Id).Skip(3).Take(4)
        );
    }

    [Fact]
    public void TakesTheTopRowsOfEachPartition()
    {
        database.AssertSameSequence(
            Ranked.Where(x => x.Rank <= 2).OrderBy(x => x.CustomerId).ThenBy(x => x.Rank)
        );
    }

    [Fact]
    public void FiltersByAColumnBesideTheWindow()
    {
        database.AssertSameRows(Ranked.Where(x => x.IsPaid));
        database.AssertSameRows(Ranked.Where(x => x.Total > 1 && x.Rank == 1));
    }

    [Fact]
    public void FiltersTwiceAfterTheWindow()
    {
        database.AssertSameRows(
            Ranked
                .Where(x => x.IsPaid)
                .Select(x => new { x.Id, x.Rank })
                .Where(x => x.Rank > 1)
        );
    }

    [Fact]
    public void ProjectsThenFiltersTheWindow()
    {
        database.AssertSameRows(
            Ranked
                .Select(x => new
                {
                    x.Id,
                    Even = x.Rank % 2 == 0,
                    x.Total,
                })
                .Where(x => x.Even)
        );
    }

    [Fact]
    public void PagesThenFiltersTheWindow()
    {
        database.AssertSameSequence(
            Ranked.OrderBy(x => x.Id).Take(8).Where(x => x.Rank == 1)
        );
    }

    [Fact]
    public void MakesTheWindowDistinct()
    {
        database.AssertSameRows(Ranked.Select(x => x.Rank).Distinct());
        database.AssertSameResult(Ranked.Select(x => x.Rank).Distinct(), q => q.Count());
        database.AssertSameRows(
            Ranked.Select(x => new { x.IsPaid, x.Rank }).Distinct().Where(x => x.Rank > 1)
        );
    }

    [Fact]
    public void GroupsTheWindow()
    {
        database.AssertSameRows(
            Ranked
                .GroupBy(x => x.Rank)
                .Select(g => new
                {
                    g.Key,
                    Count = g.Count(),
                    Total = g.Sum(x => x.Total),
                    Paid = g.Count(x => x.IsPaid),
                })
        );
        database.AssertSameRows(
            Ranked
                .GroupBy(x => x.CustomerId)
                .Where(g => g.Max(x => x.Rank) > 1)
                .Select(g => g.Key)
        );
    }

    [Fact]
    public void AggregatesTheWindow()
    {
        database.AssertSameResult(Ranked, q => q.Sum(x => x.Rank));
        database.AssertSameResult(Ranked, q => q.Count(x => x.Rank == 1));
        database.AssertSameResult(Ranked, q => q.LongCount(x => x.Rank > 1));
        database.AssertSameResult(Ranked, q => q.Average(x => x.Rank));
        database.AssertSameResult(Ranked, q => q.Min(x => x.Total / x.Rank));
        database.AssertSameResult(Ranked, q => q.Any(x => x.Rank > 3));
        database.AssertSameResult(Ranked, q => q.Any(x => x.Rank > 30));
        database.AssertSameResult(Ranked, q => q.All(x => x.Rank >= 1));
        database.AssertSameResult(Ranked, q => q.All(x => x.Rank == 1));
        database.AssertSameResult(Ranked.Select(x => x.Rank), q => q.Contains(3L));
    }

    [Fact]
    public void ReadsOneElementOfTheWindow()
    {
        database.AssertSameResult(
            Ranked,
            q => q.OrderByDescending(x => x.Rank).ThenBy(x => x.Id).First()
        );
        database.AssertSameResult(
            Ranked,
            q => q.OrderBy(x => x.Id).Last(x => x.Rank == 1)
        );
        database.AssertSameResult(Ranked, q => q.OrderBy(x => x.Id).ElementAt(4));
        database.AssertSameResult(Ranked, q => q.Single(x => x.Id == 9));
        database.AssertSameResult(Ranked, q => q.FirstOrDefault(x => x.Rank > 30));
        database.AssertSameResult(Ranked, q => q.MaxBy(x => x.Total));
    }

    [Fact]
    public void ReversesAndTrimsTheWindowInTheOrderOfTheSource()
    {
        IQueryable<RankedOrder> ordered = Trees.Over(
            Orders.OrderBy(o => o.Id),
            o => o.IsPaid,
            rows => rows.OrderBy(o => o.Total).ThenBy(o => o.Id),
            null,
            (o, w) =>
                new RankedOrder(o.Id, o.CustomerId, o.IsPaid, o.Total, w.RowNumber())
        );

        database.AssertSameSequence(ordered.Reverse());
        database.AssertSameSequence(ordered.TakeLast(3));
        database.AssertSameSequence(ordered.SkipLast(9));
        database.AssertSameSequence(ordered.Skip(10));
    }

    [Fact]
    public void JoinsTheWindowAsTheInnerSequence()
    {
        database.AssertSameRows(
            Customers.Join(
                Ranked,
                c => c.Id,
                x => x.CustomerId,
                (c, x) =>
                    new
                    {
                        c.Name,
                        x.Id,
                        x.Rank,
                    }
            )
        );
    }

    [Fact]
    public void JoinsTheWindowFilteredOnTheOuterSide()
    {
        database.AssertSameRows(
            Customers
                .Where(c => c.Id <= 2)
                .Join(
                    Ranked,
                    c => c.Id,
                    x => x.CustomerId,
                    (c, x) => new { c.Id, x.Rank }
                )
        );
    }

    [Fact]
    public void LeftJoinsTheWindow()
    {
        database.AssertSameRows(
            Customers
                .GroupJoin(Ranked, c => c.Id, x => x.CustomerId, (c, xs) => new { c, xs })
                .SelectMany(
                    x => x.xs.DefaultIfEmpty(),
                    (x, r) => new { x.c.Id, Rank = r == null ? (long?)null : r.Rank }
                )
        );
    }

    [Fact]
    public void CountsTheWindowPerOuterRow()
    {
        database.AssertSameRows(
            Customers.GroupJoin(
                Ranked,
                c => c.Id,
                x => x.CustomerId,
                (c, xs) => new { c.Id, Top = xs.Count(x => x.Rank == 1) }
            )
        );
    }

    // The window is computed over the cross join, every order once per customer:
    // SELECT t0."Id" AS "Customer", t1."Id", ROW_NUMBER() OVER (PARTITION BY
    // t1."CustomerId" ORDER BY t1."Total" DESC, t1."Id") AS "Rank" FROM "Customer" AS t0
    // CROSS JOIN "Order" AS t1 WHERE t0."Id" <= 2
    [Fact(
        Skip = "#128: a windowed inner sequence of SelectMany is cross joined without a derived table, so its window counts the joined rows"
    )]
    public void CrossJoinsTheWindow()
    {
        database.AssertSameRows(
            Customers
                .Where(c => c.Id <= 2)
                .SelectMany(
                    c => Ranked,
                    (c, x) =>
                        new
                        {
                            Customer = c.Id,
                            x.Id,
                            x.Rank,
                        }
                )
        );
    }

    [Fact]
    public void ReadsTheWindowInACorrelatedSelectMany()
    {
        database.AssertSameRows(
            Customers.SelectMany(
                c => Ranked.Where(x => x.CustomerId == c.Id && x.Rank <= 1),
                (c, x) => new { c.Name, x.Id }
            )
        );
    }

    [Fact]
    public void CombinesTheWindowWithSetOperations()
    {
        database.AssertSameRows(Ranked.Select(x => x.Rank).Union([1L, 100L]));
        database.AssertSameRows(Ranked.Select(x => x.Rank).Intersect([2L, 100L]));
        database.AssertSameRows(Ranked.Select(x => x.Rank).Except([1L]));
        database.AssertSameRows(
            Orders.Select(o => (long)o.Quantity).Concat(Ranked.Select(x => x.Rank))
        );
    }

    [Fact]
    public void ComputesASecondWindowOverAProjectedAndFilteredWindow()
    {
        database.AssertSameRows(
            Trees.Over(
                Ranked
                    .Where(x => x.Rank <= 2)
                    .Select(x => new
                    {
                        x.Id,
                        x.Total,
                        x.Rank,
                    }),
                x => x.Rank,
                rows => rows.OrderBy(x => x.Total).ThenBy(x => x.Id),
                null,
                (x, w) =>
                    new
                    {
                        x.Id,
                        x.Rank,
                        Second = w.RowNumber(),
                        Previous = w.Lag(r => (long?)r.Rank, 1, null),
                    }
            )
        );
    }

    [Fact]
    public void ComputesAWindowOverTheGroupsOfAWindow()
    {
        database.AssertSameRows(
            Trees.Over(
                Ranked.GroupBy(x => x.Rank),
                null,
                rows => rows.OrderByDescending(g => g.Key),
                null,
                (g, w) =>
                    new
                    {
                        g.Key,
                        Count = g.Count(),
                        Running = w.Rows.Sum(x => x.Count()),
                    }
            )
        );
    }

    [Fact]
    public void AggregatesTheWindowBesideItInALaterProjection()
    {
        IQueryable<RankedOrder> ranked = Ranked;

        // A window aggregate over the rows of the window, whose function it cannot nest.
        database.AssertSameRows(
            ranked.Select(x => new
            {
                x.Id,
                x.Rank,
                Count = ranked.Count(),
            })
        );
    }

    // 42P20: window function calls cannot be nested: MAX(ROW_NUMBER() OVER (...)) OVER ()
    [Fact(
        Skip = "#128: an aggregate over a windowed query in a later projection of it nests the window function in a window aggregate"
    )]
    public void AggregatesAWindowFunctionBesideItInALaterProjection()
    {
        IQueryable<RankedOrder> ranked = Ranked;

        database.AssertSameRows(
            ranked.Select(x => new
            {
                x.Id,
                x.Rank,
                Deepest = ranked.Max(y => y.Rank),
            })
        );
    }

    // 42P20: window functions are not allowed in FILTER:
    // COUNT(*) FILTER (WHERE ROW_NUMBER() OVER (...) = 1) OVER ()
    [Fact(
        Skip = "#128: a filtered aggregate over a windowed query in a later projection of it puts the window function in FILTER"
    )]
    public void CountsByAWindowFunctionBesideItInALaterProjection()
    {
        IQueryable<RankedOrder> ranked = Ranked;

        database.AssertSameRows(
            ranked.Select(x => new { x.Id, Firsts = ranked.Count(y => y.Rank == 1) })
        );
    }

    [Fact]
    public void AggregatesAFilteredWindowBesideItInALaterProjection()
    {
        IQueryable<RankedOrder> firsts = Ranked.Where(x => x.Rank == 1);

        database.AssertSameRows(
            firsts.Select(x => new
            {
                x.Id,
                Count = firsts.Count(),
                Largest = firsts.Max(y => y.Total),
                Paid = firsts.Count(y => y.IsPaid),
            })
        );
    }

    [Fact]
    public void AggregatesAWindowOverAWindowInItsSelector()
    {
        IQueryable<RankedOrder> ranked = Ranked;

        database.AssertSameRows(
            Trees.Over(
                ranked,
                x => x.Rank,
                rows => rows.OrderBy(x => x.Id),
                null,
                (x, w) =>
                    new
                    {
                        x.Id,
                        Number = w.RowNumber(),
                        Deepest = ranked.Max(y => y.Rank),
                        Firsts = ranked.Count(y => y.Rank == 1),
                    }
            )
        );
    }

    // ------------------------------------------------------------ in a subquery

    [Fact]
    public void ReadsAWindowOfACorrelatedSubqueryInAProjection()
    {
        database.AssertSameRows(
            Customers.Select(c => new
            {
                c.Id,
                Ranks = Orders
                    .Where(o => o.CustomerId == c.Id)
                    .Over(
                        null,
                        rows => rows.OrderBy(o => o.Total).ThenBy(o => o.Id),
                        null,
                        (o, w) => w.RowNumber()
                    )
                    .Sum(),
                Firsts = Orders
                    .Where(o => o.CustomerId == c.Id)
                    .Over(
                        o => o.IsPaid,
                        rows => rows.OrderBy(o => o.Id),
                        null,
                        (o, w) => new { o.Id, Number = w.RowNumber() }
                    )
                    .Count(x => x.Number == 1),
            })
        );
    }

    [Fact]
    public void FiltersByAWindowOfACorrelatedSubquery()
    {
        database.AssertSameRows(
            Customers
                .Where(c =>
                    Orders
                        .Where(o => o.CustomerId == c.Id)
                        .Over(
                            null,
                            rows =>
                                rows.OrderByDescending(o => o.Total).ThenBy(o => o.Id),
                            null,
                            (o, w) => new { o.IsPaid, Number = w.RowNumber() }
                        )
                        .Any(x => x.Number == 1 && x.IsPaid)
                )
                .Select(c => c.Id)
        );
    }

    [Fact]
    public void FiltersByMembershipInAWindow()
    {
        IQueryable<RankedOrder> ranked = Ranked;

        database.AssertSameRows(
            Orders
                .Where(o =>
                    ranked.Where(x => x.Rank == 1).Select(x => x.Id).Contains(o.Id)
                )
                .Select(o => o.Id)
        );
    }

    // The correlated window becomes an INNER JOIN, its window over every joined row:
    // SELECT t0."Name", t1."Id", ROW_NUMBER() OVER (ORDER BY t1."Id") AS "Number", ...
    // FROM "Customer" AS t0 INNER JOIN "Order" AS t1 ON t1."CustomerId" = t0."Id"
    [Fact(
        Skip = "#128: a correlated Over in SelectMany is inner joined, so its window is over all joined rows instead of the rows of each outer element"
    )]
    public void SelectsManyFromACorrelatedWindow()
    {
        database.AssertSameRows(
            Customers.SelectMany(
                c =>
                    Orders
                        .Where(o => o.CustomerId == c.Id)
                        .Over(
                            null,
                            rows => rows.OrderBy(o => o.Id),
                            null,
                            (o, w) =>
                                new
                                {
                                    o.Id,
                                    Number = w.RowNumber(),
                                    Count = w.Rows.Count(),
                                }
                        ),
                (c, x) =>
                    new
                    {
                        c.Name,
                        x.Id,
                        x.Number,
                        x.Count,
                    }
            )
        );
    }

    [Fact]
    public void LeftJoinsACorrelatedWindow()
    {
        database.AssertSameRows(
            Customers.SelectMany(
                c =>
                    Orders
                        .Where(o => o.CustomerId == c.Id)
                        .Over(
                            null,
                            rows => rows.OrderBy(o => o.Id),
                            null,
                            (o, w) => (long?)w.RowNumber()
                        )
                        .DefaultIfEmpty(),
                (c, n) => new { c.Id, Number = n }
            )
        );
    }

    [Fact]
    public void ComputesAWindowInTheSelectorOfAWindow()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders,
                o => o.CustomerId,
                rows => rows.OrderBy(o => o.Id),
                null,
                (o, w) =>
                    new
                    {
                        o.Id,
                        Number = w.RowNumber(),
                        Inner = Orders
                            .Where(x => x.CustomerId == o.CustomerId && x.Id <= o.Id)
                            .Over(
                                null,
                                rows => rows.OrderBy(x => x.Id),
                                null,
                                (x, v) => (long?)v.RowNumber()
                            )
                            .Max(),
                    }
            )
        );
    }

    [Fact]
    public void ReadsACorrelatedAggregateBesideTheWindow()
    {
        database.AssertSameRows(
            Trees.Over(
                Customers,
                c => c.Region,
                rows => rows.OrderBy(c => c.Id),
                null,
                (c, w) =>
                    new
                    {
                        c.Id,
                        Orders = Orders.Count(o => o.CustomerId == c.Id),
                        Number = w.RowNumber(),
                        Paid = Orders.Where(o => o.IsPaid).Count(),
                    }
            )
        );
    }

    // ------------------------------------------------------------- the selector

    [Fact]
    public void ProjectsTheRowAndTheWindowIntoNestedObjects()
    {
        database.AssertSameRows(
            Trees
                .Over(
                    Orders,
                    o => o.IsPaid,
                    rows => rows.OrderBy(o => o.Id),
                    null,
                    (o, w) =>
                        new
                        {
                            Order = o,
                            Window = new { Number = w.RowNumber(), Rank = w.Rank() },
                        }
                )
                .Where(x => x.Window.Number == 2)
                .Select(x => new
                {
                    x.Order.Id,
                    x.Order.Number,
                    x.Window.Rank,
                })
        );
    }

    [Fact]
    public void ProjectsTheWindowIntoTuples()
    {
        database.AssertSameRows(
            Trees
                .Over(
                    Orders,
                    null,
                    rows => rows.OrderBy(o => o.Id),
                    null,
                    (o, w) =>
                        new ValueTuple<int, long, string?>(
                            o.Id,
                            w.RowNumber(),
                            w.Lag(x => x.Note, 1, null)
                        )
                )
                .Where(t => t.Item2 > 3 && t.Item3 != null)
        );
        database.AssertSameRows(
            Trees.Over(
                Orders,
                null,
                rows => rows.OrderBy(o => o.Id),
                null,
                (o, w) => new Tuple<int, long>(o.Id, w.DenseRank())
            )
        );
    }

    [Fact]
    public void SelectsTheRowItselfAndFiltersIt()
    {
        database.AssertSameRows(
            Trees.Over(Orders, null, null, null, (o, w) => o).Where(o => o.IsPaid)
        );
    }

    [Fact]
    public void ComparesValuesOfOtherRowsAsNullable()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders,
                null,
                rows => rows.OrderBy(o => o.Id),
                null,
                (o, w) =>
                    new
                    {
                        o.Id,
                        Higher = o.Priority > w.Lag(x => x.Priority, 1, null),
                        Same = o.Priority == w.Lag(x => x.Priority, 1, null),
                        Different = w.Lead(x => x.Priority, 1, null) != o.Priority,
                        SameNote = w.Lag(x => x.Note, 1, null) == o.Note,
                        Missing = w.Lag(x => x.Discount, 2, null) == null,
                        Fallback = w.Lag(x => x.Priority, 1, null) ?? -1,
                    }
            )
        );
    }

    [Fact]
    public void FiltersByComparisonsOfValuesOfOtherRows()
    {
        database.AssertSameRows(
            Trees
                .Over(
                    Orders,
                    null,
                    rows => rows.OrderBy(o => o.Id),
                    null,
                    (o, w) =>
                        new
                        {
                            o.Id,
                            o.Priority,
                            Previous = w.Lag(x => x.Priority, 1, null),
                        }
                )
                .Where(x => x.Priority != x.Previous)
        );
    }

    [Fact]
    public void ComputesConditionalsOfWindowFunctions()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders,
                o => o.CustomerId,
                rows => rows.OrderBy(o => o.Total).ThenBy(o => o.Id),
                null,
                (o, w) =>
                    new
                    {
                        o.Id,
                        Label = w.RowNumber() == 1 ? "first"
                        : w.Rows.Count() == w.RowNumber() ? "last"
                        : "middle",
                        Kept = w.Rank() > 1 ? o.Total : 0m,
                        Half = w.CumeDist() > 0.5,
                        Top = w.PercentRank() <= 0.5 && w.Rows.Count() > 1,
                    }
            )
        );
    }

    [Fact]
    public void ComputesArithmeticOfWindowFunctions()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders,
                o => o.IsPaid,
                rows => rows.OrderBy(o => o.Priority).ThenBy(o => o.Id),
                null,
                (o, w) =>
                    new
                    {
                        o.Id,
                        Gaps = w.Rank() - w.DenseRank(),
                        Halved = w.RowNumber() / 2,
                        Mod = w.RowNumber() % 3,
                        Negated = -w.RowNumber() / 4,
                        Ratio = (double)w.RowNumber() / w.Rows.Count(),
                        Mixed = (w.Ntile(3) * 10) + w.RowNumber(),
                        Delta = o.Quantity - w.Lag(x => x.Quantity, 1, 0),
                        Scaled = o.Total * w.RowNumber(),
                    }
            )
        );
    }

    [Fact]
    public void ReadsCapturedVariablesInTheSelector()
    {
        int buckets = 4;
        int offset = 2;
        decimal factor = 1.5m;
        string fallback = "it's \"none\"";
        long start = 100;

        database.AssertSameRows(
            Trees.Over(
                Orders,
                null,
                rows => rows.OrderBy(o => o.Id),
                null,
                (o, w) =>
                    new
                    {
                        o.Id,
                        Bucket = w.Ntile(buckets),
                        Back = w.Lag(x => x.Id, offset, -offset),
                        Scaled = o.Total * factor,
                        Note = w.Lead(x => x.Note, offset, fallback),
                        Numbered = start + w.RowNumber(),
                    }
            )
        );
    }

    [Fact]
    public void ReadsOffsetsAndDefaultsComputedForEachRow()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders,
                null,
                rows => rows.OrderBy(o => o.Id),
                null,
                (o, w) =>
                    new
                    {
                        o.Id,
                        Back = w.Lag(x => x.Id, o.Quantity % 3, -o.Id),
                        Ahead = w.Lead(x => x.Discount, o.Id % 2, o.Discount),
                        Default = w.Lag(x => x.Priority, 1, o.Priority ?? 0),
                    }
            )
        );
    }

    [Fact]
    public void ReadsValuesOfEveryTypeInOtherRows()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders,
                null,
                rows => rows.OrderBy(o => o.Id),
                null,
                (o, w) =>
                    new
                    {
                        o.Id,
                        Created = w.Lag(x => x.CreatedAt, 1, DateTime.MinValue),
                        Paid = w.Lag(x => x.PaidAt, 1, null),
                        Shipped = w.Lead(x => x.ShippedOn, 1, new DateOnly(2001, 2, 3)),
                        Time = w.Lead(x => x.DeliveryTime, 1, TimeOnly.MinValue),
                        Gift = w.Lag(x => x.IsGift, 1, null),
                        PaidFlag = w.Lag(x => x.IsPaid, 1, false),
                        Status = w.Lag(x => x.Status, 1, OrderStatus.Cancelled),
                        Tracking = w.Lead(x => x.TrackingId, 1, Guid.Empty),
                        Points = w.Lag(x => x.Points, 1, long.MaxValue),
                        Weight = w.Lag(x => (double?)x.Weight, 1, null),
                        Long = w.Lead(x => x.Points + x.Quantity, 1, -1L),
                    }
            )
        );
    }

    [Fact]
    public void ReadsFirstAndLastValuesOfExpressions()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders,
                o => o.CustomerId,
                rows => rows.OrderBy(o => o.Id),
                null,
                (o, w) =>
                    new
                    {
                        o.Id,
                        FirstNumber = w.FirstValue(x => x.Number.ToUpper()),
                        LastNote = w.LastValue(x => x.Note ?? "-"),
                        FirstNet = w.FirstValue(x => x.Total - (x.Discount ?? 0m)),
                        LastPriority = w.LastValue(x => x.Priority),
                    }
            )
        );
    }

    [Fact]
    public void NumbersBucketsComputedForEachPartition()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders,
                o => o.CustomerId,
                rows => rows.OrderBy(o => o.Id),
                null,
                (o, w) =>
                    new
                    {
                        o.Id,
                        Bucket = w.Ntile(o.CustomerId),
                        One = w.Ntile(1),
                        Many = w.Ntile(int.MaxValue),
                    }
            )
        );
    }

    // ------------------------------------------------------------ the window

    [Fact]
    public void PartitionsAndOrdersByStrings()
    {
        database.AssertSameRows(
            Trees.Over(
                Customers,
                c => c.Region,
                rows =>
                    rows.OrderByDescending(c => c.Email, StringComparer.Ordinal)
                        .ThenBy(c => c.Id),
                null,
                (c, w) =>
                    new
                    {
                        c.Id,
                        Number = w.RowNumber(),
                        Rank = w.Rank(),
                        Previous = w.Lag(x => x.Email, 1, "?"),
                    }
            )
        );
        database.AssertSameRows(
            Trees.Over(
                Orders,
                o => o.Note,
                rows =>
                    rows.OrderBy(o => o.Note, StringComparer.Ordinal).ThenBy(o => o.Id),
                null,
                (o, w) =>
                    new
                    {
                        o.Id,
                        Count = w.Rows.Count(),
                        Number = w.RowNumber(),
                    }
            )
        );
    }

    [Fact]
    public void PartitionsByNullableAndComputedKeys()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders,
                o => new
                {
                    o.Priority,
                    Large = o.Total > 10,
                    o.CreatedAt.Year,
                },
                rows => rows.OrderBy(o => o.Id),
                null,
                (o, w) =>
                    new
                    {
                        o.Id,
                        Number = w.RowNumber(),
                        Count = w.Rows.Count(),
                    }
            )
        );
        database.AssertSameRows(
            Trees.Over(
                Orders,
                o => (o.Note ?? string.Empty).Length > 3,
                rows => rows.OrderByDescending(o => o.Discount).ThenBy(o => o.Id),
                null,
                (o, w) =>
                    new
                    {
                        o.Id,
                        Rank = w.Rank(),
                        Dense = w.DenseRank(),
                    }
            )
        );
    }

    [Fact]
    public void OrdersByExpressionsAndNullableTuples()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders,
                null,
                rows =>
                    rows.OrderBy(o => o.Discount ?? 0)
                        .ThenByDescending(o => o.Quantity % 3)
                        .ThenBy(o => -o.Id),
                null,
                (o, w) => new { o.Id, Number = w.RowNumber() }
            )
        );
        database.AssertSameRows(
            Trees.Over(
                Orders,
                null,
                rows =>
                    rows.OrderByDescending(o => new ValueTuple<int?, bool?>(
                            o.Priority,
                            o.IsGift
                        ))
                        .ThenBy(o => o.Id),
                null,
                (o, w) =>
                    new
                    {
                        o.Id,
                        Number = w.RowNumber(),
                        Rank = w.Rank(),
                    }
            )
        );
    }

    [Fact]
    public void OrdersByDatesAndInstants()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders,
                null,
                rows =>
                    rows.OrderBy(o => o.PaidAt)
                        .ThenByDescending(o => o.CreatedAt)
                        .ThenBy(o => o.Id),
                null,
                (o, w) =>
                    new
                    {
                        o.Id,
                        Number = w.RowNumber(),
                        Rank = w.Rank(),
                        Previous = w.Lag(x => x.ShippedOn, 1, null),
                    }
            )
        );
    }

    // PostgreSQL sorts NaN after every number, .NET before: order 10 (NaN) is numbered
    // 12 instead of 1.
    [Fact(
        Skip = "#132: a window ordered by a double sorts NaN last, while Over documents the order of .NET, where NaN is the smallest"
    )]
    public void OrdersByDoublesAsDotNetDoes()
    {
        // The window is sorted as in .NET, where NaN is the smallest double.
        database.AssertSameRows(
            Trees.Over(
                Orders,
                null,
                rows => rows.OrderBy(o => o.Weight).ThenBy(o => o.Id),
                null,
                (o, w) => new { o.Id, Number = w.RowNumber() }
            )
        );
    }

    [Fact]
    public void RanksEveryRowAsAPeerWithoutAnOrder()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders,
                o => o.IsPaid,
                null,
                null,
                (o, w) =>
                    new
                    {
                        o.Id,
                        Rank = w.Rank(),
                        Dense = w.DenseRank(),
                        Percent = w.PercentRank(),
                        Cumulative = w.CumeDist(),
                        Count = w.Rows.Count(),
                    }
            )
        );
    }

    [Fact]
    public void RanksASinglePartitionOfOneRow()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders,
                o => o.Id,
                rows => rows.OrderBy(o => o.Total),
                null,
                (o, w) =>
                    new
                    {
                        o.Id,
                        Number = w.RowNumber(),
                        Percent = w.PercentRank(),
                        Cumulative = w.CumeDist(),
                        Bucket = w.Ntile(3),
                        Previous = w.Lag(x => x.Id, 1, -1),
                        Next = w.Lead(x => x.Id, 1, -1),
                        First = w.FirstValue(x => x.Id),
                        Last = w.LastValue(x => x.Id),
                    }
            )
        );
    }

    // ------------------------------------------------- a window read by a join

    private static IQueryable<NumberedOrder> Numbered =>
        Trees.Over(
            Orders,
            null,
            rows => rows.OrderBy(o => o.Id),
            null,
            (o, w) => new NumberedOrder(o.Id, o.CustomerId, w.RowNumber(), w.Rows.Count())
        );

    // The join is computed before the window: SELECT t0."Name", t1."Id",
    // ROW_NUMBER() OVER (ORDER BY t1."Id") ... FROM "Customer" AS t0 INNER JOIN
    // "Order" AS t1 ... numbers the joined orders only.
    [Fact(
        Skip = "#128: a windowed inner sequence of Join is joined without a derived table, so its window is computed over the joined rows only"
    )]
    public void JoinsAWindowOverEveryRow()
    {
        database.AssertSameRows(
            Customers
                .Where(c => c.Id == 2)
                .Join(
                    Numbered,
                    c => c.Id,
                    x => x.CustomerId,
                    (c, x) =>
                        new
                        {
                            c.Name,
                            x.Id,
                            x.Number,
                            x.Count,
                        }
                )
        );
    }

    [Fact]
    public void LeftJoinsAWindowOverEveryRow()
    {
        database.AssertSameRows(
            Customers
                .Where(c => c.Id <= 3)
                .GroupJoin(
                    Numbered,
                    c => c.Id,
                    x => x.CustomerId,
                    (c, xs) => new { c, xs }
                )
                .SelectMany(
                    x => x.xs.DefaultIfEmpty(),
                    (x, n) => new { x.c.Id, Number = n == null ? (long?)null : n.Number }
                )
        );
    }

    // The correlation filters the rows before the window numbers them.
    [Fact(
        Skip = "#128: a windowed inner sequence of GroupJoin is filtered by the correlation before its window is computed"
    )]
    public void AggregatesTheMatchesOfAWindowOverEveryRow()
    {
        database.AssertSameRows(
            Customers.GroupJoin(
                Numbered,
                c => c.Id,
                x => x.CustomerId,
                (c, xs) =>
                    new
                    {
                        c.Id,
                        Last = xs.Max(x => (long?)x.Number),
                        Firsts = xs.Count(x => x.Number == 1),
                    }
            )
        );
    }

    [Fact]
    public void ReadsAWindowOverEveryRowInACorrelatedFilter()
    {
        IQueryable<NumberedOrder> numbered = Numbered;

        database.AssertSameRows(
            Customers.Select(c => new
            {
                c.Id,
                Last = numbered
                    .Where(x => x.CustomerId == c.Id)
                    .Max(x => (long?)x.Number),
            })
        );
    }

    // The window is computed before DISTINCT ON keeps one order per customer.
    [Fact(
        Skip = "#128: Over over DistinctBy extends its DISTINCT ON SELECT, so the window is computed over the rows before DistinctBy"
    )]
    public void ComputesTheWindowOverTheRowsOfDistinctBy()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders
                    .OrderBy(o => o.CustomerId)
                    .ThenBy(o => o.Id)
                    .DistinctBy(o => o.CustomerId),
                null,
                rows => rows.OrderBy(o => o.CustomerId),
                null,
                (o, w) =>
                    new
                    {
                        o.Id,
                        Number = w.RowNumber(),
                        Count = w.Rows.Count(),
                    }
            )
        );
    }

    // SELECT DISTINCT ON (t0."CustomerId") t0."CustomerId", ROW_NUMBER() OVER (ORDER BY
    // t0."CustomerId" DESC) AS "Number", ... FROM "Order" AS t0
    [Fact(
        Skip = "#128: Over over DistinctBy extends its DISTINCT ON SELECT, so the window is computed over the rows before DistinctBy"
    )]
    public void CountsTheRowsOfAnUnorderedDistinctBy()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders.DistinctBy(o => o.CustomerId),
                null,
                rows => rows.OrderByDescending(o => o.CustomerId),
                null,
                (o, w) =>
                    new
                    {
                        o.CustomerId,
                        Number = w.RowNumber(),
                        Count = w.Rows.Count(),
                    }
            )
        );
    }

    [Fact]
    public void ComputesTheWindowOverPagedGroups()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders.GroupBy(o => o.CustomerId).OrderBy(g => g.Key).Skip(1).Take(4),
                null,
                rows => rows.OrderByDescending(g => g.Key),
                null,
                (g, w) =>
                    new
                    {
                        g.Key,
                        Number = w.RowNumber(),
                        Groups = w.Rows.Count(),
                    }
            )
        );
    }

    [Fact]
    public void FiltersAndPagesAWindowOverGroups()
    {
        IQueryable<GroupRank> ranked = Trees.Over(
            Orders.GroupBy(o => o.CustomerId),
            g => g.Count() > 1,
            rows => rows.OrderByDescending(g => g.Sum(o => o.Total)).ThenBy(g => g.Key),
            null,
            (g, w) => new GroupRank(g.Key, g.Count(), w.RowNumber())
        );

        database.AssertSameRows(ranked.Where(x => x.Rank == 1));
        database.AssertSameSequence(
            ranked.OrderBy(x => x.Rank).ThenBy(x => x.Key).Take(3)
        );
        database.AssertSameResult(ranked, q => q.Count(x => x.Rank > 1));
        database.AssertSameResult(ranked, q => q.Sum(x => x.Size * x.Rank));
    }

    [Fact]
    public void JoinsTheStringsOfAWindowInTheOrderOfItsSource()
    {
        database.AssertSameRows(
            Customers.Select(c => new
            {
                c.Id,
                Numbers = string.Join(
                    ",",
                    Orders
                        .Where(o => o.CustomerId == c.Id)
                        .OrderBy(o => o.Id)
                        .Over(
                            null,
                            rows => rows.OrderByDescending(o => o.Id),
                            null,
                            (o, w) => o.Number + ":" + w.RowNumber()
                        )
                ),
            })
        );
    }

    // 42P01: invalid reference to FROM-clause entry for table "t0":
    // SELECT t2."Id", t2."Number" FROM "Customer" AS t0 INNER JOIN (SELECT t1."Id",
    // ROW_NUMBER() OVER (ORDER BY t1."Id") AS "Number" FROM "Order" AS t1
    // WHERE t1."CustomerId" = t0."Id") AS t2 ON t2."Number" > 0 WHERE t0."IsActive"
    [Fact(
        Skip = "#128: a filtered correlated Over in SelectMany is joined as a derived table without LATERAL, which PostgreSQL rejects"
    )]
    public void SelectsManyAWindowWithoutAResultSelector()
    {
        database.AssertSameRows(
            Customers
                .Where(c => c.IsActive)
                .SelectMany(c =>
                    Orders
                        .Where(o => o.CustomerId == c.Id)
                        .Over(
                            null,
                            rows => rows.OrderBy(o => o.Id),
                            null,
                            (o, w) => new { o.Id, Number = w.RowNumber() }
                        )
                        .Where(x => x.Number > 0)
                )
        );
    }

    // SELECT t1."Id", CAST(COUNT(*) OVER () AS integer) AS "All" FROM "Customer" AS t0
    // INNER JOIN "Order" AS t1 ON t0."Id" = t1."CustomerId" WHERE t0."Id" = 2
    // gives All = 2 instead of 12.
    [Fact(
        Skip = "#128: a projection with a window aggregate joined as the inner sequence counts the joined rows only"
    )]
    public void JoinsAWindowAggregateOverEveryRow()
    {
        database.AssertSameRows(
            Customers
                .Where(c => c.Id == 2)
                .Join(
                    Orders.Select(o => new
                    {
                        o.Id,
                        o.CustomerId,
                        All = Orders.Count(),
                    }),
                    c => c.Id,
                    x => x.CustomerId,
                    (c, x) => new { x.Id, x.All }
                )
        );
    }

    // ------------------------------------- window functions read by window functions

    // LAG(CAST(t0."Id" AS bigint), 1, ROW_NUMBER() OVER (ORDER BY t0."Id"))
    // OVER (ORDER BY t0."Id")
    [Fact(
        Skip = "#128: a window function in the default of Lag or Lead is nested in it, which PostgreSQL rejects (42P20: window function calls cannot be nested)"
    )]
    public void ReadsAWindowFunctionAsTheDefaultOfAnOffset()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders,
                null,
                rows => rows.OrderBy(o => o.Id),
                null,
                (o, w) =>
                    new
                    {
                        o.Id,
                        Previous = w.Lag(x => (long)x.Id, 1, w.RowNumber()),
                        Next = w.Lead(x => (double?)x.Quantity, 2, w.PercentRank()),
                        Counted = w.Lag(x => x.Quantity, 1, w.Rows.Count()),
                    }
            )
        );
    }

    // LAG(t0."Id", CAST(ROW_NUMBER() OVER (ORDER BY t0."Id") AS integer) % 3, 0)
    // OVER (...)
    [Fact(
        Skip = "#128: a window function in the offset of Lag is nested in it, which PostgreSQL rejects (42P20: window function calls cannot be nested)"
    )]
    public void ReadsAnOffsetComputedFromAWindowFunction()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders,
                null,
                rows => rows.OrderBy(o => o.Id),
                null,
                (o, w) => new { o.Id, Back = w.Lag(x => x.Id, (int)w.RowNumber() % 3, 0) }
            )
        );
    }

    // LAG(t0."Quantity", 1, CAST(COUNT(*) OVER () AS integer)) OVER (ORDER BY t0."Id"):
    // 42P20: window function calls cannot be nested
    [Fact(
        Skip = "#128: an aggregate of the source table in an argument of a window function becomes a nested window aggregate, which PostgreSQL rejects"
    )]
    public void ReadsAnAggregateOfATableInTheArgumentsOfAWindowFunction()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders,
                null,
                rows => rows.OrderBy(o => o.Id),
                null,
                (o, w) =>
                    new
                    {
                        o.Id,
                        Previous = w.Lag(x => x.Quantity, 1, Orders.Count()),
                        Bucket = w.Ntile(Orders.Count(x => x.IsPaid)),
                        Scaled = w.Lead(x => x.Quantity * Orders.Count(), 1, 0),
                    }
            )
        );
    }

    // COUNT(*) FILTER (WHERE t0."Quantity" < (CAST(COUNT(*) OVER () AS integer) / 3))
    // OVER (ORDER BY t0."Id"): 42P20: window functions are not allowed in FILTER
    [Fact(
        Skip = "#128: an aggregate of the source table in the filter of a frame aggregate becomes a window aggregate in FILTER, which PostgreSQL rejects"
    )]
    public void FiltersTheFrameByAnAggregateOfATable()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders,
                null,
                rows => rows.OrderBy(o => o.Id),
                null,
                (o, w) =>
                    new
                    {
                        o.Id,
                        Small = w.Rows.Count(x => x.Quantity < Orders.Count() / 3),
                    }
            )
        );
    }

    [Fact]
    public void PartitionsAndOrdersByAnAggregateOfATable()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders,
                o => o.Quantity < Orders.Count() / 3,
                rows =>
                    rows.OrderBy(o => o.Quantity - Orders.Max(x => x.Quantity))
                        .ThenBy(o => o.Id),
                null,
                (o, w) => new { o.Id, Number = w.RowNumber() }
            )
        );
    }

    [Fact]
    public void ReadsGroupAggregatesInOtherRows()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders.GroupBy(o => o.CustomerId),
                null,
                rows => rows.OrderBy(g => g.Key),
                null,
                (g, w) =>
                    new
                    {
                        g.Key,
                        First = w.FirstValue(x => x.Count()),
                        Last = w.LastValue(x => x.Max(o => o.Id)),
                        Next = w.Lead(x => (decimal?)x.Sum(o => o.Total), 1, null),
                    }
            )
        );
    }

    // SELECT DISTINCT ON (t2."CustomerId") t2."CustomerId", ROW_NUMBER() OVER (ORDER BY
    // t2."CustomerId") AS "Number", ... FROM (... UNION ALL ...) AS t2
    [Fact(
        Skip = "#128: Over over UnionBy extends its DISTINCT ON SELECT, so the window is computed over both sequences before UnionBy keeps one row per key"
    )]
    public void ComputesTheWindowOverTheRowsOfUnionBy()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders
                    .Where(o => o.Id <= 6)
                    .UnionBy(Orders.Where(o => o.Id > 3), o => o.CustomerId),
                null,
                rows => rows.OrderBy(o => o.CustomerId),
                null,
                (o, w) =>
                    new
                    {
                        o.CustomerId,
                        Number = w.RowNumber(),
                        Count = w.Rows.Count(),
                    }
            )
        );
    }

    public sealed record NumberedOrder(int Id, int CustomerId, long Number, int Count);

    public sealed record GroupRank(int Key, int Size, long Rank);

    public sealed record RankedOrder(
        int Id,
        int CustomerId,
        bool IsPaid,
        decimal Total,
        long Rank
    );
}
