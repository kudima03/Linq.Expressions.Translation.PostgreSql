using System.Linq.Expressions;
using Linq.Expressions.Translation.PostgreSql.Shapes;

namespace Linq.Expressions.Translation.PostgreSql.Translation;

internal sealed class Binding(Shape shape, LambdaExpression scope)
{
    public Shape Shape { get; } = shape;

    public LambdaExpression Scope { get; } = scope;

    public int Uses { get; set; }
}
