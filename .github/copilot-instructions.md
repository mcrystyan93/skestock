# Copilot Instructions — skestock

> See also `/AGENTS.md` for the canonical quick-reference (layer rules, DI convention, Mediator
> pipeline order, endpoint pattern). This file adds deeper file-by-file structure, the
> filtering/keyset-pagination/error conventions, and frontend status that AGENTS.md doesn't
> cover in depth. Don't contradict AGENTS.md — if anything here seems to diverge, AGENTS.md wins.

## Overview

`skestock` (school stock/inventory management) is a .NET 10 (`global.json` pins SDK
`10.0.110`, `rollForward: latestFeature`) solution generated from the **Jason Taylor Clean
Architecture** template, orchestrated end-to-end with **.NET Aspire 13.5.2**. There is
currently **no frontend project** in `src/` — only a backend Web API (`src/Web`).
`.github/instructions/angular-guidelines.instructions.md`, `ng-zorro-guidelines.instructions.md`,
and the `angular-developer`/`ngrx-signalstore` skills are pre-staged for a **planned** Angular +
ng-zorro + NgRx SignalStore frontend that has not been scaffolded yet — don't assume Angular
code exists until a project appears (e.g. under `src/Web` wwwroot as a SPA, or a new `src/<name>`
Angular project referenced from `AppHost`). `Shared.Services.WebFrontend` already reserves a
resource name for it.

The domain is a school inventory/stock system: `Category`, `Item`, `Location`, `SchoolClass`,
`ClassBalance`, `StockBatch`, `StockTransaction`, `UserProfile` (see Domain entities below).
**Four** feature slices are implemented so far under `Application/Features/`: `Categories`,
`Items`, `Locations`, `SchoolClasses` (each with a matching `Web/Endpoints/<Feature>.cs`,
`Application.UnitTests/Features/<Feature>/`, and `Application.FunctionalTests/Features/<Feature>/`
folder). `ClassBalance`, `StockBatch`, and `StockTransaction` have **no** feature slice yet.
**Categories** is still the best reference for the full `GetAll` pagination/filtering/caching
stack (`GetAllCategoriesQuery` + `Handler` + `Validator`), but it only has a single `CreateCategory`
command — it does **not** demonstrate update/disable/enable commands. Note the divergence between
slices before copying a pattern blindly:
- `Items` has the richest command set: `Create`/`Edit`/`Disable`/`Enable` plus `GetAllItems` and
  `GetItemById` — use it as the reference for a full mutable-lifecycle CRUD-ish slice.
- `Locations` and `SchoolClasses` use `Create`/`Update` (not `Edit`) naming for their mutation
  commands, and each expose a `Get<Feature>ById` query alongside `GetAll<Feature>` — check the
  existing slice's naming before assuming `Edit` vs `Update` for a new command.
- All four implemented slices have `CacheConstants.cs`, `<Feature>SortConfiguration.cs`, and
  `<Feature>FilterConfiguration.cs` — treat these three files as required boilerplate for any
  new `GetAll<Feature>` query, not optional extras.

## Solution layout (file-level)

