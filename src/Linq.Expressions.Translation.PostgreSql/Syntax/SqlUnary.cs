namespace Linq.Expressions.Translation.PostgreSql.Syntax;

internal sealed class SqlUnary(string symbol, SqlExpression operand, Type type)
    : SqlExpression(type)
{
    public override bool Nullable => operand.Nullable;

    public string Symbol { get; } = symbol;

    public bool IsNegation => Symbol == "NOT ";

    public override bool IsCompound(SqlMode mode)
    {
        return IsNegation;
    }

    // Adjacent prefix operators lex as one token: -- starts a comment, ~- is no operator.
    public override void Render(SqlWriter writer, SqlMode mode)
    {
        _ = writer.Append(Symbol);
        _ =
            operand is SqlUnary && !IsNegation
                ? writer.Append("(").Render(operand, SqlMode.Value).Append(")")
            : operand is SqlUnary ? writer.Render(operand, SqlMode.Value)
            : writer.Operand(operand, SqlMode.Value);
    }
}
