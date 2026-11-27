using System.Reflection;

namespace Linq.Expressions.Translation.PostgreSql.Tests.Markers;

// Edge cases of PostgreSqlFrame as a value (bounds at zero and at the limits of their
// types, durations and months, equal frames made by different factories) and of the
// windows that use it: frames captured or built inline in a subquery, RANGE keys of
// every type, aggregates over the rows of a frame that PostgreSQL cannot compute
// and the window aggregates over every row beside a frame.
public sealed record FrameEdgeCaseTests
{
    [Theory]
    [InlineData(-1L, -1L, "ROWS BETWEEN 1 PRECEDING AND 1 PRECEDING")]
    [InlineData(1L, 1L, "ROWS BETWEEN 1 FOLLOWING AND 1 FOLLOWING")]
    [InlineData(
        null,
        9223372036854775807L,
        "ROWS BETWEEN UNBOUNDED PRECEDING AND 9223372036854775807 FOLLOWING"
    )]
    [InlineData(0L, 0L, "ROWS BETWEEN CURRENT ROW AND CURRENT ROW")]
    public void WritesRowBoundsOnEitherSide(long? start, long? end, string text)
    {
        Assert.Equal(text, PostgreSqlFrame.Rows(start, end).TextValue);
    }

    [Fact]
    public void WritesANegativeZeroAsTheCurrentRow()
    {
        Assert.Equal(
            "RANGE BETWEEN CURRENT ROW AND CURRENT ROW",
            PostgreSqlFrame.Range(-0.0m, -0.000m).TextValue
        );
        Assert.Equal(
            "RANGE BETWEEN 123.45 PRECEDING AND 0.5 FOLLOWING",
            PostgreSqlFrame.Range(-123.4500m, 0.50m).TextValue
        );
        Assert.Equal(
            "RANGE BETWEEN 79228162514264337593543950335 FOLLOWING AND UNBOUNDED FOLLOWING",
            PostgreSqlFrame.Range(decimal.MaxValue, null).TextValue
        );
    }

    [Fact]
    public void WritesDurationsOfEverySizeInSeconds()
    {
        Assert.Equal(
            "RANGE BETWEEN INTERVAL '1.5 seconds' PRECEDING AND INTERVAL '0.25 seconds' PRECEDING",
            PostgreSqlFrame
                .Range(TimeSpan.FromSeconds(-1.5), TimeSpan.FromMilliseconds(-250))
                .TextValue
        );
        Assert.Equal(
            "RANGE BETWEEN INTERVAL '3153600000 seconds' PRECEDING AND INTERVAL '0.00001 seconds' FOLLOWING",
            PostgreSqlFrame
                .Range(TimeSpan.FromDays(-36500), TimeSpan.FromTicks(100))
                .TextValue
        );
        Assert.Equal(
            "RANGE BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING",
            PostgreSqlFrame.Range((TimeSpan?)null, null).TextValue
        );
        Assert.Equal(
            "RANGE BETWEEN CURRENT ROW AND UNBOUNDED FOLLOWING",
            PostgreSqlFrame.Range(TimeSpan.Zero, null).TextValue
        );
    }

    [Fact]
    public void WritesMonthsOnEitherSide()
    {
        Assert.Equal(
            "RANGE BETWEEN INTERVAL '12 months' FOLLOWING AND INTERVAL '2147483647 months' FOLLOWING",
            PostgreSqlFrame.RangeMonths(12, int.MaxValue).TextValue
        );
        Assert.Equal(
            "RANGE BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING",
            PostgreSqlFrame.RangeMonths(null, null).TextValue
        );
        Assert.Equal(
            "RANGE BETWEEN INTERVAL '3 months' PRECEDING AND INTERVAL '1 months' PRECEDING",
            PostgreSqlFrame.RangeMonths(-3, -1).TextValue
        );
    }

