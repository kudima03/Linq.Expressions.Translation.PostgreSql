namespace Linq.Expressions.Translation.PostgreSql.Tests.Model;

public sealed record CustomerCard
{
    public int Id { get; init; }

    public string Title { get; init; } = string.Empty;
}
