namespace Linq.Expressions.Translation.PostgreSql.ExecutionTests.Infrastructure;

// PostgreSQL's statistical aggregates in memory, by their definitions: NULL values
// (or rows with a NULL in either column) are skipped, and the result is NULL where
// PostgreSQL's is.
public static class ReferenceStatistics
{
    public static double? StddevPop<T>(IEnumerable<T> rows, Func<T, double?> value)
    {
        return Single([.. rows.Select(value).OfType<double>()], nameof(StddevPop));
    }

    public static decimal? StddevPop<T>(IEnumerable<T> rows, Func<T, decimal?> value)
    {
        return Single([.. rows.Select(value).OfType<decimal>()], nameof(StddevPop));
    }

    public static double? StddevSamp<T>(IEnumerable<T> rows, Func<T, double?> value)
    {
        return Single([.. rows.Select(value).OfType<double>()], nameof(StddevSamp));
    }

    public static decimal? StddevSamp<T>(IEnumerable<T> rows, Func<T, decimal?> value)
    {
        return Single([.. rows.Select(value).OfType<decimal>()], nameof(StddevSamp));
    }

    public static double? VarPop<T>(IEnumerable<T> rows, Func<T, double?> value)
    {
        return Single([.. rows.Select(value).OfType<double>()], nameof(VarPop));
    }

    public static decimal? VarPop<T>(IEnumerable<T> rows, Func<T, decimal?> value)
    {
        return Single([.. rows.Select(value).OfType<decimal>()], nameof(VarPop));
    }

    public static double? VarSamp<T>(IEnumerable<T> rows, Func<T, double?> value)
    {
        return Single([.. rows.Select(value).OfType<double>()], nameof(VarSamp));
    }

    public static decimal? VarSamp<T>(IEnumerable<T> rows, Func<T, decimal?> value)
    {
        return Single([.. rows.Select(value).OfType<decimal>()], nameof(VarSamp));
    }

    public static double? Corr<T>(
        IEnumerable<T> rows,
        Func<T, double?> y,
        Func<T, double?> x
    )
    {
        return Pairs(rows, y, x, nameof(Corr));
    }

    public static double? CovarPop<T>(
        IEnumerable<T> rows,
        Func<T, double?> y,
        Func<T, double?> x
    )
    {
        return Pairs(rows, y, x, nameof(CovarPop));
    }

    public static double? CovarSamp<T>(
        IEnumerable<T> rows,
        Func<T, double?> y,
        Func<T, double?> x
    )
    {
        return Pairs(rows, y, x, nameof(CovarSamp));
    }

    public static double? RegrAvgX<T>(
        IEnumerable<T> rows,
        Func<T, double?> y,
        Func<T, double?> x
    )
    {
        return Pairs(rows, y, x, nameof(RegrAvgX));
    }

    public static double? RegrAvgY<T>(
        IEnumerable<T> rows,
        Func<T, double?> y,
        Func<T, double?> x
    )
    {
        return Pairs(rows, y, x, nameof(RegrAvgY));
    }

    public static double? RegrIntercept<T>(
        IEnumerable<T> rows,
        Func<T, double?> y,
        Func<T, double?> x
    )
    {
        return Pairs(rows, y, x, nameof(RegrIntercept));
    }

    public static double? RegrR2<T>(
        IEnumerable<T> rows,
        Func<T, double?> y,
        Func<T, double?> x
    )
    {
        return Pairs(rows, y, x, nameof(RegrR2));
    }

    public static double? RegrSlope<T>(
        IEnumerable<T> rows,
        Func<T, double?> y,
        Func<T, double?> x
    )
    {
        return Pairs(rows, y, x, nameof(RegrSlope));
    }

    public static double? RegrSxx<T>(
        IEnumerable<T> rows,
        Func<T, double?> y,
        Func<T, double?> x
    )
    {
        return Pairs(rows, y, x, nameof(RegrSxx));
    }

    public static double? RegrSxy<T>(
        IEnumerable<T> rows,
        Func<T, double?> y,
        Func<T, double?> x
    )
    {
        return Pairs(rows, y, x, nameof(RegrSxy));
    }

    public static double? RegrSyy<T>(
        IEnumerable<T> rows,
        Func<T, double?> y,
        Func<T, double?> x
    )
    {
        return Pairs(rows, y, x, nameof(RegrSyy));
    }

    public static long RegrCount<T>(
        IEnumerable<T> rows,
        Func<T, double?> y,
        Func<T, double?> x
    )
    {
        return rows.Count(row => y(row) is not null && x(row) is not null);
    }

    private static double? Single(List<double> values, string name)
    {
        int n = values.Count;
        if (n == 0 || (name.EndsWith("Samp", StringComparison.Ordinal) && n < 2))
        {
            return null;
        }
        double mean = values.Average();
        double squares = values.Sum(value => (value - mean) * (value - mean));
        double variance =
            squares / (name.EndsWith("Samp", StringComparison.Ordinal) ? n - 1 : n);
        return name.StartsWith("Stddev", StringComparison.Ordinal)
            ? Math.Sqrt(variance)
            : variance;
    }

    private static decimal? Single(List<decimal> values, string name)
    {
        int n = values.Count;
        if (n == 0 || (name.EndsWith("Samp", StringComparison.Ordinal) && n < 2))
        {
            return null;
        }
        decimal sum = values.Sum();
        decimal squares = values.Sum(value => ((value * n) - sum) * ((value * n) - sum));
        decimal variance =
            squares
            / n
            / n
            / (name.EndsWith("Samp", StringComparison.Ordinal) ? n - 1 : n);
        return name.StartsWith("Stddev", StringComparison.Ordinal)
            ? Root(variance)
            : variance;
    }

    // Newton's method in decimal, from the square root in double.
    private static decimal Root(decimal value)
    {
        if (value == 0)
        {
            return 0;
        }
        decimal root = (decimal)Math.Sqrt((double)value);
        for (int step = 0; step < 10; step++)
        {
            root = (root + (value / root)) / 2;
        }
        return root;
    }

    private static double? Pairs<T>(
        IEnumerable<T> rows,
        Func<T, double?> y,
        Func<T, double?> x,
        string name
    )
    {
        List<(double Y, double X)> pairs =
        [
            .. rows.Where(row => y(row) is not null && x(row) is not null)
                .Select(row => (y(row)!.Value, x(row)!.Value)),
        ];
        int n = pairs.Count;
        if (n == 0)
        {
            return null;
        }
        double my = pairs.Average(pair => pair.Y);
        double mx = pairs.Average(pair => pair.X);
        double sxx = pairs.Sum(pair => (pair.X - mx) * (pair.X - mx));
        double syy = pairs.Sum(pair => (pair.Y - my) * (pair.Y - my));
        double sxy = pairs.Sum(pair => (pair.X - mx) * (pair.Y - my));
        return name switch
        {
            nameof(CovarPop) => sxy / n,
            nameof(CovarSamp) => n < 2 ? null : sxy / (n - 1),
            nameof(Corr) => sxx == 0 || syy == 0 ? null : sxy / Math.Sqrt(sxx * syy),
            nameof(RegrAvgX) => mx,
            nameof(RegrAvgY) => my,
            nameof(RegrSxx) => sxx,
            nameof(RegrSyy) => syy,
            nameof(RegrSxy) => sxy,
            nameof(RegrSlope) => sxx == 0 ? null : sxy / sxx,
            nameof(RegrIntercept) => sxx == 0 ? null : my - (sxy / sxx * mx),
            _ => sxx == 0 ? null
            : syy == 0 ? 1
            : sxy * sxy / (sxx * syy),
        };
    }
}
