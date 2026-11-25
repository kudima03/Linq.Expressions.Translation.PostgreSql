namespace Linq.Expressions.Translation.PostgreSql.Syntax;

// The arguments of an aggregate of several values, CORR(y, x) for example.
internal sealed class SqlArguments(IReadOnlyList<SqlExpression> values)
    : SqlExpression(typeof(object))
{
    public override void Render(SqlWriter writer, SqlMode mode)
    {
        _ = writer.Join(values, value => writer.Render(value, SqlMode.Value));
    }
}
