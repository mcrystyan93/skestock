# skestock Copilot Instructions

Read `AGENTS.md` before changing code. It is the canonical quick reference for dependency
direction, DI ownership, Mediator behavior order, endpoint discovery, caching, and scaffolding.
This file complements it with the current project map, operational details, and file-level
conventions. If this file and `AGENTS.md` disagree, follow `AGENTS.md` and verify the source.

## Instruction files and current repository reality

- Root `.github/instructions/angular-guidelines.instructions.md` and
  `ng-zorro-guidelines.instructions.md` still have `applyTo: 'src/Web/ClientApp/**'`, but the
  real frontend is `src/Client`. Apply those rules to `src/Client`.
- `src/Client/.github/copilot-instructions.md` contains older “frontend planned/not scaffolded”
  wording. The current solution **does contain** `src/Client`, `Client.esproj`, `package.json`,
  Angular source, and an Aspire `AddViteApp` resource. Trust the current source/configuration.
- `.github/agents/CSharpExpert.agent.md` and `csharp-dotnet-janitor.agent.md` are available for
  specialized C# work. `.github/skills/angular-developer`, `ngrx-signalstore`, and `aspire`
  contain deeper task-specific guidance.
- `CLAUDE.md` requires running `graphify query "<question>"` before broad codebase searches and
  `graphify update .` after code changes. Exclude `graphify-out/`, `bin/`, `obj/`, `.angular/`,
  and `dist/` from source investigations.

## Overview

`skestock` is a school inventory/stock system based on Jason Taylor's Clean Architecture
template `10.8.0`. It targets .NET 10 (`global.json` pins SDK `10.0.110`) and is orchestrated
with .NET Aspire `13.5.2`.

The backend includes SQL Server, Redis, Azure Blob/Queue clients with Azurite for local
development, ASP.NET Core Identity, SignalR, OpenTelemetry, OpenAI document extraction, a
transactional outbox, Azure Storage Queue workers, and a daily Worker schedule. The frontend is
an Angular 22 SPA under `src/Client`, served by Aspire/Vite in development and copied into the
Web image's `wwwroot` during the production Docker build.

The solution is `skestock.slnx`, not a classic `.sln`. `Directory.Build.props` applies
`net10.0`, nullable reference types, implicit usings, and `TreatWarningsAsErrors=true`.
All NuGet versions belong in `Directory.Packages.props`.

## Architecture & Layer Responsibilities

Dependencies point inward:

```text
Domain <- Application <- Infrastructure
Domain <- Application <- Web
Application + Infrastructure + ServiceDefaults <- Worker
Shared <- AppHost/Web/Worker/Application/Infrastructure/Client orchestration
Client is an independent npm/Angular project
```

| Project | Owns | Must not own |
|---|---|---|
| `src/Domain` | Entities, enums, value objects, domain events, audit bases, queue contracts | Project references, EF provider/configuration, HTTP, external adapters |
| `src/Application` | Feature-slice CQRS, validators, Mediator behaviors, typed errors/results, filtering/keyset/caching, storage/queue/document interfaces, `IApplicationDbContext` | Concrete EF provider, Identity, Azure SDK adapters, endpoint routing |
| `src/Infrastructure` | `ApplicationDbContext`, EF configurations/migrations/interceptors, Identity, Redis/HybridCache, distributed lock, Blob/Queue adapters, SignalR, OpenAI extraction | HTTP endpoint groups and business use cases |
| `src/Web` | Composition root, endpoint groups, OpenAPI/Scalar, auth/CORS, error mapping, static files, outbox publisher | Direct EF queries and feature business logic |
| `src/Worker` | Queue polling/acknowledgement/poison handling, scoped Mediator dispatch, daily scheduled service | HTTP endpoints or direct feature logic outside Mediator |
| `src/AppHost` | Aspire resource graph and publish/run wiring | Application/business logic |
| `src/ServiceDefaults` | Health checks, service discovery, HTTP resilience, OpenTelemetry | Feature-specific behavior |
| `src/Shared` | `skestock.Shared.Services` resource/config names plus small cross-project helpers/converters | Business logic or infrastructure |
| `src/Client` | Angular routes, pages, shared HTTP services, SignalStore state, SignalR bridge, UI | .NET project references |

### Domain model

