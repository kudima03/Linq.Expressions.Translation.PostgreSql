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

**Test project:** `Linq.Expressions.Translation.PostgreSql.Tests` targets `net10.0` and uses xunit.

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
