namespace Linq.Expressions.Translation.PostgreSql.Tests.Infrastructure;

public sealed record RecordedTest(string Name, IReadOnlyList<ExpectedOutcome> Outcomes)
{
    public override string ToString()
    {
        return Name;
    }
}
