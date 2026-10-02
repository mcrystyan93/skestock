# Codex instructions for skestock

Use this as the codebase map and deeper reference alongside [AGENTS.md](../AGENTS.md) and [.github/copilot-instructions.md](../.github/copilot-instructions.md). `AGENTS.md` is canonical for dependency direction, registration order, endpoint discovery, caching, and scaffolding. Keep its rules. If instructions disagree, follow the user, then `AGENTS.md`; check source when documentation looks stale. Prefer repository conventions over generic C# or Angular advice.

## Overview and current state

`skestock` is a school inventory and stock system based on Jason Taylor Clean Architecture template 10.8.0, customized with .NET Aspire, a `Shared` project, reflection-discovered Minimal API endpoint groups, queue processing, and an Angular SPA. The solution is XML `skestock.slnx`. Backend projects target `net10.0`; `global.json` requests SDK `10.0.110` with `latestFeature` roll-forward. `Directory.Build.props` enables nullable references, implicit usings, and warnings as errors. NuGet versions belong in `Directory.Packages.props`.

The runtime flow is:

```text
Angular SPA ──same-origin /api and /hubs/app──> Web
                                                   │
                                      Application/Mediator
                                                   │
                                   IApplicationDbContext, ports
                                                   │
                                      Infrastructure adapters
                                                   │
                  SQL Server · Redis · Azure Blob/Queue (Azurite locally)
                                                   │
                       transactional outbox ──> Worker queue consumers
```

`AppHost` describes and starts the system; it is not the HTTP application. `Web` and `Worker` are separate hosts. The frontend is the independent npm app at `src/Client`, also represented by `Client.esproj` so Aspire can launch its Vite development server.

## Architecture and layer boundaries

Dependencies point inward: Infrastructure/Web → Application → Domain. Domain has no project references. Worker composes Application and Infrastructure without an HTTP host. ServiceDefaults supplies host-level health, discovery, resilience, and telemetry. Shared is for resource/configuration names and small genuinely shared helpers.

Actual project references from the current project files:

```text
Domain                 → (none)
Shared                 → (none)
ServiceDefaults        → (none)
Application            → Domain, Shared
Infrastructure         → Application, Shared
Web                    → Application, Infrastructure, ServiceDefaults
Worker                 → Application, Infrastructure, ServiceDefaults
AppHost                → Shared, Web, Worker
Client                 → independent npm/Angular project
TestAppHost            → Shared, Worker
```

Test projects reference the unit under test; functional/integration tests also reference TestAppHost and/or Web as shown in their `.csproj` files. Keep these references acyclic and do not move EF/Azure implementation dependencies inward.

| Project | Owns | Keep out |
|---|---|---|
| `src/Domain` | Entities, value objects, enums, domain events, audit/entity bases, queue persistence contracts | EF provider/configuration, HTTP, cloud adapters, Application handlers |
| `src/Application` | CQRS feature slices, validation, Mediator behaviors, results/errors, filters/pagination/cache contracts, storage/queue/document interfaces | Concrete EF, Identity stores, Azure SDK adapters, endpoint routing |
| `src/Infrastructure` | `ApplicationDbContext`, EF mappings/migrations/interceptors, Identity, Redis/cache/locks, Blob/Queue, SignalR, document extraction | HTTP endpoint groups and use-case orchestration |
| `src/Web` | HTTP composition, endpoints, auth/CORS, OpenAPI/Scalar, ProblemDetails, static SPA, outbox publisher | Feature logic and direct feature-level EF queries |
| `src/Worker` | Queue polling/acknowledgement/retry/poison handling and scheduled work | HTTP endpoints; feature operations outside Mediator handlers |
| `src/AppHost` | Aspire resource graph and run/publish wiring | Domain or business behavior |
| `src/ServiceDefaults` | Health checks, service discovery, HTTP resilience, OpenTelemetry | Feature-specific behavior |
| `src/Shared` | `skestock.Shared.Services` names and cross-project primitives | Business rules or infrastructure implementations |
| `src/Client` | Angular routes/pages, HTTP services, SignalStore state, SignalR bridge, UI | .NET project references |

The solution includes `Application.UnitTests`, `Domain.UnitTests`, `Worker.UnitTests`, `Infrastructure.IntegrationTests`, `Application.FunctionalTests`, and the test-only Aspire host `TestAppHost`. The Domain test project exists but currently has no test source files.

### CQRS and feature slices

