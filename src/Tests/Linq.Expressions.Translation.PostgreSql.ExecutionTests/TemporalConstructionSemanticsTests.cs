using Xunit.Sdk;

namespace Linq.Expressions.Translation.PostgreSql.ExecutionTests;

// Dates and times built from their parts are the values .NET builds, and fail where
// .NET throws: a year out of 1..9999, an invalid month, day, hour, minute, second or
// fraction, a day number or a count of ticks out of range.
[Collection(SharedDatabase.Name)]
public sealed class TemporalConstructionSemanticsTests(Database database)
{
    [Fact]
    public void BuildsDatesFromParts()
    {
        database.AssertSameRows(
            Orders.Select(o => new
            {
                o.Id,
                Day = new DateOnly(2023 + (o.Quantity % 2), 2, 28 + (o.Quantity % 2)),
                Month = new DateOnly(1 + o.Quantity, o.Quantity + 1, o.Id),
                Last = new DateOnly(9999 - o.Quantity, 12, 31),
            })
        );
    }

    [Fact]
    public void FiltersAndGroupsOnDatesFromParts()
    {
        database.AssertSameRows(
            Orders
                .Where(o =>
                    new DateOnly(2024, 1, 1 + o.Quantity) > new DateOnly(2024, 1, 3)
                )
                .GroupBy(o => new DateOnly(2024, 1 + (o.Quantity % 3), 1))
                .Select(g => new { g.Key, Count = g.Count() })
        );
    }

    [Fact]
    public void BuildsTimesFromParts()
    {
        database.AssertSameRows(
            Orders
                .Where(o => o.Id < 10)
                .Select(o => new
                {
                    o.Id,
                    Minute = new TimeOnly(o.Quantity, o.Id),
                    Second = new TimeOnly(23, 59, 59 - o.Quantity),
                    Milli = new TimeOnly(o.Quantity, 1, 2, o.Id * 111),
                    Micro = new TimeOnly(0, 0, 0, 999 - o.Id, o.Id * 111),
                })
        );
    }

    [Fact]
    public void BuildsTimesFromTicks()
    {
        database.AssertSameRows(
            Orders
                .Where(o => o.Points >= 0 && o.Points < 8_640_000_000)
                .Select(o => new
                {
                    o.Id,
                    Time = new TimeOnly(o.Points * 10),
                    Last = new TimeOnly(TimeSpan.TicksPerDay - 10 - (o.Points * 10)),
                })
        );
    }

    [Fact]
    public void BuildsTimestampsFromParts()
    {
        database.AssertSameRows(
            Orders
                .Where(o => o.Id < 10)
                .Select(o => new
                {
                    o.Id,
                    Day = new DateTime(2024, 2, 28 + (o.Quantity % 2)),
                    Second = new DateTime(2024, 12, 31, 23, 59, 59 - o.Quantity),
                    Milli = new DateTime(1, 1, 1, o.Quantity, 0, 0, o.Id * 111),
                    Micro = new DateTime(9999, 12, 31, 23, 59, 59, 999, o.Id * 111),
                    Utc = new DateTime(
                        2024,
                        3,
                        10,
                        2 + o.Quantity,
                        30,
                        0,
                        DateTimeKind.Utc
                    ),
                    Unspecified = new DateTime(
                        2024,
                        3,
                        10,
                        2,
                        o.Id,
                        0,
                        DateTimeKind.Unspecified
                    ),
                })
        );
    }

    [Fact]
    public void JoinsDatesAndTimes()
    {
        database.AssertSameRows(
            Orders
                .Where(o => o.ShippedOn != null)
                .Select(o => new
                {
                    o.Id,
                    At = o.ShippedOn!.Value.ToDateTime(o.DeliveryTime),
                    Utc = o.ShippedOn!.Value.ToDateTime(o.DeliveryTime, DateTimeKind.Utc),
                    Built = new DateTime(o.ShippedOn!.Value, o.DeliveryTime),
                    Later = o.ShippedOn!.Value.ToDateTime(new TimeOnly(12, 0))
                        > o.CreatedAt,
                })
        );
    }

