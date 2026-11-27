#pragma warning disable CA1861 // the constant arrays are the sources under test

using Xunit.Sdk;

namespace Linq.Expressions.Translation.PostgreSql.ExecutionTests;

// Dates and times built from their parts at the edges of their ranges: every part one
// past its bound fails on the server as .NET throws, every part at its bound gives
// .NET's value, and the values compose with the members, operators and sources that
// read them. Queries over the order with Id 1 multiply a constant by o.Id, so that the
// part is computed by the server and still exactly the constant.
[Collection(SharedDatabase.Name)]
public sealed class TemporalConstructionEdgeCaseSemanticsTests(Database database)
{
    private static IQueryable<Order> First => Orders.Where(o => o.Id == 1);

    public static TheoryData<Func<IQueryable<Order>, IQueryable<object>>> PastBounds =>
        [
            // DateOnly: years 0, 10000 and negative, months 0, 13 and negative, day 0,
            // 32, 31 April, 29 February of a common year and of 1900.
            orders => orders.Select(o => (object)new DateOnly(o.Id - 1, 1, 1)),
            orders => orders.Select(o => (object)new DateOnly(10_000 * o.Id, 1, 1)),
            orders => orders.Select(o => (object)new DateOnly(-o.Id, 1, 1)),
            orders => orders.Select(o => (object)new DateOnly(2024, o.Id - 1, 1)),
            orders => orders.Select(o => (object)new DateOnly(2024, 13 * o.Id, 1)),
            orders => orders.Select(o => (object)new DateOnly(2024, -o.Id, 1)),
            orders => orders.Select(o => (object)new DateOnly(2024, 1, o.Id - 1)),
            orders => orders.Select(o => (object)new DateOnly(2024, 1, 32 * o.Id)),
            orders => orders.Select(o => (object)new DateOnly(2024, 4, 31 * o.Id)),
            orders => orders.Select(o => (object)new DateOnly(2023, 2, 29 * o.Id)),
            orders => orders.Select(o => (object)new DateOnly(1900, 2, 29 * o.Id)),
            orders => orders.Select(o => (object)new DateOnly(2100, 2, 29 * o.Id)),
            orders => orders.Select(o => (object)new DateOnly(2024, 2, 30 * o.Id)),
            // TimeOnly: hour 24 and -1, minute 60 and -1, second 60 and -1, a
            // millisecond or microsecond of 1000 or -1, also where they would add up
            // to midnight.
            orders => orders.Select(o => (object)new TimeOnly(24 * o.Id, 0)),
            orders => orders.Select(o => (object)new TimeOnly(-o.Id, 0)),
            orders => orders.Select(o => (object)new TimeOnly(0, 60 * o.Id)),
            orders => orders.Select(o => (object)new TimeOnly(0, -o.Id)),
            orders => orders.Select(o => (object)new TimeOnly(0, 0, 60 * o.Id)),
            orders => orders.Select(o => (object)new TimeOnly(0, 0, -o.Id)),
            orders => orders.Select(o => (object)new TimeOnly(0, 0, 0, 1000 * o.Id)),
            orders => orders.Select(o => (object)new TimeOnly(0, 0, 0, -o.Id)),
            orders => orders.Select(o => (object)new TimeOnly(0, 0, 0, 0, 1000 * o.Id)),
            orders => orders.Select(o => (object)new TimeOnly(0, 0, 0, 0, -o.Id)),
            orders =>
                orders.Select(o => (object)new TimeOnly(23, 59, 59, 999, 1000 * o.Id)),
            orders => orders.Select(o => (object)new TimeOnly(23, 59, 60 * o.Id, 0)),
            // TimeOnly of ticks: a day and -1 tick (below a microsecond).
            orders =>
                orders.Select(o => (object)new TimeOnly(TimeSpan.TicksPerDay * o.Id)),
            orders => orders.Select(o => (object)new TimeOnly(-o.Id)),
            orders => orders.Select(o => (object)new TimeOnly(-10L * o.Id)),
            orders => orders.Select(o => (object)new TimeOnly(long.MinValue + o.Id)),
            orders => orders.Select(o => (object)new TimeOnly(long.MaxValue - o.Id)),
            // DateTime from parts.
            orders => orders.Select(o => (object)new DateTime(o.Id - 1, 1, 1)),
            orders => orders.Select(o => (object)new DateTime(10_000 * o.Id, 12, 31)),
            orders => orders.Select(o => (object)new DateTime(2024, 13 * o.Id, 1)),
            orders => orders.Select(o => (object)new DateTime(2024, o.Id - 1, 1)),
            orders => orders.Select(o => (object)new DateTime(2023, 2, 29 * o.Id)),
            orders => orders.Select(o => (object)new DateTime(2024, 1, o.Id - 1)),
            orders =>
                orders.Select(o => (object)new DateTime(2024, 1, 1, 24 * o.Id, 0, 0)),
            orders => orders.Select(o => (object)new DateTime(2024, 1, 1, -o.Id, 0, 0)),
            orders =>
                orders.Select(o => (object)new DateTime(2024, 1, 1, 0, 60 * o.Id, 0)),
            orders => orders.Select(o => (object)new DateTime(2024, 1, 1, 0, -o.Id, 0)),
            orders =>
                orders.Select(o => (object)new DateTime(2024, 1, 1, 0, 0, 60 * o.Id)),
            orders => orders.Select(o => (object)new DateTime(2024, 1, 1, 0, 0, -o.Id)),
            orders =>
                orders.Select(o =>
                    (object)new DateTime(2024, 1, 1, 0, 0, 0, 1000 * o.Id)
                ),
            orders =>
                orders.Select(o => (object)new DateTime(2024, 1, 1, 0, 0, 0, -o.Id)),
            orders =>
                orders.Select(o =>
                    (object)new DateTime(2024, 1, 1, 0, 0, 0, 0, 1000 * o.Id)
                ),
            orders =>
                orders.Select(o => (object)new DateTime(2024, 1, 1, 0, 0, 0, 0, -o.Id)),
            orders =>
                orders.Select(o =>
                    (object)new DateTime(9999, 12, 31, 23, 59, 59, 999, 1000 * o.Id)
                ),
            orders =>
                orders.Select(o =>
                    (object)new DateTime(2024, 1, 1, 24 * o.Id, 0, 0, DateTimeKind.Utc)
                ),
            orders =>
                orders.Select(o =>
                    (object)
                        new DateTime(
                            10_000 * o.Id,
                            1,
                            1,
                            0,
                            0,
                            0,
                            0,
                            DateTimeKind.Unspecified
                        )
                ),
            // DateOnly.FromDayNumber: -1, the day after 9999-12-31 and the int bounds.
            orders => orders.Select(o => (object)DateOnly.FromDayNumber(-o.Id)),
            orders =>
                orders.Select(o => (object)DateOnly.FromDayNumber(3_652_058 + o.Id)),
            orders =>
                orders.Select(o => (object)DateOnly.FromDayNumber(int.MaxValue * o.Id)),
            orders =>
                orders.Select(o => (object)DateOnly.FromDayNumber(int.MinValue + o.Id)),
        ];

