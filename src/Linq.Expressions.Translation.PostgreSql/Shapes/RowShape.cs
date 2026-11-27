using Linq.Expressions.Translation.PostgreSql.Syntax;

namespace Linq.Expressions.Translation.PostgreSql.Shapes;

// The element of a one-element in-memory source. Its row holds what a selector
// computes from tables and needs no column of its own, so it is a SELECT without
// FROM until the element is read: then the source becomes a VALUES list of its one
// row, whose shape this stands for.
internal sealed class RowShape(Func<Shape> read) : Shape
{
    private Shape? _element;

    // The element as the columns of its VALUES row, which reading it brings about.
    public Shape Element()
    {
        return _element ??= read();
    }

    public override void Collect(List<ProjectionColumn> columns, string? path)
    {
        Element().Collect(columns, path);
    }

    public override Shape Map(
        Func<SqlExpression, string?, SqlExpression> map,
        string? path
    )
    {
        return Element().Map(map, path);
    }
}
