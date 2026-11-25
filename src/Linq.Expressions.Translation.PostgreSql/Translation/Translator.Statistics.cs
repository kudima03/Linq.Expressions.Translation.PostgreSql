using System.Linq.Expressions;
using Linq.Expressions.Translation.PostgreSql.Shapes;
using Linq.Expressions.Translation.PostgreSql.Syntax;

namespace Linq.Expressions.Translation.PostgreSql.Translation;

// The statistical aggregates of PostgreSqlFunctions over the rows of a group, of a
// window's frame or of a query: an aggregate of the grouped SELECT, a window
// aggregate or a scalar subquery.
internal sealed partial class Translator
{
    private static readonly Dictionary<string, string> Statistics = new()
    {
        [nameof(PostgreSqlFunctions.StddevPop)] = "STDDEV_POP",
        [nameof(PostgreSqlFunctions.StddevSamp)] = "STDDEV_SAMP",
        [nameof(PostgreSqlFunctions.VarPop)] = "VAR_POP",
        [nameof(PostgreSqlFunctions.VarSamp)] = "VAR_SAMP",
        [nameof(PostgreSqlFunctions.Corr)] = "CORR",
        [nameof(PostgreSqlFunctions.CovarPop)] = "COVAR_POP",
        [nameof(PostgreSqlFunctions.CovarSamp)] = "COVAR_SAMP",
        [nameof(PostgreSqlFunctions.RegrAvgX)] = "REGR_AVGX",
        [nameof(PostgreSqlFunctions.RegrAvgY)] = "REGR_AVGY",
        [nameof(PostgreSqlFunctions.RegrCount)] = "REGR_COUNT",
        [nameof(PostgreSqlFunctions.RegrIntercept)] = "REGR_INTERCEPT",
        [nameof(PostgreSqlFunctions.RegrR2)] = "REGR_R2",
        [nameof(PostgreSqlFunctions.RegrSlope)] = "REGR_SLOPE",
        [nameof(PostgreSqlFunctions.RegrSxx)] = "REGR_SXX",
        [nameof(PostgreSqlFunctions.RegrSxy)] = "REGR_SXY",
        [nameof(PostgreSqlFunctions.RegrSyy)] = "REGR_SYY",
    };

    private SqlExpression Statistic(MethodCallExpression call)
    {
        string name = Statistics[call.Method.Name];
        List<LambdaExpression> selectors =
        [
            .. Enumerable
                .Range(1, call.Arguments.Count - 1)
                .Select(index => Lambda(call, index)),
        ];
        Expression rows = call.Arguments[0];
        if (rows.Type.IsAssignableTo(typeof(IQueryable)))
        {
            SelectQuery query = Unordered(Open(Query(rows), Barrier.Aggregation, false));
            query.Shape = new ScalarShape(
                Statistic(name, selectors, query.Shape, null, null, call.Type)
            );
            return new SqlScalarSubquery(query, call.Type);
        }
        return Shape(rows) switch
        {
            FrameShape frame => selectors.All(IsClosed)
                ? Statistic(
                    name,
                    selectors,
                    frame.Element,
                    frame.Filter,
                    frame.Window.Window,
                    call.Type
                )
                : throw Unclosed(selectors.First(selector => !IsClosed(selector))),
            GroupingShape { Distinct: true } => throw new NotSupportedException(
                $"'{call.Method.Name}' over distinct group elements is not supported; aggregate the values of the elements."
            ),
            GroupingShape { Element: Shape element } group => Statistic(
                name,
                selectors,
                element,
                group.Filter,
                null,
                call.Type
            ),
            _ => throw new NotSupportedException(
                $"'{call.Method.Name}' aggregates the rows of a group, of a window's frame or of a query."
            ),
        };
    }

    private SqlAggregate Statistic(
        string name,
        List<LambdaExpression> selectors,
        Shape element,
        SqlExpression? filter,
        SqlWindow? window,
        Type type
    )
    {
        List<SqlExpression> values =
        [
            .. selectors.Select(selector => Scalar(Bind(selector, element))),
        ];
        return new SqlAggregate(
            name,
            values.Count == 1 ? values[0] : new SqlArguments(values),
            false,
            filter,
            type,
            false,
            window
        );
    }
}