Application uses **Mediator** with source generation (`Mediator.Abstractions` and `Mediator.SourceGenerator`), not MediatR. Follow adjacent `IRequest<TResponse>` / handler patterns. Feature code lives in `src/Application/Features/<Feature>/`; a use case normally gets its own `Commands/<UseCase>` or `Queries/<UseCase>` folder containing request, handler, and validator. Current feature areas: Categories, GoodsReceipts, Items, Locations, OrderLists, ScheduledJobs, SchoolClasses, Statistics, Stock, StockBatches, and SupplyLists. Storage and Queues are separate Application areas.

Copy the nearest slice, not a remembered CRUD template: verbs differ (`Edit` for Items, `Update` for Locations/SchoolClasses, lifecycle verbs for OrderLists, and import-specific Create/Confirm/Process). Goods receipts are ledger-like; inspect the current slice before adding mutations. List slices use explicit filter/sort configurations, cursor pagination, and feature cache constants. Never expose arbitrary entity property names as sort/filter inputs.

Use `IApplicationDbContext` in handlers and `SaveChangesAsync`; concrete EF belongs in Infrastructure. Expected business failures use the established Result/error model and Web's `ToProblemHttpResult()` mapping. Validation failures flow through `ValidationBehaviour` and `ValidationException`; authorization and unexpected exceptions use the existing exception/ProblemDetails path. Preserve error codes and metadata consumed by the client.

### Dependency injection and endpoint seam

Layer registration stays in its owning project/namespace: `AddApplicationServices`, `AddInfrastructureServices`, Web-only `AddWebAuthenticationServices`, `AddWebServices`, and `AddServiceDefaults`. Web composes them in this order:

```text
AddServiceDefaults → AddKeyVaultIfConfigured → AddApplicationServices
→ AddInfrastructureServices → AddWebAuthenticationServices → AddWebServices
```

Keep endpoint-aware authentication registration out of Infrastructure's Worker-safe registration. Mediator pipeline order is significant and is listed in `AGENTS.md` and `src/Application/DependencyInjection.cs`; preserve it when adding behaviors. Cacheable requests implement `ICacheableQuery`; mutations that invalidate tags implement `ICacheInvalidation`.

HTTP features are public `IEndpointGroup` implementations under `src/Web/Endpoints`. Implement static `Map(RouteGroupBuilder)`; `MapEndpoints` discovers groups by reflection. The default route is `/api/{ClassName}` and groups require authorization by default. `RoutePrefix` and `RequiresAuthorization` are static interface members. Custom route-builder overloads in `Web/Infrastructure/EndpointRouteBuilderExtensions.cs` derive the OpenAPI operation ID from the handler method name, so use named static methods rather than anonymous lambdas. Keep handlers thin: bind transport inputs, dispatch through Mediator, map the result. Use typed HTTP results, cancellation tokens, and endpoint summary/description metadata like neighboring groups.

## Solution and file map

```text
src/
  AppHost/          Aspire entry point: SQL Server, Redis, Azurite, Web, Worker, Vite app
  Application/      Common behaviors/contracts plus feature CQRS slices
  Domain/           Pure domain model and persisted queue/outbox contracts
  Infrastructure/   EF, Identity, cloud/cache/realtime/document adapters
  ServiceDefaults/  Shared host defaults
  Shared/           Resource/configuration constants
  Web/              HTTP host, Endpoints/, Infrastructure/, Services/, wwwroot/
  Worker/            Queue processors and scheduled services
  Client/            Angular application
tests/
  Application.UnitTests/           Fast Application/Infrastructure unit tests
  Domain.UnitTests/                 Domain project (currently empty)
  Worker.UnitTests/                 Worker tests, SQLite/fakes/time provider
  Infrastructure.IntegrationTests/ Aspire-backed Infrastructure tests
  Application.FunctionalTests/     HTTP/API tests using WebApiFactory and TestApp
  TestAppHost/                      Aspire graph used by container-backed tests
```

Useful landmarks:

- `src/Application/DependencyInjection.cs`, `Common/Behaviours/`, `Common/Errors/`, `Common/Filtering/`, `Common/Keyset/`, and `Common/Caching/` define cross-feature contracts.
- `src/Infrastructure/Data/ApplicationDbContext.cs`, `Data/Configurations/`, `Data/Interceptors/`, and `Data/Migrations/` own persistence. Entity configuration is separate from entities. Change model/configuration and generate migrations; do not edit generated designer or snapshot files.
- `src/Infrastructure/Data/ApplicationDbContextInitialiser.cs` owns database initialization and development seed data. Web calls initialization on startup; production uses the migration path and skips development demo seeding.
- `src/Web/Infrastructure/IEndpointGroup.cs`, `WebApplicationExtensions.cs`, `EndpointRouteBuilderExtensions.cs`, `ResultEndpointExtensions.cs`, and `ResultProblemDetailsMapper.cs` define routing and response translation.
- `src/Shared/Services.cs` is the source of Aspire resource, queue, database, cache, volume, and configuration-section names. Reuse these constants.
- `src/Worker/Queues/` owns import consumers and dispatch; `src/Worker/Statistics/DailyStatisticsService.cs` owns the daily schedule. Do not extend the sample loop in `src/Worker/Worker.cs` for real processing.

