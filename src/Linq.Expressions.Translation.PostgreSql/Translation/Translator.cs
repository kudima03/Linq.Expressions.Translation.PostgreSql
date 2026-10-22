using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using Linq.Expressions.Translation.PostgreSql.Mapping;
using Linq.Expressions.Translation.PostgreSql.Shapes;
using Linq.Expressions.Translation.PostgreSql.Syntax;

namespace Linq.Expressions.Translation.PostgreSql.Translation;

[RequiresUnreferencedCode(TableMapping.Trimming)]
internal sealed partial class Translator
{
    private readonly Dictionary<ParameterExpression, Binding> _bindings = [];
    private int _aliases;

    public string Translate(Expression expression)
    {
        SqlQuery query = Query(Evaluator.Evaluate(expression));
        if (query.Shape is GroupingShape)
        {
            throw new NotSupportedException(
                "Returning IGrouping results of GroupBy is not supported; project the groups with Select."
            );
        }
        SqlWriter writer = new();
        query.Render(writer);
        return writer.ToString();
    }

    private static LambdaExpression Lambda(
        MethodCallExpression call,
        int index,
        int parameters = 1
    )
    {
        Expression argument = call.Arguments[index];
        Expression operand = argument
            is UnaryExpression { NodeType: ExpressionType.Quote } quote
            ? quote.Operand
            : argument;
        return operand is LambdaExpression lambda && lambda.Parameters.Count == parameters
            ? lambda
            : throw Unsupported.Method(call.Method);
    }

    private static int Count(Expression expression)
    {
        return expression is ConstantExpression { Value: int count }
            ? count
            : throw Unsupported.Node(expression);
    }

    // A source is a queryable root or a Queryable operator returning IQueryable<T>.
    private static Type ElementType(Expression source)
    {
        return source is ConstantExpression { Value: IQueryable root }
            ? root.ElementType
            : source.Type.GetGenericArguments()[0];
    }

    private static bool IsRejected(ParameterInfo parameter)
    {
        Type type = parameter.ParameterType;
        return type == typeof(Range)
            || type == typeof(Index)
            || (
                type.IsGenericType
                && (
                    type.GetGenericTypeDefinition() == typeof(IEqualityComparer<>)
                    || type.GetGenericTypeDefinition() == typeof(IComparer<>)
                )
            );
    }

    private static SqlExpression And(SqlExpression? left, SqlExpression right)
    {
        return left is null ? right : Logical("AND", left, right);
    }

    private static SqlBinary Logical(
        string symbol,
        SqlExpression left,
        SqlExpression right
    )
    {
        return new SqlBinary(
            symbol,
            SqlBinaryKind.Logical,
            left,
            right,
            typeof(bool),
            left.Nullable || right.Nullable
        );
    }

    private SqlQuery Query(Expression expression)
    {
        return expression switch
        {
            ConstantExpression { Value: IQueryable root } => Root(root),
            MethodCallExpression call => Operator(call),
            _ => throw Unsupported.Node(expression),
        };
    }

    private SelectQuery Root(IQueryable root)
    {
        if (root is EnumerableQuery)
        {
            throw new NotSupportedException(
                $"The in-memory source '{root.GetType().Name}' cannot be translated into SQL."
            );
        }
        TableMapping table = new(root.ElementType);
        string alias = NextAlias();
        return new SelectQuery(
            new TableSource(table, alias),
            new EntityShape([
                .. table.Columns.Select(column => new EntityColumn(
                    column.Member,
                    column.Name,
                    new SqlColumn(alias, column.Name, column.Type, column.Nullable)
                )),
            ])
        );
    }

    private SqlQuery Operator(MethodCallExpression call)
    {
        return Terminals.Contains(call.Method.Name)
            ? Statement(Terminal(call))
            : Sequence(Operand(call), call);
    }

