using System.Linq.Expressions;
using Linq.Expressions.Translation.PostgreSql.Shapes;
using Linq.Expressions.Translation.PostgreSql.Syntax;

namespace Linq.Expressions.Translation.PostgreSql.Translation;

internal sealed partial class Translator
{
    private static readonly SqlConstant True = new(true, typeof(bool));

    private SelectQuery Join(SqlQuery source, MethodCallExpression call)
    {
        SelectQuery outer = Opened(source);
        SelectQuery inner = Simple(Joined(call), false);
        SqlExpression condition = Matching(
            Bind(Lambda(call, 2), outer.Shape),
            Bind(Lambda(call, 3), inner.Shape)
        );
        outer.Joins.Add(new SqlJoin("INNER JOIN", inner.From!, condition));
        Absorb(outer, inner);
        outer.Shape = Bind(Lambda(call, 4, 2), outer.Shape, inner.Shape);
        return outer;
    }

    private SelectQuery GroupJoin(SqlQuery source, MethodCallExpression call)
    {
        SelectQuery outer = Opened(source);
        GroupJoinShape group = new(
            JoinedSource(call),
            Lambda(call, 3),
            Bind(Lambda(call, 2), outer.Shape)
        );
        outer.Shape = Bind(Lambda(call, 4, 2), outer.Shape, group);
        return outer;
    }

    private SelectQuery SelectMany(SqlQuery source, MethodCallExpression call)
    {
        SelectQuery outer = Opened(source);
        LambdaExpression collection = Lambda(call, 1);
        Binding binding = new(outer.Shape, collection);
        _bindings[collection.Parameters[0]] = binding;
        Expression body = collection.Body;
        bool left =
            body is MethodCallExpression
            {
                Method.Name: nameof(Queryable.DefaultIfEmpty)
            };
        Expression items = left ? ((MethodCallExpression)body).Arguments[0] : body;
        Shape inner =
            items is MemberExpression or ParameterExpression
                ? Flattened(outer, Shape(items), left, call)
            : items.Type.IsAssignableTo(typeof(IQueryable))
                ? Flattened(outer, items, binding, left)
            : throw Unsupported.Method(call.Method);
        outer.Shape =
            call.Arguments.Count > 2
                ? Bind(Lambda(call, 2, 2), outer.Shape, inner)
                : inner;
        return outer;
    }

    private Shape Flattened(
        SelectQuery outer,
        Shape items,
        bool left,
        MethodCallExpression call
    )
    {
        if (items is not GroupJoinShape { Operators.Count: 0 } group)
        {
            throw Unsupported.Method(call.Method);
        }
        SelectQuery inner = Simple(Query(group.Inner), false);
        SqlExpression condition = Conjoined(
            Matching(group.OuterKey, Bind(group.InnerKey, inner.Shape)),
            inner.Where
        );
        return Attached(outer, left ? "LEFT JOIN" : "INNER JOIN", inner, condition, left);
    }

    private Shape Flattened(
        SelectQuery outer,
        Expression items,
        Binding collection,
        bool left
    )
    {
        int uses = collection.Uses;
        SqlQuery query = Query(items);
        bool correlated = collection.Uses > uses;
        bool simple = IsSimple(query);
        SelectQuery inner = simple ? (SelectQuery)query : Wrap(query, false);
        if (simple && !correlated && !left)
        {
            Absorb(outer, inner);
            return Attached(outer, "CROSS JOIN", inner, null, left);
        }
        string join =
            left ? "LEFT JOIN"
            : simple ? "INNER JOIN"
            : "CROSS JOIN";
        return Attached(
            outer,
            simple ? join : join + " LATERAL",
            inner,
            join == "CROSS JOIN" ? null : inner.Where ?? True,
            left
        );
    }

    private static Shape Attached(
        SelectQuery outer,
        string kind,
        SelectQuery inner,
        SqlExpression? condition,
        bool left
    )
    {
        outer.Joins.Add(new SqlJoin(kind, inner.From!, condition));
        return left ? Nullified(inner.Shape) : inner.Shape;
    }

    private SqlQuery Correlated(GroupJoinShape group)
    {
        SelectQuery inner = Simple(Query(group.Inner), true);
        inner.Where = Conjoined(
            Matching(Bind(group.InnerKey, inner.Shape), group.OuterKey),
            inner.Where
        );
        return group.Operators.Aggregate<MethodCallExpression, SqlQuery>(inner, Sequence);
    }

    private static void Absorb(SelectQuery outer, SelectQuery inner)
    {
        if (inner.Where is not null)
        {
            outer.Where = And(outer.Where, inner.Where);
        }
    }

    private SelectQuery Opened(SqlQuery source)
    {
        return Open(source, Barrier.Paging | Barrier.Distinct | Barrier.Grouping);
    }

    private SqlQuery Joined(MethodCallExpression call)
    {
        return Query(JoinedSource(call));
    }

    private static Expression JoinedSource(MethodCallExpression call)
    {
        return call.Arguments[1] is ConstantExpression { Value: not IQueryable }
            ? throw Unsupported.Method(call.Method)
            : call.Arguments[1];
    }

    private SelectQuery Simple(SqlQuery query, bool ordered)
    {
        return IsSimple(query) ? (SelectQuery)query : Wrap(query, ordered);
    }

    private static bool IsSimple(SqlQuery query)
    {
        return query is SelectQuery select
            && select.Joins.Count == 0
            && !select.IsPaged
            && !select.Distinct
            && select.GroupBy is null;
    }

    private static Shape Nullified(Shape shape)
    {
        return shape.Map((value, name) => value.AsNullable(), null);
    }

    private static SqlExpression Matching(Shape left, Shape right)
    {
        return left is ObjectShape composite
            ? composite
                .Members.Zip(
                    ((ObjectShape)right).Members,
                    (first, second) => Matching(first.Shape, second.Shape)
                )
                .Aggregate((first, second) => Logical("AND", first, second))
            : Keyed(Scalar(left), Scalar(right));
    }

    // Join matches null keys with each other, which = never does.
    private static SqlBinary Keyed(SqlExpression left, SqlExpression right)
    {
        return left.Nullable && right.Nullable
            ? Distinct(left, right, false)
            : Compare("=", left, right);
    }

    private static SqlExpression Conjoined(SqlExpression first, SqlExpression? second)
    {
        return second is null ? first : Logical("AND", first, second);
    }
}
