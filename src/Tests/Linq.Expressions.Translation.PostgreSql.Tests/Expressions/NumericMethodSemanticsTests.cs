#pragma warning disable xUnit1004 // skipped tests record SQL the translator does not produce yet

namespace Linq.Expressions.Translation.PostgreSql.Tests.Expressions;

public sealed record NumericMethodSemanticsTests
{
    [Fact(Skip = "Int32.Abs is not translated.")]
    public void TakesAbsoluteValueOfIntegerType()
    {
        Expression query = Orders.Select(o => int.Abs(o.Quantity)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT ABS(t0."Quantity")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "Decimal.Abs is not translated.")]
    public void TakesAbsoluteValueOfDecimalType()
    {
        Expression query = Orders.Select(o => decimal.Abs(o.Total)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT ABS(t0."Total")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "Math.Clamp is not translated.")]
    public void ClampsInteger()
    {
        Expression query = Orders.Select(o => Math.Clamp(o.Quantity, 1, 10)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT LEAST(GREATEST(t0."Quantity", 1), 10)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "Int32.Clamp is not translated.")]
    public void ClampsIntegerOnItsType()
    {
        Expression query = Orders.Select(o => int.Clamp(o.Quantity, 1, 10)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT LEAST(GREATEST(t0."Quantity", 1), 10)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "Math.Clamp is not translated.")]
    public void ClampsDecimal()
    {
        Expression query = Orders.Select(o => Math.Clamp(o.Total, 1m, 10m)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT LEAST(GREATEST(t0."Total", 1), 10)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "Double.IsNaN is not translated.")]
    public void DetectsNotANumber()
    {
        Expression query = Orders.Select(o => double.IsNaN(o.Weight)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Weight" = CAST('NaN' AS double precision)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "Double.IsInfinity is not translated.")]
    public void DetectsInfinity()
    {
        Expression query = Orders.Select(o => double.IsInfinity(o.Weight)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT (t0."Weight" = CAST('Infinity' AS double precision)) OR (t0."Weight" = CAST('-Infinity' AS double precision))
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "Double.IsFinite is not translated.")]
    public void DetectsFiniteDouble()
    {
        Expression query = Orders.Select(o => double.IsFinite(o.Weight)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Weight" NOT IN (CAST('NaN' AS double precision), CAST('Infinity' AS double precision), CAST('-Infinity' AS double precision))
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
