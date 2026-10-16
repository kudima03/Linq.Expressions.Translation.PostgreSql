#pragma warning disable IDE0048 // C# operator precedence is under test

namespace Linq.Expressions.Translation.PostgreSql.Tests.Combinations;

public sealed record ArithmeticMatrixTests
{
    public static TheoryData<
        Expression<Func<Order, object?>>,
        string,
        bool
    > Projections =>
        new()
        {
            {
                o => o.Quantity + o.Points,
                "CAST(t0.\"Quantity\" AS bigint) + t0.\"Points\"",
                false
            },
            {
                o => o.Quantity * o.Total,
                "CAST(t0.\"Quantity\" AS numeric) * t0.\"Total\"",
                false
            },
            {
                o => o.Quantity - o.Weight,
                "CAST(t0.\"Quantity\" AS double precision) - t0.\"Weight\"",
                false
            },
            {
                o => o.Points * o.Total,
                "CAST(t0.\"Points\" AS numeric) * t0.\"Total\"",
                false
            },
            {
                o => o.Points / o.Weight,
                "CAST(t0.\"Points\" AS double precision) / t0.\"Weight\"",
                false
            },
            {
                o => o.Priority + o.Points,
                "CAST(t0.\"Priority\" AS bigint) + t0.\"Points\"",
                true
            },
            {
                o => o.Priority * o.Discount,
                "CAST(t0.\"Priority\" AS numeric) * t0.\"Discount\"",
                true
            },
            {
                o => o.Quantity % o.CustomerId,
                "t0.\"Quantity\" % t0.\"CustomerId\"",
                false
            },
            { o => o.Total % 3, "t0.\"Total\" % 3", false },
            { o => o.Weight * 2, "t0.\"Weight\" * 2", false },
            { o => o.Total + 0.1m, "t0.\"Total\" + 0.1", false },
            { o => o.Quantity / 4, "t0.\"Quantity\" / 4", false },
            {
                o => (o.Quantity + 1) * (o.CustomerId - 1),
                "(t0.\"Quantity\" + 1) * (t0.\"CustomerId\" - 1)",
                false
            },
            { o => o.Quantity * 2 + 1, "(t0.\"Quantity\" * 2) + 1", false },
            {
                o => o.Quantity + o.CustomerId * 2,
                "t0.\"Quantity\" + (t0.\"CustomerId\" * 2)",
                false
            },
            {
                o => (long)o.Quantity * o.CustomerId,
                "CAST(t0.\"Quantity\" AS bigint) * CAST(t0.\"CustomerId\" AS bigint)",
                false
            },
            { o => (double)o.Total, "CAST(t0.\"Total\" AS double precision)", false },
            { o => (int)o.Points, "CAST(t0.\"Points\" AS integer)", false },
            { o => (int?)o.Weight, "CAST(TRUNC(t0.\"Weight\") AS integer)", false },
            { o => (decimal?)o.Priority, "CAST(t0.\"Priority\" AS numeric)", true },
            { o => (long)o.Status, "CAST(t0.\"Status\" AS bigint)", false },
            {
                o => o.Discount ?? o.Total * 2,
                "COALESCE(t0.\"Discount\", t0.\"Total\" * 2)",
                false
            },
            { o => -o.Total, "-t0.\"Total\"", false },
            {
                o => o.Quantity & o.CustomerId | 1,
                "(t0.\"Quantity\" & t0.\"CustomerId\") | 1",
                false
            },
            {
                o => o.Quantity ^ o.CustomerId,
                "t0.\"Quantity\" # t0.\"CustomerId\"",
                false
            },
            {
                o => o.Points >> o.Quantity,
                "t0.\"Points\" >> (t0.\"Quantity\" & 63)",
                false
            },
        };

    [Theory]
    [MemberData(nameof(Projections))]
    public void ProjectsArithmetic(
        Expression<Func<Order, object?>> selector,
        string value,
        bool nullable
    )
    {
        _ = nullable;
        Expression query = Orders.Select(selector).Expression;

        Assert.Equal(
            Sql.Valid(
                $"""
                SELECT {value}
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Theory]
    [MemberData(nameof(Projections))]
    public void OrdersByArithmetic(
        Expression<Func<Order, object?>> selector,
        string value,
        bool nullable
    )
    {
        Expression query = Orders.OrderBy(selector).Select(o => o.Id).Expression;

        Assert.Equal(
            Sql.Valid(
                $"""
                SELECT t0."Id"
                FROM "Order" AS t0
                ORDER BY {value}{(nullable ? " NULLS FIRST" : string.Empty)}
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
