using System.Reflection;

namespace Linq.Expressions.Translation.PostgreSql.Tests.Markers;

// Edge cases of PostgreSqlRecursion.WithRecursive: steps PostgreSQL allows in the
// recursive term (DISTINCT, LIMIT, OFFSET, window functions, the level preserved by
// an outer join or crossed with a table), steps it refuses (the level read in a
// subquery, a derived table, a lateral subquery or a nested recursion), depth limits
// that are captured or converted constants, the types of NULLs and constants of the
// seed, and recursions read twice or nested in one statement.
public sealed record RecursionEdgeCaseTests
{
    private static readonly Expression<
        Func<IQueryable<Employee>, IQueryable<Employee>>
    > Reports = level => level.Join(Employees, m => m.Id, e => e.ManagerId, (m, e) => e);

    public static TheoryData<Func<Expression>> Refused =>
        [
            // The level read again in a subquery of a filter of the level.
            () =>
                Trees
                    .WithRecursive(
                        Employees,
                        level => level.Where(a => level.Any(b => b.Id == a.ManagerId))
                    )
                    .Expression,
            // The level read again in a scalar subquery.
            () =>
                Trees
                    .WithRecursive(
                        Employees,
                        level => level.Where(a => a.Id < level.Max(b => b.Id))
                    )
                    .Expression,
            // The level read only in IN (subquery).
            () =>
                Trees
                    .WithRecursive(
                        Employees.Where(e => e.ManagerId == null),
                        level =>
                            Employees.Where(a =>
                                level.Select(b => (int?)b.Id).Contains(a.ManagerId)
                            )
                    )
                    .Expression,
            // The level read only in a lateral subquery.
            () =>
                Trees
                    .WithRecursive(
                        Employees.Where(e => e.ManagerId == null),
                        level =>
                            Employees.SelectMany(
                                e => level.Where(m => m.Id == e.ManagerId).Take(1),
                                (e, m) => e
                            )
                    )
                    .Expression,
            // The level and a table combined by a set operation.
            () =>
                Trees
                    .WithRecursive(
                        Employees.Select(e => e.Id),
                        level =>
                            level
                                .Where(n => n < 3)
                                .Select(n => n + 1)
                                .Concat(Employees.Select(e => e.Id))
                    )
                    .Expression,
            // An aggregate of the level in the step.
            () =>
                Trees
                    .WithRecursive(
                        Employees.Select(e => e.Id),
                        level => level.Where(n => n < 3).Select(n => level.Count())
                    )
                    .Expression,
            // The distinct level filtered: a derived table.
            () =>
                Trees
                    .WithRecursive(
                        Employees.Select(e => e.Id),
                        level => level.Distinct().Where(n => n < 3).Select(n => n + 1)
                    )
                    .Expression,
            // The first rows of the level projected and filtered: a derived table.
            () =>
                Trees
                    .WithRecursive(
                        Employees.Select(e => e.Id),
                        level =>
                            level
                                .OrderBy(n => n)
                                .Take(2)
                                .Select(n => n + 1)
                                .Where(n => n < 4)
                    )
                    .Expression,
            // The first row of each key of the level: DISTINCT ON in a derived table.
            () =>
                Trees
                    .WithRecursive(
                        Employees.Where(e => e.ManagerId == null),
                        level =>
                            level
                                .Join(Employees, m => m.Id, e => e.ManagerId, (m, e) => e)
                                .OrderBy(e => e.Id)
                                .DistinctBy(e => e.ManagerId)
                    )
                    .Expression,
            // A window over the level filtered: a derived table.
            () =>
                Trees
                    .WithRecursive(
                        Employees
                            .Where(e => e.ManagerId == null)
                            .Select(e => new Pair { Id = e.Id, Manager = 0 }),
                        level =>
                            level
                                .Over(
                                    null,
                                    rows => rows.OrderBy(p => p.Id),
                                    null,
                                    (p, w) =>
                                        new Pair
                                        {
                                            Id = p.Id + 1,
                                            Manager = (int?)w.RowNumber(),
                                        }
                                )
                                .Where(p => p.Id < 3)
                    )
                    .Expression,
            // The level read in the seed of a recursion nested in the step.
            () =>
                Trees
                    .WithRecursive(
                        Employees.Where(e => e.ManagerId == null),
                        level =>
                            level
                                .Where(m => m.Id < 3)
                                .WithRecursive(inner => inner.Where(m => m.Id < 0))
                    )
                    .Expression,
            // The level read in the step of a recursion nested in the step.
            () =>
                Trees
                    .WithRecursive(
                        Employees.Where(e => e.ManagerId == null),
                        level =>
                            level.Join(
                                Employees
                                    .Where(e => e.Id == 1)
                                    .WithRecursive(inner =>
                                        inner.Join(
                                            level,
                                            a => a.Id,
                                            b => b.Id,
                                            (a, b) => b
                                        )
                                    ),
                                a => a.Id,
                                b => b.ManagerId,
                                (a, b) => b
                            )
                    )
                    .Expression,
            // A step that yields only the constants of a table: no level at all.
            () =>
                Trees
                    .WithRecursive(Employees, level => Employees.Where(e => e.Id > 3))
                    .Expression,
        ];