Domain entities include `Category`, `CategoryIcon`, `Item`, `Location`, `SchoolClass`,
`ClassBalance`, `ClassItemStockVisibility`, `GoodsReceipt`, `GoodsReceiptImport`,
`GoodsReceiptImportLine`, `CategoryImportBatch`/`File`, `ItemImportBatch`/`File`,
`ImportBatchHistory`, `OrderList`/`Line`, `StockBatch`, `StockTransaction`, `FileMetadata`,
`UserProfile`, and `ScheduledJobRun`. Queue contracts are under `src/Domain/Queues`:
`MessageEnvelope`, `OutboxMessage`, and `ProcessedMessage`.

`BaseEntity` uses Guid v7 IDs. Persisted instants use UTC `DateTimeOffset`; business calendar
values use `DateOnly`. See `docs/adr/0001-utc-datetimeoffset-for-persisted-instants.md`.

## Solution / Project Layout

### Backend source

```text
src/
  AppHost/          Aspire graph: SQL Server, Redis, Azurite, Web, Worker, Vite frontend
  Domain/           pure domain types and queue contracts
  Application/      CQRS/features, common behaviors, storage, queues, documents
  Infrastructure/   EF/Identity/Redis/Azure/SignalR/OpenAI implementations
  ServiceDefaults/  health, discovery, resilience, OpenTelemetry
  Shared/           resource names and cross-project helpers
  Web/              HTTP host, endpoints, auth, errors, outbox publisher
  Worker/           queue consumers and daily scheduled service
  Client/           Angular 22 application
```

Application feature folders currently include:

```text
Features/
  Categories/
  GoodsReceipts/
  Items/
  Locations/
  OrderLists/
  ScheduledJobs/
  SchoolClasses/
  Statistics/
  Stock/
  StockBatches/
```

`Storage/` and `Queues/` are separate Application areas. Paginated slices normally include
`CacheConstants.cs`, `<Feature>FilterConfiguration.cs`, and
`<Feature>SortConfiguration.cs`. Check the closest slice before copying because command verbs
vary: `Items` uses `Edit/Disable/Enable`, Locations/SchoolClasses/Categories use `Update`,
OrderLists use lifecycle verbs, and imports use `Create/Confirm/Process`.

`src/Web/Endpoints` currently contains endpoint groups for Categories, CategoryImportBatches,
GoodsReceipts, Items, ItemImportBatches, Locations, OrderLists, SchoolClasses, Statistics,
Stock, StockBatches, Storage, and Users. `Antiforgery.cs` exists but is entirely commented out.

`src/Infrastructure/Data/Migrations` contains generated EF migrations and
`ApplicationDbContextModelSnapshot.cs`. Do not hand-edit generated migration designer/snapshot
files; change the model/configuration and regenerate with `dotnet ef`.

### Test projects

- `tests/Application.UnitTests`: NUnit/Shouldly/Moq unit tests mirroring Application folders.
- `tests/Application.FunctionalTests`: HTTP-level tests using `WebApiFactory`, `TestApp`, `TestBase`,
  and Respawn `DatabaseResetter`.
- `tests/Infrastructure.IntegrationTests`: real EF/Infrastructure tests; its setup starts
  `TestAppHost`, waits for resources, and migrates SQL Server.
- `tests/Worker.UnitTests`: queue processor and daily scheduling tests.
- `tests/Domain.UnitTests`: project exists but has no test files currently.
- `tests/TestAppHost`: test Aspire host. **Current source provisions SQL Server, Redis, Azurite
  Blob/Queue resources, and Worker**, even though older summaries may describe only SQL/Redis.

## Critical Workflows (build/run/test)

### Restore/build

```bash
dotnet restore skestock.slnx
dotnet build
```

Warnings are errors. Do not “fix” build failures by suppressing warnings globally.

### Full local run

```bash
dotnet run --project src/AppHost
```

This is the supported full-stack path and requires Docker or a compatible Podman runtime plus
Node/npm. AppHost starts:

- SQL Server database resource `Services.DatabaseServer` with database `Services.Database`
  (`skestockDb`).
- Redis resource `Services.Cache`.
- Azurite/Azure Storage resource `Services.Storage`, Blob and Queue clients, and `app-files`
  blob container. Run-mode emulator ports are Blob `10000`, Queue `10001`, Table `10002`.
- Web API resource `Services.WebApi`.
- Worker resource `Services.Worker`.
- Vite frontend resource `Services.WebFrontend` on port `7001`.

The Aspire dashboard is configured to host-forward to port `18080` in `src/AppHost/Program.cs`.
The Web resource advertises Scalar at `/scalar`. Do not assume a fixed Web port; use Aspire
resource URLs. Production publish mode uses explicit Web port `7001` and an external storage
public endpoint.

Running `dotnet run --project src/Web` directly is not supported: all required SQL, Redis,
Blob/Queue, and OpenAI configuration must be supplied externally. Use `src/AppHost` for local
development.

