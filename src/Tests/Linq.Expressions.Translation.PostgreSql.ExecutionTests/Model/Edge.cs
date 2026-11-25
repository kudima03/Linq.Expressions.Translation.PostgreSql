namespace Linq.Expressions.Translation.PostgreSql.ExecutionTests.Model;

// A directed graph with a cycle (1 -> 2 -> 3 -> 1), a self-loop (5 -> 5), a branch
// and a node without edges out (4), for recursive walks.
public sealed record Edge
{
    public int Id { get; init; }

    public int From { get; init; }

    public int To { get; init; }

    public int? Cost { get; init; }
}
