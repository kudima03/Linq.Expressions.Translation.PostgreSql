using System.Globalization;

namespace Linq.Expressions.Translation.PostgreSql.Tests.Expressions;

public sealed record ParseSemanticsTests
{
    [Fact]
    public void ParsesTextAsLong()
    {
        Expression query = Orders
            .Select(o =>
                long.Parse(o.Number, NumberStyles.Integer, CultureInfo.InvariantCulture)
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(t0."Number" AS bigint)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ParsesTextAsDecimal()
    {
        Expression query = Orders
            .Select(o =>
                decimal.Parse(o.Number, NumberStyles.Float, CultureInfo.InvariantCulture)
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(t0."Number" AS numeric)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ParsesTextAsBoolean()
    {
        Expression query = Orders.Select(o => bool.Parse(o.Number)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(t0."Number" AS boolean)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ParsesTextAsDateOnly()
    {
        Expression query = Orders
            .Select(o => DateOnly.Parse(o.Number, CultureInfo.InvariantCulture))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(t0."Number" AS date)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ParsesTextAsTimeOnly()
    {
        Expression query = Orders
            .Select(o => TimeOnly.Parse(o.Number, CultureInfo.InvariantCulture))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(t0."Number" AS time)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ParsesTextAsUniversalDateTime()
    {
        Expression query = Orders
            .Select(o =>
                DateTime.Parse(
                    o.Number,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal
                )
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(t0."Number" AS timestamp)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ParsesTextAsGuid()
    {
        Expression query = Orders.Select(o => Guid.Parse(o.Number)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(t0."Number" AS uuid)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ParsesEmptyTextAsNull()
    {
        Expression query = Orders
            .Select(o =>
                string.IsNullOrEmpty(o.Note)
                    ? (long?)null
                    : long.Parse(
                        o.Note,
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture
                    )
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CASE WHEN (t0."Note" IS NULL) OR (t0."Note" = '') THEN NULL ELSE CAST(t0."Note" AS bigint) END
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByParsedLong()
    {
        Expression query = Orders
            .Where(o =>
                long.Parse(o.Number, NumberStyles.Integer, CultureInfo.InvariantCulture)
                > 100
            )
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE CAST(t0."Number" AS bigint) > 100
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesParsedGuidWithColumn()
    {
        Expression query = Orders
            .Where(o => Guid.Parse(o.Number) == o.TrackingId)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE CAST(t0."Number" AS uuid) = t0."TrackingId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ParsesTextAsInteger()
    {
        Expression query = Orders
            .Select(o =>
                int.Parse(o.Number, NumberStyles.Integer, CultureInfo.InvariantCulture)
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(t0."Number" AS integer)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ParsesTextAsDouble()
    {
        Expression query = Orders
            .Select(o => double.Parse(o.Number, CultureInfo.InvariantCulture))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(t0."Number" AS double precision)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ParsesTextAsDateTimeWithoutStyles()
    {
        Expression query = Orders
            .Select(o => DateTime.Parse(o.Number, CultureInfo.InvariantCulture))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(t0."Number" AS timestamp)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ParsesTextAsGuidWithProvider()
    {
        Expression query = Orders
            .Select(o => Guid.Parse(o.Number, CultureInfo.InvariantCulture))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(t0."Number" AS uuid)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RejectsParsingWithCurrentCulture()
    {
        Expression query = Orders.Select(o => long.Parse(o.Number)).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Parse", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsParsingDateWithCurrentCulture()
    {
        Expression query = Orders.Select(o => DateOnly.Parse(o.Number)).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Parse", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsParsingWithSpecificCulture()
    {
        Expression query = Orders
            .Select(o =>
                long.Parse(
                    o.Number,
                    NumberStyles.Integer,
                    CultureInfo.GetCultureInfo("de-DE")
                )
            )
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Parse", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsParsingWithComputedCulture()
    {
        Expression query = Orders
            .Select(o =>
                long.Parse(
                    o.Number,
                    NumberStyles.Integer,
                    o.IsPaid ? CultureInfo.InvariantCulture : CultureInfo.CurrentCulture
                )
            )
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Parse", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsParsingIntoDuration()
    {
        Expression query = Orders
            .Select(o => TimeSpan.Parse(o.Number, CultureInfo.InvariantCulture))
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Parse", exception.Message, StringComparison.Ordinal);
    }
}