    [Theory]
    [MemberData(nameof(PastBounds))]
    public void FailsOnePastEachBound(Func<IQueryable<Order>, IQueryable<object>> query)
    {
        AssertFails(query(First));
    }

    public static TheoryData<
        Func<IQueryable<Order>, IQueryable<object>>
    > TicksPastBounds =>
        [
            orders => orders.Select(o => (object)new DateTime(-o.Id).Year),
            orders => orders.Select(o => (object)new DateTime(-10L * o.Id).Year),
            orders =>
                orders.Select(o =>
                    (object)new DateTime(-TimeSpan.TicksPerDay * o.Id).Year
                ),
            orders =>
                orders.Select(o =>
                    (object)new DateTime(DateTime.MaxValue.Ticks + o.Id).Year
                ),
            orders =>
                orders.Select(o =>
                    (object)new DateTime(-o.Id, DateTimeKind.Utc).Year
                ),
        ];

    // new DateTime(ticks) throws for a count of ticks before 0001-01-01 or after
    // 9999-12-31 23:59:59.9999999. The year is read so that no out-of-range timestamp
    // reaches the client.
    // Observed: the statement succeeds, as the ticks are added to 0001-01-01 without a
    // range check: -1 tick is 0001-01-01 (the year 1), -10 ticks or -1 day a date in
    // 1 BC and DateTime.MaxValue.Ticks + 1 the year 10000.
    [Theory(
        Skip = "Bug: new DateTime(ticks) does not fail on the server for ticks out of range"
    )]
    [MemberData(nameof(TicksPastBounds))]
    public void FailsForTicksOutOfRange(Func<IQueryable<Order>, IQueryable<object>> query)
    {
        AssertFails(query(First));
    }

    public static TheoryData<
        Func<IQueryable<Order>, IQueryable<object>>
    > ShiftedPastBounds =>
        [
            orders =>
                orders.Select(o =>
                    (object)new DateOnly(9999, 12, 31 * o.Id).AddDays(1).Year
                ),
            orders =>
                orders.Select(o => (object)new DateOnly(o.Id, 1, 1).AddDays(-1).Year),
            orders =>
                orders.Select(o =>
                    (object)DateOnly.FromDayNumber(3_652_058 * o.Id).AddMonths(1).Year
                ),
            orders =>
                orders.Select(o =>
                    (object)new DateTime(9999, 12, 31 * o.Id, 23, 0, 0).AddHours(1).Year
                ),
            orders =>
                orders.Select(o => (object)new DateTime(o.Id, 1, 1).AddYears(-1).Year),
        ];

