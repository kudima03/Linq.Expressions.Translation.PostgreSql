namespace Linq.Expressions.Translation.PostgreSql.Shapes;

internal sealed class ObjectMember(string name, Shape shape, Type type)
{
    public string Name { get; } = name;

    public Shape Shape { get; } = shape;

    // The type of the member as the element declares it, which may be nullable where
    // the value is not.
    public Type Type { get; } = type;

    // Constructor parameters are matched to members ignoring case (item1 sets Item1).
    public bool Matches(string member)
    {
        return string.Equals(Name, member, StringComparison.OrdinalIgnoreCase);
    }
}
