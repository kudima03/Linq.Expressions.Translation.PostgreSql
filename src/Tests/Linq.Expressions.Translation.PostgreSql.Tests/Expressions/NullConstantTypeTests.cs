namespace Linq.Expressions.Translation.PostgreSql.Tests.Expressions;

// #152: a NULL is of no type. PostgreSQL types it from what stands beside it (the
// other operand of an operator, another branch of a CASE, the other values of
// COALESCE); where nothing does, it takes the NULL for a text, or finds no function
// for it: SUM(NULL) is not unique, CASE WHEN ... THEN NULL ELSE NULL END >= 0
// compares a text with an integer. There the NULL is written with the type of its
// expression, as a constant of that type that is not null types itself.
public sealed record NullConstantTypeTests
{
    [Fact]
    public void SumsANullCapturedValueAsItsType()
    {
        long? none = null;

        Expression query = Orders.Executed(q => q.Sum(o => none));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COALESCE(SUM(CAST(NULL AS bigint)), 0) AS bigint)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesAConditionalOfANullCapturedValueAsItsType()
    {
        long? none = null;

        Expression query = Orders.Select(o => (o.IsPaid ? none : null) >= 0).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(CASE WHEN t0."IsPaid" THEN CAST(NULL AS bigint) ELSE NULL END >= 0, FALSE)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    private static Dictionary<string, (Expression Query, string Sql)> Aggregated()
    {
        int? number = null;
        long? none = null;
        decimal? money = null;
        double? real = null;
        bool? flag = null;
        string? text = null;
        DateTime? moment = null;
        return new()
        {
            ["sum of int"] = (
                Orders.Executed(q => q.Sum(o => number)),
                "CAST(COALESCE(SUM(CAST(NULL AS integer)), 0) AS integer)"
            ),
            ["sum of decimal"] = (
                Orders.Executed(q => q.Sum(o => money)),
                "COALESCE(SUM(CAST(NULL AS numeric)), 0)"
            ),
            ["sum of double"] = (
                Orders.Executed(q => q.Sum(o => real)),
                "COALESCE(SUM(CAST(NULL AS double precision)), 0)"
            ),
            ["average of long"] = (
                Orders.Executed(q => q.Average(o => none)),
                "CAST(AVG(CAST(NULL AS bigint)) AS double precision)"
            ),
            ["average of double"] = (
                Orders.Executed(q => q.Average(o => real)),
                "AVG(CAST(NULL AS double precision))"
            ),
            ["average of decimal"] = (
                Orders.Executed(q => q.Average(o => money)),
                "TRIM_SCALE(ROUND(AVG(ROUND(CAST(NULL AS numeric), 30)), 29 - LENGTH(CAST(TRUNC(ABS(AVG(ROUND(CAST(NULL AS numeric), 30))) / 0.79228162514264337593543950335) AS text))))"
            ),
            ["max of long"] = (
                Orders.Executed(q => q.Max(o => none)),
                "MAX(CAST(NULL AS bigint))"
            ),
            // -NULL is of no type either: the operator - is not unique.
            ["max of double"] = (
                Orders.Executed(q => q.Max(o => real)),
                "-MIN(-CAST(NULL AS double precision))"
            ),
            ["min of bool"] = (
                Orders.Executed(q => q.Min(o => flag)),
                "BOOL_AND(CAST(NULL AS boolean))"
            ),
            ["min of string"] = (
                Orders.Executed(q => q.Min(o => text)),
                "MIN(CAST(NULL AS text))"
            ),
            ["max of DateTime"] = (
                Orders.Executed(q => q.Max(o => moment)),
                "MAX(CAST(NULL AS timestamp))"
            ),
        };
    }

    public static TheoryData<string> Aggregates => [.. Aggregated().Keys];

    [Theory]
    [MemberData(nameof(Aggregates))]
    public void AggregatesANullAsItsType(string aggregate)
    {
        (Expression query, string sql) = Aggregated()[aggregate];

        Assert.Equal(
            Sql.Valid(
                $"""
                SELECT {sql}
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SumsANullOfAGroupAsItsType()
    {
        long? none = null;

        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new
            {
                Sum = g.Sum(o => none),
                Paid = g.Where(o => o.IsPaid).Average(o => none),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COALESCE(SUM(CAST(NULL AS bigint)), 0) AS bigint) AS "Sum", CAST(AVG(CAST(NULL AS bigint)) FILTER (WHERE t0."IsPaid") AS double precision) AS "Paid"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SumsANullOfASubqueryAsItsType()
    {
        long? none = null;

        Expression query = Customers
            .Select(c => Orders.Where(o => o.CustomerId == c.Id).Sum(o => none))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT (
                    SELECT CAST(COALESCE(SUM(CAST(NULL AS bigint)), 0) AS bigint)
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                )
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SumsANullOverEveryRowAsItsType()
    {
        long? none = null;

        Expression query = Orders
            .Select(o => new { o.Id, Sum = Orders.Sum(x => none) })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", CAST(COALESCE(SUM(CAST(NULL AS bigint)) OVER (), 0) AS bigint) AS "Sum"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AggregatesANullOverAFrameAsItsType()
    {
        long? none = null;

        Expression query = Trees
            .Over(
                Orders,
                null,
                rows => rows.OrderBy(o => o.Id),
                PostgreSqlFrame.Rows(-1, 0),
                (o, w) =>
                    new { Sum = w.Rows.Sum(x => none), Mean = w.Rows.Average(x => none) }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COALESCE(SUM(CAST(NULL AS bigint)) OVER (ORDER BY t0."Id" ROWS BETWEEN 1 PRECEDING AND CURRENT ROW), 0) AS bigint) AS "Sum", CAST(AVG(CAST(NULL AS bigint)) OVER (ORDER BY t0."Id" ROWS BETWEEN 1 PRECEDING AND CURRENT ROW) AS double precision) AS "Mean"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    // LAG and FIRST_VALUE are polymorphic: PostgreSQL cannot determine their type
    // from a NULL.
    [Fact]
    public void ReadsANullOfAnotherRowOfAWindowAsItsType()
    {
        long? none = null;
        string? text = null;

        Expression query = Trees
            .Over(
                Orders,
                null,
                rows => rows.OrderBy(o => o.Id),
                null,
                (o, w) =>
                    new
                    {
                        First = w.FirstValue(x => none),
                        Last = w.LastValue(x => text),
                        Previous = w.Lag(x => none, 1, none),
                        Next = w.Lead(x => none, 1, o.Points),
                    }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT FIRST_VALUE(CAST(NULL AS bigint)) OVER (ORDER BY t0."Id") AS "First", LAST_VALUE(CAST(NULL AS text)) OVER (ORDER BY t0."Id") AS "Last", LAG(CAST(NULL AS bigint), 1) OVER (ORDER BY t0."Id") AS "Previous", LEAD(CAST(NULL AS bigint), 1, t0."Points") OVER (ORDER BY t0."Id") AS "Next"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    private static readonly Dictionary<string, (Expression Query, string Type)> Typed =
        new()
        {
            ["int"] = (
                Orders.Select(o => o.IsPaid ? (int?)null : null).Expression,
                "integer"
            ),
            ["short"] = (
                Orders.Select(o => o.IsPaid ? (short?)null : null).Expression,
                "smallint"
            ),
            ["ulong"] = (
                Orders.Select(o => o.IsPaid ? (ulong?)null : null).Expression,
                "numeric"
            ),
            ["decimal"] = (
                Orders.Select(o => o.IsPaid ? (decimal?)null : null).Expression,
                "numeric"
            ),
            ["double"] = (
                Orders.Select(o => o.IsPaid ? (double?)null : null).Expression,
                "double precision"
            ),
            ["float"] = (
                Orders.Select(o => o.IsPaid ? (float?)null : null).Expression,
                "real"
            ),
            ["bool"] = (
                Orders.Select(o => o.IsPaid ? (bool?)null : null).Expression,
                "boolean"
            ),
            ["string"] = (
                Orders.Select(o => o.IsPaid ? (string?)null : null).Expression,
                "text"
            ),
            ["char"] = (
                Orders.Select(o => o.IsPaid ? (char?)null : null).Expression,
                "text"
            ),
            ["Guid"] = (
                Orders.Select(o => o.IsPaid ? (Guid?)null : null).Expression,
                "uuid"
            ),
            ["DateTime"] = (
                Orders.Select(o => o.IsPaid ? (DateTime?)null : null).Expression,
                "timestamp"
            ),
            ["DateTimeOffset"] = (
                Orders.Select(o => o.IsPaid ? (DateTimeOffset?)null : null).Expression,
                "timestamptz"
            ),
            ["DateOnly"] = (
                Orders.Select(o => o.IsPaid ? (DateOnly?)null : null).Expression,
                "date"
            ),
            ["TimeOnly"] = (
                Orders.Select(o => o.IsPaid ? (TimeOnly?)null : null).Expression,
                "time"
            ),
            ["TimeSpan"] = (
                Orders.Select(o => o.IsPaid ? (TimeSpan?)null : null).Expression,
                "interval"
            ),
            ["enum"] = (
                Orders.Select(o => o.IsPaid ? (OrderStatus?)null : null).Expression,
                "integer"
            ),
            ["byte[]"] = (
                Orders.Select(o => o.IsPaid ? (byte[]?)null : null).Expression,
                "bytea"
            ),
            ["string[]"] = (
                Orders.Select(o => o.IsPaid ? (string[]?)null : null).Expression,
                "text[]"
            ),
            ["long[]"] = (
                Orders.Select(o => o.IsPaid ? (long[]?)null : null).Expression,
                "bigint[]"
            ),
        };

    public static TheoryData<string> Conditionals => [.. Typed.Keys];

    [Theory]
    [MemberData(nameof(Conditionals))]
    public void TypesAConditionalWhoseBranchesAreAllNull(string type)
    {
        (Expression query, string name) = Typed[type];

        Assert.Equal(
            Sql.Valid(
                $"""
                SELECT CASE WHEN t0."IsPaid" THEN CAST(NULL AS {name}) ELSE NULL END
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    // One typed branch types the CASE.
    [Fact]
    public void TypesOneBranchOfANestedConditionalOfNulls()
    {
        long? none = null;

        Expression query = Orders
            .Where(o =>
                (
                    o.IsPaid ? none
                    : o.IsGift == true ? none
                    : null
                ) >= 0
            )
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE CASE WHEN t0."IsPaid" THEN NULL WHEN t0."IsGift" IS NOT DISTINCT FROM TRUE THEN CAST(NULL AS bigint) ELSE NULL END >= 0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AggregatesAConditionalOfNullsAsItsType()
    {
        long? none = null;

        Expression query = Orders.Executed(q => q.Sum(o => o.IsPaid ? none : null));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COALESCE(SUM(CASE WHEN t0."IsPaid" THEN CAST(NULL AS bigint) ELSE NULL END), 0) AS bigint)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    // A null of a type PostgreSQL has no name for (an object, an entity) stays a
    // bare NULL, which is all that is read of it.
    [Fact]
    public void LeavesAConditionalOfNullsOfNoSqlTypeUntyped()
    {
        Expression query = Orders
            .Select(o => (o.IsPaid ? (object?)null : null) == null)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CASE WHEN t0."IsPaid" THEN NULL ELSE NULL END IS NULL
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    private static Dictionary<string, (Expression Query, string Sql)> Beside()
    {
        long? none = null;
        int? number = null;
        return new()
        {
            ["first branch"] = (
                Orders.Select(o => o.IsPaid ? none : o.Points).Expression,
                """CASE WHEN t0."IsPaid" THEN NULL ELSE t0."Points" END"""
            ),
            ["last branch"] = (
                Orders.Select(o => o.IsPaid ? o.Points : none).Expression,
                """CASE WHEN t0."IsPaid" THEN t0."Points" ELSE NULL END"""
            ),
            ["middle branch"] = (
                Orders
                    .Select(o =>
                        o.IsPaid ? number
                        : o.IsGift == true ? 1
                        : null
                    )
                    .Expression,
                """CASE WHEN t0."IsPaid" THEN NULL WHEN t0."IsGift" IS NOT DISTINCT FROM TRUE THEN 1 ELSE NULL END"""
            ),
            ["coalesce"] = (
                Orders.Select(o => none ?? o.Points).Expression,
                """COALESCE(NULL, t0."Points")"""
            ),
            ["comparison"] = (
                Orders.Select(o => none >= o.Points).Expression,
                """COALESCE(NULL >= t0."Points", FALSE)"""
            ),
        };
    }

    public static TheoryData<string> TypedBesideIt => [.. Beside().Keys];

    // A value beside the NULL types it: it is written as it was.
    [Theory]
    [MemberData(nameof(TypedBesideIt))]
    public void LeavesANullUntypedWhereAValueBesideItTypesIt(string position)
    {
        (Expression query, string sql) = Beside()[position];

        Assert.Equal(
            Sql.Valid(
                $"""
                SELECT {sql}
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    // Only a NULL is typed: a constant that is not null is of its own type.
    [Fact]
    public void LeavesAConstantThatIsNotNullAsItIs()
    {
        Expression query = Trees
            .Over(
                Orders,
                null,
                rows => rows.OrderBy(o => o.Id),
                null,
                (o, w) =>
                    new
                    {
                        Sum = w.Rows.Sum(x => 1L),
                        First = w.FirstValue(x => 1L),
                        Negated = -(o.IsPaid ? 1L : 2L),
                    }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COALESCE(SUM(1) OVER (ORDER BY t0."Id"), 0) AS bigint) AS "Sum", FIRST_VALUE(1) OVER (ORDER BY t0."Id") AS "First", -CASE WHEN t0."IsPaid" THEN 1 ELSE 2 END AS "Negated"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    // -NULL and ~NULL are of no type: the operators are not unique.
    [Fact]
    public void NegatesANullProjectedBeforeAsItsType()
    {
        long? none = null;
        bool? flag = null;

        Expression query = Orders
            .Select(o => new { None = none, Flag = flag })
            .Select(x => new
            {
                Negated = -x.None,
                Complement = ~x.None,
                Not = !x.Flag,
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT -CAST(NULL AS bigint) AS "Negated", ~CAST(NULL AS bigint) AS "Complement", NOT CAST(NULL AS boolean) AS "Not"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    // The Value of a null captured nullable fails to evaluate, so it is read
    // through a subquery where a row reaches it: (SELECT NULL) is a text, of which
    // there is no ABS.
    [Fact]
    public void ReadsADeferredNullAsItsType()
    {
        long? none = null;

        Expression query = Orders
            .Select(o => o.IsPaid ? Math.Abs(none!.Value) : 0)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CASE WHEN t0."IsPaid" THEN ABS((SELECT CAST(NULL AS bigint))) ELSE 0 END
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