```
skestock.slnx                      # solution file (NOT a classic .sln)
Directory.Build.props              # shared MSBuild props (net10.0, Nullable, TreatWarningsAsErrors)
Directory.Packages.props           # central package management — ALL package versions live here
global.json                        # pins .NET SDK 10.0.110
aspire.config.json                 # Aspire CLI/tooling config (AppHost project path)
AGENTS.md                          # canonical quick-reference for agents

src/
  Domain/                          # no project references — pure C#
    Common/BaseEntity.cs, BaseAuditableEntity.cs, BaseEvent.cs, ValueObject.cs
    Common/IKeysetEntity.cs         # marker: `int Id`, `DateTimeOffset CreatedDate` — required for
                                    #   entities used with the keyset-pagination helpers below
    Constants/Roles.cs              # e.g. Roles.Administrator
    Entities/                       # Category, ClassBalance, Item, Location, SchoolClass,
                                    #   StockBatch, StockTransaction, UserProfile
    Enums/                          # ClassStatus, StockTransactionType
    GlobalUsings.cs                 # global using skestock.Domain.Common;
  Application/                     # → references Domain only
    Common/
      Behaviours/                  # Mediator pipeline behaviours (see pipeline order below)
      Caching/                     # HybridCache tag-based cache/invalidation (see AGENTS.md)
      Errors/                      # FluentResults error catalog — see "Errors & Result pattern" below
      Exceptions/                  # ValidationException, NotFoundException, ForbiddenAccessException
      Filtering/                   # column-filter (WHERE-clause) builder — see below
      Interfaces/                  # IApplicationDbContext, IIdentityService, IUser
      Keyset/                      # cursor-based keyset pagination — see below
      Models/                      # BasePaginationFilter, PaginatedResponse<T>, PaginationSort
      Security/                    # [Authorize] attribute for Mediator requests
    DependencyInjection.cs         # AddApplicationServices() — registers FluentValidation + Mediator pipeline
    GlobalUsings.cs                # Ardalis.GuardClauses, EF Core, FluentValidation, FluentResults, Mediator
    Features/<FeatureName>/        # CQRS feature slices, e.g. Features/Categories/
      <FeatureName>FilterConfiguration.cs  # IFilterConfiguration<TEntity> — whitelists filterable fields
      <FeatureName>SortConfiguration.cs    # IKeysetSortConfiguration<TEntity> — whitelists sort keys
      CacheConstants.cs            # per-feature cache tag/key-prefix constants
      Models/                      # DTOs + request models (e.g. CategoryDto, CategoryRequests)
      Commands/<UseCase>/          # <UseCase>Command.cs + Handler.cs + Validator.cs
      Queries/<UseCase>/           # <UseCase>Query.cs + Handler.cs + Validator.cs
  Infrastructure/                  # → references Application (implements its interfaces) + Domain
    Data/
      ApplicationDbContext.cs      # implements IApplicationDbContext (one DbSet per entity)
      ApplicationDbContextInitialiser.cs  # dev-time seeding, called from Web/Program.cs
      Configurations/              # EF Core IEntityTypeConfiguration<T> classes
      Interceptors/                # AuditableEntityInterceptor, DispatchDomainEventsInterceptor
    Identity/                      # ApplicationUser, IdentityService, custom claims/roles
    DependencyInjection.cs         # AddInfrastructureServices() — EF Core SqlServer, Identity, Redis, HybridCache
  Web/                             # → references Application + Infrastructure + ServiceDefaults
    Endpoints/                     # IEndpointGroup implementations (Categories.cs, Items.cs, Locations.cs,
                                    #   SchoolClasses.cs, Users.cs), auto-discovered
    Infrastructure/
      IEndpointGroup.cs                    # route-prefix + Map(RouteGroupBuilder) contract
      EndpointRouteBuilderExtensions.cs     # MapGet/Post/Put/Patch/Delete(Delegate, pattern) — derives
                                             #   the OpenAPI operationId from the handler METHOD NAME
      MethodInfoExtensions.cs               # Guard.Against.AnonymousMethod — handlers must be named
                                             #   static methods, not lambdas (needed for a stable operationId)
      WebApplicationExtensions.cs           # MapEndpoints(assembly) — reflection-based auto-registration
      ProblemDetailsExceptionHandler.cs     # maps thrown exceptions → ProblemDetails (see Errors section)
      ResultProblemDetailsMapper.cs         # maps failed FluentResults `Result` → ProblemDetails
      ApiErrorContract.cs                   # unified `error` extension on ProblemDetails (Code/Errors/Diagnostics)
      BearerSecuritySchemeTransformer.cs, IdentityApiOperationTransformer.cs,
      ApiExceptionOperationTransformer.cs   # OpenAPI/Scalar doc transformers
    Services/                     # Web-layer services (e.g. CurrentUser : IUser)
    Program.cs                    # composition root — see startup order below
    DependencyInjection.cs        # AddWebServices()
    GlobalUsings.cs                # Ardalis.GuardClauses, skestock.Web.Infrastructure, Mediator
    appsettings.json / .Development.json
    wwwroot/                      # static files served via UseFileServer()
  AppHost/                        # .NET Aspire orchestrator (NOT part of the runtime app)
    Program.cs                    # defines SqlServer, Redis, Web resources — see below
    Extensions.cs                 # AddKeyVaultIfConfigured() and other builder extensions
  ServiceDefaults/                # shared OpenTelemetry/health-check/service-discovery extensions
    Extensions.cs                 # AddServiceDefaults(), MapDefaultEndpoints()
  Shared/                         # cross-cutting constants shared by AppHost + app projects
    Services.cs                  # skestock.Shared.Services — service/resource/db names (NEVER hardcode these strings)

tests/
  Domain.UnitTests/               # project shell exists but has NO tests yet — don't assume coverage here
  Application.UnitTests/          # NUnit, mirrors Application/ folder layout 1:1 (Common/Behaviours,
                                   #   Common/Caching, Common/Filtering, Common/Keyset, and one folder per
                                   #   implemented slice: Features/Categories, Features/Items,
                                   #   Features/Locations, Features/SchoolClasses)
  Application.FunctionalTests/    # full Aspire-hosted stack via TestAppHost
    FunctionalTestSetup.cs        # [SetUpFixture]: boots TestAppHost, waits for DB health, creates WebApiFactory
    Infrastructure/                # WebApiFactory, TestApp, TestBase, DatabaseResetter (Respawn-based)
    Features/Categories/, Features/Items/, Features/Locations/, Features/SchoolClasses/  # end-to-end
                                    #   HTTP tests per use case, mirrors Application/Features
  Infrastructure.IntegrationTests/
  TestAppHost/                    # slimmed-down Aspire host used only by functional tests
```

