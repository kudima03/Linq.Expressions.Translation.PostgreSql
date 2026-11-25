namespace Linq.Expressions.Translation.PostgreSql.Tests.Markers;

// PostgreSqlRecursion.WithRecursive is a derived table holding
// WITH RECURSIVE name AS (seed UNION ALL step): the step reads the level before by
// the name, its columns follow the seed's by name, and a constant of the seed is
// cast to the type of its column. With a depth limit, a hidden "$depth" column
// counts the levels, the step fails beyond the limit, and every level is computed
// before any row is read.
public sealed record RecursionTests
{
    [Fact]
    public void WalksAHierarchy()
    {
        Expression query = Trees
            .WithRecursive(
                Employees.Where(e => e.ManagerId == null),
                level => level.Join(Employees, m => m.Id, e => e.ManagerId, (m, e) => e)
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
                        SELECT t3."Id", t3."Name", t3."ManagerId"
                        FROM t1 AS t2
                        INNER JOIN "Employee" AS t3 ON t2."Id" = t3."ManagerId"
                    )
                    SELECT t1."Id", t1."Name", t1."ManagerId"
                    FROM t1
                ) AS t4
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void LimitsTheDepth()
    {
        Expression query = Trees
            .WithRecursive(
                Employees.Where(e => e.ManagerId == null),
                level => level.Join(Employees, m => m.Id, e => e.ManagerId, (m, e) => e),
                10
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
                        SELECT t3."Id", t3."Name", t3."ManagerId", CASE WHEN t2."$depth" < 10 THEN t2."$depth" + 1 ELSE CAST((SELECT 'WITH RECURSIVE: a row deeper than the maximum depth of 10') AS bigint) END AS "$depth"
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
    public void LimitsTheDepthToTheSeed()
    {
        Expression query = Trees
            .WithRecursive(Employees, level => level.Where(e => e.Id > 1), 0)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t3."Id", t3."Name", t3."ManagerId"
                FROM (
                    WITH RECURSIVE t1 AS (
                        SELECT t0."Id", t0."Name", t0."ManagerId", CAST(0 AS bigint) AS "$depth"
                        FROM "Employee" AS t0
                        UNION ALL
                        SELECT t2."Id", t2."Name", t2."ManagerId", CASE WHEN t2."$depth" < 0 THEN t2."$depth" + 1 ELSE CAST((SELECT 'WITH RECURSIVE: a row deeper than the maximum depth of 0') AS bigint) END AS "$depth"
                        FROM t1 AS t2
                        WHERE t2."Id" > 1
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
    public void MatchesTheColumnsOfTheStepByName()
    {
        Expression query = Trees
            .WithRecursive(
                Employees
                    .Where(e => e.ManagerId == null)
                    .Select(e => new Walk(e.Id, 0, e.Name)),
                level =>
                    level.Join(
                        Employees,
                        w => w.Id,
                        e => e.ManagerId,
                        (w, e) => new Walk(e.Id, w.Depth + 1, w.Path + "/" + e.Name)
                    )
            )
            .Expression;
        Expression initialized = Trees
            .WithRecursive(
                Employees.Select(e => new Pair { Id = e.Id, Manager = e.ManagerId }),
                level =>
                    level.Join(
                        Employees,
                        p => p.Manager,
                        e => e.Id,
                        (p, e) => new Pair { Manager = e.ManagerId, Id = p.Id }
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
                    )
                    SELECT t1."Id", t1."Depth", t1."Path"
                    FROM t1
                ) AS t4
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Equal(
            Sql.Valid(
                """
                SELECT t4."Id", t4."Manager"
                FROM (
                    WITH RECURSIVE t1 AS (
                        SELECT t0."Id", t0."ManagerId" AS "Manager"
                        FROM "Employee" AS t0
                        UNION ALL
                        SELECT t2."Id", t3."ManagerId" AS "Manager"
                        FROM t1 AS t2
                        INNER JOIN "Employee" AS t3 ON t2."Manager" = t3."Id"
                    )
                    SELECT t1."Id", t1."Manager"
                    FROM t1
                ) AS t4
                """
            ),
            new PostgreSqlQuery(initialized).TextValue
        );
    }

    [Fact]
    public void TypesTheConstantsOfTheSeed()
    {
        Expression query = Trees
            .WithRecursive(
                Orders.Select(o => new Tally
                {
                    Id = o.Id,
                    Count = 0,
                    Big = 1L,
                    Sum = 0m,
                    Ratio = 1.5,
                    Note = null,
                    Flag = true,
                    Day = null,
                }),
                level =>
                    level
                        .Where(t => t.Count < 3)
                        .Select(t => new Tally
                        {
                            Id = t.Id,
                            Count = t.Count + 1,
                            Big = t.Big * 2,
                            Sum = t.Sum + 0.5m,
                            Ratio = t.Ratio / 2,
                            Note = t.Note + "x",
                            Flag = !t.Flag,
                            Day = t.Day,
                        })
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t3."Id", t3."Count", t3."Big", t3."Sum", t3."Ratio", t3."Note", t3."Flag", t3."Day"
                FROM (
                    WITH RECURSIVE t1 AS (
                        SELECT t0."Id", CAST(0 AS integer) AS "Count", CAST(1 AS bigint) AS "Big", CAST(0 AS numeric) AS "Sum", CAST(1.5 AS double precision) AS "Ratio", CAST(NULL AS text) AS "Note", TRUE AS "Flag", CAST(NULL AS date) AS "Day"
                        FROM "Order" AS t0
                        UNION ALL
                        SELECT t2."Id", t2."Count" + 1 AS "Count", t2."Big" * 2 AS "Big", t2."Sum" + 0.5 AS "Sum", t2."Ratio" / 2 AS "Ratio", COALESCE(t2."Note", '') || 'x' AS "Note", NOT t2."Flag" AS "Flag", t2."Day"
                        FROM t1 AS t2
                        WHERE t2."Count" < 3
                    )
                    SELECT t1."Id", t1."Count", t1."Big", t1."Sum", t1."Ratio", t1."Note", t1."Flag", t1."Day"
                    FROM t1
                ) AS t3
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RecursesOverScalarRows()
    {
        Expression query = Trees
            .WithRecursive(
                Orders.Where(o => o.Id == 1).Select(o => o.Quantity),
                level => level.Where(n => n < 10).Select(n => n + 1)
            )
            .Expression;
        Expression constant = Trees
            .WithRecursive(
                Orders.Take(1).Select(o => 1),
                level => level.Where(n => n < 10).Select(n => n * 2)
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t3."Value"
                FROM (
                    WITH RECURSIVE t1 AS (
                        SELECT t0."Quantity" AS "Value"
                        FROM "Order" AS t0
                        WHERE t0."Id" = 1
                        UNION ALL
                        SELECT t2."Value" + 1 AS "Value"
                        FROM t1 AS t2
                        WHERE t2."Value" < 10
                    )
                    SELECT t1."Value"
                    FROM t1
                ) AS t3
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Equal(
            Sql.Valid(
                """
                SELECT t3."Value"
                FROM (
                    WITH RECURSIVE t1 AS ((SELECT CAST(1 AS integer) AS "Value" FROM "Order" AS t0 LIMIT 1) UNION ALL SELECT t2."Value" * 2 AS "Value" FROM t1 AS t2 WHERE t2."Value" < 10)
                    SELECT t1."Value"
                    FROM t1
                ) AS t3
                """
            ),
            new PostgreSqlQuery(constant).TextValue
        );
    }

    [Fact]
    public void ReadsTheLevelAsTheJoinedSource()
    {
        Expression query = Trees
            .WithRecursive(
                Employees.Where(e => e.ManagerId == null),
                level => Employees.Join(level, e => e.ManagerId, m => m.Id, (e, m) => e)
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
                        FROM "Employee" AS t2
                        INNER JOIN t1 AS t3 ON t2."ManagerId" = t3."Id"
                    )
                    SELECT t1."Id", t1."Name", t1."ManagerId"
                    FROM t1
                ) AS t4
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ReadsTheLevelBeforeACorrelatedJoin()
    {
        Expression query = Trees
            .WithRecursive(
                Employees.Where(e => e.ManagerId == null),
                level => level.SelectMany(m => Employees.Where(e => e.ManagerId == m.Id))
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
                        SELECT t3."Id", t3."Name", t3."ManagerId"
                        FROM t1 AS t2
                        INNER JOIN "Employee" AS t3 ON t3."ManagerId" = t2."Id"
                    )
                    SELECT t1."Id", t1."Name", t1."ManagerId"
                    FROM t1
                ) AS t4
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersAndOrdersTheLevelBefore()
    {
        Expression query = Trees
            .WithRecursive(
                Employees.OrderBy(e => e.Id).Take(1),
                level =>
                    level
                        .Where(m => m.Id < 5)
                        .OrderBy(m => m.Name)
                        .Join(Employees, m => m.Id, e => e.ManagerId, (m, e) => e)
                        .Where(e => e.Name != "Dev")
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t4."Id", t4."Name", t4."ManagerId"
                FROM (
                    WITH RECURSIVE t1 AS ((SELECT t0."Id", t0."Name", t0."ManagerId" FROM "Employee" AS t0 ORDER BY t0."Id" LIMIT 1) UNION ALL SELECT t3."Id", t3."Name", t3."ManagerId" FROM t1 AS t2 INNER JOIN "Employee" AS t3 ON t2."Id" = t3."ManagerId" WHERE (t2."Id" < 5) AND (t3."Name" <> 'Dev'))
                    SELECT t1."Id", t1."Name", t1."ManagerId"
                    FROM t1
                ) AS t4
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SeedsFromACombinedQuery()
    {
        Expression query = Trees
            .WithRecursive(
                Employees.Where(e => e.Id == 2).Concat(Employees.Where(e => e.Id == 3)),
                level => level.Join(Employees, m => m.Id, e => e.ManagerId, (m, e) => e)
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t6."Id", t6."Name", t6."ManagerId"
                FROM (
                    WITH RECURSIVE t3 AS (
                        SELECT t2."Id", t2."Name", t2."ManagerId"
                        FROM (
                            SELECT t0."Id", t0."Name", t0."ManagerId"
                            FROM "Employee" AS t0
                            WHERE t0."Id" = 2
                            UNION ALL
                            SELECT t1."Id", t1."Name", t1."ManagerId"
                            FROM "Employee" AS t1
                            WHERE t1."Id" = 3
                        ) AS t2
                        UNION ALL
                        SELECT t5."Id", t5."Name", t5."ManagerId"
                        FROM t3 AS t4
                        INNER JOIN "Employee" AS t5 ON t4."Id" = t5."ManagerId"
                    )
                    SELECT t3."Id", t3."Name", t3."ManagerId"
                    FROM t3
                ) AS t6
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void MakesAColumnNullableWhereTheStepMayYieldNull()
    {
        Expression query = Trees
            .WithRecursive(
                Employees
                    .Where(e => e.Id == 4)
                    .Select(e => new Pair { Id = e.Id, Manager = 1 }),
                level =>
                    level.Join(
                        Employees,
                        p => p.Manager,
                        e => e.Id,
                        (p, e) => new Pair { Id = e.Id, Manager = e.ManagerId }
                    )
            )
            .Where(p => p.Manager == p.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t4."Id", t4."Manager"
                FROM (
                    WITH RECURSIVE t1 AS (
                        SELECT t0."Id", CAST(1 AS integer) AS "Manager"
                        FROM "Employee" AS t0
                        WHERE t0."Id" = 4
                        UNION ALL
                        SELECT t3."Id", t3."ManagerId" AS "Manager"
                        FROM t1 AS t2
                        INNER JOIN "Employee" AS t3 ON t2."Manager" = t3."Id"
                    )
                    SELECT t1."Id", t1."Manager"
                    FROM t1
                ) AS t4
                WHERE t4."Manager" IS NOT DISTINCT
                FROM t4."Id"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComposesWithTheQueryAroundIt()
    {
        IQueryable<Employee> reports = Trees.WithRecursive(
            Employees.Where(e => e.ManagerId == null),
            level => level.Join(Employees, m => m.Id, e => e.ManagerId, (m, e) => e),
            5
        );
        Expression filtered = reports
            .Where(e => e.Name != "Dev")
            .OrderBy(e => e.Id)
            .Select(e => e.Name)
            .Expression;
        Expression correlated = Customers
            .Select(c => new { c.Id, Reports = reports.Count(e => e.Name == c.Name) })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t4."Name"
                FROM (
                    WITH RECURSIVE t1 AS (
                        SELECT t0."Id", t0."Name", t0."ManagerId", CAST(0 AS bigint) AS "$depth"
                        FROM "Employee" AS t0
                        WHERE t0."ManagerId" IS NULL
                        UNION ALL
                        SELECT t3."Id", t3."Name", t3."ManagerId", CASE WHEN t2."$depth" < 5 THEN t2."$depth" + 1 ELSE CAST((SELECT 'WITH RECURSIVE: a row deeper than the maximum depth of 5') AS bigint) END AS "$depth"
                        FROM t1 AS t2
                        INNER JOIN "Employee" AS t3 ON t2."Id" = t3."ManagerId"
                    )
                    SELECT t1."Id", t1."Name", t1."ManagerId"
                    FROM t1
                    WHERE (SELECT COUNT(*) FROM t1) > 0
                ) AS t4
                WHERE t4."Name" <> 'Dev'
                ORDER BY t4."Id"
                """
            ),
            new PostgreSqlQuery(filtered).TextValue
        );
        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COUNT(*) AS integer)
                FROM (
                    WITH RECURSIVE t1 AS (
                        SELECT t0."Id", t0."Name", t0."ManagerId", CAST(0 AS bigint) AS "$depth"
                        FROM "Employee" AS t0
                        WHERE t0."ManagerId" IS NULL
                        UNION ALL
                        SELECT t3."Id", t3."Name", t3."ManagerId", CASE WHEN t2."$depth" < 5 THEN t2."$depth" + 1 ELSE CAST((SELECT 'WITH RECURSIVE: a row deeper than the maximum depth of 5') AS bigint) END AS "$depth"
                        FROM t1 AS t2
                        INNER JOIN "Employee" AS t3 ON t2."Id" = t3."ManagerId"
                    )
                    SELECT t1."Id", t1."Name", t1."ManagerId"
                    FROM t1
                    WHERE (SELECT COUNT(*) FROM t1) > 0
                ) AS t4
                """
            ),
            new PostgreSqlQuery(reports.Executed(q => q.Count())).TextValue
        );
        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", (
                    SELECT CAST(COUNT(*) AS integer)
                    FROM (
                        WITH RECURSIVE t2 AS (
                            SELECT t1."Id", t1."Name", t1."ManagerId", CAST(0 AS bigint) AS "$depth"
                            FROM "Employee" AS t1
                            WHERE t1."ManagerId" IS NULL
                            UNION ALL
                            SELECT t4."Id", t4."Name", t4."ManagerId", CASE WHEN t3."$depth" < 5 THEN t3."$depth" + 1 ELSE CAST((SELECT 'WITH RECURSIVE: a row deeper than the maximum depth of 5') AS bigint) END AS "$depth"
                            FROM t2 AS t3
                            INNER JOIN "Employee" AS t4 ON t3."Id" = t4."ManagerId"
                        )
                        SELECT t2."Id", t2."Name", t2."ManagerId"
                        FROM t2
                        WHERE (SELECT COUNT(*) FROM t2) > 0
                    ) AS t5
                    WHERE t5."Name" = t0."Name"
                ) AS "Reports"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(correlated).TextValue
        );
    }

    [Fact]
    public void NestsARecursionInTheSeedOfAnother()
    {
        IQueryable<Employee> inner = Trees.WithRecursive(
            Employees.Where(e => e.Id == 1),
            level => level.Join(Employees, m => m.Id, e => e.ManagerId, (m, e) => e)
        );
        Expression query = Trees
            .WithRecursive(
                inner.Select(e => e.Id),
                level => level.Where(id => id < 3).Select(id => id + 1)
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t7."Value"
                FROM (
                    WITH RECURSIVE t5 AS (
                        SELECT t4."Id" AS "Value"
                        FROM (
                            WITH RECURSIVE t1 AS (
                                SELECT t0."Id", t0."Name", t0."ManagerId"
                                FROM "Employee" AS t0
                                WHERE t0."Id" = 1
                                UNION ALL
                                SELECT t3."Id", t3."Name", t3."ManagerId"
                                FROM t1 AS t2
                                INNER JOIN "Employee" AS t3 ON t2."Id" = t3."ManagerId"
                            )
                            SELECT t1."Id", t1."Name", t1."ManagerId"
                            FROM t1
                        ) AS t4
                        UNION ALL
                        SELECT t6."Value" + 1 AS "Value"
                        FROM t5 AS t6
                        WHERE t6."Value" < 3
                    )
                    SELECT t5."Value"
                    FROM t5
                ) AS t7
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

    public sealed record Tally
    {
        public int Id { get; init; }

        public int Count { get; init; }

        public long Big { get; init; }

        public decimal Sum { get; init; }

        public double Ratio { get; init; }

        public string? Note { get; init; }

        public bool Flag { get; init; }

        public DateOnly? Day { get; init; }
    }
}
