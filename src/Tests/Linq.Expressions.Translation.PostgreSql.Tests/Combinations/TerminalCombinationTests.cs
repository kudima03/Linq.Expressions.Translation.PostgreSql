namespace Linq.Expressions.Translation.PostgreSql.Tests.Combinations;

public sealed record TerminalCombinationTests
{
    [Fact]
    public void AveragesDecimalOfEmptySequenceAsZero()
    {
        Expression query = Customers
            .Select(c => new
            {
                c.Name,
                Mean = Orders
                    .Where(o => o.CustomerId == c.Id)
                    .Select(o => o.Total)
                    .DefaultIfEmpty()
                    .Average(),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", COALESCE((
                    SELECT AVG(t1."Total")
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
    public void TakesMaximumOfDistinctValuesWithDefault()
    {
        Expression query = Customers
            .Select(c => new
            {
                c.Name,
                Peak = Orders
                    .Where(o => o.CustomerId == c.Id)
                    .Select(o => o.Quantity)
                    .Distinct()
                    .DefaultIfEmpty()
                    .Max(),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", COALESCE((
                    SELECT MAX(t2."Quantity")
                    FROM (
                        SELECT DISTINCT t1."Quantity"
                        FROM "Order" AS t1
                        WHERE t1."CustomerId" = t0."Id"
                    ) AS t2
                ), 0) AS "Peak"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CountsDefaultRowOfWholeTable()
    {
        Expression query = Orders
            .Select(o => o.Id)
            .DefaultIfEmpty()
            .Executed(q => q.Count());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(GREATEST((SELECT COUNT(*) FROM "Order" AS t0), 1) AS integer)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TreatsDefaultIfEmptyOfWholeTableAsNonEmpty()
    {
        Expression query = Orders
            .Select(o => o.Id)
            .DefaultIfEmpty()
            .Executed(q => q.Any());

        Assert.Equal(Sql.Valid("SELECT TRUE"), new PostgreSqlQuery(query).TextValue);
    }

    [Fact]
    public void CountsDefaultRowInFilter()
    {
        Expression query = Customers
            .Where(c =>
                Orders
                    .Where(o => o.CustomerId == c.Id)
                    .Select(o => o.Id)
                    .DefaultIfEmpty()
                    .Count() == 1
            )
            .Select(c => c.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Customer" AS t0
                WHERE CAST(GREATEST((
                    SELECT COUNT(*)
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                ), 1) AS integer) = 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AddsToSubstitutedMinimum()
    {
        Expression query = Customers
            .Select(c =>
                Orders
                    .Where(o => o.CustomerId == c.Id)
                    .Select(o => o.Total)
                    .DefaultIfEmpty()
                    .Min() + 1
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE((
                    SELECT MIN(t1."Total")
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                ), 0) + 1
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SubstitutesZeroForFirstValueMatchingPredicate()
    {
        Expression query = Customers
            .Select(c => new
            {
                c.Name,
                Large = Orders
                    .Where(o => o.CustomerId == c.Id)
                    .Select(o => o.Quantity)
                    .FirstOrDefault(q => q > 2),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", COALESCE((
                    SELECT t1."Quantity"
                    FROM "Order" AS t1
                    WHERE (t1."CustomerId" = t0."Id") AND (t1."Quantity" > 2)
                    LIMIT 1
                ), 0) AS "Large"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersBySubstitutedFirstValue()
    {
        Expression query = Customers
            .Where(c =>
                Orders
                    .Where(o => o.CustomerId == c.Id)
                    .OrderBy(o => o.Id)
                    .Select(o => o.Quantity)
                    .FirstOrDefault() > 5
            )
            .Select(c => c.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Customer" AS t0
                WHERE COALESCE((
                    SELECT t1."Quantity"
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                    ORDER BY t1."Id"
                    LIMIT 1
                ), 0) > 5
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersBySubstitutedSingleBoolean()
    {
        Expression query = Customers
            .Where(c =>
                Orders
                    .Where(o => o.CustomerId == c.Id)
                    .Select(o => o.IsPaid)
                    .SingleOrDefault()
            )
            .Select(c => c.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Customer" AS t0
                WHERE COALESCE((
                    SELECT t1."IsPaid"
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                    LIMIT 2
                ), FALSE)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SubstitutesZeroStatusForMissingElement()
    {
        Expression query = Customers
            .Select(c => new
            {
                c.Name,
                Second = Orders
                    .Where(o => o.CustomerId == c.Id)
                    .OrderBy(o => o.Id)
                    .Select(o => o.Status)
                    .ElementAtOrDefault(1),
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
                    LIMIT 1 OFFSET 1
                ), 0) AS "Second"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SubstitutesEmptyGuidForMissingLast()
    {
        Expression query = Customers
            .Select(c => new
            {
                c.Name,
                Tracking = Orders
                    .Where(o => o.CustomerId == c.Id)
                    .OrderBy(o => o.Id)
                    .Select(o => o.TrackingId)
                    .LastOrDefault(),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", COALESCE((
                    SELECT t1."TrackingId"
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                    ORDER BY t1."Id" DESC
                    LIMIT 1
                ), UUID '00000000-0000-0000-0000-000000000000') AS "Tracking"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void KeepsNullForMissingFirstOfNullableColumn()
    {
        Expression query = Customers
            .Select(c => new
            {
                c.Name,
                Priority = Orders
                    .Where(o => o.CustomerId == c.Id)
                    .OrderBy(o => o.Id)
                    .Select(o => o.Priority)
                    .FirstOrDefault(),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", (
                    SELECT t1."Priority"
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                    ORDER BY t1."Id"
                    LIMIT 1
                ) AS "Priority"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void LeavesMissingFirstValueOfStatementToTheReader()
    {
        Expression query = Orders
            .Select(o => o.Quantity)
            .Executed(q => q.FirstOrDefault());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Quantity"
                FROM "Order" AS t0
                LIMIT 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SubstitutesZeroForPriceCorrelatedWithGroupKey()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new
            {
                g.Key,
                Price = Products
                    .Where(p => p.Id == g.Key)
                    .Select(p => p.Price)
                    .FirstOrDefault(),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId" AS "Key", COALESCE((
                    SELECT t1."Price"
                    FROM "Product" AS t1
                    WHERE t1."Id" = t0."CustomerId"
                    LIMIT 1
                ), 0) AS "Price"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AddsSkipToElementPosition()
    {
        Expression query = Customers
            .Select(c => new
            {
                c.Name,
                Third = Orders
                    .Where(o => o.CustomerId == c.Id)
                    .OrderBy(o => o.Id)
                    .Skip(1)
                    .Select(o => o.Quantity)
                    .ElementAtOrDefault(1),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", COALESCE((
                    SELECT t1."Quantity"
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                    ORDER BY t1."Id"
                    LIMIT 1 OFFSET 2
                ), 0) AS "Third"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SubstitutesZeroForLastValueMatchingPredicate()
    {
        Expression query = Customers
            .Select(c => new
            {
                c.Name,
                Large = Orders
                    .Where(o => o.CustomerId == c.Id)
                    .OrderBy(o => o.Id)
                    .Select(o => o.Total)
                    .LastOrDefault(t => t > 10),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", COALESCE((
                    SELECT t1."Total"
                    FROM "Order" AS t1
                    WHERE (t1."CustomerId" = t0."Id") AND (t1."Total" > 10)
                    ORDER BY t1."Id" DESC
                    LIMIT 1
                ), 0) AS "Large"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ProjectsMembershipOfConstantInCorrelatedValues()
    {
        Expression query = Customers
            .Select(c => new
            {
                c.Name,
                HasPaid = Orders
                    .Where(o => o.CustomerId == c.Id)
                    .Select(o => o.Status)
                    .Contains(OrderStatus.Paid),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", 1 IN (
                    SELECT t1."Status"
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                ) AS "HasPaid"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