    [Fact]
    public void ComparesFramesOfTheSameClauseAsEqual()
    {
        Assert.Equal(PostgreSqlFrame.Range(0, 0), PostgreSqlFrame.RangeMonths(0, 0));
        Assert.Equal(
            PostgreSqlFrame.Range(0, null),
            PostgreSqlFrame.Range(TimeSpan.Zero, null)
        );
        Assert.Equal(
            PostgreSqlFrame.Range(0, 0).GetHashCode(),
            PostgreSqlFrame.Range(TimeSpan.Zero, TimeSpan.Zero).GetHashCode()
        );
        Assert.NotEqual(
            PostgreSqlFrame.Range(-1, 0),
            PostgreSqlFrame.Range(TimeSpan.FromSeconds(-1), TimeSpan.Zero)
        );
        Assert.NotEqual(
            PostgreSqlFrame.Range(TimeSpan.FromDays(-30), TimeSpan.Zero),
            PostgreSqlFrame.RangeMonths(-1, 0)
        );
        Assert.NotEqual(
            PostgreSqlFrame.Rows(null, null),
            PostgreSqlFrame.Range((decimal?)null, null)
        );
    }

    [Fact]
    public void RejectsBoundsOnTheWrongSides()
    {
        _ = Assert.Throws<ArgumentException>(() => PostgreSqlFrame.Range(0.5m, -0.5m));
        _ = Assert.Throws<ArgumentException>(() =>
            PostgreSqlFrame.Range(0.0000000000000000000000000001m, 0m)
        );
        _ = Assert.Throws<ArgumentException>(() =>
            PostgreSqlFrame.Range(
                TimeSpan.FromMicroseconds(1),
                TimeSpan.FromMicroseconds(-1)
            )
        );
        _ = Assert.Throws<ArgumentException>(() => PostgreSqlFrame.RangeMonths(0, -1));
        _ = Assert.Throws<ArgumentException>(() =>
            PostgreSqlFrame.Rows(long.MaxValue, long.MinValue)
        );
    }

    [Fact]
    public void RejectsTheExtremeDurationsFinerThanMicroseconds()
    {
        ArgumentException start = Assert.Throws<ArgumentException>(() =>
            PostgreSqlFrame.Range(TimeSpan.MinValue, null)
        );
        ArgumentException end = Assert.Throws<ArgumentException>(() =>
            PostgreSqlFrame.Range(null, TimeSpan.MaxValue)
        );

        Assert.Equal("start", start.ParamName);
        Assert.Equal("end", end.ParamName);
    }

