namespace Linq.Expressions.Translation.PostgreSql.Syntax;

internal sealed class SqlAggregate(
    string name,
    SqlExpression? argument,
    bool distinct,
    SqlExpression? filter,
    Type type
) : SqlExpression(type)
{
    public override bool Nullable => System.Nullable.GetUnderlyingType(Type) is not null;

    public override void Render(SqlWriter writer, SqlMode mode)
    {
        _ = writer.Append(name + "(" + (distinct ? "DISTINCT " : string.Empty));
        _ = argument is null
            ? writer.Append("*")
            : writer.Render(argument, SqlMode.Value);
        _ = writer.Append(")");
        if (filter is not null)
        {
            _ = writer
                .Append(" FILTER (WHERE ")
                .Render(filter, SqlMode.Predicate)
                .Append(")");
        }
    }
}
