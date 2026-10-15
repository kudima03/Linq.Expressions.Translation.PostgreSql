using System.Linq.Expressions;
using Linq.Expressions.Translation.PostgreSql.Syntax;

namespace Linq.Expressions.Translation.PostgreSql.Shapes;

internal sealed class GroupJoinShape(
    Expression inner,
    LambdaExpression innerKey,
    Shape outerKey
) : Shape
{
    public Expression Inner { get; } = inner;

    public LambdaExpression InnerKey { get; } = innerKey;

    public Shape OuterKey { get; } = outerKey;

    public override void Collect(List<ProjectionColumn> columns, string? path)
    {
        throw Rejected();
    }

    public override Shape Map(
        Func<SqlExpression, string?, SqlExpression> map,
        string? path
    )
    {
        throw Rejected();
    }

    private static NotSupportedException Rejected()
    {
        return new NotSupportedException(
            "The group of a GroupJoin can only be aggregated or flattened with SelectMany."
        );
    }
}
