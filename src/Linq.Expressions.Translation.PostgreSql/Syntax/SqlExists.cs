namespace Linq.Expressions.Translation.PostgreSql.Syntax;

internal sealed class SqlExists(SqlQuery query) : SqlExpression(typeof(bool))
{
    public override void Render(SqlWriter writer, SqlMode mode)
    {
        _ = writer.Append("EXISTS (");
        query.Render(writer);
        _ = writer.Append(")");
    }
}
