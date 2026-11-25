namespace Linq.Expressions.Translation.PostgreSql;

// PostgreSQL's statistical aggregate functions, which .NET has no equivalent of,
// with PostgreSQL's semantics: they skip NULL values (the two-column ones every row
// where either is NULL) and are NULL over no values, except REGR_COUNT, which is 0.
// rows are the rows of a group of GroupBy, the rows of a window's frame
// (PostgreSqlWindow<TSource>.Rows) or a query, each possibly filtered by Where
// (FILTER (WHERE ...)); the selectors read only their row. They mark the functions
// in expression trees and throw when called.
public static class PostgreSqlFunctions
{
    // STDDEV_POP(value): the population standard deviation.
    public static double? StddevPop<TSource>(
        IEnumerable<TSource> rows,
        Func<TSource, double?> value
    )
    {
        throw Marker.Called($"{nameof(PostgreSqlFunctions)}.{nameof(StddevPop)}");
    }

    public static decimal? StddevPop<TSource>(
        IEnumerable<TSource> rows,
        Func<TSource, decimal?> value
    )
    {
        throw Marker.Called($"{nameof(PostgreSqlFunctions)}.{nameof(StddevPop)}");
    }

    // STDDEV_SAMP(value): the sample standard deviation, NULL for fewer than two values.
    public static double? StddevSamp<TSource>(
        IEnumerable<TSource> rows,
        Func<TSource, double?> value
    )
    {
        throw Marker.Called($"{nameof(PostgreSqlFunctions)}.{nameof(StddevSamp)}");
    }

    public static decimal? StddevSamp<TSource>(
        IEnumerable<TSource> rows,
        Func<TSource, decimal?> value
    )
    {
        throw Marker.Called($"{nameof(PostgreSqlFunctions)}.{nameof(StddevSamp)}");
    }

    // VAR_POP(value): the population variance.
    public static double? VarPop<TSource>(
        IEnumerable<TSource> rows,
        Func<TSource, double?> value
    )
    {
        throw Marker.Called($"{nameof(PostgreSqlFunctions)}.{nameof(VarPop)}");
    }

    public static decimal? VarPop<TSource>(
        IEnumerable<TSource> rows,
        Func<TSource, decimal?> value
    )
    {
        throw Marker.Called($"{nameof(PostgreSqlFunctions)}.{nameof(VarPop)}");
    }

    // VAR_SAMP(value): the sample variance, NULL for fewer than two values.
    public static double? VarSamp<TSource>(
        IEnumerable<TSource> rows,
        Func<TSource, double?> value
    )
    {
        throw Marker.Called($"{nameof(PostgreSqlFunctions)}.{nameof(VarSamp)}");
    }

    public static decimal? VarSamp<TSource>(
        IEnumerable<TSource> rows,
        Func<TSource, decimal?> value
    )
    {
        throw Marker.Called($"{nameof(PostgreSqlFunctions)}.{nameof(VarSamp)}");
    }

    // CORR(y, x): the correlation coefficient.
    public static double? Corr<TSource>(
        IEnumerable<TSource> rows,
        Func<TSource, double?> y,
        Func<TSource, double?> x
    )
    {
        throw Marker.Called($"{nameof(PostgreSqlFunctions)}.{nameof(Corr)}");
    }

    // COVAR_POP(y, x): the population covariance.
    public static double? CovarPop<TSource>(
        IEnumerable<TSource> rows,
        Func<TSource, double?> y,
        Func<TSource, double?> x
    )
    {
        throw Marker.Called($"{nameof(PostgreSqlFunctions)}.{nameof(CovarPop)}");
    }

    // COVAR_SAMP(y, x): the sample covariance.
    public static double? CovarSamp<TSource>(
        IEnumerable<TSource> rows,
        Func<TSource, double?> y,
        Func<TSource, double?> x
    )
    {
        throw Marker.Called($"{nameof(PostgreSqlFunctions)}.{nameof(CovarSamp)}");
    }

    // REGR_AVGX(y, x): the mean of x.
    public static double? RegrAvgX<TSource>(
        IEnumerable<TSource> rows,
        Func<TSource, double?> y,
        Func<TSource, double?> x
    )
    {
        throw Marker.Called($"{nameof(PostgreSqlFunctions)}.{nameof(RegrAvgX)}");
    }

    // REGR_AVGY(y, x): the mean of y.
    public static double? RegrAvgY<TSource>(
        IEnumerable<TSource> rows,
        Func<TSource, double?> y,
        Func<TSource, double?> x
    )
    {
        throw Marker.Called($"{nameof(PostgreSqlFunctions)}.{nameof(RegrAvgY)}");
    }

    // REGR_INTERCEPT(y, x): the y-intercept of the least-squares line.
    public static double? RegrIntercept<TSource>(
        IEnumerable<TSource> rows,
        Func<TSource, double?> y,
        Func<TSource, double?> x
    )
    {
        throw Marker.Called($"{nameof(PostgreSqlFunctions)}.{nameof(RegrIntercept)}");
    }

    // REGR_R2(y, x): the square of the correlation coefficient.
    public static double? RegrR2<TSource>(
        IEnumerable<TSource> rows,
        Func<TSource, double?> y,
        Func<TSource, double?> x
    )
    {
        throw Marker.Called($"{nameof(PostgreSqlFunctions)}.{nameof(RegrR2)}");
    }

    // REGR_SLOPE(y, x): the slope of the least-squares line.
    public static double? RegrSlope<TSource>(
        IEnumerable<TSource> rows,
        Func<TSource, double?> y,
        Func<TSource, double?> x
    )
    {
        throw Marker.Called($"{nameof(PostgreSqlFunctions)}.{nameof(RegrSlope)}");
    }

    // REGR_SXX(y, x): the sum of squares of x about its mean.
    public static double? RegrSxx<TSource>(
        IEnumerable<TSource> rows,
        Func<TSource, double?> y,
        Func<TSource, double?> x
    )
    {
        throw Marker.Called($"{nameof(PostgreSqlFunctions)}.{nameof(RegrSxx)}");
    }

    // REGR_SXY(y, x): the sum of products of x and y about their means.
    public static double? RegrSxy<TSource>(
        IEnumerable<TSource> rows,
        Func<TSource, double?> y,
        Func<TSource, double?> x
    )
    {
        throw Marker.Called($"{nameof(PostgreSqlFunctions)}.{nameof(RegrSxy)}");
    }

    // REGR_SYY(y, x): the sum of squares of y about its mean.
    public static double? RegrSyy<TSource>(
        IEnumerable<TSource> rows,
        Func<TSource, double?> y,
        Func<TSource, double?> x
    )
    {
        throw Marker.Called($"{nameof(PostgreSqlFunctions)}.{nameof(RegrSyy)}");
    }

    // REGR_COUNT(y, x): the number of rows where neither is NULL.
    public static long RegrCount<TSource>(
        IEnumerable<TSource> rows,
        Func<TSource, double?> y,
        Func<TSource, double?> x
    )
    {
        throw Marker.Called($"{nameof(PostgreSqlFunctions)}.{nameof(RegrCount)}");
    }
}
