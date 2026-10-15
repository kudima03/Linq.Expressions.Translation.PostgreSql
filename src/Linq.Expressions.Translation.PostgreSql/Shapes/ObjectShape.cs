using System.Reflection;
using Linq.Expressions.Translation.PostgreSql.Syntax;

namespace Linq.Expressions.Translation.PostgreSql.Shapes;

internal sealed class ObjectShape(IReadOnlyList<ObjectMember> members) : Shape
{
    public IReadOnlyList<ObjectMember> Members { get; } = members;

    public override void Collect(List<ProjectionColumn> columns, string? path)
    {
        foreach (ObjectMember member in Members)
        {
            member.Shape.Collect(columns, Path(path, member.Name));
        }
    }

    public override Shape Map(
        Func<SqlExpression, string?, SqlExpression> map,
        string? path
    )
    {
        return new ObjectShape([
            .. Members.Select(member => new ObjectMember(
                member.Name,
                member.Shape.Map(map, Path(path, member.Name))
            )),
        ]);
    }

    public Shape Member(MemberInfo member)
    {
        return Members.ToDictionary(item => item.Name, item => item.Shape)[member.Name];
    }
}
