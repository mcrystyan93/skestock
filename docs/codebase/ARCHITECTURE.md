# Architecture

## Core Sections (Required)

### 1) Architectural Style

- Primary style: **Layered Clean Architecture** (Jason Taylor `Clean.Architecture.Solution.Template` 10.8.0) with a **CQRS/feature-slice** organization inside the Application layer, orchestrated end-to-end by **.NET Aspire** (not a traditional container/K8s deployment model).
- Why this classification: dependency direction is strictly inward (`Domain` ← `Application` ← `Infrastructure`/`Web`), confirmed by each project's `.csproj` `ProjectReference`s and by `Application`'s `GlobalUsings.cs` never importing an EF Core provider package directly — only `Microsoft.EntityFrameworkCore` (the abstraction namespace) is global-used, and the concrete `IApplicationDbContext` interface lives in `Application/Common/Interfaces/IApplicationDbContext.cs` while its implementation (`ApplicationDbContext`) lives in `Infrastructure`. Within `Application`, code is organized per business feature (`Features/Categories`, `Features/Items`, ...) rather than per technical concern, and each use case is a self-contained Command/Query + Handler + Validator triple dispatched through **Mediator** (source-generator library, not MediatR).
- Primary constraints: (1) `Application` must never reference an EF Core *provider* package — only the DbContext abstraction — to keep persistence swappable; (2) all cross-cutting concerns (logging, auth, validation, perf, caching) are implemented as ordered Mediator pipeline `Behaviours`, not scattered inline; (3) Aspire resource names (`Services.*` constants in `Shared`) must never be hardcoded as strings — both `AppHost` and app projects resolve connection strings/cache config through them at runtime.

### 2) System Flow

```text
HTTP request
  -> Web/Endpoints/<Feature>.cs (IEndpointGroup static handler, e.g. Categories.GetAllCategories)
  -> ISender.Send(command/query) — dispatched through the Mediator pipeline:
       LoggingBehaviour -> UnhandledExceptionBehaviour -> AuthorizationBehaviour
       -> ValidationBehaviour -> PerformanceBehaviour -> CachingBehavior -> CacheInvalidationBehavior
  -> Features/<Feature>/{Commands|Queries}/<UseCase>/<UseCase>Handler.cs
       (queries: IApplicationDbContext -> EF Core LINQ, optionally via Filtering/Keyset helpers)
       (commands: IApplicationDbContext.Add/Update + SaveChangesAsync, wrapped by SaveChanges interceptors)
  -> Result<T> (FluentResults) returned up through the pipeline
  -> Endpoint checks result.IsFailed -> ToProblemHttpResult() (ProblemDetails) OR TypedResults.Ok/Created(...)
  -> HTTP response (ProblemDetails w/ ApiErrorContract extension, or typed JSON)
```

Additional flows worth noting:
- **Thrown-exception path** (parallel to the `Result<T>` path above): `ValidationException`/`NotFoundException` (Ardalis.GuardClauses)/`UnauthorizedAccessException`/`ForbiddenAccessException` are caught globally by `Web/Infrastructure/ProblemDetailsExceptionHandler.cs` (registered via `app.UseExceptionHandler(options => { })`) and mapped to `ProblemDetails` directly, bypassing the `ApiErrorContract`/`Result` machinery.
- **Startup flow** (`src/Web/Program.cs`): `AddServiceDefaults()` → `AddKeyVaultIfConfigured()` → `AddApplicationServices()` → `AddInfrastructureServices()` → `AddWebServices()` → (dev only) `InitialiseDatabaseAsync()` else `UseHsts()` → HTTPS redirect → permissive CORS → static files → OpenAPI/Scalar → exception handler → `MapEndpoints(assembly)` (reflection-based auto-registration of all `IEndpointGroup` classes).
- **Domain event flow**: `BaseEntity.AddDomainEvent(...)` queues events on the entity; `DispatchDomainEventsInterceptor` (an EF Core `ISaveChangesInterceptor`) dispatches them around `SaveChangesAsync`; `AuditableEntityInterceptor` stamps `CreatedDate`/`CreatedById`/`LastModifiedDate`/`LastModifiedById` on `BaseAuditableEntity` instances in the same interceptor pipeline.

