namespace Linq.Expressions.Translation.PostgreSql.ExecutionTests;

[Collection(SharedDatabase.Name)]
public sealed class NumericSemanticsTests(Database database)
{
    [Fact]
    public void RoundsDecimalsHalfToEven()
    {
        database.AssertSameSequence(
            Orders
                .OrderBy(o => o.Id)
                .Select(o => new
                {
                    o.Id,
                    Whole = Math.Round(o.Total),
                    Cents = Math.Round(o.Total, 2),
                    Tenths = Math.Round(o.Total, 1),
                    Discount = Math.Round(o.Discount ?? 0m, 2),
                })
        );
    }

    [Fact]
    public void RoundsDecimalsAwayFromZero()
    {
        database.AssertSameSequence(
            Orders
                .OrderBy(o => o.Id)
                .Select(o => new
                {
                    o.Id,
                    Whole = Math.Round(o.Total, MidpointRounding.AwayFromZero),
                    Cents = Math.Round(o.Total, 2, MidpointRounding.AwayFromZero),
                    Tenths = Math.Round(o.Total, 1, MidpointRounding.AwayFromZero),
                })
        );
    }

    [Fact]
    public void RoundsDoublesHalfToEvenAndAwayFromZero()
    {
        database.AssertSameSequence(
            Orders
                .Where(o => double.IsFinite(o.Weight))
                .OrderBy(o => o.Id)
                .Select(o => new
                {
                    o.Id,
                    Even = Math.Round(o.Weight),
                    Away = Math.Round(o.Weight, MidpointRounding.AwayFromZero),
                    Tenths = Math.Round(o.Weight, 1),
                    AwayTenths = Math.Round(o.Weight, 1, MidpointRounding.AwayFromZero),
                })
        );
    }

    [Fact]
    public void DividesNegativeIntegersTowardZero()
    {
        database.AssertSameSequence(
            Orders
                .OrderBy(o => o.Id)
                .Select(o => new
                {
                    o.Id,
                    Quotient = o.Points / 7,
                    Remainder = o.Points % 7,
                    Negated = -o.Quantity / 4,
                    NegatedRemainder = -o.Quantity % 4,
                    Fraction = o.Total % 1,
                })
        );
    }
}
