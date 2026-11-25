using System.Linq.Expressions;

namespace Linq.Expressions.Translation.PostgreSql;

// WITH RECURSIVE, which LINQ has no operator for. WithRecursive marks the query
// operator WITH RECURSIVE r AS (seed UNION ALL step(r)) SELECT ... FROM r: the rows of the
// seed, level 0, then the rows of the step over the rows of the level before, until
// a level is empty; every row of every level, duplicates included, in no order. The
// step reads the level exactly once, in FROM or an inner join, never in a subquery
// or on the optional side of an outer join, and does not group (PostgreSQL's rules
// for the recursive term); it yields the columns of the seed, by name. It exists
// only in expression trees, which a lambda or Expression.Call builds, and throws
// NotSupportedException when called.
public static class PostgreSqlRecursion
{
    public static IQueryable<TSource> WithRecursive<TSource>(
        this IQueryable<TSource> seed,
        Expression<Func<IQueryable<TSource>, IQueryable<TSource>>> step
    )
    {
        throw Marker.Called($"{nameof(PostgreSqlRecursion)}.{nameof(WithRecursive)}");
    }

    // As above, with levels up to maxDepth (the seed is level 0): a row of a deeper
    // level fails the statement, whatever the query around it reads, instead of
    // being cut off, so that the recursion ends even over cyclic data.
    public static IQueryable<TSource> WithRecursive<TSource>(
        this IQueryable<TSource> seed,
        Expression<Func<IQueryable<TSource>, IQueryable<TSource>>> step,
        long maxDepth
    )
    {
        throw Marker.Called($"{nameof(PostgreSqlRecursion)}.{nameof(WithRecursive)}");
    }
}
