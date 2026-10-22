using System.Diagnostics;
using Xunit.Abstractions;

namespace Linq.Expressions.Translation.PostgreSql.PerformanceTests;

public sealed class TranslationTimeTests(ITestOutputHelper output)
{
    private static readonly TimeSpan Budget = TimeSpan.FromMilliseconds(100);

    public static TheoryData<string> Names => [.. Samples.All.Select(s => s.Name)];

    // Cold: no warm-up, so the first sample run also pays for JIT and the column mapping.
    [Theory]
    [MemberData(nameof(Names))]
    public void TranslatesWithinBudget(string name)
    {
        Expression query = Samples.All.Single(s => s.Name == name).Query();

        Stopwatch stopwatch = Stopwatch.StartNew();
        string sql = new PostgreSqlQuery(query).TextValue;
        stopwatch.Stop();

        output.WriteLine($"{stopwatch.Elapsed.TotalMilliseconds:F1} ms: {sql}");
        Assert.True(
            stopwatch.Elapsed < Budget,
            $"{name} took {stopwatch.Elapsed.TotalMilliseconds:F1} ms"
        );
        Assert.Equal(sql, Sql.Valid(sql));
    }
}
