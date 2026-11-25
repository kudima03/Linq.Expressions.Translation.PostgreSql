namespace Linq.Expressions.Translation.PostgreSql.ExecutionTests;

// The frames of PostgreSqlFrame hold the rows PostgreSQL's frame clause says, which
// the aggregates of LINQ over PostgreSqlWindow.Rows read: ROWS counted from the
// current row, RANGE of the key's distance (numbers, durations and
// months), frames before or after the current row that may be empty, and keys that
// are NULL or tie.
[Collection(SharedDatabase.Name)]
public sealed class FrameSemanticsTests(Database database)
{
    private static readonly Dictionary<string, PostgreSqlFrame> Counted = new()
    {
        ["rows -2..0"] = PostgreSqlFrame.Rows(-2, 0),
        ["rows -1..1"] = PostgreSqlFrame.Rows(-1, 1),
        ["rows 1..2"] = PostgreSqlFrame.Rows(1, 2),
        ["rows -3..-1"] = PostgreSqlFrame.Rows(-3, -1),
        ["rows all..0"] = PostgreSqlFrame.Rows(null, 0),
        ["rows 0..all"] = PostgreSqlFrame.Rows(0, null),
        ["rows all..all"] = PostgreSqlFrame.Rows(null, null),
        ["rows all..-1"] = PostgreSqlFrame.Rows(null, -1),
        ["rows 0..0"] = PostgreSqlFrame.Rows(0, 0),
        ["rows huge"] = PostgreSqlFrame.Rows(-1000, 1000),
        ["range all..0"] = PostgreSqlFrame.Range(null, 0),
        ["range 0..0"] = PostgreSqlFrame.Range(0, 0),
        ["range 0..all"] = PostgreSqlFrame.Range(0, null),
    };

    private static readonly Dictionary<string, PostgreSqlFrame> Numeric = new()
    {
        ["range -2..0"] = PostgreSqlFrame.Range(-2, 0),
        ["range -1..1"] = PostgreSqlFrame.Range(-1, 1),
        ["range 1..3"] = PostgreSqlFrame.Range(1, 3),
        ["range -3..-1"] = PostgreSqlFrame.Range(-3, -1),
        ["range all..-1"] = PostgreSqlFrame.Range(null, -1),
        ["range 1..all"] = PostgreSqlFrame.Range(1, null),
    };

    public static TheoryData<string> CountedFrames => [.. Counted.Keys];

    // The frames whose rows do not depend on the order of peers.
    public static TheoryData<string> PeerFrames =>
        [
            .. Counted.Keys.Where(name =>
                !name.StartsWith("rows", StringComparison.Ordinal)
                || name == "rows all..all"
            ),
        ];

    public static TheoryData<string> NumericFrames => [.. Numeric.Keys];

    [Theory]
    [MemberData(nameof(PeerFrames))]
    public void AggregatesTheRowsOfAFrameOverTies(string frame)
    {
        database.AssertSameRows(
            Trees.Over(
                Orders,
                o => o.IsPaid,
                rows => rows.OrderBy(o => o.Priority).ThenBy(o => o.Quantity % 3),
                Counted[frame],
                (o, w) =>
                    new
                    {
                        o.Id,
                        Count = w.Rows.Count(),
                        Paid = w.Rows.Count(x => x.Quantity > 2),
                        Quantities = w.Rows.Sum(x => x.Quantity),
                        Points = w.Rows.Sum(x => x.Points),
                        Discounts = w.Rows.Sum(x => x.Discount),
                        Top = w.Rows.Max(x => x.Priority),
                        Least = w.Rows.Min(x => (decimal?)x.Total),
                        Mean = w.Rows.Average(x => (decimal?)x.Total),
                        Gifts = w.Rows.Any(x => x.IsGift == true),
                        Noted = w.Rows.All(x => x.Note != null),
                        Some = w.Rows.Any(),
                    }
            )
        );
    }

    [Theory]
    [MemberData(nameof(CountedFrames))]
    public void AggregatesTheRowsOfAFrameOverATotalOrder(string frame)
    {
        database.AssertSameRows(
            Trees.Over(
                Orders,
                o => o.CustomerId % 3,
                rows => rows.OrderByDescending(o => o.CreatedAt).ThenBy(o => o.Id),
                Counted[frame],
                (o, w) =>
                    new
                    {
                        o.Id,
                        Count = w.Rows.LongCount(),
                        Totals = w.Rows.Where(x => x.Quantity > 1).Sum(x => x.Total),
                        Largest = w.Rows.Max(x => (int?)x.Quantity),
                        Filtered = w
                            .Rows.Where(x => x.IsPaid)
                            .Count(x => x.Priority != null),
                        Mean = w
                            .Rows.Where(x => x.IsPaid)
                            .Average(x => (double?)x.Quantity),
                        First = w.FirstValue(x => (int?)x.Id),
                        Last = w.LastValue(x => (int?)x.Id),
                    }
            )
        );
    }

