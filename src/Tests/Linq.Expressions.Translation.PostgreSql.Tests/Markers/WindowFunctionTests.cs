namespace Linq.Expressions.Translation.PostgreSql.Tests.Markers;

// Every window function of PostgreSqlWindows is PostgreSQL's function of the same
// name over the window of its Over. A function that may find no row (LAG and LEAD
// without a default, NTH_VALUE, FIRST_VALUE and LAST_VALUE of a frame that may be
// empty) is nullable.
public sealed record WindowFunctionTests
{
    public static TheoryData<
        Expression<Func<Order, PostgreSqlWindow<Order>, object>>,
        string
    > Rankings =>
        new()
        {
            { (o, w) => w.RowNumber(), "ROW_NUMBER()" },
            { (o, w) => w.Rank(), "RANK()" },
            { (o, w) => w.DenseRank(), "DENSE_RANK()" },
            { (o, w) => w.PercentRank(), "PERCENT_RANK()" },
            { (o, w) => w.CumeDist(), "CUME_DIST()" },
            { (o, w) => w.Ntile(4), "NTILE(4)" },
            { (o, w) => w.Ntile(o.Quantity), "NTILE(t0.\"Quantity\")" },
        };

    [Theory]
    [MemberData(nameof(Rankings))]
    public void RanksTheRowsOfTheWindow(
        Expression<Func<Order, PostgreSqlWindow<Order>, object>> selector,
        string function
    )
    {
        Expression query = Trees
            .Over(
                Orders,
                o => o.CustomerId,
                rows => rows.OrderBy(o => o.Total),
                null,
                selector
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                $"""
                SELECT {function} OVER (PARTITION BY t0."CustomerId" ORDER BY t0."Total")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    public static TheoryData<
        Expression<Func<Order, PostgreSqlWindow<Order>, object?>>,
        string
    > Offsets =>
        new()
        {
            { (o, w) => w.Lag(x => x.Note), "LAG(t0.\"Note\")" },
            { (o, w) => w.Lag(x => (int?)x.Quantity, 2), "LAG(t0.\"Quantity\", 2)" },
            { (o, w) => w.Lag(x => x.Quantity, 1, 0), "LAG(t0.\"Quantity\", 1, 0)" },
            {
                (o, w) => w.Lag(x => x.Total, 3, o.Total),
                "LAG(t0.\"Total\", 3, t0.\"Total\")"
            },
            { (o, w) => w.Lead(x => x.Priority), "LEAD(t0.\"Priority\")" },
            {
                (o, w) => w.Lead(x => x.Number, o.Quantity),
                "LEAD(t0.\"Number\", t0.\"Quantity\")"
            },
            {
                (o, w) => w.Lead(x => x.Number, 1, "none"),
                "LEAD(t0.\"Number\", 1, 'none')"
            },
            {
                (o, w) => w.Lead(x => x.Priority, -1, null),
                "LEAD(t0.\"Priority\", -1, NULL)"
            },
            {
                (o, w) => w.Lag(x => x.Quantity * 2, 1, -1),
                "LAG(t0.\"Quantity\" * 2, 1, -1)"
            },
            { (o, w) => w.FirstValue(x => x.Id), "FIRST_VALUE(t0.\"Id\")" },
            { (o, w) => w.LastValue(x => x.Note), "LAST_VALUE(t0.\"Note\")" },
            { (o, w) => w.NthValue(x => (int?)x.Id, 2), "NTH_VALUE(t0.\"Id\", 2)" },
            {
                (o, w) => w.NthValue(x => x.Number, o.Quantity),
                "NTH_VALUE(t0.\"Number\", t0.\"Quantity\")"
            },
        };

    [Theory]
    [MemberData(nameof(Offsets))]
    public void ReadsOtherRowsOfTheWindow(
        Expression<Func<Order, PostgreSqlWindow<Order>, object?>> selector,
        string function
    )
    {
        Expression query = Trees
            .Over(Orders, null, rows => rows.OrderBy(o => o.Id), null, selector)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                $"""
                SELECT {function} OVER (ORDER BY t0."Id")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ReadsANonNullableValueInAFrameThatHoldsTheCurrentRow()
    {
        Expression query = Trees
            .Over(
                Orders,
                null,
                rows => rows.OrderBy(o => o.Id),
                PostgreSqlFrame.Rows(-2, 0),
                (o, w) =>
                    new
                    {
                        First = w.FirstValue(x => x.Id),
                        Last = w.LastValue(x => x.Total),
                    }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT FIRST_VALUE(t0."Id") OVER (ORDER BY t0."Id" ROWS BETWEEN 2 PRECEDING AND CURRENT ROW) AS "First", LAST_VALUE(t0."Total") OVER (ORDER BY t0."Id" ROWS BETWEEN 2 PRECEDING AND CURRENT ROW) AS "Last"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ReadsANullableValueInAFrameThatMayBeEmpty()
    {
        Expression query = Trees
            .Over(
                Orders,
                null,
                rows => rows.OrderBy(o => o.Id),
                PostgreSqlFrame.Rows(1, 3),
                (o, w) =>
                    new
                    {
                        First = w.FirstValue(x => (int?)x.Id),
                        Last = w.LastValue(x => x.Note),
                    }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT FIRST_VALUE(t0."Id") OVER (ORDER BY t0."Id" ROWS BETWEEN 1 FOLLOWING AND 3 FOLLOWING) AS "First", LAST_VALUE(t0."Note") OVER (ORDER BY t0."Id" ROWS BETWEEN 1 FOLLOWING AND 3 FOLLOWING) AS "Last"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesAMaybeMissingValueAsNullable()
    {
        Expression query = Trees
            .Over(
                Orders,
                null,
                rows => rows.OrderBy(o => o.Id),
                null,
                (o, w) => new { o.Id, Previous = w.Lag(x => (int?)x.Quantity) }
            )
            .Where(x => x.Previous == 2)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Id", t1."Previous"
                FROM (
                    SELECT t0."Id", LAG(t0."Quantity") OVER (ORDER BY t0."Id") AS "Previous"
                    FROM "Order" AS t0
                ) AS t1
                WHERE t1."Previous" IS NOT DISTINCT
                FROM 2
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComposesFunctionsWithExpressionsOfTheCurrentRow()
    {
        Expression query = Trees
            .Over(
                Orders,
                o => o.CustomerId,
                rows => rows.OrderBy(o => o.CreatedAt).ThenBy(o => o.Id),
                null,
                (o, w) =>
                    new
                    {
                        o.Id,
                        Change = o.Total - w.Lag(x => x.Total, 1, o.Total),
                        Even = w.RowNumber() % 2 == 0,
                        Label = o.Number + "#" + w.RowNumber(),
                    }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Total" - LAG(t0."Total", 1, t0."Total") OVER (PARTITION BY t0."CustomerId" ORDER BY t0."CreatedAt", t0."Id") AS "Change", (ROW_NUMBER() OVER (PARTITION BY t0."CustomerId" ORDER BY t0."CreatedAt", t0."Id") % 2) = 0 AS "Even", t0."Number" || '#' || ROW_NUMBER() OVER (PARTITION BY t0."CustomerId" ORDER BY t0."CreatedAt", t0."Id") AS "Label"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OrdersTheResultByAWindowFunction()
    {
        Expression query = Trees
            .Over(
                Orders,
                null,
                rows => rows.OrderBy(o => o.Total),
                null,
                (o, w) => new { o.Id, Percent = w.PercentRank() }
            )
            .OrderBy(x => x.Percent)
            .ThenBy(x => x.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", PERCENT_RANK() OVER (ORDER BY t0."Total") AS "Percent"
                FROM "Order" AS t0
                ORDER BY PERCENT_RANK() OVER (ORDER BY t0."Total"), t0."Id"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