    [Fact]
    public void CountsDayNumbers()
    {
        database.AssertSameRows(
            Orders.Select(o => new
            {
                o.Id,
                Day = DateOnly.FromDayNumber(738_000 + o.Id),
                First = DateOnly.FromDayNumber(o.Quantity),
                Last = DateOnly.FromDayNumber(3_652_058 - o.Quantity),
            })
        );
        database.AssertSameRows(
            Orders
                .Where(o => o.ShippedOn != null)
                .Select(o => DateOnly.FromDayNumber(o.ShippedOn!.Value.DayNumber + 1))
        );
    }

    // AddMonths keeps the day, or the last day of a shorter month, as .NET does.
    [Fact]
    public void AddsMonthsClampingTheDay()
    {
        database.AssertSameRows(
            Orders
                .Where(o => o.ShippedOn != null)
                .Select(o => new
                {
                    o.Id,
                    Later = o.ShippedOn!.Value.AddMonths(o.Quantity),
                    Earlier = o.ShippedOn!.Value.AddMonths(-o.Quantity - 1),
                    Year = o.ShippedOn!.Value.AddYears(1),
                    Moment = o.CreatedAt.AddMonths(o.Quantity + 1),
                    Day = new DateOnly(2024, 1, 31).AddMonths(o.Quantity),
                })
        );
    }

    public static TheoryData<Func<IQueryable<Order>, IQueryable<object>>> Failing =>
        [
            orders => orders.Select(o => (object)new DateOnly(o.Quantity * 10_000, 1, 1)),
            orders => orders.Select(o => (object)new DateOnly(-o.Quantity, 1, 1)),
            orders => orders.Select(o => (object)new DateOnly(2023, 2, 28 + o.Quantity)),
            orders => orders.Select(o => (object)new DateOnly(2024, o.Quantity + 3, 1)),
            orders => orders.Select(o => (object)new TimeOnly(o.Quantity + 20, 0)),
            orders => orders.Select(o => (object)new TimeOnly(0, o.Quantity + 55)),
            orders => orders.Select(o => (object)new TimeOnly(0, 0, o.Quantity + 55)),
            orders => orders.Select(o => (object)new TimeOnly(0, 0, 0, o.Quantity * 250)),
            orders =>
                orders.Select(o => (object)new TimeOnly(0, 0, 0, 0, o.Quantity * 250)),
            orders => orders.Select(o => (object)new TimeOnly(o.Points * 10)),
            orders =>
                orders.Select(o =>
                    (object)new TimeOnly(TimeSpan.TicksPerDay + o.Quantity)
                ),
            orders => orders.Select(o => (object)new DateTime(o.Quantity * 10_000, 1, 1)),
            orders =>
                orders.Select(o =>
                    (object)new DateTime(2024, 1, 1, o.Quantity + 20, 0, 0)
                ),
            orders =>
                orders.Select(o =>
                    (object)new DateTime(2024, 1, 1, 0, 0, o.Quantity + 55)
                ),
            orders =>
                orders.Select(o =>
                    (object)new DateTime(2024, 1, 1, 0, 0, 0, o.Quantity * 250)
                ),
            orders =>
                orders.Select(o =>
                    (object)new DateTime(2024, 1, 1, 0, 0, 0, 0, o.Quantity * 250)
                ),
            orders => orders.Select(o => (object)DateOnly.FromDayNumber(-o.Quantity)),
            orders =>
                orders.Select(o =>
                    (object)DateOnly.FromDayNumber(3_652_054 + o.Quantity)
                ),
        ];

    [Theory]
    [MemberData(nameof(Failing))]
    public void FailsWhereDotNetThrows(Func<IQueryable<Order>, IQueryable<object>> query)
    {
        IQueryable<object> failing = query(Orders);

        _ = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new EnumerableQuery<object>(InMemory.Rewrite(failing.Expression)).ToList()
        );
        XunitException exception = Assert.Throws<XunitException>(() =>
            database.Query(new PostgreSqlQuery(failing.Expression).TextValue)
        );
        Assert.Contains("out of range", exception.Message, StringComparison.Ordinal);
    }
}
