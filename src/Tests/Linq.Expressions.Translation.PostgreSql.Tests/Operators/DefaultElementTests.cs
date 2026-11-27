namespace Linq.Expressions.Translation.PostgreSql.Tests.Operators;

// DefaultIfEmpty as a sequence operator and the default given to FirstOrDefault,
// LastOrDefault and SingleOrDefault: one row left joined to the rows, which reads the
// default where there is none.
public sealed record DefaultElementTests
{
    // #141: a value that is never NULL in a row is the default where it is NULL.
    [Fact]
    public void SubstitutesGivenDefaultOfRequiredValue()
    {
        Expression query = Orders
            .Where(o => o.IsPaid)
            .Select(o => o.Quantity)
            .DefaultIfEmpty(-1)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(t1."Quantity", -1)
                FROM (SELECT 1) AS t2
                LEFT JOIN (SELECT t0."Quantity" FROM "Order" AS t0 WHERE t0."IsPaid") AS t1 ON TRUE
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    // #141: a value that may be NULL in a row is told from the absence of a row by a
    // column that is TRUE in every row.
    [Fact]
    public void TellsNullValueFromAbsentRowForGivenDefault()
    {
        Expression query = Orders
            .OrderBy(o => o.Id)
            .Select(o => o.Note)
            .DefaultIfEmpty("none")
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CASE WHEN t1."$present" THEN t1."Note" ELSE 'none' END
                FROM (SELECT 1) AS t2
                LEFT JOIN (SELECT t0."Note", TRUE AS "$present", t0."Id" AS "$1" FROM "Order" AS t0) AS t1 ON TRUE
                ORDER BY t1."$1"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    // #141: FirstOrDefault(defaultValue) is the first row, or the default.
    [Fact]
    public void TakesFirstOrGivenDefault()
    {
        Expression query = Orders
            .OrderBy(o => o.Id)
            .Select(o => o.Quantity)
            .Executed(q => q.FirstOrDefault(-1));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(t1."Quantity", -1)
                FROM (SELECT 1) AS t2
                LEFT JOIN (SELECT t0."Quantity" FROM "Order" AS t0 ORDER BY t0."Id" LIMIT 1) AS t1 ON TRUE
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    // #141: LastOrDefault with a predicate and a default, in a lambda.
    [Fact]
    public void TakesLastMatchOrGivenDefaultPerRow()
    {
        Expression query = Customers
            .Select(c =>
                Orders
                    .OrderBy(o => o.Id)
                    .Select(o => o.CustomerId)
                    .LastOrDefault(id => id == c.Id, -1)
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT (SELECT COALESCE(t2."CustomerId", -1) FROM (SELECT 1) AS t3 LEFT JOIN (SELECT t1."CustomerId" FROM "Order" AS t1 WHERE t1."CustomerId" = t0."Id" ORDER BY t1."Id" DESC LIMIT 1) AS t2 ON TRUE)
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    // #141: SingleOrDefault(defaultValue) reads two rows, so that more than one is
    // seen.
    [Fact]
    public void TakesSingleOrGivenDefault()
    {
        Expression query = Customers
            .Select(c => c.Name)
            .Executed(q => q.SingleOrDefault("none"));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(t1."Name", 'none')
                FROM (SELECT 1) AS t2
                LEFT JOIN (SELECT t0."Name" FROM "Customer" AS t0 LIMIT 2) AS t1 ON TRUE
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    // ElementAt of an Index counted from the start is ElementAt of its value.
    [Fact]
    public void TakesElementAtIndexFromStart()
    {
        Expression query = Customers
            .OrderBy(c => c.Id)
            .Select(c => c.Id)
            .Executed(q => q.ElementAt(new Index(2)));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Customer" AS t0
                ORDER BY t0."Id"
                LIMIT 1
                OFFSET 2
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
