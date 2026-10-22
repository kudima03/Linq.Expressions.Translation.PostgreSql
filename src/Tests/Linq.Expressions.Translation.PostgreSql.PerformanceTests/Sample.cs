namespace Linq.Expressions.Translation.PostgreSql.PerformanceTests;

public sealed record Sample(string Name, Func<Expression> Query);
