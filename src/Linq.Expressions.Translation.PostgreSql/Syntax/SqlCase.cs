namespace Linq.Expressions.Translation.PostgreSql.Syntax;

internal sealed class SqlCase(
    IReadOnlyList<SqlWhen> whens,
    SqlExpression otherwise,
    Type type
) : SqlExpression(type)
{
    public override bool Nullable => System.Nullable.GetUnderlyingType(Type) is not null;

    public IReadOnlyList<SqlWhen> Whens { get; } = whens;

    public SqlExpression Otherwise { get; } = otherwise;

    public override void Render(SqlWriter writer, SqlMode mode)
    {
        _ = writer.Append("CASE");
        foreach (SqlWhen branch in Whens)
        {
            _ = writer
                .Append(" WHEN ")
                .Render(branch.Condition, SqlMode.Predicate)
                .Append(" THEN ")
                .Render(branch.Result, SqlMode.Value);
        }
        _ = writer.Append(" ELSE ").Render(Otherwise, SqlMode.Value).Append(" END");
    }
}
