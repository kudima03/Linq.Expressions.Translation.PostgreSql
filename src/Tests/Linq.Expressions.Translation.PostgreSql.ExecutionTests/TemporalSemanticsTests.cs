namespace Linq.Expressions.Translation.PostgreSql.ExecutionTests;

// The tests run in America/New_York (execution.runsettings), so the conversions cross
// a non-zero offset and daylight saving time.
[Collection(SharedDatabase.Name)]
public sealed class TemporalSemanticsTests(Database database)
{
    [Fact]
    public void ComparesDateTimeWithDateTimeOffsetColumn()
    {
        database.AssertSameSequence(
            Orders
                .Where(o => o.PaidAt != null)
                .OrderBy(o => o.Id)
                .Select(o => new
                {
                    o.Id,
                    Later = o.PaidAt > o.CreatedAt,
                    Same = o.PaidAt == o.CreatedAt,
                    Earlier = o.CreatedAt > o.PaidAt,
                })
        );
    }

    [Fact]
    public void ComparesDateTimeWithDateTimeOffsetConstant()
    {
        DateTimeOffset cutoff = new(2024, 1, 15, 10, 30, 0, TimeSpan.FromHours(-5));

        database.AssertSameSequence(
            Orders
                .OrderBy(o => o.Id)
                .Select(o => new
                {
                    o.Id,
                    Since = o.CreatedAt >= cutoff,
                    Paid = o.PaidAt >= cutoff,
                })
        );
    }

    [Fact(Skip = "#61: a Utc DateTime constant compares through the session time zone")]
    public void ComparesDateTimeWithUtcDateTimeConstant()
    {
        DateTime noon = new(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc);

        database.AssertSameSequence(
            Orders
                .OrderBy(o => o.Id)
                .Select(o => new { o.Id, Later = o.CreatedAt > noon })
        );
    }
}
