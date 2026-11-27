using System.Linq.Expressions;
using Linq.Expressions.Translation.PostgreSql.Shapes;

namespace Linq.Expressions.Translation.PostgreSql.Syntax;

internal abstract class SqlQuery(Shape shape)
{
    public Shape Shape { get; set; } = shape;

    // The expression whose rows the query yields, for an aggregate over the same
    // rows in its projection to be a window over them.
    public Expression? Origin { get; set; }

    // Whether each column of the result may be NULL.
    public virtual IReadOnlyList<bool> Nullability()
    {
        return [.. Shape.Columns().Select(column => column.Value.Nullable)];
    }

    // The kind of the DateTime each column of the result is read as.
    public virtual IReadOnlyList<SqlKind> Kinds()
    {
        return [.. Shape.Columns().Select(column => column.Value.TimeKind)];
    }

    public abstract void Render(SqlWriter writer);
}
