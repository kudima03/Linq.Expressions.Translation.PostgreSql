using System.Reflection;

namespace Linq.Expressions.Translation.PostgreSql.Tests.Markers;

// The markers exist only in expression trees: called, every one of them throws
// NotSupportedException (which tells a provider that evaluates closed subtrees to
// keep the call), and the translator never evaluates one while it folds the closed
// subtrees of a query.
public sealed record MarkerTests
{
    public static TheoryData<string> Methods =>
        [
            .. new[]
            {
                typeof(PostgreSqlWindows),
                typeof(PostgreSqlRecursion),
                typeof(PostgreSqlFunctions),
            }
                .SelectMany(type =>
                    type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                )
                .Select(Name),
        ];

    [Theory]
    [MemberData(nameof(Methods))]
    public void ThrowsWhenCalled(string name)
    {
        MethodInfo method = new[]
        {
            typeof(PostgreSqlWindows),
            typeof(PostgreSqlRecursion),
            typeof(PostgreSqlFunctions),
        }
            .SelectMany(type =>
                type.GetMethods(BindingFlags.Public | BindingFlags.Static)
            )
            .Single(candidate => Name(candidate) == name);
        MethodInfo closed = method.MakeGenericMethod([
            .. method.GetGenericArguments().Select(_ => typeof(int)),
        ]);
        object?[] arguments = [.. closed.GetParameters().Select(Default)];

        TargetInvocationException thrown = Assert.Throws<TargetInvocationException>(() =>
            closed.Invoke(null, arguments)
        );

        NotSupportedException inner = Assert.IsType<NotSupportedException>(
            thrown.InnerException
        );
        Assert.Contains(
            $"'{method.DeclaringType!.Name}.{method.Name}'",
            inner.Message,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void ThrowsWhenTheRowsOfAWindowAreRead()
    {
        PostgreSqlWindow<int> window =
            (PostgreSqlWindow<int>)
                Activator.CreateInstance(typeof(PostgreSqlWindow<int>), nonPublic: true)!;

        NotSupportedException thrown = Assert.Throws<NotSupportedException>(() =>
            window.Rows.Count()
        );

        Assert.Contains(
            "'PostgreSqlWindow.Rows'",
            thrown.Message,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void ThrowsWhenCalledAsCode()
    {
        _ = Assert.Throws<NotSupportedException>(() =>
            Orders.Over(null, null, null, (o, w) => w.RowNumber())
        );
        _ = Assert.Throws<NotSupportedException>(() =>
            Orders.WithRecursive(level => level)
        );
        _ = Assert.Throws<NotSupportedException>(() =>
            Orders.WithRecursive(level => level, 3)
        );
        _ = Assert.Throws<NotSupportedException>(() =>
            PostgreSqlFunctions.VarPop(Array.Empty<Order>(), o => o.Weight)
        );
    }

    [Fact]
    public void NeverEvaluatesAClosedMarker()
    {
        IQueryable<long> numbers = Trees.Over(
            Orders,
            null,
            rows => rows.OrderBy(o => o.Id),
            null,
            (o, w) => w.RowNumber()
        );
        IQueryable<Employee> reports = Trees.WithRecursive(Employees, level => level, 1);
        Expression query = Customers
            .Select(c => new
            {
                c.Id,
                Numbers = numbers.Count(),
                Reports = reports.Count(),
                Variance = PostgreSqlFunctions.VarPop(Orders, o => (double?)o.Weight),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", (
                    SELECT CAST(COUNT(*) AS integer)
                    FROM (
                        SELECT ROW_NUMBER() OVER (ORDER BY t1."Id") AS "Value"
                        FROM "Order" AS t1
                    ) AS t2
                ) AS "Numbers", (
                    SELECT CAST(COUNT(*) AS integer)
                    FROM (
                        WITH RECURSIVE t4 AS (
                            SELECT t3."Id", t3."Name", t3."ManagerId", CAST(0 AS bigint) AS "$depth"
                            FROM "Employee" AS t3
                            UNION ALL
                            SELECT t5."Id", t5."Name", t5."ManagerId", CASE WHEN t5."$depth" < 1 THEN t5."$depth" + 1 ELSE CAST((SELECT 'WITH RECURSIVE: a row deeper than the maximum depth of 1') AS bigint) END AS "$depth"
                            FROM t4 AS t5
                        )
                        SELECT t4."Id", t4."Name", t4."ManagerId"
                        FROM t4
                        WHERE (SELECT COUNT(*) FROM t4) > 0
                    ) AS t6
                ) AS "Reports", (SELECT VAR_POP(t7."Weight") FROM "Order" AS t7) AS "Variance"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    // The class and the signature, which tell the overloads apart.
    private static string Name(MethodInfo method)
    {
        return method.DeclaringType!.Name + ": " + method;
    }

    private static object? Default(ParameterInfo parameter)
    {
        return parameter.ParameterType.IsValueType
            ? Activator.CreateInstance(parameter.ParameterType)
            : null;
    }
}
