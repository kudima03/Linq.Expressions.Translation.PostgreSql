using System.Globalization;
using System.Text.RegularExpressions;

#pragma warning disable CA1305 // culture-sensitive overloads are the subject
#pragma warning disable SYSLIB1045 // Regex.IsMatch is the subject
#pragma warning disable IDE0120 // the LINQ calls are the subject

namespace Linq.Expressions.Translation.PostgreSql.Tests.Unsupported;

public sealed record UnsupportedExpressionTests
{
    private static bool Helper(int value)
    {
        return value > 0;
    }

    private static int Count(string value)
    {
        return value.Length;
    }

    public sealed class OrderWithCustomer
    {
        public int Id { get; init; }

        public Customer Customer { get; init; } = new();
    }

    [Fact]
    public void RejectsUserDefinedMethodInFilter()
    {
        Expression query = Orders.Where(o => Helper(o.Id)).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Helper", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsUserDefinedMethodInProjection()
    {
        Expression query = Orders.Select(o => Count(o.Number)).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Count", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsUserDefinedMethodInOrdering()
    {
        Expression query = Orders.OrderBy(o => Count(o.Number)).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Count", exception.Message, StringComparison.Ordinal);
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void RejectsUserDefinedMethodInGroupKey()
    {
        Expression query = Orders
            .GroupBy(o => Count(o.Number))
            .Select(g => g.Key)
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Count", exception.Message, StringComparison.Ordinal);
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void RejectsUserDefinedMethodInJoinKey()
    {
        Expression query = Orders
            .Join(Orders, a => Count(a.Number), b => b.Id, (a, b) => a.Id)
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Count", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsUserDefinedMethodInsideSubquery()
    {
        Expression query = Customers.Where(c => Orders.Any(o => Helper(o.Id))).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Helper", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsUserDefinedMethodInsideAggregate()
    {
        Expression query = Orders.Executed(q => q.Sum(o => Count(o.Number)));

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Count", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsStringFormat()
    {
        Expression query = Orders.Select(o => string.Format("{0}", o.Id)).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Format", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsRegexMatch()
    {
        Expression query = Orders.Where(o => Regex.IsMatch(o.Number, "^A")).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("IsMatch", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsEnumToString()
    {
        Expression query = Orders.Select(o => o.Status.ToString()).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("ToString", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsDateTimeToString()
    {
        Expression query = Orders.Select(o => o.CreatedAt.ToString()).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("ToString", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsBooleanToString()
    {
        Expression query = Orders.Select(o => o.IsPaid.ToString()).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("ToString", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsNumberToStringWithFormat()
    {
        Expression query = Orders.Select(o => o.Total.ToString("N2")).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("ToString", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsNumberToStringWithCulture()
    {
        Expression query = Orders
            .Select(o => o.Total.ToString(CultureInfo.InvariantCulture))
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("ToString", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsStringCompare()
    {
        Expression query = Orders
            .Where(o => string.Compare(o.Number, "a") > 0)
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Compare", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsCompareTo()
    {
        Expression query = Orders.Where(o => o.Number.CompareTo("a") > 0).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("CompareTo", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsRoundingOfDouble()
    {
        Expression query = Orders.Select(o => Math.Round(o.Weight)).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Round", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsRoundingOfDoubleAwayFromZero()
    {
        Expression query = Orders
            .Select(o => Math.Round(o.Weight, MidpointRounding.AwayFromZero))
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Round", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsBankersRoundingOfDecimal()
    {
        Expression query = Orders.Select(o => Math.Round(o.Total)).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Round", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsBankersRoundingOfDecimalToDigits()
    {
        Expression query = Orders.Select(o => Math.Round(o.Total, 2)).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Round", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsRoundingToEven()
    {
        Expression query = Orders
            .Select(o => Math.Round(o.Total, 2, MidpointRounding.ToEven))
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Round", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsSine()
    {
        Expression query = Orders.Select(o => Math.Sin(o.Weight)).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Sin", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsCosine()
    {
        Expression query = Orders.Select(o => Math.Cos(o.Weight)).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Cos", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsLogarithmWithBase()
    {
        Expression query = Orders.Select(o => Math.Log(o.Weight, 2)).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Log", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsCultureSensitiveUpperCase()
    {
        Expression query = Orders
            .Select(o => o.Number.ToUpper(CultureInfo.CurrentCulture))
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("ToUpper", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsCultureSensitiveLowerCase()
    {
        Expression query = Orders
            .Select(o => o.Number.ToLower(CultureInfo.CurrentCulture))
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("ToLower", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsStartsWithComparison()
    {
        Expression query = Orders
            .Where(o => o.Number.StartsWith("A", StringComparison.OrdinalIgnoreCase))
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("StartsWith", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsEndsWithComparison()
    {
        Expression query = Orders
            .Where(o => o.Number.EndsWith("A", StringComparison.Ordinal))
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("EndsWith", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsContainsComparison()
    {
        Expression query = Orders
            .Where(o => o.Number.Contains("A", StringComparison.OrdinalIgnoreCase))
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Contains", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsEqualsComparison()
    {
        Expression query = Orders
            .Where(o => o.Number.Equals("A", StringComparison.OrdinalIgnoreCase))
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Equals", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsStaticEqualsComparison()
    {
        Expression query = Orders
            .Where(o => string.Equals(o.Number, "A", StringComparison.OrdinalIgnoreCase))
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Equals", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsIndexOfComparison()
    {
        Expression query = Orders
            .Select(o => o.Number.IndexOf("A", StringComparison.Ordinal))
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("IndexOf", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsIsNullOrWhiteSpace()
    {
        Expression query = Orders
            .Where(o => string.IsNullOrWhiteSpace(o.Note))
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains(
            "IsNullOrWhiteSpace",
            exception.Message,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void RejectsPadLeft()
    {
        Expression query = Orders.Select(o => o.Number.PadLeft(10, '0')).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("PadLeft", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsInsert()
    {
        Expression query = Orders.Select(o => o.Number.Insert(1, "x")).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Insert", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsSplit()
    {
        Expression query = Orders.Select(o => o.Number.Split('-')).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Split", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsStringJoin()
    {
        Expression query = Orders.Select(o => string.Join(",", o.Tags)).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Join", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsCharacterAccess()
    {
        Expression query = Orders.Select(o => o.Number[0]).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Chars", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsParsingOfColumn()
    {
        Expression query = Orders.Select(o => int.Parse(o.Number)).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Parse", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsGuidParsingOfColumn()
    {
        Expression query = Orders.Select(o => Guid.Parse(o.Number)).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Parse", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsConvertToInteger()
    {
        Expression query = Orders.Select(o => Convert.ToInt32(o.Number)).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("ToInt32", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsEnumParsing()
    {
        Expression query = Orders
            .Select(o => Enum.Parse<OrderStatus>(o.Number))
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Parse", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsTotalDaysOfTimeSpan()
    {
        Expression query = Orders
            .Select(o => (o.CreatedAt - new DateTime(2024, 1, 1)).TotalDays)
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("TotalDays", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsToday()
    {
        Expression query = Orders.Select(o => DateTime.Today).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Today", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsDateTimeConstructionFromColumns()
    {
        Expression query = Orders.Select(o => new DateTime(o.Quantity, 1, 1)).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("DateTime", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsGetType()
    {
        Expression query = Orders.Select(o => o.GetType() == typeof(Order)).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("GetType", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsGetHashCode()
    {
        Expression query = Orders.Select(o => o.Number.GetHashCode()).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("GetHashCode", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsArrayIndexOfColumn()
    {
        Expression query = Orders.Select(o => o.Tags[0]).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("ArrayIndex", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsArrayAny()
    {
        Expression query = Orders.Select(o => o.Tags.Any()).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Any", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsArraySelect()
    {
        Expression query = Orders
            .Select(o => o.Tags.Select(t => t.Length).Sum())
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Select", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsArrayCreationFromColumns()
    {
        Expression query = Orders.Select(o => new[] { o.Id, o.Quantity }).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("NewArrayInit", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsListInitializationFromColumns()
    {
        Expression query = Orders.Select(o => new List<int> { o.Id }).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("ListInit", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsTupleCreation()
    {
        Expression query = Orders.Select(o => Tuple.Create(o.Id, o.Number)).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Create", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsDelegateInvocation()
    {
        Func<Order, bool> predicate = x => x.IsPaid;

        Expression query = Orders.Select(o => predicate(o)).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Invoke", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsDelegateInvocationInFilter()
    {
        Func<Order, bool> predicate = x => x.IsPaid;

        Expression query = Orders.Where(o => predicate(o)).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Invoke", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsEntityEquality()
    {
        Order other = new();

        Expression query = Orders.Where(o => o == other).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("equality", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsEntityEqualityInCustomerFilter()
    {
        Customer other = new();

        Expression query = Customers.Where(c => c == other).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("equality", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsEntityInequality()
    {
        Customer other = new();

        Expression query = Customers.Where(c => c != other).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("equality", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsNavigationProperty()
    {
        Expression query = new CapturingQueryable<OrderWithCustomer>()
            .Where(o => o.Customer.IsActive)
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Customer", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsProjectionOfNavigationProperty()
    {
        Expression query = new CapturingQueryable<OrderWithCustomer>()
            .Select(o => o.Customer.Name)
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Customer", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsSelectingNavigationEntity()
    {
        Expression query = new CapturingQueryable<OrderWithCustomer>()
            .Select(o => o.Customer)
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Customer", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsEntityWithNavigationAsRoot()
    {
        Expression query = new CapturingQueryable<OrderWithCustomer>().Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Customer", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsLocalDateTimeConstant()
    {
        Expression query = Orders
            .Select(o => new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Local))
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Local", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsDateTimeWithSubMicrosecondPrecision()
    {
        Expression query = Orders
            .Select(o => new DateTime(2024, 1, 1).AddTicks(1))
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("microsecond", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsStringWithNulCharacter()
    {
        Expression query = Orders.Select(o => "a\0b").Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("NUL", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsConstantOfUnmappedType()
    {
        Expression query = Orders.Select(o => new Uri("http://example.com")).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Uri", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsByteArrayConstant()
    {
        Expression query = Orders.Select(o => new byte[] { 1, 2 }).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Byte", exception.Message, StringComparison.Ordinal);
    }
}
