using System.Linq.Expressions;
using Linq.Expressions.Translation.PostgreSql.Mapping;
using Linq.Expressions.Translation.PostgreSql.Shapes;
using Linq.Expressions.Translation.PostgreSql.Syntax;

namespace Linq.Expressions.Translation.PostgreSql.Translation;

// PostgreSqlRecursion.WithRecursive is WITH RECURSIVE name AS (seed UNION ALL step),
// read as a derived table. The step's parameter is the level before, the recursive
// query by its name, which PostgreSQL lets the step read exactly once, directly in
// FROM or an inner join, and not in a step that aggregates. The columns of the step
// are matched to those of the seed by name, which also types them: a constant of
// the seed is cast to its type, as the seed alone decides the types of the columns.
// With a depth limit, a hidden column counts the levels from 0, and the step fails
// on a row deeper than the limit by casting a message to bigint, where PostgreSQL
// has no RAISE in SQL; the cast reads a scalar subquery, which the planner does not
// fold, so it fails only for a row that reaches it.
internal sealed partial class Translator
{
    private const string Depth = "$depth";

    private readonly Dictionary<ParameterExpression, Level> _levels = [];

    private SelectQuery Recursive(MethodCallExpression call)
    {
        long? limit = call.Arguments.Count > 2 ? MaxDepth(call.Arguments[2]) : null;
        SelectQuery seed = Seeded(Query(call.Arguments[0]));
        LambdaExpression step = Lambda(call, 1);
        string name = NextAlias();
        bool[] nullability = [.. seed.Nullability()];
        int aliases = _aliases;
        SelectQuery next;
        Level level;
        // A column the step may yield NULL in is nullable in the level it reads too.
        while (true)
        {
            _aliases = aliases;
            level = new Level(name, seed.Shape, nullability);
            _levels[step.Parameters[0]] = level;
            next = Stepped(Query(step.Body), level);
            _ = _levels.Remove(step.Parameters[0]);
            bool[] stepped =
            [
                .. nullability.Zip(
                    next.Nullability(),
                    (first, second) => first || second
                ),
            ];
            if (stepped.SequenceEqual(nullability))
            {
                break;
            }
            nullability = stepped;
        }
        if (limit is long depth)
        {
            seed.Hidden.Add(
                new ProjectionColumn(
                    Depth,
                    new SqlCast(Integer(0), "bigint", typeof(long))
                )
            );
            next.Hidden.Add(new ProjectionColumn(Depth, Deepened(level, depth)));
        }
        string alias = NextAlias();
        int index = 0;
        return new SelectQuery(
            new RecursiveSource(
                name,
                seed,
                next,
                [.. seed.Shape.Columns().Select(column => column.Name!)],
                limit is not null,
                alias
            ),
            seed.Shape.Map(
                (value, column) =>
                    new SqlColumn(alias, column!, value.Type, nullability[index++]),
                null
            )
        );
    }

    private static long MaxDepth(Expression argument)
    {
        return argument is ConstantExpression { Value: long depth } && depth >= 0
            ? depth
            : throw new NotSupportedException(
                "The maximum depth of WITH RECURSIVE is a constant of 0 or more."
            );
    }

    // The seed is one SELECT with named columns, a constant typed as its column.
    private SelectQuery Seeded(SqlQuery query)
    {
        _ = Ungrouped(query.Shape);
        SelectQuery seed = Unordered(query as SelectQuery ?? Wrap(query, false));
        seed.Shape = seed.Shape is ScalarShape scalar
            ? new ScalarShape(Typed(scalar.Value), "Value")
            : seed.Shape.Map((value, _) => Typed(value), null);
        return seed;
    }

    private static SqlExpression Typed(SqlExpression value)
    {
        Type type = ScalarTypes.Underlying(value.Type);
        return value is not SqlConstant constant ? value
            : ScalarTypes.IsNumber(type)
                ? new SqlCast(constant, ScalarTypes.Number(type), value.Type)
            : constant.Value is null && Names.TryGetValue(type, out string? name)
                ? new SqlCast(constant, name, value.Type)
            : value;
    }

