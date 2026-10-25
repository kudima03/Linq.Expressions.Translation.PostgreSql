#pragma warning disable IDE0004 // the cast to float is the subject
#pragma warning disable CA1861 // constant arrays are the subject

namespace Linq.Expressions.Translation.PostgreSql.Tests.PostgreSql;

public sealed record LiteralTests
{
    [Fact]
    public void RendersInteger()
    {
        Expression query = Orders.Select(o => 5).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT 5
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RendersNegativeInteger()
    {
        Expression query = Orders.Select(o => -5).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT -5
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RendersIntegerMinimum()
    {
        Expression query = Orders.Select(o => int.MinValue).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT -2147483648
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RendersIntegerMaximum()
    {
        Expression query = Orders.Select(o => int.MaxValue).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT 2147483647
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RendersLong()
    {
        Expression query = Orders.Select(o => 5000000000L).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT 5000000000
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RendersLongMinimum()
    {
        Expression query = Orders.Select(o => long.MinValue).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT -9223372036854775808
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RendersLongMaximum()
    {
        Expression query = Orders.Select(o => long.MaxValue).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT 9223372036854775807
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RendersShort()
    {
        Expression query = Orders.Select(o => (short)7).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT 7
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RendersDecimalWithScale()
    {
        Expression query = Orders.Select(o => 10.50m).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT 10.50
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RendersDecimalWithoutScale()
    {
        Expression query = Orders.Select(o => 10m).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT 10
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RendersNegativeDecimal()
    {
        Expression query = Orders.Select(o => -0.25m).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT -0.25
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RendersZeroDecimalWithScale()
    {
        Expression query = Orders.Select(o => 0.0m).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT 0.0
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RendersLargeDecimal()
    {
        Expression query = Orders.Select(o => 79228162514264337593543950335m).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT 79228162514264337593543950335
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RendersDouble()
    {
        Expression query = Orders.Select(o => 1.5).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT 1.5
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RendersWholeDouble()
    {
        Expression query = Orders.Select(o => 100000.0).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT 100000
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RendersNegativeDouble()
    {
        Expression query = Orders.Select(o => -2.75).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT -2.75
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RendersSmallDoubleInInvariantCulture()
    {
        Expression query = Orders.Select(o => 0.0000001).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT 1E-07
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RendersDoubleWithoutRoundingLoss()
    {
        Expression query = Orders.Select(o => 0.1 + 0.2).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT 0.30000000000000004
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RendersNotANumber()
    {
        Expression query = Orders.Select(o => double.NaN).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST('NaN' AS double precision)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RendersPositiveInfinity()
    {
        Expression query = Orders.Select(o => double.PositiveInfinity).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST('Infinity' AS double precision)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RendersNegativeInfinity()
    {
        Expression query = Orders.Select(o => double.NegativeInfinity).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST('-Infinity' AS double precision)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RendersTrue()
    {
        Expression query = Orders.Select(o => true).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT TRUE
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RendersFalse()
    {
        Expression query = Orders.Select(o => false).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT FALSE
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RendersNullString()
    {
        Expression query = Orders.Select(o => (string?)null).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT NULL
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RendersNullInteger()
    {
        Expression query = Orders.Select(o => (int?)null).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT NULL
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RendersNullableInteger()
    {
        Expression query = Orders.Select(o => (int?)5).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT 5
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RendersString()
    {
        Expression query = Orders.Select(o => "abc").Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT 'abc'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RendersEmptyString()
    {
        Expression query = Orders.Select(o => "").Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT ''
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RendersCharacter()
    {
        Expression query = Orders.Select(o => 'x').Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT 'x'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RendersUnicodeString()
    {
        Expression query = Orders.Select(o => "Привет, 世界").Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT 'Привет, 世界'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RendersStringWithSpaces()
    {
        Expression query = Orders.Select(o => "  padded  ").Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT '  padded  '
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RendersGuid()
    {
        Expression query = Orders
            .Select(o => Guid.Parse("11111111-2222-3333-4444-555555555555"))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT UUID '11111111-2222-3333-4444-555555555555'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RendersDateTime()
    {
        Expression query = Orders
            .Select(o => new DateTime(2024, 1, 31, 12, 30, 0))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT TIMESTAMP '2024-01-31 12:30:00'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RendersDateTimeAtMidnight()
    {
        Expression query = Orders.Select(o => new DateTime(2024, 1, 31)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT TIMESTAMP '2024-01-31 00:00:00'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RendersDateTimeWithFraction()
    {
        Expression query = Orders
            .Select(o => new DateTime(2024, 1, 31, 12, 30, 0, 250))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT TIMESTAMP '2024-01-31 12:30:00.25'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RendersDateTimeWithMicroseconds()
    {
        Expression query = Orders
            .Select(o => new DateTime(2024, 1, 31, 12, 30, 0).AddTicks(1230))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT TIMESTAMP '2024-01-31 12:30:00.000123'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RendersUtcDateTime()
    {
        Expression query = Orders
            .Select(o => new DateTime(2024, 1, 31, 12, 30, 0, DateTimeKind.Utc))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT TIMESTAMPTZ '2024-01-31 12:30:00+00'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RendersDateTimeMinimum()
    {
        Expression query = Orders.Select(o => DateTime.MinValue).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT TIMESTAMP '0001-01-01 00:00:00'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RendersDateTimeOffsetAtUtc()
    {
        Expression query = Orders
            .Select(o => new DateTimeOffset(2024, 1, 31, 12, 30, 0, TimeSpan.Zero))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT TIMESTAMPTZ '2024-01-31 12:30:00+00'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RendersDateTimeOffsetWithPositiveOffset()
    {
        Expression query = Orders
            .Select(o => new DateTimeOffset(
                2024,
                1,
                31,
                12,
                30,
                0,
                TimeSpan.FromHours(2)
            ))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT TIMESTAMPTZ '2024-01-31 12:30:00+02:00'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RendersDateTimeOffsetWithNegativeOffset()
    {
        Expression query = Orders
            .Select(o => new DateTimeOffset(
                2024,
                1,
                31,
                12,
                30,
                0,
                new TimeSpan(-5, -30, 0)
            ))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT TIMESTAMPTZ '2024-01-31 12:30:00-05:30'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RendersDateOnly()
    {
        Expression query = Orders.Select(o => new DateOnly(2024, 2, 29)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT DATE '2024-02-29'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RendersDateOnlyMinimum()
    {
        Expression query = Orders.Select(o => DateOnly.MinValue).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT DATE '0001-01-01'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RendersTimeOnly()
    {
        Expression query = Orders.Select(o => new TimeOnly(9, 30)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT TIME '09:30:00'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RendersTimeOnlyWithSeconds()
    {
        Expression query = Orders.Select(o => new TimeOnly(23, 59, 59)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT TIME '23:59:59'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RendersTimeOnlyWithMicroseconds()
    {
        Expression query = Orders
            .Select(o => new TimeOnly(9, 30, 15, 123, 456))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT TIME '09:30:15.123456'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RendersTimeSpanOfDays()
    {
        Expression query = Orders.Select(o => TimeSpan.FromDays(30)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT INTERVAL 'P30D'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RendersTimeSpanOfHoursAndMinutes()
    {
        Expression query = Orders.Select(o => TimeSpan.FromMinutes(90)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT INTERVAL 'PT1H30M'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RendersTimeSpanOfDaysAndHours()
    {
        Expression query = Orders.Select(o => TimeSpan.FromHours(36)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT INTERVAL 'P1DT12H'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RendersTimeSpanWithSecondsFraction()
    {
        Expression query = Orders.Select(o => TimeSpan.FromMilliseconds(1500)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT INTERVAL 'PT1.5S'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RendersZeroTimeSpan()
    {
        Expression query = Orders.Select(o => TimeSpan.Zero).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT INTERVAL 'PT0S'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RendersEnumAsUnderlyingInteger()
    {
        Expression query = Orders.Select(o => OrderStatus.Shipped).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT 2
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RendersFlagsEnumAsUnderlyingInteger()
    {
        Expression query = Orders
            .Select(o => ShippingOptions.Express | ShippingOptions.Insured)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT 3
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RendersIntegerArray()
    {
        Expression query = Orders.Select(o => new[] { 1, 2, 3 }).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT ARRAY[1, 2, 3]
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RendersStringArray()
    {
        Expression query = Orders.Select(o => new[] { "a", "b" }).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT ARRAY['a', 'b']
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RendersLiteralsOfEveryTypeNextToColumn()
    {
        Expression query = Orders
            .Select(o => new
            {
                o.Id,
                Int = 1,
                Dec = 2.50m,
                Text = "t",
                Flag = true,
                Day = new DateOnly(2024, 1, 2),
                Moment = new DateTime(2024, 1, 2, 3, 4, 5),
                Token = Guid.Empty,
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", 1 AS "Int", 2.50 AS "Dec", 't' AS "Text",
                TRUE AS "Flag", DATE '2024-01-02' AS "Day",
                TIMESTAMP '2024-01-02 03:04:05' AS "Moment",
                UUID '00000000-0000-0000-0000-000000000000' AS "Token"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void DoublesSingleQuote()
    {
        Expression query = Orders.Select(o => "it's").Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT 'it''s'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void DoublesEverySingleQuote()
    {
        Expression query = Orders.Select(o => "''").Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT ''''''
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void KeepsBackslashLiteral()
    {
        Expression query = Orders.Select(o => "C:\\temp\\file").Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT 'C:\temp\file'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void KeepsDoubleQuote()
    {
        Expression query = Orders.Select(o => "say \"hi\"").Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT 'say "hi"'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void KeepsPercentAndUnderscore()
    {
        Expression query = Orders.Select(o => "50%_off").Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT '50%_off'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void KeepsSemicolonAndComment()
    {
        Expression query = Orders.Select(o => "x; -- y /* z */").Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT 'x; -- y /* z */'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void KeepsSqlInjectionAttemptInsideLiteral()
    {
        Expression query = Orders.Select(o => "'; DROP TABLE \"Order\"; --").Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT '''; DROP TABLE "Order"; --'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void EscapesNewline()
    {
        Expression query = Orders.Select(o => "line1\nline2").Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT E'line1\nline2'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void EscapesCarriageReturnAndLineFeed()
    {
        Expression query = Orders.Select(o => "a\r\nb").Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT E'a\r\nb'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void EscapesTab()
    {
        Expression query = Orders.Select(o => "a\tb").Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT E'a\tb'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void EscapesBackspaceAndFormFeed()
    {
        Expression query = Orders.Select(o => "a\bb\fc").Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT E'a\bb\fc'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void EscapesBackslashTogetherWithNewline()
    {
        Expression query = Orders.Select(o => "a\\b\nc").Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT E'a\\b\nc'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void EscapesQuoteTogetherWithNewline()
    {
        Expression query = Orders.Select(o => "it's\n").Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT E'it''s\n'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void EscapesOtherControlCharacterAsHex()
    {
        Expression query = Orders.Select(o => "a\u0001b").Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT E'a\x01b'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void EscapesEscapeCharacterAsHex()
    {
        Expression query = Orders.Select(o => "a\u001bb").Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT E'a\x1bb'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void EscapesControlCharacterInCharacterLiteral()
    {
        Expression query = Orders.Select(o => '\n').Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT E'\n'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void EscapesCapturedStringWithNewline()
    {
        string text = "a\nb";

        Expression query = Orders.Select(o => text).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT E'a\nb'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void EscapesStringInsideComparison()
    {
        Expression query = Orders.Select(o => o.Number == "a\nb").Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Number" = E'a\nb'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void EscapesStringInsideInList()
    {
        string[] values = ["a", "b\n", "it's"];

        Expression query = Orders
            .Where(o => values.Contains(o.Number))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Number" IN ('a', E'b\n', 'it''s')
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void EscapesStringInsideLikePattern()
    {
        Expression query = Orders
            .Where(o => o.Number.Contains("a\nb"))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Number" LIKE E'%a\nb%'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Theory]
    [InlineData(float.NaN, "CAST('NaN' AS real)")]
    [InlineData(float.PositiveInfinity, "CAST('Infinity' AS real)")]
    [InlineData(float.NegativeInfinity, "CAST('-Infinity' AS real)")]
    [InlineData(1.5f, "1.5")]
    public void RendersSpecialSingleValues(float value, string literal)
    {
        Expression query = Orders
            .Where(o => (float)o.Quantity > value)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                $"""
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE CAST(t0."Quantity" AS real) > {literal}
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RendersEmptyArray()
    {
        string[] none = [];
        Expression query = Orders.Where(o => o.Tags == none).Select(o => o.Id).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Tags" = '{}'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void EscapesNonBreakingSpaceAsUnicode()
    {
        Expression query = Customers.Select(c => "a b").Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT E'a\u00a0b'
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void EscapesControlCharactersAboveAsciiAsUnicode()
    {
        Expression query = Customers.Select(c => "a\u0080b").Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT E'a\u0080b'
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
