using System.Text;

namespace Linq.Expressions.Translation.PostgreSql.ExecutionTests;

// Max and Min of a few values are GREATEST and LEAST, which skip NULLs and are NULL
// when every value is, as Max and Min of nullable values are; the values of the types
// they take order as in .NET, also negative, large, fractional, mixed with
// conversions and composed with the operators that read them. Normalize is NORMALIZE
// in the same form.
[Collection(SharedDatabase.Name)]
public sealed class ArrayExtremeSemanticsTests(Database database)
{
    [Fact]
    public void TakesExtremesOfNumbers()
    {
        database.AssertSameRows(
            Orders.Select(o => new
            {
                o.Id,
                Negative = new[] { o.Quantity - 10, -o.Id, -3 }.Max(),
                Widened = new[] { o.Points, o.Quantity, int.MinValue }.Max(),
                Large = new[] { o.Points, long.MaxValue - o.Id }.Min(),
                Smallest = new[] { o.Points, long.MinValue + o.Id }.Min(),
                Single = new[] { o.Quantity }.Max(),
                Many = new[]
                {
                    1,
                    o.Id,
                    2,
                    o.Quantity,
                    3,
                    4,
                    5,
                    6,
                    7,
                    o.Id * 2,
                    -1,
                }.Max(),
                Fraction = new[] { o.Total, o.Total / 3, -o.Total }.Max(),
                Scaled = new[] { o.Total, 1.50m, o.Discount ?? 0.005m }.Min(),
                Mixed = new[] { o.Total, o.Points, o.Quantity }.Max(),
            })
        );
    }

    [Fact]
    public void SkipsNulls()
    {
        database.AssertSameRows(
            Orders.Select(o => new
            {
                o.Id,
                AllNull = new[] { o.Priority, o.Priority }.Max(),
                OneNull = new[] { o.Priority, o.Priority + 1, null }.Min(),
                Discount = new[] { o.Discount, o.Discount * 2 }.Max(),
                Gift = new[] { o.IsGift, null }.Max(),
                GiftMin = new[] { o.IsGift, o.IsGift == true }.Min(),
                Shipped = new[] { o.ShippedOn, null, o.ShippedOn }.Min(),
                Paid = new[] { o.PaidAt, null }.Max(),
                Points = new[] { (long?)o.Priority, o.Points }.Min(),
            })
        );
    }

    [Fact]
    public void TakesExtremesOfFlags()
    {
        database.AssertSameRows(
            Orders.Select(o => new
            {
                o.Id,
                Any = new[] { o.IsPaid, o.IsGift ?? false, false }.Max(),
                All = new[] { o.IsPaid, o.IsGift ?? true }.Min(),
                Status = new[] { o.Status, OrderStatus.Shipped }.Max(),
                Least = new[] { o.Status, OrderStatus.Paid }.Min(),
                Options = new[] { o.Options, ShippingOptions.Fragile }.Max(),
            })
        );
    }

    [Fact]
    public void TakesExtremesOfDatesAndTimes()
    {
        database.AssertSameRows(
            Orders.Select(o => new
            {
                o.Id,
                Created = new[]
                {
                    o.CreatedAt,
                    new DateTime(2024, 1, 15, 10, 30, 0),
                }.Max(),
                Before = new[] { o.CreatedAt, o.CreatedAt.AddTicks(-10) }.Min(),
                Day = new[] { o.ShippedOn, DateOnly.FromDateTime(o.CreatedAt) }.Max(),
                Time = new[] { o.DeliveryTime, new TimeOnly(23, 59, 59, 999, 999) }.Max(),
                Midnight = new[] { o.DeliveryTime, TimeOnly.MinValue }.Min(),
                Instant = new[]
                {
                    o.PaidAt,
                    new DateTimeOffset(2024, 1, 15, 12, 0, 0, TimeSpan.FromHours(3)),
                }.Max(),
                Converted = new DateTimeOffset?[] { o.PaidAt, o.CreatedAt }.Max(),
            })
        );
    }

