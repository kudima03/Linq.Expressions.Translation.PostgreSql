namespace Linq.Expressions.Translation.PostgreSql.Tests.Model;

[Flags]
public enum ShippingOptions
{
    None = 0,
    Express = 1,
    Insured = 2,
    Fragile = 4,
}
