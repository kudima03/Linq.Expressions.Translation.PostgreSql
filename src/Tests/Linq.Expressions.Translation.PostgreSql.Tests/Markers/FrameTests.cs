namespace Linq.Expressions.Translation.PostgreSql.Tests.Markers;

// A PostgreSqlFrame is PostgreSQL's frame clause: each bound an offset from the
// current row (negative PRECEDING, positive FOLLOWING, zero CURRENT ROW, null
// UNBOUNDED), ROWS and GROUPS counted, RANGE a number, a duration in seconds or a
// number of months. A frame whose start lies on a later side than its end, which
// PostgreSQL rejects, cannot be made.
public sealed record FrameTests
{
    [Theory]
    [InlineData(null, null, "ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING")]
    [InlineData(null, 0L, "ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW")]
    [InlineData(null, -1L, "ROWS BETWEEN UNBOUNDED PRECEDING AND 1 PRECEDING")]
    [InlineData(null, 2L, "ROWS BETWEEN UNBOUNDED PRECEDING AND 2 FOLLOWING")]
    [InlineData(-3L, 0L, "ROWS BETWEEN 3 PRECEDING AND CURRENT ROW")]
    [InlineData(-3L, -1L, "ROWS BETWEEN 3 PRECEDING AND 1 PRECEDING")]
    [InlineData(-1L, -3L, "ROWS BETWEEN 1 PRECEDING AND 3 PRECEDING")]
    [InlineData(-1L, 1L, "ROWS BETWEEN 1 PRECEDING AND 1 FOLLOWING")]
    [InlineData(-1L, null, "ROWS BETWEEN 1 PRECEDING AND UNBOUNDED FOLLOWING")]
    [InlineData(0L, 0L, "ROWS BETWEEN CURRENT ROW AND CURRENT ROW")]
    [InlineData(0L, 5L, "ROWS BETWEEN CURRENT ROW AND 5 FOLLOWING")]
    [InlineData(0L, null, "ROWS BETWEEN CURRENT ROW AND UNBOUNDED FOLLOWING")]
    [InlineData(1L, 2L, "ROWS BETWEEN 1 FOLLOWING AND 2 FOLLOWING")]
    [InlineData(2L, 1L, "ROWS BETWEEN 2 FOLLOWING AND 1 FOLLOWING")]
    [InlineData(1L, null, "ROWS BETWEEN 1 FOLLOWING AND UNBOUNDED FOLLOWING")]
    [InlineData(
        -9223372036854775807L,
        9223372036854775807L,
        "ROWS BETWEEN 9223372036854775807 PRECEDING AND 9223372036854775807 FOLLOWING"
    )]
    public void CountsRows(long? start, long? end, string text)
    {
        Assert.Equal(text, PostgreSqlFrame.Rows(start, end).TextValue);
        Assert.Equal(
            text.Replace("ROWS", "GROUPS", StringComparison.Ordinal),
            PostgreSqlFrame.Groups(start, end).TextValue
        );
    }

    [Theory]
    [InlineData(null, 0.0, "RANGE BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW")]
    [InlineData(0.0, null, "RANGE BETWEEN CURRENT ROW AND UNBOUNDED FOLLOWING")]
    [InlineData(-1.0, 1.0, "RANGE BETWEEN 1 PRECEDING AND 1 FOLLOWING")]
    [InlineData(-2.5, 0.0, "RANGE BETWEEN 2.5 PRECEDING AND CURRENT ROW")]
    [InlineData(0.125, 10.0, "RANGE BETWEEN 0.125 FOLLOWING AND 10 FOLLOWING")]
    [InlineData(-0.5, -0.25, "RANGE BETWEEN 0.5 PRECEDING AND 0.25 PRECEDING")]
    public void MeasuresANumericKey(double? start, double? end, string text)
    {
        Assert.Equal(
            text,
            PostgreSqlFrame.Range((decimal?)start, (decimal?)end).TextValue
        );
    }

    [Fact]
    public void WritesDecimalOffsetsWithoutTrailingZeros()
    {
        Assert.Equal(
            "RANGE BETWEEN 1 PRECEDING AND 0.1 FOLLOWING",
            PostgreSqlFrame.Range(-1.000m, 0.1000m).TextValue
        );
        Assert.Equal(
            "RANGE BETWEEN 79228162514264337593543950335 PRECEDING AND 0.0000000000000000000000000001 FOLLOWING",
            PostgreSqlFrame
                .Range(decimal.MinValue, 0.0000000000000000000000000001m)
                .TextValue
        );
    }