    private SqlQuery Sequence(SqlQuery source, MethodCallExpression call)
    {
        return call.Method.Name switch
        {
            nameof(Queryable.Where) => Where(source, Lambda(call, 1)),
            nameof(Queryable.Select) => Select(source, Lambda(call, 1)),
            nameof(Queryable.OrderBy) => Order(source, Lambda(call, 1), false, false),
            nameof(Queryable.OrderByDescending) => Order(
                source,
                Lambda(call, 1),
                true,
                false
            ),
            nameof(Queryable.ThenBy) => Order(source, Lambda(call, 1), false, true),
            nameof(Queryable.ThenByDescending) => Order(
                source,
                Lambda(call, 1),
                true,
                true
            ),
            nameof(Queryable.Skip) => Skip(source, Count(call.Arguments[1])),
            nameof(Queryable.Take) => Take(source, Count(call.Arguments[1])),
            nameof(Queryable.TakeLast) => Reverse(
                Take(Reverse(source, call.Method), Count(call.Arguments[1])),
                call.Method
            ),
            nameof(Queryable.SkipLast) => SkipLast(source, call),
            nameof(Queryable.Distinct) => Distinct(source),
            nameof(Queryable.Reverse) => Reverse(source, call.Method),
            nameof(Queryable.Cast) or nameof(Queryable.OfType) => Cast(source, call),
            nameof(Queryable.GroupBy) => GroupBy(source, call),
            nameof(Queryable.Concat)
            or nameof(Queryable.Union)
            or nameof(Queryable.Intersect)
            or nameof(Queryable.Except) => Combined(source, call),
            nameof(Queryable.Join) => Join(source, call),
            nameof(Queryable.GroupJoin) => GroupJoin(source, call),
            nameof(Queryable.SelectMany) => SelectMany(source, call),
            _ => throw Unsupported.Method(call.Method),
        };
    }

    private SelectQuery Where(SqlQuery source, LambdaExpression predicate)
    {
        SelectQuery query = Restricted(source);
        SqlExpression condition = Scalar(Bind(predicate, query.Shape));
        if (query.GroupBy is null)
        {
            query.Where = And(query.Where, condition);
        }
        else
        {
            query.Having = And(query.Having, condition);
        }
        return query;
    }

    private SelectQuery Restricted(SqlQuery source)
    {
        return Open(source, Barrier.Paging | Barrier.Distinct);
    }

    private SelectQuery Select(SqlQuery source, LambdaExpression selector)
    {
        SelectQuery query = Open(source, Barrier.Distinct | Barrier.Projection);
        query.Shape = Bind(selector, query.Shape);
        return query;
    }

    private SelectQuery Order(
        SqlQuery source,
        LambdaExpression selector,
        bool descending,
        bool then
    )
    {
        SelectQuery query = Open(source, Barrier.Paging | Barrier.Distinct);
        List<Ordering> keys =
        [
            .. Bind(selector, query.Shape)
                .Columns()
                .Select(column => new Ordering(column.Value, descending)),
        ];
        query.Orderings = then
            ? [.. query.Orderings, .. keys]
            : [.. keys, .. query.Orderings];
        return query;
    }

    private SqlQuery Skip(SqlQuery source, int count)
    {
        if (count <= 0)
        {
            return source;
        }
        SelectQuery query = Open(source, Barrier.Bound);
        query.Offset = (query.Offset ?? 0) + count;
        if (query.Limit is long limit)
        {
            query.Limit = Math.Max(limit - count, 0);
        }
        return query;
    }

    private SelectQuery Take(SqlQuery source, int count)
    {
        SelectQuery query = Open(source, Barrier.Bound);
        query.Limit = Math.Min(query.Limit ?? long.MaxValue, Math.Max(count, 0));
        return query;
    }

    private SelectQuery SkipLast(SqlQuery source, MethodCallExpression call)
    {
        SelectQuery query = Open(source, Barrier.Paging);
        SelectQuery counted = Unordered(
            Open(
                Query(call.Arguments[0]),
                Barrier.Paging | Barrier.Distinct | Barrier.Grouping
            )
        );
        counted.Shape = new ScalarShape(
            new SqlFunction(
                "GREATEST",
                [
                    Calculate(
                        "-",
                        new SqlAggregate("COUNT", null, false, null, typeof(long)),
                        Integer(Count(call.Arguments[1]))
                    ),
                    Integer(0),
                ],
                typeof(long)
            )
        );
        query.Bound = new SqlScalarSubquery(counted, typeof(long));
        return query;
    }