    [Fact]
    public void TakesExtremesOfDurations()
    {
        database.AssertSameRows(
            MappedTables.OrderEvents.Select(e => new
            {
                e.Id,
                Elapsed = new[] { e.Elapsed, TimeSpan.Zero }.Max(),
                Negative = new[] { e.Elapsed, TimeSpan.FromMinutes(-1) }.Min(),
                Timeout = new[] { e.Timeout, e.Elapsed }.Min(),
                Longest = new[] { e.Timeout, null }.Max(),
                Day = new[] { e.Elapsed, TimeSpan.FromHours(24) }.Max(),
            })
        );
    }

    [Fact]
    public void TakesExtremesOfShorts()
    {
        database.AssertSameRows(
            MappedTables.OrderEvents.Select(e => new
            {
                e.Id,
                Most = new[] { e.Severity, (short)2 }.Max(),
                Least = new[] { e.Severity, (short)-1, (short)(e.Severity * 2) }.Min(),
            })
        );
    }

    [Fact]
    public void FiltersOrdersAndGroupsOnExtremes()
    {
        database.AssertSameSequence(
            Orders
                .Where(o => new[] { o.Quantity, o.Priority ?? 0 }.Min() >= 1)
                .OrderByDescending(o => new[] { o.Points, o.Quantity * 100L }.Max())
                .ThenBy(o => o.Id)
                .Select(o => new
                {
                    o.Id,
                    Top = new[] { o.Points, o.Quantity * 100L }.Max(),
                })
        );
        database.AssertSameRows(
            Orders
                .GroupBy(o => new[] { o.Quantity, 3 }.Max())
                .Select(g => new
                {
                    g.Key,
                    Count = g.Count(),
                    Sum = g.Sum(o => new[] { o.Id, o.Quantity }.Min()),
                    Most = new[] { g.Max(o => o.Points), g.Count() }.Max(),
                })
        );
    }

    [Fact]
    public void AggregatesExtremes()
    {
        database.AssertSameResult(
            Orders,
            q => q.Max(o => new[] { o.Quantity, o.Id }.Min())
        );
        database.AssertSameResult(
            Orders,
            q => q.Sum(o => new[] { o.Priority, o.Quantity }.Max())
        );
        database.AssertSameResult(
            Orders.Where(o => o.Id > 100),
            q => q.Max(o => new[] { o.Priority, null }.Max())
        );
    }

    [Fact]
    public void ComposesExtremes()
    {
        database.AssertSameRows(
            Orders.Select(o => new
            {
                o.Id,
                Clamped = new[] { new[] { o.Quantity, 2 }.Max(), 5 }.Min(),
                Counted = new[]
                {
                    o.Quantity,
                    Orders.Count(x => x.CustomerId == o.CustomerId),
                }.Max(),
                Arithmetic = new[] { o.Quantity, o.Id }.Max()
                    - new[] { o.Quantity, o.Id }.Min(),
                Conditional = o.IsPaid
                    ? new[] { o.Id, 7 }.Max()
                    : new[] { o.Id, 7 }.Min(),
            })
        );
    }

    [Fact]
    public void JoinsOnExtremes()
    {
        database.AssertSameRows(
            Orders.Join(
                Customers,
                o => new[] { o.CustomerId, 2 }.Max(),
                c => c.Id,
                (o, c) => new { o.Id, c.Name }
            )
        );
        database.AssertSameRows(
            Orders
                .Select(o => new[] { o.Quantity, 4 }.Min())
                .Distinct()
                .Union(Orders.Select(o => new[] { o.Id, 11 }.Max()))
        );
    }

