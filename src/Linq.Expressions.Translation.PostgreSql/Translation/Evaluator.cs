using System.Linq.Expressions;
using System.Reflection;

namespace Linq.Expressions.Translation.PostgreSql.Translation;

internal sealed class Evaluator(HashSet<Expression> candidates) : ExpressionVisitor
{
    private static readonly HashSet<string> Clocks =
    [
        nameof(DateTime.Now),
        nameof(DateTime.UtcNow),
        nameof(DateTime.Today),
    ];

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

    private static Expression Evaluated(Expression node)
    {
        object? value = Value(node);
        return
            value is IQueryable query
            && !(query.Expression is ConstantExpression root && root.Value == value)
            ? Evaluate(query.Expression)
            : Expression.Constant(value, node.Type);
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

    private sealed class Nominator : ExpressionVisitor
    {
        private bool _blocked;

        public HashSet<Expression> Candidates { get; } = [];

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
