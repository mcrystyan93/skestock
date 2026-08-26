# Coding Conventions

## Core Sections (Required)

### 1) Naming Rules

| Item | Rule | Example | Evidence |
|------|------|---------|----------|
| Files | PascalCase, one primary type per file, filename == type name | `GetAllCategoriesQuery.cs`, `CategoryFilterConfiguration.cs` | `src/Application/Features/Categories/Queries/GetAllCategories/` |
| Feature-slice folders | `Features/<FeatureName>/{Commands,Queries}/<UseCaseVerbNoun>/` per use case | `Features/Items/Commands/DisableItem/` | `src/Application/Features/Items/Commands/` |
| Command/Query verbs | Not fully standardized across slices — `Create`+`Edit`+`Disable`+`Enable` (Items) vs. `Create`+`Update` (Locations, SchoolClasses) vs. `Create` only (Categories) | `EditItemCommand` vs. `UpdateLocationCommand` | `src/Application/Features/{Items,Locations}/Commands/` |
| Endpoint handler methods | Named `static` methods matching the Mediator use case they call (never lambdas — enforced by `Guard.Against.AnonymousMethod`) | `Categories.GetAllCategories`, `Categories.CreateCategory` | `src/Web/Endpoints/Categories.cs`, `src/Web/Infrastructure/MethodInfoExtensions.cs` |
| Functions/methods | PascalCase (C# convention), async methods return `ValueTask`/`ValueTask<T>` for Mediator handlers, `Task<T>` for ASP.NET Core endpoint handlers | `Handle(GetAllCategoriesQuery request, ...)` returns `ValueTask<Result<...>>` | `src/Application/Features/Categories/Queries/GetAllCategories/GetAllCategoriesHandler.cs` |
| Types/interfaces | Interfaces prefixed `I` (`IApplicationDbContext`, `IFilterConfiguration<TEntity>`); DI extension classes named `DependencyInjection` per project namespace | `IApplicationDbContext` | `src/Application/Common/Interfaces/IApplicationDbContext.cs` |
| Error classes | `<Entity>Errors` static class holding nested `sealed class <Reason> : Error` types, each with a `public const string ErrorCode` in `"entity.reason"` snake_case form | `CategoryErrors.CategoryNotFound`, code `"categories.not_found"` | `src/Application/Common/Errors/CategoryErrors.cs` |
| Validation error codes | `validation.*` string constants in `ValidationErrorCodes` | `Between`, `InvalidSortKey`, `DuplicateName` | `src/Application/Common/Errors/ValidationErrorCodes.cs` |
| Constants/resource names | Centralized in `skestock.Shared.Services` static class — never hardcode Aspire resource/connection-string names | `Services.Database`, `Services.Cache`, `Services.WebApi` | `src/Shared/Services.cs` |

### 2) Formatting and Linting

- Formatter/linter: `.editorconfig` at the repo root drives both IDE formatting and a large set of `dotnet_*`/`csharp_*` analyzer severities (naming, `this.` qualification, expression-bodied members, `var` usage, etc.).
- `TreatWarningsAsErrors=true` is set globally in `Directory.Build.props` — any analyzer/compiler warning fails the build, effectively making the `.editorconfig` rules enforced, not advisory.
- `Nullable` reference types and `ImplicitUsings` are enabled solution-wide (`Directory.Build.props`).
- Most relevant enforced rules: `dotnet_sort_system_directives_first = true`, `dotnet_separate_import_directive_groups = false`, `end_of_line = lf`, `insert_final_newline = true`, 4-space indent for `.cs` files, 2-space for `.xml`/`.json`.
- Run commands: `dotnet build` (fails on any warning due to `TreatWarningsAsErrors`); no separate standalone lint command exists — linting is compiler/analyzer-driven, not a distinct CLI tool like ESLint.

### 3) Import and Module Conventions

- Each project defines its own `GlobalUsings.cs` in its own namespace scope, importing cross-cutting packages once instead of per-file `using`:
  - `Domain/GlobalUsings.cs`: `skestock.Domain.Common`
  - `Application/GlobalUsings.cs`: `Ardalis.GuardClauses`, `Microsoft.EntityFrameworkCore`, `FluentValidation`, `FluentResults`, `Mediator`
  - `Infrastructure/GlobalUsings.cs`: `Ardalis.GuardClauses`, `skestock.Shared`
  - `Web/GlobalUsings.cs`: `Ardalis.GuardClauses`, `skestock.Web.Infrastructure`, `Mediator`
- Each layer's `DependencyInjection.cs` (or `Extensions.cs` for `ServiceDefaults`) lives in its **own** project namespace (`skestock.Application`, `skestock.Infrastructure`, `skestock.Web`, `skestock.ServiceDefaults`) rather than the conventional `Microsoft.Extensions.DependencyInjection` namespace trick — `Web/Program.cs` pulls each in via an explicit `using` statement.
- No barrel-export/index-file pattern (not applicable to C# — namespaces serve this role).
- `Ardalis.GuardClauses` (`Guard.Against.*`) is the standard for argument/null validation instead of manual `if`/`throw` — used consistently in DI registration code (`Guard.Against.Null(connectionString, ...)`).

### 4) Error and Logging Conventions

- Error strategy by layer: **`Application`** returns `FluentResults.Result`/`Result<T>` from Mediator handlers for expected business failures (typed `Error` subclasses carrying HTTP metadata via `ErrorMetadataKeys`), and separately throws `ValidationException`/`ForbiddenAccessException`/`UnauthorizedAccessException`/(Ardalis) `NotFoundException` for cross-cutting/exceptional cases. **`Web`** endpoints check `result.IsFailed` and call `result.ToProblemHttpResult()`; a global `IExceptionHandler` (`ProblemDetailsExceptionHandler`) separately converts the four thrown exception types to `ProblemDetails`. Full detail in `.github/copilot-instructions.md` "Errors & the `Result<T>` pattern".
- Logging style: `LoggingBehaviour<TRequest, TResponse>` (a Mediator pre-processor) logs request start for every command/query; `PerformanceBehaviour` logs when a request exceeds a slow-request threshold. `[TODO]` — exact log level/structured-field conventions were not independently verified line-by-line for these two behaviours in this pass; treat as directionally accurate per file names.
- Sensitive-data redaction: `[TODO]` — no explicit redaction/scrubbing logic was found in `LoggingBehaviour`/`PerformanceBehaviour`; confirm before logging request payloads that may contain PII (e.g. `UserProfile` names) in production.

### 5) Testing Conventions

- Test file naming/location: `tests/Application.UnitTests/` and `tests/Application.FunctionalTests/` both mirror `src/Application/`'s folder structure feature-for-feature and use-case-for-use-case; test class names are `<UseCase><Handler|CommandHandler|Validator>Tests` (e.g. `GetAllCategoriesHandlerTests`, `CreateCategoryCommandValidatorTests`).
- Mocking strategy: Moq for isolated `Application.UnitTests` (no external deps, in-memory/mocked `IApplicationDbContext`); `Application.FunctionalTests` use zero mocks — they boot a real Aspire-hosted SQL Server + Redis via `TestAppHost` and drive full HTTP requests.
- Coverage expectation: `[TODO]` — no coverage threshold/gate config found (coverlet is present as a collector only; no `.runsettings`/CI enforcing a minimum %).

### 6) Evidence

- `.editorconfig`, `Directory.Build.props` (formatting/warnings-as-errors)
- `src/Application/GlobalUsings.cs`, `src/Web/GlobalUsings.cs`, `src/Infrastructure/GlobalUsings.cs`, `src/Domain/GlobalUsings.cs`
- `src/Application/Common/Errors/CategoryErrors.cs`, `ValidationErrorCodes.cs`
- `src/Application/Common/Behaviours/{LoggingBehaviour,ValidationBehaviour}.cs`
- `tests/Application.UnitTests/Features/Categories/*`, `tests/Application.FunctionalTests/Features/Categories/*`

## Extended Sections (Optional)

Not added — a full layer-specific error-handling matrix and commit/branching conventions are already covered narratively in `AGENTS.md` / `.github/copilot-instructions.md`; no CI config exists to derive branching conventions from (scan found no CI/CD pipelines).