## Stack versions and resource wiring

These are repository pins, not upgrade recommendations. Change versions only through the owning central manifest and lockfile.

| Area | Repository version/source |
|---|---|
| .NET | SDK `10.0.110`, `latestFeature`; `net10.0` target |
| EF Core and ASP.NET Core Identity EF | `10.0.11` in `Directory.Packages.props` |
| Mediator | `3.0.2` source generator + abstractions; **not MediatR** |
| FluentValidation DI | `12.1.1` |
| Aspire | AppHost SDK/core hosting and SQL/Redis integrations `13.5.2`; Azure Storage and Docker integrations `13.5.3` |
| .NET tests | NUnit `4.6.1`, NUnit3TestAdapter `6.3.0`, NUnit.Analyzers `4.14.0`, Shouldly `4.3.0`, Moq `4.20.72` |
| Container-backed tests | Respawn `7.0.0`, Aspire.Hosting.Testing `13.5.2`; EF Sqlite/InMemory `10.0.11` where used |
| Angular | `^22.2.0`; NgRx Signals/Operators `^22.0.1`; ng-zorro-antd `^22.1.1`; RxJS `~7.8.0` |
| Client tooling | npm `11.16.0`, TypeScript `~6.0.3`, Vitest `^5.0.2`, Playwright `^1.63.0`; production container currently builds with Node 22 |

`src/AppHost/Program.cs` maps `Services.DatabaseServer` to Aspire SQL Server and `Services.Database` to `skestockDb`; `Services.Cache` maps to Redis. Local run mode adds Azure Storage with Azurite, `Services.BlobService`, `Services.Queues`, and the `app-files` blob container. Blob/Queue/Table emulator ports are 10000/10001/10002. The Vite app is `Services.WebFrontend`, rooted at `../Client`, and listens on port 7001. The Aspire dashboard host port is 18080; Web advertises Scalar at `/scalar`. Use Aspire's assigned Web URL rather than assuming a port.

AppHost publish wiring differs from run mode: published SQL uses a one-shot migration service and app credentials, and published storage uses the configured public blob endpoint. Do not reuse local emulator connection strings in production. Use names from `skestock.Shared.Services` for resource/config identifiers.

## Critical commands and runtime requirements

Run from the repository root unless a working directory is shown.

```bash
# Restore/build the XML solution. Warnings are build failures.
dotnet build

# Supported whole application; needs a Docker-compatible runtime and Node/npm.
dotnet run --project src/AppHost

# Fast test suites
dotnet test tests/Application.UnitTests
dotnet test tests/Domain.UnitTests
dotnet test tests/Worker.UnitTests

# Container-backed boundaries
dotnet test tests/Infrastructure.IntegrationTests
dotnet test tests/Application.FunctionalTests

# Podman wrapper: activates/checks the user socket and configures Aspire.
./run-functional-tests.sh [extra dotnet test args...]
```

AppHost starts SQL Server, Redis, Azurite, Web, Worker, and Angular/Vite. Direct `dotnet run --project src/Web` is not the supported local stack because it does not provision those dependencies. Functional and Infrastructure integration tests require Docker or a compatible Aspire container runtime. `FunctionalTestSetup` has a 90-second startup deadline, starts `TestAppHost`, waits for database/cache/queue readiness, configures `WebApiFactory`, and creates a Respawn-backed `DatabaseResetter`. `TestBase` resets state before each functional test; use `TestApp` helpers for users, Mediator requests, and entity setup. Tests bypassing this setup must establish their own state. The integration fixture also starts TestAppHost and applies migrations; do not assume it resets database state between tests.

`TestAppHost` currently includes SQL Server, Redis, Azurite Blob/Queue resources, and Worker. It does not launch the real Web host or browser client; functional tests host Web through `WebApiFactory`. `run-functional-tests.sh` starts `podman.socket`, sets `DOCKER_HOST` and `DOTNET_ASPIRE_CONTAINER_RUNTIME=podman`, and runs the functional suite.

