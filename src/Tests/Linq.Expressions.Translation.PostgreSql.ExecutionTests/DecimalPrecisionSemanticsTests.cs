#pragma warning disable CA1861 // the constant arrays are the sources under test

using Xunit.Sdk;

namespace Linq.Expressions.Translation.PostgreSql.ExecutionTests;

// System.Decimal against numeric: whole decimal constants, a null digits argument,
// the range of decimal (an overflow fails), rounding of sums to 28 significant
// digits, and the scale of quotients, products and averages, which .NET keeps to 28
// significant digits. Decimals compare to 10 places in Canonical, so a difference
// past them is compared in the query against the exact .NET value.
[Collection(SharedDatabase.Name)]
public sealed class DecimalPrecisionSemanticsTests(Database database)
{
    private const decimal Third = 0.3333333333333333333333333333m;

    private const decimal Step = 10000000000000000000000000000m;

    private const decimal Tiny = 0.0000000000000000000000000001m;

    // The rows of an in-memory sequence behind a source of the capturing provider,
    // so that a terminal operator can be taken of them.
    private static IQueryable<T> Rows<T>(T[] values)
    {
        return Customers.Where(c => c.Id == 1).SelectMany(c => values);
    }

    // #144: CASE WHEN ... THEN 250 ELSE -250 END is an integer, so x / 100 divides
    // integers: -2 and 2 instead of -2.5 and 2.5; rounded half to even, the
    // statement fails (function mod(double precision, integer) does not exist).
    [Fact(Skip = "#144: a whole decimal constant is an integer literal, divided as one")]
    public void DividesWholeDecimalConstantsAsDecimals()
    {
        database.AssertSameRows(
            Orders.Select(o => new
            {
                o.Id,
                Away = Math.Round(
                    (o.IsPaid ? 250m : -250m) / 100m,
                    MidpointRounding.AwayFromZero
                ) * 100m,
            })
        );
        database.AssertSameRows(
            Orders.Select(o => new
            {
                o.Id,
                Even = Math.Round(
                    (o.IsPaid ? 350m : -150m) / 100m,
                    MidpointRounding.ToEven
                ) * 100m,
            })
        );
    }

    // #144: a whole decimal constant projected through a derived table is an integer
    // column.
    [Fact(Skip = "#144: a whole decimal constant is an integer literal, divided as one")]
    public void DividesAProjectedWholeDecimalConstantAsADecimal()
    {
        database.AssertSameRows(
            Orders
                .Select(o => new { o.Id, Hundreds = 250m })
                .Take(3)
                .Select(r => new
                {
                    r.Id,
                    Away = Math.Round(r.Hundreds / 100m, MidpointRounding.AwayFromZero)
                        * 100m,
                    Even = Math.Round(r.Hundreds / 100m, MidpointRounding.ToEven) * 100m,
                })
        );
    }

    // #144: a null digits argument makes the rounded value null. digits.Value fails
    // to evaluate, so it is deferred, and the deferred constant is refused
    // (NotSupportedException: [DeferredConstant].Value).
    [Fact(Skip = "#144: Math.Round with the digits of a null captured int? is refused")]
    public void RoundsToNullDigitsAsNull()
    {
        int? digits = null;

        database.AssertSameRows(
            Orders.Select(o => new
            {
                o.Id,
                Rounded = digits.HasValue
                    ? Math.Round(o.Total, digits.Value)
                    : (decimal?)null,
            })
        );
    }

    // #144: Math.Round(decimal.MaxValue / 10^28) * 10^28 is 8 * 10^28, past the range
    // of decimal: .NET throws, numeric goes on (divided by 10 the result is back in
    // range).
    [Fact(Skip = "#144: decimal arithmetic past decimal.MaxValue does not fail")]
    public void FailsToMultiplyPastTheRangeOfDecimal()
    {
        AssertOverflows(
            new[] { decimal.MaxValue, 1m }
                .AsQueryable()
                .Select(x => Math.Round(x / Step) * Step / 10m)
        );
    }

    // #144: decimal.MaxValue + decimal.MaxValue overflows before -decimal.MaxValue is
    // added.
    [Fact(Skip = "#144: a decimal sum past decimal.MaxValue does not fail")]
    public void FailsToSumPastTheRangeOfDecimal()
    {
        AssertOverflows(
            Rows([decimal.MaxValue, decimal.MaxValue, -decimal.MaxValue]),
            q => q.Sum()
        );
    }

