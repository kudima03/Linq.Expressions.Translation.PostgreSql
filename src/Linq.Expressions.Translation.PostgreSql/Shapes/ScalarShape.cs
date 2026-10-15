using Linq.Expressions.Translation.PostgreSql.Syntax;

namespace Linq.Expressions.Translation.PostgreSql.Shapes;

internal sealed class ScalarShape(SqlExpression value) : Shape
{
    public SqlExpression Value { get; } = value;

    public override void Collect(List<ProjectionColumn> columns, string? path)
    {
        columns.Add(new ProjectionColumn(Leaf(path), Value));
    }

    public override Shape Map(
        Func<SqlExpression, string?, SqlExpression> map,
        string? path
    )
    {
        return new ScalarShape(map(Value, Leaf(path)));
    }

    private string? Leaf(string? path)
    {
        return path ?? (Value as SqlColumn)?.Name;
    }
}
