namespace Linq.Expressions.Translation.PostgreSql.Tests.Expressions;

// NaN as .NET has it: the operators are false for it (!= true), a sort ranks it below
// every number, and Max and Min are NaN when every value, or any value, is.
public sealed record NotANumberTests
{
    // #132: == of two values that may be NaN is false for NaN, != true.
    [Fact]
    public void ComparesValuesThatMayBeNaNForEquality()
    {
        Expression query = Orders
            .Select(o => new
            {
                Same = o.Weight == o.Weight * 1,
                Other = o.Weight != o.Weight * 1,
                Lifted = (double?)o.Priority * o.Weight == (double?)o.Priority * 2.5,
                Unlike = (double?)o.Priority * o.Weight != (double?)o.Priority * 2.5,
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT (t0."Weight" = (t0."Weight" * 1)) AND (t0."Weight" <> 'NaN') AS "Same", (t0."Weight" <> (t0."Weight" * 1)) OR (t0."Weight" = 'NaN') AS "Other", ((CAST(t0."Priority" AS double precision) * t0."Weight") IS NOT DISTINCT FROM (CAST(t0."Priority" AS double precision) * 2.5)) AND ((CAST(t0."Priority" AS double precision) * t0."Weight") IS DISTINCT FROM 'NaN') AS "Lifted", ((CAST(t0."Priority" AS double precision) * t0."Weight") IS DISTINCT FROM (CAST(t0."Priority" AS double precision) * 2.5)) OR ((CAST(t0."Priority" AS double precision) * t0."Weight") IS NOT DISTINCT FROM 'NaN') AS "Unlike"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    // #132: < and <= are decided by NaN on their right, > and >= on their left; a
    // constant other than NaN on that side needs no guard, and Equals takes NaN for
    // equal to itself, as PostgreSQL does.
    [Fact]
    public void GuardsTheGreaterSideOfAnOrderComparison()
    {
        Expression query = Orders
            .Where(o =>
                o.Weight < o.Weight * 2
                && o.Weight <= 3
                && 3 > o.Weight
                && o.Weight.Equals(o.Weight * 1)
            )
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."Weight" < (t0."Weight" * 2)) AND ((t0."Weight" * 2) <> 'NaN') AND (t0."Weight" <= 3) AND (3 > t0."Weight") AND (t0."Weight" = (t0."Weight" * 1))
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    // #132: NaN sorts before every number ascending and after them descending; a key
    // that may be NULL sorts its nulls before NaN.
    [Fact]
    public void SortsNaNBelowEveryNumber()
    {
        Expression query = Orders
            .OrderBy(o => o.Weight)
            .ThenByDescending(o => o.Weight * 2)
            .ThenBy(o => (double?)o.Priority * o.Weight)
            .ThenByDescending(o => (double?)o.Priority * o.Weight)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                ORDER BY NULLIF(t0."Weight", 'NaN') NULLS FIRST, NULLIF(t0."Weight" * 2, 'NaN') DESC NULLS LAST, (CAST(t0."Priority" AS double precision) * t0."Weight") = 'NaN' DESC NULLS FIRST, CAST(t0."Priority" AS double precision) * t0."Weight" NULLS FIRST, (CAST(t0."Priority" AS double precision) * t0."Weight") = 'NaN' NULLS LAST, CAST(t0."Priority" AS double precision) * t0."Weight" DESC NULLS LAST
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    // #132: Max is NaN only when every value is and Min when one is, the reverse of
    // MAX and MIN, so they are taken of the negated values.
    [Fact]
    public void TakesExtremesOfTheNegatedValues()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new
            {
                Max = g.Max(o => o.Weight),
                Min = g.Where(o => o.IsPaid).Min(o => o.Weight),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT -MIN(-t0."Weight") AS "Max", -MAX(-t0."Weight") FILTER (WHERE t0."IsPaid") AS "Min"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
