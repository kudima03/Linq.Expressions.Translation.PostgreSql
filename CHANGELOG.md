# Changelog

All notable changes to `Linq.Expressions.Translation.PostgreSql` are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.1.0-preview.0.7.0] - 2026-10-28

### Added

- An aggregate (`Count`, `LongCount`, `Sum`, `Average`, `Min`, `Max`, `Any`, `All`) in the projection of a query over the rows that projection reads — the same query, in any order, below any `Where` and `Select` of the aggregate's own — is a window aggregate over all of them, `COUNT(*) OVER ()`, `COALESCE(SUM(...) FILTER (WHERE ...) OVER (), 0)`, ..., computed in the pass that reads the rows, instead of a scalar subquery that reads the sources (and runs their joins) again: `orders.Select(o => new { o.Id, Share = o.Total / orders.Sum(x => x.Total) })` is `SELECT t0."Id", t0."Total" / COALESCE(SUM(t0."Total") OVER (), 0) AS "Share" FROM "Order" AS t0`. The rows have to be the rows the projection reads (not once they are paged or made distinct) and the aggregate's lambdas must read nothing but their own element; anything else stays a scalar subquery. A projection holding a window is extended only by a projection, an order, paging or `Distinct`, which SQL allows beside one; a filter, grouping, join or aggregate over it wraps it into a derived table.

### Changed

- A nullable value read as its value, `Nullable<T>.Value` or a cast `(T)nullable`, is never NULL where the C# evaluates (it would throw), so it is non-nullable to the translation: it compares and joins with `=` instead of `IS NOT DISTINCT FROM`, also as a member of a composite key, which lets PostgreSQL hash the join, and a left join of such a value reads NULL for an unmatched row as before.

## [0.1.0-preview.0.6.3] - 2026-10-28

### Changed

- `DateOnly.Parse` and `DateTime.Parse` with the invariant culture are `TO_DATE(..., 'MM/DD/YYYY')` and `TO_TIMESTAMP('+00 ' || ..., 'TZH MM/DD/YYYY HH24:MI:SS') AT TIME ZONE 'UTC'` instead of a cast of the text rewritten by `REGEXP_REPLACE`, which ran the regular expression for every value read (about twenty times the cost of the cast, so that a date range join over a few thousand rows a side took minutes instead of seconds). They still read the invariant short date month first whatever the server's `DateStyle`: a month and a day of one or two digits, a four-digit year, any single separator, leading spaces and a 24-hour time that may be missing or lack its seconds. ISO 8601 and a month name, which were left to the server's `DateStyle`, now fail on the server, and a two-digit year is the year itself. The text is read at a fixed `+00` offset and taken back as UTC, which keeps a `DateTime` as written even in a daylight-saving gap of the session's time zone.

## [0.1.0-preview.0.6.2] - 2026-10-28

### Fixed

- An operand that throws when it is evaluated (a division by a captured zero, ...) in a branch of a conditional or on the right of `??`, `&&` or `||` no longer fails the translation, as LINQ to Objects evaluates it only for a row that reaches it: the failing operation is deferred with a scalar subquery, so that PostgreSQL neither folds it while planning the statement nor computes it for a row that does not reach it. Elsewhere the exception still propagates.
- `DateOnly.Parse` and `DateTime.Parse` with the invariant culture read a numeric date month first (`'01/02/2024'` is the second of January), as the invariant culture does, whatever the server's `DateStyle`: the date is rewritten as ISO 8601 before the cast, instead of being read day first on a DMY server.

## [0.1.0-preview.0.6.1] - 2026-10-28

### Fixed

- A left-joined source (`GroupJoin` or a correlated `SelectMany` with `DefaultIfEmpty()`) that projects anything other than its own columns (a `COALESCE`, a constant, a column of the outer element, ...) is joined as a derived table, so that an unmatched row reads `NULL` for it, as the default element is in LINQ to Objects, instead of evaluating it on the `NULL`s of the missing row or failing the statement.
- A constant column of `Concat`/`Union`/`Intersect`/`Except` is nullable when the column of the other side is, so that a comparison with it uses `IS DISTINCT FROM` and keeps the rows C# keeps.
- A left-joined element of a value type (`Select(o => o.Quantity).DefaultIfEmpty()`, a value tuple) is its default for an outer row without a match (`COALESCE(..., 0)`, member by member down to a reference or nullable member, which stays `NULL`), as `DefaultIfEmpty()` yields it, instead of `NULL`.
- A left-joined projection compared with `null` tests a column that is never `NULL` in a match, or every column when there is none, instead of being rejected; a projection that is not left-joined compares as never null.
- The explicit default of `DefaultIfEmpty(value)` under `SelectMany` is `COALESCE(..., value)` for a single value that is never `NULL` in a matching row, instead of being ignored; a nullable value or a projection is rejected, as `NULL` cannot be told from the absence of a row.