    // Shifting a date or a timestamp built at a bound past it throws in .NET.
    // Observed: the statement succeeds with the year 10000, or -1 for the year
    // before 1 (1 BC), as date and timestamp arithmetic reaches far past 9999.
    [Theory(
        Skip = "Bug: date and timestamp arithmetic does not fail past 9999-12-31 or before 0001-01-01"
    )]
    [MemberData(nameof(ShiftedPastBounds))]
    public void FailsWhenShiftedPastBound(
        Func<IQueryable<Order>, IQueryable<object>> query
    )
    {
        AssertFails(query(First));
    }

    // Ticks and day numbers read back build the same value.
    [Fact]
    public void RoundTripsTicksAndDayNumbers()
    {
        database.AssertSameRows(
            Orders.Select(o => new
            {
                o.Id,
                Moment = new DateTime(o.CreatedAt.Ticks) == o.CreatedAt,
                Time = new TimeOnly(o.DeliveryTime.Ticks) == o.DeliveryTime,
                Day = o.ShippedOn == null
                    || DateOnly.FromDayNumber(o.ShippedOn.Value.DayNumber)
                        == o.ShippedOn,
                new DateTime(o.CreatedAt.Ticks).Ticks,
            })
        );
    }

    [Fact]
    public void BuildsEveryBoundOfDates()
    {
        database.AssertSameRows(
            First.Select(o => new
            {
                Min = new DateOnly(o.Id, 1, 1),
                Max = new DateOnly(9999 * o.Id, 12, 31),
                Leap = new DateOnly(2024, 2, 29 * o.Id),
                Century = new DateOnly(2000, 2, 29 * o.Id),
                FourCenturies = new DateOnly(1600, 2, 29 * o.Id),
                Common = new DateOnly(1900, 2, 28 * o.Id),
                Year1 = new DateOnly(4, 2, 29 * o.Id),
                December = new DateOnly(2024, 12 * o.Id, 31),
                April = new DateOnly(2024, 4, 30 * o.Id),
            })
        );
    }

    [Fact]
    public void BuildsEveryBoundOfTimes()
    {
        database.AssertSameRows(
            First.Select(o => new
            {
                Midnight = new TimeOnly(0 * o.Id, 0),
                Hour = new TimeOnly(23 * o.Id, 0),
                Minute = new TimeOnly(0, 59 * o.Id),
                Second = new TimeOnly(0, 0, 59 * o.Id),
                Milli = new TimeOnly(0, 0, 0, 999 * o.Id),
                Micro = new TimeOnly(0, 0, 0, 0, 999 * o.Id),
                Last = new TimeOnly(23, 59, 59, 999, 999 * o.Id),
                LastMilli = new TimeOnly(23 * o.Id, 59, 59, 999),
                LastTick = new TimeOnly((TimeSpan.TicksPerDay * o.Id) - 10),
                Zero = new TimeOnly(0L * o.Id),
                Micros = new TimeOnly(10L * o.Id),
            })
        );
    }

    [Fact]
    public void BuildsEveryBoundOfTimestamps()
    {
        database.AssertSameRows(
            First.Select(o => new
            {
                Min = new DateTime(o.Id, 1, 1),
                MinClock = new DateTime(o.Id, 1, 1, 0, 0, 0, 0, 0),
                Max = new DateTime(9999 * o.Id, 12, 31, 23, 59, 59, 999, 999),
                Leap = new DateTime(2000, 2, 29 * o.Id, 23, 59, 59),
                Hour = new DateTime(2024, 1, 1, 23 * o.Id, 0, 0),
                Minute = new DateTime(2024, 1, 1, 0, 59 * o.Id, 0),
                Second = new DateTime(2024, 1, 1, 0, 0, 59 * o.Id),
                Milli = new DateTime(2024, 1, 1, 0, 0, 0, 999 * o.Id),
                Micro = new DateTime(2024, 1, 1, 0, 0, 0, 0, 999 * o.Id),
                Utc = new DateTime(9999, 12, 31, 23 * o.Id, 59, 59, DateTimeKind.Utc),
                FirstTick = new DateTime(0L * o.Id),
                LastTick = new DateTime(DateTime.MaxValue.Ticks - 9 + (0L * o.Id)),
                Day = new DateTime(TimeSpan.TicksPerDay * o.Id),
            })
        );
    }

    [Fact]
    public void CountsEveryBoundOfDayNumbers()
    {
        database.AssertSameRows(
            First.Select(o => new
            {
                Min = DateOnly.FromDayNumber(o.Id - 1),
                Max = DateOnly.FromDayNumber(3_652_058 * o.Id),
                Unix = DateOnly.FromDayNumber(719_162 * o.Id),
                Leap = DateOnly.FromDayNumber(738_944 * o.Id),
            })
        );
    }

