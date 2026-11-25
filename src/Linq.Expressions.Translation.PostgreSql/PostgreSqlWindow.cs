namespace Linq.Expressions.Translation.PostgreSql;

// The window of the current row, the second parameter of the selector of
// PostgreSqlWindows.Over: the window functions take it, and Rows are the rows of its
// frame in the order of the window, which the aggregates of LINQ (Count, LongCount,
// Sum, Average, Min, Max, Any, All, after Where and Select) read as window
// aggregates. It exists only in expression trees: nothing creates one.
public sealed class PostgreSqlWindow<TSource>
{
    private PostgreSqlWindow() { }

    public IEnumerable<TSource> Rows =>
        throw Marker.Called($"{nameof(PostgreSqlWindow<>)}.{nameof(Rows)}");
}
