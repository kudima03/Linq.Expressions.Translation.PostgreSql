namespace Linq.Expressions.Translation.PostgreSql.Tests.PostgreSql;

public sealed record BooleanAndNullSemanticsTests
{
    [Fact]
    public void NegatesNullCheck()
    {
        Expression query = Orders
            .Where(o => !(o.Note == null))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE NOT (t0."Note" IS NULL)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void NegatesNullSafeEquality()
    {
        Expression query = Orders
            .Where(o => !(o.Priority == 3))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE NOT (t0."Priority" IS NOT DISTINCT FROM 3)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void NegatesNullSafeInequality()
    {
        Expression query = Orders
            .Where(o => !(o.Priority != 3))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE NOT (t0."Priority" IS DISTINCT FROM 3)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ChainsNullSafeComparisonsWithAnd()
    {
        Expression query = Orders
            .Where(o => (o.Note == "a") && (o.Priority != 2))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."Note" IS NOT DISTINCT FROM 'a') AND (t0."Priority" IS DISTINCT FROM 2)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TreatsNullableBooleanAsFalseWhenComparedWithTrue()
    {
        Expression query = Orders
            .Where(o => o.IsGift == true)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."IsGift" IS NOT DISTINCT FROM TRUE
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TreatsNullableBooleanAsTrueWhenComparedWithNotTrue()
    {
        Expression query = Orders
            .Where(o => o.IsGift != true)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."IsGift" IS DISTINCT FROM TRUE
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void NegatesNullableBooleanComparison()
    {
        Expression query = Orders
            .Where(o => !(o.IsGift == true))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE NOT (t0."IsGift" IS NOT DISTINCT FROM TRUE)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void UsesCoalescedNullableBooleanAsPredicate()
    {
        Expression query = Orders
            .Where(o => o.IsGift ?? true)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE COALESCE(t0."IsGift", TRUE)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CombinesCoalescedBooleansWithOr()
    {
        Expression query = Orders
            .Where(o => (o.IsGift ?? false) || o.IsPaid)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE COALESCE(t0."IsGift", FALSE) OR t0."IsPaid"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void KeepsNullComparisonOfConstantInMemory()
    {
        Expression query = Orders.Where(o => true).Select(o => o.Id).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE TRUE
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void EvaluatesCapturedNullComparisonInMemory()
    {
        string text = "x";

        Expression query = Orders.Where(o => text == null).Select(o => o.Id).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE FALSE
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesNullableColumnWithNullableColumn()
    {
        Expression query = Orders
            .Where(o => o.Discount == o.Discount)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Discount" IS NOT DISTINCT FROM t0."Discount"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesDistinctNullableColumns()
    {
        Expression query = Orders
            .Where(o => o.Priority != o.Priority)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Priority" IS DISTINCT FROM t0."Priority"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByNullableOrderingWithoutCoalesceInWhere()
    {
        Expression query = Orders
            .Where(o => o.Discount > 5m)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Discount" > 5
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByNegatedNullableOrderingWithCoalesce()
    {
        Expression query = Orders
            .Where(o => !(o.Discount > 5m))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE NOT COALESCE(t0."Discount" > 5, FALSE)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByNullableOrderingInsideCase()
    {
        Expression query = Orders
            .Where(o => (o.Discount > 5m ? 1 : 0) == 1)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE CASE WHEN t0."Discount" > 5 THEN 1 ELSE 0 END = 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsNullableOrderingWithCoalesce()
    {
        Expression query = Orders.Select(o => o.Discount > 5m).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(t0."Discount" > 5, FALSE)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsNullableLessOrEqualWithCoalesce()
    {
        Expression query = Orders.Select(o => o.Priority <= 2).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(t0."Priority" <= 2, FALSE)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsNegatedNullableOrderingWithCoalesce()
    {
        Expression query = Orders.Select(o => !(o.Priority <= 2)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT NOT COALESCE(t0."Priority" <= 2, FALSE)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsNullSafeEqualityWithoutCoalesce()
    {
        Expression query = Orders.Select(o => o.Priority == 2).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Priority" IS NOT DISTINCT FROM 2
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsNullCheckWithoutCoalesce()
    {
        Expression query = Orders.Select(o => o.Priority == null).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Priority" IS NULL
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void PropagatesNullThroughNullableArithmetic()
    {
        Expression query = Orders.Select(o => (o.Priority * 2) + 1).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT (t0."Priority" * 2) + 1
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TreatsNullAsEmptyInStringConcatenation()
    {
        Expression query = Orders.Select(o => "[" + o.Note + "]").Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT '[' || COALESCE(t0."Note", '') || ']'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void KeepsNullInStringFunctions()
    {
        Expression query = Orders.Select(o => o.Note!.ToUpper()).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT UPPER(t0."Note")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void KeepsNullableColumnWithoutCoalesceWhenProjected()
    {
        Expression query = Orders.Select(o => o.Note).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Note"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsBooleanConstantsNextToNullableColumn()
    {
        Expression query = Orders
            .Select(o => new
            {
                o.IsGift,
                Yes = true,
                No = false,
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."IsGift", TRUE AS "Yes", FALSE AS "No"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SumsNothingAsZero()
    {
        Expression query = Orders.Where(o => false).Executed(q => q.Sum(o => o.Total));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(SUM(t0."Total"), 0)
                FROM "Order" AS t0
                WHERE FALSE
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void LeavesAverageOfNothingNull()
    {
        Expression query = Orders
            .Where(o => false)
            .Executed(q => q.Average(o => o.Total));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT AVG(t0."Total")
                FROM "Order" AS t0
                WHERE FALSE
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void LeavesMinimumOfNothingNull()
    {
        Expression query = Orders.Where(o => false).Executed(q => q.Min(o => o.Total));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT MIN(t0."Total")
                FROM "Order" AS t0
                WHERE FALSE
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CountsNothingAsZero()
    {
        Expression query = Orders.Where(o => false).Executed(q => q.Count());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COUNT(*) AS integer)
                FROM "Order" AS t0
                WHERE FALSE
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ChecksAllOfNothingAsTrue()
    {
        Expression query = Orders.Where(o => false).Executed(q => q.All(o => o.IsPaid));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT NOT EXISTS (
                    SELECT 1
                    FROM "Order" AS t0
                    WHERE FALSE AND NOT t0."IsPaid"
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SortsNullsFirstForAscendingNullableKey()
    {
        Expression query = Orders.OrderBy(o => o.Discount).Select(o => o.Id).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                ORDER BY t0."Discount" NULLS FIRST
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SortsNullsLastForDescendingNullableKey()
    {
        Expression query = Orders
            .OrderByDescending(o => o.Discount)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                ORDER BY t0."Discount" DESC NULLS LAST
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SortsNonNullableKeyWithoutNullPlacement()
    {
        Expression query = Orders.OrderBy(o => o.Total).Select(o => o.Id).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                ORDER BY t0."Total"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SortsCoalescedKeyWithoutNullPlacement()
    {
        Expression query = Orders
            .OrderBy(o => o.Discount ?? 0m)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                ORDER BY COALESCE(t0."Discount", 0)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void GroupsNullKeysTogether()
    {
        Expression query = Orders
            .GroupBy(o => o.Note)
            .Select(g => new { g.Key, Count = g.Count() })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Note" AS "Key", CAST(COUNT(*) AS integer) AS "Count"
                FROM "Order" AS t0
                GROUP BY t0."Note"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void DistinctTreatsNullsAsEqual()
    {
        Expression query = Orders.Select(o => o.Note).Distinct().Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT DISTINCT t0."Note"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    // Join never matches a null key, in C# (Lookup.CreateForJoin skips them) as in SQL.
    [Fact]
    public void JoinsNeverMatchNullKeysLikeCSharp()
    {
        Expression query = Orders
            .Join(Orders, a => a.Priority, b => b.Priority, (a, b) => a.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                INNER JOIN "Order" AS t1 ON t0."Priority" = t1."Priority"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
