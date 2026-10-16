namespace Linq.Expressions.Translation.PostgreSql.Tests.Expressions;

public sealed record DateTimeTests
{
    [Fact]
    public void SelectsYearOfDateTime()
    {
        Expression query = Orders.Select(o => o.CreatedAt.Year).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(EXTRACT(YEAR FROM t0."CreatedAt") AS integer)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsMonthOfDateTime()
    {
        Expression query = Orders.Select(o => o.CreatedAt.Month).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(EXTRACT(MONTH FROM t0."CreatedAt") AS integer)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsDayOfDateTime()
    {
        Expression query = Orders.Select(o => o.CreatedAt.Day).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(EXTRACT(DAY FROM t0."CreatedAt") AS integer)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsHourOfDateTime()
    {
        Expression query = Orders.Select(o => o.CreatedAt.Hour).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(EXTRACT(HOUR FROM t0."CreatedAt") AS integer)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsMinuteOfDateTime()
    {
        Expression query = Orders.Select(o => o.CreatedAt.Minute).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(EXTRACT(MINUTE FROM t0."CreatedAt") AS integer)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsSecondOfDateTime()
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
    public void SelectsDayOfYearOfDateTime()
    {
        Expression query = Orders.Select(o => o.CreatedAt.DayOfYear).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(EXTRACT(DOY FROM t0."CreatedAt") AS integer)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsDayOfWeekOfDateTime()
    {
        Expression query = Orders.Select(o => o.CreatedAt.DayOfWeek).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(EXTRACT(DOW FROM t0."CreatedAt") AS integer)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsDateOfDateTime()
    {
        Expression query = Orders.Select(o => o.CreatedAt.Date).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT DATE_TRUNC('day', t0."CreatedAt")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsDatePartsNextToColumn()
    {
        Expression query = Orders
            .Select(o => new
            {
                o.Id,
                o.CreatedAt.Year,
                o.CreatedAt.Month,
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id",
                CAST(EXTRACT(YEAR FROM t0."CreatedAt") AS integer) AS "Year",
                CAST(EXTRACT(MONTH FROM t0."CreatedAt") AS integer) AS "Month"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AddsDays()
    {
        Expression query = Orders.Select(o => o.CreatedAt.AddDays(7)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CreatedAt" + (7 * INTERVAL '1 day')
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SubtractsDays()
    {
        Expression query = Orders.Select(o => o.CreatedAt.AddDays(-7)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CreatedAt" + (-7 * INTERVAL '1 day')
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AddsFractionalDays()
    {
        Expression query = Orders.Select(o => o.CreatedAt.AddDays(1.5)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CreatedAt" + (1.5 * INTERVAL '1 day')
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AddsMonths()
    {
        Expression query = Orders.Select(o => o.CreatedAt.AddMonths(3)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CreatedAt" + (3 * INTERVAL '1 month')
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AddsYears()
    {
        Expression query = Orders.Select(o => o.CreatedAt.AddYears(1)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CreatedAt" + (1 * INTERVAL '1 year')
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AddsHours()
    {
        Expression query = Orders.Select(o => o.CreatedAt.AddHours(2)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CreatedAt" + (2 * INTERVAL '1 hour')
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AddsMinutes()
    {
        Expression query = Orders.Select(o => o.CreatedAt.AddMinutes(30)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CreatedAt" + (30 * INTERVAL '1 minute')
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AddsSeconds()
    {
        Expression query = Orders.Select(o => o.CreatedAt.AddSeconds(45)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CreatedAt" + (45 * INTERVAL '1 second')
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AddsDaysFromColumn()
    {
        Expression query = Orders.Select(o => o.CreatedAt.AddDays(o.Quantity)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CreatedAt" + (CAST(t0."Quantity" AS double precision) * INTERVAL '1 day')
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AddsMonthsFromColumn()
    {
        Expression query = Orders
            .Select(o => o.CreatedAt.AddMonths(o.Quantity))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CreatedAt" + (t0."Quantity" * INTERVAL '1 month')
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AddsYearsFromColumn()
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
    public void ChainsDateAdditions()
    {
        Expression query = Orders
            .Select(o => o.CreatedAt.AddDays(1).AddHours(2))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT (t0."CreatedAt" + (1 * INTERVAL '1 day')) + (2 * INTERVAL '1 hour')
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TruncatesAfterAddingDays()
    {
        Expression query = Orders.Select(o => o.CreatedAt.AddDays(1).Date).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT DATE_TRUNC('day', t0."CreatedAt" + (1 * INTERVAL '1 day'))
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ExtractsPartOfShiftedDate()
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
    public void SubtractsDateTimes()
    {
        Expression query = Orders
            .Select(o => o.CreatedAt - new DateTime(2024, 1, 1))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CreatedAt" - TIMESTAMP '2024-01-01 00:00:00'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SubtractsConstantDateFromTimeOfDay()
    {
        Expression query = Orders
            .Select(o => o.DeliveryTime - new TimeOnly(8, 0))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."DeliveryTime" - TIME '08:00:00'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByYear()
    {
        Expression query = Orders
            .Where(o => o.CreatedAt.Year == 2024)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE CAST(EXTRACT(YEAR FROM t0."CreatedAt") AS integer) = 2024
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByMonthRange()
    {
        Expression query = Orders
            .Where(o => (o.CreatedAt.Month >= 6) && (o.CreatedAt.Month <= 8))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (CAST(EXTRACT(MONTH FROM t0."CreatedAt") AS integer) >= 6) AND (CAST(EXTRACT(MONTH FROM t0."CreatedAt") AS integer) <= 8)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByDayOfWeek()
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
    public void FiltersByDayOfWeekOutsideWeekend()
    {
        Expression query = Orders
            .Where(o =>
                (o.CreatedAt.DayOfWeek != DayOfWeek.Saturday)
                && (o.CreatedAt.DayOfWeek != DayOfWeek.Sunday)
            )
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (CAST(EXTRACT(DOW FROM t0."CreatedAt") AS integer) <> 6) AND (CAST(EXTRACT(DOW FROM t0."CreatedAt") AS integer) <> 0)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByHourOfDay()
    {
        Expression query = Orders
            .Where(o => o.CreatedAt.Hour >= 9)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE CAST(EXTRACT(HOUR FROM t0."CreatedAt") AS integer) >= 9
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByDate()
    {
        Expression query = Orders
            .Where(o => o.CreatedAt.Date == new DateTime(2024, 3, 1))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE DATE_TRUNC('day',
                t0."CreatedAt") = TIMESTAMP '2024-03-01 00:00:00'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByShiftedDate()
    {
        Expression query = Orders
            .Where(o => o.CreatedAt.AddDays(7) > new DateTime(2024, 3, 1))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."CreatedAt" + (7 * INTERVAL '1 day')) > TIMESTAMP '2024-03-01 00:00:00'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByDateTimeRange()
    {
        Expression query = Orders
            .Where(o =>
                (o.CreatedAt >= new DateTime(2024, 1, 1))
                && (o.CreatedAt < new DateTime(2025, 1, 1))
            )
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."CreatedAt" >= TIMESTAMP '2024-01-01 00:00:00') AND (t0."CreatedAt" < TIMESTAMP '2025-01-01 00:00:00')
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByCapturedDate()
    {
        DateTime start = new(2024, 2, 15, 8, 30, 0);

        Expression query = Orders
            .Where(o => o.CreatedAt >= start)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."CreatedAt" >= TIMESTAMP '2024-02-15 08:30:00'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByCapturedDateShiftedInMemory()
    {
        DateTime start = new(2024, 2, 15);

        Expression query = Orders
            .Where(o => o.CreatedAt >= start.AddDays(7))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."CreatedAt" >= TIMESTAMP '2024-02-22 00:00:00'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByDateTimeWithFractionalSeconds()
    {
        Expression query = Orders
            .Where(o => o.CreatedAt > new DateTime(2024, 1, 1, 0, 0, 0, 250))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."CreatedAt" > TIMESTAMP '2024-01-01 00:00:00.25'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByCurrentLocalTime()
    {
        Expression query = Orders
            .Where(o => o.CreatedAt < DateTime.Now)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."CreatedAt" < LOCALTIMESTAMP
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByCurrentUtcTime()
    {
        Expression query = Orders
            .Where(o => o.CreatedAt < DateTime.UtcNow)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."CreatedAt" < NOW()
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByCurrentTimeShiftedIntoPast()
    {
        Expression query = Orders
            .Where(o => o.CreatedAt > DateTime.UtcNow.AddDays(-30))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."CreatedAt" > (NOW() + (-30 * INTERVAL '1 day'))
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByOffsetTimeBeforeNow()
    {
        Expression query = Orders
            .Where(o => o.PaidAt < DateTimeOffset.UtcNow)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."PaidAt" < NOW()
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByElapsedTime()
    {
        Expression query = Orders
            .Where(o => (o.CreatedAt - new DateTime(2024, 1, 1)) > TimeSpan.FromDays(30))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."CreatedAt" - TIMESTAMP '2024-01-01 00:00:00') > INTERVAL 'P30D'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByElapsedTimeWithTimePart()
    {
        Expression query = Orders
            .Where(o =>
                (o.CreatedAt - new DateTime(2024, 1, 1)) < TimeSpan.FromMinutes(90)
            )
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."CreatedAt" - TIMESTAMP '2024-01-01 00:00:00') < INTERVAL 'PT1H30M'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByYearOfNullableDateTimeOffset()
    {
        Expression query = Orders
            .Where(o => o.PaidAt!.Value.Year == 2024)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE CAST(EXTRACT(YEAR FROM t0."PaidAt") AS integer) = 2024
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByNullableDateTimeOffsetRange()
    {
        Expression query = Orders
            .Where(o =>
                (o.PaidAt >= new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero))
                && (o.PaidAt < new DateTimeOffset(2024, 2, 1, 0, 0, 0, TimeSpan.Zero))
            )
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."PaidAt" >= TIMESTAMPTZ '2024-01-01 00:00:00+00') AND (t0."PaidAt" < TIMESTAMPTZ '2024-02-01 00:00:00+00')
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByOffsetTimeWithNonZeroOffset()
    {
        Expression query = Orders
            .Where(o =>
                o.PaidAt > new DateTimeOffset(2024, 1, 1, 10, 0, 0, TimeSpan.FromHours(2))
            )
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."PaidAt" > TIMESTAMPTZ '2024-01-01 10:00:00+02:00'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByHasValueOfShippedOn()
    {
        Expression query = Orders
            .Where(o => o.ShippedOn.HasValue)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."ShippedOn" IS NOT NULL
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsYearOfDateOnly()
    {
        Expression query = Orders.Select(o => o.ShippedOn!.Value.Year).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(EXTRACT(YEAR FROM t0."ShippedOn") AS integer)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsMonthOfDateOnly()
    {
        Expression query = Orders.Select(o => o.ShippedOn!.Value.Month).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(EXTRACT(MONTH FROM t0."ShippedOn") AS integer)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsDayOfDateOnly()
    {
        Expression query = Orders.Select(o => o.ShippedOn!.Value.Day).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(EXTRACT(DAY FROM t0."ShippedOn") AS integer)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsDayOfWeekOfDateOnly()
    {
        Expression query = Orders.Select(o => o.ShippedOn!.Value.DayOfWeek).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(EXTRACT(DOW FROM t0."ShippedOn") AS integer)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsDayOfYearOfDateOnly()
    {
        Expression query = Orders.Select(o => o.ShippedOn!.Value.DayOfYear).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(EXTRACT(DOY FROM t0."ShippedOn") AS integer)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AddsDaysToDateOnly()
    {
        Expression query = Orders.Select(o => o.ShippedOn!.Value.AddDays(7)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."ShippedOn" + 7
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SubtractsDaysFromDateOnly()
    {
        Expression query = Orders.Select(o => o.ShippedOn!.Value.AddDays(-1)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."ShippedOn" + -1
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AddsDaysFromColumnToDateOnly()
    {
        Expression query = Orders
            .Select(o => o.ShippedOn!.Value.AddDays(o.Quantity))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."ShippedOn" + t0."Quantity"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ConvertsDateTimeToDateOnly()
    {
        Expression query = Orders
            .Select(o => DateOnly.FromDateTime(o.CreatedAt))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(t0."CreatedAt" AS date)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AddsDaysToConvertedDateOnly()
    {
        Expression query = Orders
            .Select(o => DateOnly.FromDateTime(o.CreatedAt).AddDays(1))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(t0."CreatedAt" AS date) + 1
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesDateOnlyWithConvertedDateTime()
    {
        Expression query = Orders
            .Where(o => o.ShippedOn == DateOnly.FromDateTime(o.CreatedAt))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."ShippedOn" IS NOT DISTINCT FROM CAST(t0."CreatedAt" AS date)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByDateOnlyConstant()
    {
        Expression query = Orders
            .Where(o => o.ShippedOn == new DateOnly(2024, 5, 1))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."ShippedOn" IS NOT DISTINCT FROM DATE '2024-05-01'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByDateOnlyRange()
    {
        Expression query = Orders
            .Where(o =>
                (o.ShippedOn >= new DateOnly(2024, 1, 1))
                && (o.ShippedOn <= new DateOnly(2024, 12, 31))
            )
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."ShippedOn" >= DATE '2024-01-01') AND (t0."ShippedOn" <= DATE '2024-12-31')
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByDateOnlyMonth()
    {
        Expression query = Orders
            .Where(o => o.ShippedOn!.Value.Month == 12)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE CAST(EXTRACT(MONTH FROM t0."ShippedOn") AS integer) = 12
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByCapturedDateOnly()
    {
        DateOnly day = new(2024, 2, 29);

        Expression query = Orders
            .Where(o => o.ShippedOn > day)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."ShippedOn" > DATE '2024-02-29'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByDateOnlyAfterShift()
    {
        Expression query = Orders
            .Where(o => o.ShippedOn!.Value.AddDays(7) < new DateOnly(2024, 6, 1))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."ShippedOn" + 7) < DATE '2024-06-01'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsHourOfTime()
    {
        Expression query = Orders.Select(o => o.DeliveryTime.Hour).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(EXTRACT(HOUR FROM t0."DeliveryTime") AS integer)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsMinuteOfTime()
    {
        Expression query = Orders.Select(o => o.DeliveryTime.Minute).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(EXTRACT(MINUTE FROM t0."DeliveryTime") AS integer)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsSecondOfTime()
    {
        Expression query = Orders.Select(o => o.DeliveryTime.Second).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(TRUNC(EXTRACT(SECOND FROM t0."DeliveryTime")) AS integer)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByTimeWindow()
    {
        Expression query = Orders
            .Where(o =>
                (o.DeliveryTime >= new TimeOnly(9, 0))
                && (o.DeliveryTime < new TimeOnly(17, 0))
            )
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."DeliveryTime" >= TIME '09:00:00') AND (t0."DeliveryTime" < TIME '17:00:00')
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByTimeWithFraction()
    {
        Expression query = Orders
            .Where(o => o.DeliveryTime == new TimeOnly(9, 30, 15, 250))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."DeliveryTime" = TIME '09:30:15.25'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByHourOfTime()
    {
        Expression query = Orders
            .Where(o => o.DeliveryTime.Hour < 12)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE CAST(EXTRACT(HOUR FROM t0."DeliveryTime") AS integer) < 12
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByMidnight()
    {
        Expression query = Orders
            .Where(o => o.DeliveryTime == TimeOnly.MinValue)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."DeliveryTime" = TIME '00:00:00'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OrdersByDate()
    {
        Expression query = Orders
            .OrderBy(o => o.CreatedAt.Date)
            .ThenByDescending(o => o.CreatedAt)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                ORDER BY DATE_TRUNC('day', t0."CreatedAt"), t0."CreatedAt" DESC
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OrdersByNullableDateOnly()
    {
        Expression query = Orders.OrderBy(o => o.ShippedOn).Select(o => o.Id).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                ORDER BY t0."ShippedOn" NULLS FIRST
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void GroupsByYearAndMonth()
    {
        Expression query = Orders
            .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Revenue = g.Sum(o => o.Total),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(EXTRACT(YEAR FROM t0."CreatedAt") AS integer) AS "Year",
                CAST(EXTRACT(MONTH FROM t0."CreatedAt") AS integer) AS "Month",
                COALESCE(SUM(t0."Total"), 0) AS "Revenue"
                FROM "Order" AS t0
                GROUP BY CAST(EXTRACT(YEAR FROM t0."CreatedAt") AS integer),
                CAST(EXTRACT(MONTH FROM t0."CreatedAt") AS integer)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void GroupsByDate()
    {
        Expression query = Orders
            .GroupBy(o => o.CreatedAt.Date)
            .Select(g => new { Day = g.Key, Count = g.Count() })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT DATE_TRUNC('day', t0."CreatedAt") AS "Day",
                CAST(COUNT(*) AS integer) AS "Count"
                FROM "Order" AS t0
                GROUP BY DATE_TRUNC('day', t0."CreatedAt")
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void GroupsByHourOfTime()
    {
        Expression query = Orders
            .GroupBy(o => o.DeliveryTime.Hour)
            .Select(g => new { Hour = g.Key, Count = g.Count() })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(EXTRACT(HOUR FROM t0."DeliveryTime") AS integer) AS "Hour",
                CAST(COUNT(*) AS integer) AS "Count"
                FROM "Order" AS t0
                GROUP BY CAST(EXTRACT(HOUR FROM t0."DeliveryTime") AS integer)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CountsOrdersOfYear()
    {
        Expression query = Orders.Executed(q => q.Count(o => o.CreatedAt.Year == 2024));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COUNT(*) AS integer)
                FROM "Order" AS t0
                WHERE CAST(EXTRACT(YEAR FROM t0."CreatedAt") AS integer) = 2024
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FindsLatestOrderDay()
    {
        Expression query = Orders.Executed(q => q.Max(o => o.CreatedAt.Date));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT MAX(DATE_TRUNC('day', t0."CreatedAt"))
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SumsHoursOfDay()
    {
        Expression query = Orders.Executed(q => q.Sum(o => o.CreatedAt.Hour));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COALESCE(SUM(CAST(EXTRACT(HOUR FROM t0."CreatedAt") AS integer)), 0) AS integer)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
