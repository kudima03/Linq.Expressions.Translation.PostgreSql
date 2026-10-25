namespace Linq.Expressions.Translation.PostgreSql.Tests.Operators;

public sealed record GroupJoinOperatorTests
{
    [Fact]
    public void CountsDistinctGroupJoinValues()
    {
        Expression query = Customers
            .GroupJoin(
                Orders,
                c => c.Id,
                o => o.CustomerId,
                (c, os) =>
                    new { c.Name, Statuses = os.Select(o => o.Status).Distinct().Count() }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", (
                    SELECT CAST(COUNT(*) AS integer)
                    FROM (
                        SELECT DISTINCT t1."Status"
                        FROM "Order" AS t1
                        WHERE t1."CustomerId" = t0."Id"
                    ) AS t2
                ) AS "Statuses"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesLatestGroupJoinRow()
    {
        Expression query = Customers
            .GroupJoin(
                Orders,
                c => c.Id,
                o => o.CustomerId,
                (c, os) =>
                    new
                    {
                        c.Name,
                        Latest = os.OrderByDescending(o => o.CreatedAt)
                            .Select(o => o.Number)
                            .FirstOrDefault(),
                    }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", (
                    SELECT t1."Number"
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                    ORDER BY t1."CreatedAt" DESC
                    LIMIT 1
                ) AS "Latest"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CountsFilteredGroupJoinRowsWithPredicate()
    {
        Expression query = Customers
            .GroupJoin(
                Orders,
                c => c.Id,
                o => o.CustomerId,
                (c, os) =>
                    new
                    {
                        c.Name,
                        Large = os.Where(o => o.IsPaid).Count(o => o.Quantity > 1),
                    }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", (
                    SELECT CAST(COUNT(*) AS integer)
                    FROM "Order" AS t1
                    WHERE (t1."CustomerId" = t0."Id") AND t1."IsPaid" AND (t1."Quantity" > 1)
                ) AS "Large"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ChecksFilteredGroupJoinRowsExist()
    {
        Expression query = Customers
            .GroupJoin(
                Orders,
                c => c.Id,
                o => o.CustomerId,
                (c, os) => new { c.Name, HasPaid = os.Any(o => o.IsPaid) }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", EXISTS (
                    SELECT 1
                    FROM "Order" AS t1
                    WHERE (t1."CustomerId" = t0."Id") AND t1."IsPaid"
                ) AS "HasPaid"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SumsTopGroupJoinValues()
    {
        Expression query = Customers
            .GroupJoin(
                Orders,
                c => c.Id,
                o => o.CustomerId,
                (c, os) =>
                    new
                    {
                        c.Name,
                        Top = os.OrderByDescending(o => o.Total)
                            .Select(o => o.Total)
                            .Take(3)
                            .Sum(),
                    }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", (
                    SELECT COALESCE(SUM(t2."Total"), 0)
                    FROM (
                        SELECT t1."Total"
                        FROM "Order" AS t1
                        WHERE t1."CustomerId" = t0."Id"
                        ORDER BY t1."Total" DESC
                        LIMIT 3
                    ) AS t2
                ) AS "Top"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RejectsProjectingFilteredGroup()
    {
        Expression query = Customers
            .GroupJoin(
                Orders,
                c => c.Id,
                o => o.CustomerId,
                (c, os) => new { c.Name, Paid = os.Where(o => o.IsPaid) }
            )
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains(
            "only be aggregated",
            exception.Message,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void RejectsFlatteningFilteredGroup()
    {
        Expression query = Customers
            .GroupJoin(
                Orders,
                c => c.Id,
                o => o.CustomerId,
                (c, os) => new { c, Paid = os.Where(o => o.IsPaid) }
            )
            .SelectMany(x => x.Paid, (x, o) => new { x.c.Name, o.Number })
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("SelectMany", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsUnsupportedOperatorOnGroup()
    {
        Expression query = Customers
            .GroupJoin(
                Orders,
                c => c.Id,
                o => o.CustomerId,
                (c, os) => new { c.Name, Rest = os.SkipWhile(o => o.IsPaid).Count() }
            )
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("SkipWhile", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SubstitutesDefaultForFirstOrDefaultOfGroupJoinValue()
    {
        Expression query = Customers
            .GroupJoin(
                Orders,
                c => c.Id,
                o => o.CustomerId,
                (c, os) =>
                    new
                    {
                        c.Name,
                        Latest = os.OrderByDescending(o => o.CreatedAt)
                            .Select(o => o.Total)
                            .FirstOrDefault(),
                    }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", COALESCE((
                    SELECT t1."Total"
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                    ORDER BY t1."CreatedAt" DESC
                    LIMIT 1
                ), 0) AS "Latest"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void KeepsNullForFirstOrDefaultOfNullableGroupJoinValue()
    {
        Expression query = Customers
            .GroupJoin(
                Orders,
                c => c.Id,
                o => o.CustomerId,
                (c, os) =>
                    new
                    {
                        c.Name,
                        Latest = os.OrderByDescending(o => o.CreatedAt)
                            .Select(o => (decimal?)o.Total)
                            .FirstOrDefault(),
                    }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", (
                    SELECT t1."Total"
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                    ORDER BY t1."CreatedAt" DESC
                    LIMIT 1
                ) AS "Latest"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
