using Linq.Expressions.Translation.PostgreSql.Syntax;

namespace Linq.Expressions.Translation.PostgreSql.Shapes;

internal abstract class Shape
{
    public List<ProjectionColumn> Columns()
    {
        List<ProjectionColumn> columns = [];
        Collect(columns, null);
        return columns;
    }

    public abstract void Collect(List<ProjectionColumn> columns, string? path);

    public abstract Shape Map(
        Func<SqlExpression, string?, SqlExpression> map,
        string? path
    );

    protected static string Path(string? path, string name)
    {
        return path is null ? name : path + "." + name;
    }
}
