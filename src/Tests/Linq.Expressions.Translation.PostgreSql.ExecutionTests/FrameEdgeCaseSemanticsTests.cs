namespace Linq.Expressions.Translation.PostgreSql.ExecutionTests;

// Edge cases of the frames of PostgreSqlFrame and of the aggregates of LINQ over
// PostgreSqlWindow.Rows: frames that are always empty, frames of NULLs only, offsets
// at the limits of their types, fractional offsets over integer keys, NaN and
// infinities, descending keys, aggregates of strings over frames that may be empty,
// windows over groups and the window aggregates over every row beside them.
[Collection(SharedDatabase.Name)]
public sealed class FrameEdgeCaseSemanticsTests(Database database)
{
    // Frames that hold no row for any row: after the current row and ending before
    // its start, before it and ending before its start, and a RANGE whose end lies
    // before its start.
    private static readonly Dictionary<string, PostgreSqlFrame> Empty = new()
    {
        ["rows 2..1"] = PostgreSqlFrame.Rows(2, 1),
        ["rows -1..-3"] = PostgreSqlFrame.Rows(-1, -3),
        ["rows 20..20"] = PostgreSqlFrame.Rows(20, 20),
    };

    public static TheoryData<string> EmptyFrames => [.. Empty.Keys];

    [Theory]
    [MemberData(nameof(EmptyFrames))]
    public void AggregatesAnEmptyFrameWithTheDefaultsOfDotNet(string frame)
    {
        database.AssertSameRows(
            Trees.Over(
                Orders,
                o => o.CustomerId % 2,
                rows => rows.OrderBy(o => o.Id),
                Empty[frame],
                (o, w) =>
                    new
                    {
                        o.Id,
                        Count = w.Rows.Count(),
                        Long = w.Rows.LongCount(x => x.IsPaid),
                        Quantities = w.Rows.Sum(x => x.Quantity),
                        Points = w.Rows.Sum(x => x.Points),
                        Totals = w.Rows.Sum(x => x.Total),
                        Weights = w.Rows.Sum(x => x.Weight),
                        Priorities = w.Rows.Sum(x => x.Priority),
                        Discounts = w.Rows.Sum(x => x.Discount),
                        Mean = w.Rows.Average(x => (double?)x.Quantity),
                        MeanTotal = w.Rows.Average(x => (decimal?)x.Total),
                        Least = w.Rows.Min(x => (int?)x.Quantity),
                        Latest = w.Rows.Max(x => (DateTime?)x.CreatedAt),
                        Note = w.Rows.Max(x => x.Note),
                        Some = w.Rows.Any(),
                        Gift = w.Rows.Any(x => x.IsGift == true),
                        Every = w.Rows.All(x => x.IsPaid),
                        First = w.FirstValue(x => (int?)x.Id),
                    }
            )
        );
    }

