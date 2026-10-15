namespace Linq.Expressions.Translation.PostgreSql.Syntax;

internal sealed class ProjectionColumn(string? name, SqlExpression value)
{
    public string? Name { get; } = name;

    public SqlExpression Value { get; } = value;

    public void Render(SqlWriter writer)
    {
        _ = writer.Render(Value, SqlMode.Value);
        if (Name is not null && !(Value is SqlColumn column && column.Name == Name))
        {
            _ = writer.Append(" AS ").Identifier(Name);
        }
    }
}
