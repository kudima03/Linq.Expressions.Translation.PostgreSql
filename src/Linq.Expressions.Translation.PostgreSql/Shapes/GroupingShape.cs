using Linq.Expressions.Translation.PostgreSql.Syntax;

namespace Linq.Expressions.Translation.PostgreSql.Shapes;

// The elements are null once the groups have left their SELECT: a derived table
// exposes their keys only. An ordered group has the keys its elements are sorted by,
// and a group of an ordered source the keys of that order, in which its elements
// come and which breaks the ties of its own.
internal sealed class GroupingShape(
    Shape key,
    Shape? element,
    SqlExpression? filter,
    bool distinct,
    IReadOnlyList<Ordering>? order = null,
    IReadOnlyList<Ordering>? source = null
) : Shape
{
    public Shape Key { get; } = key;

    public Shape? Element { get; } = element;

    public SqlExpression? Filter { get; } = filter;

    public bool Distinct { get; } = distinct;

    public IReadOnlyList<Ordering>? Order { get; } = order;

    public IReadOnlyList<Ordering> Source { get; } = source ?? [];

    // The keys the elements come in: those they are sorted by, then the source's.
    public IReadOnlyList<Ordering> Sequence => [.. Order ?? [], .. Source];

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