    private SelectQuery Distinct(SqlQuery source)
    {
        SelectQuery query = Open(source, Barrier.Paging);
        HashSet<string> projected =
        [
            .. query.Shape.Columns().Select(column => SqlWriter.Text(column.Value)),
        ];
        // SELECT DISTINCT can only be ordered by selected values; past the first hidden
        // key the order of the distinct rows is no longer the order of the source.
        query.Orderings =
        [
            .. query.Orderings.TakeWhile(ordering =>
                projected.Contains(SqlWriter.Text(ordering.Key))
            ),
        ];
        query.Distinct = true;
        return query;
    }

    private SelectQuery Reverse(SqlQuery source, MethodInfo method)
    {
        SelectQuery query = Open(source, Barrier.Paging);
        if (query.Orderings.Count == 0)
        {
            throw Unsupported.Method(method);
        }
        query.Orderings = [.. query.Orderings.Select(ordering => ordering.Inverted())];
        return query;
    }

    private SqlQuery Cast(SqlQuery source, MethodCallExpression call)
    {
        Type target = call.Method.GetGenericArguments()[0];
        Type element = ElementType(call.Arguments[0]);
        if (call.Method.Name == nameof(Queryable.OfType))
        {
            return OfType(source, element, target);
        }
        if (!ScalarTypes.IsNumber(element) || !ScalarTypes.IsNumber(target))
        {
            return source;
        }
        SelectQuery query = Open(source, Barrier.Distinct | Barrier.Projection);
        query.Shape = new ScalarShape(Convert(Scalar(query.Shape), element, target));
        return query;
    }

    private SqlQuery OfType(SqlQuery source, Type element, Type target)
    {
        Type value = Nullable.GetUnderlyingType(element) ?? element;
        bool compatible = target.IsAssignableFrom(value);
        if (compatible && value == element)
        {
            return source;
        }
        SelectQuery query = Restricted(source);
        query.Where = And(
            query.Where,
            compatible
                ? new SqlIsNull(Scalar(query.Shape), true)
                : new SqlConstant(false, typeof(bool))
        );
        return query;
    }

    private SelectQuery Open(SqlQuery source, Barrier barriers)
    {
        return source is SelectQuery query && !IsBlocked(query, barriers)
            ? query
            : Wrap(source);
    }

    private static bool IsBlocked(SelectQuery query, Barrier barriers)
    {
        return ((barriers & Barrier.Paging) != 0 && query.IsPaged)
            || ((barriers & Barrier.Distinct) != 0 && query.Distinct)
            || ((barriers & Barrier.Grouping) != 0 && query.GroupBy is not null)
            || ((barriers & Barrier.Bound) != 0 && query.Bound is not null)
            || (
                (barriers & Barrier.Projection) != 0
                && query.GroupBy is not null
                && query.Shape is not GroupingShape
            );
    }

    private SelectQuery Wrap(SqlQuery inner)
    {
        Named(inner);
        string alias = NextAlias();
        Dictionary<string, string> names = [];
        foreach (ProjectionColumn column in inner.Shape.Columns())
        {
            _ = names.TryAdd(SqlWriter.Text(column.Value), column.Name!);
        }
        SelectQuery outer = new(
            new DerivedSource(inner, alias),
            inner.Shape.Map(
                (value, name) => new SqlColumn(alias, name!, value.Type, value.Nullable),
                null
            )
        );
        if (inner is SelectQuery query)
        {
            outer.Orderings =
            [
                .. query
                    .Orderings.Where(ordering =>
                        names.ContainsKey(SqlWriter.Text(ordering.Key))
                    )
                    .Select(ordering => new Ordering(
                        new SqlColumn(
                            alias,
                            names[SqlWriter.Text(ordering.Key)],
                            ordering.Key.Type,
                            ordering.Key.Nullable
                        ),
                        ordering.Descending
                    )),
            ];
            if (!query.IsPaged)
            {
                query.Orderings = [];
            }
        }
        return outer;
    }

    private static void Named(SqlQuery query)
    {
        if (query is SetOperationQuery operation)
        {
            Named(operation.Left);
        }
        if (query.Shape is ScalarShape scalar)
        {
            query.Shape = scalar.Named();
        }
    }

    private string NextAlias()
    {
        return "t" + _aliases++;
    }

    private Shape Bind(LambdaExpression lambda, params Shape[] shapes)
    {
        for (int index = 0; index < shapes.Length; index++)
        {
            _bindings[lambda.Parameters[index]] = new Binding(shapes[index], lambda);
        }
        return Shape(lambda.Body);
    }
}