    [Fact]
    public void AggregatesAnEmptyRangeFrame()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders,
                null,
                rows => rows.OrderBy(o => o.Total),
                PostgreSqlFrame.Range(1m, 0.5m),
                (o, w) =>
                    new
                    {
                        o.Id,
                        Count = w.Rows.Count(),
                        Totals = w.Rows.Sum(x => x.Total),
                        Top = w.Rows.Max(x => (decimal?)x.Total),
                        Some = w.Rows.Any(),
                        Every = w.Rows.All(x => x.Total > 1000),
                    }
            )
        );
    }

    [Fact]
    public void AggregatesFramesOfNullsOnly()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders,
                null,
                rows => rows.OrderBy(o => o.Id),
                PostgreSqlFrame.Rows(-1, 1),
                (o, w) =>
                    new
                    {
                        o.Id,
                        Count = w.Rows.Count(x => x.Priority == null),
                        Sum = w.Rows.Where(x => x.Priority == null).Sum(x => x.Priority),
                        Mean = w
                            .Rows.Where(x => x.Priority == null)
                            .Average(x => x.Priority),
                        Top = w.Rows.Where(x => x.Priority == null).Max(x => x.Priority),
                        Discount = w
                            .Rows.Where(x => x.Discount == null)
                            .Sum(x => x.Discount),
                        Least = w
                            .Rows.Where(x => x.Discount == null)
                            .Min(x => x.Discount),
                        Note = w.Rows.Select(x => x.Note).Where(n => n == null).Max(),
                        Gift = w.Rows.Where(x => x.IsGift == null).Max(x => x.IsGift),
                    }
            )
        );
    }

    [Fact]
    public void AveragesIntegersAsDoubles()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders,
                o => o.IsPaid,
                rows => rows.OrderBy(o => o.Id),
                PostgreSqlFrame.Rows(-1, 1),
                (o, w) =>
                    new
                    {
                        o.Id,
                        Quantity = w.Rows.Average(x => x.Quantity),
                        Points = w.Rows.Average(x => x.Points),
                        Priority = w.Rows.Average(x => x.Priority),
                        Total = w.Rows.Average(x => x.Total),
                        Discount = w.Rows.Average(x => x.Discount),
                        Halves = w.Rows.Sum(x => x.Quantity) / w.Rows.Count(),
                    }
            )
        );
    }

    [Fact]
    public void SumsAndAveragesDoublesWithInfinityAndNaN()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders,
                null,
                rows => rows.OrderBy(o => o.Id),
                PostgreSqlFrame.Rows(-1, 1),
                (o, w) =>
                    new
                    {
                        o.Id,
                        Sum = w.Rows.Sum(x => x.Weight),
                        Mean = w.Rows.Average(x => x.Weight),
                        Finite = w.Rows.Count(x => double.IsFinite(x.Weight)),
                    }
            )
        );
    }

    // Weight is NaN in order 10 and +Infinity in order 8. .NET's Max skips NaN and
    // its Min returns it; PostgreSQL orders NaN above every number, so MAX is NaN
    // and MIN skips it. Observed for order 10 (frame 9, 10, 11): Max NaN, Min 1.5;
    // .NET gives Max 2.5 and Min NaN.
    [Fact]
    public void TakesTheExtremesOfDoublesWithNaNAsDotNet()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders,
                null,
                rows => rows.OrderBy(o => o.Id),
                PostgreSqlFrame.Rows(-1, 1),
                (o, w) =>
                    new
                    {
                        o.Id,
                        Top = w.Rows.Max(x => x.Weight),
                        Least = w.Rows.Min(x => x.Weight),
                    }
            )
        );
    }

    // Ordered by Weight, .NET sorts NaN before every number and PostgreSQL after it.
    // Observed for order 10 (NaN): Before 12 where .NET gives 1, and the frame
    // from UNBOUNDED PRECEDING to 1 PRECEDING holds every row where .NET holds only
    // the NaN row (and order 12, -2.5, finds 0 rows where .NET finds the NaN row).
    [Fact]
    public void OrdersAWindowByADoubleWithNaNAsDotNet()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders,
                null,
                rows => rows.OrderBy(o => o.Weight).ThenBy(o => o.Id),
                PostgreSqlFrame.Rows(null, 0),
                (o, w) => new { o.Id, Before = w.Rows.Count() }
            )
        );
        database.AssertSameRows(
            Trees.Over(
                Orders,
                null,
                rows => rows.OrderBy(o => o.Weight),
                PostgreSqlFrame.Range(null, -1),
                (o, w) => new { o.Id, Below = w.Rows.Count() }
            )
        );
    }

    // In .NET NaN > 2 is false; in PostgreSQL NaN is greater than every number, so
    // FILTER (WHERE "Weight" > 2) counts order 10. Observed for orders 9, 10 and 11
    // (frames holding order 10): one row more than .NET counts.
    [Fact]
    public void FiltersTheRowsOfAFrameByADoubleWithNaN()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders,
                null,
                rows => rows.OrderBy(o => o.Id),
                PostgreSqlFrame.Rows(-1, 1),
                (o, w) =>
                    new
                    {
                        o.Id,
                        Heavy = w.Rows.Count(x => x.Weight > 2),
                        Light = w.Rows.Where(x => x.Weight <= 2).Sum(x => x.Quantity),
                    }
            )
        );
    }

    // A bounded RANGE holds the rows by their distance, whatever the side NaN sorts
    // on: NaN and Infinity have only their peers.
    [Theory]
    [InlineData(-1.0, 1.0)]
    [InlineData(-0.5, 0.0)]
    [InlineData(0.0, 1e12)]
    public void RangesOverADoubleKeyWithNaNAndInfinity(double? start, double? end)
    {
        database.AssertSameRows(
            Trees.Over(
                Orders,
                null,
                rows => rows.OrderBy(o => o.Weight),
                PostgreSqlFrame.Range((decimal?)start, (decimal?)end),
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

    // ERROR: RANGE with offset PRECEDING/FOLLOWING is not supported for column type
    // integer and offset type numeric (the offset 0.5 is a numeric literal).
    [Fact]
    public void RangesByAFractionOverAnIntegerKey()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders,
                null,
                rows => rows.OrderBy(o => o.Quantity),
                PostgreSqlFrame.Range(-0.5m, 0.5m),
                (o, w) => new { o.Id, Peers = w.Rows.Count() }
            )
        );
    }

    // ERROR: RANGE with offset PRECEDING/FOLLOWING is not supported for column type
    // bigint and offset type numeric.
    [Fact]
    public void RangesByAFractionOverABigintKey()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders,
                null,
                rows => rows.OrderBy(o => o.Points),
                PostgreSqlFrame.Range(-1.5m, 2.5m),
                (o, w) => new { o.Id, Near = w.Rows.Count() }
            )
        );
    }

    // ERROR: RANGE with offset PRECEDING/FOLLOWING is not supported for column type
    // integer and offset type numeric (79228162514264337593543950335 is no bigint).
    [Fact]
    public void RangesByTheLargestDecimalOverAnIntegerKey()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders,
                null,
                rows => rows.OrderBy(o => o.Quantity),
                PostgreSqlFrame.Range(decimal.MinValue, 0),
                (o, w) => new { o.Id, Running = w.Rows.Count() }
            )
        );
    }

    [Fact]
    public void RangesByWholeAndLargeOffsetsOverIntegerKeys()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders,
                null,
                rows => rows.OrderBy(o => o.Quantity),
                PostgreSqlFrame.Range(-10_000_000_000m, 2.000m),
                (o, w) => new { o.Id, Count = w.Rows.Count() }
            )
        );
        database.AssertSameRows(
            Trees.Over(
                Orders,
                null,
                rows => rows.OrderByDescending(o => o.Points),
                PostgreSqlFrame.Range(-1.0m, 9_223_372_036_854_775_807m),
                (o, w) => new { o.Id, Count = w.Rows.Count() }
            )
        );
        database.AssertSameRows(
            Trees.Over(
                Orders,
                null,
                rows => rows.OrderBy(o => o.Status),
                PostgreSqlFrame.Range(-1, -1),
                (o, w) => new { o.Id, Count = w.Rows.Count() }
            )
        );
    }

    // ERROR: bigint out of range (9223372036854775808 PRECEDING).
    [Fact]
    public void CountsRowsFromTheSmallestLong()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders,
                null,
                rows => rows.OrderBy(o => o.Id),
                PostgreSqlFrame.Rows(long.MinValue, 0),
                (o, w) => new { o.Id, Running = w.Rows.Count() }
            )
        );
    }

    [Fact]
    public void CountsRowsToTheLargestLongs()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders,
                o => o.IsPaid,
                rows => rows.OrderBy(o => o.Id),
                PostgreSqlFrame.Rows(-long.MaxValue, long.MaxValue),
                (o, w) =>
                    new
                    {
                        o.Id,
                        Count = w.Rows.Count(),
                        Last = w.LastValue(x => x.Id),
                    }
            )
        );
    }

    // ERROR: interval field value out of range: "2147483648 months", whatever the
    // rows: the statement does not plan even over no row.
    [Fact]
    public void PlansAFrameFromTheSmallestMonthOffset()
    {
        string sql = new PostgreSqlQuery(
            Trees
                .Over(
                    Orders.Where(o => o.Id < 0),
                    null,
                    rows => rows.OrderBy(o => o.CreatedAt),
                    PostgreSqlFrame.RangeMonths(int.MinValue, 0),
                    (o, w) => w.Rows.Count()
                )
                .Expression
        ).TextValue;

        Assert.Empty(database.Query(sql).Rows);
    }

    [Fact]
    public void RangesOverDescendingKeys()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders,
                null,
                rows => rows.OrderByDescending(o => o.Total),
                PostgreSqlFrame.Range(-0.5m, 0.25m),
                (o, w) =>
                    new
                    {
                        o.Id,
                        Count = w.Rows.Count(),
                        Totals = w.Rows.Sum(x => x.Total),
                    }
            )
        );
        database.AssertSameRows(
            Trees.Over(
                Orders,
                null,
                rows => rows.OrderByDescending(o => o.CreatedAt),
                PostgreSqlFrame.Range(TimeSpan.FromDays(-45), TimeSpan.FromDays(1)),
                (o, w) => new { o.Id, Count = w.Rows.Count() }
            )
        );
        database.AssertSameRows(
            Trees.Over(
                Orders,
                null,
                rows => rows.OrderByDescending(o => o.DeliveryTime),
                PostgreSqlFrame.Range(TimeSpan.FromHours(-6), TimeSpan.Zero),
                (o, w) => new { o.Id, Count = w.Rows.Count() }
            )
        );
        database.AssertSameRows(
            Trees.Over(
                Orders,
                null,
                rows => rows.OrderByDescending(o => o.Quantity),
                PostgreSqlFrame.Range(null, -2),
                (o, w) => new { o.Id, Count = w.Rows.Count() }
            )
        );
        database.AssertSameRows(
            Trees.Over(
                Orders,
                null,
                rows => rows.OrderByDescending(o => o.Discount),
                PostgreSqlFrame.Range(-0.5m, 0.5m),
                (o, w) =>
                    new
                    {
                        o.Id,
                        Count = w.Rows.Count(),
                        Discounts = w.Rows.Sum(x => x.Discount),
                    }
            )
        );
    }

    [Theory]
    [InlineData(-1, 1)]
    [InlineData(null, -1)]
    [InlineData(1, 12)]
    [InlineData(-13, -11)]
    [InlineData(0, 0)]
    public void RangesOverMonthsOfANullableDateAscending(int? start, int? end)
    {
        database.AssertSameRows(
            Trees.Over(
                Orders,
                null,
                rows => rows.OrderBy(o => o.ShippedOn),
                PostgreSqlFrame.RangeMonths(start, end),
                (o, w) => new { o.Id, Count = w.Rows.Count() }
            )
        );
        database.AssertSameRows(
            Trees.Over(
                Orders,
                o => o.IsPaid,
                rows => rows.OrderBy(o => o.CreatedAt),
                PostgreSqlFrame.RangeMonths(start, end),
                (o, w) => new { o.Id, Count = w.Rows.Count() }
            )
        );
    }

    [Fact]
    public void RangesOverTimesWithoutWrappingAroundMidnight()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders,
                null,
                rows => rows.OrderBy(o => o.DeliveryTime),
                PostgreSqlFrame.Range(TimeSpan.FromDays(-1), TimeSpan.FromDays(1)),
                (o, w) => new { o.Id, Count = w.Rows.Count() }
            )
        );
        database.AssertSameRows(
            Trees.Over(
                Orders,
                null,
                rows => rows.OrderBy(o => o.DeliveryTime),
                PostgreSqlFrame.Range(TimeSpan.FromMicroseconds(-1), TimeSpan.Zero),
                (o, w) => new { o.Id, Count = w.Rows.Count() }
            )
        );
        database.AssertSameRows(
            Trees.Over(
                Orders,
                null,
                rows => rows.OrderBy(o => o.DeliveryTime),
                PostgreSqlFrame.Range(TimeSpan.FromMinutes(1), TimeSpan.FromHours(6)),
                (o, w) => new { o.Id, Count = w.Rows.Count() }
            )
        );
    }

    [Fact]
    public void RangesOverHoursOfADate()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders,
                null,
                rows => rows.OrderBy(o => o.ShippedOn),
                PostgreSqlFrame.Range(TimeSpan.FromHours(-36), TimeSpan.FromHours(12)),
                (o, w) => new { o.Id, Count = w.Rows.Count() }
            )
        );
    }

    [Fact]
    public void RangesOverADurationKey()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders.Select(o => new
                {
                    o.Id,
                    Span = o.DeliveryTime - TimeOnly.MinValue,
                }),
                null,
                rows => rows.OrderBy(x => x.Span),
                PostgreSqlFrame.Range(TimeSpan.FromHours(-3), TimeSpan.FromMinutes(30)),
                (x, w) => new { x.Id, Count = w.Rows.Count() }
            )
        );
    }

    [Fact]
    public void AggregatesWithSeveralFiltersInOneSelector()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders,
                o => o.CustomerId % 3,
                rows => rows.OrderBy(o => o.CreatedAt).ThenBy(o => o.Id),
                PostgreSqlFrame.Rows(-2, 1),
                (o, w) =>
                    new
                    {
                        o.Id,
                        Paid = w.Rows.Count(x => x.IsPaid),
                        Unpaid = w.Rows.Where(x => !x.IsPaid).Sum(x => x.Total),
                        High = w.Rows.Where(x => x.Priority > 1).Average(x => x.Priority),
                        Unnoted = w.Rows.Count(x => x.Note == null),
                        Positive = w.Rows.All(x => x.Quantity > 0),
                        WithO = w.Rows.Any(x => x.Note != null && x.Note.Contains("o")),
                        Gifts = w
                            .Rows.Where(x => x.IsGift == true)
                            .Where(x => x.IsPaid)
                            .LongCount(x => x.Quantity >= 2),
                        Large = w
                            .Rows.Where(x => x.Total > 2)
                            .Max(x => (decimal?)x.Total),
                    }
            )
        );
    }

    [Fact]
    public void ComposesAggregatesWithWindowFunctions()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders,
                o => o.IsPaid,
                rows => rows.OrderBy(o => o.Id),
                PostgreSqlFrame.Rows(null, 0),
                (o, w) =>
                    new
                    {
                        o.Id,
                        Zero = w.RowNumber() - w.Rows.LongCount(),
                        Change = w.Rows.Sum(x => x.Quantity)
                            - w.Lag(x => x.Quantity, 1, 0),
                        Ranked = w.Rank() + w.Rows.Count(x => x.Quantity > 2),
                        Share = (double)w.Rows.Count() / w.Rows.Sum(x => 1),
                        Spread = w.Rows.Max(x => x.Quantity)
                            - w.Rows.Min(x => x.Quantity),
                        Above = o.Quantity > w.Rows.Average(x => x.Quantity),
                        Started = w.FirstValue(x => x.Id) + w.Rows.Count(),
                    }
            )
        );
    }

    [Fact]
    public void AggregatesTheRowsOfAFrameAfterSelect()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders,
                null,
                rows => rows.OrderBy(o => o.Id),
                PostgreSqlFrame.Rows(-2, 2),
                (o, w) =>
                    new
                    {
                        o.Id,
                        Doubled = w.Rows.Select(x => x.Quantity * 2).Count(q => q > 4),
                        Members = w
                            .Rows.Select(x => new { x.Total, Paid = x.IsPaid })
                            .Where(x => x.Paid)
                            .Sum(x => x.Total),
                        Present = w
                            .Rows.Select(x => x.Priority)
                            .Where(p => p != null)
                            .Average(),
                        Discounts = w.Rows.Sum(x => x.Discount ?? 0),
                        Notes = w.Rows.Select(x => x.Note).Count(n => n != null),
                        Lengths = w.Rows.Max(x => x.Number.Length),
                        Statuses = w.Rows.Max(x => x.Status),
                        AnyPaid = w.Rows.Max(x => x.IsPaid),
                        AllGifts = w.Rows.Min(x => x.IsGift),
                    }
            )
        );
    }

    [Fact]
    public void AggregatesComputedValuesOfTheRowsOfAFrame()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders,
                o => o.Quantity % 2,
                rows => rows.OrderByDescending(o => o.Total).ThenBy(o => o.Id),
                PostgreSqlFrame.Rows(-1, 2),
                (o, w) =>
                    new
                    {
                        o.Id,
                        Paid = w.Rows.Sum(x => x.IsPaid ? 1 : 0),
                        Tags = w.Rows.Max(x => x.Tags.Length),
                        Gift = w.Rows.Any(x => x.Tags.Contains("gift")),
                        Attached = w.Rows.Count(x =>
                            Attachments.Any(a => a.OrderId == x.Id)
                        ),
                        Exact = w.Rows.Average(x => (decimal)x.Quantity),
                        Large = w.Rows.Sum(x => (long)x.Quantity * 1_000_000_000),
                        Chosen = w.Rows.Count() > 2 ? w.Rows.Sum(x => x.Total) : -1m,
                        Lengths = w.Rows.Sum(x => x.Number.Length),
                    }
            )
        );
    }

    [Fact]
    public void TakesTheExtremesOfTemporalValues()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders,
                o => o.CustomerId % 2,
                rows => rows.OrderBy(o => o.Id),
                PostgreSqlFrame.Rows(-1, 1),
                (o, w) =>
                    new
                    {
                        o.Id,
                        Created = w.Rows.Min(x => x.CreatedAt),
                        Shipped = w.Rows.Max(x => x.ShippedOn),
                        Delivered = w.Rows.Min(x => x.DeliveryTime),
                        Paid = w.Rows.Max(x => x.PaidAt),
                    }
            )
        );
    }

    [Fact]
    public void ReadsTheExtremeStringOfAFrameThatMayBeEmpty()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders,
                null,
                rows => rows.OrderBy(o => o.Id),
                PostgreSqlFrame.Rows(-1, -1),
                (o, w) =>
                    new
                    {
                        o.Id,
                        Previous = w.Rows.Max(x => x.Number),
                        Missing = w.Rows.Max(x => x.Number) == null,
                        Fallback = w.Rows.Min(x => x.Number) ?? "none",
                    }
            )
        );
    }

    // MIN and MAX of a string column over a frame without the current row are NULL
    // for the first row, which the translator takes for non-nullable: observed
    // '#' || MAX(...) is NULL and MIN(...) <> 'A-001' is NULL, where .NET gives "#"
    // and true.
    [Fact(
        Skip = "#128: Min/Max of strings over a maybe-empty frame is taken as non-null"
    )]
    public void ComposesTheExtremeStringOfAFrameThatMayBeEmpty()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders,
                null,
                rows => rows.OrderBy(o => o.Id),
                PostgreSqlFrame.Rows(-1, -1),
                (o, w) =>
                    new
                    {
                        o.Id,
                        Label = "#" + w.Rows.Max(x => x.Number),
                        Differs = w.Rows.Min(x => x.Number) != "A-001",
                    }
            )
        );
    }

    [Fact]
    public void AggregatesTheGroupsOfAFrame()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders.GroupBy(o => o.CustomerId),
                null,
                rows => rows.OrderBy(g => g.Key),
                PostgreSqlFrame.Rows(-1, 0),
                (g, w) =>
                    new
                    {
                        g.Key,
                        Quantities = w.Rows.Sum(x => x.Sum(o => o.Quantity)),
                        Largest = w.Rows.Max(x => x.Count()),
                        Shared = w.Rows.Any(x => x.Count() > 1),
                        Keys = w.Rows.Count(x => x.Key > 2),
                    }
            )
        );
    }

    // ERROR: aggregate functions are not allowed in FILTER, from
    // COUNT(*) FILTER (WHERE CAST(COUNT(*) AS integer) > 1) OVER (...) ... GROUP BY.
    [Fact(Skip = "#128: a frame filter on a group aggregate puts an aggregate in FILTER")]
    public void FiltersTheGroupsOfAFrameByTheirAggregates()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders.GroupBy(o => o.CustomerId),
                null,
                rows => rows.OrderBy(g => g.Key),
                PostgreSqlFrame.Rows(-1, 0),
                (g, w) =>
                    new
                    {
                        g.Key,
                        Shared = w.Rows.Count(x => x.Count() > 1),
                        Keys = w.Rows.Where(x => x.Count() > 1).Sum(x => x.Key),
                    }
            )
        );
    }

    [Fact]
    public void AggregatesEveryRowBesideTheFrameOfAFilteredSource()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders.Where(o => o.IsPaid),
                null,
                rows => rows.OrderBy(o => o.Id),
                PostgreSqlFrame.Rows(-1, 0),
                (o, w) =>
                    new
                    {
                        o.Id,
                        Paid = Orders.Where(x => x.IsPaid).Count(),
                        All = Orders.Count(),
                        Large = Orders.Where(x => x.IsPaid).Count(x => x.Quantity > 2),
                        Total = Orders.Where(x => x.IsPaid).Sum(x => x.Total),
                        Recent = w.Rows.Count(),
                    }
            )
        );
    }

    [Fact]
    public void AggregatesAFrameOverAWindowedSource()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders.Select(o => new
                {
                    o.Id,
                    o.Quantity,
                    All = Orders.Sum(x => x.Quantity),
                }),
                null,
                rows => rows.OrderBy(x => x.Id),
                PostgreSqlFrame.Rows(-1, 0),
                (x, w) =>
                    new
                    {
                        x.Id,
                        Alls = w.Rows.Sum(y => y.All),
                        Quantities = w.Rows.Sum(y => y.Quantity),
                    }
            )
        );
    }

    [Fact]
    public void FiltersAndAggregatesTheResultOfAFrame()
    {
        IQueryable<RunningQuantity> running = Trees.Over(
            Orders,
            null,
            rows => rows.OrderBy(o => o.Id),
            PostgreSqlFrame.Rows(-1, 1),
            (o, w) =>
                new RunningQuantity { Id = o.Id, Running = w.Rows.Sum(x => x.Quantity) }
        );

        database.AssertSameRows(running.Where(r => r.Running > 8).Select(r => r.Id));
        database.AssertSameResult(running, q => q.Max(r => r.Running));
        database.AssertSameResult(running, q => q.Count(r => r.Running % 2 == 0));
    }

    [Fact]
    public void AggregatesTheFilteredRowsOfAnOverBesideThem()
    {
        IQueryable<RunningQuantity> large = Trees
            .Over(
                Orders,
                null,
                rows => rows.OrderBy(o => o.Id),
                PostgreSqlFrame.Rows(-1, 1),
                (o, w) =>
                    new RunningQuantity
                    {
                        Id = o.Id,
                        Running = w.Rows.Sum(x => x.Quantity),
                    }
            )
            .Where(r => r.Running > 5);

        database.AssertSameRows(
            large.Select(r => new
            {
                r.Id,
                Count = large.Count(),
                Top = large.Max(x => x.Running),
            })
        );
    }

    // ERROR: window function calls cannot be nested, from
    // MAX(CAST(COALESCE(SUM(t0."Quantity") OVER (...), 0) AS integer)) OVER ().
    [Fact(
        Skip = "#128: an aggregate over an Over's rows in its projection nests windows"
    )]
    public void AggregatesTheRowsOfAnOverBesideThem()
    {
        IQueryable<RunningQuantity> running = Trees.Over(
            Orders,
            null,
            rows => rows.OrderBy(o => o.Id),
            PostgreSqlFrame.Rows(null, 0),
            (o, w) =>
                new RunningQuantity { Id = o.Id, Running = w.Rows.Sum(x => x.Quantity) }
        );

        database.AssertSameRows(
            running.Select(r => new
            {
                r.Id,
                Top = running.Max(x => x.Running),
                Large = running.Count(x => x.Running > 20),
            })
        );
    }
}

public sealed record RunningQuantity
{
    public int Id { get; init; }

    public int Running { get; init; }
}
