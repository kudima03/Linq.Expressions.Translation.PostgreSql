namespace Linq.Expressions.Translation.PostgreSql.ExecutionTests.Infrastructure;

// The rows a statement returned, NULL read as null.
public sealed record ResultSet(
    string Sql,
    IReadOnlyList<string> Columns,
    IReadOnlyList<object?[]> Rows
);
