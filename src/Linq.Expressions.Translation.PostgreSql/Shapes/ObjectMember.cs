namespace Linq.Expressions.Translation.PostgreSql.Shapes;

internal sealed class ObjectMember(string name, Shape shape)
{
    public string Name { get; } = name;

    public Shape Shape { get; } = shape;
}