### 3) Layer/Module Responsibilities

| Layer or module | Owns | Must not own | Evidence |
|-----------------|------|--------------|----------|
| `Domain` | Entities (`Category`, `Item`, `Location`, `SchoolClass`, `ClassBalance`, `StockBatch`, `StockTransaction`, `UserProfile`), `BaseEntity`/`BaseAuditableEntity`, `IKeysetEntity`, enums (`ClassStatus`, `StockTransactionType`), `Roles` constants | Any dependency on other layers, persistence/HTTP concerns | `src/Domain/Domain.csproj` (no `ProjectReference`s) |
| `Application` | Mediator commands/queries + handlers, FluentValidation validators, pipeline `Behaviours`, `Result`/`Error` catalog, filtering/keyset-pagination/caching helper libraries, `IApplicationDbContext`/`IIdentityService`/`IUser` interfaces | EF Core provider references, ASP.NET Core types, concrete Identity implementation | `src/Application/Application.csproj`, `src/Application/GlobalUsings.cs` |
| `Infrastructure` | `ApplicationDbContext` (implements `IApplicationDbContext` + `IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>`), EF Core `IEntityTypeConfiguration<T>` per entity, SaveChanges interceptors, `ApplicationUser`/`IdentityService`, HybridCache + Redis distributed cache registration, blob/queue/AI/SignalR adapters | Mediator/CQRS logic, HTTP endpoint definitions | `src/Infrastructure/Data/ApplicationDbContext.cs`, `src/Infrastructure/DependencyInjection.cs` |
| `Web` | `IEndpointGroup` classes (Minimal API), `EndpointRouteBuilderExtensions` (custom `MapGet/Post/Put/Patch/Delete` deriving OpenAPI `operationId` from method name), `ProblemDetailsExceptionHandler`/`ResultProblemDetailsMapper`/`ApiErrorContract`, OpenAPI/Scalar transformers, `CurrentUser : IUser` | Direct EF Core access, business/validation logic (must dispatch through `ISender`) | `src/Web/Endpoints/*.cs`, `src/Web/Infrastructure/*.cs` |
| `Shared` | `Services` static class — Aspire/service resource-name string constants (`DatabaseServer`, `Database`, `Cache`, `WebApi`, `WebFrontend` (reserved, unused), volumes) | Business logic, EF/HTTP code | `src/Shared/Services.cs` |
| `AppHost` | Aspire resource graph: `sqlserver` (`Services.DatabaseServer`), `redis` (`Services.Cache`), `webapi` (`Services.WebApi`, `WithReference`/`WaitFor` both), dashboard shortcut to `/scalar` | Application/business logic — orchestration only | `src/AppHost/Program.cs` |
| `ServiceDefaults` | `AddServiceDefaults()` (OpenTelemetry, health checks, service discovery), `MapDefaultEndpoints()` | Feature-specific logic | `src/ServiceDefaults/Extensions.cs` |

### 4) Reused Patterns