    [Fact]
    public void MeasuresDurationsInSeconds()
    {
        Assert.Equal(
            "RANGE BETWEEN INTERVAL '86400 seconds' PRECEDING AND CURRENT ROW",
            PostgreSqlFrame.Range(TimeSpan.FromDays(-1), TimeSpan.Zero).TextValue
        );
        Assert.Equal(
            "RANGE BETWEEN INTERVAL '0.000001 seconds' FOLLOWING AND INTERVAL '5400.5 seconds' FOLLOWING",
            PostgreSqlFrame
                .Range(TimeSpan.FromMicroseconds(1), new TimeSpan(0, 1, 30, 0, 500))
                .TextValue
        );
        Assert.Equal(
            "RANGE BETWEEN UNBOUNDED PRECEDING AND INTERVAL '3600 seconds' PRECEDING",
            PostgreSqlFrame.Range(null, TimeSpan.FromHours(-1)).TextValue
        );
    }

    [Fact]
    public void MeasuresMonths()
    {
        Assert.Equal(
            "RANGE BETWEEN INTERVAL '1 months' PRECEDING AND CURRENT ROW",
            PostgreSqlFrame.RangeMonths(-1, 0).TextValue
        );
        Assert.Equal(
            "RANGE BETWEEN INTERVAL '2147483648 months' PRECEDING AND INTERVAL '12 months' FOLLOWING",
            PostgreSqlFrame.RangeMonths(int.MinValue, 12).TextValue
        );
        Assert.Equal(
            "RANGE BETWEEN CURRENT ROW AND UNBOUNDED FOLLOWING",
            PostgreSqlFrame.RangeMonths(0, null).TextValue
        );
    }

    [Fact]
    public void ComparesFramesByValue()
    {
        Assert.Equal(PostgreSqlFrame.Rows(-1, 1), PostgreSqlFrame.Rows(-1, 1));
        Assert.NotEqual(PostgreSqlFrame.Rows(-1, 1), PostgreSqlFrame.Groups(-1, 1));
        Assert.Equal(PostgreSqlFrame.Range(-1.0m, 0), PostgreSqlFrame.Range(-1m, 0m));
        Assert.Equal(
            "PostgreSqlFrame { TextValue = ROWS BETWEEN CURRENT ROW AND CURRENT ROW }",
            PostgreSqlFrame.Rows(0, 0).ToString()
        );
    }

    [Theory]
    [InlineData(0, -1)]
    [InlineData(1, 0)]
    [InlineData(1, -1)]
    [InlineData(2, -5)]
    public void RejectsAStartOnALaterSideThanTheEnd(int start, int end)
    {
        _ = Assert.Throws<ArgumentException>(() => PostgreSqlFrame.Rows(start, end));
        _ = Assert.Throws<ArgumentException>(() => PostgreSqlFrame.Groups(start, end));
        _ = Assert.Throws<ArgumentException>(() => PostgreSqlFrame.Range(start, end));
        _ = Assert.Throws<ArgumentException>(() =>
            PostgreSqlFrame.Range(TimeSpan.FromSeconds(start), TimeSpan.FromSeconds(end))
        );
        _ = Assert.Throws<ArgumentException>(() =>
            PostgreSqlFrame.RangeMonths(start, end)
        );
    }

    [Fact]
    public void RejectsDurationsFinerThanMicroseconds()
    {
        ArgumentException start = Assert.Throws<ArgumentException>(() =>
            PostgreSqlFrame.Range(TimeSpan.FromTicks(-1), TimeSpan.Zero)
        );
        ArgumentException end = Assert.Throws<ArgumentException>(() =>
            PostgreSqlFrame.Range(TimeSpan.Zero, TimeSpan.FromTicks(15))
        );

        Assert.Equal("start", start.ParamName);
        Assert.Equal("end", end.ParamName);
    }

    public static TheoryData<string, PostgreSqlFrame> Frames =>
        new()
        {
            { "ROWS BETWEEN 2 PRECEDING AND CURRENT ROW", PostgreSqlFrame.Rows(-2, 0) },
            {
                "GROUPS BETWEEN 1 PRECEDING AND 1 FOLLOWING",
                PostgreSqlFrame.Groups(-1, 1)
            },
            {
                "RANGE BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING",
                PostgreSqlFrame.Range((decimal?)null, null)
            },
            { "RANGE BETWEEN 5 PRECEDING AND 5 FOLLOWING", PostgreSqlFrame.Range(-5, 5) },
        };

