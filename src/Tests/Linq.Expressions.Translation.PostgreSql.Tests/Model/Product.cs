namespace Linq.Expressions.Translation.PostgreSql.Tests.Model;

public sealed record Product : INamed
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public decimal Price { get; init; }

    public int? CategoryId { get; init; }
}
