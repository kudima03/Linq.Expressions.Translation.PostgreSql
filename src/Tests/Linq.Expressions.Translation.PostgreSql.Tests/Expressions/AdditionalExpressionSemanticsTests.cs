#pragma warning disable xUnit1004 // skipped tests record SQL the translator does not produce yet
#pragma warning disable CA1861 // the array is the IndexOfAny argument under test
#pragma warning disable CA1870 // SearchValues would hide the IndexOfAny translation

namespace Linq.Expressions.Translation.PostgreSql.Tests.Expressions;

public sealed record AdditionalExpressionSemanticsTests
{
    [Fact]
    public void DividesLongByConstant()
    {
        Expression query = Orders.Select(o => o.Points / 2).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Points" / 2
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void DividesDoubleByInteger()
    {
        Expression query = Orders.Select(o => o.Weight / o.Quantity).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Weight" / CAST(t0."Quantity" AS double precision)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AddsCheckedNullableInteger()
    {
        Expression query = Orders.Select(o => checked(o.Priority + 1)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Priority" + 1
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SubtractsTimeSpanWithOperator()
    {
        Expression query = Orders
            .Select(o => o.CreatedAt - TimeSpan.FromHours(2))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CreatedAt" - INTERVAL 'PT2H'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsYearOfDateTimeOffset()
    {
        Expression query = Orders.Select(o => o.PaidAt!.Value.Year).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(EXTRACT(YEAR FROM t0."PaidAt") AS integer)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AddsHoursToTimeFromDouble()
    {
        Expression query = Orders
            .Select(o => o.DeliveryTime.AddHours(o.Weight))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."DeliveryTime" + (t0."Weight" * INTERVAL '1 hour')
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ReplacesSeparatorWithNothing()
    {
        Expression query = Orders
            .Select(o => o.Number.Replace("-", string.Empty))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT REPLACE(t0."Number", '-', '')
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesWholeStringFromStart()
    {
        Expression query = Orders.Select(o => o.Number.Substring(0)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT SUBSTRING(t0."Number", 1)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TreatsHasFlagOfNoneAsTrue()
    {
        Expression query = Orders
            .Where(o => o.Options.HasFlag(ShippingOptions.None))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."Options" & 0) = 0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesLowercaseOfNullableColumn()
    {
        Expression query = Orders
            .Where(o => o.Number.ToLower() == o.Note!.ToLower())
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE LOWER(t0."Number") = LOWER(t0."Note")
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComputesRemainderOfNegativeDivisor()
    {
        Expression query = Orders.Select(o => o.Quantity % -5).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Quantity" % -5
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "A shift count is not masked to the operand width.")]
    public void MasksOversizedLeftShift()
    {
        Expression query = Orders.Select(o => o.Quantity << 33).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Quantity" << 1
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "A negative shift count is not masked to the operand width.")]
    public void MasksNegativeLeftShift()
    {
        Expression query = Orders.Select(o => o.Quantity << -1).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Quantity" << 31
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "A shift count is not masked to the long operand width.")]
    public void MasksOversizedLeftShiftOfLong()
    {
        Expression query = Orders.Select(o => o.Points << 65).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Points" << 1
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "A shift count is not masked to the operand width.")]
    public void MasksOversizedRightShift()
    {
        Expression query = Orders.Select(o => o.Quantity >> 33).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Quantity" >> 1
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "DateTimeOffset.Date is not translated.")]
    public void SelectsDateOfDateTimeOffset()
    {
        Expression query = Orders.Select(o => o.PaidAt!.Value.Date).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT DATE_TRUNC('day', t0."PaidAt")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "DateTime.TimeOfDay is not translated.")]
    public void SelectsTimeOfDay()
    {
        Expression query = Orders.Select(o => o.CreatedAt.TimeOfDay).Expression;

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

    [Fact(Skip = "DateTimeOffset.UtcDateTime is not translated.")]
    public void SelectsUtcDateTime()
    {
        Expression query = Orders.Select(o => o.PaidAt!.Value.UtcDateTime).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."PaidAt" AT TIME ZONE 'UTC'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "DateOnly.DayNumber is not translated.")]
    public void SelectsDayNumber()
    {
        Expression query = Orders.Select(o => o.ShippedOn!.Value.DayNumber).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."ShippedOn" - DATE '0001-01-01'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "DateOnly.AddMonths is not cast back to date.")]
    public void AddsMonthsToDateOnlyFromColumn()
    {
        Expression query = Orders
            .Select(o => o.ShippedOn!.Value.AddMonths(o.Quantity))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(t0."ShippedOn" + (t0."Quantity" * INTERVAL '1 month') AS date)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "String.IndexOfAny is not translated.")]
    public void FindsAnyListedCharacter()
    {
        Expression query = Orders
            .Select(o => o.Number.IndexOfAny(new[] { '-', '_' }))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(LEAST(NULLIF(STRPOS(t0."Number", '-') - 1, -1), NULLIF(STRPOS(t0."Number", '_') - 1, -1)), -1)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "String.IndexOf with a start and a count is not translated.")]
    public void FindsCharacterInsideWindow()
    {
        Expression query = Orders.Select(o => o.Number.IndexOf('-', 1, 3)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CASE
                WHEN STRPOS(SUBSTRING(t0."Number", 2, 3), '-') = 0 THEN -1
                ELSE (STRPOS(SUBSTRING(t0."Number", 2, 3), '-') - 1) + 1
                END
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "Math.BigMul is not translated.")]
    public void MultipliesToLongWithoutOverflow()
    {
        Expression query = Orders
            .Select(o => Math.BigMul(o.Quantity, o.CustomerId))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(t0."Quantity" AS bigint) * CAST(t0."CustomerId" AS bigint)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "Int32.Min is not translated.")]
    public void TakesLeastIntegerOnItsType()
    {
        Expression query = Orders.Select(o => int.Min(o.Quantity, 1)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT LEAST(t0."Quantity", 1)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "Int64.Max is not translated.")]
    public void TakesGreatestLongOnItsType()
    {
        Expression query = Orders.Select(o => long.Max(o.Points, 1L)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT GREATEST(t0."Points", 1)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "Int64.Abs is not translated.")]
    public void TakesAbsoluteLongOnItsType()
    {
        Expression query = Orders.Select(o => long.Abs(o.Points)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT ABS(t0."Points")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "Double.Abs is not translated.")]
    public void TakesAbsoluteDoubleOnItsType()
    {
        Expression query = Orders.Select(o => double.Abs(o.Weight)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT ABS(t0."Weight")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "Decimal.Clamp is not translated.")]
    public void ClampsDecimalOnItsType()
    {
        Expression query = Orders.Select(o => decimal.Clamp(o.Total, 1m, 10m)).Expression;

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
}
