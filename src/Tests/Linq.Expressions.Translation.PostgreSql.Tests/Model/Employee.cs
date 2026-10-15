namespace Linq.Expressions.Translation.PostgreSql.Tests.Model;

public sealed record Employee
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public int? ManagerId { get; init; }
}
