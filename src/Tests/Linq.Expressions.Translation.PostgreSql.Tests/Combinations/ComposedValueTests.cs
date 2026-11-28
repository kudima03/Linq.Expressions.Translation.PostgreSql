namespace Linq.Expressions.Translation.PostgreSql.Tests.Combinations;

// Values that are read through, to tell what they are computed from: the argument of
// an aggregate that reads nothing of its own SELECT, the arguments of a window
// function, the kind of a DateTime and the keys of an order.
public sealed record ComposedValueTests
{
    // An aggregate of a value that reads the outer row alone, through a membership
    // test, a subquery test, a deferred operand and a Utc timestamp, is taken of a
    // derived table of the rows.
    [Fact]
    public void AggregatesValuesOfTheOuterRowFromADerivedTable()
    {
        decimal one = 1m;
        decimal zero = 0m;
        Expression query = Orders
            .Select(o => new
            {
                Tagged = Customers
                    .Where(c => c.Id == o.CustomerId)
                    .Max(c => o.Tags.Contains("x") ? 1 : 0),
                Listed = Customers
                    .Where(c => c.Id == o.CustomerId)
                    .Max(c => Orders.Select(x => x.Id).Contains(o.Id) ? 1 : 0),
                Shared = Customers
                    .Where(c => c.Id == o.CustomerId)
                    .Max(c => o.Id < 0 ? one / zero : o.Total),
                Moment = Customers
                    .Where(c => c.Id == o.CustomerId)
                    .Max(c => new DateTime(
                        2024,
                        1,
                        1,
                        o.Quantity,
                        0,
                        0,
                        DateTimeKind.Utc
                    )),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT (SELECT MAX(t2."Value") FROM (SELECT CASE WHEN 'x' = ANY (t0."Tags") THEN 1 ELSE 0 END AS "Value" FROM "Customer" AS t1 WHERE t1."Id" = t0."CustomerId") AS t2) AS "Tagged", (SELECT MAX(t5."Value") FROM (SELECT CASE WHEN t0."Id" IN (SELECT t4."Id" FROM "Order" AS t4) THEN 1 ELSE 0 END AS "Value" FROM "Customer" AS t3 WHERE t3."Id" = t0."CustomerId") AS t5) AS "Listed", (SELECT MAX(t7."Value") FROM (SELECT CASE WHEN t0."Id" < 0 THEN TRIM_SCALE(ROUND(ROUND((SELECT 1.), 30) / 0, 29 - LENGTH(CAST(TRUNC(ABS(ROUND((SELECT 1.), 30) / 0) / 0.79228162514264337593543950335) AS text)))) ELSE t0."Total" END AS "Value" FROM "Customer" AS t6 WHERE t6."Id" = t0."CustomerId") AS t7) AS "Shared", (SELECT MAX(t9."Value") FROM (SELECT MAKE_TIMESTAMP(CASE WHEN (t0."Quantity" >= 0) AND (t0."Quantity" <= 23) THEN 2024 ELSE 0 END, 1, 1, t0."Quantity", 0, 0) AS "Value" FROM "Customer" AS t8 WHERE t8."Id" = t0."CustomerId") AS t9) AT TIME ZONE 'UTC' AS "Moment"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    // The elements of a group read by a window function over the groups: the first
    // of them, their values joined, and their distinct values joined.
    [Fact]
    public void ReadsAggregatesOfGroupsInWindowFunctions()
    {
        Expression query = Trees
            .Over(
                Orders.GroupBy(o => o.CustomerId),
                null,
                rows => rows.OrderBy(g => g.Key),
                null,
                (g, w) =>
                    new
                    {
                        First = w.Lag(
                            x => x.OrderBy(o => o.Id).Select(o => o.Number).First(),
                            1,
                            null
                        ),
                        Joined = w.Lag(
                            x =>
                                string.Join(
                                    ",",
                                    x.OrderBy(o => o.Id).Select(o => o.Number)
                                ),
                            1,
                            null
                        ),
                        Distinct = w.Lag(
                            x =>
                                string.Join(
                                    ",",
                                    x.OrderBy(o => o.Id)
                                        .Where(o => o.IsPaid)
                                        .Select(o => o.Note)
                                        .Distinct()
                                ),
                            1,
                            null
                        ),
                    }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT LAG((ARRAY_AGG(t0."Number" ORDER BY t0."Id"))[1], 1) OVER (ORDER BY t0."CustomerId") AS "First", LAG(COALESCE(STRING_AGG(t0."Number", ',' ORDER BY t0."Id"), ''), 1) OVER (ORDER BY t0."CustomerId") AS "Joined", LAG(ARRAY_TO_STRING(ARRAY(SELECT u.v FROM UNNEST(ARRAY_AGG(t0."Note" ORDER BY t0."Id") FILTER (WHERE t0."IsPaid")) WITH ORDINALITY AS u (v, i) GROUP BY u.v ORDER BY MIN(u.i)), ',', ''), 1) OVER (ORDER BY t0."CustomerId") AS "Distinct"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    // SELECT DISTINCT is ordered by what it selects: a Utc timestamp is selected as
    // an instant, and ordered by it.
    [Fact]
    public void OrdersDistinctUtcTimestampsByTheInstantsSelected()
    {
        Expression query = Orders
            .OrderBy(o => new DateTime(2024, 1, 1, o.Quantity, 0, 0, DateTimeKind.Utc))
            .Select(o => new DateTime(2024, 1, 1, o.Quantity, 0, 0, DateTimeKind.Utc))
            .Distinct()
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT DISTINCT MAKE_TIMESTAMP(CASE WHEN (t0."Quantity" >= 0) AND (t0."Quantity" <= 23) THEN 2024 ELSE 0 END, 1, 1, t0."Quantity", 0, 0) AT TIME ZONE 'UTC'
                FROM "Order" AS t0
                ORDER BY MAKE_TIMESTAMP(CASE WHEN (t0."Quantity" >= 0) AND (t0."Quantity" <= 23) THEN 2024 ELSE 0 END, 1, 1, t0."Quantity", 0, 0) AT TIME ZONE 'UTC'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    // A deferred operand is of the kind of its constant, and a float constant that
    // is no NaN needs no guard on its side of a comparison.
    [Fact]
    public void ReadsTheKindOfADeferredOperandAndAFloatConstant()
    {
        DateTime last = new(9999, 12, 31);
        Expression query = Orders
            .Where(o => o.Quantity < 1.5f)
            .Select(o => o.IsPaid ? last.AddDays(1) : o.CreatedAt)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CASE WHEN t0."IsPaid" THEN (SELECT TIMESTAMP '9999-12-31 00:00:00') + (1 * INTERVAL '1 day') ELSE t0."CreatedAt" END
                FROM "Order" AS t0
                WHERE CAST(t0."Quantity" AS real) < CAST(1.5 AS real)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    // A sequence converted to IEnumerable<T> in a tree built by hand is the query
    // below the conversion.
    [Fact]
    public void ReadsAQueryBelowAConversionToASequence()
    {
        ParameterExpression customer = Expression.Parameter(typeof(Customer), "c");
        Expression<Func<Order, bool>> paid = o => o.IsPaid;
        Expression query = Expression.Call(
            typeof(Queryable),
            nameof(Queryable.Where),
            [typeof(Customer)],
            Customers.Expression,
            Expression.Lambda<Func<Customer, bool>>(
                Expression.Call(
                    typeof(Enumerable),
                    nameof(Enumerable.Any),
                    [typeof(Order)],
                    Expression.Convert(
                        Orders.Where(paid).Expression,
                        typeof(IEnumerable<Order>)
                    )
                ),
                customer
            )
        );

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                WHERE EXISTS (SELECT 1 FROM "Order" AS t1 WHERE t1."IsPaid")
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    // Max of new T[n] with a comparer has no translation.
    [Fact]
    public void RefusesExtremeOfDefaultsWithAComparer()
    {
        Expression query = Orders
            .Select(o => new string[o.Id].Max(StringComparer.Ordinal))
            .Expression;

        _ = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
    }
}
