using System.Text;

namespace Linq.Expressions.Translation.PostgreSql.ExecutionTests;

// The bytes of a bytea column read as UTF-8 text are the text, over which the text
// operations run as over a text column.
[Collection(SharedDatabase.Name)]
public sealed class EncodingSemanticsTests(Database database)
{
    [Fact]
    public void ReadsBytesAsText()
    {
        database.AssertSameSequence(
            Attachments
                .OrderBy(a => a.Id)
                .Select(a => new
                {
                    a.Id,
                    Content = Encoding.UTF8.GetString(a.Content),
                    Checksum = a.Checksum == null
                        ? null
                        : Encoding.UTF8.GetString(a.Checksum),
                })
        );
    }

    [Fact]
    public void RunsTextOperationsOverTextOfBytes()
    {
        database.AssertSameSequence(
            Attachments
                .OrderBy(a => a.Id)
                .Select(a => new
                {
                    a.Id,
                    Upper = Encoding.UTF8.GetString(a.Content).ToUpperInvariant(),
                    Trimmed = Encoding.UTF8.GetString(a.Content).Trim(),
                    Lower = Encoding.UTF8.GetString(a.Content).ToLowerInvariant(),
                    Dashed = Encoding.UTF8.GetString(a.Content).Contains('-'),
                    Replaced = Encoding.UTF8.GetString(a.Content).Replace("-", "/"),
                })
        );
    }

    [Fact]
    public void FiltersByTextOfBytes()
    {
        database.AssertSameRows(
            Attachments
                .Where(a => Encoding.UTF8.GetString(a.Content).StartsWith("A-"))
                .Select(a => a.Id)
        );
    }

    [Fact]
    public void JoinsTextOfBytesWithText()
    {
        database.AssertSameRows(
            Attachments.Join(
                Orders,
                a => Encoding.UTF8.GetString(a.Content),
                o => o.Number,
                (a, o) => new { a.Name, o.Total }
            )
        );
    }

    [Fact]
    public void UnionsTextOfBytesWithText()
    {
        database.AssertSameRows(
            Attachments
                .Select(a => Encoding.UTF8.GetString(a.Content))
                .Union(Orders.Select(o => o.Number))
        );
    }

    [Fact]
    public void WritesTextAsBytes()
    {
        database.AssertSameSequence(
            Attachments
                .OrderBy(a => a.Id)
                .Select(a => new
                {
                    a.Id,
                    Name = Encoding.UTF8.GetBytes(a.Name),
                    Constant = Encoding.UTF8.GetBytes("O'Brien é \U0001F600"),
                    Empty = Encoding.UTF8.GetBytes(string.Empty),
                })
        );
    }

    [Fact]
    public void FiltersByBytesOfText()
    {
        database.AssertSameRows(
            Attachments
                .Where(a =>
                    Encoding.UTF8.GetString(a.Content) == a.Name
                    || Encoding.UTF8.GetString(a.Content) == "é"
                    || (
                        a.Checksum != null
                        && Encoding.UTF8.GetString(a.Checksum) == "O'Brien"
                    )
                )
                .Select(a => a.Id)
        );
    }
}