    [Theory]
    [MemberData(nameof(NumericFrames))]
    public void RangesOverANumericKey(string frame)
    {
        database.AssertSameRows(
            Trees.Over(
                Orders,
                null,
                rows => rows.OrderBy(o => o.Quantity),
                Numeric[frame],
                (o, w) =>
                    new
                    {
                        o.Id,
                        Count = w.Rows.Count(),
                        Quantities = w.Rows.Sum(x => x.Quantity),
                    }
            )
        );
        database.AssertSameRows(
            Trees.Over(
                Orders,
                o => o.IsPaid,
                rows => rows.OrderByDescending(o => o.Priority),
                Numeric[frame],
                (o, w) =>
                    new
                    {
                        o.Id,
                        Count = w.Rows.Count(),
                        Ids = w.Rows.Sum(x => x.Id),
                    }
            )
        );
    }

    [Theory]
    [InlineData(-5.5, 5.5)]
    [InlineData(-0.125, 0.0)]
    [InlineData(0.0, 100.0)]
    [InlineData(null, -3.0)]
    public void RangesOverADecimalKey(double? start, double? end)
    {
        database.AssertSameRows(
            Trees.Over(
                Orders,
                null,
                rows => rows.OrderBy(o => o.Total),
                PostgreSqlFrame.Range((decimal?)start, (decimal?)end),
                (o, w) =>
                    new
                    {
                        o.Id,
                        Count = w.Rows.Count(),
                        Totals = w.Rows.Sum(x => x.Total),
                    }
            )
        );
    }

    [Theory]
    [InlineData(-31, 0)]
    [InlineData(-1, 1)]
    [InlineData(0, 400)]
    [InlineData(1, null)]
    public void RangesOverDaysOfATimestamp(int? start, int? end)
    {
        database.AssertSameRows(
            Trees.Over(
                Orders,
                null,
                rows => rows.OrderBy(o => o.CreatedAt),
                PostgreSqlFrame.Range(
                    start is int first ? TimeSpan.FromDays(first) : null,
                    end is int last ? TimeSpan.FromDays(last) : null
                ),
                (o, w) => new { o.Id, Count = w.Rows.Count() }
            )
        );
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(-12, 0)]
    [InlineData(0, 2)]
    [InlineData(null, -1)]
    public void RangesOverMonthsOfATimestampAndADate(int? start, int? end)
    {
        database.AssertSameRows(
            Trees.Over(
                Orders,
                null,
                rows => rows.OrderBy(o => o.CreatedAt),
                PostgreSqlFrame.RangeMonths(start, end),
                (o, w) => new { o.Id, Count = w.Rows.Count() }
            )
        );
        database.AssertSameRows(
            Trees.Over(
                Orders,
                null,
                rows => rows.OrderByDescending(o => o.ShippedOn),
                PostgreSqlFrame.RangeMonths(start, end),
                (o, w) => new { o.Id, Count = w.Rows.Count() }
            )
        );
    }

    [Fact]
    public void RangesOverSecondsOfATimeAndDaysOfANullableDate()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders,
                null,
                rows => rows.OrderBy(o => o.DeliveryTime),
                PostgreSqlFrame.Range(TimeSpan.FromHours(-3), TimeSpan.FromSeconds(1.5)),
                (o, w) => new { o.Id, Count = w.Rows.Count() }
            )
        );
        database.AssertSameRows(
            Trees.Over(
                Orders,
                null,
                rows => rows.OrderBy(o => o.ShippedOn),
                PostgreSqlFrame.Range(TimeSpan.FromDays(-2), TimeSpan.FromDays(2)),
                (o, w) => new { o.Id, Count = w.Rows.Count() }
            )
        );
    }

    [Fact]
    public void ReadsThePartitionWithoutAnOrder()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders,
                o => o.CustomerId,
                null,
                null,
                (o, w) =>
                    new
                    {
                        o.Id,
                        Count = w.Rows.Count(),
                        Share = o.Total / w.Rows.Sum(x => x.Total == 0 ? 1 : x.Total),
                    }
            )
        );
        database.AssertSameRows(
            Trees.Over(
                Orders,
                o => o.CustomerId,
                null,
                PostgreSqlFrame.Range(null, 0),
                (o, w) => new { o.Id, Count = w.Rows.Count() }
            )
        );
    }

    [Fact]
    public void AggregatesAnEmptyPartitionOfAFilter()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders,
                o => o.CustomerId,
                rows => rows.OrderBy(o => o.Id),
                PostgreSqlFrame.Rows(null, null),
                (o, w) =>
                    new
                    {
                        o.Id,
                        None = w.Rows.Count(x => x.Total > 1000),
                        Zero = w.Rows.Where(x => x.Total > 1000).Sum(x => x.Total),
                        Missing = w
                            .Rows.Where(x => x.Total > 1000)
                            .Max(x => (decimal?)x.Total),
                        Any = w.Rows.Any(x => x.Total > 1000),
                        All = w.Rows.Where(x => x.Total > 1000).All(x => x.IsPaid),
                    }
            )
        );
    }
}
