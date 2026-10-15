using Linq.Expressions.Translation.PostgreSql.Shapes;

namespace Linq.Expressions.Translation.PostgreSql.Syntax;

internal abstract class SqlQuery(Shape shape)
{
    public Shape Shape { get; set; } = shape;

    public abstract void Render(SqlWriter writer);
}
