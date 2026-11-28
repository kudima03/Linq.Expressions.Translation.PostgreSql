#pragma warning disable CA1861 // the constant arrays are the sources under test

namespace Linq.Expressions.Translation.PostgreSql.ExecutionTests;

// Edge cases of double, float and the double average of long against LINQ to
// Objects: the sign of zero in constants, float arithmetic in single precision, the
// order and comparisons of NaN, and the exact sum behind the average of longs.
[Collection(SharedDatabase.Name)]
public sealed class FloatingPointEdgeCaseSemanticsTests(Database database)
{
    // The rows of an in-memory sequence behind a source of the capturing provider,
    // so that a terminal operator can be taken of them.
    private static IQueryable<T> Rows<T>(T[] values)
    {
        return Customers.Where(c => c.Id == 1).SelectMany(c => values);
    }

    // #131: a -0.0 past the first row of VALUES is the untyped -0, which is 0.
    [Fact]
    public void KeepsSignOfNegativeZeroPastTheFirstRow()
    {
        database.AssertSameRows(
            new[] { 1.5, -0.0 }.AsQueryable().Select(d => d.ToString())
        );
    }

    // #131: the default of LAG is written -0.
    [Fact]
    public void KeepsSignOfNegativeZeroAsDefaultOfLag()
    {
        database.AssertSameRows(
            Trees
                .Over(
                    new[] { 1.5, 2.5 }.AsQueryable(),
                    null,
                    rows => rows.Order(),
                    null,
                    (v, w) => new { Value = v, Previous = w.Lag(x => x, 1, -0.0) }
                )
                .Select(r => new { r.Value, Previous = r.Previous.ToString() })
        );
    }

    // #131: a -0.0 in a computed value is the integer literal -0.
    [Fact]
    public void KeepsSignOfNegativeZeroInAComputedValue()
    {
        database.AssertSameRows(
            Orders.Select(o => new { o.Id, Zero = (o.IsPaid ? -0.0 : 1.0).ToString() })
        );
    }

    // Whole double constants in the branches of a conditional are divided as
    // doubles, not as the integers their digits alone would be.
    [Fact]
    public void DividesWholeDoubleConstantsAsDoubles()
    {
        database.AssertSameRows(
            Orders.Select(o => new { o.Id, Half = (o.IsPaid ? 7.0 : 5.0) / 2.0 })
        );
    }

    // #131: 100000000f + x is 100000000f in single precision for small x, so the
    // difference is 0; computed in double precision it is x.
    [Fact]
    public void AddsFloatConstantsInSinglePrecision()
    {
        database.AssertSameRows(
            new[] { 1f, 2.5f, -3f }
                .AsQueryable()
                .Select(x => new
                {
                    Value = x,
                    Shifted = x + 100000000f - 100000000f,
                    Scaled = (x * 3f) + 100000000f - 100000000f,
                })
        );
    }

    // #131: x.Score + 0.5f over a real column is t0."Score" + 0.5, double precision.
    [Fact]
    public void AddsFloatConstantsToARealColumnInSinglePrecision()
    {
        database.AssertSameRows(
            MappedTables.OrderEvents.Select(x => new
            {
                x.Id,
                Shifted = x.Score + 100000000f - 100000000f,
                Scaled = (x.Score * 3f) + 100000000f - 100000000f,
            })
        );
    }

    // #132: .NET orders NaN below -Infinity, PostgreSQL above +Infinity.
    [Fact]
    public void OrdersNaNAndInfinitiesAsDotNet()
    {
        database.AssertSameSequence(
            new[]
            {
                2,
                double.NaN,
                double.NegativeInfinity,
                double.PositiveInfinity,
                -1,
                double.NaN,
            }
                .AsQueryable()
                .OrderBy(d => d)
        );
    }

    // #132: RANK() OVER (ORDER BY value) ranks NaN first in .NET: 1, 5, 2, 6, 3, 3.
    [Fact]
    public void RanksNaNAsDotNet()
    {
        database.AssertSameRows(
            Trees.Over(
                new[]
                {
                    double.NaN,
                    1,
                    double.NegativeInfinity,
                    double.PositiveInfinity,
                    -0.0,
                    0,
                }.AsQueryable(),
                null,
                rows => rows.Order(),
                null,
                (v, w) => new { Value = v, Rank = w.Rank() }
            )
        );
    }

    // #132: for NaN, n > 0, n < 0 and n == n * 1 are all false in .NET.
    [Fact]
    public void ComparesNaNWithItselfAndZeroAsDotNet()
    {
        database.AssertSameRows(
            new[] { double.NaN, 1, -1 }
                .AsQueryable()
                .Select(n => new
                {
                    Value = n,
                    Positive = n > 0,
                    Negative = n < 0,
                    Same = n == (n * 1),
                })
        );
    }

    // #140: .NET sums the longs exactly and divides once: the mean of long.MinValue
    // and long.MaxValue is -0.5; averaged as doubles it is 0.
    [Fact]
    public void AveragesLongsFromTheirExactSum()
    {
        database.AssertSameResult(Rows([long.MinValue, long.MaxValue]), q => q.Average());
    }

    // #140: the same in a group.
    [Fact]
    public void AveragesTheLongsOfAGroupFromTheirExactSum()
    {
        (int Key, long Big)[] rows = [(1, long.MinValue), (1, long.MaxValue), (2, 3)];

        database.AssertSameRows(
            rows.AsQueryable()
                .GroupBy(r => r.Key)
                .Select(g => new { g.Key, Mean = g.Average(r => r.Big) })
        );
    }

    // #140: the same over the rows of a window.
    [Fact]
    public void AveragesTheLongsOfAWindowFromTheirExactSum()
    {
        database.AssertSameRows(
            Trees.Over(
                new[] { long.MinValue, long.MaxValue }.AsQueryable(),
                null,
                null,
                null,
                (v, w) => new { Value = v, Mean = w.Rows.Average(r => r) }
            )
        );
    }
}
