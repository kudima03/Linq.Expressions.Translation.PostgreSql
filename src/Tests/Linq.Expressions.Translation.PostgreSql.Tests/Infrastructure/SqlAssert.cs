namespace Linq.Expressions.Translation.PostgreSql.Tests.Infrastructure;

public static class SqlAssert
{
    private static readonly AsyncLocal<ICollection<ExpectedOutcome>?> Recording = new();

    public static void Translates(string expected, IQueryable query)
    {
        Verify(expected, query.Expression);
    }

    public static void Translates<T, TResult>(
        string expected,
        IQueryable<T> source,
        Func<IQueryable<T>, TResult> terminal
    )
    {
        Verify(expected, Executed(source, terminal));
    }

    public static void NotSupported(IQueryable query, string construct)
    {
        Reject(query.Expression, construct);
    }

    public static void NotSupported<T, TResult>(
        IQueryable<T> source,
        Func<IQueryable<T>, TResult> terminal,
        string construct
    )
    {
        Reject(Executed(source, terminal), construct);
    }

    public static IReadOnlyList<ExpectedOutcome> Record(Action test)
    {
        List<ExpectedOutcome> outcomes = [];
        Recording.Value = outcomes;
        try
        {
            test();
        }
        finally
        {
            Recording.Value = null;
        }
        return outcomes;
    }

    private static Expression Executed<T, TResult>(
        IQueryable<T> source,
        Func<IQueryable<T>, TResult> terminal
    )
    {
        _ = terminal(source);
        return ((CapturingQueryProvider)source.Provider).Executed;
    }

    private static void Verify(string expected, Expression expression)
    {
        string statement = string.Join(
            ' ',
            expected.ReplaceLineEndings("\n").Split('\n')
        );
        if (Recording.Value is ICollection<ExpectedOutcome> outcomes)
        {
            outcomes.Add(new ExpectedOutcome(statement));
            return;
        }
        Assert.Equal(statement, new PostgreSqlQuery(expression).TextValue);
    }

    private static void Reject(Expression expression, string construct)
    {
        if (Recording.Value is ICollection<ExpectedOutcome> outcomes)
        {
            outcomes.Add(new ExpectedOutcome(null));
            return;
        }
        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(expression).TextValue
        );
        Assert.Contains(construct, exception.Message, StringComparison.Ordinal);
    }
}
