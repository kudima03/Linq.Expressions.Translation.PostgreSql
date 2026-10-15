namespace Linq.Expressions.Translation.PostgreSql.Tests.Infrastructure;

public static class QueryableExtensions
{
    public static Expression Executed<T, TResult>(
        this IQueryable<T> source,
        Func<IQueryable<T>, TResult> terminal
    )
    {
        _ = terminal(source);
        return ((CapturingQueryProvider)source.Provider).Executed;
    }
}
