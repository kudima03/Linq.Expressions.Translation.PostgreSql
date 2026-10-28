using System.Linq.Expressions;
using System.Reflection;

namespace Linq.Expressions.Translation.PostgreSql.Translation;

// Whether two trees are the same query: the same nodes with the same members,
// methods and constants, the parameters of their lambdas matched by position and
// any other parameter the same one. A node this does not know is never the same.
internal sealed class TreeEquality
{
    private readonly Dictionary<ParameterExpression, ParameterExpression> _parameters =
    [];

    public static bool Equal(Expression? left, Expression? right)
    {
        return new TreeEquality().Same(left, right);
    }

    private bool Same(Expression? left, Expression? right)
    {
        return ReferenceEquals(left, right)
            || (
                left is not null
                && right is not null
                && left.NodeType == right.NodeType
                && Nodes(left, right)
            );
    }

    private bool Nodes(Expression left, Expression right)
    {
        return (left, right) switch
        {
            (ConstantExpression constant, ConstantExpression other) => Constants(
                constant,
                other
            ),
            _ when left.Type != right.Type => false,
            (DefaultExpression, DefaultExpression) => true,
            (ParameterExpression first, ParameterExpression second) =>
                _parameters.TryGetValue(first, out ParameterExpression? matched)
                    ? matched == second
                    : first == second,
            (LambdaExpression first, LambdaExpression second) => Lambdas(first, second),
            (MethodCallExpression first, MethodCallExpression second) => first.Method
                == second.Method
                && Same(first.Object, second.Object)
                && All(first.Arguments, second.Arguments),
            (MemberExpression first, MemberExpression second) => first.Member
                == second.Member
                && Same(first.Expression, second.Expression),
            (BinaryExpression first, BinaryExpression second) => first.Method
                == second.Method
                && Same(first.Left, second.Left)
                && Same(first.Right, second.Right)
                && Same(first.Conversion, second.Conversion),
            (UnaryExpression first, UnaryExpression second) => first.Method
                == second.Method
                && Same(first.Operand, second.Operand),
            (ConditionalExpression first, ConditionalExpression second) => Same(
                first.Test,
                second.Test
            )
                && Same(first.IfTrue, second.IfTrue)
                && Same(first.IfFalse, second.IfFalse),
            (NewExpression first, NewExpression second) => first.Constructor
                == second.Constructor
                && Members(first.Members, second.Members)
                && All(first.Arguments, second.Arguments),
            (NewArrayExpression first, NewArrayExpression second) => All(
                first.Expressions,
                second.Expressions
            ),
            (MemberInitExpression first, MemberInitExpression second) => Same(
                first.NewExpression,
                second.NewExpression
            ) && Bindings(first.Bindings, second.Bindings),
            (InvocationExpression first, InvocationExpression second) => Same(
                first.Expression,
                second.Expression
            ) && All(first.Arguments, second.Arguments),
            (TypeBinaryExpression first, TypeBinaryExpression second) => first.TypeOperand
                == second.TypeOperand
                && Same(first.Expression, second.Expression),
            (IndexExpression first, IndexExpression second) => first.Indexer
                == second.Indexer
                && Same(first.Object, second.Object)
                && All(first.Arguments, second.Arguments),
            (DeferredConstant first, DeferredConstant second) => Same(
                first.Operand,
                second.Operand
            ),
            _ => false,
        };
    }

    // A queryable root is the table of its element type, whichever object stands
    // for it and whatever the constant is typed; an in-memory sequence is its own.
    private static bool Constants(ConstantExpression left, ConstantExpression right)
    {
        return (left.Type == right.Type && Equals(left.Value, right.Value))
            || (
                left.Value is IQueryable first
                && right.Value is IQueryable second
                && first is not EnumerableQuery
                && second is not EnumerableQuery
                && first.ElementType == second.ElementType
            );
    }

    private bool Lambdas(LambdaExpression left, LambdaExpression right)
    {
        if (left.Parameters.Count != right.Parameters.Count)
        {
            return false;
        }
        for (int index = 0; index < left.Parameters.Count; index++)
        {
            _parameters[left.Parameters[index]] = right.Parameters[index];
        }
        bool same = Same(left.Body, right.Body);
        foreach (ParameterExpression parameter in left.Parameters)
        {
            _ = _parameters.Remove(parameter);
        }
        return same;
    }

    private bool All(IReadOnlyList<Expression> left, IReadOnlyList<Expression> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }
        for (int index = 0; index < left.Count; index++)
        {
            if (!Same(left[index], right[index]))
            {
                return false;
            }
        }
        return true;
    }

    private bool Bindings(
        IReadOnlyList<MemberBinding> left,
        IReadOnlyList<MemberBinding> right
    )
    {
        if (left.Count != right.Count)
        {
            return false;
        }
        for (int index = 0; index < left.Count; index++)
        {
            if (
                left[index] is not MemberAssignment first
                || right[index] is not MemberAssignment second
                || first.Member != second.Member
                || !Same(first.Expression, second.Expression)
            )
            {
                return false;
            }
        }
        return true;
    }

    private static bool Members(
        IReadOnlyList<MemberInfo>? left,
        IReadOnlyList<MemberInfo>? right
    )
    {
        return left is null || right is null
            ? left is null && right is null
            : left.SequenceEqual(right);
    }
}
