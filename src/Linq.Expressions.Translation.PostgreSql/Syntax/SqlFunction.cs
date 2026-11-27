namespace Linq.Expressions.Translation.PostgreSql.Syntax;

internal sealed class SqlFunction(
    string name,
    IReadOnlyList<SqlExpression> arguments,
    Type type,
    bool nullable
) : SqlExpression(type)
{
    public override bool Nullable => nullable;

    public SqlFunction(string name, IReadOnlyList<SqlExpression> arguments, Type type)
        : this(name, arguments, type, System.Nullable.GetUnderlyingType(type) is not null)
    { }

    // The functions whose arguments PostgreSQL resolves to one type.
    private static readonly HashSet<string> Unifying = ["COALESCE", "GREATEST", "LEAST"];

    public override void Render(SqlWriter writer, SqlMode mode)
    {
        bool numeric = Unifying.Contains(name) && SqlWriter.IsNumeric(arguments);
        _ = writer
            .Append(name + "(")
            .Join(arguments, argument => writer.Unified(argument, numeric))
            .Append(")");
    }
}
