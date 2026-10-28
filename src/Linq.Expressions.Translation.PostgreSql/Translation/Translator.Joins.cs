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
        Shape inner = items.Type.IsAssignableTo(typeof(IQueryable))
            ? Flattened(outer, items, binding, left)
            : Flattened(outer, Shape(items), left, call);
        outer.Shape =
            call.Arguments.Count > 2
                ? Bind(Lambda(call, 2, 2), outer.Shape, inner)
                : inner;
        return outer;
    }

    // The matches of a GroupJoin are filtered in the join condition: filtered after the
    // join, an outer row whose matches all fail the filter would be dropped.
    private Shape Flattened(
        SelectQuery outer,
        Shape items,
        bool left,
        MethodCallExpression call
    )
    {
        if (items is not GroupJoinShape group || group.Operators.Any(IsNotFilter))
        {
            throw Unsupported.Method(call.Method);
        }
        SelectQuery inner = Joinable(Query(group.Inner), left);
        SqlExpression condition = Conjoined(
            Matching(group.OuterKey, Bind(group.InnerKey, inner.Shape)),
            inner.Where
        );
        foreach (MethodCallExpression filter in group.Operators)
        {
            condition = Logical(
                "AND",
                condition,
                Scalar(Bind(Lambda(filter, 1), inner.Shape))
            );
        }
        return Attached(outer, left ? "LEFT JOIN" : "INNER JOIN", inner, condition, left);
    }

    private static bool IsNotFilter(MethodCallExpression call)
    {
        return call.Method.Name != nameof(Enumerable.Where);
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
        bool simple = IsJoinable(query, left);
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
            && !select.KeepsOrder
            && !select.Distinct
            && select.GroupBy is null;
    }

    // LEFT JOIN fills an unmatched row with NULLs only for the columns of the joined
    // source: a computed value, a constant or an outer column projected by the source
    // would be evaluated on them instead of being NULL, as the default element is in
    // LINQ to Objects, so such a source is joined as a derived table.
    private SelectQuery Joinable(SqlQuery query, bool left)
    {
        return IsJoinable(query, left) ? (SelectQuery)query : Wrap(query, false);
    }

    private static bool IsJoinable(SqlQuery query, bool left)
    {
        return IsSimple(query) && (!left || ProjectsOwnColumns((SelectQuery)query));
    }

    private static bool ProjectsOwnColumns(SelectQuery query)
    {
        return query
            .Shape.Columns()
            .All(column =>
                column.Value is SqlColumn own && own.Alias == query.From!.Alias
            );
    }

    private static Shape Nullified(Shape shape)
    {
        return shape.Map((value, name) => value.AsNullable(), null);
    }

    // Join never matches a null key, as = does not; a composite key is an object that
    // is never null itself and compares its members with the default comparer, where
    // null equals null.
    private static SqlExpression Matching(Shape left, Shape right)
    {
        return left is ObjectShape composite
            ? Members(composite, (ObjectShape)right)
            : Compare("=", Scalar(left), Scalar(right));
    }

    private static SqlExpression Members(ObjectShape left, ObjectShape right)
    {
        return left
            .Members.Zip(
                right.Members,
                (first, second) => Keyed(first.Shape, second.Shape)
            )
            .Aggregate((first, second) => Logical("AND", first, second));
    }

    private static SqlExpression Keyed(Shape left, Shape right)
    {
        if (left is ObjectShape composite)
        {
            return Members(composite, (ObjectShape)right);
        }
        SqlExpression first = Scalar(left);
        SqlExpression second = Scalar(right);
        return first.Nullable && second.Nullable
            ? Distinct(first, second, false)
            : Compare("=", first, second);
    }

    private static SqlExpression Conjoined(SqlExpression first, SqlExpression? second)
    {
        return second is null ? first : Logical("AND", first, second);
    }
}
