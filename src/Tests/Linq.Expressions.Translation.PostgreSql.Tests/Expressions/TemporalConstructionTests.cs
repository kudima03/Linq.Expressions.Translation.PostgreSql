using System.Globalization;

namespace Linq.Expressions.Translation.PostgreSql.Tests.Expressions;

// Dates and times built from parts are MAKE_DATE, MAKE_TIME and MAKE_TIMESTAMP; a
// part they would accept where .NET throws turns the year into 0 or the hour into
// -1, which they reject. A constant part in range needs no guard.
public sealed record TemporalConstructionTests
{
    private const string Year =
        "CASE WHEN (t0.\"Quantity\" >= 1) AND (t0.\"Quantity\" <= 9999) THEN t0.\"Quantity\" ELSE 0 END";

    private const string Hour =
        "CASE WHEN (t0.\"Quantity\" >= 0) AND (t0.\"Quantity\" <= 23) THEN t0.\"Quantity\" ELSE -1 END";

    public static TheoryData<Expression<Func<Order, DateOnly>>, string> Dates =>
        new()
        {
            {
                o => new DateOnly(o.Quantity, o.Id, 1),
                $"MAKE_DATE({Year}, t0.\"Id\", 1)"
            },
            {
                o => new DateOnly(2024, o.Id, o.Quantity),
                "MAKE_DATE(2024, t0.\"Id\", t0.\"Quantity\")"
            },
            { o => new DateOnly(0, o.Id, 1), "MAKE_DATE(0, t0.\"Id\", 1)" },
            { o => new DateOnly(10000, o.Id, 1), "MAKE_DATE(0, t0.\"Id\", 1)" },
            { o => new DateOnly(9999, 12, o.Id), "MAKE_DATE(9999, 12, t0.\"Id\")" },
            {
                o => DateOnly.FromDayNumber(o.Id),
                "MAKE_DATE(CASE WHEN (t0.\"Id\" >= 0) AND (t0.\"Id\" <= 3652058) THEN 1 ELSE 0 END, 1, 1) + t0.\"Id\""
            },
            {
                o => DateOnly.FromDayNumber(o.ShippedOn!.Value.DayNumber + 1),
                "MAKE_DATE(CASE WHEN (((t0.\"ShippedOn\" - DATE '0001-01-01') + 1) >= 0) AND (((t0.\"ShippedOn\" - DATE '0001-01-01') + 1) <= 3652058) THEN 1 ELSE 0 END, 1, 1) + ((t0.\"ShippedOn\" - DATE '0001-01-01') + 1)"
            },
        };

    [Theory]
    [MemberData(nameof(Dates))]
    public void BuildsDate(Expression<Func<Order, DateOnly>> selector, string sql)
    {
        Expression query = Orders.Select(selector).Expression;

        Assert.Equal(
            Sql.Valid($"SELECT {sql} FROM \"Order\" AS t0"),
            new PostgreSqlQuery(query).TextValue
        );
    }

    public static TheoryData<Expression<Func<Order, TimeOnly>>, string> Times =>
        new()
        {
            { o => new TimeOnly(o.Quantity, o.Id), $"MAKE_TIME({Hour}, t0.\"Id\", 0)" },
            { o => new TimeOnly(12, o.Id), "MAKE_TIME(12, t0.\"Id\", 0)" },
            { o => new TimeOnly(24, o.Id), "MAKE_TIME(-1, t0.\"Id\", 0)" },
            {
                o => new TimeOnly(12, 0, o.Id),
                "MAKE_TIME(CASE WHEN (t0.\"Id\" >= 0) AND (t0.\"Id\" <= 59) THEN 12 ELSE -1 END, 0, t0.\"Id\")"
            },
            { o => new TimeOnly(12, o.Id, 60), "MAKE_TIME(-1, t0.\"Id\", 60)" },
            { o => new TimeOnly(12, o.Id, 30, 5), "MAKE_TIME(12, t0.\"Id\", 30.005)" },
            { o => new TimeOnly(12, o.Id, 0, 1000), "MAKE_TIME(-1, t0.\"Id\", 1)" },
            {
                o => new TimeOnly(12, 0, 0, o.Id),
                "MAKE_TIME(CASE WHEN (t0.\"Id\" >= 0) AND (t0.\"Id\" <= 999) THEN 12 ELSE -1 END, 0, t0.\"Id\" * 0.001)"
            },
            { o => new TimeOnly(1, o.Id, 2, 3, 4), "MAKE_TIME(1, t0.\"Id\", 2.003004)" },
            {
                o => new TimeOnly(o.Quantity, 1, o.Id, 0, o.Id),
                "MAKE_TIME(CASE WHEN (t0.\"Quantity\" >= 0) AND (t0.\"Quantity\" <= 23) AND (t0.\"Id\" >= 0) AND (t0.\"Id\" <= 59) AND (t0.\"Id\" >= 0) AND (t0.\"Id\" <= 999) THEN t0.\"Quantity\" ELSE -1 END, 1, t0.\"Id\" + (t0.\"Id\" * 0.000001))"
            },
            {
                o => new TimeOnly(o.Points),
                "MAKE_TIME(CASE WHEN (t0.\"Points\" >= 0) AND (t0.\"Points\" <= 863999999999) THEN 0 ELSE -1 END, 0, 0) + ((t0.\"Points\" / 10) * INTERVAL '1 microsecond')"
            },
        };