    [Fact]
    public void ReadsAFrameCapturedInASubquery()
    {
        PostgreSqlFrame frame = PostgreSqlFrame.Rows(-1, 0);
        Expression query = Customers
            .Select(c => new
            {
                c.Id,
                Pair = Orders
                    .Where(o => o.CustomerId == c.Id)
                    .Over(
                        null,
                        rows => rows.OrderBy(o => o.Id),
                        frame,
                        (o, w) => (int?)w.Rows.Sum(x => x.Quantity)
                    )
                    .Max(),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", (
                    SELECT MAX(t2."Value")
                    FROM (
                        SELECT CAST(COALESCE(SUM(t1."Quantity") OVER (ORDER BY t1."Id" ROWS BETWEEN 1 PRECEDING AND CURRENT ROW), 0) AS integer) AS "Value"
                        FROM "Order" AS t1
                        WHERE t1."CustomerId" = t0."Id"
                    ) AS t2
                ) AS "Pair"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ReadsAFrameBuiltInASubqueryFromCapturedOffsets()
    {
        long before = 3;
        decimal after = 1.5m;
        Expression query = Customers
            .Select(c => new
            {
                c.Id,
                Rows = Orders
                    .Where(o => o.CustomerId == c.Id)
                    .Over(
                        null,
                        rows => rows.OrderBy(o => o.Id),
                        PostgreSqlFrame.Rows(-before, before),
                        (o, w) => (int?)w.Rows.Count()
                    )
                    .Max(),
                Range = Orders
                    .Where(o => o.CustomerId == c.Id)
                    .Over(
                        null,
                        rows => rows.OrderBy(o => o.Total),
                        PostgreSqlFrame.Range(null, after),
                        (o, w) => (decimal?)w.Rows.Sum(x => x.Total)
                    )
                    .Min(),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", (
                    SELECT MAX(t2."Value")
                    FROM (
                        SELECT CAST(COUNT(*) OVER (ORDER BY t1."Id" ROWS BETWEEN 3 PRECEDING AND 3 FOLLOWING) AS integer) AS "Value"
                        FROM "Order" AS t1
                        WHERE t1."CustomerId" = t0."Id"
                    ) AS t2
                ) AS "Rows", (
                    SELECT MIN(t4."Value")
                    FROM (
                        SELECT COALESCE(SUM(t3."Total") OVER (ORDER BY t3."Total" RANGE BETWEEN UNBOUNDED PRECEDING AND 1.5 FOLLOWING), 0) AS "Value"
                        FROM "Order" AS t3
                        WHERE t3."CustomerId" = t0."Id"
                    ) AS t4
                ) AS "Range"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ReadsACapturedNullFrameAsTheDefault()
    {
        PostgreSqlFrame? frame = null;
        Expression query = Customers
            .Select(c => new
            {
                c.Id,
                Running = Orders
                    .Where(o => o.CustomerId == c.Id)
                    .Over(
                        null,
                        rows => rows.OrderBy(o => o.Id),
                        frame,
                        (o, w) => (long?)w.Rows.LongCount()
                    )
                    .Max(),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", (
                    SELECT MAX(t2."Value")
                    FROM (
                        SELECT COUNT(*) OVER (ORDER BY t1."Id") AS "Value"
                        FROM "Order" AS t1
                        WHERE t1."CustomerId" = t0."Id"
                    ) AS t2
                ) AS "Running"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FailsAsDotNetForAnInvalidFrameBuiltInASubquery()
    {
        long start = 1;
        Expression query = Customers
            .Select(c =>
                Orders
                    .Where(o => o.CustomerId == c.Id)
                    .Over(
                        null,
                        rows => rows.OrderBy(o => o.Id),
                        PostgreSqlFrame.Rows(start, -start),
                        (o, w) => w.Rows.Count()
                    )
                    .Count()
            )
            .Expression;

        ArgumentException thrown = Assert.Throws<ArgumentException>(() =>
            new PostgreSqlQuery(query).TextValue
        );

        Assert.Equal("start", thrown.ParamName);
    }

    [Fact]
    public void RefusesAFrameBuiltFromTheRowsOfAnEnclosingQuery()
    {
        Expression query = Customers
            .Select(c =>
                Orders
                    .Where(o => o.CustomerId == c.Id)
                    .Over(
                        null,
                        rows => rows.OrderBy(o => o.Id),
                        PostgreSqlFrame.Rows(-c.Id, 0),
                        (o, w) => w.Rows.Count()
                    )
                    .Count()
            )
            .Expression;

        NotSupportedException thrown = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );

        Assert.Contains("must be a constant", thrown.Message, StringComparison.Ordinal);
    }

    public static TheoryData<
        Expression<Func<IEnumerable<Order>, IOrderedEnumerable<Order>>>,
        PostgreSqlFrame
    > UnrangedKeys =>
        new()
        {
            { rows => rows.OrderBy(o => o.IsPaid), PostgreSqlFrame.Range(-1, 0) },
            { rows => rows.OrderBy(o => o.IsGift), PostgreSqlFrame.Range(0, 1) },
            { rows => rows.OrderBy(o => o.TrackingId), PostgreSqlFrame.Range(-1, 1) },
            {
                rows => rows.OrderBy(o => o.Number, StringComparer.Ordinal),
                PostgreSqlFrame.Range(null, -1)
            },
            {
                rows => rows.OrderBy(o => o.Quantity),
                PostgreSqlFrame.Range(TimeSpan.FromSeconds(-1), TimeSpan.Zero)
            },
            {
                rows => rows.OrderBy(o => o.Number),
                PostgreSqlFrame.Range(TimeSpan.FromSeconds(-1), TimeSpan.Zero)
            },
            { rows => rows.OrderBy(o => o.DeliveryTime), PostgreSqlFrame.Range(-1, 1) },
            { rows => rows.OrderBy(o => o.ShippedOn), PostgreSqlFrame.Range(0, 2) },
            { rows => rows.OrderBy(o => o.PaidAt), PostgreSqlFrame.Range(-1.5m, 0) },
            {
                rows => rows.OrderBy(o => o.CreatedAt).ThenBy(o => o.ShippedOn),
                PostgreSqlFrame.RangeMonths(-1, 0)
            },
            {
                rows => rows.OrderBy(o => new ValueTuple<int, int>(o.Quantity, o.Id)),
                PostgreSqlFrame.Range(-1, 1)
            },
            { rows => rows.OrderBy(o => 1), PostgreSqlFrame.Range(-1, 1) },
            { rows => rows.OrderBy(o => o.Points), PostgreSqlFrame.RangeMonths(0, 1) },
        };

    [Theory]
    [MemberData(nameof(UnrangedKeys))]
    public void RefusesAnOffsetRangeOverAKeyItCannotMeasure(
        Expression<Func<IEnumerable<Order>, IOrderedEnumerable<Order>>> orderBy,
        PostgreSqlFrame frame
    )
    {
        Expression query = Trees
            .Over(Orders, null, orderBy, frame, (o, w) => w.Rows.Count())
            .Expression;

        NotSupportedException thrown = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );

        Assert.Contains("exactly one key", thrown.Message, StringComparison.Ordinal);
    }

    public static TheoryData<
        Expression<Func<IEnumerable<Order>, IOrderedEnumerable<Order>>>,
        string
    > UnmeasuredKeys =>
        new()
        {
            { rows => rows.OrderBy(o => o.IsPaid), "t0.\"IsPaid\"" },
            { rows => rows.OrderBy(o => o.TrackingId), "t0.\"TrackingId\"" },
            {
                rows => rows.OrderByDescending(o => o.Number, StringComparer.Ordinal),
                "t0.\"Number\" COLLATE \"C\" DESC"
            },
            {
                rows => rows.OrderBy(o => o.Quantity).ThenBy(o => o.Id),
                "t0.\"Quantity\", t0.\"Id\""
            },
        };

    // A RANGE whose bounds are UNBOUNDED or CURRENT ROW measures no distance, over any
    // key, whatever the factory that made it.
    [Theory]
    [MemberData(nameof(UnmeasuredKeys))]
    public void RangesWithoutOffsetsOverAnyKey(
        Expression<Func<IEnumerable<Order>, IOrderedEnumerable<Order>>> orderBy,
        string key
    )
    {
        Expression numeric = Trees
            .Over(
                Orders,
                null,
                orderBy,
                PostgreSqlFrame.Range(null, 0),
                (o, w) => w.Rows.Count()
            )
            .Expression;
        Expression temporal = Trees
            .Over(
                Orders,
                null,
                orderBy,
                PostgreSqlFrame.Range(TimeSpan.Zero, TimeSpan.Zero),
                (o, w) => w.Rows.Count()
            )
            .Expression;
        Expression months = Trees
            .Over(
                Orders,
                null,
                orderBy,
                PostgreSqlFrame.RangeMonths(0, null),
                (o, w) => w.Rows.Count()
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                $"""
                SELECT CAST(COUNT(*) OVER (ORDER BY {key} RANGE BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) AS integer)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(numeric).TextValue
        );
        Assert.Equal(
            Sql.Valid(
                $"""
                SELECT CAST(COUNT(*) OVER (ORDER BY {key} RANGE BETWEEN CURRENT ROW AND CURRENT ROW) AS integer)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(temporal).TextValue
        );
        Assert.Equal(
            Sql.Valid(
                $"""
                SELECT CAST(COUNT(*) OVER (ORDER BY {key} RANGE BETWEEN CURRENT ROW AND UNBOUNDED FOLLOWING) AS integer)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(months).TextValue
        );
    }

    [Fact]
    public void RangesOverAComputedKeyBesideAConstantOne()
    {
        Expression query = Trees
            .Over(
                Orders,
                null,
                rows => rows.OrderBy(o => 1).ThenByDescending(o => (o.Quantity * 2) + 1),
                PostgreSqlFrame.Range(-1, 1),
                (o, w) => w.Rows.Count()
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COUNT(*) OVER (ORDER BY (t0."Quantity" * 2) + 1 DESC RANGE BETWEEN 1 PRECEDING AND 1 FOLLOWING) AS integer)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RangesOverTheValuesOfAScalarSource()
    {
        Expression query = Trees
            .Over(
                Orders.Select(o => o.Quantity),
                null,
                rows => rows.OrderDescending(),
                PostgreSqlFrame.Range(-1, 1),
                (q, w) => w.Rows.Sum()
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COALESCE(SUM(t0."Quantity") OVER (ORDER BY t0."Quantity" DESC RANGE BETWEEN 1 PRECEDING AND 1 FOLLOWING), 0) AS integer)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RangesOverDurationsAndInstants()
    {
        Expression durations = Trees
            .Over(
                Orders.Select(o => new
                {
                    o.Id,
                    Span = o.DeliveryTime - TimeOnly.MinValue,
                }),
                null,
                rows => rows.OrderBy(x => x.Span),
                PostgreSqlFrame.Range(TimeSpan.FromHours(-1), TimeSpan.Zero),
                (x, w) => w.Rows.Count()
            )
            .Expression;
        Expression instants = Trees
            .Over(
                Orders,
                null,
                rows => rows.OrderByDescending(o => o.PaidAt),
                PostgreSqlFrame.RangeMonths(-1, 1),
                (o, w) => w.Rows.Count()
            )
            .Expression;
        Expression days = Trees
            .Over(
                Orders,
                null,
                rows => rows.OrderBy(o => o.CreatedAt.Date),
                PostgreSqlFrame.Range(TimeSpan.FromDays(-1), TimeSpan.Zero),
                (o, w) => w.Rows.Count()
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COUNT(*) OVER (ORDER BY t0."DeliveryTime" - TIME '00:00:00' RANGE BETWEEN INTERVAL '3600 seconds' PRECEDING AND CURRENT ROW) AS integer)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(durations).TextValue
        );
        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COUNT(*) OVER (ORDER BY t0."PaidAt" DESC NULLS LAST RANGE BETWEEN INTERVAL '1 months' PRECEDING AND INTERVAL '1 months' FOLLOWING) AS integer)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(instants).TextValue
        );
        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COUNT(*) OVER (ORDER BY DATE_TRUNC('day', t0."CreatedAt") RANGE BETWEEN INTERVAL '86400 seconds' PRECEDING AND CURRENT ROW) AS integer)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(days).TextValue
        );
    }

    public static TheoryData<
        Expression<Func<Order, PostgreSqlWindow<Order>, object?>>
    > Refused =>
        [
            (o, w) => w.Rows.Min(x => x.TrackingId),
            (o, w) => w.Rows.Select(x => x.Number).Max(StringComparer.Ordinal),
            (o, w) => w.Rows.First().Id,
            (o, w) => w.Rows.Take(2).Count(),
            (o, w) => w.Rows.Skip(1).Sum(x => x.Total),
            (o, w) => w.Rows.OrderBy(x => x.Id).Count(),
            (o, w) => w.Rows.MaxBy(x => x.Id)!.Id,
            (o, w) => w.Rows.ToList().Count,
            (o, w) => w.Rows.ElementAt(0).Id,
            (o, w) => w.Rows.Aggregate(0, (sum, x) => sum + x.Quantity),
            (o, w) => w.Rows.DistinctBy(x => x.CustomerId).Count(),
            (o, w) => w.Rows.GroupBy(x => x.CustomerId).Count(),
            (o, w) => w.Rows.Sum(x => x.Quantity + o.Quantity),
            (o, w) => w.Rows.Count(x => x.CustomerId == o.CustomerId),
            (o, w) =>
                w.Rows.Sum(x => x.Quantity) + Orders.Count(y => y.Id < w.Rows.Count()),
            (o, w) => w.Rows.Sum(x => w.Rows.Count()),
            (o, w) => w.Rows.Select(x => new { x.Id }),
        ];

    [Theory]
    [MemberData(nameof(Refused))]
    public void RefusesWhatTheRowsOfAFrameCannotAggregate(
        Expression<Func<Order, PostgreSqlWindow<Order>, object?>> selector
    )
    {
        Expression query = Trees
            .Over(
                Orders,
                null,
                rows => rows.OrderBy(o => o.Id),
                PostgreSqlFrame.Rows(-1, 1),
                selector
            )
            .Expression;

        _ = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
    }

    public static TheoryData<
        Expression<Func<IEnumerable<Order>, IOrderedEnumerable<Order>>>,
        PostgreSqlFrame
    > Unanchored =>
        new()
        {
            { rows => rows.OrderBy(o => o.Id), PostgreSqlFrame.Rows(-1, -3) },
            { rows => rows.OrderBy(o => o.Id), PostgreSqlFrame.Rows(3, 1) },
            { rows => rows.OrderBy(o => o.Total), PostgreSqlFrame.Range(0.5m, 1m) },
            { rows => rows.OrderBy(o => o.Total), PostgreSqlFrame.Range(null, -0.5m) },
            {
                rows => rows.OrderBy(o => o.CreatedAt),
                PostgreSqlFrame.Range(TimeSpan.FromDays(1), null)
            },
            {
                rows => rows.OrderBy(o => o.ShippedOn),
                PostgreSqlFrame.RangeMonths(null, -1)
            },
        };

    // A frame that may not hold the current row may be empty, where FIRST_VALUE and
    // LAST_VALUE are NULL, which an int cannot hold.
    [Theory]
    [MemberData(nameof(Unanchored))]
    public void RefusesTheNonNullableValueOfAFrameThatMayBeEmpty(
        Expression<Func<IEnumerable<Order>, IOrderedEnumerable<Order>>> orderBy,
        PostgreSqlFrame frame
    )
    {
        Expression first = Trees
            .Over(Orders, null, orderBy, frame, (o, w) => w.FirstValue(x => x.Id))
            .Expression;
        Expression last = Trees
            .Over(Orders, null, orderBy, frame, (o, w) => w.LastValue(x => x.Quantity))
            .Expression;

        NotSupportedException firstThrown = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(first).TextValue
        );
        NotSupportedException lastThrown = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(last).TextValue
        );

        Assert.Contains("may find no row", firstThrown.Message, StringComparison.Ordinal);
        Assert.Contains("may find no row", lastThrown.Message, StringComparison.Ordinal);
    }

    public static TheoryData<PostgreSqlFrame> Anchored =>
        [
            PostgreSqlFrame.Rows(-3, 0),
            PostgreSqlFrame.Rows(0, 3),
            PostgreSqlFrame.Range(-0.5m, 0.5m),
            PostgreSqlFrame.Range((decimal?)null, null),
            PostgreSqlFrame.Range(0, 0),
        ];

    // A frame that holds the current row always has a first and a last row.
    [Theory]
    [MemberData(nameof(Anchored))]
    public void ReadsTheNonNullableValueOfAFrameThatHoldsTheCurrentRow(
        PostgreSqlFrame frame
    )
    {
        Expression query = Trees
            .Over(
                Orders,
                null,
                rows => rows.OrderBy(o => o.Total),
                frame,
                (o, w) => w.FirstValue(x => x.Id) + w.LastValue(x => x.Id)
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                $"""
                SELECT FIRST_VALUE(t0."Id") OVER (ORDER BY t0."Total" {frame.TextValue}) + LAST_VALUE(t0."Id") OVER (ORDER BY t0."Total" {frame.TextValue})
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AggregatesTheGroupsOfAFrame()
    {
        Expression query = Trees
            .Over(
                Orders.GroupBy(o => o.CustomerId),
                null,
                rows => rows.OrderBy(g => g.Key),
                PostgreSqlFrame.Rows(-1, 0),
                (g, w) =>
                    new
                    {
                        g.Key,
                        Sum = w.Rows.Sum(x => x.Sum(o => o.Quantity)),
                        Any = w.Rows.Any(x => x.Count() > 1),
                    }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId" AS "Key", CAST(COALESCE(SUM(CAST(COALESCE(SUM(t0."Quantity"), 0) AS integer)) OVER (ORDER BY t0."CustomerId" ROWS BETWEEN 1 PRECEDING AND CURRENT ROW), 0) AS integer) AS "Sum", COALESCE(BOOL_OR(CAST(COUNT(*) AS integer) > 1) OVER (ORDER BY t0."CustomerId" ROWS BETWEEN 1 PRECEDING AND CURRENT ROW), FALSE) AS "Any"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AggregatesEveryRowOfAFilteredSourceBesideTheFrame()
    {
        Expression query = Trees
            .Over(
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
                        Recent = w.Rows.Count(),
                    }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", CAST(COUNT(*) OVER () AS integer) AS "Paid", (
                    SELECT CAST(COUNT(*) AS integer)
                    FROM "Order" AS t1
                ) AS "All", CAST(COUNT(*) FILTER (WHERE t0."Quantity" > 2) OVER () AS integer) AS "Large", CAST(COUNT(*) OVER (ORDER BY t0."Id" ROWS BETWEEN 1 PRECEDING AND CURRENT ROW) AS integer) AS "Recent"
                FROM "Order" AS t0
                WHERE t0."IsPaid"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AggregatesEveryRowOfAPagedSourceBesideTheFrame()
    {
        Expression query = Trees
            .Over(
                Orders.OrderBy(o => o.Id).Take(5),
                null,
                rows => rows.OrderBy(o => o.Id),
                PostgreSqlFrame.Rows(-1, 0),
                (o, w) =>
                    new
                    {
                        o.Id,
                        Count = Orders.OrderBy(x => x.Id).Take(5).Count(),
                        Recent = w.Rows.Count(),
                    }
            )
            .Select(r => r.Count - r.Recent)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COUNT(*) OVER () AS integer) - CAST(COUNT(*) OVER (ORDER BY t1."Id" ROWS BETWEEN 1 PRECEDING AND CURRENT ROW) AS integer)
                FROM (
                    SELECT t0."Id", t0."CustomerId", t0."Number", t0."Note", t0."Total", t0."Discount", t0."Quantity", t0."Priority", t0."Points", t0."Weight", t0."IsPaid", t0."IsGift", t0."Status", t0."Options", t0."CreatedAt", t0."PaidAt", t0."ShippedOn", t0."DeliveryTime", t0."TrackingId", t0."Tags"
                    FROM "Order" AS t0
                    ORDER BY t0."Id"
                    LIMIT 5
                ) AS t1
                ORDER BY t1."Id"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComposesAggregatesOfAFrameWithWindowFunctions()
    {
        Expression query = Trees
            .Over(
                Orders,
                null,
                rows => rows.OrderBy(o => o.Id),
                PostgreSqlFrame.Rows(null, 0),
                (o, w) =>
                    new
                    {
                        Zero = w.RowNumber() - w.Rows.LongCount(),
                        Chosen = w.Rows.Count() > 2 ? w.Rows.Max(x => x.Quantity) : 0,
                        Missing = w.Rows.Average(x => x.Priority) > 1,
                    }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT ROW_NUMBER() OVER (ORDER BY t0."Id" ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) - COUNT(*) OVER (ORDER BY t0."Id" ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) AS "Zero", CASE WHEN CAST(COUNT(*) OVER (ORDER BY t0."Id" ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) AS integer) > 2 THEN MAX(t0."Quantity") OVER (ORDER BY t0."Id" ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) ELSE 0 END AS "Chosen", COALESCE(CAST(AVG(t0."Priority") OVER (ORDER BY t0."Id" ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) AS double precision) > 1, FALSE) AS "Missing"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void WrapsTheResultOfAFrameForAFilterAndDistinctValues()
    {
        Expression filtered = Trees
            .Over(
                Orders,
                null,
                rows => rows.OrderBy(o => o.Id),
                PostgreSqlFrame.Rows(-1, -1),
                (o, w) => new { o.Id, Previous = w.Rows.Max(x => x.Number) }
            )
            .Where(r => r.Previous == null)
            .Expression;
        Expression distinct = Trees
            .Over(
                Orders,
                null,
                rows => rows.OrderBy(o => o.Id),
                PostgreSqlFrame.Rows(-1, 0),
                (o, w) => w.Rows.Count()
            )
            .Distinct()
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Id", t1."Previous"
                FROM (
                    SELECT t0."Id", MAX(t0."Number") OVER (ORDER BY t0."Id" ROWS BETWEEN 1 PRECEDING AND 1 PRECEDING) AS "Previous"
                    FROM "Order" AS t0
                ) AS t1
                WHERE t1."Previous" IS NULL
                """
            ),
            new PostgreSqlQuery(filtered).TextValue
        );
        Assert.Equal(
            Sql.Valid(
                """
                SELECT DISTINCT CAST(COUNT(*) OVER (ORDER BY t0."Id" ROWS BETWEEN 1 PRECEDING AND CURRENT ROW) AS integer)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(distinct).TextValue
        );
    }

    [Fact]
    public void TranslatesTheFrameOfAnOverBuiltByReflection()
    {
        MethodInfo over = typeof(PostgreSqlWindows)
            .GetMethod(nameof(PostgreSqlWindows.Over))!
            .MakeGenericMethod(typeof(Order), typeof(int));
        Expression<Func<Order, PostgreSqlWindow<Order>, int>> selector = (o, w) =>
            w.Rows.Count();
        Expression query = Expression.Call(
            over,
            Orders.Expression,
            Expression.Constant(null, typeof(Expression<Func<Order, object?>>)),
            Expression.Constant(
                null,
                typeof(Expression<Func<IEnumerable<Order>, IOrderedEnumerable<Order>>>)
            ),
            Expression.Call(
                typeof(PostgreSqlFrame),
                nameof(PostgreSqlFrame.Rows),
                null,
                Expression.Constant(-2L, typeof(long?)),
                Expression.Constant(null, typeof(long?))
            ),
            Expression.Quote(selector)
        );

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COUNT(*) OVER (ROWS BETWEEN 2 PRECEDING AND UNBOUNDED FOLLOWING) AS integer)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    public static TheoryData<decimal?, decimal?, string> WholeBounds =>
        new()
        {
            { -1.5m, 0.5m, "1 PRECEDING AND 0 FOLLOWING" },
            { 0.2m, 2.5m, "1 FOLLOWING AND 2 FOLLOWING" },
            { -2.5m, -0.2m, "2 PRECEDING AND 1 PRECEDING" },
            { -0.8m, -0.2m, "0 PRECEDING AND 1 PRECEDING" },
            { null, 1.5m, "UNBOUNDED PRECEDING AND 1 FOLLOWING" },
            { -1.5m, null, "1 PRECEDING AND UNBOUNDED FOLLOWING" },
            { -1.5m, 0m, "1 PRECEDING AND CURRENT ROW" },
            { -3m, 2m, "3 PRECEDING AND 2 FOLLOWING" },
            { decimal.MinValue, 0m, "9223372036854775807 PRECEDING AND CURRENT ROW" },
        };

    // #123: PostgreSQL takes an integer offset over an integer key, so a fraction is
    // the next whole offset inside the frame, which holds the same keys, and an
    // offset past a bigint is the largest one.
    [Theory]
    [MemberData(nameof(WholeBounds))]
    public void RangesByWholeOffsetsOverAnIntegerKey(
        decimal? start,
        decimal? end,
        string bounds
    )
    {
        Expression query = Trees
            .Over(
                Orders,
                null,
                rows => rows.OrderBy(o => o.Points),
                PostgreSqlFrame.Range(start, end),
                (o, w) => w.Rows.Count()
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                $"""
                SELECT CAST(COUNT(*) OVER (ORDER BY t0."Points" RANGE BETWEEN {bounds}) AS integer)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    // A fraction is kept over a key that has fractions.
    [Fact]
    public void RangesByAFractionOverADecimalKey()
    {
        Expression query = Trees
            .Over(
                Orders,
                null,
                rows => rows.OrderBy(o => o.Total),
                PostgreSqlFrame.Range(-1.5m, 0.5m),
                (o, w) => w.Rows.Count()
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COUNT(*) OVER (ORDER BY t0."Total" RANGE BETWEEN 1.5 PRECEDING AND 0.5 FOLLOWING) AS integer)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    // More rows than a bigint counts are every row, and the months of an interval
    // end at int.MaxValue.
    [Fact]
    public void WritesTheLargestOffsetsPostgreSqlTakes()
    {
        Assert.Equal(
            "ROWS BETWEEN 9223372036854775807 PRECEDING AND CURRENT ROW",
            PostgreSqlFrame.Rows(long.MinValue, 0).TextValue
        );
        Assert.Equal(
            "RANGE BETWEEN INTERVAL '2147483647 months' PRECEDING AND CURRENT ROW",
            PostgreSqlFrame.RangeMonths(int.MinValue, 0).TextValue
        );
    }
}
