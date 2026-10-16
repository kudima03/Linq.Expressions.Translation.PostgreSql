namespace Linq.Expressions.Translation.PostgreSql.Tests.Expressions;

public sealed record OperatorPrecedenceTests
{
    [Fact]
    public void ComplementsComplement()
    {
        Expression query = Orders.Select(o => ~~o.Quantity).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT ~(~t0."Quantity")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void NegatesComplement()
    {
        Expression query = Orders.Select(o => -~o.Quantity).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT -(~t0."Quantity")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComplementsNegation()
    {
        Expression query = Orders.Select(o => ~-o.Quantity).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT ~(-t0."Quantity")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void NegatesNegatedSum()
    {
        Expression query = Orders.Select(o => -(-(o.Quantity + 1))).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT -(-(t0."Quantity" + 1))
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SubtractsFromComplement()
    {
        Expression query = Orders.Select(o => ~o.Quantity - 1).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT (~t0."Quantity") - 1
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void DividesComplement()
    {
        Expression query = Orders.Select(o => ~o.Quantity / 2).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT (~t0."Quantity") / 2
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesRemainderOfComplement()
    {
        Expression query = Orders.Select(o => ~o.Quantity % 3).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT (~t0."Quantity") % 3
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AddsComplementOnTheRight()
    {
        Expression query = Orders.Select(o => 1 + ~o.Quantity).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT 1 + ~t0."Quantity"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void MultipliesComplementInsideSum()
    {
        Expression query = Orders.Select(o => o.Id + (~o.Quantity * 2)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id" + ((~t0."Quantity") * 2)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ShiftsComplement()
    {
        Expression query = Orders.Select(o => ~o.Quantity << 1).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT ~t0."Quantity" << 1
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void XorsComplementOfLong()
    {
        Expression query = Orders.Select(o => ~o.Points ^ 1L).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT ~t0."Points" # 1
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesComplement()
    {
        Expression query = Orders
            .Where(o => ~o.Quantity > -5)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE ~t0."Quantity" > -5
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesNegatedBooleanWithComparison()
    {
        Expression query = Customers
            .Where(c => !c.IsActive == (c.Id > 5))
            .Select(c => c.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Customer" AS t0
                WHERE (NOT t0."IsActive") = (t0."Id" > 5)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesNegatedBooleanForInequality()
    {
        Expression query = Orders
            .Where(o => !o.IsPaid != (o.Quantity > 1))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (NOT t0."IsPaid") <> (t0."Quantity" > 1)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void XorsNegatedBoolean()
    {
        Expression query = Customers
            .Where(c => !c.IsActive ^ (c.Id > 1))
            .Select(c => c.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Customer" AS t0
                WHERE (NOT t0."IsActive") <> (t0."Id" > 1)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TestsNegatedNullableBooleanForNull()
    {
        Expression query = Orders
            .Where(o => !o.IsGift == null)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (NOT t0."IsGift") IS NULL
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ProjectsMembershipOfNegatedBoolean()
    {
        bool[] flags = [true];
        Expression query = Orders.Select(o => flags.Contains(!o.IsPaid)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT (NOT t0."IsPaid") IN (TRUE)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ProjectsNegatedBooleanComparedWithNullableBoolean()
    {
        Expression query = Orders.Select(o => !o.IsPaid == o.IsGift).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT (NOT t0."IsPaid") IS NOT DISTINCT FROM t0."IsGift"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void KeepsNegationBareInsideDisjunction()
    {
        Expression query = Orders
            .Where(o => !o.IsPaid || !(o.IsGift == true))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE NOT t0."IsPaid" OR NOT (t0."IsGift" IS NOT DISTINCT FROM TRUE)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
