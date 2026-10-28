using Linq.Expressions.Translation.PostgreSql.Shapes;

namespace Linq.Expressions.Translation.PostgreSql.Syntax;

internal sealed class SetOperationQuery(
    string operation,
    SqlQuery left,
    SqlQuery right,
    Shape shape
) : SqlQuery(shape)
{
    public string Operation { get; } = operation;

    public SqlQuery Left { get; } = left;

    public SqlQuery Right { get; } = right;

    // A column of the result is nullable when it is nullable on either side.
    public override IReadOnlyList<bool> Nullability()
    {
        return
        [
            .. Left.Nullability()
                .Zip(Right.Nullability(), (first, second) => first || second),
        ];
    }

    public override void Render(SqlWriter writer)
    {
        Operand(
            writer,
            Left,
            Left is SetOperationQuery nested && nested.Operation != Operation
        );
        _ = writer.Append(" " + Operation + " ");
        Operand(writer, Right, Right is SetOperationQuery);
    }

    private static void Operand(SqlWriter writer, SqlQuery query, bool nested)
    {
        bool parenthesized = nested || query is SelectQuery { KeepsOrder: true };
        _ = writer.Append(parenthesized ? "(" : string.Empty);
        query.Render(writer);
        _ = writer.Append(parenthesized ? ")" : string.Empty);
    }
}
