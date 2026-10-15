namespace Linq.Expressions.Translation.PostgreSql.Tests.Operators;

public sealed record AggregateTests
{
    [Fact(Skip = Skipped.NotImplemented)]
    public void CountsRows()
    {
        Expression query = Customers.Executed(q => q.Count());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COUNT(*) AS integer)
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void CountsRowsMatchingPredicate()
    {
        Expression query = Customers.Executed(q => q.Count(c => c.IsActive));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COUNT(*) AS integer)
                FROM "Customer" AS t0
                WHERE t0."IsActive"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void CountsRowsAfterFilter()
    {
        Expression query = Customers
            .Where(c => c.Region == "EU")
            .Executed(q => q.Count());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COUNT(*) AS integer)
                FROM "Customer" AS t0
                WHERE t0."Region" = 'EU'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void CountsFilteredRowsMatchingPredicate()
    {
        Expression query = Customers
            .Where(c => c.Region == "EU")
            .Executed(q => q.Count(c => c.IsActive));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COUNT(*) AS integer)
                FROM "Customer" AS t0
                WHERE (t0."Region" = 'EU') AND t0."IsActive"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void CountsProjectedRows()
    {
        Expression query = Customers.Select(c => c.Region).Executed(q => q.Count());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COUNT(*) AS integer)
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void LongCountsRows()
    {
        Expression query = Customers.Executed(q => q.LongCount());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COUNT(*)
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void LongCountsRowsMatchingPredicate()
    {
        Expression query = Customers.Executed(q => q.LongCount(c => c.IsActive));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COUNT(*)
                FROM "Customer" AS t0
                WHERE t0."IsActive"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void CountsRowsMatchingCapturedPredicate()
    {
        int minimum = 3;

        Expression query = Customers.Executed(q => q.Count(c => c.Id > minimum));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COUNT(*) AS integer)
                FROM "Customer" AS t0
                WHERE t0."Id" > 3
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void WrapsCountAfterTake()
    {
        Expression query = Customers.Take(5).Executed(q => q.Count());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COUNT(*) AS integer)
                FROM (
                    SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                    FROM "Customer" AS t0
                    LIMIT 5
                ) AS t1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void WrapsCountPredicateAfterSkip()
    {
        Expression query = Customers
            .OrderBy(c => c.Id)
            .Skip(5)
            .Executed(q => q.Count(c => c.IsActive));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COUNT(*) AS integer)
                FROM (
                    SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                    FROM "Customer" AS t0
                    ORDER BY t0."Id"
                    OFFSET 5
                ) AS t1
                WHERE t1."IsActive"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void WrapsCountAfterDistinct()
    {
        Expression query = Customers
            .Select(c => c.Region)
            .Distinct()
            .Executed(q => q.Count());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COUNT(*) AS integer)
                FROM (
                    SELECT DISTINCT t0."Region"
                    FROM "Customer" AS t0
                ) AS t1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void SumsInteger()
    {
        Expression query = Orders.Executed(q => q.Sum(o => o.Quantity));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COALESCE(SUM(t0."Quantity"), 0) AS integer)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void SumsLong()
    {
        Expression query = Orders.Executed(q => q.Sum(o => o.Points));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COALESCE(SUM(t0."Points"), 0) AS bigint)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void SumsDecimal()
    {
        Expression query = Orders.Executed(q => q.Sum(o => o.Total));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(SUM(t0."Total"), 0)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void SumsDouble()
    {
        Expression query = Orders.Executed(q => q.Sum(o => o.Weight));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(SUM(t0."Weight"), 0)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void SumsNullableInteger()
    {
        Expression query = Orders.Executed(q => q.Sum(o => o.Priority));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COALESCE(SUM(t0."Priority"), 0) AS integer)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void SumsNullableDecimal()
    {
        Expression query = Orders.Executed(q => q.Sum(o => o.Discount));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(SUM(t0."Discount"), 0)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void SumsComputedValue()
    {
        Expression query = Orders.Executed(q => q.Sum(o => o.Total * o.Quantity));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(SUM(t0."Total" * CAST(t0."Quantity" AS numeric)), 0)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void SumsProjectedColumn()
    {
        Expression query = Orders.Select(o => o.Total).Executed(q => q.Sum());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(SUM(t0."Total"), 0)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void SumsFilteredRows()
    {
        Expression query = Orders.Where(o => o.IsPaid).Executed(q => q.Sum(o => o.Total));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(SUM(t0."Total"), 0)
                FROM "Order" AS t0
                WHERE t0."IsPaid"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void WrapsSumAfterTake()
    {
        Expression query = Orders
            .OrderBy(o => o.Id)
            .Take(3)
            .Executed(q => q.Sum(o => o.Total));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(SUM(t1."Total"), 0)
                FROM (
                    SELECT t0."Id", t0."CustomerId", t0."Number", t0."Note",
                    t0."Total", t0."Discount", t0."Quantity", t0."Priority",
                    t0."Points", t0."Weight", t0."IsPaid", t0."IsGift", t0."Status",
                    t0."Options", t0."CreatedAt", t0."PaidAt", t0."ShippedOn",
                    t0."DeliveryTime", t0."TrackingId", t0."Tags"
                    FROM "Order" AS t0
                    ORDER BY t0."Id"
                    LIMIT 3
                ) AS t1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void AveragesInteger()
    {
        Expression query = Orders.Executed(q => q.Average(o => o.Quantity));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT AVG(CAST(t0."Quantity" AS double precision))
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void AveragesLong()
    {
        Expression query = Orders.Executed(q => q.Average(o => o.Points));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT AVG(CAST(t0."Points" AS double precision))
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void AveragesNullableInteger()
    {
        Expression query = Orders.Executed(q => q.Average(o => o.Priority));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT AVG(CAST(t0."Priority" AS double precision))
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void AveragesDecimal()
    {
        Expression query = Orders.Executed(q => q.Average(o => o.Total));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT AVG(t0."Total")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void AveragesDouble()
    {
        Expression query = Orders.Executed(q => q.Average(o => o.Weight));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT AVG(t0."Weight")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void AveragesNullableDecimal()
    {
        Expression query = Orders.Executed(q => q.Average(o => o.Discount));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT AVG(t0."Discount")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void AveragesFilteredProjection()
    {
        Expression query = Orders
            .Where(o => o.IsPaid)
            .Select(o => o.Total)
            .Executed(q => q.Average());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT AVG(t0."Total")
                FROM "Order" AS t0
                WHERE t0."IsPaid"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FindsMinimumInteger()
    {
        Expression query = Orders.Executed(q => q.Min(o => o.Quantity));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT MIN(t0."Quantity")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FindsMaximumInteger()
    {
        Expression query = Orders.Executed(q => q.Max(o => o.Quantity));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT MAX(t0."Quantity")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FindsMinimumDecimal()
    {
        Expression query = Orders.Executed(q => q.Min(o => o.Total));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT MIN(t0."Total")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FindsMaximumDecimal()
    {
        Expression query = Orders.Executed(q => q.Max(o => o.Total));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT MAX(t0."Total")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FindsMinimumString()
    {
        Expression query = Customers.Executed(q => q.Min(c => c.Name));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT MIN(t0."Name")
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FindsMaximumString()
    {
        Expression query = Customers.Executed(q => q.Max(c => c.Name));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT MAX(t0."Name")
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FindsMinimumDateTime()
    {
        Expression query = Orders.Executed(q => q.Min(o => o.CreatedAt));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT MIN(t0."CreatedAt")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FindsMaximumNullableDateTimeOffset()
    {
        Expression query = Orders.Executed(q => q.Max(o => o.PaidAt));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT MAX(t0."PaidAt")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FindsMaximumNullableDateOnly()
    {
        Expression query = Orders.Executed(q => q.Max(o => o.ShippedOn));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT MAX(t0."ShippedOn")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FindsMinimumTimeOnly()
    {
        Expression query = Orders.Executed(q => q.Min(o => o.DeliveryTime));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT MIN(t0."DeliveryTime")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FindsMaximumEnum()
    {
        Expression query = Orders.Executed(q => q.Max(o => o.Status));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT MAX(t0."Status")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FindsMaximumOfProjectedColumn()
    {
        Expression query = Orders.Select(o => o.Total).Executed(q => q.Max());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT MAX(t0."Total")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FindsMaximumOfFilteredRows()
    {
        Expression query = Orders.Where(o => o.IsPaid).Executed(q => q.Max(o => o.Total));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT MAX(t0."Total")
                FROM "Order" AS t0
                WHERE t0."IsPaid"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FindsMaximumOfComputedValue()
    {
        Expression query = Orders.Executed(q => q.Max(o => o.Total - o.Discount));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT MAX(t0."Total" - t0."Discount")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
