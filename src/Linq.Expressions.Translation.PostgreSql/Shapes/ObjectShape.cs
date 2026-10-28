using System.Reflection;
using Linq.Expressions.Translation.PostgreSql.Syntax;

namespace Linq.Expressions.Translation.PostgreSql.Shapes;

internal sealed class ObjectShape(
    IReadOnlyList<ObjectMember> members,
    IReadOnlyList<string>? presence = null
) : Shape
{
    public IReadOnlyList<ObjectMember> Members { get; } = members;

    // The paths of the columns that are NULL only where a left join found no row, which
    // is where the object is null; null for an object that is never null.
    public IReadOnlyList<string>? Presence { get; } = presence;

    public ObjectShape Present(IReadOnlyList<string> columns)
    {
        return new ObjectShape(Members, columns);
    }

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
        return new ObjectShape(
            [
                .. Members.Select(member => new ObjectMember(
                    member.Name,
                    member.Shape.Map(map, Path(path, member.Name)),
                    member.Type
                )),
            ],
            Presence
        );
    }

    public Shape Member(MemberInfo member)
    {
        ObjectMember? found =
            Members.FirstOrDefault(item => item.Name == member.Name)
            ?? Members.FirstOrDefault(item => item.Matches(member.Name));
        return found?.Shape
            ?? throw new NotSupportedException(
                $"The member '{member.DeclaringType!.Name}.{member.Name}' is neither initialized nor a constructor argument."
            );
    }
}
