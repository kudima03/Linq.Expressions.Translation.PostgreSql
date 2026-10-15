namespace Linq.Expressions.Translation.PostgreSql.Syntax;

internal sealed class SetOperationQuery(string operation, SqlQuery left, SqlQuery right)
    : SqlQuery(left.Shape)
{
    public string Operation { get; } = operation;

    public override void Render(SqlWriter writer)
    {
        Operand(
            writer,
            left,
            left is SetOperationQuery nested && nested.Operation != Operation
        );
        _ = writer.Append(" " + Operation + " ");
        Operand(writer, right, right is SetOperationQuery);
    }

    private static void Operand(SqlWriter writer, SqlQuery query, bool nested)
    {
        bool parenthesized = nested || query is SelectQuery { IsPaged: true };
        _ = writer.Append(parenthesized ? "(" : string.Empty);
        query.Render(writer);
        _ = writer.Append(parenthesized ? ")" : string.Empty);
    }
}
