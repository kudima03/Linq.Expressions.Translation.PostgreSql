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

The README's "Translation conventions" section is the contract; the expected SQL in the tests follows it rule by rule. Change both together.

**Test project:** `Linq.Expressions.Translation.PostgreSql.Tests` targets `net10.0` and uses xunit.
- `Infrastructure/CapturingQueryable<T>` is the test-only root: its provider builds trees and, for terminal operators (`Count`, `First`, …), records the executed expression and returns `default` — nothing is executed.
- Expected SQL is a raw string literal with one clause per line; `SqlAssert` joins the lines with single spaces, so the compared statement is one line.
- Every test goes through `SqlAssert`: `Translates(expectedSql, query)` (or `Translates(expectedSql, source, q => q.Count())` for terminal operators) wraps the tree in `PostgreSqlQuery` and compares `TextValue`; `NotSupported(query, construct)` expects a `NotSupportedException` whose message names the construct.
- `Model/` holds the tables (`Customer`, `Order`, `OrderLine`, `Product`).
- Translation tests are skipped with `Skip = Skipped.NotImplemented` until the translator exists (`xUnit1004` is suppressed in the test project for that reason). Combinations use `[Theory]` with `MemberData`/`InlineData`.
- `ExpectedStatementTests` is not skipped: it invokes every test method (every data row) with `SqlAssert` in recording mode, checks that each asserts exactly one outcome, and parses every expected statement with `pgsqlparser` (libpg_query, the PostgreSQL parser) as a single `SELECT`.

**Multi-targeting:** net8.0, net9.0, net10.0.

**Publishing:** triggered by pushing a semver tag (pattern `*.*.*`). The tag becomes the `PackageVersion`. The workflow publishes to both GitHub Packages and NuGet.org.

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