## Architecture & dependency direction

`Domain` ← `Application` ← `Infrastructure` & `Web`. `Web` is the composition root and depends
on both `Application` and `Infrastructure`. `Shared` is referenced by `AppHost` and the app
projects for resource-name constants only — it has no business logic. `Application` never
references an EF Core provider directly; it only depends on the `IApplicationDbContext`
abstraction (`DbSet<Category>`, `DbSet<Item>`, etc. + `SaveChangesAsync`), keeping persistence
swappable.

### AppHost resource graph (`src/AppHost/Program.cs`)

- `sqlserver` (`Services.DatabaseServer`) — SQL Server container, data volume
  `Services.DatabaseVolumes`, exposes database `Services.Database` (`skestockDb`).
- `redis` (`Services.Cache`) — Redis container, data volume `Services.CacheVolumes`.
- `webapi` (`Services.WebApi`) — the `Web` project; `WithReference`/`WaitFor` both SQL and
  Redis, external HTTP endpoints, and a dashboard shortcut URL to `/scalar`.
- No local fallback connection strings exist in `appsettings.json` — everything is wired
  through Aspire resource references. Running outside Aspire (`dotnet run` on `Web` directly)
  will not have a working DB/cache connection.

### Web startup order (`src/Web/Program.cs`)

```
AddServiceDefaults() → AddKeyVaultIfConfigured() → AddApplicationServices()
  → AddInfrastructureServices() → AddWebServices()
→ (Development only) InitialiseDatabaseAsync()  else  UseHsts()
→ UseHttpsRedirection() → UseCors(AllowAnyMethod/Header/Origin)
→ UseFileServer() → MapOpenApi() → MapScalarApiReference()
→ UseExceptionHandler(options => { }) → Map("/", redirect to /scalar)
→ MapDefaultEndpoints() → MapEndpoints(assembly)
```
Follow this same ordering when adding new middleware/DI wiring — don't insert ad hoc
`builder.Services.AddX()` calls in `Program.cs`; add them to the owning layer's
`DependencyInjection.cs` instead (see AGENTS.md DI convention). Each layer's
`DependencyInjection.cs`/`Extensions.cs` lives in its own project namespace
(`skestock.Application`, `skestock.Infrastructure`, `skestock.Web`, `skestock.ServiceDefaults`)
— `Program.cs` pulls each in via an explicit `using` statement, not the older
`Microsoft.Extensions.DependencyInjection`-namespace trick.

## Mediator pipeline behaviours (`src/Application/Common/Behaviours/`)

