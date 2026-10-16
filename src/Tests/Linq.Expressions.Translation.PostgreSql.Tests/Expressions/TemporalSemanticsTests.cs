namespace Linq.Expressions.Translation.PostgreSql.Tests.Expressions;

public sealed record TemporalSemanticsTests
{
    [Fact]
    public void AddsHoursToNullableDateTimeOffset()
    {
        Expression query = Orders.Select(o => o.PaidAt!.Value.AddHours(2)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."PaidAt" + (2 * INTERVAL '1 hour')
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AddsMinutesToTime()
    {
        Expression query = Orders.Select(o => o.DeliveryTime.AddMinutes(30)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."DeliveryTime" + (30 * INTERVAL '1 minute')
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AddsHoursToTime()
    {
        Expression query = Orders.Select(o => o.DeliveryTime.AddHours(2)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."DeliveryTime" + (2 * INTERVAL '1 hour')
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AddsSecondsFromIntegerColumn()
    {
        Expression query = Orders
            .Select(o => o.CreatedAt.AddSeconds(o.Quantity))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CreatedAt" + (CAST(t0."Quantity" AS double precision) * INTERVAL '1 second')
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsHourOfNullableDateTimeOffset()
    {
        Expression query = Orders.Select(o => o.PaidAt!.Value.Hour).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(EXTRACT(HOUR FROM t0."PaidAt") AS integer)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByCurrentDateTimeOffset()
    {
        Expression query = Orders
            .Where(o => o.PaidAt < DateTimeOffset.Now)
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
    public void SelectsMillisecond()
    {
        Expression query = Orders.Select(o => o.CreatedAt.Millisecond).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(EXTRACT(MILLISECONDS FROM t0."CreatedAt") AS integer) % 1000
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByMillisecond()
    {
        Expression query = Orders
            .Where(o => o.CreatedAt.Millisecond == 250)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (CAST(EXTRACT(MILLISECONDS FROM t0."CreatedAt") AS integer) % 1000) = 250
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsMicrosecond()
    {
        Expression query = Orders.Select(o => o.CreatedAt.Microsecond).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(EXTRACT(MICROSECONDS FROM t0."CreatedAt") AS integer) % 1000
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsMillisecondOfDateTimeOffset()
    {
        Expression query = Orders.Select(o => o.PaidAt!.Value.Millisecond).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(EXTRACT(MILLISECONDS FROM t0."PaidAt") AS integer) % 1000
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AddsMilliseconds()
    {
        Expression query = Orders
            .Select(o => o.CreatedAt.AddMilliseconds(500))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CreatedAt" + (500 * INTERVAL '1 millisecond')
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SubtractsMilliseconds()
    {
        Expression query = Orders
            .Select(o => o.CreatedAt.AddMilliseconds(-500))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CreatedAt" + (-500 * INTERVAL '1 millisecond')
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AddsMillisecondsFromIntegerColumn()
    {
        Expression query = Orders
            .Select(o => o.CreatedAt.AddMilliseconds(o.Quantity))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CreatedAt" + (CAST(t0."Quantity" AS double precision) * INTERVAL '1 millisecond')
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AddsTenTicksAsOneMicrosecond()
    {
        Expression query = Orders.Select(o => o.CreatedAt.AddTicks(10)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CreatedAt" + (1 * INTERVAL '1 microsecond')
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AddsTimeSpan()
    {
        Expression query = Orders
            .Select(o => o.CreatedAt.Add(TimeSpan.FromHours(2)))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CreatedAt" + INTERVAL 'PT2H'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AddsTimeSpanWithOperator()
    {
        Expression query = Orders
            .Select(o => o.CreatedAt + TimeSpan.FromHours(2))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CreatedAt" + INTERVAL 'PT2H'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AddsTimeSpanToTime()
    {
        Expression query = Orders
            .Select(o => o.DeliveryTime.Add(TimeSpan.FromMinutes(30)))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."DeliveryTime" + INTERVAL 'PT30M'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AddsMonthsToDateOnly()
    {
        Expression query = Orders.Select(o => o.ShippedOn!.Value.AddMonths(1)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(t0."ShippedOn" + (1 * INTERVAL '1 month') AS date)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AddsYearsToDateOnly()
    {
        Expression query = Orders.Select(o => o.ShippedOn!.Value.AddYears(1)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(t0."ShippedOn" + (1 * INTERVAL '1 year') AS date)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ConvertsDateTimeToTime()
    {
        Expression query = Orders
            .Select(o => TimeOnly.FromDateTime(o.CreatedAt))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(t0."CreatedAt" AS time)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
