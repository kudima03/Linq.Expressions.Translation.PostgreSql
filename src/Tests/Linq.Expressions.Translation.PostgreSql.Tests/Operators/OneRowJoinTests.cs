#pragma warning disable CA1861 // the one-element array is the root under test

namespace Linq.Expressions.Translation.PostgreSql.Tests.Operators;

// A one-row root of aggregates is joined as a derived table on either side, which puts
// the totals next to every row.
public sealed record OneRowJoinTests
{
    private static IQueryable<int> Totals =>
        new[] { 0 }.AsQueryable().Select(_ => Orders.Count());

    private static IQueryable<decimal> Revenue =>
        new[] { 0 }.AsQueryable().Select(_ => Orders.Sum(o => o.Total));

    [Fact]
    public void CrossJoinsOneRowRootToEveryRow()
    {
        IQueryable<int> totals = Totals;
        Expression query = Customers
            .SelectMany(c => totals, (c, n) => new { c.Name, Orders = n })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t2."Value" AS "Orders"
                FROM "Customer" AS t0
                CROSS JOIN (
                    SELECT (
                        SELECT CAST(COUNT(*) AS integer)
                        FROM "Order" AS t1
                    ) AS "Value"
                ) AS t2
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CrossJoinsOneRowRootWithQuerySyntax()
    {
        IQueryable<int> totals = Totals;
        Expression query = (
            from c in Customers
            from n in totals
            select new { c.Name, Orders = n }
        ).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t2."Value" AS "Orders"
                FROM "Customer" AS t0
                CROSS JOIN (
                    SELECT (
                        SELECT CAST(COUNT(*) AS integer)
                        FROM "Order" AS t1
                    ) AS "Value"
                ) AS t2
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CrossJoinsOneRowRootOfSeveralAggregates()
    {
        IQueryable<OrderTotals> totals = new[] { 0 }
            .AsQueryable()
            .Select(_ => new OrderTotals(Orders.Count(), Orders.Sum(o => o.Total)));
        Expression query = Customers
            .SelectMany(
                c => totals,
                (c, t) =>
                    new
                    {
                        c.Name,
                        t.Orders,
                        t.Revenue,
                    }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t3."Orders", t3."Revenue"
                FROM "Customer" AS t0
                CROSS JOIN (
                    SELECT (
                        SELECT CAST(COUNT(*) AS integer)
                        FROM "Order" AS t1
                    ) AS "Orders", (
                        SELECT COALESCE(SUM(t2."Total"), 0)
                        FROM "Order" AS t2
                    ) AS "Revenue"
                ) AS t3
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsOneRowRootOnKey()
    {
        IQueryable<int> totals = Totals;
        Expression query = Customers
            .Join(totals, c => c.Id, n => n, (c, n) => new { c.Name, Orders = n })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t2."Value" AS "Orders"
                FROM "Customer" AS t0
                INNER JOIN (
                    SELECT (
                        SELECT CAST(COUNT(*) AS integer)
                        FROM "Order" AS t1
                    ) AS "Value"
                ) AS t2 ON t0."Id" = t2."Value"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CrossJoinsOneRowRootToGroupedRows()
    {
        IQueryable<int> totals = Totals;
        Expression query = Orders
            .GroupBy(o => o.Status)
            .Select(g => g.Key)
            .SelectMany(s => totals, (s, n) => new { Status = s, Orders = n })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Status", t3."Value" AS "Orders"
                FROM (
                    SELECT t0."Status"
                    FROM "Order" AS t0
                    GROUP BY t0."Status"
                ) AS t1
                CROSS JOIN (
                    SELECT (
                        SELECT CAST(COUNT(*) AS integer)
                        FROM "Order" AS t2
                    ) AS "Value"
                ) AS t3
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CrossJoinsEveryRowToOneRowRoot()
    {
        Expression query = Totals
            .SelectMany(n => Customers, (n, c) => new { c.Name, Orders = n })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t2."Name", t1."Value" AS "Orders"
                FROM (
                    SELECT (
                        SELECT CAST(COUNT(*) AS integer)
                        FROM "Order" AS t0
                    ) AS "Value"
                ) AS t1
                CROSS JOIN "Customer" AS t2
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsEveryRowToOneRowRootOnKey()
    {
        Expression query = Totals
            .Join(Customers, n => n, c => c.Id, (n, c) => new { c.Name, Orders = n })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t2."Name", t1."Value" AS "Orders"
                FROM (
                    SELECT (
                        SELECT CAST(COUNT(*) AS integer)
                        FROM "Order" AS t0
                    ) AS "Value"
                ) AS t1
                INNER JOIN "Customer" AS t2 ON t1."Value" = t2."Id"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CrossJoinsTwoOneRowRoots()
    {
        IQueryable<decimal> revenue = Revenue;
        Expression query = Totals
            .SelectMany(n => revenue, (n, r) => new { Orders = n, Revenue = r })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Value" AS "Orders", t3."Value" AS "Revenue"
                FROM (
                    SELECT (
                        SELECT CAST(COUNT(*) AS integer)
                        FROM "Order" AS t0
                    ) AS "Value"
                ) AS t1
                CROSS JOIN (
                    SELECT (
                        SELECT COALESCE(SUM(t2."Total"), 0)
                        FROM "Order" AS t2
                    ) AS "Value"
                ) AS t3
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ProjectsShareOfOneRowRootTotal()
    {
        IQueryable<decimal> revenue = Revenue;
        Expression query = Orders
            .SelectMany(o => revenue, (o, r) => new { o.Id, Share = o.Total / r })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Total" / t2."Value" AS "Share"
                FROM "Order" AS t0
                CROSS JOIN (
                    SELECT (
                        SELECT COALESCE(SUM(t1."Total"), 0)
                        FROM "Order" AS t1
                    ) AS "Value"
                ) AS t2
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void LeftJoinsOneRowRoot()
    {
        IQueryable<int> totals = Totals;
        Expression query = Customers
            .SelectMany(
                c => totals.DefaultIfEmpty(),
                (c, n) => new { c.Name, Orders = n }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", COALESCE(t2."Value", 0) AS "Orders"
                FROM "Customer" AS t0
                LEFT JOIN (
                    SELECT (
                        SELECT CAST(COUNT(*) AS integer)
                        FROM "Order" AS t1
                    ) AS "Value"
                ) AS t2 ON TRUE
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CountsOneRowRootMatchesInScalarSubquery()
    {
        IQueryable<int> totals = Totals;
        Expression query = Customers
            .GroupJoin(
                totals,
                c => c.Id,
                n => n,
                (c, ns) => new { c.Name, Matches = ns.Count() }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", (
                    SELECT CAST(COUNT(*) AS integer)
                    WHERE (
                        SELECT CAST(COUNT(*) AS integer)
                        FROM "Order" AS t1
                    ) = t0."Id"
                ) AS "Matches"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void LeftJoinsOneRowRootThroughGroupJoin()
    {
        IQueryable<int> totals = Totals;
        Expression query = Customers
            .GroupJoin(totals, c => c.Id, n => n, (c, ns) => new { c, ns })
            .SelectMany(
                x => x.ns.DefaultIfEmpty(),
                (x, n) => new { x.c.Name, Orders = n }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", COALESCE(t2."Value", 0) AS "Orders"
                FROM "Customer" AS t0
                LEFT JOIN (
                    SELECT (
                        SELECT CAST(COUNT(*) AS integer)
                        FROM "Order" AS t1
                    ) AS "Value"
                ) AS t2 ON t0."Id" = t2."Value"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void InnerJoinsOneRowRootThroughGroupJoin()
    {
        IQueryable<int> totals = Totals;
        Expression query = Customers
            .GroupJoin(totals, c => c.Id, n => n, (c, ns) => new { c, ns })
            .SelectMany(x => x.ns, (x, n) => new { x.c.Name, Orders = n })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t2."Value" AS "Orders"
                FROM "Customer" AS t0
                INNER JOIN (
                    SELECT (
                        SELECT CAST(COUNT(*) AS integer)
                        FROM "Order" AS t1
                    ) AS "Value"
                ) AS t2 ON t0."Id" = t2."Value"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersAndOrdersJoinedOneRowRoot()
    {
        IQueryable<int> totals = Totals;
        Expression query = Customers
            .SelectMany(c => totals, (c, n) => new { c.Name, Orders = n })
            .Where(x => x.Orders > 1)
            .OrderBy(x => x.Name)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t2."Value" AS "Orders"
                FROM "Customer" AS t0
                CROSS JOIN (
                    SELECT (
                        SELECT CAST(COUNT(*) AS integer)
                        FROM "Order" AS t1
                    ) AS "Value"
                ) AS t2
                WHERE t2."Value" > 1
                ORDER BY t0."Name"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