    [Theory]
    [MemberData(nameof(Frames))]
    public void FramesTheWindow(string text, PostgreSqlFrame frame)
    {
        Expression query = Trees
            .Over(
                Orders,
                o => o.CustomerId,
                rows => rows.OrderBy(o => o.Quantity),
                frame,
                (o, w) => w.Rows.Sum(x => x.Total)
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                $"""
                SELECT COALESCE(SUM(t0."Total") OVER (PARTITION BY t0."CustomerId" ORDER BY t0."Quantity" {text}), 0)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FramesAWindowWithoutPartitionOrOrder()
    {
        Expression query = Trees
            .Over(
                Orders,
                null,
                null,
                PostgreSqlFrame.Rows(null, null),
                (o, w) => w.Rows.Count()
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COUNT(*) OVER (ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING) AS integer)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    public static TheoryData<
        Expression<Func<IEnumerable<Order>, IOrderedEnumerable<Order>>>,
        string
    > RangeKeys =>
        new()
        {
            { rows => rows.OrderBy(o => o.CreatedAt), "t0.\"CreatedAt\"" },
            {
                rows => rows.OrderByDescending(o => o.PaidAt),
                "t0.\"PaidAt\" DESC NULLS LAST"
            },
            { rows => rows.OrderBy(o => o.ShippedOn), "t0.\"ShippedOn\" NULLS FIRST" },
            { rows => rows.OrderBy(o => o.DeliveryTime), "t0.\"DeliveryTime\"" },
        };

    [Theory]
    [MemberData(nameof(RangeKeys))]
    public void RangesOverATemporalKey(
        Expression<Func<IEnumerable<Order>, IOrderedEnumerable<Order>>> orderBy,
        string key
    )
    {
        Expression seconds = Trees
            .Over(
                Orders,
                null,
                orderBy,
                PostgreSqlFrame.Range(TimeSpan.FromMinutes(-30), TimeSpan.Zero),
                (o, w) => w.Rows.Count()
            )
            .Expression;
        Expression months = Trees
            .Over(
                Orders,
                null,
                orderBy,
                PostgreSqlFrame.RangeMonths(-1, 1),
                (o, w) => w.Rows.Count()
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                $"""
                SELECT CAST(COUNT(*) OVER (ORDER BY {key} RANGE BETWEEN INTERVAL '1800 seconds' PRECEDING AND CURRENT ROW) AS integer)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(seconds).TextValue
        );
        Assert.Equal(
            Sql.Valid(
                $"""
                SELECT CAST(COUNT(*) OVER (ORDER BY {key} RANGE BETWEEN INTERVAL '1 months' PRECEDING AND INTERVAL '1 months' FOLLOWING) AS integer)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(months).TextValue
        );
    }

    public static TheoryData<
        Expression<Func<IEnumerable<Order>, IOrderedEnumerable<Order>>>,
        string
    > NumericKeys =>
        new()
        {
            { rows => rows.OrderBy(o => o.Quantity), "t0.\"Quantity\"" },
            { rows => rows.OrderBy(o => o.Points), "t0.\"Points\"" },
            { rows => rows.OrderByDescending(o => o.Total), "t0.\"Total\" DESC" },
            { rows => rows.OrderBy(o => o.Weight), "t0.\"Weight\"" },
            { rows => rows.OrderBy(o => o.Priority), "t0.\"Priority\" NULLS FIRST" },
            { rows => rows.OrderBy(o => o.Status), "t0.\"Status\"" },
        };

    [Theory]
    [MemberData(nameof(NumericKeys))]
    public void RangesOverANumericKey(
        Expression<Func<IEnumerable<Order>, IOrderedEnumerable<Order>>> orderBy,
        string key
    )
    {
        Expression query = Trees
            .Over(
                Orders,
                null,
                orderBy,
                PostgreSqlFrame.Range(-2, 1),
                (o, w) => w.Rows.Count()
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                $"""
                SELECT CAST(COUNT(*) OVER (ORDER BY {key} RANGE BETWEEN 2 PRECEDING AND 1 FOLLOWING) AS integer)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RangesWithoutOffsetOverAnyOrder()
    {
        Expression query = Trees
            .Over(
                Customers,
                null,
                rows =>
                    rows.OrderBy(c => c.Region)
                        .ThenBy(c => c.Name, StringComparer.Ordinal),
                PostgreSqlFrame.Range(0, null),
                (c, w) => w.Rows.Count()
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COUNT(*) OVER (ORDER BY t0."Region", t0."Name" COLLATE "C" RANGE BETWEEN CURRENT ROW AND UNBOUNDED FOLLOWING) AS integer)
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
