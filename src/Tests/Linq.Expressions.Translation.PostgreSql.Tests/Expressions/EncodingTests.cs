using System.Text;

namespace Linq.Expressions.Translation.PostgreSql.Tests.Expressions;

// A bytea column holding the UTF-8 bytes of a text is read as that text with
// Encoding.UTF8.GetString, over which the text operations compose; the text has the
// collation of the database, as a text column has, not the "C" one convert_from
// derives from the name of the encoding.
public sealed record EncodingTests
{
    [Fact]
    public void MapsByteArrayColumns()
    {
        Expression query = Attachments.Where(a => a.OrderId == 1).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."OrderId", t0."Name", t0."Content", t0."Checksum"
                FROM "Attachment" AS t0
                WHERE t0."OrderId" = 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ReadsBytesAsText()
    {
        Expression query = Attachments
            .Select(a => Encoding.UTF8.GetString(a.Content))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CONVERT_FROM(t0."Content", 'UTF8') COLLATE "default"
                FROM "Attachment" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ReadsBytesOfAnotherUtf8EncodingInstanceAsText()
    {
        UTF8Encoding encoding = new(false);
        Expression query = Attachments
            .Select(a => encoding.GetString(a.Content))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CONVERT_FROM(t0."Content", 'UTF8') COLLATE "default"
                FROM "Attachment" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void UppercasesAndTrimsTextOfBytes()
    {
        Expression query = Attachments
            .Select(a => new
            {
                Upper = Encoding.UTF8.GetString(a.Content).ToUpperInvariant(),
                Trimmed = Encoding.UTF8.GetString(a.Content).Trim(),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                $"""
                SELECT UPPER(CONVERT_FROM(t0."Content", 'UTF8') COLLATE "default") AS "Upper", BTRIM(CONVERT_FROM(t0."Content", 'UTF8') COLLATE "default", {Sql.WhiteSpace}) AS "Trimmed"
                FROM "Attachment" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesTextOfBytesWithText()
    {
        Expression query = Attachments
            .Where(a => Encoding.UTF8.GetString(a.Content) == "A-001")
            .Select(a => a.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Attachment" AS t0
                WHERE CONVERT_FROM(t0."Content", 'UTF8') COLLATE "default" = 'A-001'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ReadsNullableBytesAsNullableText()
    {
        Expression query = Attachments
            .Where(a => Encoding.UTF8.GetString(a.Checksum!) == "A-001")
            .Select(a => a.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Attachment" AS t0
                WHERE CONVERT_FROM(t0."Checksum", 'UTF8') COLLATE "default" IS NOT DISTINCT FROM 'A-001'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsOnTextOfBytes()
    {
        Expression query = Attachments
            .Join(
                Orders,
                a => Encoding.UTF8.GetString(a.Content),
                o => o.Number,
                (a, o) => new { a.Name, o.Total }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t1."Total"
                FROM "Attachment" AS t0
                INNER JOIN "Order" AS t1 ON CONVERT_FROM(t0."Content", 'UTF8') COLLATE "default" = t1."Number"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void UnionsTextOfBytesWithText()
    {
        Expression query = Attachments
            .Select(a => Encoding.UTF8.GetString(a.Content))
            .Union(Orders.Select(o => o.Number))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CONVERT_FROM(t0."Content", 'UTF8') COLLATE "default"
                FROM "Attachment" AS t0
                UNION
                SELECT t1."Number"
                FROM "Order" AS t1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void WritesTextAsBytes()
    {
        Expression query = Attachments
            .Select(a => Encoding.UTF8.GetBytes(a.Name))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CONVERT_TO(t0."Name", 'UTF8')
                FROM "Attachment" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesBytesWithBytesOfText()
    {
        Expression query = Attachments
            .Where(a => a.Content == Encoding.UTF8.GetBytes(a.Name))
            .Select(a => a.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Attachment" AS t0
                WHERE t0."Content" = CONVERT_TO(t0."Name", 'UTF8')
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesBytesWithBytesOfConstantText()
    {
        Expression query = Attachments
            .Where(a => a.Content == Encoding.UTF8.GetBytes("é"))
            .Select(a => a.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Attachment" AS t0
                WHERE t0."Content" = BYTEA '\xc3a9'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RejectsOtherEncodings()
    {
        Expression query = Attachments
            .Select(a => Encoding.ASCII.GetString(a.Content))
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("us-ascii", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsReadingPartOfBytes()
    {
        Expression query = Attachments
            .Select(a => Encoding.UTF8.GetString(a.Content, 0, 2))
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("GetString", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsCountingBytes()
    {
        Expression query = Attachments
            .Select(a => Encoding.UTF8.GetByteCount(a.Name))
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("GetByteCount", exception.Message, StringComparison.Ordinal);
    }
}