### Client

```bash
cd src/Client
npm ci
npm run dev
npm run build
npm run build:prod
npm test
```

`npm ci` is preferred because `package-lock.json` is committed. The production Web Dockerfile
builds Angular first with Node 22 and copies `dist/ske/browser` into Web `wwwroot`.

### .NET tests

```bash
dotnet test
dotnet test tests/Application.UnitTests
dotnet test tests/Worker.UnitTests
dotnet test tests/Infrastructure.IntegrationTests
dotnet test tests/Application.FunctionalTests
```

Functional/integration tests need container resources. `FunctionalTestSetup` uses a 90-second
cancellation window, starts `TestAppHost` with `DistributedApplicationTestingBuilder`, waits for
`Services.Database`, `Services.Cache`, and `Services.Queues`, then creates `WebApiFactory`.
`TestBase` resets database state through Respawn; do not assume a clean database without it.

For Podman:

```bash
./run-functional-tests.sh [extra dotnet test arguments]
```

The script starts the user Podman socket, validates its Unix socket, exports
`DOCKER_HOST=unix://...` and `DOTNET_ASPIRE_CONTAINER_RUNTIME=podman`, and runs functional tests.
`functional-tests.runsettings` supplies equivalent environment variables but assumes UID 1000;
prefer the script when possible.

### EF migrations

Use the existing EF CLI and never hand-edit generated files:

```bash
dotnet ef migrations add <MigrationName> \
  --project src/Infrastructure \
  --startup-project src/Web \
  --context ApplicationDbContext \
  --output-dir Data/Migrations
dotnet ef migrations has-pending-model-changes \
  --project src/Infrastructure \
  --startup-project src/Web \
  --context ApplicationDbContext
```

### Production deployment

`.github/workflows/deploy-production.yml` is `workflow_dispatch`-only. Its validation job
restores/builds .NET, runs Application unit tests, installs Node dependencies, runs client tests,
and builds the production client. It then builds/pushes Web and Worker images to GHCR and deploys
to Fedora rootless Podman/systemd Quadlet through Cloudflare Access SSH. Deployment variables and
secrets are documented in `deploy/README.md` and `deploy/production.env.example`.

## Conventions & Patterns

### Packages, formatting, and global usings

- Central package versions only in `Directory.Packages.props`.
- Preserve project `GlobalUsings.cs` files and file-scoped namespace style.
- Use `Guard.Against.*` for argument/configuration guards.
- Keep the root `.editorconfig` clean; `TreatWarningsAsErrors` makes compiler/analyzer warnings
  part of the build contract.
- Do not add generated outputs, secrets, `bin/`, `obj/`, `.angular/`, or `dist/` to source edits.

### DI registration and host composition

Layer registration belongs in:

- `skestock.Application.DependencyInjection.AddApplicationServices`
- `skestock.Infrastructure.DependencyInjection.AddInfrastructureServices`
- `skestock.Infrastructure.DependencyInjection.AddWebAuthenticationServices`
- `skestock.Web.DependencyInjection.AddWebServices`
- `skestock.ServiceDefaults.Extensions.AddServiceDefaults`

`src/Web/Program.cs` composes in this order:

```text
AddServiceDefaults
-> AddKeyVaultIfConfigured
-> AddApplicationServices
-> AddInfrastructureServices
-> AddWebAuthenticationServices
-> AddWebServices
```

Worker composes ServiceDefaults, Application, Infrastructure, scoped `AmbientUser`/`IUser`,
queue processor, three queue hosted services, and `DailyStatisticsService`. Keep endpoint-bound
authentication wiring out of Worker-safe Infrastructure registration.

### Mediator pipeline

`src/Application/DependencyInjection.cs` registers scoped Mediator in this exact order:

1. `LoggingBehaviour`
2. `UnhandledExceptionBehaviour`
3. `AuthorizationBehaviour`
4. `ValidationBehaviour`
5. `PerformanceBehaviour`
6. `CachingBehavior`
7. `CacheInvalidationBehavior`

Order is behavior, not style. New behaviors must be inserted deliberately. Queries that should
cache implement `ICacheableQuery`; mutations that invalidate cache implement `ICacheInvalidation`.

### Feature slices, validation, and errors

Use `Features/<Feature>/{Commands|Queries}/<UseCase>/` with command/query, handler, and validator.
Register validators through assembly scanning; `ValidationBehaviour` aggregates failures and throws
the repository's `ValidationException`.

