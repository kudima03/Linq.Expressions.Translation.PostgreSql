#pragma warning disable IDE0004 // casts are part of the scenarios

namespace Linq.Expressions.Translation.PostgreSql.Tests.Combinations;

public sealed record ScenarioTests
{
    [Fact(Skip = Skipped.NotImplemented)]
    public void FindsTopCustomersInPeriod()
    {
        DateTime from = new(2024, 1, 1);
        DateTime to = new(2024, 4, 1);

        Expression query = Customers
            .Join(
                Orders
                    .Where(o => o.IsPaid && (o.CreatedAt >= from) && (o.CreatedAt < to))
                    .GroupBy(o => o.CustomerId)
                    .Select(g => new
                    {
                        CustomerId = g.Key,
                        Revenue = g.Sum(o => o.Total),
                    })
                    .Where(x => x.Revenue > 1000m),
                c => c.Id,
                x => x.CustomerId,
                (c, x) => new { c.Name, x.Revenue }
            )
            .OrderByDescending(y => y.Revenue)
            .Take(5)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t2."Revenue"
                FROM "Customer" AS t0
                INNER JOIN (
                    SELECT t1."CustomerId", COALESCE(SUM(t1."Total"), 0) AS "Revenue"
                    FROM "Order" AS t1
                    WHERE t1."IsPaid" AND (t1."CreatedAt" >= TIMESTAMP '2024-01-01 00:00:00') AND (t1."CreatedAt" < TIMESTAMP '2024-04-01 00:00:00')
                    GROUP BY t1."CustomerId"
                    HAVING COALESCE(SUM(t1."Total"), 0) > 1000
                ) AS t2 ON t0."Id" = t2."CustomerId"
                ORDER BY t2."Revenue" DESC
                LIMIT 5
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FindsCustomersWithoutOrders()
    {
        Expression query = Customers
            .Where(c => !Orders.Any(o => o.CustomerId == c.Id))
            .OrderBy(c => c.Name)
            .Select(c => new { c.Id, c.Name })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name"
                FROM "Customer" AS t0
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                )
                ORDER BY t0."Name"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FindsCustomersWithoutOrdersWithLeftJoin()
    {
        Expression query = Customers
            .GroupJoin(Orders, c => c.Id, o => o.CustomerId, (c, os) => new { c, os })
            .SelectMany(
                x => x.os.DefaultIfEmpty(),
                (x, o) =>
                    new
                    {
                        x.c.Id,
                        x.c.Name,
                        OrderId = (int?)o!.Id,
                    }
            )
            .Where(x => x.OrderId == null)
            .OrderBy(x => x.Name)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t1."Id" AS "OrderId"
                FROM "Customer" AS t0
                LEFT JOIN "Order" AS t1 ON t0."Id" = t1."CustomerId"
                WHERE t1."Id" IS NULL
                ORDER BY t0."Name"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FindsOrdersWithinAWeekOfFirstOrder()
    {
        Expression query = Orders
            .Join(
                Orders
                    .GroupBy(o => o.CustomerId)
                    .Select(g => new
                    {
                        CustomerId = g.Key,
                        First = g.Min(o => o.CreatedAt),
                    }),
                o => o.CustomerId,
                x => x.CustomerId,
                (o, x) =>
                    new
                    {
                        o.Id,
                        o.CreatedAt,
                        x.First,
                    }
            )
            .Where(y => y.CreatedAt <= y.First.AddDays(7))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."CreatedAt", t2."First"
                FROM "Order" AS t0
                INNER JOIN (
                    SELECT t1."CustomerId", MIN(t1."CreatedAt") AS "First"
                    FROM "Order" AS t1
                    GROUP BY t1."CustomerId"
                ) AS t2 ON t0."CustomerId" = t2."CustomerId"
                WHERE t0."CreatedAt" <= (t2."First" + (7 * INTERVAL '1 day'))
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void BucketsOrdersBySize()
    {
        Expression query = Orders
            .GroupBy(o =>
                o.Quantity < 5 ? "small" : (o.Quantity < 20 ? "medium" : "large")
            )
            .Select(g => new
            {
                Bucket = g.Key,
                Orders = g.Count(),
                Revenue = g.Sum(o => o.Total),
            })
            .OrderBy(x => x.Bucket)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CASE WHEN t0."Quantity" < 5 THEN 'small' WHEN t0."Quantity" < 20 THEN 'medium' ELSE 'large' END AS "Bucket", CAST(COUNT(*) AS integer) AS "Orders", COALESCE(SUM(t0."Total"), 0) AS "Revenue"
                FROM "Order" AS t0
                GROUP BY CASE WHEN t0."Quantity" < 5 THEN 'small' WHEN t0."Quantity" < 20 THEN 'medium' ELSE 'large' END
                ORDER BY CASE WHEN t0."Quantity" < 5 THEN 'small' WHEN t0."Quantity" < 20 THEN 'medium' ELSE 'large' END
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void ComputesLineTotalsWithDiscount()
    {
        decimal discount = 0.1m;

        Expression query = OrderLines
            .Join(
                Products,
                l => l.ProductId,
                p => p.Id,
                (l, p) =>
                    new
                    {
                        l.OrderId,
                        Product = p.Name,
                        Total = l.Quantity * l.UnitPrice * (1m - discount),
                    }
            )
            .Where(x => x.Total > 10m)
            .OrderByDescending(x => x.Total)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."OrderId", t1."Name" AS "Product",
                (CAST(t0."Quantity" AS numeric) * t0."UnitPrice") * 0.9 AS "Total"
                FROM "OrderLine" AS t0
                INNER JOIN "Product" AS t1 ON t0."ProductId" = t1."Id"
                WHERE ((CAST(t0."Quantity" AS numeric) * t0."UnitPrice") * 0.9) > 10
                ORDER BY (CAST(t0."Quantity" AS numeric) * t0."UnitPrice") * 0.9 DESC
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FindsOrdersAboveCustomerAverage()
    {
        Expression query = Orders
            .Where(o =>
                o.Total
                > Orders.Where(x => x.CustomerId == o.CustomerId).Average(x => x.Total)
            )
            .Select(o => new { o.Id, o.Total })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Total"
                FROM "Order" AS t0
                WHERE t0."Total" > (
                    SELECT AVG(t1."Total")
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."CustomerId"
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FindsLatestOrderOfEachCustomer()
    {
        Expression query = Orders
            .Where(o =>
                o.CreatedAt
                == Orders.Where(x => x.CustomerId == o.CustomerId).Max(x => x.CreatedAt)
            )
            .Select(o => new { o.CustomerId, o.Id })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId", t0."Id"
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

    [Fact(Skip = Skipped.NotImplemented)]
    public void ComputesShareOfTotalRevenue()
    {
        Expression query = Orders
            .Select(o => new { o.Id, Share = o.Total / Orders.Sum(x => x.Total) })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Total" / (
                    SELECT COALESCE(SUM(t1."Total"), 0)
                    FROM "Order" AS t1
                ) AS "Share"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FindsDeliveriesInBusinessHours()
    {
        Expression query = Orders
            .Where(o =>
                (o.DeliveryTime >= new TimeOnly(9, 0))
                && (o.DeliveryTime < new TimeOnly(17, 0))
                && (o.CreatedAt.DayOfWeek != DayOfWeek.Saturday)
                && (o.CreatedAt.DayOfWeek != DayOfWeek.Sunday)
            )
            .OrderBy(o => o.DeliveryTime)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."DeliveryTime" >= TIME '09:00:00') AND (t0."DeliveryTime" < TIME '17:00:00') AND (CAST(EXTRACT(DOW FROM t0."CreatedAt") AS integer) <> 6) AND (CAST(EXTRACT(DOW FROM t0."CreatedAt") AS integer) <> 0)
                ORDER BY t0."DeliveryTime"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void MeasuresGiftEffectiveness()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                Gifts = g.Count(o => o.IsGift == true),
                GiftRevenue = g.Sum(o => o.IsGift == true ? o.Total : 0m),
                Revenue = g.Sum(o => o.Total),
            })
            .Where(x => x.Gifts > 0)
            .OrderByDescending(x => x.GiftRevenue)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId",
                CAST(COUNT(*) FILTER (WHERE t0."IsGift" IS NOT DISTINCT FROM TRUE) AS integer) AS "Gifts", COALESCE(SUM(CASE WHEN t0."IsGift" IS NOT DISTINCT FROM TRUE THEN t0."Total" ELSE 0 END), 0) AS "GiftRevenue", COALESCE(SUM(t0."Total"), 0) AS "Revenue"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                HAVING CAST(COUNT(*) FILTER (WHERE t0."IsGift" IS NOT DISTINCT FROM TRUE) AS integer) > 0
                ORDER BY COALESCE(SUM(CASE WHEN t0."IsGift" IS NOT DISTINCT FROM TRUE THEN t0."Total" ELSE 0 END), 0) DESC
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FiltersGroupsWithBooleanLogic()
    {
        int minOrders = 3;

        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Where(g =>
                (
                    (g.Sum(o => o.Total) > 250m)
                    && !g.Any(o => o.Status == OrderStatus.Cancelled)
                ) || (g.Count() >= minOrders)
            )
            .Select(g => new { CustomerId = g.Key, Revenue = g.Sum(o => o.Total) })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId", COALESCE(SUM(t0."Total"), 0) AS "Revenue"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                HAVING ((COALESCE(SUM(t0."Total"),
                0) > 250) AND NOT BOOL_OR(t0."Status" = 3)) OR (CAST(COUNT(*) AS integer) >= 3)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void GroupsByComputedBucket()
    {
        Expression query = Orders
            .GroupBy(o => (int)(o.Total / 100m))
            .Select(g => new { Bucket = g.Key, Count = g.Count() })
            .OrderBy(x => x.Bucket)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(TRUNC(t0."Total" / 100) AS integer) AS "Bucket",
                CAST(COUNT(*) AS integer) AS "Count"
                FROM "Order" AS t0
                GROUP BY CAST(TRUNC(t0."Total" / 100) AS integer)
                ORDER BY CAST(TRUNC(t0."Total" / 100) AS integer)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void ListsDistinctStatusesInOrder()
    {
        Expression query = Orders
            .Select(o => o.Status)
            .Distinct()
            .OrderBy(s => s)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Status"
                FROM (
                    SELECT DISTINCT t0."Status"
                    FROM "Order" AS t0
                ) AS t1
                ORDER BY t1."Status"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void ShiftsDatesEverywhere()
    {
        DateTime cutoff = new(2024, 6, 1);

        Expression query = Orders
            .Where(o => o.CreatedAt.AddDays(30) > cutoff)
            .GroupBy(o => o.CreatedAt.AddDays(30).Date)
            .Select(g => new { Due = g.Key, Count = g.Count() })
            .OrderBy(x => x.Due)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT DATE_TRUNC('day',
                t0."CreatedAt" + (30 * INTERVAL '1 day')) AS "Due",
                CAST(COUNT(*) AS integer) AS "Count"
                FROM "Order" AS t0
                WHERE (t0."CreatedAt" + (30 * INTERVAL '1 day')) > TIMESTAMP '2024-06-01 00:00:00'
                GROUP BY DATE_TRUNC('day', t0."CreatedAt" + (30 * INTERVAL '1 day'))
                ORDER BY DATE_TRUNC('day', t0."CreatedAt" + (30 * INTERVAL '1 day'))
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void CombinesNullableOperatorsInOneProjection()
    {
        Expression query = Orders
            .Select(o => new
            {
                o.Id,
                Net = (o.Total - o.Discount) ?? o.Total,
                Rush = (o.Priority ?? 0) > 5,
                Late = o.PaidAt == null,
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", COALESCE(t0."Total" - t0."Discount",
                t0."Total") AS "Net", COALESCE(t0."Priority", 0) > 5 AS "Rush",
                t0."PaidAt" IS NULL AS "Late"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void BuildsReferralChainWithManagerNames()
    {
        Expression query = Employees
            .GroupJoin(Employees, e => e.ManagerId, m => m.Id, (e, ms) => new { e, ms })
            .SelectMany(
                x => x.ms.DefaultIfEmpty(),
                (x, m) =>
                    new
                    {
                        x.e.Id,
                        x.e.Name,
                        Manager = m!.Name,
                    }
            )
            .OrderBy(x => x.Manager)
            .ThenBy(x => x.Name)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t1."Name" AS "Manager"
                FROM "Employee" AS t0
                LEFT JOIN "Employee" AS t1 ON t0."ManagerId" = t1."Id"
                ORDER BY t1."Name" NULLS FIRST, t0."Name"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void PagesCustomersWithOrderCountsAndFilter()
    {
        string region = "EU";
        int page = 3;

        Expression query = Customers
            .Where(c => c.IsActive && (c.Region == region))
            .Select(c => new { c.Name, Orders = Orders.Count(o => o.CustomerId == c.Id) })
            .Where(x => x.Orders > 0)
            .OrderByDescending(x => x.Orders)
            .ThenBy(x => x.Name)
            .Skip((page - 1) * 10)
            .Take(10)
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
                WHERE t0."IsActive" AND (t0."Region" = 'EU') AND ((
                    SELECT CAST(COUNT(*) AS integer)
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                ) > 0)
                ORDER BY (
                    SELECT CAST(COUNT(*) AS integer)
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                ) DESC, t0."Name"
                LIMIT 10 OFFSET 20
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
