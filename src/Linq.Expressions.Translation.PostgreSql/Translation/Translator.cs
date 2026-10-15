using System.Linq.Expressions;
using System.Reflection;
using Linq.Expressions.Translation.PostgreSql.Mapping;
using Linq.Expressions.Translation.PostgreSql.Shapes;
using Linq.Expressions.Translation.PostgreSql.Syntax;

namespace Linq.Expressions.Translation.PostgreSql.Translation;

internal sealed partial class Translator
{
    private readonly Dictionary<ParameterExpression, Binding> _bindings = [];
    private int _aliases;

    public string Translate(Expression expression)
    {
        SqlQuery query = Query(Evaluator.Evaluate(expression));
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

    private static Type ElementType(Type sequence)
    {
        return sequence.GetInterface("IEnumerable`1")!.GetGenericArguments()[0];
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
        return expression is ConstantExpression { Value: IQueryable root }
            ? Root(root)
            : Operator((MethodCallExpression)expression);
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
        SqlQuery source = Operand(call);
        return Terminals.Contains(call.Method.Name)
            ? Statement(Terminal(source, call))
            : Sequence(source, call);
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
            nameof(Queryable.Distinct) => Distinct(source),
            nameof(Queryable.Reverse) => Reverse(source, call.Method),
            nameof(Queryable.Cast) or nameof(Queryable.OfType) => Cast(source, call),
            nameof(Queryable.Join) => Join(source, call),
            nameof(Queryable.GroupJoin) => GroupJoin(source, call),
            nameof(Queryable.SelectMany) => SelectMany(source, call),
            _ => throw Unsupported.Method(call.Method),
        };
    }

    private SelectQuery Where(SqlQuery source, LambdaExpression predicate)
    {
        SelectQuery query = Restricted(source);
        query.Where = And(query.Where, Scalar(Bind(predicate, query.Shape)));
        return query;
    }

    private SelectQuery Restricted(SqlQuery source)
    {
        return Open(source, Barrier.Paging | Barrier.Distinct);
    }

    private SelectQuery Select(SqlQuery source, LambdaExpression selector)
    {
        SelectQuery query = Open(source, Barrier.Distinct);
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
        SelectQuery query = Open(source, Barrier.None);
        query.Offset = (query.Offset ?? 0) + count;
        if (query.Limit is long limit)
        {
            query.Limit = Math.Max(limit - count, 0);
        }
        return query;
    }

    private SelectQuery Take(SqlQuery source, int count)
    {
        SelectQuery query = Open(source, Barrier.None);
        query.Limit = Math.Min(query.Limit ?? long.MaxValue, Math.Max(count, 0));
        return query;
    }

    private SelectQuery Distinct(SqlQuery source)
    {
        SelectQuery query = Open(source, Barrier.Paging);
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
        Type element = ElementType(call.Arguments[0].Type);
        if (
            call.Method.Name != nameof(Queryable.OfType)
            || Nullable.GetUnderlyingType(element) != target
        )
        {
            return source;
        }
        SelectQuery query = Restricted(source);
        query.Where = And(query.Where, new SqlIsNull(Scalar(query.Shape), true));
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
            || ((barriers & Barrier.Distinct) != 0 && query.Distinct);
    }

    private SelectQuery Wrap(SqlQuery inner)
    {
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