    [Theory]
    [MemberData(nameof(Refused))]
    public void RefusesAStepPostgreSqlRejects(Func<Expression> query)
    {
        _ = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query()).TextValue
        );
    }

    [Fact]
    public void RefusesAMaximumDepthReadFromARow()
    {
        Expression query = Customers
            .Select(c =>
                Employees.WithRecursive(level => level.Where(e => e.Id < 0), c.Id).Count()
            )
            .Expression;

        _ = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ReadsACapturedMaximumDepth()
    {
        long depth = 3;
        int narrow = 3;
        Expression captured = Customers
            .Select(c =>
                Employees
                    .WithRecursive(level => level.Where(e => e.Id < 0), depth)
                    .Count()
            )
            .Expression;
        Expression converted = Customers
            .Select(c =>
                Employees
                    .WithRecursive(level => level.Where(e => e.Id < 0), narrow)
                    .Count()
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT (SELECT CAST(COUNT(*) AS integer) FROM (WITH RECURSIVE t2 AS (SELECT t1."Id", t1."Name", t1."ManagerId", CAST(0 AS bigint) AS "$depth" FROM "Employee" AS t1 UNION ALL SELECT t3."Id", t3."Name", t3."ManagerId", CASE WHEN t3."$depth" < 3 THEN t3."$depth" + 1 ELSE CAST((SELECT 'WITH RECURSIVE: a row deeper than the maximum depth of 3') AS bigint) END AS "$depth" FROM t2 AS t3 WHERE t3."Id" < 0) SELECT t2."Id", t2."Name", t2."ManagerId" FROM t2 WHERE (SELECT COUNT(*) FROM t2) > 0) AS t4)
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(captured).TextValue
        );
        Assert.Equal(
            Sql.Valid(
                """
                SELECT (SELECT CAST(COUNT(*) AS integer) FROM (WITH RECURSIVE t2 AS (SELECT t1."Id", t1."Name", t1."ManagerId", CAST(0 AS bigint) AS "$depth" FROM "Employee" AS t1 UNION ALL SELECT t3."Id", t3."Name", t3."ManagerId", CASE WHEN t3."$depth" < 3 THEN t3."$depth" + 1 ELSE CAST((SELECT 'WITH RECURSIVE: a row deeper than the maximum depth of 3') AS bigint) END AS "$depth" FROM t2 AS t3 WHERE t3."Id" < 0) SELECT t2."Id", t2."Name", t2."ManagerId" FROM t2 WHERE (SELECT COUNT(*) FROM t2) > 0) AS t4)
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(converted).TextValue
        );
    }

    [Fact]
    public void ReadsAConvertedConstantMaximumDepth()
    {
        MethodInfo recursive = typeof(PostgreSqlRecursion)
            .GetMethods()
            .Single(method => method.GetParameters().Length == 3)
            .MakeGenericMethod(typeof(Employee));
        Expression<Func<IQueryable<Employee>, IQueryable<Employee>>> step = level =>
            level.Where(e => e.Id < 0);
        Expression query = Expression.Call(
            recursive,
            Employees.Expression,
            Expression.Quote(step),
            Expression.Convert(Expression.Constant(3), typeof(long))
        );

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t3."Id", t3."Name", t3."ManagerId"
                FROM (
                    WITH RECURSIVE t1 AS (
                        SELECT t0."Id", t0."Name", t0."ManagerId", CAST(0 AS bigint) AS "$depth"
                        FROM "Employee" AS t0
                        UNION ALL
                        SELECT t2."Id", t2."Name", t2."ManagerId", CASE WHEN t2."$depth" < 3 THEN t2."$depth" + 1 ELSE CAST((SELECT 'WITH RECURSIVE: a row deeper than the maximum depth of 3') AS bigint) END AS "$depth"
                        FROM t1 AS t2
                        WHERE t2."Id" < 0
                    )
                    SELECT t1."Id", t1."Name", t1."ManagerId"
                    FROM t1
                    WHERE (SELECT COUNT(*) FROM t1) > 0
                ) AS t3
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void LimitsTheDepthToTheLargestBigint()
    {
        Expression query = Trees
            .WithRecursive(
                Employees.Where(e => e.ManagerId == null),
                Reports,
                long.MaxValue
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t4."Id", t4."Name", t4."ManagerId"
                FROM (
                    WITH RECURSIVE t1 AS (
                        SELECT t0."Id", t0."Name", t0."ManagerId", CAST(0 AS bigint) AS "$depth"
                        FROM "Employee" AS t0
                        WHERE t0."ManagerId" IS NULL
                        UNION ALL
                        SELECT t3."Id", t3."Name", t3."ManagerId", CASE WHEN t2."$depth" < 9223372036854775807 THEN t2."$depth" + 1 ELSE CAST((SELECT 'WITH RECURSIVE: a row deeper than the maximum depth of 9223372036854775807') AS bigint) END AS "$depth"
                        FROM t1 AS t2
                        INNER JOIN "Employee" AS t3 ON t2."Id" = t3."ManagerId"
                    )
                    SELECT t1."Id", t1."Name", t1."ManagerId"
                    FROM t1
                    WHERE (SELECT COUNT(*) FROM t1) > 0
                ) AS t4
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ReadsTheLevelOnThePreservedSideOfALeftJoin()
    {
        Expression query = Trees
            .WithRecursive(
                Employees.Where(e => e.ManagerId == null),
                level =>
                    level
                        .GroupJoin(
                            Employees,
                            m => m.Id,
                            e => e.ManagerId,
                            (m, es) => new { m, es }
                        )
                        .SelectMany(x => x.es.DefaultIfEmpty(), (x, e) => x.m)
                        .Where(m => m.Id < 0)
            )
            .Expression;
        Expression flattened = Trees
            .WithRecursive(
                Employees.Where(e => e.ManagerId == null),
                level =>
                    level
                        .SelectMany(
                            m =>
                                Employees
                                    .Where(e => e.ManagerId == m.Id)
                                    .DefaultIfEmpty(),
                            (m, e) => m
                        )
                        .Where(m => m.Id < 0)
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t4."Id", t4."Name", t4."ManagerId"
                FROM (
                    WITH RECURSIVE t1 AS (
                        SELECT t0."Id", t0."Name", t0."ManagerId"
                        FROM "Employee" AS t0
                        WHERE t0."ManagerId" IS NULL
                        UNION ALL
                        SELECT t2."Id", t2."Name", t2."ManagerId"
                        FROM t1 AS t2
                        LEFT JOIN "Employee" AS t3 ON t2."Id" = t3."ManagerId"
                        WHERE t2."Id" < 0
                    )
                    SELECT t1."Id", t1."Name", t1."ManagerId"
                    FROM t1
                ) AS t4
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Equal(
            Sql.Valid(
                """
                SELECT t4."Id", t4."Name", t4."ManagerId"
                FROM (
                    WITH RECURSIVE t1 AS (
                        SELECT t0."Id", t0."Name", t0."ManagerId"
                        FROM "Employee" AS t0
                        WHERE t0."ManagerId" IS NULL
                        UNION ALL
                        SELECT t2."Id", t2."Name", t2."ManagerId"
                        FROM t1 AS t2
                        LEFT JOIN "Employee" AS t3 ON t3."ManagerId" = t2."Id"
                        WHERE t2."Id" < 0
                    )
                    SELECT t1."Id", t1."Name", t1."ManagerId"
                    FROM t1
                ) AS t4
                """
            ),
            new PostgreSqlQuery(flattened).TextValue
        );
    }

    [Fact]
    public void CrossesATableWithTheLevel()
    {
        Expression query = Trees
            .WithRecursive(
                Employees.Where(e => e.Id == 1).Select(e => e.Id),
                level =>
                    Customers
                        .Where(c => c.Id < 3)
                        .SelectMany(c => level, (c, n) => n + c.Id)
                        .Where(n => n < 5)
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t4."Value"
                FROM (
                    WITH RECURSIVE t1 AS (
                        SELECT t0."Id" AS "Value"
                        FROM "Employee" AS t0
                        WHERE t0."Id" = 1
                        UNION ALL
                        SELECT t3."Value" + t2."Id" AS "Value"
                        FROM "Customer" AS t2
                        CROSS JOIN t1 AS t3
                        WHERE (t2."Id" < 3) AND ((t3."Value" + t2."Id") < 5)
                    )
                    SELECT t1."Value"
                    FROM t1
                ) AS t4
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ProjectsTheLevelBeforeJoiningIt()
    {
        Expression query = Trees
            .WithRecursive(
                Employees
                    .Where(e => e.ManagerId == null)
                    .Select(e => new Walk(e.Id, 0, e.Name)),
                level =>
                    level
                        .Select(w => new
                        {
                            w.Id,
                            Next = w.Depth + 1,
                            w.Path,
                        })
                        .Where(w => w.Next < 3)
                        .Join(
                            Employees,
                            w => w.Id,
                            e => e.ManagerId,
                            (w, e) => new Walk(e.Id, w.Next, w.Path + "/" + e.Name)
                        )
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t4."Id", t4."Depth", t4."Path"
                FROM (
                    WITH RECURSIVE t1 AS (
                        SELECT t0."Id", CAST(0 AS integer) AS "Depth", t0."Name" AS "Path"
                        FROM "Employee" AS t0
                        WHERE t0."ManagerId" IS NULL
                        UNION ALL
                        SELECT t3."Id", t2."Depth" + 1 AS "Depth", t2."Path" || '/' || t3."Name" AS "Path"
                        FROM t1 AS t2
                        INNER JOIN "Employee" AS t3 ON t2."Id" = t3."ManagerId"
                        WHERE (t2."Depth" + 1) < 3
                    )
                    SELECT t1."Id", t1."Depth", t1."Path"
                    FROM t1
                ) AS t4
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void LimitsAndSkipsEachLevel()
    {
        Expression limited = Trees
            .WithRecursive(
                Employees.Select(e => e.Id),
                level =>
                    level.Where(n => n < 3).Select(n => n + 1).OrderBy(n => n).Take(2),
                3
            )
            .Expression;
        Expression skipped = Trees
            .WithRecursive(
                Employees.Select(e => e.Id),
                level =>
                    level.Where(n => n < 4).OrderBy(n => n).Skip(1).Select(n => n + 1)
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t3."Value"
                FROM (
                    WITH RECURSIVE t1 AS (
                        SELECT t0."Id" AS "Value", CAST(0 AS bigint) AS "$depth"
                        FROM "Employee" AS t0
                        UNION ALL
                        (SELECT t2."Value" + 1 AS "Value", CASE WHEN t2."$depth" < 3 THEN t2."$depth" + 1 ELSE CAST((SELECT 'WITH RECURSIVE: a row deeper than the maximum depth of 3') AS bigint) END AS "$depth" FROM t1 AS t2 WHERE t2."Value" < 3 ORDER BY t2."Value" + 1 LIMIT 2)
                    )
                    SELECT t1."Value"
                    FROM t1
                    WHERE (SELECT COUNT(*) FROM t1) > 0
                ) AS t3
                """
            ),
            new PostgreSqlQuery(limited).TextValue
        );
        Assert.Equal(
            Sql.Valid(
                """
                SELECT t3."Value"
                FROM (
                    WITH RECURSIVE t1 AS (
                        SELECT t0."Id" AS "Value"
                        FROM "Employee" AS t0
                        UNION ALL
                        (SELECT t2."Value" + 1 AS "Value" FROM t1 AS t2 WHERE t2."Value" < 4 ORDER BY t2."Value" OFFSET 1)
                    )
                    SELECT t1."Value"
                    FROM t1
                ) AS t3
                """
            ),
            new PostgreSqlQuery(skipped).TextValue
        );
    }

    [Fact]
    public void DeduplicatesEachLevel()
    {
        Expression query = Trees
            .WithRecursive(
                Employees.Select(e => e.Id),
                level => level.Where(n => n < 3).Select(n => n + 1).Distinct(),
                3
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t3."Value"
                FROM (
                    WITH RECURSIVE t1 AS (
                        SELECT t0."Id" AS "Value", CAST(0 AS bigint) AS "$depth"
                        FROM "Employee" AS t0
                        UNION ALL
                        SELECT DISTINCT t2."Value" + 1 AS "Value", CASE WHEN t2."$depth" < 3 THEN t2."$depth" + 1 ELSE CAST((SELECT 'WITH RECURSIVE: a row deeper than the maximum depth of 3') AS bigint) END AS "$depth"
                        FROM t1 AS t2
                        WHERE t2."Value" < 3
                    )
                    SELECT t1."Value"
                    FROM t1
                    WHERE (SELECT COUNT(*) FROM t1) > 0
                ) AS t3
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void DropsTheOrderOfTheSeedAndOfTheStep()
    {
        Expression query = Trees
            .WithRecursive(
                Employees.OrderBy(e => e.Name).Select(e => e.Id),
                level => level.Where(n => n < 3).Select(n => n + 1).OrderBy(n => n)
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t3."Value"
                FROM (
                    WITH RECURSIVE t1 AS (
                        SELECT t0."Id" AS "Value"
                        FROM "Employee" AS t0
                        UNION ALL
                        SELECT t2."Value" + 1 AS "Value"
                        FROM t1 AS t2
                        WHERE t2."Value" < 3
                    )
                    SELECT t1."Value"
                    FROM t1
                ) AS t3
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void NumbersEachLevelWithAWindow()
    {
        Expression query = Trees
            .WithRecursive(
                Employees
                    .Where(e => e.ManagerId == null)
                    .Select(e => new Pair { Id = e.Id, Manager = 0 }),
                level =>
                    level
                        .Where(p => p.Id < 3)
                        .Over(
                            null,
                            rows => rows.OrderBy(p => p.Id),
                            null,
                            (p, w) =>
                                new Pair { Id = p.Id + 1, Manager = (int?)w.RowNumber() }
                        )
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t3."Id", t3."Manager"
                FROM (
                    WITH RECURSIVE t1 AS (
                        SELECT t0."Id", CAST(0 AS integer) AS "Manager"
                        FROM "Employee" AS t0
                        WHERE t0."ManagerId" IS NULL
                        UNION ALL
                        SELECT t2."Id" + 1 AS "Id", CAST(ROW_NUMBER() OVER (ORDER BY t2."Id") AS integer) AS "Manager"
                        FROM t1 AS t2
                        WHERE t2."Id" < 3
                    )
                    SELECT t1."Id", t1."Manager"
                    FROM t1
                ) AS t3
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CountsEachLevelAsAWindowOverIt()
    {
        Expression query = Trees
            .WithRecursive(
                Employees.Select(e => new Pair { Id = e.Id, Manager = 0 }),
                level =>
                    level
                        .Where(p => p.Id < 4)
                        .Select(p => new Pair
                        {
                            Id = p.Id + 1,
                            Manager = level.Where(q => q.Id < 4).Count(),
                        })
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t3."Id", t3."Manager"
                FROM (
                    WITH RECURSIVE t1 AS (
                        SELECT t0."Id", CAST(0 AS integer) AS "Manager"
                        FROM "Employee" AS t0
                        UNION ALL
                        SELECT t2."Id" + 1 AS "Id", CAST(COUNT(*) OVER () AS integer) AS "Manager"
                        FROM t1 AS t2
                        WHERE t2."Id" < 4
                    )
                    SELECT t1."Id", t1."Manager"
                    FROM t1
                ) AS t3
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TypesTheNullsOfTheSeed()
    {
        Expression query = Trees
            .WithRecursive(
                Employees.Select(e => new Typed
                {
                    Id = e.Id,
                    Text = null,
                    Amount = null,
                    Key = null,
                    Moment = null,
                    Span = null,
                    Status = null,
                    Flag = null,
                    Bytes = null,
                    Small = null,
                    Single = null,
                }),
                level =>
                    level
                        .Where(t => t.Id < 3)
                        .Select(t => new Typed
                        {
                            Id = t.Id + 1,
                            Text = t.Text,
                            Amount = t.Amount,
                            Key = t.Key,
                            Moment = t.Moment,
                            Span = t.Span,
                            Status = t.Status,
                            Flag = t.Flag,
                            Bytes = t.Bytes,
                            Small = t.Small,
                            Single = t.Single,
                        })
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t3."Id", t3."Text", t3."Amount", t3."Key", t3."Moment", t3."Span", t3."Status", t3."Flag", t3."Bytes", t3."Small", t3."Single"
                FROM (
                    WITH RECURSIVE t1 AS (
                        SELECT t0."Id", CAST(NULL AS text) AS "Text", CAST(NULL AS numeric) AS "Amount", CAST(NULL AS uuid) AS "Key", CAST(NULL AS timestamptz) AS "Moment", CAST(NULL AS interval) AS "Span", CAST(NULL AS integer) AS "Status", CAST(NULL AS boolean) AS "Flag", CAST(NULL AS bytea) AS "Bytes", CAST(NULL AS smallint) AS "Small", CAST(NULL AS real) AS "Single"
                        FROM "Employee" AS t0
                        UNION ALL
                        SELECT t2."Id" + 1 AS "Id", t2."Text", t2."Amount", t2."Key", t2."Moment", t2."Span", t2."Status", t2."Flag", t2."Bytes", t2."Small", t2."Single"
                        FROM t1 AS t2
                        WHERE t2."Id" < 3
                    )
                    SELECT t1."Id", t1."Text", t1."Amount", t1."Key", t1."Moment", t1."Span", t1."Status", t1."Flag", t1."Bytes", t1."Small", t1."Single"
                    FROM t1
                ) AS t3
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void KeepsTheSelfTypedConstantsOfTheSeed()
    {
        Expression query = Trees
            .WithRecursive(
                Employees.Select(e => new Typed
                {
                    Id = e.Id,
                    Text = "x",
                    Amount = 2.5m,
                    Key = Guid.Empty,
                    Moment = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
                    Span = TimeSpan.FromMinutes(5),
                    Status = OrderStatus.Paid,
                    Flag = false,
                    Bytes = new byte[] { 1 },
                    Small = 2,
                    Single = 1.5f,
                }),
                level =>
                    level
                        .Where(t => t.Id < 3)
                        .Select(t => new Typed
                        {
                            Id = t.Id + 1,
                            Text = t.Text,
                            Amount = t.Amount,
                            Key = t.Key,
                            Moment = t.Moment,
                            Span = t.Span,
                            Status = t.Status,
                            Flag = t.Flag,
                            Bytes = t.Bytes,
                            Small = t.Small,
                            Single = t.Single,
                        })
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t3."Id", t3."Text", t3."Amount", t3."Key", t3."Moment", t3."Span", t3."Status", t3."Flag", t3."Bytes", t3."Small", t3."Single"
                FROM (
                    WITH RECURSIVE t1 AS (
                        SELECT t0."Id", 'x' AS "Text", CAST(2.5 AS numeric) AS "Amount", UUID '00000000-0000-0000-0000-000000000000' AS "Key", TIMESTAMPTZ '2024-01-01 00:00:00+00' AS "Moment", INTERVAL 'PT5M' AS "Span", CAST(1 AS integer) AS "Status", FALSE AS "Flag", BYTEA '\x01' AS "Bytes", CAST(2 AS smallint) AS "Small", CAST(1.5 AS real) AS "Single"
                        FROM "Employee" AS t0
                        UNION ALL
                        SELECT t2."Id" + 1 AS "Id", t2."Text", t2."Amount", t2."Key", t2."Moment", t2."Span", t2."Status", t2."Flag", t2."Bytes", t2."Small", t2."Single"
                        FROM t1 AS t2
                        WHERE t2."Id" < 3
                    )
                    SELECT t1."Id", t1."Text", t1."Amount", t1."Key", t1."Moment", t1."Span", t1."Status", t1."Flag", t1."Bytes", t1."Small", t1."Single"
                    FROM t1
                ) AS t3
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TypesTheColumnsOfAnInMemorySeed()
    {
        Expression query = Trees
            .WithRecursive(
                new[] { (long?)null, 3L }.AsQueryable(),
                level => level.Where(n => n < 4).Select(n => n * 3_000_000_000L)
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t3."Value"
                FROM (
                    WITH RECURSIVE t1 AS (
                        SELECT t0."Value"
                        FROM (
                            VALUES (CAST(NULL AS bigint)), (3)
                        ) AS t0 ("Value")
                        UNION ALL
                        SELECT t2."Value" * 3000000000 AS "Value"
                        FROM t1 AS t2
                        WHERE t2."Value" < 4
                    )
                    SELECT t1."Value"
                    FROM t1
                ) AS t3
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ReadsTheSameRecursionTwice()
    {
        IQueryable<Employee> reports = Trees.WithRecursive(
            Employees.Where(e => e.ManagerId == null),
            Reports,
            3
        );
        Expression query = reports
            .Join(reports, a => a.Id, b => b.ManagerId, (a, b) => b.Name)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t9."Name"
                FROM (
                    WITH RECURSIVE t1 AS (
                        SELECT t0."Id", t0."Name", t0."ManagerId", CAST(0 AS bigint) AS "$depth"
                        FROM "Employee" AS t0
                        WHERE t0."ManagerId" IS NULL
                        UNION ALL
                        SELECT t3."Id", t3."Name", t3."ManagerId", CASE WHEN t2."$depth" < 3 THEN t2."$depth" + 1 ELSE CAST((SELECT 'WITH RECURSIVE: a row deeper than the maximum depth of 3') AS bigint) END AS "$depth"
                        FROM t1 AS t2
                        INNER JOIN "Employee" AS t3 ON t2."Id" = t3."ManagerId"
                    )
                    SELECT t1."Id", t1."Name", t1."ManagerId"
                    FROM t1
                    WHERE (SELECT COUNT(*) FROM t1) > 0
                ) AS t4
                INNER JOIN (
                    WITH RECURSIVE t6 AS (
                        SELECT t5."Id", t5."Name", t5."ManagerId", CAST(0 AS bigint) AS "$depth"
                        FROM "Employee" AS t5
                        WHERE t5."ManagerId" IS NULL
                        UNION ALL
                        SELECT t8."Id", t8."Name", t8."ManagerId", CASE WHEN t7."$depth" < 3 THEN t7."$depth" + 1 ELSE CAST((SELECT 'WITH RECURSIVE: a row deeper than the maximum depth of 3') AS bigint) END AS "$depth"
                        FROM t6 AS t7
                        INNER JOIN "Employee" AS t8 ON t7."Id" = t8."ManagerId"
                    )
                    SELECT t6."Id", t6."Name", t6."ManagerId"
                    FROM t6
                    WHERE (SELECT COUNT(*) FROM t6) > 0
                ) AS t9 ON t4."Id" = t9."ManagerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsARecursionInTheStepWhoseColumnBecomesNullable()
    {
        IQueryable<Employee> reports = Trees.WithRecursive(
            Employees.Where(e => e.ManagerId == null),
            Reports
        );
        Expression query = Trees
            .WithRecursive(
                Employees
                    .Where(e => e.Id == 4)
                    .Select(e => new Pair { Id = e.Id, Manager = 1 }),
                level =>
                    level.Join(
                        reports,
                        p => p.Manager,
                        e => e.Id,
                        (p, e) => new Pair { Id = e.Id, Manager = e.ManagerId }
                    )
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t8."Id", t8."Manager"
                FROM (
                    WITH RECURSIVE t1 AS (
                        SELECT t0."Id", CAST(1 AS integer) AS "Manager"
                        FROM "Employee" AS t0
                        WHERE t0."Id" = 4
                        UNION ALL
                        SELECT t7."Id", t7."ManagerId" AS "Manager"
                        FROM t1 AS t2
                        INNER JOIN (
                            WITH RECURSIVE t4 AS (
                                SELECT t3."Id", t3."Name", t3."ManagerId"
                                FROM "Employee" AS t3
                                WHERE t3."ManagerId" IS NULL
                                UNION ALL
                                SELECT t6."Id", t6."Name", t6."ManagerId"
                                FROM t4 AS t5
                                INNER JOIN "Employee" AS t6 ON t5."Id" = t6."ManagerId"
                            )
                            SELECT t4."Id", t4."Name", t4."ManagerId"
                            FROM t4
                        ) AS t7 ON t2."Manager" = t7."Id"
                    )
                    SELECT t1."Id", t1."Manager"
                    FROM t1
                ) AS t8
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void NestsARecursionInTheStepOfARecursionWithADepthLimit()
    {
        IQueryable<int> ids = Trees
            .WithRecursive(Employees.Where(e => e.ManagerId == null), Reports, 2)
            .Select(e => e.Id);
        Expression query = Trees
            .WithRecursive(
                ids,
                level => level.Join(ids, n => n, id => id - 1, (n, id) => id),
                4
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t12."Value"
                FROM (
                    WITH RECURSIVE t5 AS (
                        SELECT t4."Id" AS "Value", CAST(0 AS bigint) AS "$depth"
                        FROM (
                            WITH RECURSIVE t1 AS (
                                SELECT t0."Id", t0."Name", t0."ManagerId", CAST(0 AS bigint) AS "$depth"
                                FROM "Employee" AS t0
                                WHERE t0."ManagerId" IS NULL
                                UNION ALL
                                SELECT t3."Id", t3."Name", t3."ManagerId", CASE WHEN t2."$depth" < 2 THEN t2."$depth" + 1 ELSE CAST((SELECT 'WITH RECURSIVE: a row deeper than the maximum depth of 2') AS bigint) END AS "$depth"
                                FROM t1 AS t2
                                INNER JOIN "Employee" AS t3 ON t2."Id" = t3."ManagerId"
                            )
                            SELECT t1."Id", t1."Name", t1."ManagerId"
                            FROM t1
                            WHERE (SELECT COUNT(*) FROM t1) > 0
                        ) AS t4
                        UNION ALL
                        SELECT t11."Id" AS "Value", CASE WHEN t6."$depth" < 4 THEN t6."$depth" + 1 ELSE CAST((SELECT 'WITH RECURSIVE: a row deeper than the maximum depth of 4') AS bigint) END AS "$depth"
                        FROM t5 AS t6
                        INNER JOIN (
                            WITH RECURSIVE t8 AS (
                                SELECT t7."Id", t7."Name", t7."ManagerId", CAST(0 AS bigint) AS "$depth"
                                FROM "Employee" AS t7
                                WHERE t7."ManagerId" IS NULL
                                UNION ALL
                                SELECT t10."Id", t10."Name", t10."ManagerId", CASE WHEN t9."$depth" < 2 THEN t9."$depth" + 1 ELSE CAST((SELECT 'WITH RECURSIVE: a row deeper than the maximum depth of 2') AS bigint) END AS "$depth"
                                FROM t8 AS t9
                                INNER JOIN "Employee" AS t10 ON t9."Id" = t10."ManagerId"
                            )
                            SELECT t8."Id", t8."Name", t8."ManagerId"
                            FROM t8
                            WHERE (SELECT COUNT(*) FROM t8) > 0
                        ) AS t11 ON t6."Value" = (t11."Id" - 1)
                    )
                    SELECT t5."Value"
                    FROM t5
                    WHERE (SELECT COUNT(*) FROM t5) > 0
                ) AS t12
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    public sealed record Walk(int Id, int Depth, string Path);

    public sealed record Pair
    {
        public int Id { get; init; }

        public int? Manager { get; init; }
    }

    public sealed record Typed
    {
        public int Id { get; init; }

        public string? Text { get; init; }

        public decimal? Amount { get; init; }

        public Guid? Key { get; init; }

        public DateTimeOffset? Moment { get; init; }

        public TimeSpan? Span { get; init; }

        public OrderStatus? Status { get; init; }

        public bool? Flag { get; init; }

        public byte[]? Bytes { get; init; }

        public short? Small { get; init; }

        public float? Single { get; init; }
    }
}
