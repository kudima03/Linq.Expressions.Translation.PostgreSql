using Linq.Expressions.Translation.PostgreSql.Syntax;

namespace Linq.Expressions.Translation.PostgreSql.Shapes;

// The element of a one-element in-memory source: its row holds what the selector
// computes from tables and has no column of its own, so the element cannot be read.
internal sealed class RowShape(string source) : Shape
{
    public NotSupportedException Rejected()
    {
        return new NotSupportedException(
            $"The element of the in-memory source '{source}' cannot be translated into SQL; select aggregates over tables from it."
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
