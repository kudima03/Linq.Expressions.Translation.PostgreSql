#pragma warning disable xUnit1004 // skipped tests record SQL the translator does not produce yet
#pragma warning disable CA1829 // Enumerable.Count on the array column is the subject

namespace Linq.Expressions.Translation.PostgreSql.Tests.Combinations;

public sealed record CapturedAndShapeCombinationTests
{
    private static string Prefix()
    {
        return "A-";
    }

    [Fact]
    public void FoldsCapturedFlagOfOptionalFilter()
    {
        bool onlyPaid = true;
        Expression query = Orders
            .Where(o => !onlyPaid || o.IsPaid)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE FALSE OR t0."IsPaid"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void MaterializesLazySequenceBeforeMembership()
    {
        List<int> ids = [1, 2, 3];
        IEnumerable<int> later = ids.Where(i => i > 1);
        Expression query = Orders
            .Where(o => later.Contains(o.Id))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Id" IN (2, 3)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RendersCapturedEnumsAsNumbersInMembership()
    {
        OrderStatus[] statuses = [OrderStatus.Paid, OrderStatus.Shipped];
        Expression query = Orders
            .Where(o => statuses.Contains(o.Status))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Status" IN (1, 2)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void EscapesCapturedStringsInMembership()
    {
        string[] numbers = ["O'B", "x"];
        Expression query = Orders
            .Where(o => numbers.Contains(o.Number))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Number" IN ('O''B', 'x')
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RendersCapturedGuidSetInMembership()
    {
        HashSet<Guid> trackings = [Guid.Empty];
        Expression query = Orders
            .Where(o => trackings.Contains(o.TrackingId))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."TrackingId" IN (UUID '00000000-0000-0000-0000-000000000000')
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void KeepsEveryRowWhenExcludingEmptyCapture()
    {
        int[] excluded = [];
        Expression query = Orders
            .Where(o => !excluded.Contains(o.Id))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE NOT FALSE
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(
        Skip = "Enumerable.Any with an equality predicate on a captured list is not translated."
    )]
    public void FiltersByCapturedListThroughAny()
    {
        List<int> ids = [1, 2, 3];
        Expression query = Orders
            .Where(o => ids.Any(i => i == o.Id))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Id" IN (1, 2, 3)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void InlinesCapturedQueryAsSubquery()
    {
        IQueryable<int> paying = Orders.Where(o => o.IsPaid).Select(o => o.CustomerId);
        Expression query = Customers
            .Where(c => paying.Contains(c.Id))
            .Select(c => c.Name)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name"
                FROM "Customer" AS t0
                WHERE t0."Id" IN (SELECT t1."CustomerId" FROM "Order" AS t1 WHERE t1."IsPaid")
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void EvaluatesCapturedObjectMemberAndMethod()
    {
        var limits = new { Min = 5 };
        Expression query = Orders
            .Where(o => o.Quantity > limits.Min && o.Number.StartsWith(Prefix()))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."Quantity" > 5) AND (t0."Number" LIKE 'A-%')
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TruncatesLocalClockToDay()
    {
        Expression query = Orders
            .Where(o => o.CreatedAt > DateTime.Now.Date)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."CreatedAt" > DATE_TRUNC('day', LOCALTIMESTAMP)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "Enumerable.Count of an array column is not translated.")]
    public void CountsArrayColumnElements()
    {
        Expression query = Orders.Select(o => o.Tags.Count()).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CARDINALITY(t0."Tags")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void MatchesSuffixCharacter()
    {
        Expression query = Orders.Select(o => o.Number.EndsWith('x')).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Number" LIKE '%x'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SubstitutesMinimumOffsetForNullPayment()
    {
        Expression query = Orders.Select(o => o.PaidAt.GetValueOrDefault()).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(t0."PaidAt", TIMESTAMPTZ '0001-01-01 00:00:00+00')
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsAggregatedGroupsInDerivedTable()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new { CustomerId = g.Key, Total = g.Sum(o => o.Total) })
            .Join(
                Customers,
                x => x.CustomerId,
                c => c.Id,
                (x, c) => new { c.Name, x.Total }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t2."Name", t1."Total"
                FROM (
                    SELECT t0."CustomerId", COALESCE(SUM(t0."Total"), 0) AS "Total"
                    FROM "Order" AS t0
                    GROUP BY t0."CustomerId"
                ) AS t1
                INNER JOIN "Customer" AS t2 ON t1."CustomerId" = t2."Id"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersProjectedAggregateInHaving()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new { g.Key, Total = g.Sum(o => o.Total) })
            .Where(x => x.Total > 100)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId" AS "Key", COALESCE(SUM(t0."Total"), 0) AS "Total"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                HAVING COALESCE(SUM(t0."Total"), 0) > 100
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CrossJoinsAndFiltersInQuerySyntax()
    {
        Expression query = (
            from o in Orders
            from c in Customers
            where o.CustomerId == c.Id
            select new { o.Id, c.Name }
        ).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t1."Name"
                FROM "Order" AS t0
                CROSS JOIN "Customer" AS t1
                WHERE t0."CustomerId" = t1."Id"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsTakenOuterRowsKeepingTheirOrder()
    {
        Expression query = Orders
            .OrderBy(o => o.Id)
            .Select(o => new { o.Id, o.CustomerId })
            .Take(5)
            .Join(Customers, o => o.CustomerId, c => c.Id, (o, c) => new { o.Id, c.Name })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Id", t2."Name"
                FROM (
                    SELECT t0."Id", t0."CustomerId"
                    FROM "Order" AS t0
                    ORDER BY t0."Id"
                    LIMIT 5
                ) AS t1
                INNER JOIN "Customer" AS t2 ON t1."CustomerId" = t2."Id"
                ORDER BY t1."Id"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CountsDistinctJoinedValues()
    {
        Expression query = Orders
            .Join(Customers, o => o.CustomerId, c => c.Id, (o, c) => c.Region)
            .Distinct()
            .Executed(q => q.Count());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COUNT(*) AS integer)
                FROM (
                    SELECT DISTINCT t1."Region"
                    FROM "Order" AS t0
                    INNER JOIN "Customer" AS t1 ON t0."CustomerId" = t1."Id"
                ) AS t2
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RegroupsAggregatedGroups()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new { g.Key, Orders = g.Count() })
            .GroupBy(x => x.Orders)
            .Select(g => new { g.Key, Customers = g.Count() })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Orders" AS "Key", CAST(COUNT(*) AS integer) AS "Customers"
                FROM (
                    SELECT t0."CustomerId" AS "Key", CAST(COUNT(*) AS integer) AS "Orders"
                    FROM "Order" AS t0
                    GROUP BY t0."CustomerId"
                ) AS t1
                GROUP BY t1."Orders"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersProjectedRecord()
    {
        Expression query = Customers
            .Select(c => new CustomerSummary(c.Id, c.Name))
            .Where(s => s.Name != "")
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name"
                FROM "Customer" AS t0
                WHERE t0."Name" <> ''
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OrdersByComputedMemberOfInitializedObject()
    {
        Expression query = Customers
            .Select(c => new CustomerCard { Id = c.Id, Title = c.Name + "!" })
            .OrderBy(x => x.Title)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name" || '!' AS "Title"
                FROM "Customer" AS t0
                ORDER BY t0."Name" || '!'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ProjectsConstant()
    {
        Expression query = Customers.Select(c => 1).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT 1
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ProjectsNestedMemberAfterTake()
    {
        Expression query = Orders
            .Select(o => new { o.Id, Info = new { o.Number, o.Total } })
            .Take(2)
            .Select(x => x.Info.Total)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Total"
                FROM "Order" AS t0
                LIMIT 2
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
