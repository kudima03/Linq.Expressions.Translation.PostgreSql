namespace Linq.Expressions.Translation.PostgreSql.Syntax;

internal sealed class SqlBinary(
    string symbol,
    SqlBinaryKind kind,
    SqlExpression left,
    SqlExpression right,
    Type type,
    bool nullable
) : SqlExpression(type)
{
    // PostgreSQL binds these tighter than a prefix ~, so ~a + 1 means ~(a + 1).
    private static readonly HashSet<string> Arithmetic = ["+", "-", "*", "/", "%"];

    // A lifted comparison is a bool in C#: NULL in SQL, it is FALSE as a value. A
    // lifted ^ of booleans is a bool? and stays NULL.
    public override bool Nullable =>
        nullable && (Kind != SqlBinaryKind.Comparison || IsLifted);

    private bool IsLifted => System.Nullable.GetUnderlyingType(Type) is not null;

    public string Symbol { get; } = symbol;

    public SqlBinaryKind Kind { get; } = kind;

    public SqlExpression Left { get; } = left;

    public SqlExpression Right { get; } = right;

    public override bool IsCompound(SqlMode mode)
    {
        return !IsCoalesced(mode);
    }

    public override void Render(SqlWriter writer, SqlMode mode)
    {
        if (IsCoalesced(mode))
        {
            _ = writer.Append("COALESCE(");
            RenderOperation(writer, SqlMode.Predicate);
            _ = writer.Append(", FALSE)");
            return;
        }
        RenderOperation(writer, mode);
    }

    private bool IsCoalesced(SqlMode mode)
    {
        return mode == SqlMode.Value
            && Kind == SqlBinaryKind.Comparison
            && nullable
            && !IsLifted;
    }

    private void RenderOperation(SqlWriter writer, SqlMode mode)
    {
        _ =
            Arithmetic.Contains(Symbol) && Left is SqlUnary { Symbol: "~" } complement
                ? writer.Append("(").Render(complement, mode).Append(")")
                : RenderOperand(writer, Left, mode);
        _ = writer.Append(" " + Symbol + " ");
        _ = RenderOperand(writer, Right, mode);
    }

    // Only logical operators pass a predicate position on; every other operand is a value.
    private SqlWriter RenderOperand(SqlWriter writer, SqlExpression operand, SqlMode mode)
    {
        SqlMode inner = Kind == SqlBinaryKind.Logical ? mode : SqlMode.Value;
        return
            IsChained(operand) || (Kind == SqlBinaryKind.Logical && operand is SqlUnary)
            ? writer.Render(operand, inner)
            : writer.Operand(operand, inner);
    }

    private bool IsChained(SqlExpression operand)
    {
        return Kind is SqlBinaryKind.Logical or SqlBinaryKind.Concatenation
            && operand is SqlBinary { Symbol: string other }
            && other == Symbol;
    }
}
