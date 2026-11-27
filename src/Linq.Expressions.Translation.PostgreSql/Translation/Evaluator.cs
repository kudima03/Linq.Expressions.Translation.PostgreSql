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

    // A constant test decides the conditional: only the branch it takes is
    // translated, as the other one is never reached, whatever it would do.
    protected override Expression VisitConditional(ConditionalExpression node)
    {
        Expression test = Visit(node.Test)!;
        return test is ConstantExpression { Value: bool taken }
            ? Visit(taken ? node.IfTrue : node.IfFalse)!
            : node.Update(test, Lazily(node.IfTrue), Lazily(node.IfFalse));
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

    // Max and Min of a captured array bind to MemoryExtensions over a span of it,
    // which is no value an expression can be folded into: once the array is a
    // constant, they are folded as Enumerable.Max and Enumerable.Min would be.
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        Expression visited = base.VisitMethodCall(node);
        if (
            visited
                is not MethodCallExpression
                {
                    Arguments: [MethodCallExpression span]
                } call
            || call.Method.DeclaringType != typeof(MemoryExtensions)
            || call.Method.Name is not (nameof(Enumerable.Max) or nameof(Enumerable.Min))
            || span.Method.Name != "op_Implicit"
            || span.Arguments is not [ConstantExpression { Value: Array items }]
        )
        {
            return visited;
        }
        // Where it may not be reached, an extreme that fails is left as it is.
        try
        {
            return Expression.Constant(
                Extreme(items, call.Method.Name == nameof(Enumerable.Max)),
                call.Type
            );
        }
        catch (InvalidOperationException) when (_lazy)
        {
            return visited;
        }
    }

    // The greatest or the least of the values, nulls skipped, as the default comparer
    // orders them; of no value, null for a type that has it and a failure otherwise.
    private static object? Extreme(Array items, bool greatest)
    {
        object? extreme = null;
        foreach (object item in items.Cast<object?>().OfType<object>())
        {
            int order = extreme is null
                ? 1
                : System.Collections.Comparer.DefaultInvariant.Compare(item, extreme);
            if (extreme is null || (greatest ? order > 0 : order < 0))
            {
                extreme = item;
            }
        }
        Type element = items.GetType().GetElementType()!;
        return
            extreme is null
            && element.IsValueType
            && Nullable.GetUnderlyingType(element) is null
            ? throw new InvalidOperationException("Sequence contains no elements")
            : extreme;
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
            && !IsEnumeration(node)
            && !IsClock(node);
    }

    // A call that reads a query as a sequence, string.Join or ToList of a captured
    // IQueryable: evaluated, it would run the query while the statement is written.
    // It is left in the tree, where the query is a subquery.
    private static bool IsEnumeration(Expression node)
    {
        return node is MethodCallExpression call
            && !typeof(IQueryable).IsAssignableFrom(call.Type)
            && call.Arguments.Prepend(call.Object)
                .OfType<Expression>()
                .Any(argument =>
                    typeof(IQueryable).IsAssignableFrom(
                        (
                            argument
                                is UnaryExpression
                                {
                                    NodeType: ExpressionType.Convert
                                } conversion
                                ? conversion.Operand
                                : argument
                        ).Type
                    )
                );
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
            && (
                (
                    call.Method.DeclaringType == typeof(Queryable)
                    && call.Method.Name != nameof(Queryable.AsQueryable)
                )
                || call.Method.DeclaringType == typeof(PostgreSqlWindows)
                || call.Method.DeclaringType == typeof(PostgreSqlRecursion)
            );
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
