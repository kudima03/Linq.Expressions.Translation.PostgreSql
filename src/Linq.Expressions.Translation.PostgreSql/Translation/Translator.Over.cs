using System.Linq.Expressions;
using Linq.Expressions.Translation.PostgreSql.Mapping;
using Linq.Expressions.Translation.PostgreSql.Shapes;
using Linq.Expressions.Translation.PostgreSql.Syntax;

namespace Linq.Expressions.Translation.PostgreSql.Translation;

// PostgreSqlWindows.Over is the projection of a SELECT whose window functions are
// over one window: OVER (PARTITION BY ... ORDER BY ... frame). Window functions are
// computed after WHERE, GROUP BY and HAVING and before DISTINCT, ORDER BY and
// paging, so a paged, distinct or windowed source is wrapped into a derived table,
// and the result is wrapped by what SQL does not allow beside a window, as a
// projection with a window aggregate is (Barrier.Window).
internal sealed partial class Translator
{
    private static readonly HashSet<string> WindowSorts =
    [
        nameof(Enumerable.Order),
        nameof(Enumerable.OrderDescending),
        nameof(Enumerable.OrderBy),
        nameof(Enumerable.OrderByDescending),
        nameof(Enumerable.ThenBy),
        nameof(Enumerable.ThenByDescending),
    ];

    // The types of an ORDER BY key an interval can be added to.
    private static readonly HashSet<Type> Spanned =
    [
        typeof(DateTime),
        typeof(DateTimeOffset),
        typeof(DateOnly),
        typeof(TimeOnly),
        typeof(TimeSpan),
    ];

    private static bool IsMarker(MethodCallExpression call)
    {
        return call.Method.DeclaringType == typeof(PostgreSqlWindows)
            || call.Method.DeclaringType == typeof(PostgreSqlRecursion);
    }

    private SelectQuery Marked(MethodCallExpression call)
    {
        return call.Method.Name == nameof(PostgreSqlWindows.Over)
            ? Over(call)
            : Recursive(call);
    }

    // DISTINCT ON keeps its rows after the window functions of its SELECT are
    // computed, so a window over them is one over a derived table of them.
    private SelectQuery Over(MethodCallExpression call)
    {
        SqlQuery source = Query(call.Arguments[0]);
        SelectQuery query = source is SelectQuery { DistinctOn: not null }
            ? Wrap(source)
            : Open(
                source,
                Barrier.Paging | Barrier.Distinct | Barrier.Projection | Barrier.Window
            );
        List<SqlExpression> partition = Optional(call, 1) is LambdaExpression partitionBy
            ?
            [
                .. Bind(partitionBy, query.Shape)
                    .Columns()
                    .Select(column => column.Value)
                    .Where(value => value is not SqlConstant),
            ]
            : [];
        PostgreSqlFrame? frame = call.Arguments[3] is ConstantExpression constant
            ? (PostgreSqlFrame?)constant.Value
            : throw new NotSupportedException(
                "The frame of a window must be a constant, not computed for each row."
            );
        // A RANGE with an offset measures the distance of its one key: a key that
        // may be NULL is not split in two to put NaN where .NET sorts it.
        List<Ordering> order = Optional(call, 2) is LambdaExpression orderBy
            ? WindowOrder(
                orderBy,
                query.Shape,
                frame?.Kind is FrameKind.NumericRange or FrameKind.IntervalRange
            )
            : [];
        WindowShape window = new(
            new SqlWindow(
                partition,
                order,
                frame is null ? null : Framed(frame, order),
                frame?.IncludesCurrentRow ?? true
            ),
            query.Shape,
            frame?.IncludesCurrentRow ?? true
        );
        LambdaExpression selector = Lambda(call, 4, 2);
        if (new NestedWindow(selector.Parameters[1]).Found(selector.Body))
        {
            throw new NotSupportedException(
                "A window function is computed in the selector of its Over, not in a lambda or a subquery of it: "
                    + selector
            );
        }
        SelectQuery? outer = _window;
        _window = query;
        query.Shape = Bind(selector, query.Shape, window);
        _window = outer;
        query.Windowed = true;
        return query;
    }

    // An absent lambda of Over is a null constant.
    private static LambdaExpression? Optional(MethodCallExpression call, int index)
    {
        return call.Arguments[index] is ConstantExpression { Value: null }
            ? null
            : Lambda(call, index);
    }

