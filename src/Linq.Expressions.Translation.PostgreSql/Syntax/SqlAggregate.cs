namespace Linq.Expressions.Translation.PostgreSql.Syntax;

internal sealed class SqlAggregate(
    string name,
    SqlExpression? argument,
    bool distinct,
    SqlExpression? filter,
    Type type,
    bool windowed = false
) : SqlExpression(type)
{
    // MIN and MAX of a reference type are NULL when every value is NULL or filtered out.
    public override bool Nullable =>
        System.Nullable.GetUnderlyingType(Type) is not null
        || (!Type.IsValueType && (filter is not null || argument is { Nullable: true }));

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
        if (windowed)
        {
            _ = writer.Append(" OVER ()");
        }
    }
}