Client commands (run inside `src/Client`):

```bash
npm ci
npm test -- --watch=false
npm run build -- --configuration production
npm run e2e                 # requires the full stack running at port 7001
```

`npm ci` uses the committed lockfile. `prebuild` runs `generate:icons`, so a build may refresh the generated icon catalog. Angular unit tests use `@angular/build:unit-test` (Vitest). Playwright is configured in `playwright.config.ts` with `e2e/` specs and Chromium/mobile projects; tests expect AppHost and default to `http://localhost:7001`. During development, the client proxies same-origin `/api` and `/hubs/app`.

EF migrations use the local `dotnet-ef` tool manifest (10.0.11):

```bash
dotnet ef migrations add <MigrationName> --project src/Infrastructure \
  --startup-project src/Web --context ApplicationDbContext --output-dir Data/Migrations
dotnet ef migrations has-pending-model-changes --project src/Infrastructure \
  --startup-project src/Web --context ApplicationDbContext
```

## Conventions and cross-cutting behavior

- Keep central NuGet versions in `Directory.Packages.props`; do not add project-local versions. Preserve file-scoped namespaces and each project's `GlobalUsings.cs`. Use existing `Guard.Against.*` conventions.
- Root `GlobalUsings.cs` files are per project (`Application`, `Domain`, `Infrastructure`, `Web`). Follow the nearest project's usings rather than adding broad globals.
- Pipeline order: `LoggingBehaviour` → `UnhandledExceptionBehaviour` → `AuthorizationBehaviour` → `ValidationBehaviour` → `PerformanceBehaviour` → `CachingBehavior` → `CacheInvalidationBehavior`. This order changes behavior.
- Filtering and ordering use `IFilterConfiguration<TEntity>` and `IKeysetSortConfiguration<TEntity>` allowlists. Cursor helpers are in `Application/Common/Keyset`; API contracts use `BasePaginationFilter` and `PaginatedResponse<T>`. Existing paged queries fetch one extra row to calculate `HasNextPage` and `NextCursor`.
- Cache keys are normalized and invalidation is tag-based. Use feature and entity/collection tags with bounded expirations. HybridCache tag invalidation is logical/lazy; it does not justify unbounded TTLs.
- Persist instants as UTC `DateTimeOffset`; use `DateOnly` for calendar-only concepts (see `docs/adr/0001-utc-datetimeoffset-for-persisted-instants.md`). `AuditableEntityInterceptor` stamps audit fields; `DispatchDomainEventsInterceptor` dispatches events after successful persistence.
- Imports use a transactional outbox: Application/domain writes `OutboxMessage` with business changes, Web's `OutboxPublisherService` publishes envelopes, and Worker consumers restore and dispatch Mediator requests. Delivery is at-least-once; consumers must be idempotent. `ProcessedMessage` records idempotency; exhausted failures go to `<queue>-poison`.
- `DailyStatisticsService` is a Worker hosted service, defaults to 21:00 `Europe/Bucharest`, persists run state, and uses `Services.DailyStatisticsLockKey`. Its current job body is scaffolded/log-only; future work belongs behind Application/Mediator.
- Expected business errors retain typed codes and metadata. ProblemDetails responses also carry the frontend `error` contract; keep that mapping stable.

### Scaffolding

Run the existing CQRS template from `src/Application`, then complete the local slice:

```bash
cd src/Application
dotnet new ca-usecase --name CreateExample --feature-name Examples \
  --usecase-type command --return-type int
```

If unavailable, install `Clean.Architecture.Solution.Template::10.8.0` with `dotnet new install`. The template does not create all local filter/sort/cursor/cache constants, Web endpoint mapping, typed error mapping, or mirrored tests. Compare an adjacent slice. For EF changes, edit model/configuration and generate migrations; do not hand-edit migration designer or snapshot output.

## Authentication and security

Infrastructure registers Identity Core (`ApplicationUser`, `IdentityRole<Guid>`, EF stores). Web's `AddWebAuthenticationServices` adds Identity API endpoints, application cookie as the default scheme, and Identity's bearer-token scheme. Client login requests `useCookies=true`; `authInterceptor` sends credentials for `/api` calls. Preserve this cookie/bearer arrangement; the app does not configure a custom JWT validation scheme.

Endpoint groups require authorization by default. `Users` opts out at group level for Identity login/manage routes; logout requires authorization. `UseIdentityRegistrationDisabled` returns 404 for `/api/Users/register`; accounts are provisioned administratively. Application `AuthorizationBehaviour` also evaluates request authorization metadata, so inspect roles/policies before changing access.

