namespace Linq.Expressions.Translation.PostgreSql.Tests.Model;

// A table with bytea columns, holding the UTF-8 bytes of a text.
public sealed record Attachment
{
    public int Id { get; init; }

    public int OrderId { get; init; }

    public string Name { get; init; } = string.Empty;

    public byte[] Content { get; init; } = [];

    public byte[]? Checksum { get; init; }
}
