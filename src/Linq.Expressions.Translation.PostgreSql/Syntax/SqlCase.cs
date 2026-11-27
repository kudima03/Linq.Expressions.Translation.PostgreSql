namespace Linq.Expressions.Translation.PostgreSql.Syntax;

internal sealed class SqlCase(
    IReadOnlyList<SqlWhen> whens,
    SqlExpression otherwise,
    Type type
) : SqlExpression(type)
{
    // The value is one of the branches, so it is nullable when one of them is.
    public override bool Nullable =>
        Otherwise.Nullable || Whens.Any(branch => branch.Result.Nullable);

    public IReadOnlyList<SqlWhen> Whens { get; } = whens;

    public SqlExpression Otherwise { get; } = otherwise;

    public override void Render(SqlWriter writer, SqlMode mode)
    {
        bool numeric = SqlWriter.IsNumeric([
            .. Whens.Select(branch => branch.Result),
            Otherwise,
        ]);
        _ = writer.Append("CASE");
        foreach (SqlWhen branch in Whens)
        {
            _ = writer
                .Append(" WHEN ")
                .Render(branch.Condition, SqlMode.Predicate)
                .Append(" THEN ")
                .Unified(branch.Result, numeric);
        }
        _ = writer.Append(" ELSE ").Unified(Otherwise, numeric).Append(" END");
    }
}