    // #144: the same in a group.
    [Fact(Skip = "#144: a decimal sum past decimal.MaxValue does not fail")]
    public void FailsToSumAGroupPastTheRangeOfDecimal()
    {
        (int Key, decimal Value)[] rows =
        [
            (1, decimal.MaxValue),
            (1, decimal.MaxValue),
            (1, -decimal.MaxValue),
        ];

        AssertOverflows(
            rows.AsQueryable()
                .GroupBy(r => r.Key)
                .Select(g => new { g.Key, Total = g.Sum(r => r.Value) })
        );
    }

    // #144: the running sum of a window reaches 2 * decimal.MaxValue on the second
    // row (divided by 4 it is back in range).
    [Fact(Skip = "#144: a decimal sum past decimal.MaxValue does not fail")]
    public void FailsToSumAWindowPastTheRangeOfDecimal()
    {
        AssertOverflows(
            Trees.Over(
                new[] { decimal.MaxValue, decimal.MaxValue - 1m, 1m }.AsQueryable(),
                null,
                rows => rows.OrderDescending(),
                PostgreSqlFrame.Rows(null, 0),
                (v, w) => new { Value = v, Running = w.Rows.Sum(r => r) / 4m }
            )
        );
    }

    // #144: 9 + 0.0000000000000000000000000001 has 29 significant digits, rounded to
    // 9 by .NET in any order; numeric keeps 9.0000000000000000000000000003.
    [Fact(Skip = "#144: a decimal sum keeps more than 28 significant digits")]
    public void RoundsTheSumOfAGroupTo28SignificantDigits()
    {
        (int Key, decimal Value)[] rows = [(1, 9m), (1, Tiny), (1, Tiny), (1, Tiny)];

        database.AssertSameRows(
            rows.AsQueryable()
                .GroupBy(r => r.Key)
                .Select(g => new { g.Key, Nine = g.Sum(r => r.Value) == 9m })
        );
    }

    // #144: the same over the rows of a window.
    [Fact(Skip = "#144: a decimal sum keeps more than 28 significant digits")]
    public void RoundsTheSumOfAWindowTo28SignificantDigits()
    {
        database.AssertSameRows(
            Trees.Over(
                new[] { 9m, Tiny, Tiny, Tiny }.AsQueryable(),
                null,
                null,
                null,
                (v, w) => new { Value = v, Nine = w.Rows.Sum(r => r) == 9m }
            )
        );
    }

    // #144: 250.0000000000000000000000001 / 100 is 2.500000000000000000000000001 in
    // .NET, which rounds to 3; numeric keeps the dividend's 25 places, 2.5, which
    // rounds to 2.
    [Fact(Skip = "#144: numeric division keeps fewer digits than System.Decimal")]
    public void DividesJustAboveAMidpointExactly()
    {
        database.AssertSameRows(
            new[] { 250.0000000000000000000000001m, -250.0000000000000000000000001m }
                .AsQueryable()
                .Select(x => new
                {
                    Value = x,
                    Even = Math.Round(x / 100m, MidpointRounding.ToEven) * 100m,
                })
        );
    }

    // #145: 1 / 3 is 0.3333333333333333333333333333 in .NET and
    // 0.33333333333333333333 in numeric.
    [Fact(Skip = "#145: numeric division keeps about 20 significant digits, not 28")]
    public void DividesDecimalsTo28Digits()
    {
        (decimal X, decimal Y)[] rows = [(1m, 3m), (2m, 6m), (1m, 4m)];

        database.AssertSameRows(
            rows.AsQueryable()
                .Select(r => new
                {
                    r.X,
                    r.Y,
                    Third = r.X / r.Y == Third,
                })
        );
        database.AssertSameRows(rows.AsQueryable().Where(r => r.X / r.Y == Third));
    }

