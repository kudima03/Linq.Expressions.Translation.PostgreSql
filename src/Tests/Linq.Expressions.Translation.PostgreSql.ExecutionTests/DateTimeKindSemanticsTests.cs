#pragma warning disable CA1861 // the constant arrays are the sources under test

using Xunit.Sdk;

namespace Linq.Expressions.Translation.PostgreSql.ExecutionTests;

// The kind of a DateTime read back from SQL: a value LINQ to Objects gives the kind
// Utc reads back as Utc, and a value of another kind does not take the kind Utc from
// a value it is mixed with: a column that would be of the kind Utc on some rows and
// Unspecified on others is refused, as a PostgreSQL column is a timestamp or a
// timestamptz. Canonical compares timestamps without their kind, so each row is
// compared as its canonical text and the kind of the DateTime it holds.
[Collection(SharedDatabase.Name)]
public sealed class DateTimeKindSemanticsTests(Database database)
{
    // #129: a Utc constant read back as a result is a TIMESTAMP literal, a timestamp,
    // read with the kind Unspecified.
    [Fact]
    public void ReadsUtcConstantWithItsKind()
    {
        AssertSameKinds(
            Orders.Select(o => new
            {
                o.Id,
                Moment = new DateTime(2005, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            }),
            row => row.Moment
        );
    }

    // #129: VALUES (TIMESTAMP '2000-01-01 00:00:00') for an element of the kind Utc.
    [Fact]
    public void ReadsUtcInMemoryValuesWithTheirKind()
    {
        AssertSameKinds(
            new[]
            {
                new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            }.AsQueryable(),
            moment => moment
        );
    }

    // #129: a CASE of a Utc and an unspecified DateTime is of one kind on some rows
    // and of the other on the rest, which no column can be read as.
    [Fact]
    public void RefusesConditionalBranchesOfTwoKinds()
    {
        AssertRefused(
            Orders.Select(o => new
            {
                o.Id,
                Moment = o.Id % 2 == 0
                    ? new DateTime(2005, 1, 1, o.Quantity, 0, 0, DateTimeKind.Utc)
                    : new DateTime(2005, 1, 1, o.Quantity, 0, 0),
            })
        );
    }

    // #129: the default of FirstOrDefault, default(DateTime), is Unspecified where
    // the values of the group are Utc.
    [Fact]
    public void RefusesUtcValuesWithAnUnspecifiedDefault()
    {
        AssertRefused(
            Orders
                .GroupBy(o => o.CustomerId)
                .Select(g => new
                {
                    g.Key,
                    First = g.Where(o => o.Quantity > 5)
                        .Select(o => new DateTime(
                            2024,
                            1,
                            1,
                            o.Quantity,
                            0,
                            0,
                            DateTimeKind.Utc
                        ))
                        .FirstOrDefault(),
                })
        );
    }

    // #129: Max returns the Utc value for the orders before 2005 and the unspecified
    // one for the others.
    [Fact]
    public void RefusesMaximumOfTwoKinds()
    {
        AssertRefused(
            Orders.Select(o => new
            {
                o.Id,
                Latest = new[]
                {
                    new DateTime(2000 + o.Id, 1, 1),
                    new DateTime(2005, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                }.Max(),
            })
        );
    }

    // #129: Min returns the unspecified value for the orders from 2005 and the Utc
    // one for the others.
    [Fact]
    public void RefusesMinimumOfTwoKinds()
    {
        AssertRefused(
            Orders.Select(o => new
            {
                o.Id,
                Earliest = new[]
                {
                    new DateTime(2000 + o.Id, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    new DateTime(2005, 1, 1),
                }.Min(),
            })
        );
    }

    private static void AssertRefused<T>(IQueryable<T> query)
    {
        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query.Expression).TextValue
        );
        Assert.Contains("Utc on some rows", exception.Message, StringComparison.Ordinal);
    }

    // The rows of both sides in any order, each with the kind of its DateTime.
    private void AssertSameKinds<T>(IQueryable<T> query, Func<T, DateTime> moment)
    {
        List<string> expected =
        [
            .. new EnumerableQuery<T>(InMemory.Rewrite(query.Expression))
                .AsEnumerable()
                .Select(row => Describe(row, moment)),
        ];
        Assert.True(expected.Count > 0, "The query must return rows to compare.");
        ResultSet result = database.Query(
            new PostgreSqlQuery(query.Expression).TextValue
        );
        List<string> actual =
        [
            .. result.Rows.Select(row =>
                Describe((T)Materializer.Element(typeof(T), result, row)!, moment)
            ),
        ];
        expected.Sort(StringComparer.Ordinal);
        actual.Sort(StringComparer.Ordinal);
        if (!expected.SequenceEqual(actual, StringComparer.Ordinal))
        {
            string line = Environment.NewLine + "  ";
            throw new XunitException(
                $"LINQ to Objects and PostgreSQL disagree.{Environment.NewLine}"
                    + $"SQL: {result.Sql}{Environment.NewLine}"
                    + $"LINQ to Objects:{line}{string.Join(line, expected)}"
                    + $"{Environment.NewLine}PostgreSQL:{line}{string.Join(line, actual)}"
            );
        }
    }

    private static string Describe<T>(T row, Func<T, DateTime> moment)
    {
        return $"{Canonical.Describe(row)} {moment(row).Kind}";
    }
}