## [0.1.0-preview.0.6.0] - 2026-10-28

### Added

- Translation of `MinBy` and `MaxBy` as the first row in the order of the key (`NULLS LAST`, so that elements with a null key are skipped unless every key is null) and then of the source, so that ties resolve as in .NET; `StringComparer.Ordinal` is accepted as `COLLATE "C"`, and a `Guid` key is rejected like `Min`/`Max` of `Guid`.
- Translation of `IntersectBy` and `ExceptBy` as a filter on the membership of the key in the second sequence, a subquery or an in-memory collection of keys, with the null semantics of `Contains`, keeping one element per key with `DISTINCT ON` as `DistinctBy` does.
- Translation of `UnionBy` as `DISTINCT ON (keys)` over both sides combined with `UNION ALL`, each marked with its side, so that the element of the first sequence is the one kept; the result has no order, like `Union`.

## [0.1.0-preview.0.5.1] - 2026-10-27

### Fixed

- `Join` and `GroupJoin` keys compare with `=`, so null keys never match, as in LINQ to Objects; the members of a composite key (an anonymous type or a tuple) still match with `IS NOT DISTINCT FROM` when both are nullable.
- `Contains` on an array column (`= ANY (...)`) of a non-null item is `COALESCE(..., FALSE)` as a value or under `NOT`, as an array may hold `NULL` elements.
- A `Utc` `DateTime` constant renders as a `TIMESTAMP` instead of a `TIMESTAMPTZ`, as C# compares `DateTime` values by their ticks whatever their kind; it becomes a `TIMESTAMPTZ` only once converted to a `DateTimeOffset`.

### Changed

- String lengths and positions are documented to count code points, as PostgreSQL does, not UTF-16 code units.

## [0.1.0-preview.0.5.0] - 2026-10-27

### Added

- Translation of `Parse` of `bool` and `Guid`, and of numbers, `DateOnly`, `TimeOnly` and `DateTime` with `CultureInfo.InvariantCulture` (plus constant `NumberStyles`/`DateTimeStyles`), as a `CAST` from text. The text has to be in PostgreSQL's input syntax; the current-culture overloads are still rejected.
- Ordinal string comparison: `string.CompareOrdinal(a, b)` compared with `0` is translated as a comparison under `COLLATE "C"`, ranking a null string below every other as .NET does, and the ordering operators accept `StringComparer.Ordinal` (`ORDER BY ... COLLATE "C"`).
- Translation of `DistinctBy` as `SELECT DISTINCT ON (keys)`, keeping a source order that does not start with the keys (for example the latest order per user).
- Tick-based date and time arithmetic: `DateTime.Ticks`, `TimeOnly.Ticks`, `TimeSpan.FromTicks`, `AddTicks` and `new DateTime(ticks[, kind])` of a computed count, in microseconds, the finest unit PostgreSQL keeps.
- A `Where` on the matches of a `GroupJoin` before `DefaultIfEmpty()`, possibly chained, becomes part of the `LEFT JOIN` condition, so unmatched rows are kept.
- Constant tuples (`default(ValueTuple<int?, string?>)` or a captured one) are projected member by member as literals.
- One row of aggregates over tables: a constant one-element sequence as the root (`new[] { 0 }.AsQueryable().Select(_ => new { Orders = orders.Count(), ... })`) is translated as a `SELECT` of scalar subqueries without `FROM`.

## [0.1.0-preview.0.4.0] - 2026-10-26

### Added

- Translation of `Math.Round` (and `decimal.Round`/`double.Round`) half to even, .NET's default, of decimals and doubles, with or without digits, and of `Math.Round` of doubles away from zero. The half-way case is decided explicitly, as PostgreSQL rounds a numeric half away from zero and a double by the platform's rule.

