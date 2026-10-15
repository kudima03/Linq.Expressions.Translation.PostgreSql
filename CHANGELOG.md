# Changelog

All notable changes to `Linq.Expressions.Translation.PostgreSql` are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- `PostgreSqlQuery` — accepts the `Expression` of an `IQueryable<T>` and exposes the PostgreSQL `SELECT` statement for it as `TextValue`.
- Translation of filtering, projection, ordering and paging: `Where`, `Select`, `OrderBy`/`ThenBy` (and their descending forms), `Skip`, `Take`, `Distinct`, `Reverse`, `Cast` and `OfType`, with derived tables when an operator cannot extend the current `SELECT`.
- Translation of comparison, logical, arithmetic, bitwise and conditional operators, `??`, numeric and enum conversions, string concatenation and `Nullable<T>` members, preserving C# null semantics.
- Translation of `string` members, `Math` functions, date and time parts and arithmetic, `DateTime.Now`/`UtcNow`, `Guid.NewGuid`, `Enum.HasFlag` and `ToString` of numbers and `Guid`.
- Translation of `Contains` on in-memory collections into `IN (...)` (with a separate `IS NULL` check for null elements) and on array columns into `= ANY (...)`.
- Translation of terminal operators — `Count`, `LongCount`, `Sum`, `Average`, `Min`, `Max`, `Any`, `All`, `Contains`, `First`, `Single`, `Last`, `ElementAt` and their `OrDefault` forms — into a single statement, and of the same operators inside lambdas into `EXISTS`, `IN (SELECT ...)` and scalar subqueries.
- Translation of `Join`, `GroupJoin` and `SelectMany` into inner, left, cross and lateral joins, and of aggregates over a `GroupJoin` group into correlated subqueries.
- Translation of `GroupBy` into `GROUP BY` and `HAVING`, with aggregates over groups (`FILTER (WHERE ...)`, `COUNT(DISTINCT ...)`, `BOOL_OR`/`BOOL_AND`); returning `IGrouping` results is rejected.
- Translation of `Concat`, `Union`, `Intersect` and `Except` into `UNION ALL`, `UNION`, `INTERSECT` and `EXCEPT`.
- Inline literals for numbers, strings, booleans, enums, `Guid`, `DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly`, `TimeSpan` and arrays; captured variables and parameter-free subtrees are evaluated in memory.
- Table and column mapping by type and property names, overridable with `[Table]`, `[Column]` and `[NotMapped]`.
- `NotSupportedException` naming the offending member, method or node for constructs that cannot be translated.