    [Theory]
    [MemberData(nameof(Times))]
    public void BuildsTime(Expression<Func<Order, TimeOnly>> selector, string sql)
    {
        Expression query = Orders.Select(selector).Expression;

        Assert.Equal(
            Sql.Valid($"SELECT {sql} FROM \"Order\" AS t0"),
            new PostgreSqlQuery(query).TextValue
        );
    }

    public static TheoryData<Expression<Func<Order, DateTime>>, string> Moments =>
        new()
        {
            {
                o => new DateTime(o.Quantity, 1, o.Id),
                $"MAKE_TIMESTAMP({Year}, 1, t0.\"Id\", 0, 0, 0)"
            },
            {
                o => new DateTime(2024, 1, o.Id, o.Quantity, 30, 15),
                "MAKE_TIMESTAMP(CASE WHEN (t0.\"Quantity\" >= 0) AND (t0.\"Quantity\" <= 23) THEN 2024 ELSE 0 END, 1, t0.\"Id\", t0.\"Quantity\", 30, 15)"
            },
            {
                o => new DateTime(2024, 1, o.Id, 23, 59, 59, 999),
                "MAKE_TIMESTAMP(2024, 1, t0.\"Id\", 23, 59, 59.999)"
            },
            {
                o => new DateTime(2024, 1, o.Id, 0, 0, 0, 0, 1),
                "MAKE_TIMESTAMP(2024, 1, t0.\"Id\", 0, 0, 0.000001)"
            },
            {
                o => new DateTime(2024, 1, 1, 0, 0, o.Id, o.Quantity),
                "MAKE_TIMESTAMP(CASE WHEN (t0.\"Id\" >= 0) AND (t0.\"Id\" <= 59) AND (t0.\"Quantity\" >= 0) AND (t0.\"Quantity\" <= 999) THEN 2024 ELSE 0 END, 1, 1, 0, 0, t0.\"Id\" + (t0.\"Quantity\" * 0.001))"
            },
            {
                o => new DateTime(2024, 1, o.Id, 24, 0, 0),
                "MAKE_TIMESTAMP(0, 1, t0.\"Id\", 24, 0, 0)"
            },
            {
                o => new DateTime(2024, 1, o.Id, 12, 0, 0, DateTimeKind.Utc),
                "MAKE_TIMESTAMP(2024, 1, t0.\"Id\", 12, 0, 0) AT TIME ZONE 'UTC'"
            },
            {
                o => new DateTime(2024, 1, o.Id, 12, 0, 0, DateTimeKind.Unspecified),
                "MAKE_TIMESTAMP(2024, 1, t0.\"Id\", 12, 0, 0)"
            },
            {
                o => new DateTime(2024, 1, o.Id, 12, 0, 0, 5, DateTimeKind.Utc),
                "MAKE_TIMESTAMP(2024, 1, t0.\"Id\", 12, 0, 0.005) AT TIME ZONE 'UTC'"
            },
            {
                o => new DateTime(2024, 1, o.Id, 12, 0, 0, 5, 6, DateTimeKind.Utc),
                "MAKE_TIMESTAMP(2024, 1, t0.\"Id\", 12, 0, 0.005006) AT TIME ZONE 'UTC'"
            },
            {
                o => new DateTime(o.ShippedOn!.Value, o.DeliveryTime),
                "t0.\"ShippedOn\" + t0.\"DeliveryTime\""
            },
            {
                o => new DateTime(o.ShippedOn!.Value, o.DeliveryTime, DateTimeKind.Utc),
                "(t0.\"ShippedOn\" + t0.\"DeliveryTime\") AT TIME ZONE 'UTC'"
            },
            {
                o => o.ShippedOn!.Value.ToDateTime(o.DeliveryTime),
                "t0.\"ShippedOn\" + t0.\"DeliveryTime\""
            },
            {
                o => o.ShippedOn!.Value.ToDateTime(new TimeOnly(9, 30)),
                "t0.\"ShippedOn\" + TIME '09:30:00'"
            },
            {
                o => o.ShippedOn!.Value.ToDateTime(o.DeliveryTime, DateTimeKind.Utc),
                "(t0.\"ShippedOn\" + t0.\"DeliveryTime\") AT TIME ZONE 'UTC'"
            },
            {
                o =>
                    o.ShippedOn!.Value.ToDateTime(
                        o.DeliveryTime,
                        DateTimeKind.Unspecified
                    ),
                "t0.\"ShippedOn\" + t0.\"DeliveryTime\""
            },
        };

