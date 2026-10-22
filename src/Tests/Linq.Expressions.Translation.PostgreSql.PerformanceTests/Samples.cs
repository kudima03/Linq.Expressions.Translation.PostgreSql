namespace Linq.Expressions.Translation.PostgreSql.PerformanceTests;

// Queries from simple to complex; each builds its tree on demand, outside the timing.
public static class Samples
{
    public static IReadOnlyList<Sample> All { get; } =
    [
        new("ScansTable", () => Customers.Expression),
        new("FiltersByCapturedValue", FiltersByCapturedValue),
        new("ProjectsComputedColumns", ProjectsComputedColumns),
        new("PagesOrderedRows", PagesOrderedRows),
        new("MatchesAndFormatsStrings", MatchesAndFormatsStrings),
        new("CountsMatchingRows", CountsMatchingRows),
        new("JoinsTwoTables", JoinsTwoTables),
        new("FiltersAggregatedGroups", FiltersAggregatedGroups),
        new("LeftJoinsMissingRows", LeftJoinsMissingRows),
        new("CorrelatesSubqueries", CorrelatesSubqueries),
        new("FiltersByLargeCollection", FiltersByLargeCollection),
        new("CombinesSets", CombinesSets),
        new("WrapsDistinctProjection", WrapsDistinctProjection),
        new("ReportsRevenueAcrossFourTables", ReportsRevenueAcrossFourTables),
        new("ComposesDynamicFilters", ComposesDynamicFilters),
    ];

    private static Expression FiltersByCapturedValue()
    {
        string region = "EU";
        return Customers.Where(c => c.Region == region && c.IsActive).Expression;
    }

    private static Expression ProjectsComputedColumns()
    {
        return Orders
            .Select(o => new
            {
                o.Id,
                o.Number,
                Net = o.Total - (o.Discount ?? 0),
                Express = (o.Options & ShippingOptions.Express) != 0,
            })
            .Expression;
    }

    private static Expression PagesOrderedRows()
    {
        return Orders
            .OrderBy(o => o.CreatedAt)
            .ThenByDescending(o => o.Total)
            .Skip(40)
            .Take(20)
            .Expression;
    }

    private static Expression MatchesAndFormatsStrings()
    {
        return Customers
            .Where(c =>
                c.Name.StartsWith("Al") || (c.Email != null && c.Email.Contains("@x."))
            )
            .Select(c => $"{c.Name.ToUpperInvariant()} <{c.Email}>")
            .Expression;
    }

    private static Expression CountsMatchingRows()
    {
        return Orders.Executed(q => q.Count(o => o.IsPaid && o.Total > 100));
    }

    private static Expression JoinsTwoTables()
    {
        return Orders
            .Join(
                Customers,
                o => o.CustomerId,
                c => c.Id,
                (o, c) =>
                    new
                    {
                        o.Number,
                        c.Name,
                        o.Total,
                    }
            )
            .Where(x => x.Total > 50)
            .Expression;
    }

    private static Expression FiltersAggregatedGroups()
    {
        return Orders
            .GroupBy(o => o.CustomerId)
            .Where(g => g.Count() > 5)
            .Select(g => new
            {
                CustomerId = g.Key,
                Total = g.Sum(o => o.Total),
                Average = g.Average(o => o.Quantity),
            })
            .Expression;
    }

    private static Expression LeftJoinsMissingRows()
    {
        return Customers
            .GroupJoin(Orders, c => c.Id, o => o.CustomerId, (c, os) => new { c, os })
            .SelectMany(
                x => x.os.DefaultIfEmpty(),
                (x, o) => new { x.c.Name, OrderId = (int?)o!.Id }
            )
            .Where(x => x.OrderId == null)
            .OrderBy(x => x.Name)
            .Expression;
    }

    private static Expression CorrelatesSubqueries()
    {
        return Customers
            .Where(c => Orders.Any(o => o.CustomerId == c.Id && o.IsPaid))
            .Select(c => new
            {
                c.Name,
                Orders = Orders.Count(o => o.CustomerId == c.Id),
                Last = Orders
                    .Where(o => o.CustomerId == c.Id)
                    .Max(o => (DateTime?)o.CreatedAt),
            })
            .Expression;
    }

    private static Expression FiltersByLargeCollection()
    {
        List<int> ids = [.. Enumerable.Range(1, 1000)];
        return Orders.Where(o => ids.Contains(o.CustomerId)).Expression;
    }

    private static Expression CombinesSets()
    {
        return Customers
            .Select(c => c.Name)
            .Union(Products.Select(p => p.Name))
            .Except(Employees.Select(e => e.Name))
            .OrderBy(n => n)
            .Expression;
    }

    private static Expression WrapsDistinctProjection()
    {
        return Orders
            .Select(o => new { o.CustomerId, o.Status })
            .Distinct()
            .OrderBy(x => x.CustomerId)
            .Take(10)
            .Where(x => x.Status != OrderStatus.Cancelled)
            .Expression;
    }

    private static Expression ReportsRevenueAcrossFourTables()
    {
        return OrderLines
            .Join(Orders, l => l.OrderId, o => o.Id, (l, o) => new { l, o })
            .Join(
                Products,
                x => x.l.ProductId,
                p => p.Id,
                (x, p) =>
                    new
                    {
                        x.l,
                        x.o,
                        p,
                    }
            )
            .Join(
                Customers,
                x => x.o.CustomerId,
                c => c.Id,
                (x, c) =>
                    new
                    {
                        x.l,
                        x.o,
                        x.p,
                        c,
                    }
            )
            .Where(x => x.o.IsPaid && x.c.IsActive)
            .GroupBy(x => new { x.c.Region, Product = x.p.Name })
            .Select(g => new
            {
                g.Key.Region,
                g.Key.Product,
                Revenue = g.Sum(x => x.l.Quantity * x.l.UnitPrice),
                Orders = g.Select(x => x.o.Id).Distinct().Count(),
            })
            .OrderByDescending(x => x.Revenue)
            .ThenBy(x => x.Region)
            .Skip(10)
            .Take(10)
            .Expression;
    }

    // A filter builder ORs fifty conditions, and each refinement pages the previous page.
    private static Expression ComposesDynamicFilters()
    {
        ParameterExpression order = Expression.Parameter(typeof(Order), "o");
        Expression number = Expression.Property(order, nameof(Order.Number));
        Expression body = Expression.Constant(false);
        for (int i = 0; i < 50; i++)
        {
            body = Expression.OrElse(
                body,
                Expression.Equal(number, Expression.Constant("N" + i))
            );
        }
        IQueryable<Order> query = Orders.Where(
            Expression.Lambda<Func<Order, bool>>(body, order)
        );
        for (int i = 0; i < 10; i++)
        {
            int quantity = i;
            query = query
                .Where(o => o.Quantity > quantity)
                .OrderBy(o => o.Id)
                .Take(100 - i);
        }
        return query.Expression;
    }
}
