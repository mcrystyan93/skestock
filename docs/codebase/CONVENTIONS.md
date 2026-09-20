# Coding Conventions

## Core Sections (Required)

### 1) Naming Rules

| Item | Rule | Example | Evidence |
|------|------|---------|----------|
| Files (.NET) | PascalCase, one primary type per file, filename == type name | `GetAllCategoriesQuery.cs`, `CategoryFilterConfiguration.cs` | `src/Application/Features/Categories/Queries/GetAllCategories/` |
| Feature-slice folders | `Features/<FeatureName>/{Commands,Queries}/<UseCaseVerbNoun>/` per use case | `Features/Items/Commands/DisableItem/`, `Features/Categories/Commands/ConfirmCategoryImportBatch/` | `src/Application/Features/*` |
| Command/Query verbs | Still not fully standardized across slices — `Create`+`Edit`+`Disable`+`Enable` (Items) vs. `Create`+`Update` (Locations, SchoolClasses, Categories now includes `Update` too) vs. lifecycle verbs `Create`+`Update`+`Submit`+`Cancel`+`Delete` (OrderLists) vs. import-specific verbs (`Create*ImportBatch`, `Confirm*ImportBatch`, `Process*ImportBatch`) for the async import flows | `EditItemCommand`, `UpdateCategoryCommand`, `SubmitOrderListCommand`, `ConfirmGoodsReceiptImportCommand` | `src/Application/Features/*/Commands/*` |
| Endpoint handler methods | Named `static` methods matching the Mediator use case they call (never lambdas) | `Categories.GetAllCategories`, `CategoryImportBatches.ConfirmCategoryImportBatch` | `src/Web/Endpoints/*.cs` |
| Functions/methods | PascalCase, async Mediator handlers return `ValueTask`/`ValueTask<T>`; ASP.NET Core endpoint handlers return `Task<Results<...>>` | `Handle(GetAllCategoriesQuery request, ...)` | `src/Application/Features/Categories/Queries/GetAllCategories/GetAllCategoriesHandler.cs` |
| Types/interfaces | Interfaces prefixed `I`; DI extension classes named `DependencyInjection` per project namespace | `IApplicationDbContext`, `IRealtimeNotifier` | `src/Application/Common/Interfaces/*` |
| Error classes | `<Entity>Errors` static class holding nested `sealed class <Reason> : Error` types, each with a `public const string ErrorCode` in `"entity.reason"` snake_case form | `CategoryErrors.CategoryNotFound`, code `"categories.not_found"` | `src/Application/Common/Errors/CategoryErrors.cs` |
| Constants/resource names | Centralized in `skestock.Shared.Services` static class — never hardcode Aspire resource/connection-string/queue names | `Services.Database`, `Services.Cache`, `Services.GoodsReceiptImportQueue` | `src/Shared/Services.cs` |

### 2) Formatting and Linting

- `.editorconfig` at the repo root drives IDE formatting and `dotnet_*`/`csharp_*` analyzer severities.
- `TreatWarningsAsErrors=true` (`Directory.Build.props`) — any analyzer/compiler warning fails the .NET build.
- `Nullable` reference types and `ImplicitUsings` are enabled solution-wide.
- Run commands: `dotnet build` (fails on any warning); no separate standalone .NET lint command.
- Client (Angular): standard Angular CLI/ESLint-equivalent tooling via `ng`/`npm` scripts (`dev`, `build`, `watch`, `test`); no dedicated lint script confirmed beyond editor-level TS strictness.

### 3) Import and Module Conventions

- Each .NET project defines its own `GlobalUsings.cs` (`Domain`, `Application`, `Infrastructure`, `Web`, `Worker`), importing cross-cutting packages once.
- Each layer's `DependencyInjection.cs` lives in its own project namespace (`skestock.Application`, `skestock.Infrastructure`, `skestock.Web`, `skestock.ServiceDefaults`); `Web/Program.cs` composes them via explicit `using` statements in order: service defaults → optional Key Vault → Application → Infrastructure → web auth → Web services.
- `Ardalis.GuardClauses` (`Guard.Against.*`) is the standard for argument/null validation instead of manual `if`/`throw`.
- **Angular client**: import across `src/app/features/*` and `src/app/shared/*` boundaries through the `@ske/...` path aliases in `tsconfig.json`, not deep relative paths. Standalone components (no explicit `standalone: true`), signals/`computed`/`input()`/`output()`/`inject()`, native `@if`/`@for`/`@switch`; avoid `ngClass`/`ngStyle`/`@HostBinding`/`@HostListener`.

### 4) Error and Logging Conventions

- Error strategy by layer: `Application` returns `FluentResults.Result`/`Result<T>` for expected business failures (typed `Error` subclasses with `ErrorMetadataKeys` metadata), and separately throws `ValidationException`/`ForbiddenAccessException`/`UnauthorizedAccessException`/`NotFoundException` for cross-cutting/exceptional cases. `Web` endpoints check `result.IsFailed` → `result.ToProblemHttpResult()`; a global `IExceptionHandler` (`ProblemDetailsExceptionHandler`) separately converts the thrown exception types to `ProblemDetails`.
- Logging style: `LoggingBehaviour<TRequest, TResponse>` logs request start for every command/query; `PerformanceBehaviour` logs when a request exceeds a slow-request threshold.
- Worker/queue error handling: failed queue message processing is retried up to a bounded attempt count, then routed to a poison queue (`goods-receipt-import-poison` and equivalents for category/item imports) rather than silently dropped or infinitely retried.
- Sensitive-data redaction: `[TODO]` — no explicit redaction/scrubbing logic confirmed in `LoggingBehaviour`/`PerformanceBehaviour`; confirm before logging payloads that may contain PII (e.g. `UserProfile` names).

### 5) Testing Conventions

- Test file naming/location: `tests/Application.UnitTests/` and `tests/Application.FunctionalTests/` mirror `src/Application/`'s folder structure; test class names are `<UseCase><Handler|CommandHandler|Validator>Tests`.
- Mocking strategy: Moq for isolated `Application.UnitTests`; `Application.FunctionalTests` use zero mocks — real Aspire-hosted SQL Server + Redis via `TestAppHost`.
- Client tests: Vitest, run via `npm test` in `src/Client`.
- Coverage expectation: `[TODO]` — no coverage threshold/gate config found (coverlet present as a collector only).

### 6) Evidence

- `.editorconfig`, `Directory.Build.props`
- `src/Application/GlobalUsings.cs`, `src/Web/GlobalUsings.cs`, `src/Infrastructure/GlobalUsings.cs`, `src/Domain/GlobalUsings.cs`
- `src/Application/Common/Errors/*.cs`, `src/Application/Common/Behaviours/*.cs`
- `src/Shared/Services.cs`
- `src/Client/tsconfig.json`, `.github/instructions/angular-guidelines.instructions.md`

## Extended Sections (Optional)

Not added — a full layer-specific error-handling matrix and commit/branching conventions are already covered narratively in `AGENTS.md` / `.github/copilot-instructions.md`; the only CI config present (`deploy-production.yml`) is deploy-only and doesn't imply a branching model.