    [Fact]
    public void TakesOrdinalExtremesOfText()
    {
        database.AssertSameRows(
            Orders.Select(o => new
            {
                o.Id,
                Most = new[] { o.Number, o.Note, "B-001" }.Max(StringComparer.Ordinal),
                Least = new[] { o.Note, o.Number.ToLower() }.Min(StringComparer.Ordinal),
                Empty = new[] { o.Note, string.Empty }.Min(StringComparer.Ordinal),
                AllNull = new[] { o.Note, null }.Min(StringComparer.Ordinal),
                Case = new[] { o.Number, "a" }.Max(StringComparer.Ordinal),
                Accent = new[] { o.Note, "é" }.Max(StringComparer.Ordinal),
            })
        );
    }

    // StringComparer.Ordinal compares UTF-16 code units: a character outside the
    // Basic Multilingual Plane is a surrogate pair from U+D800, which orders before
    // U+E000..U+FFFF, where COLLATE "C" compares code points.
    // Observed: GREATEST('😀', U+FFFD) is '😀' where .NET's Max is U+FFFD.
    [Fact(
        Skip = "#133: Max/Min with StringComparer.Ordinal order surrogate pairs after U+E000..U+FFFF (code point, not UTF-16 order)"
    )]
    public void TakesOrdinalExtremesOfSurrogatePairs()
    {
        database.AssertSameRows(
            Orders
                .Where(o => o.Note != null)
                .Select(o => new
                {
                    o.Id,
                    Most = new[] { o.Note, "�" }.Max(StringComparer.Ordinal),
                    Least = new[] { o.Note, "！" }.Min(StringComparer.Ordinal),
                })
        );
    }

    // The order of an ordinal extreme is the ordinal order of the value.
    [Fact]
    public void OrdersByOrdinalExtremes()
    {
        database.AssertSameSequence(
            Orders
                .OrderBy(
                    o => new[] { o.Number, o.Note }.Max(StringComparer.Ordinal),
                    StringComparer.Ordinal
                )
                .ThenBy(o => o.Id)
                .Select(o => new
                {
                    o.Id,
                    Top = new[] { o.Number, o.Note }.Max(StringComparer.Ordinal),
                })
        );
    }

    // new int[n] is n zeros, whose Max is 0, and new int?[n] n nulls, whose Max is
    // null. Observed: GREATEST(t0."Id"), the length taken for the only element.
    [Fact(
        Skip = "#128: Max/Min of new T[n] is GREATEST(n)/LEAST(n), the array's length read as its element"
    )]
    public void TakesExtremesOfArraysOfDefaults()
    {
        database.AssertSameRows(
            Orders.Select(o => new
            {
                o.Id,
                Zero = new int[o.Id].Max(),
                Null = new int?[o.Id].Min(),
            })
        );
    }

    [Fact]
    public void NormalizesComputedText()
    {
        database.AssertSameRows(
            Orders.Select(o => new
            {
                o.Id,
                Composed = (o.Number + "ẹ́").Normalize(),
                Decomposed = (o.Number + "éṣ").Normalize(NormalizationForm.FormD),
                Ligature = (o.Number + "ﬁ①").Normalize(NormalizationForm.FormKC),
                Compatible = (o.Number + "Å½").Normalize(NormalizationForm.FormKD),
                Hangul = (o.Number + "각").Normalize(NormalizationForm.FormC),
                Ordered = (o.Number + "ậ").Normalize(NormalizationForm.FormD),
                (o.Number + "é").Normalize(NormalizationForm.FormD).Length,
            })
        );
    }

    [Fact]
    public void ComparesNormalizedText()
    {
        NormalizationForm form = NormalizationForm.FormKD;

        database.AssertSameRows(
            Customers
                .Where(c => (c.Name + "́").Normalize() != c.Name + "́")
                .Select(c => new
                {
                    c.Id,
                    Same = c.Name.Normalize(form) == c.Name,
                    Contains = c.Name.Normalize(NormalizationForm.FormD).Contains("̈"),
                })
        );
        database.AssertSameRows(
            Customers
                .GroupBy(c => c.Name.Normalize(NormalizationForm.FormKC))
                .Select(g => new { g.Key, Count = g.Count() })
        );
    }
}
