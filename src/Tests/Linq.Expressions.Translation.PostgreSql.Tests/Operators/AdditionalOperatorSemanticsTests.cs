namespace Linq.Expressions.Translation.PostgreSql.Tests.Operators;

public sealed record AdditionalOperatorSemanticsTests
{
    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
    public void TakesAfterSkippingLastRows()
    {
        Expression query = Customers.OrderBy(c => c.Id).SkipLast(2).Take(3).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t2."Id", t2."Name", t2."Email", t2."Region", t2."IsActive"
                FROM (
                    SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                    FROM "Customer" AS t0
                    ORDER BY t0."Id"
                    LIMIT (SELECT GREATEST(COUNT(*) - 2, 0) FROM "Customer" AS t1)
                ) AS t2
                ORDER BY t2."Id"
                LIMIT 3
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SkipsAfterSkippingLastRows()
    {
        Expression query = Customers.OrderBy(c => c.Id).SkipLast(2).Skip(1).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t2."Id", t2."Name", t2."Email", t2."Region", t2."IsActive"
                FROM (
                    SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                    FROM "Customer" AS t0
                    ORDER BY t0."Id"
                    LIMIT (SELECT GREATEST(COUNT(*) - 2, 0) FROM "Customer" AS t1)
                ) AS t2
                ORDER BY t2."Id"
                OFFSET 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SkipsLastRowsOfTakenRows()
    {
        Expression query = Customers
            .OrderBy(c => c.Id)
            .Take(10)
            .SkipLast(2)
            .Select(c => c.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Id"
                FROM (
                    SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                    FROM "Customer" AS t0
                    ORDER BY t0."Id"
                    LIMIT 10
                ) AS t1
                ORDER BY t1."Id"
                LIMIT (
                    SELECT GREATEST(COUNT(*) - 2, 0)
                    FROM (
                        SELECT t2."Id", t2."Name", t2."Email", t2."Region", t2."IsActive"
                        FROM "Customer" AS t2
                        ORDER BY t2."Id"
                        LIMIT 10
                    ) AS t3
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SubstitutesZeroForMissingMaximumOfWholeTable()
    {
        Expression query = Orders
            .Select(o => o.Total)
            .DefaultIfEmpty()
            .Executed(q => q.Max());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE((SELECT MAX(t0."Total") FROM "Order" AS t0), 0)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SumsDefaultIfEmptyLikeSource()
    {
        Expression query = Customers
            .Select(c => new
            {
                c.Name,
                Revenue = Orders
                    .Where(o => o.CustomerId == c.Id)
                    .Select(o => o.Total)
                    .DefaultIfEmpty()
                    .Sum(),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", (
                    SELECT COALESCE(SUM(t1."Total"), 0)
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                ) AS "Revenue"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void LongCountsDefaultRowOfEmptySequence()
    {
        Expression query = Customers
            .Select(c => new
            {
                c.Name,
                Count = Orders
                    .Where(o => o.CustomerId == c.Id)
                    .Select(o => o.Total)
                    .DefaultIfEmpty()
                    .LongCount(),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", GREATEST((
                    SELECT COUNT(*)
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                ), 1) AS "Count"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void KeepsNullForMissingNullableMaximum()
    {
        Expression query = Customers
            .Select(c => new
            {
                c.Name,
                Peak = Orders
                    .Where(o => o.CustomerId == c.Id)
                    .Select(o => o.Discount)
                    .DefaultIfEmpty()
                    .Max(),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", (
                    SELECT MAX(t1."Discount")
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                ) AS "Peak"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void KeepsNullForMissingFirstString()
    {
        Expression query = Customers
            .Select(c => new
            {
                c.Name,
                Number = Orders
                    .Where(o => o.CustomerId == c.Id)
                    .OrderBy(o => o.Id)
                    .Select(o => o.Number)
                    .FirstOrDefault(),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", (
                    SELECT t1."Number"
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                    ORDER BY t1."Id"
                    LIMIT 1
                ) AS "Number"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void KeepsScalarsCastToObject()
    {
        Expression query = Customers.Select(c => c.Id).Cast<object>().Expression;

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
}