    // Every millisecond and microsecond of the last second of a day adds up to the
    // fraction exactly: the parts are summed as numeric and rounded once.
    [Fact]
    public void AddsFractionsExactly()
    {
        database.AssertSameRows(
            Orders.SelectMany(
                o => Orders,
                (a, b) =>
                    new
                    {
                        a.Id,
                        Other = b.Id,
                        Time = new TimeOnly(23, 59, 59, a.Id * 83, b.Id * 83),
                        Moment = new DateTime(
                            9999,
                            12,
                            31,
                            23,
                            59,
                            59,
                            a.Id * 83,
                            b.Id * 83
                        ),
                        Inverse = new TimeOnly(
                            0,
                            0,
                            0,
                            999 - (a.Id * 83),
                            999 - (b.Id * 83)
                        ),
                    }
            )
        );
    }

    // A part out of range in a branch no row reaches does not fail the statement, also
    // when the other parts are constants that PostgreSQL folds while planning it.
    [Fact]
    public void LeavesUnreachedPartsOutOfRange()
    {
        int month = 13;
        long ticks = TimeSpan.TicksPerDay;

        database.AssertSameRows(
            Orders.Select(o => new
            {
                o.Id,
                Date = o.Id > 100
                    ? new DateOnly(2024, month, 1)
                    : new DateOnly(2024, 1, 1),
                Year = o.Id > 100 ? new DateOnly(10_000, 1, 1) : DateOnly.MinValue,
                Hour = o.Id > 100 ? new TimeOnly(24, 0) : TimeOnly.MinValue,
                Ticks = o.Id > 100 ? new TimeOnly(ticks) : TimeOnly.MinValue,
                Day = o.Id > 100 ? DateOnly.FromDayNumber(-1) : DateOnly.MinValue,
                Moment = o.Id > 100 ? new DateTime(10_000, 1, 1) : DateTime.MinValue,
            })
        );
    }

    // A constant fraction or second out of range makes the guarded hour or year the
    // constant -1 or 0: with constant other parts, MAKE_TIME(-1, 0, 60) or
    // MAKE_TIMESTAMP(0, 1, 1, 24, 0, 0) is folded, and fails, while PostgreSQL plans
    // the statement, though no row reaches the branch.
    // Observed: "time field value out of range: -1:00:60" for the first of them.
    [Fact(
        Skip = "Bug: a constant part out of range fails the statement at planning time, even in a branch no row reaches"
    )]
    public void LeavesUnreachedConstantPartsOutOfRange()
    {
        database.AssertSameRows(
            Orders.Select(o => new
            {
                o.Id,
                Second = o.Id > 100 ? new TimeOnly(o.Id, 0, 60) : TimeOnly.MinValue,
                Milli = o.Id > 100 ? new TimeOnly(o.Id, 0, 0, 1000) : TimeOnly.MinValue,
                Hour = o.Id > 100
                    ? new DateTime(o.Id, 1, 1, 24, 0, 0)
                    : DateTime.MinValue,
                Micro = o.Id > 100
                    ? new DateTime(o.Id, 1, 1, 0, 0, 0, 0, 1000)
                    : DateTime.MinValue,
            })
        );
    }

    [Fact(
        Skip = "Bug: a constant part out of range fails the statement at planning time, even in a branch no row reaches"
    )]
    public void LeavesUnreachedFoldedPartsOutOfRange()
    {
        database.AssertSameRows(
            Orders.Select(o => new
            {
                o.Id,
                Second = o.Id > 100 ? new TimeOnly(0, 0, 60) : TimeOnly.MinValue,
                Hour = o.Id > 100
                    ? new DateTime(2024, 1, 1, 24, 0, 0)
                    : DateTime.MinValue,
            })
        );
    }

    // The same, where the parts are captured: a captured part is folded like a literal.
    [Fact]
    public void FailsWhereCapturedPartIsReached()
    {
        int month = 13;

        AssertFails(
            First.Select(o =>
                (object)(o.Id > 0 ? new DateOnly(2024, month, 1) : DateOnly.MinValue)
            )
        );
    }

    [Fact]
    public void ComparesCapturedPartsWithColumns()
    {
        int year = 2024;
        int month = 2;
        int day = 29;

        database.AssertSameRows(
            Orders.Select(o => new
            {
                o.Id,
                Before = o.ShippedOn < new DateOnly(year, month, day),
                Same = o.ShippedOn == new DateOnly(year, month, day),
                Later = o.CreatedAt > new DateTime(year, month, day, 12, 0, 0),
                Earlier = o.DeliveryTime < new TimeOnly(month * 4, day),
            })
        );
    }

