using System.Globalization;

namespace Linq.Expressions.Translation.PostgreSql.Tests.Expressions;

// Dates and times built from parts at the edges: captured parts and kinds fold into
// literals, every overload with a Calendar or a Local kind is refused, the guards
// cover every part MAKE_DATE, MAKE_TIME and MAKE_TIMESTAMP would accept where .NET
// throws, and the values compose with the operators that read them.
public sealed record TemporalConstructionEdgeCaseTests
{
    [Fact]
    public void FoldsCapturedPartsIntoLiterals()
    {
        int year = 2024;
        int month = 2;
        int day = 29;
        Expression query = Orders
            .Where(o =>
                o.ShippedOn == new DateOnly(year, month, day)
                || o.CreatedAt < new DateTime(year, month, day, 23, 59, 59, 999, 999)
                || o.DeliveryTime == new TimeOnly(month, day, 1, 2, 3)
            )
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."ShippedOn" IS NOT DISTINCT FROM DATE '2024-02-29') OR (t0."CreatedAt" < TIMESTAMP '2024-02-29 23:59:59.999999') OR (t0."DeliveryTime" = TIME '02:29:01.002003')
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FoldsCapturedKind()
    {
        DateTimeKind kind = DateTimeKind.Utc;
        Expression query = Orders
            .Select(o => new DateTime(2024, 1, o.Id, 0, 0, 0, kind))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                "SELECT MAKE_TIMESTAMP(2024, 1, t0.\"Id\", 0, 0, 0) AT TIME ZONE 'UTC' FROM \"Order\" AS t0"
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FoldsDefaultConstruction()
    {
        Expression query = Orders
            .Select(o => new
            {
                o.Id,
                Day = new DateOnly(),
                Time = new TimeOnly(),
                Moment = new DateTime(),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", DATE '0001-01-01' AS "Day", TIME '00:00:00' AS "Time", TIMESTAMP '0001-01-01 00:00:00' AS "Moment"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    public static TheoryData<Expression<Func<Order, object>>, string> Guarded =>
        new()
        {
            // Every part of a time: the hour carries the range of the seconds and of
            // both fractions; MAKE_TIME rejects the minute itself.
            {
                o => new TimeOnly(o.Quantity, o.Id, o.Id, o.Id, o.Id),
                "MAKE_TIME(CASE WHEN (t0.\"Quantity\" >= 0) AND (t0.\"Quantity\" <= 23) AND (t0.\"Id\" >= 0) AND (t0.\"Id\" <= 59) AND (t0.\"Id\" >= 0) AND (t0.\"Id\" <= 999) AND (t0.\"Id\" >= 0) AND (t0.\"Id\" <= 999) THEN t0.\"Quantity\" ELSE -1 END, t0.\"Id\", (t0.\"Id\" + (t0.\"Id\" * 0.001)) + (t0.\"Id\" * 0.000001))"
            },
            {
                o => new TimeOnly(0, 0, 0, 0, o.Id),
                "MAKE_TIME(CASE WHEN (t0.\"Id\" >= 0) AND (t0.\"Id\" <= 999) THEN 0 ELSE -1 END, 0, t0.\"Id\" * 0.000001)"
            },
            // The year of a timestamp carries the ranges of the hour, the seconds and
            // both fractions.
            {
                o => new DateTime(o.Id, 1, 1, o.Quantity, 0, o.Id, o.Id, o.Id),
                "MAKE_TIMESTAMP(CASE WHEN (t0.\"Id\" >= 1) AND (t0.\"Id\" <= 9999) AND (t0.\"Quantity\" >= 0) AND (t0.\"Quantity\" <= 23) AND (t0.\"Id\" >= 0) AND (t0.\"Id\" <= 59) AND (t0.\"Id\" >= 0) AND (t0.\"Id\" <= 999) AND (t0.\"Id\" >= 0) AND (t0.\"Id\" <= 999) THEN t0.\"Id\" ELSE 0 END, 1, 1, t0.\"Quantity\", 0, (t0.\"Id\" + (t0.\"Id\" * 0.001)) + (t0.\"Id\" * 0.000001))"
            },
            {
                o => new DateTime(2024, 1, 1, 0, 0, 0, 0, o.Id, DateTimeKind.Utc),
                "MAKE_TIMESTAMP(CASE WHEN (t0.\"Id\" >= 0) AND (t0.\"Id\" <= 999) THEN 2024 ELSE 0 END, 1, 1, 0, 0, t0.\"Id\" * 0.000001) AT TIME ZONE 'UTC'"
            },
            // Constant parts in range need no guard, out of range they are the invalid
            // part itself.
            {
                o => new DateTime(9999, 12, 31, 23, 59, 59, 999, o.Id),
                "MAKE_TIMESTAMP(CASE WHEN (t0.\"Id\" >= 0) AND (t0.\"Id\" <= 999) THEN 9999 ELSE 0 END, 12, 31, 23, 59, (t0.\"Id\" * 0.000001) + 59.999)"
            },
            { o => new DateOnly(-1, 1, o.Id), "MAKE_DATE(0, 1, t0.\"Id\")" },
            { o => new TimeOnly(-1, o.Id), "MAKE_TIME(-1, t0.\"Id\", 0)" },
            { o => new TimeOnly(0, o.Id, 0, -1), "MAKE_TIME(-1, t0.\"Id\", -0.001)" },
            {
                o => new DateTime(2024, 1, 1, 0, o.Id, 0, 0, 1000),
                "MAKE_TIMESTAMP(0, 1, 1, 0, t0.\"Id\", 0.001000)"
            },
            // Ticks and day numbers.
            {
                o => new TimeOnly(o.Points * 10),
                "MAKE_TIME(CASE WHEN ((t0.\"Points\" * 10) >= 0) AND ((t0.\"Points\" * 10) <= 863999999999) AND (((t0.\"Points\" * 10) % 10) = 0) THEN 0 ELSE -1 END, 0, 0) + (((t0.\"Points\" * 10) / 10) * INTERVAL '1 microsecond')"
            },
            {
                o => DateOnly.FromDayNumber(o.Id).ToDateTime(new TimeOnly(o.Quantity, 0)),
                "(MAKE_DATE(CASE WHEN (t0.\"Id\" >= 0) AND (t0.\"Id\" <= 3652058) THEN 1 ELSE 0 END, 1, 1) + t0.\"Id\") + MAKE_TIME(CASE WHEN (t0.\"Quantity\" >= 0) AND (t0.\"Quantity\" <= 23) THEN t0.\"Quantity\" ELSE -1 END, 0, 0)"
            },
        };

    [Theory]
    [MemberData(nameof(Guarded))]
    public void GuardsEveryPart(Expression<Func<Order, object>> selector, string sql)
    {
        Expression query = Orders.Select(selector).Expression;

        Assert.Equal(
            Sql.Valid($"SELECT {sql} FROM \"Order\" AS t0"),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComposesValuesFromPartsWithOperators()
    {
        Expression query = Orders
            .Where(o => new TimeOnly(o.Quantity, 0) < o.DeliveryTime)
            .GroupBy(o => new DateOnly(2024, 1 + o.Quantity, 1))
            .OrderBy(g => g.Key)
            .Select(g => new
            {
                g.Key,
                Latest = g.Max(o => new DateTime(o.ShippedOn!.Value, o.DeliveryTime)),
            })
            .Skip(1)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT MAKE_DATE(2024, 1 + t0."Quantity", 1) AS "Key", MAX(t0."ShippedOn" + t0."DeliveryTime") AS "Latest"
                FROM "Order" AS t0
                WHERE MAKE_TIME(CASE WHEN (t0."Quantity" >= 0) AND (t0."Quantity" <= 23) THEN t0."Quantity" ELSE -1 END, 0, 0) < t0."DeliveryTime"
                GROUP BY MAKE_DATE(2024, 1 + t0."Quantity", 1)
                ORDER BY MAKE_DATE(2024, 1 + t0."Quantity", 1)
                OFFSET 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsInMemoryDatesOnValuesFromParts()
    {
        DateOnly[] months = [new DateOnly(2024, 1, 1), new DateOnly(2024, 2, 1)];
        Expression query = Orders
            .Join(
                months,
                o => new DateOnly(o.CreatedAt.Year, o.CreatedAt.Month, 1),
                m => m,
                (o, m) => o.Id
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                INNER JOIN (VALUES (DATE '2024-01-01'), (DATE '2024-02-01')) AS t1 ("Value")
                ON MAKE_DATE(CASE WHEN (CAST(EXTRACT(YEAR FROM t0."CreatedAt") AS integer) >= 1) AND (CAST(EXTRACT(YEAR FROM t0."CreatedAt") AS integer) <= 9999) THEN CAST(EXTRACT(YEAR FROM t0."CreatedAt") AS integer) ELSE 0 END, CAST(EXTRACT(MONTH FROM t0."CreatedAt") AS integer), 1) = t1."Value"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    // A captured part out of range in a branch no row may reach is left to the
    // database, deferred so that the planner does not fold it.
    [Fact]
    public void DefersCapturedPartOutOfRange()
    {
        int month = 13;
        Expression query = Orders
            .Select(o => o.Id > 100 ? new DateOnly(2024, month, 1) : DateOnly.MinValue)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CASE WHEN t0."Id" > 100 THEN MAKE_DATE(CASE WHEN ((SELECT 2024) >= 1) AND ((SELECT 2024) <= 9999) THEN (SELECT 2024) ELSE 0 END, 13, 1) ELSE DATE '0001-01-01' END
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    // A DateTime constant finer than a microsecond is refused, as PostgreSQL keeps
    // none; one built from constant ticks is such a constant.
    [Fact]
    public void RejectsConstantTicksBelowMicrosecond()
    {
        Expression query = Orders
            .Where(o => o.CreatedAt > new DateTime(DateTime.UnixEpoch.Ticks + 5))
            .Select(o => o.Id)
            .Expression;

        _ = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
    }

    public static TheoryData<Expression<Func<Order, object>>, string> Rejected =>
        new()
        {
            {
                o => new DateTime(2024, 1, o.Id, 0, 0, 0, 0, new GregorianCalendar()),
                "Calendar"
            },
            {
                o => new DateTime(2024, 1, o.Id, 0, 0, 0, 0, 0, new GregorianCalendar()),
                "Calendar"
            },
            {
                o => new DateTime(
                    2024,
                    1,
                    o.Id,
                    0,
                    0,
                    0,
                    0,
                    new GregorianCalendar(),
                    DateTimeKind.Utc
                ),
                "Calendar"
            },
            { o => new DateTime(2024, 1, o.Id, 0, 0, 0, 0, DateTimeKind.Local), "Local" },
            {
                o => new DateTime(2024, 1, o.Id, 0, 0, 0, 0, 0, DateTimeKind.Local),
                "Local"
            },
            {
                o => new DateTime(o.ShippedOn!.Value, o.DeliveryTime, DateTimeKind.Local),
                "Local"
            },
            {
                o => new DateTime(
                    o.ShippedOn!.Value,
                    o.DeliveryTime,
                    o.IsPaid ? DateTimeKind.Utc : DateTimeKind.Unspecified
                ),
                "constant kind"
            },
            {
                o =>
                    o.ShippedOn!.Value.ToDateTime(
                        o.DeliveryTime,
                        o.IsPaid ? DateTimeKind.Utc : DateTimeKind.Unspecified
                    ),
                "constant kind"
            },
            { o => new DateTime(o.Points, DateTimeKind.Local), "Local" },
            {
                o => new DateTime(
                    o.Points,
                    o.IsPaid ? DateTimeKind.Utc : DateTimeKind.Unspecified
                ),
                "constant kind"
            },
        };

    [Theory]
    [MemberData(nameof(Rejected))]
    public void RejectsConstructionWithoutEquivalent(
        Expression<Func<Order, object>> selector,
        string message
    )
    {
        Expression query = Orders.Select(selector).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains(message, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsCapturedLocalKind()
    {
        DateTimeKind kind = DateTimeKind.Local;
        Expression query = Orders
            .Select(o => new DateTime(2024, 1, o.Id, 0, 0, 0, kind))
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Local", exception.Message, StringComparison.Ordinal);
    }

    public static TheoryData<Expression<Func<Order, object>>> Unconstructed =>
        [
            o => new DateTimeOffset(2024, 1, o.Id, 0, 0, 0, TimeSpan.Zero),
            o => new DateTimeOffset(o.CreatedAt),
            o => new DateOnly(2024, 1, o.Id).ToDateTime(o.DeliveryTime).Kind,
        ];

    // What has no translation is refused with NotSupportedException, not another
    // exception.
    [Theory]
    [MemberData(nameof(Unconstructed))]
    public void RejectsOtherConstruction(Expression<Func<Order, object>> selector)
    {
        Expression query = Orders.Select(selector).Expression;

        _ = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
    }
}
