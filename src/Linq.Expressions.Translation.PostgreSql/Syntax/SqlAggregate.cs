namespace Linq.Expressions.Translation.PostgreSql.Syntax;

internal sealed class SqlAggregate(
    string name,
    SqlExpression? argument,
    bool distinct,
    SqlExpression? filter,
    Type type,
    bool windowed = false,
    SqlWindow? window = null
) : SqlExpression(type)
{
    public override IEnumerable<SqlExpression> Operands =>
        [.. new[] { argument, filter }.OfType<SqlExpression>(), .. window?.Keys ?? []];

    public override bool IsAggregate => !IsWindow;

    public override bool IsWindow => windowed || window is not null;

    // MIN and MAX of a reference type are NULL when every value is NULL or filtered
    // out, or when the frame they are taken over holds no row.
    public override bool Nullable =>
        System.Nullable.GetUnderlyingType(Type) is not null
        || (
            !Type.IsValueType
            && (
                filter is not null
                || argument is { Nullable: true }
                || window is { IncludesCurrentRow: false }
            )
        );

    // A window aggregate may aggregate the groups of its SELECT, but PostgreSQL
    // allows no aggregate in its FILTER: the filter then chooses the argument, which
    // is NULL, and skipped, for a row it leaves out.
    private bool IsChosen =>
        IsWindow && filter is not null && filter.Any(part => part.IsAggregate);

    // MIN and MAX of DateTime values are one of them.
    public override SqlKind TimeKind =>
        argument is null ? SqlKind.Unspecified : KindOf([argument]);

    public override void Render(SqlWriter writer, SqlMode mode)
    {
        _ = writer.Append(name + "(" + (distinct ? "DISTINCT " : string.Empty));
        if (IsChosen)
        {
            _ = writer
                .Append("CASE WHEN ")
                .Render(filter!, SqlMode.Predicate)
                .Append(" THEN ");
            _ = argument is null
                ? writer.Append("1")
                : writer.Render(argument, SqlMode.Value);
            _ = writer.Append(" END");
        }
        else
        {
            _ = argument is null
                ? writer.Append("*")
                : writer.Render(argument, SqlMode.Value);
        }
        _ = writer.Append(")");
        if (filter is not null && !IsChosen)
        {
            _ = writer
                .Append(" FILTER (WHERE ")
                .Render(filter, SqlMode.Predicate)
                .Append(")");
        }
        if (window is not null)
        {
            window.Render(writer);
        }
        else if (windowed)
        {
            _ = writer.Append(" OVER ()");
        }
    }
}
