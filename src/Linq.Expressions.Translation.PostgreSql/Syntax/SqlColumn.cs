namespace Linq.Expressions.Translation.PostgreSql.Syntax;

internal sealed class SqlColumn(string alias, string name, Type type, bool nullable)
    : SqlExpression(type)
{
    public override bool Nullable => nullable;

    public string Alias { get; } = alias;

    public string Name { get; } = name;

    public SqlColumn AsNullable()
    {
        return new SqlColumn(Alias, Name, Type, true);
    }

    public override void Render(SqlWriter writer, SqlMode mode)
    {
        _ = writer.Append(Alias + ".").Identifier(Name);
    }
}