## [0.1.0-preview.0.3.0] - 2026-10-25

### Added

- Translation of `Order()`/`OrderDescending()`, which sort the elements themselves, and of `Any()` on an array column (`CARDINALITY(...) > 0`).
- Translation of the fields and totals of a `TimeSpan` (`Days`, `Hours`, `Minutes`, `Seconds`, `Milliseconds`, `Total...` and `Ticks`).
- Conversion of a `DateTime` to a `DateTimeOffset`, so comparisons such as `o.PaidAt > o.CreatedAt` or `DateTimeOffset.UtcNow > o.CreatedAt` are translated instead of rejected.
- Translation of `DateTime.Today` as the start of the local day, like `DateTime.Now.Date`.
- A left-joined entity can be compared with `null` (`x.o == null`, `g.Count(x => x.o != null)`); it is null only where the left join found no row.

### Changed

- Only properties with a public getter and no index parameters are mapped to columns; indexers and write-only properties are no longer selected.
- A reference property without nullable annotations (`#nullable disable`) is treated as nullable, so comparisons, ordering and concatenation keep its `NULL` rows as C# does.

### Fixed

- Nullability is derived from conditional branches, scalar subqueries and `MIN`/`MAX` of reference types, both sides of `Concat`/`Union`/`Intersect`/`Except` and computed columns of a left-joined source, instead of from the CLR type alone.
- A negated comparison with a nullable correlated operand (`m.Id != e.ManagerId`) uses `IS DISTINCT FROM` and keeps the rows C# keeps.
- `Contains` matches a null item with a null element of a subquery or array column, and `Join`/`GroupJoin` match two null keys, as in C#.
- `All` and `OfType` on a grouped query restrict it in `HAVING` instead of putting aggregates into `WHERE`; `OfType` drops nulls from a nullable sequence and checks the entity's table type, and `Cast` to an entity type the table is not assignable to is rejected.
- `Min`/`Max` of booleans use `BOOL_AND`/`BOOL_OR` instead of `MIN`/`MAX`, which PostgreSQL lacks; `Min`/`Max` of `Guid` are rejected, as uuids order differently in .NET.
- `Distinct().Count()` of a nullable value on a group counts `NULL` as one of the distinct values.
- `Select` after `Distinct` over group elements is rejected instead of applying `DISTINCT` to the projected value.
- `ElementAtOrDefault` with a negative index yields no row instead of the first one.
- A paged query wrapped into a derived table keeps ordering by keys that are not selected, so `First`, `Last`, `Reverse` and `TakeLast` after it respect the original order.
- A grouping that is paged, made distinct or combined keeps its key selectable and its result rejected, instead of silently selecting the keys or failing on `g.Key`.
- A constructed object of constants in a projection (`new CustomerSummary(1, "a")`) is projected member by member instead of being rejected.
- A lifted exclusive-or of booleans (`o.IsGift ^ o.IsPaid`) stays `NULL` when an operand is, instead of being coalesced to `FALSE`.
- `Trim`, `TrimStart` and `TrimEnd` remove every white-space character, not only spaces, and invisible characters in string literals are written as `\u` escapes instead of invalid byte escapes.

## [0.1.0-preview.0.2.1] - 2026-10-22

### Changed

- Captured closure fields are read by reflection instead of compiling a delegate for each one.

### Fixed

- `Distinct()` before `Sum` or `Average` on a group is `SUM(DISTINCT ...)`/`AVG(DISTINCT ...)` instead of being ignored; `Sum`/`Average` with a selector over distinct group elements is rejected.
- A comparison nested in another comparison keeps its `COALESCE(..., FALSE)`, and a lifted comparison is not treated as a nullable ordering key or equality operand, matching its `bool` type in C#.
- A non-terminal query (`Where`, `Select`, ...) used as a value is rejected instead of being translated as `First()`; a subquery selecting an entity or a projection is rejected instead of rendering a multi-column scalar subquery.
- `float.NaN` and infinities render as `CAST('...' AS real)`, and an empty array constant renders as `'{}'` instead of the untyped `ARRAY[]`.
- `as` casts are rejected instead of rendering as negation, conversions to non-numeric types, `[NotMapped]` members, entities or projections in scalar positions and expressions that are not queries throw `NotSupportedException` instead of `InvalidCastException`, `KeyNotFoundException` or `NullReferenceException`, and a `null` expression throws `ArgumentNullException`.

