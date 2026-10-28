using System.Linq.Expressions;
using Linq.Expressions.Translation.PostgreSql.Shapes;
using Linq.Expressions.Translation.PostgreSql.Syntax;

namespace Linq.Expressions.Translation.PostgreSql.Translation;

// An aggregate, in the projection of a SELECT, over the rows that SELECT reads
// (the same query, whatever its order, below any Where and Select of its own) is a
// window aggregate over all of them, AGG(...) OVER (), computed in the pass that
// reads the rows, instead of a scalar subquery that reads the sources again. It
// needs the rows the SELECT projects to be the rows it reads: not once they are
// paged or made distinct, and the aggregate's own lambdas must read nothing but
// their own element. The projection that holds a window is only extended by what
// SQL allows beside one (a projection, an order, paging, DISTINCT): anything else
// wraps it into a derived table.
internal sealed partial class Translator
{
    private static readonly HashSet<string> Windowable =
    [
        nameof(Queryable.Count),
        nameof(Queryable.LongCount),
        nameof(Queryable.Sum),
        nameof(Queryable.Average),
        nameof(Queryable.Min),
        nameof(Queryable.Max),
        nameof(Queryable.Any),
        nameof(Queryable.All),
    ];

    // The SELECT whose projection is being bound.
    private SelectQuery? _window;

    private SqlExpression? Window(MethodCallExpression call)
    {
        if (
            _window is not SelectQuery query
            || !Windowable.Contains(call.Method.Name)
            || call.Method.GetParameters().Any(IsRejected)
            || query.IsPaged
            || query.Distinct
            || query.DistinctOn is not null
        )
        {
            return null;
        }
        List<MethodCallExpression>? layers = Layers(call.Arguments[0], query.Origin);
        LambdaExpression? argument = call.Arguments.Count > 1 ? Lambda(call, 1) : null;
        if (
            layers is null
            || !layers.All(layer => IsClosed(Lambda(layer, 1)))
            || (argument is not null && !IsClosed(argument))
        )
        {
            return null;
        }
        Shape shape = query.Shape;
        SqlExpression? filter = null;
        foreach (MethodCallExpression layer in layers)
        {
            if (layer.Method.Name == nameof(Queryable.Where))
            {
                filter = And(filter, Scalar(Bind(Lambda(layer, 1), shape)));
            }
            else
            {
                shape = Bind(Lambda(layer, 1), shape);
            }
        }
        query.Windowed = true;
        return Windowed(call, argument, shape, filter);
    }

    private SqlExpression Windowed(
        MethodCallExpression call,
        LambdaExpression? argument,
        Shape shape,
        SqlExpression? filter
    )
    {
        string name = call.Method.Name;
        if (name is nameof(Queryable.Count) or nameof(Queryable.LongCount))
        {
            return Counting(
                name,
                null,
                argument is null ? filter : And(filter, Scalar(Bind(argument, shape))),
                true
            );
        }
        if (name is nameof(Queryable.Any) or nameof(Queryable.All))
        {
            bool any = name == nameof(Queryable.Any);
            // The row projected is one of the rows, so there is one.
            if (argument is null && filter is null)
            {
                return True;
            }
            SqlAggregate quantified = new(
                any ? "BOOL_OR" : "BOOL_AND",
                argument is null ? True : Scalar(Bind(argument, shape)),
                false,
                filter,
                typeof(bool),
                true
            );
            return new SqlFunction(
                "COALESCE",
                [quantified, new SqlConstant(!any, typeof(bool))],
                typeof(bool)
            );
        }
        return Aggregate(
            name,
            Scalar(argument is null ? shape : Bind(argument, shape)),
            call.Type,
            false,
            filter,
            true
        );
    }

    // The Where and Select operators between the aggregate's source and the rows of
    // the SELECT, innermost first; null when the source is anything else.
    private static List<MethodCallExpression>? Layers(Expression source, Expression? rows)
    {
        if (rows is null)
        {
            return null;
        }
        Expression origin = Unsorted(rows);
        List<MethodCallExpression> layers = [];
        Expression current = Unsorted(source);
        while (!TreeEquality.Equal(current, origin))
        {
            if (
                current is not MethodCallExpression layer
                || layer.Method.DeclaringType != typeof(Queryable)
                || layer.Method.Name
                    is not (nameof(Queryable.Where) or nameof(Queryable.Select))
                || layer.Arguments.Count != 2
                || Lambda(layer, 1).Parameters.Count != 1
            )
            {
                return null;
            }
            layers.Insert(0, layer);
            current = Unsorted(layer.Arguments[0]);
        }
        return layers;
    }

    // An aggregate reads the rows whatever their order.
    private static Expression Unsorted(Expression expression)
    {
        while (
            expression is MethodCallExpression call
            && call.Method.DeclaringType == typeof(Queryable)
            && Sorts.Contains(call.Method.Name)
        )
        {
            expression = call.Arguments[0];
        }
        return expression;
    }

    // A lambda that reads only its own parameters: one reading the element of an
    // enclosing lambda is correlated with it, which a window over all rows is not.
    private static bool IsClosed(LambdaExpression lambda)
    {
        FreeParameters free = new();
        _ = free.Visit(lambda);
        return free.Found.Count == 0;
    }

    private sealed class FreeParameters : ExpressionVisitor
    {
        private readonly HashSet<ParameterExpression> _declared = [];

        public HashSet<ParameterExpression> Found { get; } = [];

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            _declared.UnionWith(node.Parameters);
            return base.VisitLambda(node);
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (!_declared.Contains(node))
            {
                _ = Found.Add(node);
            }
            return node;
        }
    }
}
