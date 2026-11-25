using System.Reflection;

namespace Linq.Expressions.Translation.PostgreSql.Tests.Markers;

// What SQL cannot say, or PostgreSQL rejects, is refused with NotSupportedException
// rather than translated into a statement that fails or means something else.
public sealed record UnsupportedMarkerTests
{
    public static TheoryData<Func<Expression>, string> Refused =>
        new()
        {
            // A window function belongs to the projection of its own Over.
            {
                () =>
                    Trees
                        .Over(
                            Orders,
                            null,
                            rows => rows.OrderBy(o => o.Id),
                            null,
                            (o, w) => Orders.Count(x => x.Id < w.RowNumber())
                        )
                        .Expression,
                "is computed in the selector of its Over"
            },
            // The lambdas over the rows of a window read only their own row.
            {
                () =>
                    Trees
                        .Over(
                            Orders,
                            null,
                            null,
                            null,
                            (o, w) => w.Rows.Count(x => x.Total > o.Total)
                        )
                        .Expression,
                "reads only its own row"
            },
            {
                () =>
                    Trees
                        .Over(
                            Orders,
                            null,
                            rows => rows.OrderBy(o => o.Id),
                            null,
                            (o, w) => w.Lag(x => x.Total - o.Total, 1, 0m)
                        )
                        .Expression,
                "reads only its own row"
            },
            // PostgreSQL has no DISTINCT in a window aggregate.
            {
                () =>
                    Trees
                        .Over(
                            Orders,
                            null,
                            null,
                            null,
                            (o, w) => w.Rows.Select(x => x.Quantity).Distinct().Count()
                        )
                        .Expression,
                "Enumerable.Distinct"
            },
            // Neither a window nor the rows of its frame are values.
            {
                () => Trees.Over(Orders, null, null, null, (o, w) => w).Expression,
                "cannot be projected"
            },
            {
                () =>
                    Trees
                        .Over(Orders, null, null, null, (o, w) => new { o.Id, w.Rows })
                        .Expression,
                "cannot be projected"
            },
            // A function that may find no row cannot be a non-nullable value.
            {
                () =>
                    Trees
                        .Over(
                            Orders,
                            null,
                            rows => rows.OrderBy(o => o.Id),
                            PostgreSqlFrame.Rows(1, 2),
                            (o, w) => w.FirstValue(x => x.Id)
                        )
                        .Expression,
                "may find no row"
            },
            {
                () =>
                    Trees
                        .Over(
                            Orders,
                            null,
                            rows => rows.OrderBy(o => o.Id),
                            PostgreSqlFrame.Rows(null, -1),
                            (o, w) => w.LastValue(x => x.Total)
                        )
                        .Expression,
                "may find no row"
            },
            // The order of a window is OrderBy of its rows, then ThenBy.
            {
                () =>
                    Trees
                        .Over(
                            Orders,
                            null,
                            rows => rows.Where(o => o.IsPaid).OrderBy(o => o.Id),
                            null,
                            (o, w) => w.RowNumber()
                        )
                        .Expression,
                "The order of a window"
            },
            {
                () =>
                    Trees
                        .Over(
                            Orders,
                            null,
                            rows => rows.OrderBy(o => o.Id).OrderBy(o => o.Total),
                            null,
                            (o, w) => w.RowNumber()
                        )
                        .Expression,
                "The order of a window"
            },
            {
                () =>
                    Trees
                        .Over(
                            Customers,
                            null,
                            rows =>
                                rows.OrderBy(
                                    c => c.Name,
                                    StringComparer.OrdinalIgnoreCase
                                ),
                            null,
                            (c, w) => w.RowNumber()
                        )
                        .Expression,
                "Enumerable.OrderBy"
            },
            {
                () =>
                    Trees
                        .Over(
                            Customers,
                            null,
                            rows => rows.Order(Comparer<Customer>.Default),
                            null,
                            (c, w) => w.RowNumber()
                        )
                        .Expression,
                "Enumerable.Order"
            },
            // A RANGE offset needs one key of a type it can be added to.
            {
                () =>
                    Trees
                        .Over(
                            Orders,
                            null,
                            null,
                            PostgreSqlFrame.Range(-1, 0),
                            (o, w) => w.Rows.Count()
                        )
                        .Expression,
                "exactly one key"
            },
            {
                () =>
                    Trees
                        .Over(
                            Orders,
                            null,
                            rows => rows.OrderBy(o => o.Quantity).ThenBy(o => o.Id),
                            PostgreSqlFrame.Range(-1, 0),
                            (o, w) => w.Rows.Count()
                        )
                        .Expression,
                "exactly one key"
            },
            {
                () =>
                    Trees
                        .Over(
                            Orders,
                            null,
                            rows => rows.OrderBy(o => o.Number),
                            PostgreSqlFrame.Range(-1, 0),
                            (o, w) => w.Rows.Count()
                        )
                        .Expression,
                "exactly one key"
            },
            {
                () =>
                    Trees
                        .Over(
                            Orders,
                            null,
                            rows => rows.OrderBy(o => o.Quantity),
                            PostgreSqlFrame.RangeMonths(-1, 0),
                            (o, w) => w.Rows.Count()
                        )
                        .Expression,
                "exactly one key"
            },
            {
                () =>
                    Trees
                        .Over(
                            Orders,
                            null,
                            rows => rows.OrderBy(o => o.CreatedAt),
                            PostgreSqlFrame.Range(-1, 0),
                            (o, w) => w.Rows.Count()
                        )
                        .Expression,
                "exactly one key"
            },
            // The step of WITH RECURSIVE follows PostgreSQL's rules for the recursive term.
            {
                () =>
                    Trees
                        .WithRecursive(
                            Employees,
                            level => level.Join(level, a => a.Id, b => b.Id, (a, b) => a)
                        )
                        .Expression,
                "exactly once"
            },
            {
                () =>
                    Trees
                        .WithRecursive(
                            Employees,
                            level =>
                                Employees.Where(e => level.Any(m => m.Id == e.ManagerId))
                        )
                        .Expression,
                "exactly once"
            },
            {
                () =>
                    Trees
                        .WithRecursive(
                            Employees.Where(e => e.ManagerId == null),
                            level =>
                                level
                                    .OrderBy(m => m.Id)
                                    .Take(1)
                                    .Join(
                                        Employees,
                                        m => m.Id,
                                        e => e.ManagerId,
                                        (m, e) => e
                                    )
                        )
                        .Expression,
                "exactly once"
            },
            {
                () =>
                    Trees
                        .WithRecursive(
                            Employees.Where(e => e.ManagerId == null),
                            level =>
                                Employees
                                    .GroupJoin(
                                        level,
                                        e => e.ManagerId,
                                        m => m.Id,
                                        (e, ms) => new { e, ms }
                                    )
                                    .SelectMany(x => x.ms.DefaultIfEmpty(), (x, m) => x.e)
                        )
                        .Expression,
                "exactly once"
            },
            {
                () => Trees.WithRecursive(Employees, level => Employees).Expression,
                "exactly once"
            },
            {
                () =>
                    Trees
                        .WithRecursive(
                            Employees.Select(e => e.Id),
                            level => level.GroupBy(id => id % 2).Select(g => g.Key)
                        )
                        .Expression,
                "does not group"
            },
            {
                () =>
                    Trees
                        .WithRecursive(Employees, level => level.Concat(level))
                        .Expression,
                "does not group"
            },
            {
                () =>
                    Trees
                        .WithRecursive(
                            Employees.Select(e => new Named { Id = e.Id, Name = e.Name }),
                            level => level.Select(x => new Named { Id = x.Id })
                        )
                        .Expression,
                "yields no column 'Name' of the seed"
            },
            {
                () =>
                    Trees
                        .WithRecursive(
                            Employees.Select(e => new Named { Id = e.Id, Name = e.Name }),
                            level =>
                                level.Select(x => new Named
                                {
                                    Id = x.Id,
                                    Name = x.Name,
                                    Extra = 1,
                                })
                        )
                        .Expression,
                "and no others"
            },
            {
                () =>
                    Trees
                        .WithRecursive(
                            Employees.GroupBy(e => e.ManagerId),
                            level => level
                        )
                        .Expression,
                "IGrouping"
            },
        };

