using System.Collections;
using System.Linq.Expressions;

namespace Linq.Expressions.Translation.PostgreSql.Translation;

// A query or an in-memory sequence read through IEnumerable inside a lambda: a
// table or a query captured as IEnumerable<T>, an array, or one of them after
// ToList, ToArray, AsEnumerable or AsQueryable. The Enumerable operators over it
// are the Queryable operators of the same name, as the operators are translated by
// their names and their lambdas whether quoted or not.
internal sealed partial class Translator
{
    private static readonly HashSet<string> Conversions =
    [
        nameof(Enumerable.ToList),
        nameof(Enumerable.ToArray),
        nameof(Enumerable.AsEnumerable),
        nameof(Queryable.AsQueryable),
    ];

    // The sequence below the conversions that only change how it is read.
    private static Expression Sequence(Expression source)
    {
        while (true)
        {
            if (
                source is UnaryExpression { NodeType: ExpressionType.Convert } conversion
                && conversion.Type != typeof(string)
                && typeof(IEnumerable).IsAssignableFrom(conversion.Type)
                && typeof(IEnumerable).IsAssignableFrom(conversion.Operand.Type)
            )
            {
                source = conversion.Operand;
            }
            else if (
                source is MethodCallExpression { Arguments.Count: 1 } call
                && Conversions.Contains(call.Method.Name)
                && (
                    call.Method.DeclaringType == typeof(Enumerable)
                    || call.Method.DeclaringType == typeof(Queryable)
                )
            )
            {
                source = call.Arguments[0];
            }
            else
            {
                return source;
            }
        }
    }

    // Whether an expression is a sequence of rows: a table, a query, an in-memory
    // collection, or an Enumerable operator over one of them.
    private static bool IsSequence(Expression source)
    {
        return Sequence(source) switch
        {
            ConstantExpression { Value: IQueryable } => true,
            ConstantExpression constant => IsInMemory(constant),
            MethodCallExpression call => call.Method.DeclaringType == typeof(Queryable)
                || IsMarker(call)
                || (
                    call.Method.DeclaringType == typeof(Enumerable)
                    && call.Arguments.Count > 0
                    && IsSequence(call.Arguments[0])
                ),
            _ => false,
        };
    }

    // Whether an expression is a query, not a collection whose values are at hand.
    private static bool IsQueried(Expression source)
    {
        return IsSequence(source)
            && Sequence(source) is not ConstantExpression { Value: not IQueryable };
    }
}
