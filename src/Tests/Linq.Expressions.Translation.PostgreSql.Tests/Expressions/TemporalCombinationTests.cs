namespace Linq.Expressions.Translation.PostgreSql.Tests.Expressions;

public sealed record TemporalCombinationTests
{
    [Fact]
    public void FiltersByWeekday()
    {
        Expression query = Orders
            .Where(o => o.CreatedAt.DayOfWeek == DayOfWeek.Monday)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE CAST(EXTRACT(DOW FROM t0."CreatedAt") AS integer) = 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersWeekendDays()
    {
        Expression query = Orders
            .Where(o =>
                o.CreatedAt.DayOfWeek == DayOfWeek.Saturday
                || o.CreatedAt.DayOfWeek == DayOfWeek.Sunday
            )
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (CAST(EXTRACT(DOW FROM t0."CreatedAt") AS integer) = 6) OR (CAST(EXTRACT(DOW FROM t0."CreatedAt") AS integer) = 0)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByYearAndFirstHalf()
    {
        Expression query = Orders
            .Where(o => o.CreatedAt.Year == 2024 && o.CreatedAt.Month <= 6)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (CAST(EXTRACT(YEAR FROM t0."CreatedAt") AS integer) = 2024) AND (CAST(EXTRACT(MONTH FROM t0."CreatedAt") AS integer) <= 6)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsWholeSecondOfFractionalTimestamp()
    {
        Expression query = Orders.Select(o => o.CreatedAt.Second).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(TRUNC(EXTRACT(SECOND FROM t0."CreatedAt")) AS integer)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsWholeMillisecondOfFractionalTimestamp()
    {
        Expression query = Orders.Select(o => o.CreatedAt.Millisecond).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(TRUNC(EXTRACT(MILLISECONDS FROM t0."CreatedAt")) AS integer) % 1000
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesTruncatedDateWithCapturedDate()
    {
        DateTime day = new(2024, 3, 1);
        Expression query = Orders
            .Where(o => o.CreatedAt.Date == day)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE DATE_TRUNC('day', t0."CreatedAt") = TIMESTAMP '2024-03-01 00:00:00'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByHalfOpenRangeOfCapturedMoments()
    {
        DateTime from = new(2024, 1, 1);
        DateTime to = new(2024, 2, 1);
        Expression query = Orders
            .Where(o => from <= o.CreatedAt && o.CreatedAt < to)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (TIMESTAMP '2024-01-01 00:00:00' <= t0."CreatedAt") AND (t0."CreatedAt" < TIMESTAMP '2024-02-01 00:00:00')
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesWithCapturedUtcMoment()
    {
        DateTime cutoff = new(2024, 5, 6, 7, 8, 9, DateTimeKind.Utc);
        Expression query = Orders
            .Where(o => o.CreatedAt >= cutoff)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."CreatedAt" >= TIMESTAMPTZ '2024-05-06 07:08:09+00'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AddsDaysFromColumnBeforeComparingWithNow()
    {
        Expression query = Orders
            .Where(o => o.CreatedAt.AddDays(o.Quantity) < DateTime.UtcNow)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."CreatedAt" + (CAST(t0."Quantity" AS double precision) * INTERVAL '1 day')) < NOW()
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ExtractsMonthOfShiftedMoment()
    {
        Expression query = Orders.Select(o => o.CreatedAt.AddMonths(1).Month).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(EXTRACT(MONTH FROM (t0."CreatedAt" + (1 * INTERVAL '1 month'))) AS integer)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AddsYearsFromIntegerColumnWithoutCast()
    {
        Expression query = Orders
            .Select(o => o.CreatedAt.AddYears(o.Quantity))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CreatedAt" + (t0."Quantity" * INTERVAL '1 year')
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SubtractsNegatedColumnSeconds()
    {
        Expression query = Orders
            .Select(o => o.CreatedAt.AddSeconds(-o.Quantity))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CreatedAt" + (CAST(-t0."Quantity" AS double precision) * INTERVAL '1 second')
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SubtractsTimeSpan()
    {
        Expression query = Orders
            .Select(o => o.CreatedAt - TimeSpan.FromDays(1))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CreatedAt" - INTERVAL 'P1D'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesTimeOfDayWithTimeSpan()
    {
        Expression query = Orders
            .Where(o => o.CreatedAt.TimeOfDay > TimeSpan.FromHours(12))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."CreatedAt" - DATE_TRUNC('day', t0."CreatedAt")) > INTERVAL 'PT12H'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesTimeWithLiteral()
    {
        Expression query = Orders
            .Where(o => o.DeliveryTime > new TimeOnly(12, 30))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."DeliveryTime" > TIME '12:30:00'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersAfternoonDeliveries()
    {
        Expression query = Orders
            .Where(o => o.DeliveryTime.Hour >= 12)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE CAST(EXTRACT(HOUR FROM t0."DeliveryTime") AS integer) >= 12
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ShiftsTimeByFractionalHours()
    {
        Expression query = Orders.Select(o => o.DeliveryTime.AddHours(1.5)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."DeliveryTime" + (1.5 * INTERVAL '1 hour')
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesNullableOffsetWithLiteralOfOtherZone()
    {
        Expression query = Orders
            .Where(o =>
                o.PaidAt > new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.FromHours(3))
            )
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."PaidAt" > TIMESTAMPTZ '2024-01-02 03:04:05+03:00'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesShippingDateWithCreationDate()
    {
        Expression query = Orders
            .Where(o => o.ShippedOn > DateOnly.FromDateTime(o.CreatedAt))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."ShippedOn" > CAST(t0."CreatedAt" AS date)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ShiftsDateByWeekBeforeComparing()
    {
        Expression query = Orders
            .Where(o => o.ShippedOn!.Value.AddDays(7) <= new DateOnly(2024, 6, 30))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."ShippedOn" + 7) <= DATE '2024-06-30'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CountsDaysBetweenDates()
    {
        Expression query = Orders
            .Select(o =>
                o.ShippedOn!.Value.DayNumber
                - DateOnly.FromDateTime(o.CreatedAt).DayNumber
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT (t0."ShippedOn" - DATE '0001-01-01') - (CAST(t0."CreatedAt" AS date) - DATE '0001-01-01')
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void GroupsByCalendarDay()
    {
        Expression query = Orders
            .GroupBy(o => o.CreatedAt.Date)
            .Select(g => new { Day = g.Key, Count = g.Count() })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT DATE_TRUNC('day', t0."CreatedAt") AS "Day", CAST(COUNT(*) AS integer) AS "Count"
                FROM "Order" AS t0
                GROUP BY DATE_TRUNC('day', t0."CreatedAt")
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OrdersByDayDescendingThenDeliveryTime()
    {
        Expression query = Orders
            .OrderByDescending(o => o.CreatedAt.Date)
            .ThenBy(o => o.DeliveryTime)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                ORDER BY DATE_TRUNC('day', t0."CreatedAt") DESC, t0."DeliveryTime"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByCurrentYear()
    {
        Expression query = Orders
            .Where(o => o.CreatedAt.Year == DateTime.UtcNow.Year)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE CAST(EXTRACT(YEAR FROM t0."CreatedAt") AS integer) = CAST(EXTRACT(YEAR FROM NOW()) AS integer)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByLastHourOfLocalClock()
    {
        Expression query = Orders
            .Where(o => o.CreatedAt > DateTime.Now.AddHours(-1))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."CreatedAt" > (LOCALTIMESTAMP + (-1 * INTERVAL '1 hour'))
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesLatestPaymentPerCustomer()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new { g.Key, LastPaid = g.Max(o => o.PaidAt) })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId" AS "Key", MAX(t0."PaidAt") AS "LastPaid"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesUtcDateTimeOfOffsetWithTimestamp()
    {
        Expression query = Orders
            .Where(o => o.PaidAt!.Value.UtcDateTime < o.CreatedAt)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."PaidAt" AT TIME ZONE 'UTC') < t0."CreatedAt"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SubstitutesMinimumDateTimeForMissingEarliest()
    {
        Expression query = Customers
            .Select(c => new
            {
                c.Name,
                First = Orders
                    .Where(o => o.CustomerId == c.Id)
                    .Select(o => o.CreatedAt)
                    .DefaultIfEmpty()
                    .Min(),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", COALESCE((
                    SELECT MIN(t1."CreatedAt")
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                ), TIMESTAMP '0001-01-01 00:00:00') AS "First"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
