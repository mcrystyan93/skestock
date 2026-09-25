# Coding Conventions

## Core Sections

### 1) Naming Rules

| Item | Rule | Example | Evidence |
|------|------|---------|----------|
| .NET files/types | PascalCase; file matches primary type | `GetAllCategoriesHandler.cs` | `src/Application/Features` |
| Interfaces | `I` prefix | `IApplicationDbContext`, `IQueueSender` | `src/Application/Common/Interfaces` |
| DI extensions | `DependencyInjection` in owning namespace | `skestock.Application.DependencyInjection` | `src/*/DependencyInjection.cs` |
| Feature folders | `Features/<Feature>/{Commands|Queries}/<UseCase>` | `Features/Items/Commands/EditItem` | `src/Application/Features` |
| Constants/resource names | Centralize shared infrastructure names | `Services.Database`, `Services.GoodsReceiptImportQueue` | `src/Shared/Services.cs` |
| Angular aliases | Use `@ske/...` instead of deep relatives across app boundaries | `@ske/shared/categories` | `src/Client/tsconfig.json` |

Command naming is feature-specific: `Items` uses `Edit/Disable/Enable`, Locations/SchoolClasses/Categories use `Update`, OrderLists use lifecycle verbs, and import flows use `Create/Confirm/Process`. Inspect the nearest slice before copying names.

### 2) Formatting and Linting

- Root `.editorconfig` specifies LF, final newline, four-space C# indentation, sorted System usings, file-scoped namespaces, and naming rules.
- `Directory.Build.props` enables nullable reference types, implicit usings, `net10.0`, and `TreatWarningsAsErrors=true`.
- All NuGet versions belong in `Directory.Packages.props`; do not add inline versions to project files.
- No standalone .NET lint command is configured; `dotnet build` is the warning gate.
- Angular uses strict TypeScript compiler options in `src/Client/tsconfig.json`; package scripts provide build/test/watch commands but no dedicated lint script.

### 3) Import and Module Conventions

- Each .NET project owns a `GlobalUsings.cs`; preserve it and use file-scoped namespaces.
- Register services in the owning layer's `DependencyInjection.cs`, not ad hoc in host `Program.cs`.
- Use `Guard.Against.*` for argument/configuration guards.
- Use Application abstractions (`IApplicationDbContext`, `IBlobStorageService`, `IQueueSender`, `IRealtimeNotifier`) from handlers.
- Angular components/services use `inject()`, `@Service()`, signals, `input()`/`output()`, and named exports; import through aliases.

### 4) Error and Logging Conventions

- Expected business failures are `FluentResults.Result`/`Result<T>` with typed `Error` subclasses and metadata keys from `ErrorMetadataKeys`.
- Endpoints map failed results using `ToProblemHttpResult()`; thrown validation/auth/unhandled exceptions go through `ProblemDetailsExceptionHandler`.
- `ValidationBehaviour` throws `ValidationException` after running registered FluentValidation validators.
- Use structured `ILogger` messages with identifiers such as request/message ID, queue name, and resource name; do not log secrets or raw uploaded document contents.
- Queue failures are classified as permanent/retryable, retried through Azure visibility timeout, and moved to a poison queue at exhaustion.

### 5) Testing Conventions

- NUnit is the .NET framework; Shouldly is the assertion library; Moq is used for unit-test doubles.
- Application tests mirror source folders and use focused in-memory/test `IApplicationDbContext` fixtures.
- Functional tests use real Aspire resources and `DatabaseResetter`; never assume a clean database without `TestBase`.
- Angular tests are Vitest specs in `src/Client` and use Angular test utilities where needed.

### 6) Evidence

- `.editorconfig`
- `Directory.Build.props`
- `Directory.Packages.props`
- `src/Application/GlobalUsings.cs`, `src/Infrastructure/GlobalUsings.cs`, `src/Web/GlobalUsings.cs`
- `src/Application/Common/Behaviours`
- `src/Web/Infrastructure/ProblemDetailsExceptionHandler.cs`
- `src/Client/tsconfig.json`
