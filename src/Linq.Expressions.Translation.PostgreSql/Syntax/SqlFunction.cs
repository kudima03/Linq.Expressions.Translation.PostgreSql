namespace Linq.Expressions.Translation.PostgreSql.Syntax;

internal sealed class SqlFunction(
    string name,
    IReadOnlyList<SqlExpression> arguments,
    Type type,
    bool nullable
) : SqlExpression(type)
{
    public override bool Nullable => nullable;

    public SqlFunction(string name, IReadOnlyList<SqlExpression> arguments, Type type)
        : this(name, arguments, type, System.Nullable.GetUnderlyingType(type) is not null)
    { }

    public override void Render(SqlWriter writer, SqlMode mode)
    {
        _ = writer
            .Append(name + "(")
            .Join(arguments, argument => writer.Render(argument, SqlMode.Value))
            .Append(")");
    }
}
