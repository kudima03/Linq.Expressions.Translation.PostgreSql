namespace Linq.Expressions.Translation.PostgreSql.Tests.Combinations;

public sealed record BusinessScenarioTests
{
    [Fact]
    public void RanksRegionsByPaidRevenueOfYear()
    {
        Expression query = Orders
            .Where(o => o.IsPaid && o.CreatedAt.Year == 2024)
            .Join(
                Customers,
                o => o.CustomerId,
                c => c.Id,
                (o, c) => new { c.Region, o.Total }
            )
            .GroupBy(x => x.Region)
            .Select(g => new
            {
                Region = g.Key,
                Revenue = g.Sum(x => x.Total),
                Orders = g.Count(),
            })
            .OrderByDescending(x => x.Revenue)
            .Take(3)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Region", COALESCE(SUM(t0."Total"), 0) AS "Revenue", CAST(COUNT(*) AS integer) AS "Orders"
                FROM "Order" AS t0
                INNER JOIN "Customer" AS t1 ON t0."CustomerId" = t1."Id"
                WHERE t0."IsPaid" AND (CAST(EXTRACT(YEAR FROM t0."CreatedAt") AS integer) = 2024)
                GROUP BY t1."Region"
                ORDER BY COALESCE(SUM(t0."Total"), 0) DESC
                LIMIT 3
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ListsOrdersWithoutLines()
    {
        Expression query = Orders
            .Where(o => !OrderLines.Any(l => l.OrderId == o.Id))
            .Select(o => o.Number)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Number"
                FROM "Order" AS t0
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM "OrderLine" AS t1
                    WHERE t1."OrderId" = t0."Id"
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ListsProductsNeverOrdered()
    {
        Expression query = Products
            .Where(p => !OrderLines.Select(l => l.ProductId).Contains(p.Id))
            .OrderBy(p => p.Name)
            .Select(p => p.Name)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name"
                FROM "Product" AS t0
                WHERE NOT (t0."Id" IN (SELECT t1."ProductId" FROM "OrderLine" AS t1))
                ORDER BY t0."Name"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SummarizesActiveCustomersByOrderActivity()
    {
        Expression query = Customers
            .Where(c => c.IsActive)
            .GroupJoin(
                Orders,
                c => c.Id,
                o => o.CustomerId,
                (c, os) =>
                    new
                    {
                        c.Name,
                        Count = os.Count(),
                        Last = os.Max(o => (DateTime?)o.CreatedAt),
                    }
            )
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Name)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", (
                    SELECT CAST(COUNT(*) AS integer)
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                ) AS "Count", (
                    SELECT MAX(t2."CreatedAt")
                    FROM "Order" AS t2
                    WHERE t2."CustomerId" = t0."Id"
                ) AS "Last"
                FROM "Customer" AS t0
                WHERE t0."IsActive"
                ORDER BY (
                    SELECT CAST(COUNT(*) AS integer)
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                ) DESC, t0."Name"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void PagesCustomerSearchResults()
    {
        string term = "o'b";
        Expression query = Customers
            .Where(c =>
                c.Name.Contains(term) || (c.Email != null && c.Email.EndsWith("@x.com"))
            )
            .OrderBy(c => c.Name)
            .ThenBy(c => c.Id)
            .Skip(20)
            .Take(10)
            .Select(c => new { c.Id, c.Name })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name"
                FROM "Customer" AS t0
                WHERE (t0."Name" LIKE '%o''b%') OR ((t0."Email" IS NOT NULL) AND (t0."Email" LIKE '%@x.com'))
                ORDER BY t0."Name", t0."Id"
                LIMIT 10
                OFFSET 20
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ReportsMonthlyNetRevenue()
    {
        Expression query = Orders
            .Where(o => o.Status != OrderStatus.Cancelled)
            .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Net = g.Sum(o => o.Total - (o.Discount ?? 0)),
            })
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(EXTRACT(YEAR FROM t0."CreatedAt") AS integer) AS "Year",
                CAST(EXTRACT(MONTH FROM t0."CreatedAt") AS integer) AS "Month",
                COALESCE(SUM(t0."Total" - COALESCE(t0."Discount", 0)), 0) AS "Net"
                FROM "Order" AS t0
                WHERE t0."Status" <> 3
                GROUP BY CAST(EXTRACT(YEAR FROM t0."CreatedAt") AS integer), CAST(EXTRACT(MONTH FROM t0."CreatedAt") AS integer)
                ORDER BY CAST(EXTRACT(YEAR FROM t0."CreatedAt") AS integer), CAST(EXTRACT(MONTH FROM t0."CreatedAt") AS integer)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ReportsRevenueOfBestSellingProducts()
    {
        Expression query = OrderLines
            .GroupBy(l => l.ProductId)
            .Where(g => g.Sum(l => l.Quantity) >= 10)
            .Select(g => new
            {
                ProductId = g.Key,
                Revenue = g.Sum(l => l.UnitPrice * l.Quantity),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."ProductId", COALESCE(SUM(t0."UnitPrice" * CAST(t0."Quantity" AS numeric)), 0) AS "Revenue"
                FROM "OrderLine" AS t0
                GROUP BY t0."ProductId"
                HAVING CAST(COALESCE(SUM(t0."Quantity"), 0) AS integer) >= 10
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsLatestOrderOfEachCustomer()
    {
        Expression query = Orders
            .Where(o =>
                o.CreatedAt
                == Orders.Where(x => x.CustomerId == o.CustomerId).Max(x => x.CreatedAt)
            )
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."CreatedAt" = (
                    SELECT MAX(t1."CreatedAt")
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."CustomerId"
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CountsCustomersWhoBoughtProduct()
    {
        Expression query = OrderLines
            .Where(l => l.ProductId == 7)
            .Join(Orders, l => l.OrderId, o => o.Id, (l, o) => o.CustomerId)
            .Distinct()
            .Executed(q => q.Count());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COUNT(*) AS integer)
                FROM (
                    SELECT DISTINCT t1."CustomerId"
                    FROM "OrderLine" AS t0
                    INNER JOIN "Order" AS t1 ON t0."OrderId" = t1."Id"
                    WHERE t0."ProductId" = 7
                ) AS t2
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void PagesBusiestCustomersOfStatus()
    {
        Expression query = Orders
            .Where(o => o.Status == OrderStatus.Shipped)
            .GroupBy(o => o.CustomerId, o => o.Total)
            .Where(g => g.Count() > 1)
            .OrderByDescending(g => g.Key)
            .Select(g => new
            {
                CustomerId = g.Key,
                Largest = g.Max(),
                Distinct = g.Distinct().Count(),
            })
            .Skip(5)
            .Take(5)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId", MAX(t0."Total") AS "Largest", CAST(COUNT(DISTINCT t0."Total") AS integer) AS "Distinct"
                FROM "Order" AS t0
                WHERE t0."Status" = 2
                GROUP BY t0."CustomerId"
                HAVING CAST(COUNT(*) AS integer) > 1
                ORDER BY t0."CustomerId" DESC
                LIMIT 5
                OFFSET 5
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FlagsOrdersWhoseLinesDoNotAddUp()
    {
        Expression query = Orders
            .Select(o => new
            {
                o.Id,
                Lines = OrderLines
                    .Where(l => l.OrderId == o.Id)
                    .Sum(l => l.UnitPrice * l.Quantity),
                o.Total,
            })
            .Where(x => x.Lines != x.Total)
            .OrderBy(x => x.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", (
                    SELECT COALESCE(SUM(t1."UnitPrice" * CAST(t1."Quantity" AS numeric)), 0)
                    FROM "OrderLine" AS t1
                    WHERE t1."OrderId" = t0."Id"
                ) AS "Lines", t0."Total"
                FROM "Order" AS t0
                WHERE (
                    SELECT COALESCE(SUM(t1."UnitPrice" * CAST(t1."Quantity" AS numeric)), 0)
                    FROM "OrderLine" AS t1
                    WHERE t1."OrderId" = t0."Id"
                ) <> t0."Total"
                ORDER BY t0."Id"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
