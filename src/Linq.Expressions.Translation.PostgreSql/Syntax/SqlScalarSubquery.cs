namespace Linq.Expressions.Translation.PostgreSql.Syntax;

internal sealed class SqlScalarSubquery(SelectQuery query, Type type)
    : SqlExpression(type)
{
    public SelectQuery Query { get; } = query;

    // Without a row, the subquery yields NULL: the default of a reference type.
    public override bool Nullable =>
        !Type.IsValueType || System.Nullable.GetUnderlyingType(Type) is not null;

    public override void Render(SqlWriter writer, SqlMode mode)
    {
        _ = writer.Append("(");
        Query.Render(writer);
        _ = writer.Append(")");
    }
}