    // The ORDER BY of a window: OrderBy (or Order) of its rows, then ThenBy, each
    // ascending or descending and by the default or the ordinal comparer.
    private List<Ordering> WindowOrder(LambdaExpression orderBy, Shape row, bool ranged)
    {
        List<Ordering> order = [];
        Expression body = orderBy.Body;
        while (
            body is MethodCallExpression sort
            && sort.Method.DeclaringType == typeof(Enumerable)
            && WindowSorts.Contains(sort.Method.Name)
        )
        {
            string name = sort.Method.Name;
            bool sorted =
                name is nameof(Enumerable.Order) or nameof(Enumerable.OrderDescending);
            bool ordinal = IsOrdinal(sort);
            if (sort.Arguments.Count > (sorted ? 1 : 2) && !ordinal)
            {
                throw Unsupported.Method(sort.Method);
            }
            Shape key = sorted ? row : Bind(Lambda(sort, 1), row);
            bool descending = name.EndsWith("Descending", StringComparison.Ordinal);
            order.InsertRange(
                0,
                key.Columns()
                    .Where(column => column.Value is not SqlConstant)
                    .SelectMany(column =>
                        ranged && column.Value.Nullable
                            ? [new Ordering(column.Value, descending)]
                            : Keys(
                                ordinal ? CodeUnits(column.Value) : column.Value,
                                descending,
                                descending
                            )
                    )
            );
            body = sort.Arguments[0];
            if (!name.StartsWith("Then", StringComparison.Ordinal))
            {
                break;
            }
        }
        return body == orderBy.Parameters[0]
            ? order
            : throw new NotSupportedException(
                "The order of a window is OrderBy or Order of its rows, followed by ThenBy: "
                    + orderBy
            );
    }

    // The frame clause. A RANGE with an offset needs exactly one key of a type the
    // offset can be added to.
    private static string Framed(PostgreSqlFrame frame, List<Ordering> order)
    {
        if (frame.Kind is not (FrameKind.NumericRange or FrameKind.IntervalRange))
        {
            return frame.TextValue;
        }
        Type? key = order.Count == 1 ? ScalarTypes.Underlying(order[0].Key.Type) : null;
        return key is null
            || (frame.Kind == FrameKind.NumericRange && !ScalarTypes.IsNumber(key))
            || (frame.Kind == FrameKind.IntervalRange && !Spanned.Contains(key))
                ? throw new NotSupportedException(
                    "A RANGE frame with an offset needs the window to be ordered by exactly one key, a number for a numeric offset and a date, a time or a duration for an interval."
                )
            : frame.Kind == FrameKind.NumericRange && !Fractional.Contains(key)
                ? "RANGE BETWEEN "
                    + Whole(frame.Start, true)
                    + " AND "
                    + Whole(frame.End, false)
            : frame.TextValue;
    }

    // A bound of a RANGE over an integer key. PostgreSQL takes an integer offset
    // there, no numeric one: the keys from x + start to x + end are the integers from
    // the next whole offset inside the frame, and an offset past a bigint is the
    // largest one, which reaches every key.
    private static string Whole(decimal? offset, bool start)
    {
        if (offset is not decimal value)
        {
            return start ? "UNBOUNDED PRECEDING" : "UNBOUNDED FOLLOWING";
        }
        if (value == 0)
        {
            return "CURRENT ROW";
        }
        decimal distance = Math.Min(
            start == (value < 0)
                ? Math.Floor(Math.Abs(value))
                : Math.Ceiling(Math.Abs(value)),
            long.MaxValue
        );
        return SqlLiteral.Number(distance) + (value < 0 ? " PRECEDING" : " FOLLOWING");
    }

    // PostgreSqlWindow<TSource>.Rows, the rows of the frame.
    private static FrameShape Framed(WindowShape window, MemberExpression member)
    {
        return member.Member.Name == nameof(PostgreSqlWindow<>.Rows)
            ? new FrameShape(window, window.Element, null)
            : throw Unsupported.Member(member.Member);
    }