    [Fact]
    public void ReadsMembersOfDatesFromParts()
    {
        database.AssertSameRows(
            Orders.Select(o => new
            {
                o.Id,
                new DateOnly(2000 + o.Quantity, 1 + o.Quantity, 1).Year,
                new DateOnly(2024, 1 + o.Quantity, 28).Month,
                Weekday = new DateOnly(2024, 1 + o.Quantity, 1).DayOfWeek,
                YearDay = new DateOnly(2024, 12, 21 + o.Quantity).DayOfYear,
                Number = new DateOnly(1, 1, 1 + o.Quantity).DayNumber,
                LastOfMonth = new DateOnly(2024, 1 + o.Quantity, 1)
                    .AddMonths(1)
                    .AddDays(-1),
                LeapYear = new DateOnly(2024, 2, 29).AddYears(o.Quantity),
                Moment = new DateOnly(2024, 1 + o.Quantity, 1).ToDateTime(o.DeliveryTime),
            })
        );
    }

    [Fact]
    public void ReadsMembersOfTimesFromParts()
    {
        database.AssertSameRows(
            Orders.Select(o => new
            {
                o.Id,
                new TimeOnly(o.Quantity, 30).Hour,
                new TimeOnly(1, 50 + (o.Quantity % 10)).Minute,
                new TimeOnly(1, 1, 50 + (o.Quantity % 10)).Second,
                Milli = new TimeOnly(1, 1, 1, o.Quantity * 99).Millisecond,
                Micro = new TimeOnly(1, 1, 1, 1, o.Quantity * 99).Microsecond,
                new TimeOnly(o.Quantity, 1, 2, 3, 4).Ticks,
                Wrapped = new TimeOnly(23, o.Quantity).AddHours(o.Quantity),
                Earlier = new TimeOnly(o.Quantity, 0).AddMinutes(-90),
                FromTicks = new TimeOnly(
                    (o.Points * 0) + (o.Quantity * TimeSpan.TicksPerHour)
                ).Hour,
            })
        );
    }

    [Fact]
    public void ReadsMembersOfTimestampsFromParts()
    {
        database.AssertSameRows(
            Orders.Select(o => new
            {
                o.Id,
                new DateTime(2024, 1, 1, o.Quantity, 0, 0).Hour,
                Weekday = new DateTime(2024, 3, 1 + o.Quantity).DayOfWeek,
                new DateTime(2024, 3, 1 + o.Quantity, 23, 59, 59).Date,
                Clock = new DateTime(2024, 3, 1, o.Quantity, 1, 2, 3, 4).TimeOfDay,
                Tomorrow = new DateTime(2024, 2, 28, 12, 0, 0).AddDays(o.Quantity),
                new DateTime(2024, 1, 1 + o.Quantity).Ticks,
                Millis = new DateTime(2024, 1, 1, 0, 0, 0, o.Quantity * 99).Millisecond,
                Back = new DateTime(
                    (o.Points * 0) + (o.Quantity * TimeSpan.TicksPerDay)
                ).Day,
            })
        );
    }

    [Fact]
    public void SubtractsDatesAndTimesFromParts()
    {
        database.AssertSameRows(
            Orders.Select(o => new
            {
                o.Id,
                Elapsed = new DateTime(2024, 1, 1 + o.Quantity, 6, 0, 0) - o.CreatedAt,
                Days = new DateOnly(2024, 3, 1 + o.Quantity).DayNumber
                    - new DateOnly(2024, 2, 1).DayNumber,
                Hours = (
                    new DateTime(2024, 3, 10, o.Quantity, 0, 0)
                    - new DateTime(2024, 3, 9, 12, 0, 0)
                ).TotalHours,
            })
        );
    }

    // TimeOnly subtraction is the time elapsed on a circular clock in .NET, never
    // negative. Observed: time - time is a negative interval for an earlier time,
    // 09:00 - 12:02 is -03:02:00 where .NET gives 20:58:00.
    [Fact(
        Skip = "Bug: TimeOnly subtraction is time - time, negative where .NET wraps around midnight"
    )]
    public void SubtractsTimesAroundMidnight()
    {
        database.AssertSameRows(
            Orders.Select(o => new
            {
                o.Id,
                Since = o.DeliveryTime - new TimeOnly(12, o.Quantity),
            })
        );
    }

    [Fact]
    public void OrdersAndGroupsByValuesFromParts()
    {
        database.AssertSameSequence(
            Orders
                .OrderBy(o => new TimeOnly(o.DeliveryTime.Hour, 0))
                .ThenByDescending(o => new DateTime(2024, 1 + (o.Quantity % 12), 1))
                .ThenBy(o => o.Id)
                .Select(o => o.Id)
        );
        database.AssertSameRows(
            Orders
                .GroupBy(o => new TimeOnly(o.DeliveryTime.Hour / 6 * 6, 0))
                .Select(g => new
                {
                    g.Key,
                    Count = g.Count(),
                    Latest = g.Max(o => new DateTime(2024, 1, 1 + o.Quantity, 0, 0, 0)),
                    Earliest = g.Min(o => new DateOnly(2024, 1, 1 + o.Quantity)),
                })
        );
    }

