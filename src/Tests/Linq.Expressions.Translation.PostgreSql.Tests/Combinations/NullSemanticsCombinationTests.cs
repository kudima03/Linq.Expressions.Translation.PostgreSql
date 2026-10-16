#pragma warning disable xUnit1004 // skipped tests record SQL the translator does not produce yet

namespace Linq.Expressions.Translation.PostgreSql.Tests.Combinations;

public sealed record NullSemanticsCombinationTests
{
    [Fact]
    public void KeepsRowsWithNullWhenNegatingNullableComparison()
    {
        Expression query = Orders
            .Where(o => !(o.Priority > 3))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE NOT COALESCE(t0."Priority" > 3, FALSE)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void KeepsRowsWithNullWhenNegatingDisjunctionOfNullableComparison()
    {
        Expression query = Orders
            .Where(o => !(o.Priority > 3 || o.IsPaid))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE NOT (COALESCE(t0."Priority" > 3, FALSE) OR t0."IsPaid")
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void KeepsRowsWithNullWhenNegatingConjunctionOfNullableComparisons()
    {
        Expression query = Orders
            .Where(o => !(o.Priority > 3 && o.Discount < 10m))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE NOT (COALESCE(t0."Priority" > 3, FALSE) AND COALESCE(t0."Discount" < 10, FALSE))
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ProjectsConjunctionWithNullableComparisonAsBoolean()
    {
        Expression query = Orders.Select(o => o.Priority > 3 && o.IsPaid).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(t0."Priority" > 3, FALSE) AND t0."IsPaid"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByNullableBooleanDifferentFromFalse()
    {
        Expression query = Orders
            .Where(o => o.IsGift != false)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."IsGift" IS DISTINCT FROM FALSE
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByNegatedCoalescedNullableBoolean()
    {
        Expression query = Orders
            .Where(o => !(o.IsGift ?? true))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE NOT COALESCE(t0."IsGift", TRUE)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByValueOfNullableAfterHasValue()
    {
        Expression query = Orders
            .Where(o => o.Priority.HasValue && o.Priority.Value > 3)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."Priority" IS NOT NULL) AND (t0."Priority" > 3)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesNullableColumnWithNonNullableColumn()
    {
        Expression query = Orders
            .Where(o => o.Priority == o.Quantity)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Priority" IS NOT DISTINCT FROM t0."Quantity"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ProjectsOrderingOfNullableAndNonNullableColumns()
    {
        Expression query = Orders.Select(o => o.Priority < o.Quantity).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(t0."Priority" < t0."Quantity", FALSE)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersBySumOfNullableAndNonNullableColumns()
    {
        Expression query = Orders
            .Where(o => o.Priority + o.Quantity > 10)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."Priority" + t0."Quantity") > 10
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ProjectsConditionalOnNullableComparison()
    {
        Expression query = Orders.Select(o => o.Priority > 3 ? "high" : "low").Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CASE WHEN t0."Priority" > 3 THEN 'high' ELSE 'low' END
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ProjectsConditionalReturningNull()
    {
        Expression query = Orders.Select(o => o.IsPaid ? o.Discount : null).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CASE WHEN t0."IsPaid" THEN t0."Discount" ELSE NULL END
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OrdersByNullableKeysInBothDirections()
    {
        Expression query = Orders
            .OrderBy(o => o.Priority)
            .ThenByDescending(o => o.Discount)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                ORDER BY t0."Priority" NULLS FIRST, t0."Discount" DESC NULLS LAST
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OrdersByCoalescedNullableKeyWithoutNullsClause()
    {
        Expression query = Orders
            .OrderBy(o => o.Priority ?? int.MaxValue)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                ORDER BY COALESCE(t0."Priority", 2147483647)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CountsRowsMatchingNullableComparison()
    {
        Expression query = Orders.Executed(q => q.Count(o => o.Priority > 3));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COUNT(*) AS integer)
                FROM "Order" AS t0
                WHERE t0."Priority" > 3
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CountsRowsNotMatchingNullableComparison()
    {
        Expression query = Orders.Executed(q => q.Count(o => !(o.Priority > 3)));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COUNT(*) AS integer)
                FROM "Order" AS t0
                WHERE NOT COALESCE(t0."Priority" > 3, FALSE)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SumsNullableDecimalAsZeroWhenEmpty()
    {
        Expression query = Orders.Executed(q => q.Sum(o => o.Discount));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(SUM(t0."Discount"), 0)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SumsNullableIntegerAsInteger()
    {
        Expression query = Orders.Executed(q => q.Sum(o => o.Priority));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COALESCE(SUM(t0."Priority"), 0) AS integer)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AveragesNullableIntegerAsDouble()
    {
        Expression query = Orders.Executed(q => q.Average(o => o.Priority));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT AVG(CAST(t0."Priority" AS double precision))
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void GroupsByNullableKey()
    {
        Expression query = Orders
            .GroupBy(o => o.Priority)
            .Select(g => new { g.Key, Count = g.Count() })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Priority" AS "Key", CAST(COUNT(*) AS integer) AS "Count"
                FROM "Order" AS t0
                GROUP BY t0."Priority"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersGroupsByNegatedNullableAggregateComparison()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Where(g => !(g.Max(o => o.Priority) > 3))
            .Select(g => g.Key)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                HAVING NOT COALESCE(MAX(t0."Priority") > 3, FALSE)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByCorrelatedNullableMaximum()
    {
        Expression query = Customers
            .Where(c => Orders.Where(o => o.CustomerId == c.Id).Max(o => o.Priority) > 3)
            .Select(c => c.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Customer" AS t0
                WHERE (
                    SELECT MAX(t1."Priority")
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                ) > 3
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ProjectsComparisonOfCorrelatedNullableMaximum()
    {
        Expression query = Customers
            .Select(c => Orders.Where(o => o.CustomerId == c.Id).Max(o => o.Priority) > 3)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE((
                    SELECT MAX(t1."Priority")
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                ) > 3, FALSE)
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ChecksCorrelatedRowsWithNullableComparison()
    {
        Expression query = Customers
            .Where(c => Orders.Any(o => o.CustomerId == c.Id && o.Priority > 3))
            .Select(c => c.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Customer" AS t0
                WHERE EXISTS (
                    SELECT 1
                    FROM "Order" AS t1
                    WHERE (t1."CustomerId" = t0."Id") AND (t1."Priority" > 3)
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ChecksAllCorrelatedRowsWithNullableComparison()
    {
        Expression query = Customers
            .Where(c => Orders.Where(o => o.CustomerId == c.Id).All(o => o.Priority > 3))
            .Select(c => c.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Customer" AS t0
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM "Order" AS t1
                    WHERE (t1."CustomerId" = t0."Id") AND NOT COALESCE(t1."Priority" > 3, FALSE)
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "Correlation equality uses = and never matches two nulls.")]
    public void MatchesNullCorrelationKeysLikeCSharp()
    {
        Expression query = Products
            .Where(p => Products.Any(q => q.CategoryId == p.CategoryId && q.Id != p.Id))
            .Select(p => p.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Product" AS t0
                WHERE EXISTS (
                    SELECT 1
                    FROM "Product" AS t1
                    WHERE (t1."CategoryId" IS NOT DISTINCT FROM t0."CategoryId") AND (t1."Id" <> t0."Id")
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "IN on a nullable value is not null-safe under NOT.")]
    public void KeepsNullWhenNegatingMembershipOfNullableColumn()
    {
        int?[] priorities = [1, 2];
        Expression query = Orders
            .Where(o => !priorities.Contains(o.Priority))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE NOT COALESCE(t0."Priority" IN (1, 2), FALSE)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "IN on a nullable value is projected as NULL instead of FALSE.")]
    public void ProjectsMembershipOfNullableColumnAsBoolean()
    {
        string[] notes = ["a", "b"];
        Expression query = Orders.Select(o => notes.Contains(o.Note)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(t0."Note" IN ('a', 'b'), FALSE)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "= ANY on a nullable value is not null-safe under NOT.")]
    public void KeepsNullWhenNegatingArrayMembershipOfNullableColumn()
    {
        Expression query = Orders
            .Where(o => !o.Tags.Contains(o.Note))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE NOT COALESCE(t0."Note" = ANY (t0."Tags"), FALSE)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByMembershipIncludingNullOrOtherCondition()
    {
        int?[] priorities = [1, null];
        Expression query = Orders
            .Where(o => priorities.Contains(o.Priority) || o.IsPaid)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."Priority" IN (1)) OR (t0."Priority" IS NULL) OR t0."IsPaid"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByCapturedNullAsIsNull()
    {
        int? priority = null;
        Expression query = Orders
            .Where(o => o.Priority == priority && o.Discount != null)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."Priority" IS NULL) AND (t0."Discount" IS NOT NULL)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersNullableStringBeforeCallingMember()
    {
        Expression query = Orders
            .Where(o => o.Note != null && o.Note.StartsWith("A"))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."Note" IS NOT NULL) AND (t0."Note" LIKE 'A%')
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AddsCoalescedNullableToColumn()
    {
        Expression query = Orders
            .Select(o => o.Discount.GetValueOrDefault() + o.Total)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(t0."Discount", 0) + t0."Total"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void LeftJoinsSelfOnNullableKey()
    {
        Expression query = (
            from e in Employees
            join m in Employees on e.ManagerId equals m.Id into ms
            from m in ms.DefaultIfEmpty()
            select new { e.Name, Manager = m.Name }
        ).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t1."Name" AS "Manager"
                FROM "Employee" AS t0
                LEFT JOIN "Employee" AS t1 ON t0."ManagerId" = t1."Id"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
