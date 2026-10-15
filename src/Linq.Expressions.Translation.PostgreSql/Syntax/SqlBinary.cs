namespace Linq.Expressions.Translation.PostgreSql.Syntax;

internal sealed class SqlBinary(
    string symbol,
    SqlBinaryKind kind,
    SqlExpression left,
    SqlExpression right,
    Type type,
    bool nullable
) : SqlExpression(type, nullable)
{
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
        return mode == SqlMode.Value && Kind == SqlBinaryKind.Comparison && Nullable;
    }

    private void RenderOperation(SqlWriter writer, SqlMode mode)
    {
        RenderOperand(writer, Left, mode);
        _ = writer.Append(" " + Symbol + " ");
        RenderOperand(writer, Right, mode);
    }

    private void RenderOperand(SqlWriter writer, SqlExpression operand, SqlMode mode)
    {
        _ =
            Kind is SqlBinaryKind.Logical or SqlBinaryKind.Concatenation
            && operand is SqlBinary { Symbol: string other }
            && other == Symbol
                ? writer.Render(operand, mode)
                : writer.Operand(operand, mode);
    }
}