    // An aggregate over the rows of a frame, after Where (FILTER) and Select. Its
    // lambdas read the rows of the frame, never the current row, which SQL cannot
    // name inside an aggregate.
    private Shape Framed(FrameShape frame, MethodCallExpression call)
    {
        string name = call.Method.Name;
        LambdaExpression? argument = call.Arguments.Count > 1 ? Lambda(call, 1) : null;
        if (argument is not null && !IsClosed(argument))
        {
            throw Unclosed(argument);
        }
        SqlWindow window = frame.Window.Window;
        SqlExpression? Predicate()
        {
            return argument is null
                ? frame.Filter
                : And(frame.Filter, Scalar(Bind(argument, frame.Element)));
        }
        return name switch
        {
            nameof(Enumerable.Where) => new FrameShape(
                frame.Window,
                frame.Element,
                Predicate()
            ),
            nameof(Enumerable.Select) => new FrameShape(
                frame.Window,
                Bind(argument!, frame.Element),
                frame.Filter
            ),
            nameof(Enumerable.Count) or nameof(Enumerable.LongCount) => new ScalarShape(
                Counting(name, null, Predicate(), false, window)
            ),
            nameof(Enumerable.Sum)
            or nameof(Enumerable.Average)
            or nameof(Enumerable.Min)
            or nameof(Enumerable.Max) => new ScalarShape(
                Aggregate(
                    name,
                    Scalar(
                        argument is null ? frame.Element : Bind(argument, frame.Element)
                    ),
                    call.Type,
                    false,
                    frame.Filter,
                    false,
                    window
                )
            ),
            nameof(Enumerable.Any) or nameof(Enumerable.All) => new ScalarShape(
                FrameQuantified(frame, argument, name == nameof(Enumerable.Any))
            ),
            _ => throw Unsupported.Method(call.Method),
        };
    }

    // BOOL_OR and BOOL_AND are NULL over no row, where Any is false and All true.
    private SqlFunction FrameQuantified(
        FrameShape frame,
        LambdaExpression? argument,
        bool any
    )
    {
        SqlAggregate quantified = new(
            any ? "BOOL_OR" : "BOOL_AND",
            argument is null ? True : Scalar(Bind(argument, frame.Element)),
            false,
            frame.Filter,
            typeof(bool),
            false,
            frame.Window.Window
        );
        return new SqlFunction(
            "COALESCE",
            [quantified, new SqlConstant(!any, typeof(bool))],
            typeof(bool)
        );
    }

    private static NotSupportedException Unclosed(LambdaExpression lambda)
    {
        return new NotSupportedException(
            "A lambda over the rows of a window reads only its own row, not the current row or another lambda's: "
                + lambda
        );
    }

    // ROW_NUMBER, RANK, ..., LAG, LEAD, FIRST_VALUE and LAST_VALUE over the
    // window of the selector of Over, in the projection of its SELECT.
    // An aggregate of a table in an argument of a window function is a subquery, not
    // a window aggregate of the SELECT, which PostgreSQL would find nested; and a
    // window function in an argument, which nothing but a derived table of its own
    // can compute first, is refused.
    private SqlExpression WindowFunction(MethodCallExpression call)
    {
        SqlExpression function = Plain(() => Function(call));
        return function.Any(part =>
            part.IsWindow
            && part.Operands.Any(operand => operand.Any(inner => inner.IsWindow))
        )
            ? throw new NotSupportedException(
                $"'{call.Method.Name}' reads a window function in its arguments, and PostgreSQL nests no window function in another: compute it in an Over of its own and read its result here."
            )
            : function;
    }

    // What is bound without the SELECT whose projection is being bound, so that an
    // aggregate of its rows is a subquery.
    private T Plain<T>(Func<T> bind)
    {
        SelectQuery? window = _window;
        _window = null;
        T bound = bind();
        _window = window;
        return bound;
    }

