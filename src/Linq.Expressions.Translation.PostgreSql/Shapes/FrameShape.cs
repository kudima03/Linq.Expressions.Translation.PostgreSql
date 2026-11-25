using Linq.Expressions.Translation.PostgreSql.Syntax;

namespace Linq.Expressions.Translation.PostgreSql.Shapes;

// The rows of a window's frame (PostgreSqlWindow<TSource>.Rows), after the Where
// (FILTER) and Select of the aggregate that reads them.
internal sealed class FrameShape(WindowShape window, Shape element, SqlExpression? filter)
    : Shape
{
    public WindowShape Window { get; } = window;

    public Shape Element { get; } = element;

    public SqlExpression? Filter { get; } = filter;

    public override void Collect(List<ProjectionColumn> columns, string? path)
    {
        throw WindowShape.Rejected();
    }

    public override Shape Map(
        Func<SqlExpression, string?, SqlExpression> map,
        string? path
    )
    {
        throw WindowShape.Rejected();
    }
}