For expected business failures, return `Result`/`Result<T>` with typed `Error` subclasses and
metadata keys from `Application/Common/Errors/ErrorMetadataKeys.cs`. Web maps failed results with
`ToProblemHttpResult()`, producing RFC ProblemDetails plus the frontend-facing `error` contract.
Use thrown exceptions only for validation, authorization, and exceptional cases already handled by
`ProblemDetailsExceptionHandler`.

### Endpoints

Endpoints are not controllers. Add a public class under `src/Web/Endpoints` implementing
`IEndpointGroup` with:

```csharp
public static void Map(RouteGroupBuilder groupBuilder) { ... }
```

`MapEndpoints(typeof(Program).Assembly)` discovers groups by reflection and defaults to
`/api/{ClassName}`. Use the custom `MapGet/MapPost/MapPut/MapPatch/MapDelete` overloads from
`EndpointRouteBuilderExtensions`; handlers must be named static methods, not lambdas, because the
method name becomes the OpenAPI operation ID. Use typed `Results<...>` and pass a
`CancellationToken`.

Complex list requests use `POST .../get-all` with a body containing pagination/filter/sort data.
Map failed results through `result.ToProblemHttpResult()` (or the existing typed result helpers).
Add `[EndpointSummary]` and `[EndpointDescription]`.

### Filtering, keyset pagination, and caching

- Do not accept arbitrary property names for filters or sort. Add explicit
  `IFilterConfiguration<TEntity>` and `IKeysetSortConfiguration<TEntity>` allowlists.
- Use `FilterQueryBuilder`, `DynamicSortBuilder`, `CursorCodec`, `KeysetPredicateBuilder`, and
  `OrderByBuilder`.
- Fetch `pageSize + 1`, remove the extra item, and return `HasNextPage`/`NextCursor`.
- Use `BasePaginationFilter` and `PaginatedResponse<T>`.
- Implement `ICacheableQuery` with normalized keys, feature tags, and randomized sliding expiration.
- Commands implement `ICacheInvalidation` with collection/entity tags as appropriate. HybridCache
  tag invalidation is logical/lazy; keep a sensible TTL.

### Persistence, dates, and domain events

Application handlers use `IApplicationDbContext` and `SaveChangesAsync`; EF configuration lives
in `Infrastructure/Data/Configurations`. `ApplicationDbContext` applies configurations from its
assembly and generates Guid v7 IDs for `BaseEntity`.

`AuditableEntityInterceptor` stamps `BaseAuditableEntity` fields using `IUser` and
`TimeProvider`. `DispatchDomainEventsInterceptor` dispatches domain events after successful save.
Persist instants as UTC `DateTimeOffset`; use `DateOnly` for calendar-only business values.

### Async messaging and scheduling

Goods receipt/category/item imports use a transactional outbox:

1. Handler/domain event writes `OutboxMessage` in the same DB unit of work.
2. `OutboxPublisherService` claims bounded batches, sends `MessageEnvelope` to Azure Queue, and
   records retry/error state.
3. Worker queue services receive messages with visibility timeouts, restore the original Mediator
   request, set `AmbientUser` from the envelope, and use `ProcessedMessages` for idempotency.
4. Permanent or exhausted failures go to `<queue>-poison`.

Delivery is at-least-once. New consumers must be idempotent and cancellation-aware.

`DailyStatisticsService` is a separate Worker `BackgroundService`. It defaults to 21:00
`Europe/Bucharest`, catches up one missed occurrence at startup, persists `ScheduledJobRun` state
through Mediator, takes `Services.DailyStatisticsLockKey` through Redis, and retries failures
after 1/5/15 minutes. Its current `ExecuteJobAsync` body only logs; future scheduled commands
belong there, not in `Worker.cs`.

## Scaffolding/Codegen tools

From `src/Application`:

```bash
dotnet new ca-usecase --name CreateTodoList \
  --feature-name TodoLists --usecase-type command --return-type int
dotnet new ca-usecase -n GetTodos -fn TodoLists -ut query -rt TodosVm
```

If unavailable:

```bash
dotnet new install Clean.Architecture.Solution.Template::10.8.0
```

Complete generated slices manually with feature-specific filter/sort/cache files, endpoint
mapping, typed errors, and mirrored tests. Do not assume the template creates repository-specific
pagination or cache boilerplate.

## Auth

Infrastructure registers `ApplicationUser` with `IdentityRole<Guid>` and EF stores. Web adds the
Identity application cookie as the default scheme plus `IdentityConstants.BearerScheme`, then
maps Identity API endpoints in `Web/Endpoints/Users.cs`. `CurrentUser` reads the
`ClaimTypes.NameIdentifier` Guid and roles.

