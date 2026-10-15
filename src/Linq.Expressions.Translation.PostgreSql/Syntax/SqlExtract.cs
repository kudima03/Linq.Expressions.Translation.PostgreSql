namespace Linq.Expressions.Translation.PostgreSql.Syntax;

internal sealed class SqlExtract(string field, SqlExpression operand)
    : SqlExpression(typeof(decimal))
{
    public override void Render(SqlWriter writer, SqlMode mode)
    {
        _ = writer
            .Append("EXTRACT(" + field + " FROM ")
            .Operand(operand, SqlMode.Value)
            .Append(")");
    }
}
