using System.Linq.Expressions;

namespace Linq.Expressions.Translation.PostgreSql;

// Window functions, which LINQ has no operator for. Over marks the query operator
// SELECT selector FROM source, each window function of the selector
// OVER (PARTITION BY partitionBy ORDER BY orderBy frame): one result per row of the
// source, in its order. The window functions mark PostgreSQL's functions of the same
// name in that selector, with PostgreSQL's semantics. All of them exist only in
// expression trees, which a lambda or Expression.Call builds, and throw
// NotSupportedException when called.
public static class PostgreSqlWindows
{
    // partitionBy: the PARTITION BY keys, one value or a tuple or an object of them,
    // or null for one partition of every row. orderBy: the ORDER BY of the window, as
    // OrderBy, OrderByDescending, ThenBy and ThenByDescending of its rows (sorted as
    // in .NET: nulls first ascending; StringComparer.Ordinal sorts by code point), or
    // null for none. frame: the frame clause, or null for PostgreSQL's default (with
    // an ORDER BY, RANGE BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW; without, the
    // partition).
    public static IQueryable<TResult> Over<TSource, TResult>(
        this IQueryable<TSource> source,
        Expression<Func<TSource, object?>>? partitionBy,
        Expression<Func<IEnumerable<TSource>, IOrderedEnumerable<TSource>>>? orderBy,
        PostgreSqlFrame? frame,
        Expression<Func<TSource, PostgreSqlWindow<TSource>, TResult>> selector
    )
    {
        throw Marker.Called($"{nameof(PostgreSqlWindows)}.{nameof(Over)}");
    }

    // ROW_NUMBER(): the number of the row in its partition, from 1.
    public static long RowNumber<TSource>(this PostgreSqlWindow<TSource> window)
    {
        throw Marker.Called($"{nameof(PostgreSqlWindows)}.{nameof(RowNumber)}");
    }

    // RANK(): the row number of the first peer of the row, with gaps.
    public static long Rank<TSource>(this PostgreSqlWindow<TSource> window)
    {
        throw Marker.Called($"{nameof(PostgreSqlWindows)}.{nameof(Rank)}");
    }

    // DENSE_RANK(): the number of the row's group of peers, without gaps.
    public static long DenseRank<TSource>(this PostgreSqlWindow<TSource> window)
    {
        throw Marker.Called($"{nameof(PostgreSqlWindows)}.{nameof(DenseRank)}");
    }

    // PERCENT_RANK(): (rank - 1) / (rows of the partition - 1), 0 for one row.
    public static double PercentRank<TSource>(this PostgreSqlWindow<TSource> window)
    {
        throw Marker.Called($"{nameof(PostgreSqlWindows)}.{nameof(PercentRank)}");
    }

    // CUME_DIST(): the rows up to the row's last peer over the rows of the partition.
    public static double CumeDist<TSource>(this PostgreSqlWindow<TSource> window)
    {
        throw Marker.Called($"{nameof(PostgreSqlWindows)}.{nameof(CumeDist)}");
    }

    // NTILE(buckets): the bucket of the row, from 1, when the partition is divided
    // as equally as possible; buckets must be positive.
    public static int Ntile<TSource>(this PostgreSqlWindow<TSource> window, int buckets)
    {
        throw Marker.Called($"{nameof(PostgreSqlWindows)}.{nameof(Ntile)}");
    }

    // LAG(value[, offset[, default]]): the value in the row offset rows (1 by
    // default) before the current one in its partition, or default (NULL by default)
    // where there is none. The value is read in that row, the default in the
    // current row.
    public static TValue Lag<TSource, TValue>(
        this PostgreSqlWindow<TSource> window,
        Func<TSource, TValue> value
    )
    {
        throw Marker.Called($"{nameof(PostgreSqlWindows)}.{nameof(Lag)}");
    }

    public static TValue Lag<TSource, TValue>(
        this PostgreSqlWindow<TSource> window,
        Func<TSource, TValue> value,
        int offset
    )
    {
        throw Marker.Called($"{nameof(PostgreSqlWindows)}.{nameof(Lag)}");
    }

    public static TValue Lag<TSource, TValue>(
        this PostgreSqlWindow<TSource> window,
        Func<TSource, TValue> value,
        int offset,
        TValue defaultValue
    )
    {
        throw Marker.Called($"{nameof(PostgreSqlWindows)}.{nameof(Lag)}");
    }

    // LEAD(value[, offset[, default]]): as LAG, offset rows after the current one.
    public static TValue Lead<TSource, TValue>(
        this PostgreSqlWindow<TSource> window,
        Func<TSource, TValue> value
    )
    {
        throw Marker.Called($"{nameof(PostgreSqlWindows)}.{nameof(Lead)}");
    }

    public static TValue Lead<TSource, TValue>(
        this PostgreSqlWindow<TSource> window,
        Func<TSource, TValue> value,
        int offset
    )
    {
        throw Marker.Called($"{nameof(PostgreSqlWindows)}.{nameof(Lead)}");
    }

    public static TValue Lead<TSource, TValue>(
        this PostgreSqlWindow<TSource> window,
        Func<TSource, TValue> value,
        int offset,
        TValue defaultValue
    )
    {
        throw Marker.Called($"{nameof(PostgreSqlWindows)}.{nameof(Lead)}");
    }

    // FIRST_VALUE(value): the value in the first row of the frame, NULL for none.
    public static TValue FirstValue<TSource, TValue>(
        this PostgreSqlWindow<TSource> window,
        Func<TSource, TValue> value
    )
    {
        throw Marker.Called($"{nameof(PostgreSqlWindows)}.{nameof(FirstValue)}");
    }

    // LAST_VALUE(value): the value in the last row of the frame, NULL for none.
    public static TValue LastValue<TSource, TValue>(
        this PostgreSqlWindow<TSource> window,
        Func<TSource, TValue> value
    )
    {
        throw Marker.Called($"{nameof(PostgreSqlWindows)}.{nameof(LastValue)}");
    }

    // NTH_VALUE(value, n): the value in row n of the frame, from 1, NULL for none.
    public static TValue NthValue<TSource, TValue>(
        this PostgreSqlWindow<TSource> window,
        Func<TSource, TValue> value,
        int n
    )
    {
        throw Marker.Called($"{nameof(PostgreSqlWindows)}.{nameof(NthValue)}");
    }
}
