using Xunit.Sdk;

namespace Linq.Expressions.Translation.PostgreSql.ExecutionTests;

// A time PostgreSQL cannot hold, finer than its microsecond, is not read back as
// another time: a TimeOnly built from ticks that are not a whole number of
// microseconds is refused, by the translator or by the server, as a DateTime finer
// than a microsecond is.
[Collection(SharedDatabase.Name)]
public sealed class SubMicrosecondTimeSemanticsTests(Database database)
{
    private static IQueryable<Order> First => Orders.Where(o => o.Id == 1);

    // #130: MAKE_TIME(...) + ((ticks / 10) * INTERVAL '1 microsecond') drops the
    // ticks below a microsecond, so 15 ticks read back as 00:00:00.000001 where .NET
    // gives 00:00:00.0000015.
    [Fact]
    public void RefusesTimeOfTicksFinerThanMicrosecond()
    {
        IQueryable<TimeOnly> query = First.Select(o => new TimeOnly(o.Id * 15L));

        Assert.Equal(
            new TimeOnly(15),
            new EnumerableQuery<TimeOnly>(InMemory.Rewrite(query.Expression)).Single()
        );
        AssertRefused(query);
    }

    // #130: the same ticks below a microsecond added to a time of day.
    [Fact]
    public void RefusesTimeOfDayWithTicksFinerThanMicrosecond()
    {
        AssertRefused(
            First.Select(o => new TimeOnly((o.Id * TimeSpan.TicksPerHour) + 5))
        );
    }

    // A refusal by the translator, or a statement the server fails, not a row.
    private void AssertRefused<T>(IQueryable<T> query)
    {
        string sql;
        try
        {
            sql = new PostgreSqlQuery(query.Expression).TextValue;
        }
        catch (NotSupportedException)
        {
            return;
        }
        _ = Assert.Throws<XunitException>(() => database.Query(sql));
    }
}
