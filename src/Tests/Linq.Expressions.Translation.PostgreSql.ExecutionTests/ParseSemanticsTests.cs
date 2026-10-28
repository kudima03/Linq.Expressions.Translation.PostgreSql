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
    public void ParsesInvariantShortDateWithSingleDigits()
    {
        database.AssertSameSequence(
            Orders
                .Where(o => o.Quantity > 0 && o.Quantity < 10)
                .OrderBy(o => o.Id)
                .Select(o => new
                {
                    o.Id,
                    Date = DateOnly.Parse(
                        o.Quantity + "/2/2024",
                        CultureInfo.InvariantCulture
                    ),
                })
        );
    }

    [Fact]
    public void ParsesInvariantShortDateWithoutTimeAsMidnight()
    {
        database.AssertSameSequence(
            Orders
                .Where(o => o.Quantity > 0 && o.Quantity < 10)
                .OrderBy(o => o.Id)
                .Select(o => new
                {
                    o.Id,
                    Time = DateTime.Parse(
                        "0" + o.Quantity + "/02/2024",
                        CultureInfo.InvariantCulture
                    ),
                })
        );
    }

    // The session zone is America/New_York (execution.runsettings), where the hour
    // from 02:00 on 2024-03-10 does not exist; the time is still read as written.
    [Fact]
    public void ParsesInvariantTimeInDaylightSavingGapAsWritten()
    {
        database.AssertSameSequence(
            Orders
                .Where(o => o.Quantity > 0 && o.Quantity < 10)
                .OrderBy(o => o.Id)
                .Select(o => new
                {
                    o.Id,
                    Time = DateTime.Parse(
                        "03/10/2024 02:3" + o.Quantity + ":00",
                        CultureInfo.InvariantCulture
                    ),
                })
        );
    }
}
