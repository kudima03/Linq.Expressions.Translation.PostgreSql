#pragma warning disable xUnit1004 // skipped tests record SQL the translator does not produce yet

namespace Linq.Expressions.Translation.PostgreSql.Tests.Expressions;

public sealed record TickArithmeticTests
{
    [Fact(Skip = "DateTime.Ticks is not translated (#47).")]
    public void SelectsTicksOfDateTime()
    {
        Expression query = Orders.Select(o => o.CreatedAt.Ticks).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(EXTRACT(EPOCH FROM (t0."CreatedAt" - TIMESTAMP '0001-01-01 00:00:00')) * 10000000 AS bigint)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "TimeOnly.Ticks is not translated (#47).")]
    public void SelectsTicksOfTimeOnly()
    {
        Expression query = Orders.Select(o => o.DeliveryTime.Ticks).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(EXTRACT(EPOCH FROM t0."DeliveryTime") * 10000000 AS bigint)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "TimeSpan.FromTicks of a column is not translated (#47).")]
    public void CreatesTimeSpanFromColumnTicks()
    {
        Expression query = Orders.Select(o => TimeSpan.FromTicks(o.Points)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT (t0."Points" / 10) * INTERVAL '1 microsecond'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "TimeSpan.FromTicks of a column is not translated (#47).")]
    public void AddsTimeSpanFromColumnTicksToTimeOnly()
    {
        Expression query = Orders
            .Select(o => o.DeliveryTime.Add(TimeSpan.FromTicks(o.Points)))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."DeliveryTime" + ((t0."Points" / 10) * INTERVAL '1 microsecond')
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "DateTime.AddTicks of a column is not translated (#47).")]
    public void AddsColumnTicksToDateTime()
    {
        Expression query = Orders.Select(o => o.CreatedAt.AddTicks(o.Points)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CreatedAt" + ((t0."Points" / 10) * INTERVAL '1 microsecond')
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "DateTime is not constructed from column ticks (#47).")]
    public void CreatesDateTimeFromColumnTicks()
    {
        Expression query = Orders.Select(o => new DateTime(o.Points)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT (TIMESTAMP '0001-01-01 00:00:00' + ((t0."Points" / 864000000000) * INTERVAL '1 day')) + (((t0."Points" % 864000000000) / 10) * INTERVAL '1 microsecond')
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "DateTime is not constructed from column ticks (#47).")]
    public void CreatesUtcDateTimeFromColumnTicks()
    {
        Expression query = Orders
            .Select(o => new DateTime(o.Points, DateTimeKind.Utc))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT ((TIMESTAMP '0001-01-01 00:00:00' + ((t0."Points" / 864000000000) * INTERVAL '1 day')) + (((t0."Points" % 864000000000) / 10) * INTERVAL '1 microsecond')) AT TIME ZONE 'UTC'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "Timestamps are not averaged through their ticks (#47).")]
    public void AveragesTimestampsThroughTicks()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                AverageCreatedAt = new DateTime(
                    (long)(g.Average(o => o.CreatedAt.Ticks / 10m) * 10)
                ),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId",
                (TIMESTAMP '0001-01-01 00:00:00' + ((CAST(TRUNC(AVG(CAST(CAST(EXTRACT(EPOCH FROM (t0."CreatedAt" - TIMESTAMP '0001-01-01 00:00:00')) * 10000000 AS bigint) AS numeric) / 10) * 10) AS bigint) / 864000000000) * INTERVAL '1 day')) + (((CAST(TRUNC(AVG(CAST(CAST(EXTRACT(EPOCH FROM (t0."CreatedAt" - TIMESTAMP '0001-01-01 00:00:00')) * 10000000 AS bigint) AS numeric) / 10) * 10) AS bigint) % 864000000000) / 10) * INTERVAL '1 microsecond') AS "AverageCreatedAt"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