Endpoint groups require authorization by default through `IEndpointGroup.RequiresAuthorization`.
`Users` explicitly disables group authorization so login/register/manage endpoints can work; its
logout endpoint calls `.RequireAuthorization()`. The Application `AuthorizationBehaviour` supports
bare authentication, role-based, and policy-based `[Authorize]` attributes, but inspect current
requests before assuming role restrictions exist.

CORS must use explicit origins with `.AllowCredentials()`; never replace the allowlist with
`AllowAnyOrigin()`. Antiforgery configuration and middleware are currently commented out, so
do not claim CSRF protection is active.

## Testing strategy

- Add unit tests under the mirrored Application/Worker path for validators, handlers, behaviors,
  pagination/filter/keyset logic, caching, storage/queue classification, and scheduled services.
- Use NUnit `[TestFixture]`/`[Test]`, Shouldly, and Moq; follow existing fixture names.
- Use real SQL Server/Redis/Azurite/Aspire only for functional/integration boundaries.
- Functional setup uses `TestAppHost`, `WebApiFactory`, and Respawn. Use `TestApp.ResetState()`
  through `TestBase`; do not manually assume a blank database.
- Worker unit tests can use SQLite/fakes and `FakeTimeProvider` (`Microsoft.Extensions.TimeProvider.Testing`).
- Domain tests are currently absent; add them when domain behavior is introduced.
- Client tests use Vitest through `npm test`; there is no configured browser E2E runner.

## Frontend

The actual client is `src/Client`, an Angular 22 standalone-style application:

```text
src/Client/src/app/
  core/       auth, guards/interceptors, layouts, models, routes, theme, SignalR
  features/   routed pages and page-level stores (categories, items, school classes, analytics, login, home)
  shared/     reusable HTTP services, collection/detail stores, UI, loading/error features
```

Use the aliases in `src/Client/tsconfig.json` (`@ske/models`, `@ske/auth`, `@ske/features/...`,
`@ske/shared/...`) instead of deep relative imports.

Angular rules:

- Standalone components are the default; do not set `standalone: true`.
- Use `inject()`, `@Service()`, signals, `computed`, `input()`/`output()`, and native
  `@if`/`@for`/`@switch`.
- Do not use `ngClass`, `ngStyle`, `@HostBinding`, or `@HostListener`; use class/style bindings
  and the component/directive `host` object.
- Prefer Signal Forms for new forms; use Reactive Forms for existing complex forms.
- Use `patchState()` for every SignalStore state update.
- Use `rxMethod()` plus RxJS operators for asynchronous store work and `mapResponse({ next, error })`
  for success/error branches.
- Use `withEntities()` for true entity collections; do not introduce classic NgRx
  actions/reducers/effects for new code.
- Reusable loading/error state belongs in `withLoadingFeature` and
  `withProblemDetailsFeature`; clear errors before requests.
- Stores use named exports only.
- Configure accessible WCAG AA markup, keyboard/focus behavior, and ARIA.

The client uses `provideNzI18n(ro_RO)`, `provideNzDateFnsAdapter()`, Romanian locale/currency
(`ro`, `RON`), credentialed `/api` requests, and XSRF names `XSRF-TOKEN`/`X-XSRF-TOKEN` in
`app.config.ts`. `authInterceptor` adds `withCredentials` to `/api` calls. `SignalRBridge`
connects to `/hubs/app`, handles reconnects, de-duplicates event envelopes, and dispatches
NgRx SignalStore events.

For ng-zorro, import only required components/icons, use typed overlay options, prefer global
providers (`provideNzConfig`, `provideNzDateFnsAdapter`), and consult
`.github/instructions/llms-full.txt`/official ng-zorro docs instead of guessing APIs.

## Template deviations and non-obvious gotchas

- Mediator source generation replaces MediatR.
- Scalar replaces Swagger UI.
- Aspire owns resource wiring; `AppHost` is not the HTTP app.
- Shared resource-name constants must be used instead of duplicated strings.
- Endpoint groups are reflection-discovered rather than manually registered.
- Web uses cookie auth by default but retains bearer endpoints.
- Outbox/queue processing is custom and at-least-once.
- `src/Client` is real and current even though some older nested instruction text says it is planned.
- Current `TestAppHost` provisions Worker/Azurite in addition to SQL Server/Redis; verify source if
  older documentation claims otherwise.
- Generated EF migrations, Angular `dist/.angular`, `bin/obj`, and graphify outputs are not
  hand-edited source.
