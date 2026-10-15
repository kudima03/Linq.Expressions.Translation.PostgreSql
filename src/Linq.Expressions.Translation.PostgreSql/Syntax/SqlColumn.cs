namespace Linq.Expressions.Translation.PostgreSql.Syntax;

internal sealed class SqlColumn(string alias, string name, Type type, bool nullable)
    : SqlExpression(type)
{
    public override bool Nullable => nullable;

    public string Name { get; } = name;

    public override void Render(SqlWriter writer, SqlMode mode)
    {
        _ = writer.Append(alias + ".").Identifier(Name);
    }
}