    private SqlExpression Function(MethodCallExpression call)
    {
        return
            call.Method.Name == nameof(PostgreSqlWindows.Over)
            || Shape(call.Arguments[0]) is not WindowShape window
            ? throw Unsupported.Method(call.Method)
            : call.Method.Name switch
            {
                nameof(PostgreSqlWindows.RowNumber) => Ranking(
                    "ROW_NUMBER",
                    window,
                    call
                ),
                nameof(PostgreSqlWindows.Rank) => Ranking("RANK", window, call),
                nameof(PostgreSqlWindows.DenseRank) => Ranking(
                    "DENSE_RANK",
                    window,
                    call
                ),
                nameof(PostgreSqlWindows.PercentRank) => Ranking(
                    "PERCENT_RANK",
                    window,
                    call
                ),
                nameof(PostgreSqlWindows.CumeDist) => Ranking("CUME_DIST", window, call),
                nameof(PostgreSqlWindows.Ntile) => new SqlWindowFunction(
                    "NTILE",
                    [Sql(call.Arguments[1])],
                    window.Window,
                    typeof(int),
                    false
                ),
                nameof(PostgreSqlWindows.Lag) => Shifted("LAG", window, call),
                nameof(PostgreSqlWindows.Lead) => Shifted("LEAD", window, call),
                nameof(PostgreSqlWindows.FirstValue) => Valued(
                    "FIRST_VALUE",
                    window,
                    call
                ),
                _ => Valued("LAST_VALUE", window, call),
            };
    }

    private static SqlWindowFunction Ranking(
        string name,
        WindowShape window,
        MethodCallExpression call
    )
    {
        return new SqlWindowFunction(name, [], window.Window, call.Type, false);
    }

    // LAG and LEAD read their value in another row, and their default in this one,
    // where a null default is PostgreSQL's own: NULL for a missing row.
    private SqlExpression Shifted(
        string name,
        WindowShape window,
        MethodCallExpression call
    )
    {
        SqlExpression value = Read(window, call);
        SqlExpression offset = Sql(call.Arguments[2]);
        SqlExpression fallback = Sql(call.Arguments[3]);
        // PostgreSQL nests no window function in another. A default that is one is
        // read beside the function, where there is no row, which a value that is
        // never NULL tells: COALESCE(LAG(value, offset) OVER (...), default).
        if (fallback.Any(part => part.IsWindow))
        {
            return value.Nullable
                ? throw new NotSupportedException(
                    $"A default of '{name}' that is a window function is read where '{name}' is NULL, which tells a missing row only for a value that is never NULL."
                )
                : new SqlFunction(
                    "COALESCE",
                    [
                        new SqlWindowFunction(
                            name,
                            [value, offset],
                            window.Window,
                            call.Type,
                            true
                        ),
                        fallback,
                    ],
                    call.Type,
                    fallback.Nullable
                );
        }
        return Missing(
            name,
            fallback is SqlConstant { Value: null }
                ? [value, offset]
                : [value, offset, fallback],
            window,
            call,
            value.Nullable || fallback.Nullable
        );
    }

    // FIRST_VALUE and LAST_VALUE are NULL where the frame is empty.
    private SqlWindowFunction Valued(
        string name,
        WindowShape window,
        MethodCallExpression call
    )
    {
        SqlExpression value = Read(window, call);
        return Missing(
            name,
            [value],
            window,
            call,
            !window.IncludesCurrentRow || value.Nullable
        );
    }

    // The value a function reads in another row of the window.
    private SqlExpression Read(WindowShape window, MethodCallExpression call)
    {
        LambdaExpression value = Lambda(call, 1);
        return IsClosed(value)
            ? Scalar(Bind(value, window.Element))
            : throw Unclosed(value);
    }

    // A function that may find no row is NULL there, which a non-nullable value type
    // cannot hold.
    private static SqlWindowFunction Missing(
        string name,
        IReadOnlyList<SqlExpression> arguments,
        WindowShape window,
        MethodCallExpression call,
        bool nullable
    )
    {
        return
            nullable
            && call.Type.IsValueType
            && Nullable.GetUnderlyingType(call.Type) is null
            ? throw new NotSupportedException(
                $"'{call.Method.Name}' may find no row, where it is NULL: read a nullable value, as (int?)x.Value, or give a default that is never NULL."
            )
            : new SqlWindowFunction(name, arguments, window.Window, call.Type, nullable);
    }

    // Whether the window is read inside a lambda of the selector, a predicate of a
    // subquery for example, where SQL cannot compute it.
    private sealed class NestedWindow(ParameterExpression window) : ExpressionVisitor
    {
        private int _depth;

        private bool _found;

        public bool Found(Expression body)
        {
            _ = Visit(body);
            return _found;
        }

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            _depth++;
            _ = base.VisitLambda(node);
            _depth--;
            return node;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            _found |= node == window && _depth > 0;
            return node;
        }
    }
}
