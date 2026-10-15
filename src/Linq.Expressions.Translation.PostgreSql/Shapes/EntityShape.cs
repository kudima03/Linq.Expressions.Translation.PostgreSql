using System.Reflection;
using Linq.Expressions.Translation.PostgreSql.Syntax;

namespace Linq.Expressions.Translation.PostgreSql.Shapes;

internal sealed class EntityShape(IReadOnlyList<EntityColumn> columns) : Shape
{
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
        return new EntityShape([
            .. columns.Select(column => new EntityColumn(
                column.Member,
                column.Name,
                map(column.Value, Path(path, column.Name))
            )),
        ]);
    }

    public ScalarShape Member(MemberInfo member)
    {
        return new ScalarShape(
            columns.ToDictionary(column => column.Member, column => column.Value)[
                member.Name
            ]
        );
    }
}
