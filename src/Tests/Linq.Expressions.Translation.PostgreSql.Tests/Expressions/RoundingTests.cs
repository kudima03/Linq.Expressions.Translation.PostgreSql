namespace Linq.Expressions.Translation.PostgreSql.Tests.Expressions;

/// <summary>
/// PostgreSQL's ROUND rounds a numeric half away from zero and a double by the
/// platform's rule, so the half-way case is decided explicitly: half to even, as
/// .NET does by default, or away from zero.
/// </summary>
public sealed record RoundingTests
{
    [Fact]
    public void RoundsDecimalHalfToEven()
    {
        Expression query = Orders.Select(o => Math.Round(o.Total)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CASE WHEN (ABS(t0."Total" - TRUNC(t0."Total")) = 0.5) AND (MOD(TRUNC(t0."Total"), 2) = 0) THEN TRUNC(t0."Total") ELSE ROUND(t0."Total") END
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RoundsDecimalToDigitsHalfToEven()
    {
        Expression query = Orders.Select(o => Math.Round(o.Total, 2)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CASE WHEN (ABS((t0."Total" * 100) - TRUNC(t0."Total" * 100)) = 0.5) AND (MOD(TRUNC(t0."Total" * 100), 2) = 0) THEN TRUNC(t0."Total", 2) ELSE ROUND(t0."Total", 2) END
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RoundsDecimalToEvenExplicitly()
    {
        Expression query = Orders
            .Select(o => Math.Round(o.Total, MidpointRounding.ToEven))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CASE WHEN (ABS(t0."Total" - TRUNC(t0."Total")) = 0.5) AND (MOD(TRUNC(t0."Total"), 2) = 0) THEN TRUNC(t0."Total") ELSE ROUND(t0."Total") END
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RoundsDecimalToDigitsToEvenExplicitly()
    {
        Expression query = Orders
            .Select(o => Math.Round(o.Total, 2, MidpointRounding.ToEven))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CASE WHEN (ABS((t0."Total" * 100) - TRUNC(t0."Total" * 100)) = 0.5) AND (MOD(TRUNC(t0."Total" * 100), 2) = 0) THEN TRUNC(t0."Total", 2) ELSE ROUND(t0."Total", 2) END
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RoundsDecimalToNegativeDigitsHalfToEven()
    {
        Expression query = Orders.Select(o => Math.Round(o.Total, -1)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CASE WHEN (ABS((t0."Total" * 0.1) - TRUNC(t0."Total" * 0.1)) = 0.5) AND (MOD(TRUNC(t0."Total" * 0.1), 2) = 0) THEN TRUNC(t0."Total", -1) ELSE ROUND(t0."Total", -1) END
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RoundsDoubleHalfToEven()
    {
        Expression query = Orders.Select(o => Math.Round(o.Weight)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CASE WHEN (ABS(t0."Weight" - TRUNC(t0."Weight")) = 0.5) AND (MOD(CAST(TRUNC(t0."Weight") AS numeric), 2) = 0) THEN TRUNC(t0."Weight") ELSE ROUND(t0."Weight") END
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RoundsDoubleToDigitsHalfToEven()
    {
        Expression query = Orders.Select(o => Math.Round(o.Weight, 2)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CASE WHEN (ABS((t0."Weight" * 100) - TRUNC(t0."Weight" * 100)) = 0.5) AND (MOD(CAST(TRUNC(t0."Weight" * 100) AS numeric), 2) = 0) THEN TRUNC(t0."Weight" * 100) ELSE ROUND(t0."Weight" * 100) END / 100
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RoundsDoubleAwayFromZero()
    {
        Expression query = Orders
            .Select(o => Math.Round(o.Weight, MidpointRounding.AwayFromZero))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CASE WHEN ABS(t0."Weight" - TRUNC(t0."Weight")) = 0.5 THEN TRUNC(t0."Weight") + SIGN(t0."Weight") ELSE ROUND(t0."Weight") END
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RoundsDoubleToDigitsAwayFromZero()
    {
        Expression query = Orders
            .Select(o => Math.Round(o.Weight, 1, MidpointRounding.AwayFromZero))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CASE WHEN ABS((t0."Weight" * 10) - TRUNC(t0."Weight" * 10)) = 0.5 THEN TRUNC(t0."Weight" * 10) + SIGN(t0."Weight" * 10) ELSE ROUND(t0."Weight" * 10) END / 10
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RoundsThroughTheNumericCounterparts()
    {
        Expression totals = Orders.Select(o => decimal.Round(o.Total)).Expression;
        Expression weights = Orders.Select(o => double.Round(o.Weight)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CASE WHEN (ABS(t0."Total" - TRUNC(t0."Total")) = 0.5) AND (MOD(TRUNC(t0."Total"), 2) = 0) THEN TRUNC(t0."Total") ELSE ROUND(t0."Total") END
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(totals).TextValue
        );
        Assert.Equal(
            Sql.Valid(
                """
                SELECT CASE WHEN (ABS(t0."Weight" - TRUNC(t0."Weight")) = 0.5) AND (MOD(CAST(TRUNC(t0."Weight") AS numeric), 2) = 0) THEN TRUNC(t0."Weight") ELSE ROUND(t0."Weight") END
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(weights).TextValue
        );
    }

    [Fact]
    public void FiltersByRoundedDouble()
    {
        Expression query = Orders
            .Where(o => Math.Round(o.Weight) > 2)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE CASE WHEN (ABS(t0."Weight" - TRUNC(t0."Weight")) = 0.5) AND (MOD(CAST(TRUNC(t0."Weight") AS numeric), 2) = 0) THEN TRUNC(t0."Weight") ELSE ROUND(t0."Weight") END > 2
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RejectsRoundingWithAComputedMode()
    {
        Expression query = Orders
            .Select(o =>
                Math.Round(
                    o.Total,
                    o.IsPaid ? MidpointRounding.ToEven : MidpointRounding.AwayFromZero
                )
            )
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Round", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsRoundingTowardZero()
    {
        Expression query = Orders
            .Select(o => Math.Round(o.Total, MidpointRounding.ToZero))
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Round", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsRoundingToComputedDigits()
    {
        Expression query = Orders.Select(o => Math.Round(o.Total, o.Quantity)).Expression;

        _ = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
    }
}
