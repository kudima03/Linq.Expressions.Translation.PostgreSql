namespace Linq.Expressions.Translation.PostgreSql.Tests.Model;

public sealed record Customer
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Email { get; init; }

    public string Region { get; init; } = string.Empty;

    public bool IsActive { get; init; }
}
