#pragma warning disable CA1861 // the one-element array is the root under test

namespace Linq.Expressions.Translation.PostgreSql.Tests.Operators;

public sealed record OneRowAggregateTests
{
    [Fact]
    public void SelectsCountAndSumAsOneRow()
    {
        Expression query = new[] { 0 }
            .AsQueryable()
            .Select(_ => new ValueTuple<long, decimal>(
                Orders.LongCount(),
                Orders.Sum(o => o.Total)
            ))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT (
                    SELECT COUNT(*)
                    FROM "Order" AS t0
                ) AS "item1", (
                    SELECT COALESCE(SUM(t1."Total"), 0)
                    FROM "Order" AS t1
                ) AS "item2"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsAggregatesOfDifferentTablesAsOneRow()
    {
        Expression query = new[] { 0 }
            .AsQueryable()
            .Select(_ => new ValueTuple<int, int, decimal>(
                Orders.Count(),
                Customers.Count(c => c.IsActive),
                Products.Max(p => p.Price)
            ))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT (
                    SELECT CAST(COUNT(*) AS integer)
                    FROM "Order" AS t0
                ) AS "item1", (
                    SELECT CAST(COUNT(*) AS integer)
                    FROM "Customer" AS t1
                    WHERE t1."IsActive"
                ) AS "item2", (
                    SELECT MAX(t2."Price")
                    FROM "Product" AS t2
                ) AS "item3"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsAnonymousRowOfAggregates()
    {
        Expression query = new[] { 0 }
            .AsQueryable()
            .Select(_ => new
            {
                Customers = Customers.LongCount(),
                Revenue = Orders.Sum(o => o.Total),
                AverageTotal = Orders.Average(o => o.Total),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT (
                    SELECT COUNT(*)
                    FROM "Customer" AS t0
                ) AS "Customers", (
                    SELECT COALESCE(SUM(t1."Total"), 0)
                    FROM "Order" AS t1
                ) AS "Revenue", (
                    SELECT AVG(t2."Total")
                    FROM "Order" AS t2
                ) AS "AverageTotal"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsFilteredAggregatesAsOneRow()
    {
        Expression query = new[] { 0 }
            .AsQueryable()
            .Select(_ => new
            {
                Paid = Orders.Count(o => o.IsPaid),
                PaidRevenue = Orders.Where(o => o.IsPaid).Sum(o => o.Total),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT (
                    SELECT CAST(COUNT(*) AS integer)
                    FROM "Order" AS t0
                    WHERE t0."IsPaid"
                ) AS "Paid", (
                    SELECT COALESCE(SUM(t1."Total"), 0)
                    FROM "Order" AS t1
                    WHERE t1."IsPaid"
                ) AS "PaidRevenue"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsQuantifiersAsOneRow()
    {
        Expression query = new[] { 0 }
            .AsQueryable()
            .Select(_ => new
            {
                Orders = Orders.Count(),
                AnyPaid = Orders.Any(o => o.IsPaid),
                AllPaid = Orders.All(o => o.IsPaid),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT (
                    SELECT CAST(COUNT(*) AS integer)
                    FROM "Order" AS t0
                ) AS "Orders", EXISTS (
                    SELECT 1
                    FROM "Order" AS t1
                    WHERE t1."IsPaid"
                ) AS "AnyPaid", NOT EXISTS (
                    SELECT 1
                    FROM "Order" AS t2
                    WHERE NOT t2."IsPaid"
                ) AS "AllPaid"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void NumbersAliasesAcrossNestedSubqueriesOfOneRow()
    {
        Expression query = new[] { 0 }
            .AsQueryable()
            .Select(_ => new
            {
                Buyers = Customers.Count(c => Orders.Any(o => o.CustomerId == c.Id)),
                Products = Products.Count(),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT (
                    SELECT CAST(COUNT(*) AS integer)
                    FROM "Customer" AS t0
                    WHERE EXISTS (
                        SELECT 1
                        FROM "Order" AS t1
                        WHERE t1."CustomerId" = t0."Id"
                    )
                ) AS "Buyers", (
                    SELECT CAST(COUNT(*) AS integer)
                    FROM "Product" AS t2
                ) AS "Products"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsSingleAggregateAsOneRow()
    {
        Expression query = new[] { 0 }
            .AsQueryable()
            .Select(_ => Orders.Count())
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT (
                    SELECT CAST(COUNT(*) AS integer)
                    FROM "Order" AS t0
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersOneRowOfAggregates()
    {
        Expression query = new[] { 0 }
            .AsQueryable()
            .Where(_ => Orders.Any())
            .Select(_ => Orders.Count())
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT (
                    SELECT CAST(COUNT(*) AS integer)
                    FROM "Order" AS t1
                )
                WHERE EXISTS (
                    SELECT 1
                    FROM "Order" AS t0
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RejectsBareOneElementInMemoryRoot()
    {
        Expression query = new[] { 0 }.AsQueryable().Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("EnumerableQuery", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsOrderedOneElementInMemoryRoot()
    {
        Expression query = new[] { 0 }
            .AsQueryable()
            .Order()
            .Select(_ => Orders.Count())
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("EnumerableQuery", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsOneElementInMemoryRootInSetOperation()
    {
        Expression query = new[] { 0 }
            .AsQueryable()
            .Concat(Orders.Select(o => o.Id))
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("EnumerableQuery", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsOneElementInMemoryRootAsSubquerySource()
    {
        Expression query = Customers
            .Where(c => new[] { 1 }.AsQueryable().Any(i => i == c.Id))
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("EnumerableQuery", exception.Message, StringComparison.Ordinal);
    }
}
