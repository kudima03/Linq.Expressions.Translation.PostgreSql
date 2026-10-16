namespace Linq.Expressions.Translation.PostgreSql.Shapes;

internal sealed class ObjectMember(string name, Shape shape)
{
    public string Name { get; } = name;

    public Shape Shape { get; } = shape;

    // Constructor parameters are matched to members ignoring case (item1 sets Item1).
    public bool Matches(string member)
    {
        return string.Equals(Name, member, StringComparison.OrdinalIgnoreCase);
    }
}
