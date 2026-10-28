using System.Linq.Expressions;
using System.Reflection;
using Linq.Expressions.Translation.PostgreSql.Mapping;

namespace Linq.Expressions.Translation.PostgreSql.Translation;

internal sealed class Evaluator(HashSet<Expression> candidates) : ExpressionVisitor
{
    private static readonly HashSet<string> Clocks =
    [
        nameof(DateTime.Now),
        nameof(DateTime.UtcNow),
        nameof(DateTime.Today),
    ];

    // Set while visiting an operand that is evaluated only when it is reached: a
    // branch of a conditional or the right operand of ??, && and ||.
    private bool _lazy;

    public static Expression Evaluate(Expression expression)
    {
        Nominator nominator = new();
        _ = nominator.Visit(expression);
        return new Evaluator(nominator.Candidates).Visit(expression)!;
    }

    public override Expression? Visit(Expression? node)
    {
        return candidates.Contains(node!) ? Evaluated(node!) : base.Visit(node);
    }

    protected override Expression VisitConditional(ConditionalExpression node)
    {
        return node.Update(Visit(node.Test)!, Lazily(node.IfTrue), Lazily(node.IfFalse));
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        return
            node.NodeType
                is ExpressionType.Coalesce
                    or ExpressionType.AndAlso
                    or ExpressionType.OrElse
            ? node.Update(Visit(node.Left)!, node.Conversion, Lazily(node.Right))
            : base.VisitBinary(node);
    }

    private Expression Lazily(Expression node)
    {
        bool lazy = _lazy;
        _lazy = true;
        Expression visited = Visit(node)!;
        _lazy = lazy;
        return visited;
    }

    protected override Expression VisitMemberInit(MemberInitExpression node)
    {
        return node.Update(
            node.NewExpression.Update(Visit(node.NewExpression.Arguments)),
            node.Bindings.Select(VisitMemberBinding)
        );
    }

    protected override Expression VisitListInit(ListInitExpression node)
    {
        return node.Update(
            node.NewExpression.Update(Visit(node.NewExpression.Arguments)),
            node.Initializers.Select(VisitElementInit)
        );
    }

    // An operand that fails where it may not be reached is left to the database, with
    // the constants below it folded; a nested operation that fails is left the same
    // way as the children are visited.
    private Expression Evaluated(Expression node)
    {
        object? value;
        try
        {
            value = Value(node);
        }
        catch (Exception) when (_lazy)
        {
            return Deferred(base.Visit(node));
        }
        return
            value is IQueryable query
            && !(query.Expression is ConstantExpression root && root.Value == value)
            ? Evaluate(query.Expression)
            : Expression.Constant(value, node.Type);
    }

    // The planner folds an operation on constants alone, and fails the statement
    // before any row reaches it: the first operand is deferred, unless one of them
    // is a failed operation itself, deferred already.
    private static Expression Deferred(Expression operation)
    {
        List<Expression> operands = [];
        _ = new Operands(operand =>
        {
            operands.Add(operand);
            return operand;
        }).Visit(operation);
        if (operands.Any(operand => operand is not ConstantExpression))
        {
            return operation;
        }
        bool deferred = false;
        return new Operands(operand =>
        {
            if (deferred)
            {
                return operand;
            }
            deferred = true;
            return new DeferredConstant((ConstantExpression)operand);
        }).Visit(operation)!;
    }

    private static object? Value(Expression node)
    {
        return node switch
        {
            ConstantExpression constant => constant.Value,
            MemberExpression
            {
                Expression: ConstantExpression { Value: object target },
                Member: FieldInfo field,
            } => field.GetValue(target),
            _ => Expression
                .Lambda<Func<object?>>(Expression.Convert(node, typeof(object)))
                .Compile()(),
        };
    }

    private static bool CanEvaluate(Expression node)
    {
        return node
                is not (
                    ParameterExpression
                    or LambdaExpression
                    or MemberInitExpression
                    or NewExpression { Members: not null }
                )
            && !node.Type.IsByRefLike
            && !IsQueryOperator(node)
            && !IsClock(node);
    }

    private static bool IsClock(Expression node)
    {
        return node switch
        {
            MemberExpression { Expression: null } member => (
                member.Member.DeclaringType == typeof(DateTime)
                || member.Member.DeclaringType == typeof(DateTimeOffset)
            ) && Clocks.Contains(member.Member.Name),
            MethodCallExpression call => call.Method.DeclaringType == typeof(Guid)
                && call.Method.Name == nameof(Guid.NewGuid),
            _ => false,
        };
    }

    private static bool IsQueryOperator(Expression node)
    {
        return node is MethodCallExpression call
            && call.Method.DeclaringType == typeof(Queryable)
            && call.Method.Name != nameof(Queryable.AsQueryable);
    }

    // Visits the operands of an operation, not what is below them.
    private sealed class Operands(Func<Expression, Expression> visit) : ExpressionVisitor
    {
        private bool _below;

        public override Expression? Visit(Expression? node)
        {
            if (!_below)
            {
                _below = true;
                return base.Visit(node);
            }
            return node is null ? null : visit(node);
        }
    }

    private sealed class Nominator : ExpressionVisitor
    {
        private bool _blocked;

        public HashSet<Expression> Candidates { get; } = [];

        // A constructed object that is projected keeps its constructor and has its
        // arguments evaluated: folded, it would be a constant with no SQL literal.
        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            Projected(node.Body);
            return node;
        }

        // An argument of a constructed object is projected with it.
        protected override Expression VisitNew(NewExpression node)
        {
            foreach (Expression argument in node.Arguments)
            {
                Projected(argument);
            }
            return node;
        }

        protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
        {
            Projected(node.Expression);
            return node;
        }

        private void Projected(Expression expression)
        {
            if (
                expression is NewExpression { Members: null } creation
                && !ScalarTypes.IsScalar(creation.Type)
            )
            {
                foreach (Expression argument in creation.Arguments)
                {
                    Projected(argument);
                }
                return;
            }
            _ = Visit(expression);
        }

        public override Expression? Visit(Expression? node)
        {
            if (node is null)
            {
                return null;
            }
            bool blocked = _blocked;
            _blocked = false;
            _ = base.Visit(node);
            if (!_blocked)
            {
                if (CanEvaluate(node))
                {
                    _ = Candidates.Add(node);
                }
                else
                {
                    _blocked = true;
                }
            }
            _blocked |= blocked;
            return node;
        }
    }
}
