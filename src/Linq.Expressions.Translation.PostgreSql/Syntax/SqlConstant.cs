namespace Linq.Expressions.Translation.PostgreSql.Syntax;

internal sealed class SqlConstant(object? value, Type type) : SqlExpression(type)
{
    public override bool Nullable => Value is null;

    public object? Value { get; } = value;

    // A literal in ORDER BY or GROUP BY is recognized by its node, so it stays one.
    public override SqlExpression AsNullable()
    {
        return this;
    }

    public override void Render(SqlWriter writer, SqlMode mode)
    {
        _ = writer.Append(SqlLiteral.Render(Value));
    }
}
