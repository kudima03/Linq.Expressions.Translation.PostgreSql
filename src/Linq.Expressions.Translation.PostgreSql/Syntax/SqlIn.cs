namespace Linq.Expressions.Translation.PostgreSql.Syntax;

internal sealed class SqlIn(
    SqlExpression operand,
    IReadOnlyList<SqlExpression> values,
    bool negated
) : SqlMembership(operand.Nullable)
{
    public override IEnumerable<SqlExpression> Operands => [operand, .. values];

    protected override void RenderMembership(SqlWriter writer)
    {
        _ = writer
            .Operand(operand, SqlMode.Value)
            .Append(negated ? " NOT IN (" : " IN (")
            .Join(values, value => writer.Unified(value, SqlWriter.IsNumeric([operand])))
            .Append(")");
    }
}
