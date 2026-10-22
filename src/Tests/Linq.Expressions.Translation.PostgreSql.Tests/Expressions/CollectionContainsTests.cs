#pragma warning disable IDE0004 // the cast is the subject

namespace Linq.Expressions.Translation.PostgreSql.Tests.Expressions;

public sealed record CollectionContainsTests
{
    [Fact]
    public void FiltersByIntegerArray()
    {
        int[] ids = [1, 2, 3];

        Expression query = Orders
            .Where(o => ids.Contains(o.Id))
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
    public void FiltersByIntegerList()
    {
        List<int> ids = [1, 2, 3];

        Expression query = Orders
            .Where(o => ids.Contains(o.Id))
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
    public void FiltersByIntegerHashSet()
    {
        HashSet<int> ids = [1, 2, 3];

        Expression query = Orders
            .Where(o => ids.Contains(o.Id))
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
    public void FiltersByEnumerableInterface()
    {
        IEnumerable<int> ids = [4, 5];

        Expression query = Orders
            .Where(o => ids.Contains(o.Id))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Id" IN (4, 5)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByReadOnlyList()
    {
        IReadOnlyList<int> ids = [4, 5];

        Expression query = Orders
            .Where(o => ids.Contains(o.Id))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Id" IN (4, 5)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByInlineArray()
    {
        Expression query = Orders
            .Where(o => new[] { 1, 2, 3 }.Contains(o.Id))
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
    public void FiltersBySingleElement()
    {
        Expression query = Orders
            .Where(o => new[] { 7 }.Contains(o.Id))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Id" IN (7)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByStaticEnumerableContains()
    {
        int[] ids = [1, 2, 3];

        Expression query = Orders
            .Where(o => Enumerable.Contains(ids, o.Id))
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
    public void KeepsDuplicatesAndOrderOfElements()
    {
        int[] ids = [3, 1, 3];

        Expression query = Orders
            .Where(o => ids.Contains(o.Id))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Id" IN (3, 1, 3)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByStringArray()
    {
        string[] names = ["A-1", "B-2"];

        Expression query = Orders
            .Where(o => names.Contains(o.Number))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Number" IN ('A-1', 'B-2')
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void EscapesQuotesInStringElements()
    {
        string[] names = ["O'Brien", "x"];

        Expression query = Orders
            .Where(o => names.Contains(o.Number))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Number" IN ('O''Brien', 'x')
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByLongArray()
    {
        long[] points = [1L, 5000000000L];

        Expression query = Orders
            .Where(o => points.Contains(o.Points))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Points" IN (1, 5000000000)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByDecimalList()
    {
        List<decimal> totals = [1.5m, 10m];

        Expression query = Orders
            .Where(o => totals.Contains(o.Total))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Total" IN (1.5, 10)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByGuidArray()
    {
        Guid[] ids = [Guid.Parse("11111111-2222-3333-4444-555555555555"), Guid.Empty];

        Expression query = Orders
            .Where(o => ids.Contains(o.TrackingId))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."TrackingId" IN (UUID '11111111-2222-3333-4444-555555555555',
                UUID '00000000-0000-0000-0000-000000000000')
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByDateTimeArray()
    {
        DateTime[] dates = [new DateTime(2024, 1, 1), new DateTime(2024, 6, 1, 12, 0, 0)];

        Expression query = Orders
            .Where(o => dates.Contains(o.CreatedAt))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."CreatedAt" IN (TIMESTAMP '2024-01-01 00:00:00',
                TIMESTAMP '2024-06-01 12:00:00')
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByBooleanArray()
    {
        bool[] flags = [true];

        Expression query = Orders
            .Where(o => flags.Contains(o.IsPaid))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."IsPaid" IN (TRUE)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByNullableIntegerListWithColumn()
    {
        int?[] ids = [1, 2];

        Expression query = Orders
            .Where(o => ids.Contains(o.Priority))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Priority" IN (1, 2)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByValueOfNullableColumn()
    {
        int[] ids = [1, 2];

        Expression query = Orders
            .Where(o => ids.Contains(o.Priority!.Value))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Priority" IN (1, 2)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AddsNullCheckForNullElement()
    {
        string?[] names = ["a", null];

        Expression query = Orders
            .Where(o => names.Contains(o.Note))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."Note" IN ('a')) OR (t0."Note" IS NULL)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AddsNullCheckForNullElementInTheMiddle()
    {
        int?[] ids = [1, null, 3];

        Expression query = Orders
            .Where(o => ids.Contains(o.Priority))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."Priority" IN (1, 3)) OR (t0."Priority" IS NULL)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ReturnsFalseForEmptyArray()
    {
        int[] ids = [];

        Expression query = Orders
            .Where(o => ids.Contains(o.Id))
            .Select(o => o.Id)
            .Expression;

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
    public void ReturnsFalseForEmptyList()
    {
        List<int> ids = [];

        Expression query = Orders
            .Where(o => ids.Contains(o.Id))
            .Select(o => o.Id)
            .Expression;

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
    public void NegatesContainsOfEmptyArray()
    {
        int[] ids = [];

        Expression query = Orders
            .Where(o => !ids.Contains(o.Id))
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

    [Fact]
    public void NegatesContains()
    {
        int[] ids = [1, 2, 3];

        Expression query = Orders
            .Where(o => !ids.Contains(o.Id))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE NOT (t0."Id" IN (1, 2, 3))
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CombinesContainsWithComparison()
    {
        int[] ids = [1, 2];

        Expression query = Orders
            .Where(o => ids.Contains(o.CustomerId) && (o.Total > 100m))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."CustomerId" IN (1, 2)) AND (t0."Total" > 100)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CombinesTwoContains()
    {
        int[] ids = [1, 2];
        string[] names = ["A-1"];

        Expression query = Orders
            .Where(o => ids.Contains(o.CustomerId) || names.Contains(o.Number))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."CustomerId" IN (1, 2)) OR (t0."Number" IN ('A-1'))
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByContainsOfComputedValue()
    {
        int[] ids = [2, 3];

        Expression query = Orders
            .Where(o => ids.Contains(o.Quantity + 1))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."Quantity" + 1) IN (2, 3)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByContainsOfStringFunction()
    {
        string[] names = ["A", "B"];

        Expression query = Orders
            .Where(o => names.Contains(o.Number.ToUpper()))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE UPPER(t0."Number") IN ('A', 'B')
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByContainsOfDatePart()
    {
        int[] years = [2023, 2024];

        Expression query = Orders
            .Where(o => years.Contains(o.CreatedAt.Year))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE CAST(EXTRACT(YEAR FROM t0."CreatedAt") AS integer) IN (2023, 2024)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByContainsOfCastColumn()
    {
        long[] ids = [1L, 2L];

        Expression query = Orders
            .Where(o => ids.Contains((long)o.Quantity))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE CAST(t0."Quantity" AS bigint) IN (1, 2)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByContainsInRecordMember()
    {
        Filter filter = new([8, 9]);

        Expression query = Orders
            .Where(o => filter.Ids.Contains(o.Id))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Id" IN (8, 9)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByContainsInCapturedMemberChain()
    {
        Holder holder = new(new Filter([8, 9]));

        Expression query = Orders
            .Where(o => holder.Filter.Ids.Contains(o.Id))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Id" IN (8, 9)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsContainsAsBooleanValue()
    {
        int[] ids = [1, 2];

        Expression query = Orders.Select(o => ids.Contains(o.Id)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id" IN (1, 2)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsContainsNextToColumn()
    {
        int[] ids = [1, 2];

        Expression query = Orders
            .Select(o => new { o.Id, Listed = ids.Contains(o.Id) })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Id" IN (1, 2) AS "Listed"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsConditionalOnContains()
    {
        int[] ids = [1, 2];

        Expression query = Orders.Select(o => ids.Contains(o.Id) ? 1 : 0).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CASE WHEN t0."Id" IN (1, 2) THEN 1 ELSE 0 END
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByArrayColumnContains()
    {
        Expression query = Orders
            .Where(o => o.Tags.Contains("vip"))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE 'vip' = ANY (t0."Tags")
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByArrayColumnContainsColumn()
    {
        Expression query = Orders
            .Where(o => o.Tags.Contains(o.Number))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Number" = ANY (t0."Tags")
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByArrayColumnContainsCapturedValue()
    {
        string tag = "vip";

        Expression query = Orders
            .Where(o => o.Tags.Contains(tag))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE 'vip' = ANY (t0."Tags")
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByArrayColumnNotContains()
    {
        Expression query = Orders
            .Where(o => !o.Tags.Contains("vip"))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE NOT ('vip' = ANY (t0."Tags"))
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByArrayColumnLength()
    {
        Expression query = Orders
            .Where(o => o.Tags.Length > 2)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE CARDINALITY(t0."Tags") > 2
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByEmptyArrayColumn()
    {
        Expression query = Orders
            .Where(o => o.Tags.Length == 0)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE CARDINALITY(t0."Tags") = 0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CombinesArrayContainsWithOtherPredicate()
    {
        Expression query = Orders
            .Where(o => o.Tags.Contains("vip") && o.IsPaid)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE ('vip' = ANY (t0."Tags")) AND t0."IsPaid"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsArrayColumn()
    {
        Expression query = Orders.Select(o => o.Tags).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Tags"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsArrayLength()
    {
        Expression query = Orders.Select(o => o.Tags.Length).Expression;

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
    public void SelectsArrayContainsAsBooleanValue()
    {
        Expression query = Orders.Select(o => o.Tags.Contains("vip")).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT 'vip' = ANY (t0."Tags")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OrdersByArrayLength()
    {
        Expression query = Orders
            .OrderByDescending(o => o.Tags.Length)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                ORDER BY CARDINALITY(t0."Tags") DESC
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CountsTaggedOrdersPerCustomer()
    {
        Expression query = Orders
            .Where(o => o.Tags.Contains("vip"))
            .GroupBy(o => o.CustomerId)
            .Select(g => new { CustomerId = g.Key, Count = g.Count() })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId", CAST(COUNT(*) AS integer) AS "Count"
                FROM "Order" AS t0
                WHERE 'vip' = ANY (t0."Tags")
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersCustomersByIdList()
    {
        int[] ids = [1, 2, 3];

        Expression query = Customers
            .Where(c => ids.Contains(c.Id))
            .Select(c => c.Name)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name"
                FROM "Customer" AS t0
                WHERE t0."Id" IN (1, 2, 3)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CountsRowsInList()
    {
        int[] ids = [1, 2];

        Expression query = Orders.Executed(q => q.Count(o => ids.Contains(o.Id)));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COUNT(*) AS integer)
                FROM "Order" AS t0
                WHERE t0."Id" IN (1, 2)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ChecksAnyRowInList()
    {
        int[] ids = [1, 2];

        Expression query = Orders.Executed(q => q.Any(o => ids.Contains(o.Id)));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT EXISTS (
                    SELECT 1
                    FROM "Order" AS t0
                    WHERE t0."Id" IN (1, 2)
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void MatchesNullWhenTheListContainsOnlyNull()
    {
        int?[] priorities = [null];

        Expression query = Orders
            .Where(o => priorities.Contains(o.Priority))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE FALSE OR (t0."Priority" IS NULL)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void KeepsNullCheckWhenANonNullableValueIsInANullableList()
    {
        int?[] ids = [1, null];

        Expression query = Orders
            .Where(o => ids.Contains(o.Id))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."Id" IN (1)) OR (t0."Id" IS NULL)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    public sealed record Filter(int[] Ids);

    public sealed record Holder(Filter Filter);

    [Fact]
    public void SelectsMembershipOfNullableColumnWithCoalesce()
    {
        Expression query = Orders
            .Select(o => new int?[] { 1, 2 }.Contains(o.Priority))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(t0."Priority" IN (1, 2), FALSE)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByMembershipOfNullableColumnWithoutCoalesce()
    {
        Expression query = Orders
            .Where(o => new int?[] { 1, 2 }.Contains(o.Priority))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Priority" IN (1, 2)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsNegatedMembershipOfNullableColumn()
    {
        Expression query = Orders
            .Select(o => !new int?[] { 1, 2 }.Contains(o.Priority))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT NOT COALESCE(t0."Priority" IN (1, 2), FALSE)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void UsesMembershipOfNullableColumnAsCaseCondition()
    {
        Expression query = Orders
            .Select(o => new int?[] { 1, 2 }.Contains(o.Priority) ? 1 : 0)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CASE WHEN t0."Priority" IN (1, 2) THEN 1 ELSE 0 END
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsArrayMembershipOfNullableColumnWithCoalesce()
    {
        Expression query = Orders.Select(o => o.Tags.Contains(o.Note)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(t0."Note" = ANY (t0."Tags"), FALSE)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
