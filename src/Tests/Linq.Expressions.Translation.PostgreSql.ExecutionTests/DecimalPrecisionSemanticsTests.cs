#pragma warning disable CA1861 // the constant arrays are the sources under test

namespace Linq.Expressions.Translation.PostgreSql.ExecutionTests;

// System.Decimal against numeric: whole decimal constants, a null digits argument,
// and the places of quotients and averages, which .NET keeps to the 96 bits of a
// decimal and the translation computes to. Sums and products are exact in numeric,
// which has neither the 96 bits nor the range of a decimal: where a decimal rounds
// or overflows, PostgreSQL goes on with the exact value. Fitting them to a decimal
// would wrap every SUM and every product of decimals into the rounding a quotient
// needs, and an overflow inside a sum depends on the order of its rows, which SQL
// does not have; so they are left exact, and the tests say what that gives.
// Decimals compare to 10 places in Canonical, so a difference past them is compared
// in the query against the exact .NET value.
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
    [Fact]
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
    [Fact]
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
    [Fact]
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

    // Math.Round(decimal.MaxValue / 10^28) * 10^28 is 8 * 10^28, past the range of
    // decimal: .NET throws, numeric goes on, and divided by 10 the result is back
    // in range.
    [Fact]
    public void MultipliesPastTheRangeOfDecimalExactly()
    {
        IQueryable<decimal> query = new[] { decimal.MaxValue, 1m }
            .AsQueryable()
            .Select(x => Math.Round(x / Step) * Step / 10m);

        AssertOverflowsInMemory(query);
        Assert.Equal([0m, Step / 10m * 8m], Decimals(query.Expression));
    }

    // decimal.MaxValue + decimal.MaxValue overflows in .NET before -decimal.MaxValue
    // is added; the sum of the three is decimal.MaxValue whatever their order, which
    // is what SUM gives.
    [Fact]
    public void SumsPastTheRangeOfDecimalExactly()
    {
        IQueryable<decimal> rows = Rows([
            decimal.MaxValue,
            decimal.MaxValue,
            -decimal.MaxValue,
        ]);
        Expression executed = rows.Executed(q => q.Sum());

        IQueryProvider provider = new EnumerableQuery<decimal>([]);
        _ = Assert.Throws<OverflowException>(() =>
            provider.Execute<decimal>(InMemory.Rewrite(executed))
        );
        Assert.Equal([decimal.MaxValue], Decimals(executed));
    }

    // The same in a group.
    [Fact]
    public void SumsAGroupPastTheRangeOfDecimalExactly()
    {
        (int Key, decimal Value)[] rows =
        [
            (1, decimal.MaxValue),
            (1, decimal.MaxValue),
            (1, -decimal.MaxValue),
        ];
        IQueryable<decimal> query = rows.AsQueryable()
            .GroupBy(r => r.Key)
            .Select(g => g.Sum(r => r.Value));

        AssertOverflowsInMemory(query);
        Assert.Equal([decimal.MaxValue], Decimals(query.Expression));
    }

    // The running sum of a window reaches 2 * decimal.MaxValue - 1 on the second
    // row, where .NET throws; divided by 4 it is back in range, a quarter and two
    // halves of decimal.MaxValue rounded to its 29 digits.
    [Fact]
    public void SumsAWindowPastTheRangeOfDecimalExactly()
    {
        IQueryable<decimal> query = Trees.Over(
            new[] { decimal.MaxValue, decimal.MaxValue - 1m, 1m }.AsQueryable(),
            null,
            rows => rows.OrderDescending(),
            PostgreSqlFrame.Rows(null, 0),
            (v, w) => w.Rows.Sum(r => r) / 4m
        );

        AssertOverflowsInMemory(query);
        Assert.Equal(
            [
                19807040628566084398385987584m,
                39614081257132168796771975167m,
                39614081257132168796771975168m,
            ],
            Decimals(query.Expression)
        );
    }

    // 9 + 0.0000000000000000000000000001 has 29 significant digits, which .NET
    // rounds to 9; numeric keeps 9.0000000000000000000000000003, which is not 9.
    [Fact]
    public void SumsAGroupExactlyWhereADecimalRounds()
    {
        (int Key, decimal Value)[] rows = [(1, 9m), (1, Tiny), (1, Tiny), (1, Tiny)];
        IQueryable<bool> query = rows.AsQueryable()
            .GroupBy(r => r.Key)
            .Select(g => g.Sum(r => r.Value) == 9m);

        Assert.Equal([true], new EnumerableQuery<bool>(query.Expression).ToList());
        Assert.Equal([false], Values<bool>(query.Expression));
    }

    // The same over the rows of a window.
    [Fact]
    public void SumsAWindowExactlyWhereADecimalRounds()
    {
        IQueryable<bool> query = Trees.Over(
            new[] { 9m, Tiny, Tiny, Tiny }.AsQueryable(),
            null,
            null,
            null,
            (v, w) => w.Rows.Sum(r => r) == 9m
        );

        Assert.Equal(
            [true, true, true, true],
            new EnumerableQuery<bool>(InMemory.Rewrite(query.Expression)).ToList()
        );
        Assert.Equal([false, false, false, false], Values<bool>(query.Expression));
    }

    // #144: 250.0000000000000000000000001 / 100 is 2.500000000000000000000000001 in
    // .NET, which rounds to 3; numeric keeps the dividend's 25 places, 2.5, which
    // rounds to 2.
    [Fact]
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
    [Fact]
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

    // .NET rounds 0.111111111111111 squared to 28 places; numeric keeps all 30, which
    // is not that value (and cannot be read as a decimal). A product of fewer places
    // is the same in both.
    [Fact]
    public void MultipliesDecimalsExactlyPast28Places()
    {
        IQueryable<bool> query = new[] { 0.111111111111111m, 0.5m }
            .AsQueryable()
            .OrderBy(x => x)
            .Select(x => x * x == (x < 0.2m ? 0.0123456790123456543209876543m : 0.25m));

        Assert.Equal([true, true], new EnumerableQuery<bool>(query.Expression).ToList());
        Assert.Equal([false, true], Values<bool>(query.Expression));
    }

    // #145: the mean of 1, 0, 0 is 0.3333333333333333333333333333 in .NET and
    // 0.33333333333333333333 for AVG(numeric).
    [Fact]
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
    [Fact]
    public void AveragesLongsAsDecimalsTo28Digits()
    {
        database.AssertSameResult(
            Rows([long.MaxValue, 1L, -1L]),
            q => q.Average(l => (decimal?)l)
        );
    }

    // #145: the mean of 1, 2 and 2 over all rows of a window is
    // 1.6666666666666666666666666667 in .NET.
    [Fact]
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
    [Fact]
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

    private static void AssertOverflowsInMemory<T>(IQueryable<T> query)
    {
        _ = Assert.Throws<OverflowException>(() =>
            new EnumerableQuery<T>(InMemory.Rewrite(query.Expression)).ToList()
        );
    }

    // The single column of the statement, sorted.
    private List<decimal> Decimals(Expression expression)
    {
        return [.. Values<decimal>(expression).Order()];
    }

    private List<T> Values<T>(Expression expression)
    {
        ResultSet result = database.Query(new PostgreSqlQuery(expression).TextValue);
        return
        [
            .. result.Rows.Select(row =>
                (T)Materializer.Element(typeof(T), result, row)!
            ),
        ];
    }
}