## [0.1.0-preview.0.2.0] - 2026-10-16

### Added

- Translation of `TakeLast` (a reversed `LIMIT` in a derived table) and `SkipLast` (`LIMIT` by a row-count subquery).
- Translation of `DefaultIfEmpty()` before `Count`/`LongCount`, `Sum`, `Average`, `Min`, `Max` and `Any`, substituting the default for an empty sequence.
- Translation of `Abs`, `Min`, `Max` and `Clamp` on `int`, `long`, `decimal` and `double`, `Math.Clamp`, `Math.BigMul`, `double.IsNaN`/`IsInfinity`/`IsFinite`, and checked subtraction and multiplication.
- Translation of `string.IndexOf` with a start index and count, `LastIndexOf`, `IndexOfAny`, `Remove`, trimming of several characters, and `Contains`/`StartsWith`/`EndsWith` with `StringComparison.Ordinal`.
- Translation of `Millisecond`/`Microsecond`, `AddMilliseconds`, `AddTicks` (whole microseconds), `Add(TimeSpan)`, `DateTime.TimeOfDay`, `DateTimeOffset.Date`/`UtcDateTime`/`Now`, `DateOnly.DayNumber` and `TimeOnly.FromDateTime`.
- `Any()` without a predicate on a group.
- Translation of `string.Format` and interpolated strings, `string.Concat` of an array, `Any(x => x == value)` on collections and array columns, and `Count()`/`LongCount()` of an array column.
- `Where`, `Select`, ordering, `Distinct` and paging on a `GroupJoin` group before an aggregate.

### Changed

- The package is marked `IsAotCompatible`. `TextValue` is annotated `[RequiresUnreferencedCode]`, as columns are read from the public properties of the queried element types, which trimming removes unless they are preserved.
- `ToString()` describes the expression instead of translating it.
- Package validation compares the public API with the `0.1.0-preview.0.1.0` package.

### Fixed

- Columns are ordered without `MetadataToken`, which Native AOT does not provide; inherited properties still come before declared ones.
- `FirstOrDefault`, `SingleOrDefault`, `LastOrDefault` and `ElementAtOrDefault` of a non-nullable value inside a lambda substitute the type's default instead of `NULL` when no row matches.
- `Any`/`All` on a filtered group are `FALSE`/`TRUE` instead of `NULL` when no row matches the filter.
- Chained `Where` calls on a group combine their predicates instead of keeping only the last one.
- `Cast` to another numeric type converts the values, and `OfType` of an incompatible type returns no rows instead of every row.
- `DateOnly.AddMonths`/`AddYears` return a `date`, and shift counts are masked to the operand width as in C#.
- Struct initializers such as `new ValueTuple<int, string> { Item1 = ..., Item2 = ... }` are translated instead of throwing `NullReferenceException`. Members of constructor projections (`new ValueTuple<,>(...)`, `new Tuple<,>(...)`) resolve to the argument whose name matches ignoring case, an initializer replaces the constructor argument it sets, and an unmatched member throws `NotSupportedException` instead of `KeyNotFoundException`.
- Nested prefix operators (`-(-x)`, `~~x`) are parenthesized instead of rendering `--`, which PostgreSQL reads as a comment; `~x` is parenthesized before `+`, `-`, `*`, `/` and `%`, and `NOT x` inside comparisons, `IS NULL` and `IN`.
- `IN`, `IN (SELECT ...)` and `= ANY` of a nullable value are `FALSE` instead of `NULL` when used as a value or under `NOT`, and equality of two nullable correlated columns matches nulls as in C#.
- `Second` and `Millisecond` truncate the fraction instead of rounding it.
- Concatenating a nullable number casts it to `text`, and concatenating a `bool`, an enum or a date or time value is rejected instead of using PostgreSQL's formatting.
- `Distinct` after ordering by a value that is not selected drops the ordering from that key on, which PostgreSQL otherwise rejects.
- An unnamed computed column is aliased `"Value"` when wrapped into a derived table instead of throwing `NullReferenceException`.

## [0.1.0-preview.0.1.0] - 2026-10-15

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
