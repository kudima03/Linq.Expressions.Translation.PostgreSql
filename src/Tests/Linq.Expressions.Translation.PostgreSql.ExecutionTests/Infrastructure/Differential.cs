using Xunit.Sdk;

namespace Linq.Expressions.Translation.PostgreSql.ExecutionTests.Infrastructure;

// Runs a query with LINQ to Objects over the Seed rows and as its translated SQL over
// the same rows in PostgreSQL, and requires the same results.
public static class Differential
{
    private const string Throws = "throws";

    private static readonly HashSet<string> Elements =
    [
        nameof(Queryable.First),
        nameof(Queryable.FirstOrDefault),
        nameof(Queryable.Single),
        nameof(Queryable.SingleOrDefault),
        nameof(Queryable.Last),
        nameof(Queryable.LastOrDefault),
        nameof(Queryable.ElementAt),
        nameof(Queryable.ElementAtOrDefault),
        nameof(Queryable.MinBy),
        nameof(Queryable.MaxBy),
    ];

    // The same rows in any order.
    public static void AssertSameRows<T>(this Database database, IQueryable<T> query)
    {
        Compare(database, query, false);
    }

    // The same rows in the same order; the query must order its rows totally.
    public static void AssertSameSequence<T>(this Database database, IQueryable<T> query)
    {
        Compare(database, query, true);
    }

    // The same result of a terminal operator; an operator that throws in memory (no
    // element, more than one element, no value to aggregate) must find no row, too
    // many rows or a NULL aggregate in SQL.
    public static void AssertSameResult<TSource, TResult>(
        this Database database,
        IQueryable<TSource> source,
        Func<IQueryable<TSource>, TResult> terminal
    )
    {
        Expression executed = source.Executed(terminal);
        string expected = InMemoryResult<TResult>(executed);
        ResultSet result = database.Query(new PostgreSqlQuery(executed).TextValue);
        string actual = SqlResult<TResult>(
            result,
            ((MethodCallExpression)executed).Method.Name
        );
        if (expected != actual)
        {
            throw new XunitException(Report(result.Sql, [expected], [actual]));
        }
    }

    private static void Compare<T>(Database database, IQueryable<T> query, bool ordered)
    {
        List<string> expected =
        [
            .. new EnumerableQuery<T>(InMemory.Rewrite(query.Expression))
                .AsEnumerable()
                .Select(row => Canonical.Describe(row)),
        ];
        Assert.True(expected.Count > 0, "The query must return rows to compare.");
        ResultSet result = database.Query(
            new PostgreSqlQuery(query.Expression).TextValue
        );
        List<string> actual =
        [
            .. result.Rows.Select(row =>
                Canonical.Describe(Materializer.Element(typeof(T), result, row))
            ),
        ];
        if (!ordered)
        {
            expected.Sort(StringComparer.Ordinal);
            actual.Sort(StringComparer.Ordinal);
        }
        if (!expected.SequenceEqual(actual, StringComparer.Ordinal))
        {
            throw new XunitException(Report(result.Sql, expected, actual));
        }
    }

    private static string InMemoryResult<TResult>(Expression executed)
    {
        IQueryProvider provider = new EnumerableQuery<TResult>([]);
        try
        {
            return Canonical.Describe(
                provider.Execute<TResult>(InMemory.Rewrite(executed))
            );
        }
        catch (Exception exception)
            when (exception is InvalidOperationException or ArgumentOutOfRangeException)
        {
            return Throws;
        }
    }

    private static string SqlResult<TResult>(ResultSet result, string name)
    {
        bool single = name.StartsWith(nameof(Queryable.Single), StringComparison.Ordinal);
        if (!Elements.Contains(name))
        {
            object?[] row = Assert.Single(result.Rows);
            _ = Assert.Single(result.Columns);
            return row[0] is null && IsRequired<TResult>()
                ? Throws
                : Canonical.Describe(Materializer.Element(typeof(TResult), result, row));
        }
        Assert.True(
            result.Rows.Count <= (single ? 2 : 1),
            $"Too many rows for {name}: {result.Sql}"
        );
        return result.Rows.Count > 1 ? Throws
            : result.Rows.Count == 1
                ? Canonical.Describe(
                    Materializer.Element(typeof(TResult), result, result.Rows[0])
                )
            : HasDefault<TResult>(name) ? Canonical.Describe(default(TResult))
            : Throws;
    }

    private static bool IsRequired<TResult>()
    {
        return typeof(TResult).IsValueType
            && Nullable.GetUnderlyingType(typeof(TResult)) is null;
    }

    // MinBy and MaxBy of no element are the default of a reference or nullable type.
    private static bool HasDefault<TResult>(string name)
    {
        return name.EndsWith("OrDefault", StringComparison.Ordinal)
            || (
                name is nameof(Queryable.MinBy) or nameof(Queryable.MaxBy)
                && !IsRequired<TResult>()
            );
    }

    private static string Report(
        string sql,
        IEnumerable<string> expected,
        IEnumerable<string> actual
    )
    {
        string line = Environment.NewLine;
        string item = line + "  ";
        return $"LINQ to Objects and PostgreSQL disagree.{line}SQL: {sql}{line}"
            + $"LINQ to Objects:{item}{string.Join(item, expected)}{line}"
            + $"PostgreSQL:{item}{string.Join(item, actual)}";
    }
}