    [Theory]
    [MemberData(nameof(Refused))]
    public void RefusesWhatPostgreSqlCannotSay(Func<Expression> query, string reason)
    {
        NotSupportedException thrown = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query()).TextValue
        );

        Assert.Contains(reason, thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RefusesAFrameComputedForEachRow()
    {
        MethodInfo over = typeof(PostgreSqlWindows)
            .GetMethod(nameof(PostgreSqlWindows.Over))!
            .MakeGenericMethod(typeof(Order), typeof(long));
        ParameterExpression customer = Expression.Parameter(typeof(Customer), "c");
        Expression<Func<Order, PostgreSqlWindow<Order>, long>> selector = (o, w) =>
            w.RowNumber();
        Expression framed = Expression.Call(
            over,
            Orders.Expression,
            Expression.Constant(null, typeof(Expression<Func<Order, object?>>)),
            Expression.Constant(
                null,
                typeof(Expression<Func<IEnumerable<Order>, IOrderedEnumerable<Order>>>)
            ),
            Expression.Condition(
                Expression.GreaterThan(
                    Expression.Property(customer, nameof(Customer.Id)),
                    Expression.Constant(0)
                ),
                Expression.Constant(PostgreSqlFrame.Rows(null, 0)),
                Expression.Constant(null, typeof(PostgreSqlFrame))
            ),
            Expression.Quote(selector)
        );
        Expression query = Expression.Call(
            typeof(Queryable),
            nameof(Queryable.Select),
            [typeof(Customer), typeof(int)],
            Customers.Expression,
            Expression.Quote(
                Expression.Lambda(
                    Expression.Call(
                        typeof(Queryable),
                        nameof(Queryable.Count),
                        [typeof(long)],
                        framed
                    ),
                    customer
                )
            )
        );

        NotSupportedException thrown = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );

        Assert.Contains("must be a constant", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RefusesANegativeOrComputedDepth()
    {
        MethodInfo recursive = typeof(PostgreSqlRecursion)
            .GetMethods()
            .Single(method => method.GetParameters().Length == 3)
            .MakeGenericMethod(typeof(Employee));
        Expression<Func<IQueryable<Employee>, IQueryable<Employee>>> step = level =>
            level;

        NotSupportedException negative = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(
                Expression.Call(
                    recursive,
                    Employees.Expression,
                    Expression.Quote(step),
                    Expression.Constant(-1L)
                )
            ).TextValue
        );

        Assert.Contains(
            "a constant of 0 or more",
            negative.Message,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void RefusesAMarkerQueryAsAValue()
    {
        Expression query = Customers
            .Select(c => Orders.Over(null, null, null, (o, w) => w.RowNumber()))
            .Expression;

        _ = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
    }

    public sealed record Named
    {
        public int Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public int Extra { get; init; }
    }
}
