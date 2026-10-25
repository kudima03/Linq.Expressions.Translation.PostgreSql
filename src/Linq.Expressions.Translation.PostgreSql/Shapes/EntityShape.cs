using System.Reflection;
using Linq.Expressions.Translation.PostgreSql.Syntax;
using Linq.Expressions.Translation.PostgreSql.Translation;

namespace Linq.Expressions.Translation.PostgreSql.Shapes;

internal sealed class EntityShape(Type type, IReadOnlyList<EntityColumn> columns) : Shape
{
    public Type Type { get; } = type;

    public override void Collect(List<ProjectionColumn> projection, string? path)
    {
        projection.AddRange(
            columns.Select(column => new ProjectionColumn(
                Path(path, column.Name),
                column.Value
            ))
        );
    }

    public override Shape Map(
        Func<SqlExpression, string?, SqlExpression> map,
        string? path
    )
    {
        return new EntityShape(
            Type,
            [
                .. columns.Select(column => new EntityColumn(
                    column.Member,
                    column.Name,
                    map(column.Value, Path(path, column.Name))
                )),
            ]
        );
    }

    public ScalarShape Member(MemberInfo member)
    {
        foreach (EntityColumn column in columns)
        {
            if (column.Member == member.Name)
            {
                return new ScalarShape(column.Value);
            }
        }
        throw Unsupported.Member(member);
    }
}
