namespace Linq.Expressions.Translation.PostgreSql.Tests.Expressions;

public sealed record CapturedVariableTests
{
    private static readonly int StaticLimit = 6;

    private int Limit { get; } = 5;

    private static int Twice(int value)
    {
        return value * 2;
    }

    private static decimal Rate()
    {
        return 1.25m;
    }

    [Fact]
    public void ComparesWithCapturedInteger()
    {
        int limit = 5;

        Expression query = Orders
            .Where(o => o.Quantity > limit)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Quantity" > 5
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesWithCapturedLong()
    {
        long limit = 5000000000L;

        Expression query = Orders
            .Where(o => o.Points > limit)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Points" > 5000000000
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesWithCapturedDecimal()
    {
        decimal limit = 10.50m;

        Expression query = Orders
            .Where(o => o.Total > limit)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Total" > 10.50
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesWithCapturedDouble()
    {
        double limit = 1.5;

        Expression query = Orders
            .Where(o => o.Weight > limit)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Weight" > 1.5
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesWithCapturedString()
    {
        string number = "A-1";

        Expression query = Orders
            .Where(o => o.Number == number)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Number" = 'A-1'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesWithCapturedStringContainingQuote()
    {
        string number = "O'Brien";

        Expression query = Orders
            .Where(o => o.Number == number)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Number" = 'O''Brien'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesWithCapturedBoolean()
    {
        bool paid = true;

        Expression query = Orders
            .Where(o => o.IsPaid == paid)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."IsPaid" = TRUE
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesWithCapturedDate()
    {
        DateTime date = new(2024, 3, 1, 10, 0, 0);

        Expression query = Orders
            .Where(o => o.CreatedAt > date)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."CreatedAt" > TIMESTAMP '2024-03-01 10:00:00'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesWithCapturedEnum()
    {
        OrderStatus status = OrderStatus.Cancelled;

        Expression query = Orders
            .Where(o => o.Status == status)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Status" = 3
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesWithCapturedGuid()
    {
        Guid id = Guid.Parse("11111111-2222-3333-4444-555555555555");

        Expression query = Orders
            .Where(o => o.TrackingId == id)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."TrackingId" = UUID '11111111-2222-3333-4444-555555555555'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesNullableColumnWithCapturedValue()
    {
        int? limit = 3;

        Expression query = Orders
            .Where(o => o.Priority == limit)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Priority" IS NOT DISTINCT FROM 3
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesNullableColumnWithCapturedNull()
    {
        int? limit = null;

        Expression query = Orders
            .Where(o => o.Priority == limit)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Priority" IS NULL
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesInequalityWithCapturedNull()
    {
        int? limit = null;

        Expression query = Orders
            .Where(o => o.Priority != limit)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Priority" IS NOT NULL
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesNullableStringColumnWithCapturedNull()
    {
        string? note = null;

        Expression query = Orders.Where(o => o.Note == note).Select(o => o.Id).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Note" IS NULL
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesNullableStringColumnWithCapturedValue()
    {
        string? note = "x";

        Expression query = Orders.Where(o => o.Note == note).Select(o => o.Id).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Note" IS NOT DISTINCT FROM 'x'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesOrderingWithCapturedNullableValue()
    {
        int? limit = 3;

        Expression query = Orders
            .Where(o => o.Priority > limit)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Priority" > 3
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesColumnWithCapturedValueOnTheLeft()
    {
        int limit = 5;

        Expression query = Orders
            .Where(o => limit < o.Quantity)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE 5 < t0."Quantity"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesWithTwoCapturedVariables()
    {
        int low = 1;
        int high = 9;

        Expression query = Orders
            .Where(o => (o.Quantity >= low) && (o.Quantity <= high))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."Quantity" >= 1) AND (t0."Quantity" <= 9)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void EvaluatesArithmeticOfCapturedVariables()
    {
        int low = 4;
        int high = 6;

        Expression query = Orders
            .Where(o => o.Quantity > (low + high))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Quantity" > 10
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void EvaluatesNegatedCapturedVariable()
    {
        int limit = 5;

        Expression query = Orders
            .Where(o => o.Quantity > -limit)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Quantity" > -5
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void EvaluatesStringConcatenationOfCapturedVariables()
    {
        string prefix = "A-";
        string suffix = "1";

        Expression query = Orders
            .Where(o => o.Number == prefix + suffix)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Number" = 'A-1'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void EvaluatesConditionalOfCapturedVariables()
    {
        bool flag = true;

        Expression query = Orders
            .Where(o => o.Quantity > (flag ? 5 : 10))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Quantity" > 5
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void EvaluatesMethodCallOnCapturedValue()
    {
        string name = "abc";

        Expression query = Orders
            .Where(o => o.Number == name.ToUpper())
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Number" = 'ABC'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void EvaluatesPropertyOfCapturedValue()
    {
        string name = "abc";

        Expression query = Orders
            .Where(o => o.Quantity > name.Length)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Quantity" > 3
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void EvaluatesStaticMethodWithoutColumnArguments()
    {
        Expression query = Orders
            .Where(o => o.Quantity > Twice(5))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Quantity" > 10
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void EvaluatesStaticMethodWithCapturedArgument()
    {
        int limit = 7;

        Expression query = Orders
            .Where(o => o.Quantity > Twice(limit))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Quantity" > 14
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void EvaluatesStaticMethodReturningDecimal()
    {
        Expression query = Orders
            .Where(o => o.Total > Rate())
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Total" > 1.25
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void EvaluatesCapturedStaticField()
    {
        Expression query = Orders
            .Where(o => o.Quantity > StaticLimit)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Quantity" > 6
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void EvaluatesCapturedInstanceProperty()
    {
        Expression query = Orders
            .Where(o => o.Quantity > Limit)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Quantity" > 5
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void EvaluatesMemberOfCapturedObject()
    {
        Product product = new() { Price = 99.9m };

        Expression query = Orders
            .Where(o => o.Total > product.Price)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Total" > 99.9
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void EvaluatesMemberChainOfCapturedObjects()
    {
        Holder holder = new(new Customer { Name = "Ann" });

        Expression query = Orders
            .Where(o => o.Number == holder.Customer.Name)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Number" = 'Ann'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void EvaluatesMemberChainWithMethodCall()
    {
        Holder holder = new(new Customer { Name = "Ann" });

        Expression query = Orders
            .Where(o => o.Number == holder.Customer.Name.ToLower())
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Number" = 'ann'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void EvaluatesCapturedRecordMember()
    {
        Range range = new(2, 8);

        Expression query = Orders
            .Where(o => o.Quantity >= range.Min && o.Quantity <= range.Max)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."Quantity" >= 2) AND (t0."Quantity" <= 8)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void EvaluatesCapturedDateArithmetic()
    {
        DateTime start = new(2024, 1, 1);

        Expression query = Orders
            .Where(o => o.CreatedAt > start.AddDays(7))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."CreatedAt" > TIMESTAMP '2024-01-08 00:00:00'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void EvaluatesCapturedTimeSpanArithmetic()
    {
        DateTime start = new(2024, 1, 1);

        Expression query = Orders
            .Where(o => o.CreatedAt > start + TimeSpan.FromHours(6))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."CreatedAt" > TIMESTAMP '2024-01-01 06:00:00'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void KeepsColumnDependentSubtreeInSql()
    {
        int limit = -5;

        Expression query = Orders
            .Where(o => o.Quantity > Math.Abs(limit) + o.CustomerId)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Quantity" > (5 + t0."CustomerId")
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesBooleanLiteralResultOfCapturedVariable()
    {
        bool flag = true;

        Expression query = Orders
            .Where(o => flag && o.IsPaid)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE TRUE AND t0."IsPaid"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesNegatedCapturedFlag()
    {
        bool flag = true;

        Expression query = Orders
            .Where(o => !flag || o.IsPaid)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE FALSE OR t0."IsPaid"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsConditionalOnCapturedFlag()
    {
        bool flag = true;

        Expression query = Orders
            .Where(o => flag ? o.Quantity > 5 : o.Quantity > 1)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE CASE WHEN TRUE THEN t0."Quantity" > 5 ELSE t0."Quantity" > 1 END
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsCapturedInteger()
    {
        int limit = 5;

        Expression query = Orders.Select(o => limit).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT 5
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsCapturedString()
    {
        string name = "abc";

        Expression query = Orders.Select(o => name).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT 'abc'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsCapturedNull()
    {
        string? note = null;

        Expression query = Orders.Select(o => note).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT NULL
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsCapturedNextToColumn()
    {
        decimal rate = 1.25m;

        Expression query = Orders.Select(o => new { o.Id, Rate = rate }).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", 1.25 AS "Rate"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void MultipliesColumnByCapturedVariable()
    {
        decimal rate = 1.25m;

        Expression query = Orders.Select(o => o.Total * rate).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Total" * 1.25
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void MultipliesColumnByCapturedExpression()
    {
        decimal rate = 1.25m;

        Expression query = Orders.Select(o => o.Total * (1 + rate)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Total" * 2.25
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AddsCapturedMemberOfObject()
    {
        Range range = new(2, 8);

        Expression query = Orders.Select(o => o.Quantity + range.Min).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Quantity" + 2
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ConcatenatesCapturedMemberChain()
    {
        Holder holder = new(new Customer { Name = "Ann" });

        Expression query = Orders.Select(o => o.Number + holder.Customer.Name).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Number" || 'Ann'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OrdersByColumnPlusCapturedVariable()
    {
        int offset = 3;

        Expression query = Orders
            .OrderBy(o => o.Quantity + offset)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                ORDER BY t0."Quantity" + 3
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesCapturedCount()
    {
        int count = 12;

        Expression query = Orders.Take(count).Select(o => o.Id).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                LIMIT 12
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SkipsCapturedCount()
    {
        int count = 12;

        Expression query = Orders.Skip(count).Select(o => o.Id).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                OFFSET 12
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesCapturedMemberAsCount()
    {
        Range range = new(2, 8);

        Expression query = Orders.Take(range.Max).Select(o => o.Id).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                LIMIT 8
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsOnCapturedKeyOffset()
    {
        int offset = 1;

        Expression query = Customers
            .Join(
                Orders,
                c => c.Id + offset,
                o => o.CustomerId,
                (c, o) => new { c.Name, o.Id }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t1."Id"
                FROM "Customer" AS t0
                INNER JOIN "Order" AS t1 ON (t0."Id" + 1) = t1."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void GroupsAfterFilterByCapturedVariable()
    {
        decimal limit = 100m;

        Expression query = Orders
            .Where(o => o.Total > limit)
            .GroupBy(o => o.CustomerId)
            .Select(g => new { CustomerId = g.Key, Count = g.Count() })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId", CAST(COUNT(*) AS integer) AS "Count"
                FROM "Order" AS t0
                WHERE t0."Total" > 100
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CountsRowsAboveCapturedLimit()
    {
        decimal limit = 100m;

        Expression query = Orders.Executed(q => q.Count(o => o.Total > limit));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COUNT(*) AS integer)
                FROM "Order" AS t0
                WHERE t0."Total" > 100
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SumsColumnTimesCapturedRate()
    {
        decimal rate = 1.25m;

        Expression query = Orders.Executed(q => q.Sum(o => o.Total * rate));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(SUM(t0."Total" * 1.25), 0)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    public sealed record Holder(Customer Customer);

    public sealed record Range(int Min, int Max);
}
