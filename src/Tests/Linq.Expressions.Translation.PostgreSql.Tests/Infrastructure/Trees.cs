using System.Reflection;

namespace Linq.Expressions.Translation.PostgreSql.Tests.Infrastructure;

// Builds the calls of the query markers into a query's tree, as a provider or a
// tree rewriter does: called, the markers throw.
public static class Trees
{
    public static IQueryable<TResult> Over<TSource, TResult>(
        IQueryable<TSource> source,
        Expression<Func<TSource, object?>>? partitionBy,
        Expression<Func<IEnumerable<TSource>, IOrderedEnumerable<TSource>>>? orderBy,
        PostgreSqlFrame? frame,
        Expression<Func<TSource, PostgreSqlWindow<TSource>, TResult>> selector
    )
    {
        return source.Provider.CreateQuery<TResult>(
            Expression.Call(
                Method(typeof(PostgreSqlWindows), nameof(PostgreSqlWindows.Over), 5)
                    .MakeGenericMethod(typeof(TSource), typeof(TResult)),
                source.Expression,
                Quoted(partitionBy),
                Quoted(orderBy),
                Expression.Constant(frame, typeof(PostgreSqlFrame)),
                Expression.Quote(selector)
            )
        );
    }

    public static IQueryable<TSource> WithRecursive<TSource>(
        IQueryable<TSource> seed,
        Expression<Func<IQueryable<TSource>, IQueryable<TSource>>> step
    )
    {
        return seed.Provider.CreateQuery<TSource>(
            Expression.Call(
                Method(
                        typeof(PostgreSqlRecursion),
                        nameof(PostgreSqlRecursion.WithRecursive),
                        2
                    )
                    .MakeGenericMethod(typeof(TSource)),
                seed.Expression,
                Expression.Quote(step)
            )
        );
    }

    public static IQueryable<TSource> WithRecursive<TSource>(
        IQueryable<TSource> seed,
        Expression<Func<IQueryable<TSource>, IQueryable<TSource>>> step,
        long maxDepth
    )
    {
        return seed.Provider.CreateQuery<TSource>(
            Expression.Call(
                Method(
                        typeof(PostgreSqlRecursion),
                        nameof(PostgreSqlRecursion.WithRecursive),
                        3
                    )
                    .MakeGenericMethod(typeof(TSource)),
                seed.Expression,
                Expression.Quote(step),
                Expression.Constant(maxDepth)
            )
        );
    }

    private static MethodInfo Method(Type type, string name, int parameters)
    {
        return type.GetMethods()
            .Single(method =>
                method.Name == name && method.GetParameters().Length == parameters
            );
    }

    private static Expression Quoted<TDelegate>(Expression<TDelegate>? lambda)
    {
        return lambda is null
            ? Expression.Constant(null, typeof(Expression<TDelegate>))
            : Expression.Quote(lambda);
    }
}