    [Fact]
    public void PagesAndDistinctsValuesFromParts()
    {
        database.AssertSameSequence(
            Orders
                .Select(o => new DateOnly(2024, 1 + (o.Quantity % 3), 1))
                .Distinct()
                .OrderBy(d => d)
                .Skip(1)
                .Take(5)
        );
        database.AssertSameRows(
            Orders
                .Where(o => o.ShippedOn != null)
                .Select(o => new DateTime(
                    o.ShippedOn!.Value,
                    new TimeOnly(o.Quantity, 0)
                ))
                .Where(m => m.Hour < 5)
        );
    }

    [Fact]
    public void JoinsValuesFromPartsWithInMemoryValues()
    {
        DateOnly[] months =
        [
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 2, 1),
            new DateOnly(2000, 1, 1),
        ];

        database.AssertSameRows(
            Orders.Join(
                months,
                o => new DateOnly(o.CreatedAt.Year, o.CreatedAt.Month, 1),
                m => m,
                (o, m) => new { o.Id, Month = m }
            )
        );
        database.AssertSameRows(
            Orders
                .Select(o => new DateOnly(o.CreatedAt.Year, o.CreatedAt.Month, 1))
                .Intersect(months)
        );
        database.AssertSameRows(
            Orders
                .Select(o => new DateOnly(2024, 1 + (o.Quantity % 2), 1))
                .Except(months)
                .Concat(months)
        );
    }

    [Fact]
    public void CountsValuesFromParts()
    {
        database.AssertSameResult(
            Orders,
            q =>
                q.Count(o =>
                    new DateOnly(2024, 1, 1 + o.Quantity) > new DateOnly(2024, 1, 4)
                )
        );
        database.AssertSameResult(
            Orders,
            q => q.Max(o => new TimeOnly(o.Quantity, o.Id))
        );
        database.AssertSameResult(
            Orders,
            q =>
                q.Where(o => o.ShippedOn != null)
                    .Min(o => o.ShippedOn!.Value.ToDateTime(new TimeOnly(o.Quantity, 0)))
        );
    }

    // An unspecified DateTime in the gap or the overlap of daylight saving time
    // converts to a DateTimeOffset through the local zone in .NET and the session zone
    // in PostgreSQL: both take it as standard time.
    [Fact]
    public void ConvertsTimestampsFromPartsAcrossDaylightSaving()
    {
        database.AssertSameRows(
            Orders.Select(o => new
            {
                o.Id,
                Gap = o.PaidAt > new DateTime(2024, 3, 10, 2, o.Quantity * 5, 0),
                Overlap = o.PaidAt > new DateTime(2024, 11, 3, 1, o.Quantity * 5, 0),
                Offset = (DateTimeOffset)new DateTime(2024, 3, 10, 2, o.Quantity * 5, 0),
                Back = (DateTimeOffset)new DateTime(2024, 11, 3, 1, o.Quantity * 5, 0),
            })
        );
    }

    // A Utc DateTime converts to a DateTimeOffset with a zero offset.
    [Fact]
    public void ConvertsUtcTimestampsFromPartsToOffsets()
    {
        database.AssertSameRows(
            Orders.Select(o => new
            {
                o.Id,
                Later = o.PaidAt
                    > new DateTime(2024, 1, 15, o.Quantity, 0, 0, DateTimeKind.Utc),
                Offset = (DateTimeOffset)
                    new DateTime(2024, 1, 15, o.Quantity, 0, 0, DateTimeKind.Utc),
            })
        );
    }

    // C# compares DateTime values by their ticks whatever their kind, and the members
    // of a Utc DateTime are read as they are written, not in the local zone.
    // Observed: MAKE_TIMESTAMP(...) AT TIME ZONE 'UTC' is a timestamptz, whose hour,
    // day and date PostgreSQL reads in the session zone (America/New_York), so Hour
    // of 2024-01-01 02:00 UTC is 21 instead of 2.
    [Fact(
        Skip = "Bug: a DateTime constructed with DateTimeKind.Utc is a timestamptz, read in the session time zone"
    )]
    public void ReadsMembersOfUtcTimestampsAsWritten()
    {
        database.AssertSameRows(
            Orders.Select(o => new
            {
                o.Id,
                new DateTime(2024, 1, 1, o.Quantity, 0, 0, DateTimeKind.Utc).Hour,
                new DateTime(2024, 1, 1, o.Quantity, 0, 0, DateTimeKind.Utc).Day,
                new DateTime(2024, 1, 1, o.Quantity, 0, 0, DateTimeKind.Utc).Date,
                Joined = o.ShippedOn == null
                    ? 0
                    : o.ShippedOn.Value.ToDateTime(
                        o.DeliveryTime,
                        DateTimeKind.Utc
                    ).Hour,
                Built = o.ShippedOn == null
                    ? 0
                    : new DateTime(
                        o.ShippedOn.Value,
                        o.DeliveryTime,
                        DateTimeKind.Utc
                    ).Hour,
                Ticks = new DateTime(
                    (o.Quantity * TimeSpan.TicksPerHour) + DateTime.UnixEpoch.Ticks,
                    DateTimeKind.Utc
                ).Hour,
                Calendar = DateOnly.FromDateTime(
                    new DateTime(2024, 1, 1, o.Quantity, 0, 0, DateTimeKind.Utc)
                ),
                Time = TimeOnly.FromDateTime(
                    new DateTime(2024, 1, 1, o.Quantity, 0, 0, DateTimeKind.Utc)
                ),
            })
        );
    }

    // Observed: the timestamptz compares with the timestamp column and the TIMESTAMP
    // constant through the session zone, five hours later.
    [Fact(
        Skip = "Bug: a DateTime constructed with DateTimeKind.Utc compares with a DateTime through the session time zone"
    )]
    public void ComparesUtcTimestampsByTicks()
    {
        database.AssertSameRows(
            Orders.Select(o => new
            {
                o.Id,
                Column = new DateTime(
                    o.CreatedAt.Year,
                    o.CreatedAt.Month,
                    o.CreatedAt.Day,
                    o.CreatedAt.Hour,
                    o.CreatedAt.Minute,
                    o.CreatedAt.Second,
                    DateTimeKind.Utc
                ) >= o.CreatedAt,
                Constant = new DateTime(2024, 1, 1, o.Quantity, 0, 0, DateTimeKind.Utc)
                    > new DateTime(2024, 1, 1, 3, 0, 0, DateTimeKind.Utc),
                Unspecified = new DateTime(2024, 1, 1, o.Quantity, 0, 0, DateTimeKind.Utc)
                    > new DateTime(2024, 1, 1, 3, 0, 0),
            })
        );
    }

    // Observed: timestamptz - timestamp converts the timestamp through the session
    // zone, and adding a day across the change to daylight saving time adds 23 hours.
    [Fact(
        Skip = "Bug: arithmetic on a DateTime constructed with DateTimeKind.Utc follows the session time zone"
    )]
    public void ComputesWithUtcTimestampsByTicks()
    {
        database.AssertSameRows(
            Orders.Select(o => new
            {
                o.Id,
                Elapsed = new DateTime(2024, 1, 1, o.Quantity, 0, 0, DateTimeKind.Utc)
                    - new DateTime(2024, 1, 1, 0, 0, 0),
                Tomorrow = new DateTime(
                    2024,
                    3,
                    9,
                    12,
                    o.Quantity,
                    0,
                    DateTimeKind.Utc
                ).AddDays(1),
            })
        );
    }

    // Observed: UNION and GREATEST resolve timestamptz and timestamp to timestamptz,
    // so the column's values shift by the session zone's offset.
    [Fact(
        Skip = "Bug: a DateTime constructed with DateTimeKind.Utc shifts a DateTime it is combined with by the session time zone"
    )]
    public void CombinesUtcTimestampsWithColumns()
    {
        database.AssertSameRows(
            Orders
                .Select(o => new DateTime(2024, 1, 1, o.Quantity, 0, 0, DateTimeKind.Utc))
                .Union(Orders.Select(o => o.CreatedAt))
        );
        database.AssertSameRows(
            Orders.Select(o => new
            {
                o.Id,
                Latest = new[]
                {
                    o.CreatedAt,
                    new DateTime(2024, 1, 1, o.Quantity, 0, 0, DateTimeKind.Utc),
                }.Max(),
            })
        );
    }

    // A Utc DateTime reads back with the kind Utc and an unspecified one without a
    // kind, both with .NET's ticks.
    [Fact]
    public void ReadsKindsOfTimestampsFromParts()
    {
        ResultSet result = database.Query(
            new PostgreSqlQuery(
                First
                    .Select(o => new
                    {
                        Utc = new DateTime(
                            2024,
                            3,
                            10,
                            2 * o.Id,
                            30,
                            0,
                            DateTimeKind.Utc
                        ),
                        Joined = o.ShippedOn!.Value.ToDateTime(
                            o.DeliveryTime,
                            DateTimeKind.Utc
                        ),
                        Plain = new DateTime(2024, 3, 10, 2 * o.Id, 30, 0),
                        Unspecified = new DateTime(
                            2024,
                            3,
                            10,
                            2 * o.Id,
                            30,
                            0,
                            DateTimeKind.Unspecified
                        ),
                    })
                    .Expression
            ).TextValue
        );

        object?[] row = Assert.Single(result.Rows);
        DateTime utc = Assert.IsType<DateTime>(row[0]);
        DateTime joined = Assert.IsType<DateTime>(row[1]);
        DateTime plain = Assert.IsType<DateTime>(row[2]);
        DateTime unspecified = Assert.IsType<DateTime>(row[3]);
        Assert.Equal(
            (new DateTime(2024, 3, 10, 2, 30, 0).Ticks, DateTimeKind.Utc),
            (utc.Ticks, utc.Kind)
        );
        Assert.Equal(
            (new DateTime(2024, 1, 16, 9, 0, 0).Ticks, DateTimeKind.Utc),
            (joined.Ticks, joined.Kind)
        );
        Assert.Equal(
            (new DateTime(2024, 3, 10, 2, 30, 0).Ticks, DateTimeKind.Unspecified),
            (plain.Ticks, plain.Kind)
        );
        Assert.Equal(plain, unspecified);
    }

    // A part read from a subquery over no rows is the default of its type in .NET.
    [Fact]
    public void BuildsFromPartsOfEmptySubqueries()
    {
        database.AssertSameRows(
            Orders.Select(o => new
            {
                o.Id,
                Year = new DateOnly(
                    2000
                        + Orders
                            .Where(x => x.Id == o.Id + 100)
                            .Select(x => x.Quantity)
                            .FirstOrDefault(),
                    1,
                    1
                ),
                Hour = new TimeOnly(
                    Orders.Where(x => x.Id == o.Id + 100).Sum(x => x.Quantity),
                    0
                ),
                Count = new DateTime(
                    2024,
                    1,
                    1 + Orders.Count(x => x.CustomerId == o.Id),
                    Orders.Count(x => x.Id > 100),
                    0,
                    0
                ),
            })
        );
    }

    [Fact]
    public void BuildsFromPartsOfOuterJoinedRows()
    {
        database.AssertSameRows(
            Customers
                .GroupJoin(
                    Orders,
                    c => c.Id,
                    o => o.CustomerId,
                    (c, os) => new { c.Id, os }
                )
                .SelectMany(
                    x => x.os.DefaultIfEmpty(),
                    (x, o) =>
                        new
                        {
                            x.Id,
                            Day = o == null
                                ? (DateOnly?)null
                                : new DateOnly(2024, 1, o.Quantity + 1),
                            Time = o == null
                                ? TimeOnly.MinValue
                                : new TimeOnly(o.Quantity, o.Id),
                        }
                )
        );
        database.AssertSameRows(
            Customers.Select(c => new
            {
                c.Id,
                Latest = Orders
                    .Where(o => o.CustomerId == c.Id)
                    .Select(o => (DateOnly?)new DateOnly(2024, 1, 1 + o.Quantity))
                    .Max(),
            })
        );
    }

    [Fact]
    public void CrossesInMemoryValuesWithValuesFromParts()
    {
        (int Hour, int Minute)[] clock = [(0, 0), (12, 30), (23, 59)];

        database.AssertSameRows(
            Orders.SelectMany(
                o => clock,
                (o, t) =>
                    new
                    {
                        o.Id,
                        Time = new TimeOnly(t.Hour, t.Minute, o.Quantity),
                        Later = new TimeOnly(t.Hour, t.Minute) > o.DeliveryTime,
                    }
            )
        );
    }

    // A count of ticks below a microsecond is truncated, as PostgreSQL keeps none.
    [Fact]
    public void TruncatesTicksBelowMicrosecond()
    {
        ResultSet result = database.Query(
            new PostgreSqlQuery(
                First.Select(o => new TimeOnly((o.Id * 10_000_000L) + 9)).Expression
            ).TextValue
        );

        object?[] row = Assert.Single(result.Rows);
        Assert.Equal(new TimeOnly(0, 0, 1), row[0]);
    }

    // A TimeOnly constant finer than a microsecond is not what PostgreSQL compares:
    // .NET orders 00:00:00 before 00:00:00.0000005 where TIME '00:00:00' is equal.
    // Observed: the constant renders as TIME '00:00:00', truncated, where a DateTime
    // constant finer than a microsecond is refused with NotSupportedException.
    [Fact(
        Skip = "Bug: a TimeOnly constant finer than a microsecond is truncated silently"
    )]
    public void ComparesTimeConstantFinerThanMicrosecond()
    {
        database.AssertSameRows(
            Orders.Select(o => new
            {
                o.Id,
                Later = o.DeliveryTime >= new TimeOnly(5),
                Before = o.DeliveryTime
                    < new TimeOnly(23, 59, 59).Add(TimeSpan.FromTicks(9_999_995)),
            })
        );
    }

    private void AssertFails(IQueryable<object> failing)
    {
        _ = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new EnumerableQuery<object>(InMemory.Rewrite(failing.Expression)).ToList()
        );
        XunitException exception = Assert.Throws<XunitException>(() =>
            database.Query(new PostgreSqlQuery(failing.Expression).TextValue)
        );
        Assert.Contains("out of range", exception.Message, StringComparison.Ordinal);
    }
}