    private static readonly Dictionary<Type, string> Names = new()
    {
        [typeof(bool)] = "boolean",
        [typeof(char)] = "text",
        [typeof(string)] = "text",
        [typeof(Guid)] = "uuid",
        [typeof(DateTime)] = "timestamp",
        [typeof(DateTimeOffset)] = "timestamptz",
        [typeof(DateOnly)] = "date",
        [typeof(TimeOnly)] = "time",
        [typeof(TimeSpan)] = "interval",
        [typeof(byte[])] = "bytea",
    };

    // The rows of the level before, inside the step.
    private SelectQuery Leveled(Level level)
    {
        string alias = NextAlias();
        LevelSource source = new(level.Name, alias);
        level.Sources.Add(source);
        int index = 0;
        return new SelectQuery(
            source,
            level.Shape.Map(
                (value, column) =>
                    new SqlColumn(alias, column!, value.Type, level.Nullability[index++]),
                null
            )
        );
    }

    // The step as PostgreSQL's recursive term, with the columns of the seed.
    private static SelectQuery Stepped(SqlQuery query, Level level)
    {
        if (query is not SelectQuery step || step.GroupBy is not null)
        {
            throw new NotSupportedException(
                "The step of WITH RECURSIVE is one SELECT that does not group: PostgreSQL allows no aggregate or set operation in it."
            );
        }
        if (
            level.Sources.Count != 1
            || (
                step.From != level.Sources[0]
                && !step.Joins.Any(join =>
                    join.Source == level.Sources[0] && join.Kind != "LEFT JOIN"
                )
            )
        )
        {
            throw new NotSupportedException(
                "The step of WITH RECURSIVE reads the level before exactly once, in FROM or an inner join: not in a subquery, a derived table or on the optional side of an outer join."
            );
        }
        _ = Unordered(step);
        List<ProjectionColumn> columns = step.Shape.Columns();
        step.Shape =
            level.Shape is ScalarShape
                ? new ScalarShape(Scalar(step.Shape), "Value")
                : level.Shape.Map((_, name) => Named(columns, name!), null);
        return columns.Count == level.Shape.Columns().Count
            ? step
            : throw new NotSupportedException(
                "The step of WITH RECURSIVE yields the columns of the seed, and no others."
            );
    }

    // A column of the step by the name of the seed's: PostgreSQL's quoted names are
    // case-sensitive, so the exact name first, and only then the one that differs in
    // case, as a constructor parameter does from its member (item1 sets Item1).
    private static SqlExpression Named(List<ProjectionColumn> columns, string name)
    {
        ProjectionColumn? column =
            columns.FirstOrDefault(column => column.Name == name)
            ?? columns.FirstOrDefault(column =>
                string.Equals(column.Name, name, StringComparison.OrdinalIgnoreCase)
            );
        return column?.Value
            ?? throw new NotSupportedException(
                $"The step of WITH RECURSIVE yields no column '{name}' of the seed."
            );
    }

    // The depth of a row of the step, one more than that of the row it was read from,
    // or a failure beyond the limit.
    private static SqlCase Deepened(Level level, long limit)
    {
        SqlColumn depth = new(level.Sources[0].Alias, Depth, typeof(long), false);
        return new SqlCase(
            [
                new SqlWhen(
                    Compare("<", depth, new SqlConstant(limit, typeof(long))),
                    Calculate("+", depth, Integer(1))
                ),
            ],
            new SqlCast(
                new SqlFragment(
                    "(SELECT "
                        + SqlLiteral.Render(
                            $"WITH RECURSIVE: a row deeper than the maximum depth of {limit}"
                        )
                        + ")",
                    typeof(string)
                ),
                "bigint",
                typeof(long)
            ),
            typeof(long)
        );
    }

    private sealed class Level(string name, Shape shape, IReadOnlyList<bool> nullability)
    {
        public string Name { get; } = name;

        public Shape Shape { get; } = shape;

        public IReadOnlyList<bool> Nullability { get; } = nullability;

        public List<LevelSource> Sources { get; } = [];
    }
}
