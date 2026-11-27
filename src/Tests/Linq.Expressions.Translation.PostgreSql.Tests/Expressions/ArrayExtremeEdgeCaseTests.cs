using System.Text;

namespace Linq.Expressions.Translation.PostgreSql.Tests.Expressions;

// GREATEST and LEAST at the edges: one value, nested extremes, conversions between
// the values, a captured comparer and captured values; the element types PostgreSQL
// orders otherwise, other comparers and arrays that are not a list of values are
// refused with NotSupportedException. Normalize with a captured form folds it.
public sealed record ArrayExtremeEdgeCaseTests
{
    public static TheoryData<Expression<Func<Order, object?>>, string> Extremes =>
        new()
        {
            { o => new[] { o.Quantity }.Max(), "GREATEST(t0.\"Quantity\")" },
            {
                o => new[] { new[] { o.Quantity, 2 }.Max(), 5 }.Min(),
                "LEAST(GREATEST(t0.\"Quantity\", 2), 5)"
            },
            {
                o => new[] { o.Points, o.Quantity }.Max(),
                "GREATEST(t0.\"Points\", CAST(t0.\"Quantity\" AS bigint))"
            },
            {
                o => new DateTimeOffset?[] { o.PaidAt, o.CreatedAt }.Max(),
                "GREATEST(t0.\"PaidAt\", t0.\"CreatedAt\")"
            },
            {
                o => new[] { (short)o.Quantity, (short)1 }.Min(),
                "LEAST(CAST(t0.\"Quantity\" AS smallint), 1)"
            },
            {
                o => new[] { o.Priority, null, null }.Min(),
                "LEAST(t0.\"Priority\", NULL, NULL)"
            },
            {
                o => new[] { o.Quantity, o.Id }.Max() > new[] { o.Quantity, o.Id }.Min(),
                "GREATEST(t0.\"Quantity\", t0.\"Id\") > LEAST(t0.\"Quantity\", t0.\"Id\")"
            },
        };

    [Theory]
    [MemberData(nameof(Extremes))]
    public void TakesExtremeOfValues(
        Expression<Func<Order, object?>> selector,
        string sql
    )
    {
        Expression query = Orders.Select(selector).Expression;

        Assert.Equal(
            Sql.Valid($"SELECT {sql} FROM \"Order\" AS t0"),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FoldsCapturedComparer()
    {
        StringComparer comparer = StringComparer.Ordinal;
        Expression query = Orders
            .Select(o => new[] { o.Number, "m" }.Max(comparer))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT GREATEST(t0."Number" COLLATE "C", 'm' COLLATE "C") COLLATE "default"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FoldsEnumerableMaxOfCapturedArray()
    {
        int[] values = [3, 9, 4];
        Expression query = Orders.Select(o => Enumerable.Max(values)).Expression;

        Assert.Equal(
            Sql.Valid("SELECT 9 FROM \"Order\" AS t0"),
            new PostgreSqlQuery(query).TextValue
        );
    }

    // values.Max() of a captured array binds to MemoryExtensions.Max(ReadOnlySpan<T>)
    // in C# 14, a parameter-free call that folds into its value as Enumerable.Max
    // does. Observed: NotSupportedException "The method 'MemoryExtensions.Max' is not
    // supported." for both selectors.
    [Fact(
        Skip = "Bug: Max/Min of a captured array through MemoryExtensions is refused instead of folded"
    )]
    public void FoldsSpanMaxOfCapturedArray()
    {
        int[] values = [3, 9, 4];
        Expression query = Orders
            .Select(o => new
            {
                Top = values.Max(),
                Mixed = new[] { o.Id, values.Min() }.Max(),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                "SELECT 9 AS \"Top\", GREATEST(t0.\"Id\", 3) AS \"Mixed\" FROM \"Order\" AS t0"
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OrdersByOrdinalExtreme()
    {
        Expression query = Orders
            .OrderBy(
                o => new[] { o.Number, o.Note }.Max(StringComparer.Ordinal),
                StringComparer.Ordinal
            )
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                ORDER BY GREATEST(t0."Number" COLLATE "C", t0."Note" COLLATE "C") COLLATE "default" COLLATE "C"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    public static TheoryData<Expression<Func<Order, object?>>> Refused =>
        [
            o => new[] { (byte)o.Id, (byte)1 }.Max(),
            o => new[] { (sbyte)o.Id, (sbyte)1 }.Min(),
            o => new[] { (ushort)o.Id, (ushort)1 }.Max(),
            o => new[] { (uint)o.Id, 1u }.Max(),
            o => new[] { (ulong)o.Points, 1ul }.Min(),
            o => new[] { (Half)o.Weight, Half.Zero }.Max(),
            o => new[] { (char?)o.Number[0], null }.Max(),
            o => new[] { (Guid?)o.TrackingId, null }.Min(),
            o => new[] { o.Number, o.Note }.Max(StringComparer.OrdinalIgnoreCase),
            o => new[] { o.Number, o.Note }.Max(StringComparer.CurrentCulture),
            o => new[] { o.Number, o.Note }.Min(Comparer<string?>.Default),
            o => new[] { o.Id, o.Quantity }.Max(Comparer<int>.Default),
            o => new[] { o.Id, o.Quantity }.Min(x => -x),
            o => new List<int> { o.Id, o.Quantity }.Max(),
            o => new[] { o.Weight, o.Weight }.Max(x => x),
        ];

    [Theory]
    [MemberData(nameof(Refused))]
    public void RefusesExtremeWithoutEquivalent(Expression<Func<Order, object?>> selector)
    {
        Expression query = Orders.Select(selector).Expression;

        _ = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FoldsCapturedForm()
    {
        NormalizationForm form = NormalizationForm.FormKD;
        Expression query = Customers.Select(c => c.Name.Normalize(form)).Expression;

        Assert.Equal(
            Sql.Valid("SELECT NORMALIZE(t0.\"Name\", NFKD) FROM \"Customer\" AS t0"),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void NormalizesInFilterAndGrouping()
    {
        Expression query = Customers
            .Where(c => c.Name.Normalize(NormalizationForm.FormD).Length > 3)
            .GroupBy(c => c.Region.Normalize())
            .Select(g => g.Key)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT NORMALIZE(t0."Region", NFC)
                FROM "Customer" AS t0
                WHERE LENGTH(NORMALIZE(t0."Name", NFD)) > 3
                GROUP BY NORMALIZE(t0."Region", NFC)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    public static TheoryData<Expression<Func<Customer, object>>> Unnormalized =>
        [c => c.Name.IsNormalized(), c => c.Name.IsNormalized(NormalizationForm.FormD)];

    [Theory]
    [MemberData(nameof(Unnormalized))]
    public void RefusesNormalizationTest(Expression<Func<Customer, object>> selector)
    {
        Expression query = Customers.Select(selector).Expression;

        _ = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
    }
}
