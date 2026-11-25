using Linq.Expressions.Translation.PostgreSql.Syntax;

namespace Linq.Expressions.Translation.PostgreSql.Shapes;

// The window parameter of the selector of Over: the window its functions are over
// and the element of its rows. It has no SQL value of its own.
internal sealed class WindowShape(
    SqlWindow window,
    Shape element,
    bool includesCurrentRow
) : Shape
{
    public SqlWindow Window { get; } = window;

    public Shape Element { get; } = element;

    // Whether the frame always holds the current row, so that it is never empty.
    public bool IncludesCurrentRow { get; } = includesCurrentRow;

    public static NotSupportedException Rejected()
    {
        return new NotSupportedException(
            "A window or the rows of its frame cannot be projected; select the window functions or aggregates over them."
        );
    }

    public override void Collect(List<ProjectionColumn> columns, string? path)
    {
        throw Rejected();
    }

    public override Shape Map(
        Func<SqlExpression, string?, SqlExpression> map,
        string? path
    )
    {
        throw Rejected();
    }
}
