#pragma warning disable xUnit1004 // skipped tests record SQL the translator does not produce yet

namespace Linq.Expressions.Translation.PostgreSql.Tests.Operators;

public sealed record CastAndDefaultSemanticsTests
{
    [Fact(Skip = "Queryable.Cast to a different scalar type is ignored.")]
    public void CastsScalarSequenceToWiderInteger()
    {
        Expression query = Customers.Select(c => c.Id).Cast<long>().Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(t0."Id" AS bigint)
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "OfType of an impossible scalar type keeps every row.")]
    public void DropsRowsWhoseRuntimeTypeIsNotTheTarget()
    {
        Expression query = Customers.Select(c => c.Id).OfType<long>().Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Customer" AS t0
                WHERE FALSE
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "OfType of an unrelated entity keeps every row.")]
    public void DropsRowsOfAnUnrelatedEntity()
    {
        Expression query = Customers.OfType<Employee>().Select(e => e.Id).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Customer" AS t0
                WHERE FALSE
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "FirstOrDefault of a non-nullable int does not substitute zero.")]
    public void SubstitutesZeroForMissingFirstInteger()
    {
        Expression query = Customers
            .Select(c => new
            {
                c.Name,
                FirstId = Orders
                    .Where(o => o.CustomerId == c.Id)
                    .OrderBy(o => o.Id)
                    .Select(o => o.Id)
                    .FirstOrDefault(),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", COALESCE((
                    SELECT t1."Id"
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                    ORDER BY t1."Id"
                    LIMIT 1
                ), 0) AS "FirstId"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "FirstOrDefault of bool does not substitute false.")]
    public void SubstitutesFalseForMissingFirstBoolean()
    {
        Expression query = Customers
            .Select(c => new
            {
                c.Name,
                Paid = Orders
                    .Where(o => o.CustomerId == c.Id)
                    .OrderBy(o => o.Id)
                    .Select(o => o.IsPaid)
                    .FirstOrDefault(),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", COALESCE((
                    SELECT t1."IsPaid"
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                    ORDER BY t1."Id"
                    LIMIT 1
                ), FALSE) AS "Paid"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "ElementAtOrDefault of a non-nullable int does not substitute zero.")]
    public void SubstitutesZeroForMissingElement()
    {
        Expression query = Customers
            .Select(c => new
            {
                c.Name,
                ThirdId = Orders
                    .Where(o => o.CustomerId == c.Id)
                    .OrderBy(o => o.Id)
                    .Select(o => o.Id)
                    .ElementAtOrDefault(2),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", COALESCE((
                    SELECT t1."Id"
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                    ORDER BY t1."Id"
                    LIMIT 1 OFFSET 2
                ), 0) AS "ThirdId"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "LastOrDefault of a non-nullable int does not substitute zero.")]
    public void SubstitutesZeroForMissingLastInteger()
    {
        Expression query = Customers
            .Select(c => new
            {
                c.Name,
                LastId = Orders
                    .Where(o => o.CustomerId == c.Id)
                    .OrderBy(o => o.Id)
                    .Select(o => o.Id)
                    .LastOrDefault(),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", COALESCE((
                    SELECT t1."Id"
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                    ORDER BY t1."Id" DESC
                    LIMIT 1
                ), 0) AS "LastId"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "SingleOrDefault of a non-nullable int does not substitute zero.")]
    public void SubstitutesZeroForMissingSingleInteger()
    {
        Expression query = Customers
            .Select(c => new
            {
                c.Name,
                OnlyId = Orders
                    .Where(o => o.CustomerId == c.Id)
                    .Select(o => o.Id)
                    .SingleOrDefault(),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", COALESCE((
                    SELECT t1."Id"
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                    LIMIT 2
                ), 0) AS "OnlyId"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
