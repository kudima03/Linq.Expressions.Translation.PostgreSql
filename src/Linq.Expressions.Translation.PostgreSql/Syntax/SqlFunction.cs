namespace Linq.Expressions.Translation.PostgreSql.Syntax;

internal sealed class SqlFunction(
    string name,
    IReadOnlyList<SqlExpression> arguments,
    Type type,
    bool nullable
) : SqlExpression(type)
{
    public override IEnumerable<SqlExpression> Operands => arguments;

    public override bool Nullable => nullable;

    // One of the arguments, or the day of a DateTime, is of the kind it was.
    public override SqlKind TimeKind =>
        Unifying.Contains(name) ? KindOf(arguments)
        : name == "DATE_TRUNC" ? KindOf([arguments[^1]])
        : SqlKind.Unspecified;

    public SqlFunction(string name, IReadOnlyList<SqlExpression> arguments, Type type)
        : this(name, arguments, type, System.Nullable.GetUnderlyingType(type) is not null)
    { }

    // The functions whose arguments PostgreSQL resolves to one type.
    private static readonly HashSet<string> Unifying = ["COALESCE", "GREATEST", "LEAST"];

    public override void Render(SqlWriter writer, SqlMode mode)
    {
        bool numeric = SqlWriter.IsNumeric(arguments);
        _ = writer
            .Append(name + "(")
            .Join(arguments, argument => writer.Unified(argument, numeric))
            .Append(")");
    }
}
