namespace Linq.Expressions.Translation.PostgreSql.Syntax;

internal sealed class SqlColumn(
    string alias,
    string name,
    Type type,
    bool nullable,
    SqlKind kind = SqlKind.Unspecified
) : SqlExpression(type)
{
    public override bool Nullable => nullable;

    public override SqlKind TimeKind => kind;

    public string Alias { get; } = alias;

    public string Name { get; } = name;

    public SqlColumn AsNullable()
    {
        return new SqlColumn(Alias, Name, Type, true, kind);
    }

    public override void Render(SqlWriter writer, SqlMode mode)
    {
        _ = writer.Append(Alias + ".").Identifier(Name);
    }
}
