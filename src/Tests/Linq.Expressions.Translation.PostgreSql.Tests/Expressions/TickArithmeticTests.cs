namespace Linq.Expressions.Translation.PostgreSql.Tests.Expressions;

public sealed record TickArithmeticTests
{
    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    private const string Moment =
        "(TIMESTAMP '0001-01-01 00:00:00' + ((t0.\"Points\" / 864000000000) * INTERVAL '1 day')) + (((t0.\"Points\" % 864000000000) / 10) * INTERVAL '1 microsecond')";

    private const string NullableMoment =
        "(TIMESTAMP '0001-01-01 00:00:00' + ((CAST(t0.\"Priority\" AS bigint) / 864000000000) * INTERVAL '1 day')) + (((CAST(t0.\"Priority\" AS bigint) % 864000000000) / 10) * INTERVAL '1 microsecond')";

    // The timestamp is as nullable as its ticks, with and without a kind.
    public static TheoryData<Expression<Func<Order, bool>>, string> Constructed =>
        new()
        {
            {
                o => new DateTime(o.Points) == o.CreatedAt,
                $"({Moment}) = t0.\"CreatedAt\""
            },
            {
                o => new DateTime(o.Priority!.Value) == o.CreatedAt,
                $"({NullableMoment}) IS NOT DISTINCT FROM t0.\"CreatedAt\""
            },
            {
                o => new DateTime(o.Points, DateTimeKind.Utc) == o.CreatedAt,
                $"(({Moment}) AT TIME ZONE 'UTC') = t0.\"CreatedAt\""
            },
            {
                o => new DateTime(o.Priority!.Value, DateTimeKind.Utc) == o.CreatedAt,
                $"(({NullableMoment}) AT TIME ZONE 'UTC') IS NOT DISTINCT FROM t0.\"CreatedAt\""
            },
            {
                o => TimeSpan.FromTicks(o.Points) == TimeSpan.Zero,
                "((t0.\"Points\" / 10) * INTERVAL '1 microsecond') = INTERVAL 'PT0S'"
            },
            {
                o => TimeSpan.FromTicks(o.Priority!.Value) == TimeSpan.Zero,
                "((CAST(t0.\"Priority\" AS bigint) / 10) * INTERVAL '1 microsecond') IS NOT DISTINCT FROM INTERVAL 'PT0S'"
            },
        };

    [Theory]
    [MemberData(nameof(Constructed))]
    public void ComparesValueConstructedFromColumnTicks(
        Expression<Func<Order, bool>> predicate,
        string condition
    )
    {
        Expression query = Orders.Where(predicate).Select(o => o.Id).Expression;

        Assert.Equal(
            Sql.Valid(
                $"""
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE {condition}
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CreatesUnspecifiedDateTimeFromColumnTicks()
    {
        Expression query = Orders
            .Select(o => new DateTime(o.Points, DateTimeKind.Unspecified))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                $"""
                SELECT {Moment}
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RejectsLocalDateTimeFromColumnTicks()
    {
        Expression query = Orders
            .Select(o => new DateTime(o.Points, DateTimeKind.Local))
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Local", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsComputedKindOfDateTimeFromColumnTicks()
    {
        Expression query = Orders
            .Select(o => new DateTime(
                o.Points,
                o.IsPaid ? DateTimeKind.Utc : DateTimeKind.Unspecified
            ))
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("kind", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsTimeSpanFromColumnDays()
    {
        Expression query = Orders.Select(o => TimeSpan.FromDays(o.Points)).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("FromDays", exception.Message, StringComparison.Ordinal);
    }
}
