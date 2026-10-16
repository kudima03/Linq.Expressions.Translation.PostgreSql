using Linq.Expressions.Translation.PostgreSql.Syntax;

namespace Linq.Expressions.Translation.PostgreSql.Shapes;

internal sealed class ScalarShape(SqlExpression value, string? name = null) : Shape
{
    public SqlExpression Value { get; } = value;

    public ScalarShape Named()
    {
        return Leaf(null) is null ? new ScalarShape(Value, "Value") : this;
    }

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
        return path ?? name ?? (Value as SqlColumn)?.Name;
    }
}
