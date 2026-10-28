using System.Globalization;

namespace Linq.Expressions.Translation.PostgreSql.ExecutionTests;

// The invariant culture reads a short date as month/day/year whatever DateStyle the
// server has; the database runs with a day-first DateStyle (Database).
[Collection(SharedDatabase.Name)]
public sealed class ParseSemanticsTests(Database database)
{
    [Fact]
    public void ParsesInvariantShortDateMonthFirst()
    {
        database.AssertSameSequence(
            Orders
                .Where(o => o.Quantity > 0 && o.Quantity < 10)
                .OrderBy(o => o.Id)
                .Select(o => new
                {
                    o.Id,
                    Date = DateOnly.Parse(
                        "0" + o.Quantity + "/02/2024",
                        CultureInfo.InvariantCulture
                    ),
                })
        );
    }

    [Fact]
    public void ParsesInvariantShortDateAndTimeMonthFirst()
    {
        database.AssertSameSequence(
            Orders
                .Where(o => o.Quantity > 0 && o.Quantity < 10)
                .OrderBy(o => o.Id)
                .Select(o => new
                {
                    o.Id,
                    Time = DateTime.Parse(
                        "0" + o.Quantity + "/02/2024 10:30:00",
                        CultureInfo.InvariantCulture
                    ),
                })
        );
    }

    [Fact]
    public void ParsesIsoDateWhateverDateStyle()
    {
        database.AssertSameSequence(
            Orders
                .Where(o => o.Quantity > 0 && o.Quantity < 10)
                .OrderBy(o => o.Id)
                .Select(o => new
                {
                    o.Id,
                    Date = DateOnly.Parse(
                        "2024-02-0" + o.Quantity,
                        CultureInfo.InvariantCulture
                    ),
                })
        );
    }
}