    // #145: .NET rounds the product to 28 places; numeric keeps all 30, which cannot
    // be read as a decimal.
    [Fact(Skip = "#145: a numeric product keeps every decimal place, past 28")]
    public void MultipliesDecimalsTo28Places()
    {
        database.AssertSameRows(
            new[] { 0.111111111111111m, 0.5m }.AsQueryable().Select(x => x * x)
        );
        database.AssertSameRows(
            new[] { 0.111111111111111m, 0.5m }
                .AsQueryable()
                .Select(x => new
                {
                    Value = x,
                    Exact = x * x == 0.0123456790123456543209876543m,
                })
        );
    }

    // #145: the mean of 1, 0, 0 is 0.3333333333333333333333333333 in .NET and
    // 0.33333333333333333333 for AVG(numeric).
    [Fact(Skip = "#145: AVG of numeric keeps about 20 significant digits, not 28")]
    public void AveragesDecimalsTo28Digits()
    {
        (int Key, decimal Value)[] rows =
        [
            (1, 1m),
            (1, 0m),
            (1, 0m),
            (2, 1m),
            (2, 1m),
            (2, 0m),
        ];

        database.AssertSameRows(
            rows.AsQueryable()
                .GroupBy(r => r.Key)
                .Select(g => new { g.Key, Third = g.Average(r => r.Value) == Third })
        );
        database.AssertSameRows(
            Trees.Over(
                new[] { 1m, 0m, 0m }.AsQueryable(),
                null,
                null,
                null,
                (v, w) => new { Value = v, Third = w.Rows.Average(r => r) == Third }
            )
        );
    }

    // #145: the mean of 2^63 - 1, 1 and -1 as decimals is
    // 3074457345618258602.3333333333 in .NET.
    [Fact(Skip = "#145: the decimal average of longs keeps fewer digits than .NET")]
    public void AveragesLongsAsDecimalsTo28Digits()
    {
        database.AssertSameResult(
            Rows([long.MaxValue, 1L, -1L]),
            q => q.Average(l => (decimal?)l)
        );
    }

    // #145: the mean of 1, 2 and 2 over all rows of a window is
    // 1.6666666666666666666666666667 in .NET.
    [Fact(Skip = "#145: the decimal average of longs keeps fewer digits than .NET")]
    public void AveragesLongsAsDecimalsOverAWindowTo28Digits()
    {
        database.AssertSameRows(
            Trees.Over(
                new[] { 1L, 2L, 2L }.AsQueryable(),
                null,
                null,
                null,
                (v, w) =>
                    new
                    {
                        Value = v,
                        Exact = w.Rows.Average(r => (decimal?)r)
                            == 1.6666666666666666666666666667m,
                    }
            )
        );
    }

    // #145: the mean of group 1 is 1.6666666666666666666666666667 in .NET, below
    // 1.66666666666666667; AVG(numeric) is 1.6666666666666667, above it.
    [Fact(Skip = "#145: the decimal average of longs keeps fewer digits than .NET")]
    public void ComparesTheDecimalAverageOfAGroupTo28Digits()
    {
        (int Key, long Value)[] rows =
        [
            (1, 1),
            (1, 2),
            (1, 2),
            (2, 1),
            (2, 1),
            (2, 2),
            (3, 2),
            (3, 2),
        ];

        database.AssertSameRows(
            rows.AsQueryable()
                .GroupBy(r => r.Key)
                .Where(g => g.Average(r => (decimal?)r.Value) < 1.66666666666666667m)
                .Select(g => g.Key)
        );
    }

    // In memory the query throws OverflowException; in SQL the statement must fail.
    private void AssertOverflows<T>(IQueryable<T> query)
    {
        _ = Assert.Throws<OverflowException>(() =>
            new EnumerableQuery<T>(InMemory.Rewrite(query.Expression)).ToList()
        );
        _ = Assert.Throws<XunitException>(() =>
            database.Query(new PostgreSqlQuery(query.Expression).TextValue)
        );
    }

    private void AssertOverflows<TSource, TResult>(
        IQueryable<TSource> source,
        Func<IQueryable<TSource>, TResult> terminal
    )
    {
        Expression executed = source.Executed(terminal);
        IQueryProvider provider = new EnumerableQuery<TResult>([]);
        _ = Assert.Throws<OverflowException>(() =>
            provider.Execute<TResult>(InMemory.Rewrite(executed))
        );
        _ = Assert.Throws<XunitException>(() =>
            database.Query(new PostgreSqlQuery(executed).TextValue)
        );
    }
}
