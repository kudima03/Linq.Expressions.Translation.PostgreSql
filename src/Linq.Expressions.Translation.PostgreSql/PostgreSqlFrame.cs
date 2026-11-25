using System.Globalization;

namespace Linq.Expressions.Translation.PostgreSql;

// The frame clause of a window, as PostgreSQL writes it: the rows of the ordered
// partition of the current row that its aggregates and FIRST_VALUE, LAST_VALUE and
// NTH_VALUE read. Each bound is an offset from the current row: a negative one is
// PRECEDING, a positive one FOLLOWING, zero is CURRENT ROW and null is UNBOUNDED on
// its side (PRECEDING for the start, FOLLOWING for the end). ROWS counts rows, GROUPS
// counts groups of peers (rows equal on every ORDER BY key) and RANGE measures the
// distance of the one ORDER BY key from the current row's: a number, a duration
// (elapsed time, written in seconds) or a number of calendar months.
public sealed record PostgreSqlFrame
{
    private PostgreSqlFrame(string textValue, FrameKind kind, bool includesCurrentRow)
    {
        TextValue = textValue;
        Kind = kind;
        IncludesCurrentRow = includesCurrentRow;
    }

    // The frame clause, e.g. ROWS BETWEEN 3 PRECEDING AND CURRENT ROW.
    public string TextValue { get; }

    internal FrameKind Kind { get; }

    // Whether the current row is always in its frame, which is never empty then.
    internal bool IncludesCurrentRow { get; }

    public static PostgreSqlFrame Rows(long? start, long? end)
    {
        return Framed("ROWS", start, end, Number, FrameKind.Rows);
    }

    public static PostgreSqlFrame Groups(long? start, long? end)
    {
        return Framed("GROUPS", start, end, Number, FrameKind.Groups);
    }

    public static PostgreSqlFrame Range(decimal? start, decimal? end)
    {
        return Framed("RANGE", start, end, Number, FrameKind.NumericRange);
    }

    public static PostgreSqlFrame Range(TimeSpan? start, TimeSpan? end)
    {
        return Framed(
            "RANGE",
            Ticks(start, nameof(start)),
            Ticks(end, nameof(end)),
            ticks => $"INTERVAL '{Number(ticks / TimeSpan.TicksPerSecond)} seconds'",
            FrameKind.IntervalRange
        );
    }

    public static PostgreSqlFrame RangeMonths(int? start, int? end)
    {
        return Framed(
            "RANGE",
            start,
            end,
            months => $"INTERVAL '{Number(months)} months'",
            FrameKind.IntervalRange
        );
    }

    private static string Number(decimal value)
    {
        return value.ToString(
            "0.############################",
            CultureInfo.InvariantCulture
        );
    }

    // A duration is a whole number of microseconds, PostgreSQL's finest unit.
    private static decimal? Ticks(TimeSpan? offset, string side)
    {
        return offset is TimeSpan span && span.Ticks % 10 != 0
            ? throw new ArgumentException(
                "PostgreSQL intervals are limited to microsecond precision.",
                side
            )
            : offset?.Ticks;
    }

    private static PostgreSqlFrame Framed(
        string unit,
        decimal? start,
        decimal? end,
        Func<decimal, string> distance,
        FrameKind kind
    )
    {
        int first = Side(start, -2);
        int last = Side(end, 2);
        return first > last
            ? throw new ArgumentException(
                "The start of a frame cannot lie on a later side of the current row than its end.",
                nameof(start)
            )
            : new PostgreSqlFrame(
                $"{unit} BETWEEN {Bound(start, first, distance)} AND {Bound(end, last, distance)}",
                kind is FrameKind.Rows or FrameKind.Groups
                || Math.Abs(first) == 1
                || Math.Abs(last) == 1
                    ? kind
                    : FrameKind.Range,
                first <= 0 && last >= 0
            );
    }

    // -2 and 2 are UNBOUNDED, -1 PRECEDING, 0 CURRENT ROW and 1 FOLLOWING.
    private static int Side(decimal? offset, int unbounded)
    {
        return offset is decimal value ? Math.Sign(value) : unbounded;
    }

    private static string Bound(decimal? offset, int side, Func<decimal, string> distance)
    {
        return side switch
        {
            -2 => "UNBOUNDED PRECEDING",
            2 => "UNBOUNDED FOLLOWING",
            0 => "CURRENT ROW",
            _ => distance(Math.Abs(offset!.Value))
                + (side < 0 ? " PRECEDING" : " FOLLOWING"),
        };
    }
}
