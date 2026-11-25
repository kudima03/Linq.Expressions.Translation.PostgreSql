namespace Linq.Expressions.Translation.PostgreSql.ExecutionTests;

// The window functions of PostgreSqlWindows.Over give every row what PostgreSQL's
// definitions give it, which the in-memory side (MemoryWindow) computes from them:
// rankings over ties and nulls, rows at offsets with and without defaults, values
// of frames that may be empty. A function whose result depends on the order of
// peers is only read over a total order.
[Collection(SharedDatabase.Name)]
public sealed class WindowFunctionSemanticsTests(Database database)
{
    [Fact]
    public void RanksPeersWithTiesAndNulls()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders,
                o => o.IsPaid,
                rows => rows.OrderBy(o => o.Priority),
                null,
                (o, w) =>
                    new
                    {
                        o.Id,
                        Rank = w.Rank(),
                        Dense = w.DenseRank(),
                        Percent = w.PercentRank(),
                        Cumulative = w.CumeDist(),
                    }
            )
        );
    }

    [Fact]
    public void RanksDescendingKeysWithNullsLast()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders,
                null,
                rows => rows.OrderByDescending(o => o.Priority).ThenBy(o => o.IsPaid),
                null,
                (o, w) =>
                    new
                    {
                        o.Id,
                        Rank = w.Rank(),
                        Dense = w.DenseRank(),
                        Cumulative = w.CumeDist(),
                    }
            )
        );
    }

    [Fact]
    public void NumbersAndBucketsRowsOfATotalOrder()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders,
                o => o.CustomerId,
                rows => rows.OrderBy(o => o.CreatedAt).ThenByDescending(o => o.Id),
                null,
                (o, w) =>
                    new
                    {
                        o.Id,
                        Number = w.RowNumber(),
                        Halves = w.Ntile(2),
                        Thirds = w.Ntile(3),
                        Many = w.Ntile(20),
                    }
            )
        );
    }

    [Fact]
    public void NumbersEveryRowWithoutPartition()
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
                        Number = w.RowNumber(),
                        Buckets = w.Ntile(5),
                        Percent = w.PercentRank(),
                    }
            )
        );
    }

    [Fact]
    public void ReadsRowsAtOffsetsWithAndWithoutDefaults()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders,
                o => o.IsPaid,
                rows => rows.OrderBy(o => o.Id),
                null,
                (o, w) =>
                    new
                    {
                        o.Id,
                        Previous = w.Lag(x => (int?)x.Quantity),
                        TwoBack = w.Lag(x => (int?)x.Id, 2),
                        Defaulted = w.Lag(x => x.Quantity, 1, -1),
                        Next = w.Lead(x => x.Number, 1, "none"),
                        ThreeOn = w.Lead(x => (decimal?)x.Total, 3),
                        Nullable = w.Lead(x => x.Priority),
                        Backwards = w.Lag(x => (int?)x.Id, -1),
                        Current = w.Lead(x => x.Id, 0, 0),
                        OwnDefault = w.Lag(x => x.Total, 1, o.Total),
                    }
            )
        );
    }

    [Fact]
    public void ReadsTheValuesOfFrames()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders,
                o => o.CustomerId,
                rows => rows.OrderBy(o => o.Id),
                PostgreSqlFrame.Rows(-1, 1),
                (o, w) =>
                    new
                    {
                        o.Id,
                        First = w.FirstValue(x => x.Id),
                        Last = w.LastValue(x => x.Number),
                        Second = w.NthValue(x => (int?)x.Id, 2),
                        Third = w.NthValue(x => x.Note, 3),
                    }
            )
        );
    }

    [Fact]
    public void ReadsValuesOfFramesThatMayBeEmpty()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders,
                null,
                rows => rows.OrderBy(o => o.Id),
                PostgreSqlFrame.Rows(1, 2),
                (o, w) =>
                    new
                    {
                        o.Id,
                        First = w.FirstValue(x => (int?)x.Id),
                        Last = w.LastValue(x => (decimal?)x.Total),
                        Second = w.NthValue(x => (int?)x.Quantity, 2),
                    }
            )
        );
    }

    [Fact]
    public void ReadsTheLastPeerInTheDefaultFrame()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders,
                null,
                rows => rows.OrderBy(o => o.CustomerId),
                null,
                (o, w) =>
                    new
                    {
                        o.Id,
                        Last = w.LastValue(x => x.CustomerId),
                        First = w.FirstValue(x => x.CustomerId),
                        Count = w.Rows.Count(),
                    }
            )
        );
    }

    [Fact]
    public void PartitionsByCompositeAndComputedKeys()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders,
                o => new { o.CustomerId, o.IsPaid },
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
                o => new ValueTuple<int, bool>(o.Quantity % 2, o.Priority == null),
                null,
                null,
                (o, w) =>
                    new
                    {
                        o.Id,
                        Count = w.Rows.LongCount(),
                        Total = w.Rows.Sum(x => x.Total),
                    }
            )
        );
    }

    [Fact]
    public void OrdersStringsByCodePoint()
    {
        database.AssertSameRows(
            Trees.Over(
                Customers,
                c => c.Region,
                rows =>
                    rows.OrderBy(c => c.Name, StringComparer.Ordinal).ThenBy(c => c.Id),
                null,
                (c, w) =>
                    new
                    {
                        c.Id,
                        Number = w.RowNumber(),
                        Previous = w.Lag(x => x.Name),
                    }
            )
        );
    }

    [Fact]
    public void RanksScalarRowsByThemselves()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders.Select(o => o.Quantity),
                null,
                rows => rows.OrderDescending(),
                null,
                (q, w) =>
                    new
                    {
                        Quantity = q,
                        Rank = w.Rank(),
                        Dense = w.DenseRank(),
                    }
            )
        );
    }

    [Fact]
    public void KeepsTheFirstRowOfEachPartitionThroughAFilter()
    {
        database.AssertSameRows(
            Trees
                .Over(
                    Orders,
                    o => o.CustomerId,
                    rows => rows.OrderByDescending(o => o.Total).ThenBy(o => o.Id),
                    null,
                    (o, w) =>
                        new
                        {
                            o.Id,
                            o.CustomerId,
                            Number = w.RowNumber(),
                        }
                )
                .Where(x => x.Number == 1)
        );
    }

    [Fact]
    public void OrdersAndPagesByAWindowFunction()
    {
        database.AssertSameSequence(
            Trees
                .Over(
                    Orders,
                    null,
                    rows => rows.OrderBy(o => o.Total).ThenBy(o => o.Id),
                    null,
                    (o, w) => new { o.Id, Number = w.RowNumber() }
                )
                .OrderByDescending(x => x.Number)
                .Skip(2)
                .Take(4)
        );
    }

    [Fact]
    public void ComputesWindowsOverFilteredAndPagedRows()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders.Where(o => o.Quantity > 1),
                null,
                rows => rows.OrderBy(o => o.Id),
                null,
                (o, w) => new { o.Id, Number = w.RowNumber() }
            )
        );
        database.AssertSameRows(
            Trees.Over(
                Orders.OrderBy(o => o.Id).Take(5),
                null,
                rows => rows.OrderByDescending(o => o.Id),
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
                Orders.Select(o => o.CustomerId).Distinct(),
                null,
                rows => rows.Order(),
                null,
                (id, w) => new { Id = id, Number = w.RowNumber() }
            )
        );
    }

    [Fact]
    public void ComputesASecondWindowOverTheFirst()
    {
        database.AssertSameRows(
            Trees.Over(
                Trees.Over(
                    Orders,
                    o => o.CustomerId,
                    rows => rows.OrderBy(o => o.Id),
                    null,
                    (o, w) => new { o.Id, Number = w.RowNumber() }
                ),
                x => x.Number,
                rows => rows.OrderBy(x => x.Id),
                null,
                (x, w) =>
                    new
                    {
                        x.Id,
                        x.Number,
                        Rank = w.RowNumber(),
                    }
            )
        );
    }

    [Fact]
    public void ComputesWindowsOverJoinedRows()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders.Join(
                    Customers,
                    o => o.CustomerId,
                    c => c.Id,
                    (o, c) =>
                        new
                        {
                            o.Id,
                            o.Total,
                            c.Region,
                        }
                ),
                x => x.Region,
                rows => rows.OrderBy(x => x.Total).ThenBy(x => x.Id),
                PostgreSqlFrame.Rows(null, 0),
                (x, w) =>
                    new
                    {
                        x.Id,
                        Running = w.Rows.Sum(r => r.Total),
                        Rank = w.Rank(),
                    }
            )
        );
    }

    [Fact]
    public void RanksGroups()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders
                    .GroupBy(o => o.CustomerId)
                    .Select(g => new { g.Key, Total = g.Sum(o => o.Total) }),
                null,
                rows => rows.OrderByDescending(g => g.Total).ThenBy(g => g.Key),
                null,
                (g, w) =>
                    new
                    {
                        g.Key,
                        Rank = w.Rank(),
                        Share = g.Total / w.Rows.Sum(x => x.Total),
                    }
            )
        );
    }

    [Fact]
    public void RanksTheGroupsOfTheSameSelect()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders.GroupBy(o => o.IsPaid),
                null,
                rows => rows.OrderBy(g => g.Count()).ThenBy(g => g.Key),
                PostgreSqlFrame.Rows(null, null),
                (g, w) =>
                    new
                    {
                        g.Key,
                        Number = w.RowNumber(),
                        Groups = w.Rows.Count(),
                        Orders = w.Rows.Sum(x => x.Count()),
                    }
            )
        );
    }

    [Fact]
    public void AggregatesTheWindowedRows()
    {
        database.AssertSameResult(
            Trees.Over(
                Orders,
                o => o.CustomerId,
                rows => rows.OrderBy(o => o.Id),
                null,
                (o, w) => w.RowNumber()
            ),
            q => q.Max()
        );
        database.AssertSameResult(
            Trees.Over(Orders, null, null, null, (o, w) => w.Rows.Sum(x => x.Quantity)),
            q => q.Count()
        );
    }

    [Fact]
    public void ComputesWindowsOfACorrelatedSubquery()
    {
        database.AssertSameRows(
            Customers.Select(c => new
            {
                c.Id,
                Latest = Orders
                    .Where(o => o.CustomerId == c.Id)
                    .Over(
                        null,
                        rows => rows.OrderBy(o => o.Id),
                        null,
                        (o, w) => (long?)w.RowNumber()
                    )
                    .Max(),
            })
        );
    }

    [Fact]
    public void ComputesNoRowOverAnEmptySource()
    {
        database.AssertSameResult(
            Trees.Over(
                Orders.Where(o => o.Id > 1000),
                o => o.CustomerId,
                rows => rows.OrderBy(o => o.Id),
                PostgreSqlFrame.Rows(-1, 1),
                (o, w) =>
                    new
                    {
                        o.Id,
                        Number = w.RowNumber(),
                        Count = w.Rows.Count(),
                    }
            ),
            q => q.Count()
        );
    }
}