CQRS is implemented with the **[Mediator](https://github.com/martinothamar/Mediator)** source-generator
library (`Mediator.Abstractions` + `Mediator.SourceGenerator`), not MediatR. Handler methods
return `ValueTask`/`ValueTask<T>`; the pipeline delegate is `MessageHandlerDelegate<TMessage,
TResponse> next` (called as `next(message, cancellationToken)`). Open-generic behaviours must
constrain `TRequest` with `Mediator.IMessage` (e.g. `where TRequest : notnull, IMessage`).

Registered in `src/Application/DependencyInjection.cs`'s `AddMediator(options => ...)` call, with
`ServiceLifetime.Scoped` (handlers depend on scoped `IApplicationDbContext`), in this exact order:
1. `LoggingBehaviour<TRequest, TResponse>` — pre-processor (`MessagePreProcessor<,>`), logs request start.
2. `UnhandledExceptionBehaviour<TRequest, TResponse>` — wraps and logs unhandled exceptions.
3. `AuthorizationBehaviour<TRequest, TResponse>` — enforces `[Authorize]` on requests (`Common/Security`).
4. `ValidationBehaviour<TRequest, TResponse>` — runs FluentValidation validators.
5. `PerformanceBehaviour<TRequest, TResponse>` — logs slow requests.
6. `CachingBehavior<TRequest, TResponse>` — intercepts `ICacheableQuery<T>` requests (HybridCache read-through).
7. `CacheInvalidationBehavior<TRequest, TResponse>` — invalidates tags for successful `ICacheInvalidation` commands.

New cross-cutting behaviours must be inserted at the correct position in this chain, not
appended blindly — order affects whether auth/validation run before/after logging, timing, or caching.

## Query/filter/pagination conventions (`src/Application/Common/{Filtering,Keyset,Models}`)

These are the building blocks every "get all X" query should reuse — see `GetAllCategoriesQuery`
+ `GetAllCategoriesHandler` + `GetAllCategoriesQueryValidator` as the reference implementation.

**Column filtering** (`Common/Filtering/`):
- `IFilterConfiguration<TEntity>` — implemented once per entity (e.g. `CategoryFilterConfiguration`)
  to whitelist filterable fields as a `Dictionary<string, FilterField<TEntity>>` keyed by
  case-insensitive API field name, each mapping to a typed `Expression<Func<TEntity, TValue>>` selector.
- `ColumnFilter` — the wire-format filter item (`Field`, `Operator` (`FilterOperator` enum:
  Equals/NotEquals/Contains/GreaterThan/LessThan/In/Between), `Value(s)`).
- `FilterQueryBuilder<TEntity>.Apply(query, filters, configuration)` — translates a
  `List<ColumnFilter>` into `IQueryable<TEntity>.Where(...)` expressions against the whitelisted
  fields only (never accepts arbitrary property names from the request).
- `FilterValueParser` — converts raw filter value strings to the target `FilterField.ValueType`.

**Keyset (cursor) pagination** (`Common/Keyset/`), *not* offset/skip-take pagination:
- `IKeysetSortConfiguration<TEntity>` — implemented once per entity (e.g. `CategorySortConfiguration`)
  with `AllowedSortKeys` (API key → ordered EF property names, so a key can map to a composite
  sort like `["Name", "Id"]` for stable tiebreaking), `DefaultSort`, `GetPropertyExpression`, and
  `GetPropertyValue` (used to read values off the last entity for cursor encoding).
- `DynamicSortBuilder<TEntity>.BuildEffectiveSort(requestedSort, configuration)` — merges the
  caller's `List<PaginationSort>` with the configuration's `DefaultSort`/allowlist.
- `CursorCodec<TEntity>.Encode(lastEntity, effectiveSort, configuration)` /
  `.Decode(cursorString)` — base64-encodes/decodes an opaque `CursorState` (sort keys + key
  values) as the `NextCursor`/`Cursor` string; `.MatchesSort(...)` guards against reusing a
  cursor issued for a different sort order (validators call this, e.g.
  `GetAllCategoriesQueryValidator.HaveCursorMatchingRequestedSort`).
- `KeysetPredicateBuilder<TEntity>.ApplyKeysetPredicate(query, effectiveSort, cursorKeyValues, configuration)`
  and `OrderByBuilder<TEntity>.ApplyOrderBy(query, effectiveSort, configuration)` — build the
  `WHERE`/`ORDER BY` clauses for the next page. Handlers fetch `pageSize + 1` rows and trim the
  extra one to compute `HasNextPage` (see `GetAllCategoriesHandler`).
- `BasePaginationFilter` (`SearchTerm`, `PageSize` default `50`, `Cursor`, `Sort`) and
  `PaginatedResponse<T>` (`Data`, `NextCursor`, `HasNextPage`, `Sort`) in `Common/Models/` are the
  standard request/response shapes — feature request DTOs (e.g. `CategoryRequests.GetAllCategoriesRequest`)
  extend `BasePaginationFilter` rather than redefining paging fields.
- Entities used with these helpers should implement `Domain.Common.IKeysetEntity` (`Id`, `CreatedDate`).

## Errors & the `Result<T>` pattern (`src/Application/Common/Errors/`)

Two parallel, **intentionally separate** error-to-HTTP mechanisms coexist — know which one a new
use case should use:

1. **FluentResults `Result`/`Result<T>`** (preferred for expected business failures, e.g. "not
   found", validation-adjacent domain rules): handlers return `Result.Ok(value)` /
   `Result.Fail(new SomeError(...))`. Error classes derive from FluentResults' `Error` and stuff
   HTTP metadata onto `Metadata` using the well-known keys in `ErrorMetadataKeys`
   (`StatusCode`, `Title`, `Code`, `Params`) — see `CategoryErrors.CategoryNotFound` for the
   pattern. In the endpoint, check `result.IsFailed` and call `result.ToProblemHttpResult()`
   (`Web/Infrastructure/ResultProblemDetailsMapper.cs`) to get a `ProblemHttpResult` with a
   unified `error` extension (`ApiErrorContract`: `Code`, `Errors[]`, `Diagnostics.CorrelationId`)
   — this is what the (currently unbuilt) frontend is meant to consume for locale-specific error copy.
2. **Thrown exceptions** (`ValidationException`, `NotFoundException`, `UnauthorizedAccessException`,
   `ForbiddenAccessException` in `Application/Common/Exceptions/`): caught globally by
   `Web/Infrastructure/ProblemDetailsExceptionHandler.cs` (registered via
   `app.UseExceptionHandler(options => { })` + DI registration in `AddWebServices`), which maps
   them to `ProblemDetails` (400/404/401/403) — but does **not** attach the `ApiErrorContract`
   extension, so this path is less rich than the `Result` path. `ValidationBehaviour` still throws
   `ValidationException` for FluentValidation failures (it does not return a `Result`), so both
   error mechanisms will be seen from the same request pipeline.
3. `ValidationErrorCodes` — stable `validation.*` string codes used as FluentValidation
   `.WithErrorCode(...)` values (e.g. `Between`, `InvalidSortKey`, `InvalidCursor`,
   `CursorSortMismatch`, `DuplicateName`) so the frontend can map codes to copy without parsing messages.

When adding a new use case, prefer the `Result<T>` + typed `Error` pattern for anything the API
consumer needs to branch on programmatically; reserve thrown exceptions for truly exceptional/
cross-cutting cases already handled by the global exception handler.

## Endpoints (`src/Web/Endpoints/`)

Not controllers — each feature group is a `class : IEndpointGroup` with a static
`Map(RouteGroupBuilder)`. Route prefix defaults to `/api/{ClassName}` (override via the static
`RoutePrefix` property for nested resource paths, e.g. `/api/Orders/{orderId}/OrderItems`) and
the OpenAPI tag matches the class name. Groups are auto-discovered by reflection via
`app.MapEndpoints(typeof(Program).Assembly)` (`WebApplicationExtensions.cs`) — just add a new
class, no manual registration.

Conventions to follow (see `Categories.cs` / `Users.cs`):
- Use the custom `groupBuilder.MapGet/MapPost/MapPut/MapPatch/MapDelete(handler, pattern)`
  extension overloads (`EndpointRouteBuilderExtensions.cs`), **not** the built-in ASP.NET Core
  ones directly — these derive the OpenAPI `operationId` from the handler's method name (used for
  typed client generation, e.g. nswag) via `WithName(handler.Method.Name)`. Handlers **must** be
  named static methods, not lambdas — `Guard.Against.AnonymousMethod` throws otherwise.
  `pattern` is optional for `MapGet`/`MapPost` (collection-level ops) but required for
  `MapPut`/`MapPatch`/`MapDelete` (resource-level ops, typically `"{id}"`).
- Complex/filterable "get all" queries are exposed as `MapPost(..., "get-all")` (request body,
  not query string) rather than `MapGet`, since the request DTO (`BasePaginationFilter` +
  `Filters`/`Sort`) doesn't serialize cleanly to a query string — see `Categories.GetAllCategories`.
- Handler signature pattern: `static async Task<Results<TSuccess, ProblemHttpResult>> Handler(
  ISender sender, TRequest request, CancellationToken ct)` — map the request DTO to a Mediator
  command/query, `await sender.Send(...)`, then `if (result.IsFailed) return result.ToProblemHttpResult();`
  before returning the typed success result (`TypedResults.Ok(...)`, `TypedResults.Created(...)`, etc.).
- Use `[EndpointSummary]`/`[EndpointDescription]` attributes on handler methods for OpenAPI/Scalar docs.
- `Users.cs` maps ASP.NET Core Identity's built-in `MapIdentityApi<ApplicationUser>()` plus a
  custom `logout` POST endpoint requiring authorization, in the same `IEndpointGroup` class.

## Caching (`src/Application/Common/Caching/`)

Query-side caching and command-side invalidation are cross-cutting `Behaviours`, wired into the
Mediator pipeline last (after `PerformanceBehaviour`): `CachingBehavior<,>` then
`CacheInvalidationBehavior<,>`. Both use `HybridCache`'s native **tag-based invalidation**:
- A query implements `ICacheableQuery<TResponse>` (`Tags`, `BypassCache`, `SlidingExpiration`,
  `BuildCacheKey()`) — see `GetAllCategoriesQuery` for a realistic `BuildCacheKey()` that
  incorporates search/filters/pageSize/cursor/sort via `CacheKeyNormalization` helpers (avoid ad
  hoc string interpolation, which risks inconsistent keys for logically-equal queries).
  `CachingBehavior` calls `HybridCache.GetOrCreateAsync(key, factory, options, tags: message.Tags,
  ...)`, wrapping the `Result<TResponse>` via `ResultCache`/`ResultCacheTransformer` (HybridCache
  can't natively cache a `FluentResults.Result<T>`).
- A command implements `ICacheInvalidation` (`Tags`) — see `CreateCategoryCommand`, which
  invalidates the whole `CategoryListTag` (any new category can affect any cached page/filter/sort
  combination). After a successful `Result`, `CacheInvalidationBehavior` calls
  `HybridCache.RemoveByTagAsync(cacheInvalidation.Tags, ct)`.
- `SlidingExpirationHelper.GetRandomizedSlidingExpiration(baseTtl, jitterPercent)` — adds jitter
  to avoid synchronized cache stampedes; use this rather than a fixed `TimeSpan` for query TTLs.
- Tag invalidation is lazy/logical (a watermark, not physical eviction) — always set a sensible
  `SlidingExpiration` as a safety net too.
- Per-feature `CacheConstants` (e.g. `Features/Categories/CacheConstants.cs`) hold the tag/key
  prefix strings for that feature — add one alongside new feature slices rather than inlining tag
  strings in the query/command.

## Scaffolding new CQRS features

```bash
cd src/Application
dotnet new ca-usecase --name CreateTodoList --feature-name TodoLists --usecase-type command --return-type int
dotnet new ca-usecase -n GetTodos -fn TodoLists -ut query -rt TodosVm
```
If missing: `dotnet new install Clean.Architecture.Solution.Template::10.8.0` (already installed
in this environment — confirmed via `dotnet new list`). This scaffolds a feature folder under
`Application/Features/<FeatureName>/` — **after scaffolding**, still add the feature-specific
`FilterConfiguration`/`SortConfiguration`/`CacheConstants` by hand if the use case needs
filtering/pagination/caching (the template doesn't generate those), following the `Categories`
slice as the model.

## Testing strategy

- **Domain.UnitTests**: project exists but is currently **empty** (no test files) — don't assume
  domain logic has test coverage yet.
- **Application.UnitTests**: NUnit + Shouldly + Moq, no external deps. Folder layout mirrors
  `Application/` 1:1, including `Common/Filtering`, `Common/Keyset`, `Common/Caching`, and
  `Features/Categories/`, `Features/Items/`, `Features/Locations/`, `Features/SchoolClasses/` — put
  new tests at the matching path for the feature being changed.
- **Application.FunctionalTests**: boots the *real* app stack via `TestAppHost` using
  `DistributedApplicationTestingBuilder` (`FunctionalTestSetup`, `[OneTimeSetUp]`). Waits for
  `Services.Database` resource health before building a `WebApiFactory` (`Infrastructure/`), and
  resets the DB between tests/fixtures with `DatabaseResetter` (Respawn-based) — **do not assume
  a clean DB is provided automatically** outside that helper. Requires Docker. Tests live under
  `Features/<FeatureName>/...` mirroring the Application feature they exercise, driving the full
  HTTP pipeline (`TestApp`/`TestBase`).
- **Infrastructure.IntegrationTests**: exercises `ApplicationDbContext`/EF Core directly.
- Run everything: `dotnet test` (needs Docker running for functional/integration tests).
- Test framework stack: NUnit 4.6, Shouldly, Moq, Respawn 7 — not xUnit.

## Package & SDK versions worth knowing (`Directory.Packages.props`)

.NET 10 / ASP.NET Core 10.0.11, EF Core 10.0.11, Mediator 3.0.2 (source-generator-based, not
MediatR), FluentValidation 12.1.1, FluentResults 4.0.0 (`Result`/`Result<T>` — see "Errors &
Result pattern" above), Ardalis.GuardClauses 5.0.0, Aspire 13.5.2 (Hosting/AppHost/Redis/
Azure.Sql/Testing/JavaScript/StackExchange.Redis.DistributedCaching), Microsoft.Extensions.
Caching.Hybrid 10.9.0 (`AddHybridCache()` in `Infrastructure/DependencyInjection.cs`, backed by
Redis distributed cache as L2), Scalar.AspNetCore 2.17.1, OpenTelemetry 1.18.0. Add new package
versions here, never inline in a `.csproj`.

## Auth

ASP.NET Core Identity via `MapIdentityApi<ApplicationUser>()` plus bearer tokens
(`AddBearerToken(IdentityConstants.BearerScheme)`) — not cookie-only. `ApplicationUser`,
`IdentityService`, and Identity DI wiring (`AddIdentityCore<ApplicationUser>().AddRoles<
IdentityRole<int>>().AddEntityFrameworkStores<ApplicationDbContext>().AddApiEndpoints()`) live in
`Infrastructure/DependencyInjection.cs` and `Infrastructure/Identity/`. Roles are int-keyed
(`IdentityRole<int>`); `Domain.Constants.Roles.Administrator` is the only role defined so far.
Custom auth-adjacent endpoints (e.g. `Users.Logout`) sit in the same `IEndpointGroup` class as
the built-in Identity endpoints rather than a separate file.

## Frontend (not yet built — planned)

No Angular/JS project currently exists in the repo (no `package.json`/`angular.json` found
anywhere). When one is added, follow `.github/instructions/angular-guidelines.instructions.md`
and `ng-zorro-guidelines.instructions.md`, and use the `angular-developer`/`ngrx-signalstore`
skills under `.github/skills/` for component/state-management conventions. `Shared.Services.
WebFrontend` already reserves a resource name for wiring a frontend project into `AppHost` once
it exists. The `ApiErrorContract`/`Code` error convention described above (in "Errors & the
`Result<T>` pattern") is explicitly designed for this future frontend to map error codes to
locale-specific copy — keep it in mind when adding new `Error` types.

## Custom agents available (`.github/agents/`)

- `CSharpExpert.agent.md` — general .NET/C# development assistance.
- `csharp-dotnet-janitor.agent.md` — cleanup/modernization/tech-debt tasks on C#/.NET code.
