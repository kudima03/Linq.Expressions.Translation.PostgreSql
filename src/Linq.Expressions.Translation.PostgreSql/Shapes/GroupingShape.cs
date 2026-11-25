using Linq.Expressions.Translation.PostgreSql.Syntax;

namespace Linq.Expressions.Translation.PostgreSql.Shapes;

// The elements are null once the groups have left their SELECT: a derived table
// exposes their keys only. An ordered group has the keys its elements are sorted by.
internal sealed class GroupingShape(
    Shape key,
    Shape? element,
    SqlExpression? filter,
    bool distinct,
    IReadOnlyList<Ordering>? order = null
) : Shape
{
    public Shape Key { get; } = key;

    public Shape? Element { get; } = element;

    public SqlExpression? Filter { get; } = filter;

    public bool Distinct { get; } = distinct;

    public IReadOnlyList<Ordering>? Order { get; } = order;

    public override void Collect(List<ProjectionColumn> columns, string? path)
    {
        Key.Collect(columns, path);
    }

    public override Shape Map(
        Func<SqlExpression, string?, SqlExpression> map,
        string? path
    )
    {
        return new GroupingShape(Key.Map(map, path), null, Filter, Distinct);
    }
}