    [Theory]
    [MemberData(nameof(Moments))]
    public void BuildsTimestamp(Expression<Func<Order, DateTime>> selector, string sql)
    {
        Expression query = Orders.Select(selector).Expression;

        Assert.Equal(
            Sql.Valid($"SELECT {sql} FROM \"Order\" AS t0"),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesDateFromPartsInFilter()
    {
        Expression query = Orders
            .Where(o => new DateOnly(2024, o.Quantity, 1) <= o.ShippedOn)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE MAKE_DATE(2024, t0."Quantity", 1) <= t0."ShippedOn"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void GroupsByMonthFromParts()
    {
        Expression query = Orders
            .GroupBy(o => new DateOnly(o.CreatedAt.Year, o.CreatedAt.Month, 1))
            .Select(g => new { g.Key, Count = g.Count() })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT MAKE_DATE(CASE WHEN (CAST(EXTRACT(YEAR FROM t0."CreatedAt") AS integer) >= 1) AND (CAST(EXTRACT(YEAR FROM t0."CreatedAt") AS integer) <= 9999) THEN CAST(EXTRACT(YEAR FROM t0."CreatedAt") AS integer) ELSE 0 END, CAST(EXTRACT(MONTH FROM t0."CreatedAt") AS integer), 1) AS "Key", CAST(COUNT(*) AS integer) AS "Count"
                FROM "Order" AS t0
                GROUP BY MAKE_DATE(CASE WHEN (CAST(EXTRACT(YEAR FROM t0."CreatedAt") AS integer) >= 1) AND (CAST(EXTRACT(YEAR FROM t0."CreatedAt") AS integer) <= 9999) THEN CAST(EXTRACT(YEAR FROM t0."CreatedAt") AS integer) ELSE 0 END, CAST(EXTRACT(MONTH FROM t0."CreatedAt") AS integer), 1)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OrdersByTimeFromParts()
    {
        Expression query = Orders
            .OrderBy(o => new TimeOnly(12, o.Quantity))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                ORDER BY MAKE_TIME(12, t0."Quantity", 0)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FoldsConstantParts()
    {
        Expression query = Orders
            .Where(o => o.ShippedOn > new DateOnly(2024, 2, 29))
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

    // A struct created without arguments has no constructor to read parts from.
    [Fact]
    public void ProjectsStructCreatedWithoutArguments()
    {
        Expression query = Orders
            .Select(o => new { o.Id, Empty = new ValueTuple() })
            .Expression;

        Assert.Equal(
            Sql.Valid("SELECT t0.\"Id\" FROM \"Order\" AS t0"),
            new PostgreSqlQuery(query).TextValue
        );
    }

    public static TheoryData<Expression<Func<Order, object>>, string> Rejected =>
        new()
        {
            {
                o => new DateOnly(2024, 1, o.Id, new GregorianCalendar()),
                "Constructing 'DateOnly'"
            },
            { o => new DateTime(2024, 1, o.Id, new GregorianCalendar()), "Calendar" },
            {
                o => new DateTime(2024, 1, o.Id, 0, 0, 0, new GregorianCalendar()),
                "Calendar"
            },
            { o => new DateTime(2024, 1, o.Id, 0, 0, 0, DateTimeKind.Local), "Local" },
            {
                o => new DateTime(
                    2024,
                    1,
                    o.Id,
                    0,
                    0,
                    0,
                    o.IsPaid ? DateTimeKind.Utc : DateTimeKind.Unspecified
                ),
                "constant kind"
            },
            {
                o => o.ShippedOn!.Value.ToDateTime(o.DeliveryTime, DateTimeKind.Local),
                "Local"
            },
            { o => TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(o.Id)), "FromTimeSpan" },
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
}
