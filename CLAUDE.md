# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

All `dotnet` commands must be run from the `./src` directory.

```bash
dotnet restore
dotnet tool restore
dotnet build --no-restore -warnaserror
dotnet format --verify-no-changes          # check code style (CI enforces this)
dotnet csharpier check .                   # check formatting (CI enforces this)
dotnet format                              # auto-fix code style
dotnet csharpier format .                  # auto-fix formatting
dotnet test --no-build --verbosity normal  # run tests
dotnet pack --configuration Release -p:Version=<version> --output .
```

CI additionally passes `-p:AssemblyVersion` (pinned to the major) and `-p:FileVersion`; see `.github/workflows/publish-nuget.yml`.

## Architecture

A **LINQ-to-PostgreSQL translator** with exactly one public type, `PostgreSqlQuery`: it takes the `Expression` of an `IQueryable<T>` and exposes the equivalent PostgreSQL `SELECT` statement as `TextValue`. Everything else in the library is `internal`. The package does not execute queries.

Constants (literals, captured variables, in-memory collections) are rendered as escaped inline literals — the output is a single string without bind parameters.

The repository is not part of the Pure ecosystem: do not reference `Pure.*` packages.

**Library layout** (all `internal`):
- `Translation/Evaluator` folds captured variables and parameter-free subtrees into constants (never `Queryable` calls, `DateTime.Now`/`UtcNow`/`Today` or `Guid.NewGuid`).
- `Translation/Translator` (partial, one file per feature) walks the operator chain into a `SelectQuery`/`SetOperationQuery`; an operator that cannot extend the current `SELECT` wraps it into a derived table (`Open`/`Wrap`, see `Barrier`).
- `Shapes/` map a LINQ element onto SQL values (`ScalarShape`, `EntityShape`, `ObjectShape`, `GroupingShape`, `GroupJoinShape`); lambda parameters are bound to shapes.
- `Syntax/` is the SQL tree and renderer; `SqlMode` distinguishes predicate from value positions (a nullable comparison used as a value renders as `COALESCE(..., FALSE)`), and operands are parenthesized when compound.
- `Mapping/` derives table and column names (`[Table]`, `[Column]`, `[NotMapped]`) and nullability.
- The library is `IsAotCompatible` and must build without trimming/AOT warnings and without suppressions. Reflection over element types is confined to `TableMapping`, annotated `[RequiresUnreferencedCode]` and surfaced on `Translator` and `PostgreSqlQuery.TextValue`; everything else must stay analyzer-clean (no `Activator.CreateInstance`, `GetInterface`, `MetadataToken` or other reflection that needs `DynamicallyAccessedMembers` or fails under Native AOT).
- CI also enforces 98% line coverage and a 98% Stryker mutation score: avoid code paths no test exercises.

**Test project:** `Linq.Expressions.Translation.PostgreSql.Tests` targets `net10.0` and uses xunit.
- Every test is the same three steps: build a LINQ query, translate it with `new PostgreSqlQuery(expression).TextValue`, `Assert.Equal` it with the expected SQL. The expected SQL is the specification of the translation.
- `Infrastructure/CapturingQueryable<T>` is the test-only root (`Tables.Customers`, …): its provider only builds trees. For terminal operators (`Count`, `First`, …) `query.Executed(q => q.Count())` returns the expression the provider was asked to execute; nothing is executed.
- Expected SQL is a raw string literal with one clause per line, wrapped in `Sql.Valid(...)`, which joins the lines with single spaces (a subquery may be indented between a line ending in `(` and a line starting with `)`) and asserts the statement parses as one PostgreSQL `SELECT` (`pgsqlparser`, libpg_query).
- `Model/` holds the tables (`Customer`, `Order`, `OrderLine`, `Product`, `Employee`); a table is named after its type, columns after properties, aliases are `t0`, `t1`, … in the order sources are introduced.
- Unsupported constructs are asserted with `Assert.Throws<NotSupportedException>`.
- `xUnit1044` is suppressed in the test project for expression-valued theory data, as are the culture/`StringComparison` analyzers `CA1304`, `CA1307`, `CA1309`, `CA1310`, `CA1311`, `CA1847`, `CA1862`, `CA1865`, `CA1866` and `CA2249`, as the tests exercise exactly those overloads. Combinations use `[Theory]` with `InlineData`/`MemberData`.

**Performance test project:** `Linq.Expressions.Translation.PostgreSql.PerformanceTests` references the test project for its tables and model.
- `Samples` lists 15 queries from simple to complex; `TranslationTimeTests` asserts that each translates cold (no warm-up) under 100 ms and that the output parses.
- The `Translation time` workflow (`.github/workflows/translation-time.yml`) runs it in `Release` on PRs to `main`: `dotnet test Tests/Linq.Expressions.Translation.PostgreSql.PerformanceTests --configuration Release`.
- `stryker-config.json` filters these tests out of mutation testing (Stryker runs in solution mode, which ignores `test-projects`), since timing must not decide whether a mutant is killed.

**Multi-targeting:** net8.0, net9.0, net10.0.

**Publishing:** triggered by pushing a semver tag (pattern `*.*.*`). The tag becomes the `PackageVersion`. The workflow publishes to both GitHub Packages and NuGet.org. `dotnet pack` validates the public API against `PackageValidationBaselineVersion` (the latest released tag, from `git describe --tags --abbrev=0`); move it to the new tag after a release.

## Code Style

Enforced via `.editorconfig`, `dotnet format --verify-no-changes` and `dotnet csharpier check .` in CI:

- No `var` — always use explicit types
- No expression-bodied methods or constructors — use block bodies
- Expression-bodied properties and accessors are required
- File-scoped namespace declarations (`namespace Foo.Bar;`)
- Private fields prefixed with `_` in camelCase (e.g., `_expression`)
- Braces always required, even for single-statement blocks
- Max line length: 90 characters
- Allman brace style — opening brace on its own line for all blocks
- `System.*` using directives sorted first, no blank line between using groups
- Parentheses required in arithmetic and relational binary operators for clarity

## Commit Messages

Do not mention Claude or AI assistance in commit messages.
