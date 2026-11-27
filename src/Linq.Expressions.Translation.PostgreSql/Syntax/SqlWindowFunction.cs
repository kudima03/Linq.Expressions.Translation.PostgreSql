namespace Linq.Expressions.Translation.PostgreSql.Syntax;

// A window function other than an aggregate: ROW_NUMBER() OVER (...), LAG(...) OVER
// (...), ...
internal sealed class SqlWindowFunction(
    string name,
    IReadOnlyList<SqlExpression> arguments,
    SqlWindow window,
    Type type,
    bool nullable
) : SqlExpression(type)
{
    public override IEnumerable<SqlExpression> Operands => [.. arguments, .. window.Keys];

    public override bool IsWindow => true;

    public override bool Nullable => nullable;

    // Whether the function yields a value of one of its rows, not a number of its
    // own as a rank or a distribution is.
    public bool IsRead => arguments.Count > 0 && name != "NTILE";

    // LAG, LEAD, FIRST_VALUE and LAST_VALUE yield their value or their default.
    public override SqlKind TimeKind =>
        KindOf(arguments.Where((argument, index) => index != 1));

    public override void Render(SqlWriter writer, SqlMode mode)
    {
        _ = writer
            .Append(name + "(")
            .Join(arguments, argument => writer.Render(argument, SqlMode.Value))
            .Append(")");
        window.Render(writer);
    }
}
