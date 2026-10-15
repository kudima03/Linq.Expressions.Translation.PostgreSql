namespace Linq.Expressions.Translation.PostgreSql.Syntax;

internal sealed class SqlWhen(SqlExpression condition, SqlExpression result)
{
    public SqlExpression Condition { get; } = condition;

    public SqlExpression Result { get; } = result;
}