| Pattern | Where found | Why it exists |
|---------|-------------|-----------------|
| CQRS + Mediator pipeline behaviours | `src/Application/Common/Behaviours/*`, registered in `src/Application/DependencyInjection.cs` | Centralizes logging, auth, validation, perf timing, and caching around every command/query without per-handler boilerplate; order is load-bearing (Logging → UnhandledException → Authorization → Validation → Performance → Caching → CacheInvalidation) |
| Reflection-based endpoint auto-discovery | `IEndpointGroup` + `WebApplicationExtensions.MapEndpoints(assembly)` | Avoids a manual endpoint registration list — new `Web/Endpoints/*.cs` classes are picked up automatically; also derives OpenAPI `operationId` from the handler method name (for typed client generation) rather than requiring explicit `WithName(...)` calls |
| `Result<T>` (FluentResults) + typed `Error` classes | `src/Application/Common/Errors/*Errors.cs`, `ResultProblemDetailsMapper.cs` | Lets handlers return "expected" business failures (not-found, duplicate name, etc.) as data rather than exceptions, and lets the `ApiErrorContract` carry a stable machine-readable `Code` for a future frontend to localize |
| Keyset (cursor) pagination + column filtering + tag-based HybridCache | `src/Application/Common/{Keyset,Filtering,Caching}/*` | Reused across every `GetAll<Feature>` query (`Categories`, `Items`, `Locations`, `SchoolClasses`) instead of hand-rolling skip/take pagination or ad hoc cache keys per feature |
| SaveChanges interceptors for audit + domain events | `src/Infrastructure/Data/Interceptors/{AuditableEntityInterceptor,DispatchDomainEventsInterceptor}.cs` | Centralizes `CreatedDate`/`LastModifiedDate`/`CreatedById`/`LastModifiedById` stamping and domain-event dispatch so individual handlers don't need to remember to do it |
| Central Package Management | `Directory.Packages.props` | Single source of truth for every NuGet version across 12 projects |

### 5) Known Architectural Risks

- **Two parallel error-to-HTTP mechanisms** (`Result<T>`/`Error` vs. thrown exceptions caught by `ProblemDetailsExceptionHandler`) coexist by design, but only the `Result` path attaches the richer `ApiErrorContract` (`Code`/`Errors[]`/`Diagnostics.CorrelationId`) that the planned frontend depends on for localized error copy — any handler that throws instead of returning a typed `Error` silently degrades the API contract for consumers. See `CONCERNS.md` §2.
- **`NotFoundException` is referenced but never thrown** anywhere in current handlers (`grep` found zero `Guard.Against.NotFound(...)`/`throw new NotFoundException` call sites) — that branch of `ProblemDetailsExceptionHandler` is currently dead code, and all "not found" cases instead go through the `Result`/`Error` path (e.g. `CategoryErrors.CategoryNotFound`). Two mechanisms exist for the same concern with no enforced convention preventing a future handler from picking the less-rich one. See `CONCERNS.md`.
- **Feature-slice command-verb inconsistency**: `Items` uses `Edit`/`Disable`/`Enable`, `Locations`/`SchoolClasses` use `Update`, `Categories` has only `Create` — no `Update`/`Delete` at all for `Categories`. A new contributor copying the "wrong" slice as a template will introduce further naming drift. See `CONCERNS.md`.
- **Antiforgery enforcement is disabled**: the Angular client configures the expected XSRF cookie/header names, but Web registration and middleware are commented out. This should be reviewed before relying on cookie-authenticated state-changing requests in production. See `CONCERNS.md` (Security).
- **LocalDB connection-string fallback in `appsettings.json`** (see `STACK.md`) suggests a possible non-Aspire run path was intended but is incomplete (no equivalent Redis fallback) — this is either dead config or a latent trap for a developer trying `dotnet run` on `Web` directly.

### 6) Evidence

- `src/Web/Program.cs`, `src/Application/DependencyInjection.cs`, `src/Infrastructure/DependencyInjection.cs`, `src/AppHost/Program.cs` (system flow / startup order)
- `src/Application/Common/Behaviours/*.cs` (pipeline behaviours)
- `src/Web/Infrastructure/{IEndpointGroup,WebApplicationExtensions,ProblemDetailsExceptionHandler,ResultProblemDetailsMapper}.cs`
- `src/Application/Features/Categories/*`, `src/Application/Features/Items/*` (feature-slice pattern + divergence)
- `src/Infrastructure/Data/Interceptors/*.cs`

## Extended Sections (Optional)

Not added — no async/event topology beyond domain-event dispatch documented above, and a full anti-pattern catalog is covered inline in "Known Architectural Risks" / `CONCERNS.md` given repo size.
