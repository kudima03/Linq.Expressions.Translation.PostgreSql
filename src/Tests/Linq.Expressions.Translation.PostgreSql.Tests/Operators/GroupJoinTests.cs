namespace Linq.Expressions.Translation.PostgreSql.Tests.Operators;

public sealed record GroupJoinTests
{
    [Fact]
    public void CountsGroupedRowsInScalarSubquery()
    {
        Expression query = Customers
            .GroupJoin(
                Orders,
                c => c.Id,
                o => o.CustomerId,
                (c, os) => new { c.Name, Orders = os.Count() }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", (
                    SELECT CAST(COUNT(*) AS integer)
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                ) AS "Orders"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CountsGroupedRowsWithPredicate()
    {
        Expression query = Customers
            .GroupJoin(
                Orders,
                c => c.Id,
                o => o.CustomerId,
                (c, os) => new { c.Name, Paid = os.Count(o => o.IsPaid) }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", (
                    SELECT CAST(COUNT(*) AS integer)
                    FROM "Order" AS t1
                    WHERE (t1."CustomerId" = t0."Id") AND t1."IsPaid"
                ) AS "Paid"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void LongCountsGroupedRows()
    {
        Expression query = Customers
            .GroupJoin(
                Orders,
                c => c.Id,
                o => o.CustomerId,
                (c, os) => new { c.Name, Orders = os.LongCount() }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", (
                    SELECT COUNT(*)
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                ) AS "Orders"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SumsGroupedColumn()
    {
        Expression query = Customers
            .GroupJoin(
                Orders,
                c => c.Id,
                o => o.CustomerId,
                (c, os) => new { c.Name, Revenue = os.Sum(o => o.Total) }
            )
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
    public void AveragesGroupedColumn()
    {
        Expression query = Customers
            .GroupJoin(
                Orders,
                c => c.Id,
                o => o.CustomerId,
                (c, os) => new { c.Name, Mean = os.Average(o => o.Total) }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", (
                    SELECT AVG(t1."Total")
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                ) AS "Mean"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void MaximizesGroupedColumn()
    {
        Expression query = Customers
            .GroupJoin(
                Orders,
                c => c.Id,
                o => o.CustomerId,
                (c, os) => new { c.Name, Largest = os.Max(o => o.Total) }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", (
                    SELECT MAX(t1."Total")
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                ) AS "Largest"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void MinimizesGroupedColumn()
    {
        Expression query = Customers
            .GroupJoin(
                Orders,
                c => c.Id,
                o => o.CustomerId,
                (c, os) => new { c.Name, Smallest = os.Min(o => o.Total) }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", (
                    SELECT MIN(t1."Total")
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                ) AS "Smallest"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ChecksExistenceOfGroupedRows()
    {
        Expression query = Customers
            .GroupJoin(
                Orders,
                c => c.Id,
                o => o.CustomerId,
                (c, os) => new { c.Name, HasOrders = os.Any() }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", EXISTS (
                    SELECT 1
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                ) AS "HasOrders"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ChecksExistenceOfGroupedRowsWithPredicate()
    {
        Expression query = Customers
            .GroupJoin(
                Orders,
                c => c.Id,
                o => o.CustomerId,
                (c, os) => new { c.Name, HasPaid = os.Any(o => o.IsPaid) }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", EXISTS (
                    SELECT 1
                    FROM "Order" AS t1
                    WHERE (t1."CustomerId" = t0."Id") AND t1."IsPaid"
                ) AS "HasPaid"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ChecksAllGroupedRows()
    {
        Expression query = Customers
            .GroupJoin(
                Orders,
                c => c.Id,
                o => o.CustomerId,
                (c, os) => new { c.Name, AllPaid = os.All(o => o.IsPaid) }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", NOT EXISTS (
                    SELECT 1
                    FROM "Order" AS t1
                    WHERE (t1."CustomerId" = t0."Id") AND NOT t1."IsPaid"
                ) AS "AllPaid"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByAggregateOfGroupedRows()
    {
        Expression query = Customers
            .GroupJoin(
                Orders,
                c => c.Id,
                o => o.CustomerId,
                (c, os) => new { c.Name, Orders = os.Count() }
            )
            .Where(x => x.Orders > 2)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", (
                    SELECT CAST(COUNT(*) AS integer)
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                ) AS "Orders"
                FROM "Customer" AS t0
                WHERE (
                    SELECT CAST(COUNT(*) AS integer)
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                ) > 2
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OrdersByAggregateOfGroupedRows()
    {
        Expression query = Customers
            .GroupJoin(
                Orders,
                c => c.Id,
                o => o.CustomerId,
                (c, os) => new { c.Name, Revenue = os.Sum(o => o.Total) }
            )
            .OrderByDescending(x => x.Revenue)
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
                ORDER BY (
                    SELECT COALESCE(SUM(t1."Total"), 0)
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                ) DESC
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComputesSeveralAggregatesOfSameGroup()
    {
        Expression query = Customers
            .GroupJoin(
                Orders,
                c => c.Id,
                o => o.CustomerId,
                (c, os) =>
                    new
                    {
                        c.Name,
                        Orders = os.Count(),
                        Revenue = os.Sum(o => o.Total),
                    }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", (
                    SELECT CAST(COUNT(*) AS integer)
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                ) AS "Orders", (
                    SELECT COALESCE(SUM(t2."Total"), 0)
                    FROM "Order" AS t2
                    WHERE t2."CustomerId" = t0."Id"
                ) AS "Revenue"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AggregatesProjectedGroupedColumn()
    {
        Expression query = Customers
            .GroupJoin(
                Orders,
                c => c.Id,
                o => o.CustomerId,
                (c, os) => new { c.Name, Units = os.Sum(o => o.Quantity) }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", (
                    SELECT CAST(COALESCE(SUM(t1."Quantity"), 0) AS integer)
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                ) AS "Units"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
