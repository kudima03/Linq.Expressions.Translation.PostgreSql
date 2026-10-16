namespace Linq.Expressions.Translation.PostgreSql.Tests.Expressions;

public sealed record StringConcatenationTypeTests
{
    [Fact]
    public void ConcatenatesNullableDecimalAsText()
    {
        Expression query = Orders.Select(o => o.Number + o.Discount).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Number" || COALESCE(CAST(t0."Discount" AS text), '')
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ConcatenatesLong()
    {
        Expression query = Orders.Select(o => o.Number + o.Points).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Number" || t0."Points"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ConcatenatesDouble()
    {
        Expression query = Orders.Select(o => o.Number + ":" + o.Weight).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Number" || ':' || t0."Weight"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ConcatenatesGuid()
    {
        Expression query = Orders.Select(o => "#" + o.TrackingId).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT '#' || t0."TrackingId"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ConcatenatesCapturedCharacter()
    {
        char separator = '/';
        Expression query = Orders.Select(o => o.Number + separator).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Number" || '/'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ConcatenatesObjectArray()
    {
        Expression query = Orders
            .Select(o => string.Concat(new object[] { o.Number, o.Quantity }))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Number" || t0."Quantity"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RejectsConcatenatingCapturedBoolean()
    {
        bool flag = true;
        Expression query = Orders.Select(o => o.Number + flag).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("'Boolean'", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsConcatenatingTimeOnly()
    {
        Expression query = Orders.Select(o => o.Number + o.DeliveryTime).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("'TimeOnly'", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsConcatenatingNullableDateOnly()
    {
        Expression query = Orders.Select(o => o.Number + o.ShippedOn).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("'DateOnly'", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsConcatenatingStringArrayColumn()
    {
        Expression query = Orders.Select(o => string.Concat(o.Tags)).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("'String[]'", exception.Message, StringComparison.Ordinal);
    }
}
