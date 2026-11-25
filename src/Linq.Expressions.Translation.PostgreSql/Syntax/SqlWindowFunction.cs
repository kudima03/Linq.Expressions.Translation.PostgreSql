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
    public override bool Nullable => nullable;

    public override void Render(SqlWriter writer, SqlMode mode)
    {
        _ = writer
            .Append(name + "(")
            .Join(arguments, argument => writer.Render(argument, SqlMode.Value))
            .Append(")");
        window.Render(writer);
    }
}
