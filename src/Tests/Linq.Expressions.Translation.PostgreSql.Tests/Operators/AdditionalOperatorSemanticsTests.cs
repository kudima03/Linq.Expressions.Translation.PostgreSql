#pragma warning disable xUnit1004 // skipped tests record SQL the translator does not produce yet

namespace Linq.Expressions.Translation.PostgreSql.Tests.Operators;

public sealed record AdditionalOperatorSemanticsTests
{
    [Fact(Skip = "TakeLast is not translated.")]
    public void TakesLastRowsInOriginalOrder()
    {
        Expression query = Customers.OrderBy(c => c.Id).TakeLast(2).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Id", t1."Name", t1."Email", t1."Region", t1."IsActive"
                FROM (
                    SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                    FROM "Customer" AS t0
                    ORDER BY t0."Id" DESC
                    LIMIT 2
                ) AS t1
                ORDER BY t1."Id"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "SkipLast is not translated.")]
    public void SkipsLastRows()
    {
        Expression query = Customers.OrderBy(c => c.Id).SkipLast(2).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                ORDER BY t0."Id"
                LIMIT (SELECT GREATEST(COUNT(*) - 2, 0) FROM "Customer" AS t1)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void KeepsIntegersWhenOfTypeIsNullable()
    {
        Expression query = Customers.Select(c => c.Id).OfType<int?>().Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "OfType of an unrelated scalar keeps every row.")]
    public void DropsCustomersFromStringOfType()
    {
        Expression query = Customers.OfType<string>().Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                WHERE FALSE
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "DefaultIfEmpty().Max does not substitute zero for an empty sequence.")]
    public void SubstitutesZeroForMissingMaximum()
    {
        Expression query = Customers
            .Select(c => new
            {
                c.Name,
                Peak = Orders
                    .Where(o => o.CustomerId == c.Id)
                    .Select(o => o.Total)
                    .DefaultIfEmpty()
                    .Max(),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", COALESCE((
                    SELECT MAX(t1."Total")
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                ), 0) AS "Peak"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "DefaultIfEmpty().Min does not substitute zero for an empty sequence.")]
    public void SubstitutesZeroForMissingMinimum()
    {
        Expression query = Customers
            .Select(c => new
            {
                c.Name,
                Lowest = Orders
                    .Where(o => o.CustomerId == c.Id)
                    .Select(o => o.Quantity)
                    .DefaultIfEmpty()
                    .Min(),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", COALESCE((
                    SELECT MIN(t1."Quantity")
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                ), 0) AS "Lowest"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(
        Skip = "DefaultIfEmpty().Average does not substitute zero for an empty sequence."
    )]
    public void SubstitutesZeroForMissingAverage()
    {
        Expression query = Customers
            .Select(c => new
            {
                c.Name,
                Mean = Orders
                    .Where(o => o.CustomerId == c.Id)
                    .Select(o => o.Quantity)
                    .DefaultIfEmpty()
                    .Average(),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", COALESCE((
                    SELECT AVG(CAST(t1."Quantity" AS double precision))
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                ), 0) AS "Mean"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "DefaultIfEmpty().Any() is true even when the source is empty.")]
    public void TreatsDefaultIfEmptyAsNonEmpty()
    {
        Expression query = Customers
            .Select(c => new
            {
                c.Name,
                Has = Orders
                    .Where(o => o.CustomerId == c.Id)
                    .Select(o => o.Total)
                    .DefaultIfEmpty()
                    .Any(),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", TRUE AS "Has"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "DefaultIfEmpty().Count() counts the default row of an empty sequence.")]
    public void CountsDefaultRowOfEmptySequence()
    {
        Expression query = Customers
            .Select(c => new
            {
                c.Name,
                Count = Orders
                    .Where(o => o.CustomerId == c.Id)
                    .Select(o => o.Total)
                    .DefaultIfEmpty()
                    .Count(),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", CAST(GREATEST((
                    SELECT COUNT(*)
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                ), 1) AS integer) AS "Count"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "FirstOrDefault of a non-nullable long does not substitute zero.")]
    public void SubstitutesZeroForMissingFirstLong()
    {
        Expression query = Customers
            .Select(c => new
            {
                c.Name,
                Points = Orders
                    .Where(o => o.CustomerId == c.Id)
                    .OrderBy(o => o.Id)
                    .Select(o => o.Points)
                    .FirstOrDefault(),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", COALESCE((
                    SELECT t1."Points"
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                    ORDER BY t1."Id"
                    LIMIT 1
                ), 0) AS "Points"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "FirstOrDefault of a non-nullable decimal does not substitute zero.")]
    public void SubstitutesZeroForMissingFirstDecimal()
    {
        Expression query = Customers
            .Select(c => new
            {
                c.Name,
                Total = Orders
                    .Where(o => o.CustomerId == c.Id)
                    .OrderBy(o => o.Id)
                    .Select(o => o.Total)
                    .FirstOrDefault(),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", COALESCE((
                    SELECT t1."Total"
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                    ORDER BY t1."Id"
                    LIMIT 1
                ), 0) AS "Total"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "FirstOrDefault of a non-nullable double does not substitute zero.")]
    public void SubstitutesZeroForMissingFirstDouble()
    {
        Expression query = Customers
            .Select(c => new
            {
                c.Name,
                Weight = Orders
                    .Where(o => o.CustomerId == c.Id)
                    .OrderBy(o => o.Id)
                    .Select(o => o.Weight)
                    .FirstOrDefault(),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", COALESCE((
                    SELECT t1."Weight"
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                    ORDER BY t1."Id"
                    LIMIT 1
                ), 0) AS "Weight"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "FirstOrDefault of Guid does not substitute Guid.Empty.")]
    public void SubstitutesEmptyGuidForMissingFirst()
    {
        Expression query = Customers
            .Select(c => new
            {
                c.Name,
                Tracking = Orders
                    .Where(o => o.CustomerId == c.Id)
                    .OrderBy(o => o.Id)
                    .Select(o => o.TrackingId)
                    .FirstOrDefault(),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", COALESCE((
                    SELECT t1."TrackingId"
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                    ORDER BY t1."Id"
                    LIMIT 1
                ), UUID '00000000-0000-0000-0000-000000000000') AS "Tracking"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "FirstOrDefault of an enum does not substitute the zero value.")]
    public void SubstitutesZeroForMissingFirstStatus()
    {
        Expression query = Customers
            .Select(c => new
            {
                c.Name,
                Status = Orders
                    .Where(o => o.CustomerId == c.Id)
                    .OrderBy(o => o.Id)
                    .Select(o => o.Status)
                    .FirstOrDefault(),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", COALESCE((
                    SELECT t1."Status"
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                    ORDER BY t1."Id"
                    LIMIT 1
                ), 0) AS "Status"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "FirstOrDefault of DateTime does not substitute DateTime.MinValue.")]
    public void SubstitutesMinimumForMissingFirstDateTime()
    {
        Expression query = Customers
            .Select(c => new
            {
                c.Name,
                Created = Orders
                    .Where(o => o.CustomerId == c.Id)
                    .OrderBy(o => o.Id)
                    .Select(o => o.CreatedAt)
                    .FirstOrDefault(),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", COALESCE((
                    SELECT t1."CreatedAt"
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                    ORDER BY t1."Id"
                    LIMIT 1
                ), TIMESTAMP '0001-01-01 00:00:00') AS "Created"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "FirstOrDefault of TimeOnly does not substitute TimeOnly.MinValue.")]
    public void SubstitutesMidnightForMissingFirstTime()
    {
        Expression query = Customers
            .Select(c => new
            {
                c.Name,
                Delivery = Orders
                    .Where(o => o.CustomerId == c.Id)
                    .OrderBy(o => o.Id)
                    .Select(o => o.DeliveryTime)
                    .FirstOrDefault(),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", COALESCE((
                    SELECT t1."DeliveryTime"
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                    ORDER BY t1."Id"
                    LIMIT 1
                ), TIME '00:00:00') AS "Delivery"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "ElementAtOrDefault of a non-nullable double does not substitute zero.")]
    public void SubstitutesZeroForMissingElementDouble()
    {
        Expression query = Customers
            .Select(c => new
            {
                c.Name,
                Weight = Orders
                    .Where(o => o.CustomerId == c.Id)
                    .OrderBy(o => o.Id)
                    .Select(o => o.Weight)
                    .ElementAtOrDefault(1),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", COALESCE((
                    SELECT t1."Weight"
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                    ORDER BY t1."Id"
                    LIMIT 1 OFFSET 1
                ), 0) AS "Weight"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "LastOrDefault of a non-nullable decimal does not substitute zero.")]
    public void SubstitutesZeroForMissingLastDecimal()
    {
        Expression query = Customers
            .Select(c => new
            {
                c.Name,
                Total = Orders
                    .Where(o => o.CustomerId == c.Id)
                    .OrderBy(o => o.Id)
                    .Select(o => o.Total)
                    .LastOrDefault(),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", COALESCE((
                    SELECT t1."Total"
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                    ORDER BY t1."Id" DESC
                    LIMIT 1
                ), 0) AS "Total"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
