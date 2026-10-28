using System.Linq.Expressions;

namespace Linq.Expressions.Translation.PostgreSql.Translation;

// A constant operand of an operation that failed when it was evaluated: left to the
// database, the operation has to be hidden from the planner, which folds an
// operation on constants alone and fails the statement before any row reaches it.
internal sealed class DeferredConstant(ConstantExpression constant) : Expression
{
    public ConstantExpression Operand { get; } = constant;

    public override ExpressionType NodeType => ExpressionType.Extension;

    public override Type Type => Operand.Type;

    protected override Expression VisitChildren(ExpressionVisitor visitor)
    {
        return this;
    }
}