CORS must use explicit origins and allow credentials for cookie auth. AppHost injects `Cors:AllowedOrigins`; localhost fallback is development-only. `AllowAnyOrigin()` is incompatible with credentials. Although the client configures Angular's `XSRF-TOKEN` / `X-XSRF-TOKEN` names and server antiforgery code is scaffolded, server registration/middleware and the endpoint are commented out: do not claim antiforgery is enforced. Preserve same-origin proxy/cookie behavior unless changing the full auth design.

## Frontend architecture (Angular)

The real project is `src/Client`; replace old `src/Web/ClientApp` paths found in staged guidance. It is standalone-style Angular 22.2. Root providers are in `src/Client/src/app/app.config.ts`; root routes are in `app.routes.ts`; routed features are lazy-loaded through layout routes.

```text
src/Client/src/app/
  core/       auth, guards/interceptors, models, routes/query state, SignalR, theme, layouts
  features/   route pages and page-specific stores (categories, items, school classes, login)
  shared/     API clients, reusable collection/detail stores, loading/errors, tables/UI
```

Use named aliases from `src/Client/tsconfig.json` (`@ske/models`, `@ske/auth`, `@ske/features/...`, `@ske/shared/...`) instead of deep relative imports. Shared HTTP clients map to Web route groups such as `/api/Categories`, `/api/Items`, and `/api/Stock`; keep DTO/filter/cursor shapes aligned with server contracts.

State uses NgRx SignalStore, Angular signals, and RxJS through `rxMethod`. Use `patchState()`, `withComputed()`, `mapResponse({ next, error })`, and reusable `withLoadingFeature` / `withProblemDetailsFeature` when neighboring stores do. Export stores by name. Use `withEntities()` for normalized entity CRUD collections. Cursor-paginated screens commonly keep the current page array with pagination metadata; follow the nearest store instead of forcing a new shape. Read `.github/skills/ngrx-signalstore/` for detailed state patterns.

Use `inject()`, `@Service()` for new singleton HTTP services where appropriate, `input()`/`output()`, `computed()`, and native `@if`/`@for`/`@switch`. Angular 22 defaults components to standalone and OnPush; don't add `standalone: true` or explicit OnPush boilerplate. Prefer Signal Forms for new forms while retaining Reactive Forms for existing complex flows. Use class/style bindings and metadata `host`, not `ngClass`, `ngStyle`, `@HostBinding`, or `@HostListener`. Preserve WCAG AA semantics, focus/keyboard behavior, and the existing responsive large/small table split.

The UI uses ng-zorro-antd (import only needed components/icons), Less theme bundles plus Tailwind utilities, Romanian locale/currency, `provideNzI18n(ro_RO)`, and `provideNzDateFnsAdapter()`. Configure the date adapter explicitly. Follow `.github/instructions/ng-zorro-guidelines.instructions.md` and verify uncertain APIs against its official references.

`app.config.ts` configures credentialed API requests, XSRF names, and SignalR at `/hubs/app`. Keep Web CORS, cookie auth, and Angular `withCredentials` aligned. SignalR event names/payloads are wired through `core/signalr`; add events through the existing client bridge/provider and server realtime event/group conventions.

## Guidance conflicts and known deviations

- Root Angular/ng-zorro instruction files still set `applyTo: 'src/Web/ClientApp/**'`. Root `.github/copilot-instructions.md` says to apply them to the real frontend, `src/Client`; follow that correction.
- `src/Client/.github/copilot-instructions.md` contains older claims that no frontend has been scaffolded and describes an earlier backend topology. Current `src/Client`, `Client.esproj`, package files, source, and AppHost are live. Prefer current source/configuration and root guidance over the stale nested summary.
- Root Copilot guidance says no browser E2E runner exists. Current `src/Client/package.json`, `playwright.config.ts`, and `e2e/` define Playwright scripts/specs; they require the running full stack.
- `.github/agents/*.agent.md` files are optional task personas, not architecture authorities. Generic references to xUnit/MSTest, Swagger, or defaults do not override this repository's NUnit, Mediator, Scalar, and source patterns.
- This resembles Clean Architecture/Aspire templates but intentionally differs in `Shared.Services`, endpoint-group discovery, source-generated Mediator, the separate Worker, the Result-to-ProblemDetails contract, cookie-first Identity API, and the outbox/idempotent queue path. Preserve these seams rather than reverting to template defaults.
