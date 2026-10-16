#pragma warning disable CA2241 // format strings without items are inputs under test
using System.Globalization;

namespace Linq.Expressions.Translation.PostgreSql.Tests.Expressions;

public sealed record StringFormatTests
{
    [Fact]
    public void FormatsSingleInteger()
    {
        Expression query = Orders.Select(o => string.Format("{0}", o.Id)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(t0."Id" AS text)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FormatsSingleString()
    {
        Expression query = Orders.Select(o => string.Format("{0}", o.Number)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Number"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void InterpolatesNullableString()
    {
        Expression query = Orders.Select(o => $"{o.Note}").Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(t0."Note", '')
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void InterpolatesNullableInteger()
    {
        Expression query = Orders.Select(o => $"{o.Priority}").Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(CAST(t0."Priority" AS text), '')
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FormatsEscapedBraces()
    {
        Expression query = Orders
            .Select(o => string.Format("{{{0}}}", o.Number))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT '{' || t0."Number" || '}'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FormatsRepeatedItem()
    {
        Expression query = Orders
            .Select(o => string.Format("{0}/{0}", o.Number))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Number" || '/' || t0."Number"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FormatsThreeItems()
    {
        Expression query = Orders
            .Select(o => string.Format("{0}{1}{2}", o.Number, o.Quantity, o.Note))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Number" || t0."Quantity" || COALESCE(t0."Note", '')
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void InterpolatesFourItems()
    {
        Expression query = Orders
            .Select(o => $"{o.Id}:{o.Number}:{o.Quantity}:{o.Note}")
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id" || ':' || t0."Number" || ':' || t0."Quantity" || ':' || COALESCE(t0."Note", '')
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FormatsLiteralWithoutItems()
    {
        Expression query = Orders.Select(o => string.Format("none", o.Id)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT 'none'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FormatsEmptyText()
    {
        Expression query = Orders.Select(o => string.Format("", o.Id)).Expression;

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
    public void FiltersByInterpolatedText()
    {
        Expression query = Orders
            .Where(o => $"{o.Number}-{o.Quantity}" == "A-1")
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."Number" || '-' || t0."Quantity") = 'A-1'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ConcatenatesInterpolatedText()
    {
        Expression query = Orders.Select(o => $"[{o.Number}]" + o.Note).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT '[' || t0."Number" || ']' || COALESCE(t0."Note", '')
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RejectsInterpolatedBoolean()
    {
        Expression query = Orders.Select(o => $"paid: {o.IsPaid}").Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("'Boolean'", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsAlignedFormatItem()
    {
        Expression query = Orders
            .Select(o => string.Format("{0,5}", o.Number))
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("{0,5}", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsInterpolationWithFormatString()
    {
        Expression query = Orders.Select(o => $"{o.Total:N2}").Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("{0:N2}", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsComputedFormat()
    {
        Expression query = Orders.Select(o => string.Format(o.Number, o.Id)).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Format", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsFormatProvider()
    {
        Expression query = Orders
            .Select(o => string.Format(CultureInfo.InvariantCulture, "{0}", o.Id))
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Format", exception.Message, StringComparison.Ordinal);
    }
}
