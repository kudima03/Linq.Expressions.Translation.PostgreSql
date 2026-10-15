namespace Linq.Expressions.Translation.PostgreSql.Tests;

public sealed record PostgreSqlQueryTests
{
    [Fact(Skip = Skipped.NotImplemented)]
    public void SelectsEveryMappedColumnOfRoot()
    {
        SqlAssert.Translates(
            "SELECT t0.\"Id\", t0.\"Name\", t0.\"Email\", t0.\"Region\", t0.\"IsActive\" "
                + "FROM \"Customer\" AS t0",
            new CapturingQueryable<Customer>()
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void SelectsColumnsInPropertyDeclarationOrder()
    {
        SqlAssert.Translates(
            "SELECT t0.\"Id\", t0.\"CustomerId\", t0.\"Number\", t0.\"Note\", "
                + "t0.\"Total\", t0.\"Discount\", t0.\"Quantity\", t0.\"Priority\", "
                + "t0.\"Points\", t0.\"Weight\", t0.\"IsPaid\", t0.\"IsGift\", "
                + "t0.\"Status\", t0.\"Options\", t0.\"CreatedAt\", t0.\"PaidAt\", "
                + "t0.\"ShippedOn\", t0.\"DeliveryTime\", t0.\"TrackingId\", t0.\"Tags\" "
                + "FROM \"Order\" AS t0",
            new CapturingQueryable<Order>()
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void SelectsSingleColumnWithoutAlias()
    {
        SqlAssert.Translates(
            "SELECT t0.\"Name\" FROM \"Customer\" AS t0",
            new CapturingQueryable<Customer>().Select(c => c.Name)
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void TranslatesTerminalOperatorCapturedFromProvider()
    {
        SqlAssert.Translates(
            "SELECT CAST(COUNT(*) AS integer) FROM \"Customer\" AS t0",
            new CapturingQueryable<Customer>(),
            q => q.Count()
        );
    }
}
