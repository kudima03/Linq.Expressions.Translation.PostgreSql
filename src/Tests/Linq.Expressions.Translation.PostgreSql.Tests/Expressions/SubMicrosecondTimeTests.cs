#pragma warning disable CA1861 // the constant arrays are the sources under test

namespace Linq.Expressions.Translation.PostgreSql.Tests.Expressions;

// TimeOnly and TimeSpan constants finer than a microsecond, which PostgreSQL's time
// and interval cannot hold, are refused as a DateTime constant finer than a
// microsecond is, whether compared, computed with or read as VALUES.
public sealed record SubMicrosecondTimeTests
{
    // #130: TimeOnly.MaxValue renders as TIME '23:59:59.999999', the seventh digit
    // dropped by HH:mm:ss.FFFFFF.
    [Fact]
    public void RefusesTimeConstantFinerThanMicrosecond()
    {
        Expression query = Orders
            .Where(o => o.DeliveryTime == TimeOnly.MaxValue)
            .Select(o => o.Id)
            .Expression;

        AssertRefused(query);
    }

    // #130: VALUES (TIME '23:59:59.999999'), (TIME '00:00:01') joined to the time
    // column matches the stored 23:59:59.999999 and 00:00:01, which the values
    // differ from.
    [Fact]
    public void RefusesTimeValuesFinerThanMicrosecond()
    {
        Expression query = Orders
            .Join(
                new[]
                {
                    TimeOnly.MaxValue,
                    new TimeOnly(0, 0, 1).Add(TimeSpan.FromTicks(1)),
                }.AsQueryable(),
                o => o.DeliveryTime,
                t => t,
                (o, t) => o.Id
            )
            .Expression;

        AssertRefused(query);
    }

    // #130: INTERVAL 'PT0.0000015S', which PostgreSQL rounds to 00:00:00.000002.
    [Fact]
    public void RefusesIntervalConstantFinerThanMicrosecond()
    {
        Expression query = Orders
            .Select(o => o.CreatedAt + TimeSpan.FromTicks(15))
            .Expression;

        AssertRefused(query);
    }

    // #130: VALUES (INTERVAL 'PT0.0000001S'), (INTERVAL 'PT0.0000015S'), which
    // PostgreSQL rounds to 00:00:00 and 00:00:00.000002.
    [Fact]
    public void RefusesIntervalValuesFinerThanMicrosecond()
    {
        Expression query = new[] { TimeSpan.FromTicks(1), TimeSpan.FromTicks(15) }
            .AsQueryable()
            .Expression;

        AssertRefused(query);
    }

    // #144 (5): a captured TimeOnly of 23:59:59.9999995 used in a computed value is
    // read back as 23:59:59.999999.
    [Fact]
    public void RefusesCapturedTimeFinerThanMicrosecondInComputedValue()
    {
        TimeOnly fine = new TimeOnly(23, 59, 59).Add(TimeSpan.FromTicks(9_999_995));
        Expression query = Orders
            .Select(o => new { o.Id, Time = fine.AddMinutes(o.Quantity) })
            .Expression;

        AssertRefused(query);
    }

    // A constant whole number of microseconds is still translated.
    [Fact]
    public void TranslatesTimeConstantsOfWholeMicroseconds()
    {
        Expression query = Orders
            .Where(o =>
                o.DeliveryTime == new TimeOnly(23, 59, 59, 999, 999)
                && o.CreatedAt + TimeSpan.FromTicks(20) > o.CreatedAt
            )
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."DeliveryTime" = TIME '23:59:59.999999') AND ((t0."CreatedAt" + INTERVAL 'PT0.000002S') > t0."CreatedAt")
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    private static void AssertRefused(Expression query)
    {
        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("microsecond", exception.Message, StringComparison.Ordinal);
    }
}
