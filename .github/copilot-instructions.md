# Copilot Instructions — skestock

> **Read `/AGENTS.md` first.** It is the canonical quick-reference (layer rules, DI convention,
> Mediator pipeline order, endpoint pattern, scaffolding, caching, auth). This file **complements**
> it with deeper, file-by-file structure and the newer subsystems AGENTS.md doesn't cover yet
> (async **outbox → queue → worker** messaging, **Azure Blob storage / SAS uploads**, the
> **GoodsReceiptImport** async flow, the **Angular** frontend, and per-slice divergences). Where
> a topic is covered by AGENTS.md, defer to it and don't contradict it. Also honour the globbed
> rules in `.github/instructions/*.instructions.md` and the agents in `.github/agents/*`.

## Overview

`skestock` (school stock/inventory management) is a **.NET 10** solution (`global.json` pins SDK
`10.0.110`, `rollForward: latestFeature`) generated from the **Jason Taylor Clean Architecture**
template and orchestrated end-to-end with **.NET Aspire 13.5.x**. `Directory.Build.props` applies
`net10.0`, `Nullable`, `ImplicitUsings`, and **`TreatWarningsAsErrors=true`** to every project — a
warning fails the build, so keep code warning-clean.

The solution file is `skestock.slnx` (an XML `.slnx`, **not** a classic `.sln`). It contains
**nine** `src/` projects and **five** `tests/` projects (see layout). Notably it includes
`src/Client/Client.esproj` (the Angular SPA as an MSBuild "JS project") and `src/Worker` (a queue
consumer host), so a solution-level `dotnet build`/`dotnet test` also builds the JS project
(needs Node/npm).

Domain: a school inventory/stock system — `Category`, `Item`, `Location`, `SchoolClass`,
`ClassBalance`, `GoodsReceipt`, `GoodsReceiptImport`, `GoodsReceiptImportLine`, `StockBatch`,
`StockTransaction`, `FileMetadata`, `OutboxMessage`, `UserProfile`. **Seven** backend feature
slices exist under `Application/Features/`: `Categories`, `Items`, `Locations`, `SchoolClasses`,
`GoodsReceipts`, `Stock`, `StockBatches`. Two additional non-`Features` application areas exist:
`Application/Storage` (blob upload/download commands) and `Application/Queues` (queue-sender
interface). `ClassBalance` still has no dedicated slice.

## Architecture & Layer Responsibilities

Dependencies point inward only: `Domain` ← `Application` ← (`Infrastructure` & `Web`); `Web` is the
composition root. `Shared` (`skestock.Shared.Services`) holds resource/service-name constants used
by both `AppHost` and app projects — **never hardcode a service/queue/container/volume name, use
`Services.*`**. `Client` is a standalone npm/Angular project that references **no** .NET project; it
only talks to `Web` over HTTP and is in the solution purely for build/orchestration.

- **Domain** — entities, value objects, domain events, enums; plus `Domain/Queues`
  (`OutboxMessage`, `MessageEnvelope`). No references to other layers.
- **Application** — CQRS via **[Mediator](https://github.com/martinothamar/Mediator)** (source
  generator, `Mediator.Abstractions`/`Mediator.SourceGenerator` — **not** MediatR). `IRequest`/
  `IRequestHandler`, FluentValidation validators, and cross-cutting `Common/Behaviours` (pipeline
  order matters — see AGENTS.md). Talks to data only through `IApplicationDbContext`
  (`Common/Interfaces`), never an EF Core provider. New sub-areas: `Application/Storage`
  (blob commands + `IBlobStorageService`), `Application/Queues` (`IQueueSender`).
- **Infrastructure** — `ApplicationDbContext` (EF Core, SQL Server), Identity, SaveChanges
  interceptors, and the concrete adapters `AzureBlobStorageService` (`Infrastructure/Storage`) and
  `AzureQueueSender` (`Infrastructure/Queues`).
- **Web** — Minimal-API endpoints (`IEndpointGroup`), OpenAPI/Scalar, exception→ProblemDetails
  mapping, `CurrentUser` (`IUser`), and the **`OutboxPublisherService`** hosted `BackgroundService`
  (`Web/BackgroundJobs`) that drains the outbox to the queue.
- **Worker** — standalone `Microsoft.NET.Sdk.Worker` host meant to **consume** the queue.
  Currently a **stub** (`Worker.cs` just logs every second); references only `ServiceDefaults`. Wire
  real queue-consumption here, not in `Web`.
- **ServiceDefaults** — shared OpenTelemetry / health-check / service-discovery / resilience
  extensions (`AddServiceDefaults()`), consumed by `Web`, `Worker`, and `AppHost`-hosted projects.
- **AppHost** — Aspire orchestrator (see resource graph below).

## Solution / Project Layout (file-level)

```
skestock.slnx                      # XML solution — includes Client.esproj (Angular) + Worker.csproj
Directory.Build.props              # net10.0, Nullable, ImplicitUsings, TreatWarningsAsErrors=true
Directory.Packages.props           # central package management — ALL .NET versions live here
global.json                        # pins .NET SDK 10.0.110
aspire.config.json                 # points Aspire CLI at src/AppHost/AppHost.csproj
AGENTS.md                          # canonical agent quick-reference (read first)

src/
  Domain/            Entities/ Enums/ Events/ Constants/ Common/ Queues/(OutboxMessage, MessageEnvelope)
  Application/       → references Domain only
    Features/<Name>/ Categories, Items, Locations, SchoolClasses, GoodsReceipts, Stock, StockBatches
    Storage/         Commands/{RequestUpload,ConfirmUpload}, Interfaces/IBlobStorageService, DTOs, Models
    Queues/          Interfaces/IQueueSender
    Common/          Behaviours, Caching, Filtering, Keyset, Models, Errors, Interfaces
  Infrastructure/    → references Application + Domain
    Data/ Identity/ Storage/AzureBlobStorageService Queues/AzureQueueSender
  Web/               → references Application + Infrastructure + ServiceDefaults (composition root)
    Endpoints/       Categories, GoodsReceipts, Items, Locations, SchoolClasses, Stock, StockBatches,
                     Storage, Users (active); Antiforgery (commented out)
    BackgroundJobs/OutboxPublisherService.cs   Services/CurrentUser.cs   Program.cs
  Worker/            Program.cs, Worker.cs (stub queue consumer)
  ServiceDefaults/   Extensions.cs
  Shared/            Services (service/resource/queue name constants)
  Client/            Angular 22 SPA (Client.esproj, NOT Client.csproj) — see Frontend section

tests/
  Domain.UnitTests/               # project shell exists, NO tests yet
  Application.UnitTests/          # NUnit, mirrors Application/ 1:1 (7 Features folders)
  Application.FunctionalTests/    # full Aspire stack via TestAppHost (6 Features folders)
  Infrastructure.IntegrationTests/
  TestAppHost/                    # slim Aspire host: SQL Server + Redis ONLY
```
`src/Web/ClientApp/` is an empty leftover — **do not** put frontend code there; the real SPA is
`src/Client`.

## Critical Workflows (build / run / test)

```bash
dotnet build                              # builds whole solution (incl. Client.esproj → needs Node/npm)
dotnet run --project src/AppHost          # run full stack via Aspire (dashboard + containers + frontend)
dotnet test                               # all backend unit/integration/functional tests
```
- **Running via AppHost requires both Docker and Node/npm**: Docker for SQL Server, Redis, and
  Azurite (blob+queue emulator) containers; Node/npm for the Vite/Angular dev server (`AddViteApp`
  runs `npm install` + `npm run dev` automatically).
- The Aspire dashboard is forwarded on **:8080**; the Angular frontend on **:7001**; API docs at
  **`/scalar`** (root `/` redirects there — no Swagger UI).
- **No local fallback connection strings** exist for SQL/Redis/Storage/Queues in `appsettings.json`
  — everything is wired via Aspire resource references. Running `Web` alone (`dotnet run` without
  AppHost) has no working DB/cache/storage; only a `localhost:4200` CORS fallback is provided for
  quick standalone frontend iteration.
- **Functional & integration tests need Docker.** `Application.FunctionalTests` boots `TestAppHost`
  (`DistributedApplicationTestingBuilder`), waits on `Services.Database` health, and resets the DB
  per test/fixture via Respawn (`DatabaseResetter`). `TestAppHost` stands up **SQL Server + Redis
  only** (no Storage/Azurite, no Client/Worker) — functional tests never exercise blob storage, the
  queue, or the frontend.
- Frontend tests: from `src/Client` run `npm test` (`ng test` → **vitest**). Not run by solution
  `dotnet test` beyond the `Client.esproj` build.

## Conventions & Patterns

Most core conventions (central package management, per-project `GlobalUsings.cs`,
`Guard.Against.*` clauses, the Mediator **pipeline order**, `IEndpointGroup` auto-discovery,
`AddXServices(this IHostApplicationBuilder)` DI registration in each layer's **own** namespace,
`ca-usecase` scaffolding, and `HybridCache` tag-based caching) are documented in **AGENTS.md** —
follow it. Additions/specifics for this repo:

- **Central packages** (`Directory.Packages.props`) — key versions: EF Core **10.0.11**,
  Identity.EntityFrameworkCore **10.0.11**, `Mediator.*` **3.0.2**, `FluentValidation.*` **12.1.1**,
  **FluentResults 4.0.0**, `Microsoft.Extensions.Caching.Hybrid` **10.9.0**, `Scalar.AspNetCore`
  **2.17.1**, `Azure.Storage.Blobs` **12.28.0**, `Aspire.Azure.Storage.{Blobs,Queues}` **13.5.3**,
  Aspire hosting **13.5.2**, NUnit **4.6.1** + Shouldly + Moq + Respawn **7.0.0**. Add versions
  here, never inline in a `.csproj`.
- **Endpoints are Minimal APIs, not controllers.** Each is a class implementing `IEndpointGroup`
  with `static void Map(RouteGroupBuilder)`; handlers are `static` methods returning typed
  `Results<Ok<T>, ProblemHttpResult>` and injecting `ISender`. On `result.IsFailed` return
  `result.ToProblemHttpResult()`. Discovered via `app.MapEndpoints(typeof(Program).Assembly)` — just
  drop a new class in `Web/Endpoints/`. See `Web/Endpoints/Storage.cs` / `GoodsReceipts.cs`.
- **Result / error pattern** — handlers return FluentResults `Result<T>`; failures are typed
  `Error` subclasses carrying `Metadata` (`ErrorMetadataKeys.StatusCode/Title/Code/Params`) that
  `ResultProblemDetailsMapper` turns into the `ApiErrorContract` ProblemDetails the frontend
  consumes. Reference: `Application/Common/Errors/StorageErrors.cs` (`FileNotFound` → 404,
  `BlobNotFound` → 409). **Don't throw** for expected domain failures — return `Result.Fail(...)`.
- **Query/filter/pagination** — a paginated `GetAll<Feature>` query needs `CacheConstants.cs`,
  `<Feature>SortConfiguration.cs`, and `<Feature>FilterConfiguration.cs` (the `ca-usecase` template
  does **not** generate these — add by hand). Reference: `Categories` (full paginated/filterable/
  cached, keyset cursor). Reference for a deliberately **non**-paginated query that skips all of
  that: `Stock.GetClassLocationStock`.

### Per-slice divergences (check the slice before copying a pattern)
- `Items` — richest: `Create`/`Edit`/`Disable`/`Enable` + `GetAllItems` + `GetItemById`.
- `Locations` / `SchoolClasses` — `Create`/`Update` (not `Edit`) + `Get<Feature>ById` + `GetAll`.
- `GoodsReceipts` — create/read-only ledger. `CreateGoodsReceipt` fans out into one `StockBatch`
  + one `StockTransaction` per line inside one `SaveChangesAsync`. Also hosts the **async import**
  sub-flow (`CreateGoodsReceiptImport`, `EventHandlers/GoodsReceiptImportCreatedEventHandler`,
  `Messages/ProcessGoodsReceiptImportMessage`). **No `Application.FunctionalTests/Features/
  GoodsReceipts` folder yet** — unit tests only.
- `Stock` — **no** pagination boilerplate. `GetClassLocationStock` returns `List<StockItemDto>`;
  `AdjustStock` diffs a physical count against summed remaining batch quantities (draws down oldest
  expiry first on a shortfall, creates an unattributed `StockBatch` on a surplus). Full unit +
  functional coverage.
- `StockBatches` — read-only `GetAllStockBatches`, full cache/sort/filter boilerplate. `StockBatch`/
  `StockTransaction` rows are only ever created as a side effect of `GoodsReceipts`/`Stock`, never
  as their own aggregate root.

## Async messaging: Outbox → Queue → Worker

A transactional-outbox pattern moves work off the request thread. The end-to-end reference flow is
the **goods-receipt file import**:

1. `POST /api/Storage/request-upload` → `RequestUploadCommand` creates `FileMetadata` (`Pending`)
   and returns a short-lived **SAS upload URL**; the client uploads the blob directly to storage.
2. `POST /api/Storage/confirm-upload` → `ConfirmUploadCommand` verifies the blob exists, records
   size/ETag, marks `FileMetadata` `Completed`.
3. `POST /api/GoodsReceipts/imports` → `CreateGoodsReceiptImportCommand` creates a
   `GoodsReceiptImport` (`Processing`, capturing the blob path) and raises
   `GoodsReceiptImportCreatedEvent`.
4. `GoodsReceiptImportCreatedEventHandler` (a Mediator `INotificationHandler`, dispatched by
   `DispatchDomainEventsInterceptor`) **adds an `OutboxMessage`** in the **same DbContext / same
   transaction** — `Type = typeof(ProcessGoodsReceiptImportMessage).AssemblyQualifiedName`,
   JSON `Payload`, `QueueName = Services.GoodsReceiptImportQueue` (`"goods-receipt-import"`).
5. `OutboxPublisherService` (hosted `BackgroundService` in **Web**, registered via
   `AddHostedService<OutboxPublisherService>()`) polls every 5s: takes up to 50 unprocessed rows
   (`ProcessedAtUtc == null && RetryCount < 5`, oldest first), sends each as a `MessageEnvelope`
   through `IQueueSender`, stamps `ProcessedAtUtc` or increments `RetryCount`/`Error`, then
   `SaveChangesAsync`.
6. `AzureQueueSender` serialises the envelope to JSON, Base64-encodes it, and enqueues to the named
   Azure Storage Queue (creating it if missing).
7. **`Worker`** is intended to dequeue and process these messages — **not yet implemented** (stub).
   Implement consumers there. Per `GoodsReceiptImportStatus`, the intended lifecycle is
   `Processing` (worker calls an AI extraction API) → `PendingReview` (extraction done, awaiting
   user confirmation) → `Confirmed` (user confirmed, `GoodsReceipt` created) / `Failed`.

Conventions when extending this: enqueue outbox rows via `dbContext.OutboxMessages.Add(...)` inside
the same unit of work as the state change (never publish to the queue directly from a handler);
use a `Services.*` queue-name constant; keep the message a small `record` under the feature's
`Messages/` folder; store the message type as its `AssemblyQualifiedName`.

## Storage (Azure Blob, SAS-based)

`Application/Storage` defines `IBlobStorageService` (generate upload/download SAS URIs, get blob
info, delete) implemented by `Infrastructure/Storage/AzureBlobStorageService` over a
`BlobServiceClient`. Uploads/downloads use **client-direct SAS URLs** (10-min write SAS for upload,
read SAS for download) — the API never proxies file bytes. Files live in the **`app-files`**
container; `FileMetadata` (`Pending` → `Completed`, `FileStatus` enum) tracks each one. Blob CORS
rules for the frontend origin are configured in `AppHost` (`storage.SetBlobCorsRules(...)`).

## Auth

Registered in `Infrastructure/DependencyInjection.cs`:
```csharp
builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddBearerToken(IdentityConstants.BearerScheme)
    .AddCookie(IdentityConstants.ApplicationScheme);           // cookie is the DEFAULT (SPA)
builder.Services.AddIdentityCore<ApplicationUser>()
    .AddRoles<IdentityRole<Guid>>()                            // roles are Guid-keyed
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddApiEndpoints();
```
- The **cookie** `ApplicationScheme` is the default (used by the Angular SPA with credentialed CORS
  + XSRF); `BearerScheme` remains for API/non-browser clients.
- **Roles are `IdentityRole<Guid>`** and `IUser.Id` is `Guid?` (`Web/Services/CurrentUser.cs` reads
  the `NameIdentifier` claim). Only `Domain.Constants.Roles.Administrator` exists so far.
- `Web/Endpoints/Users.cs` maps `MapIdentityApi<ApplicationUser>()` plus a custom authenticated
  `logout` POST (`SignInManager.SignOutAsync()`).
- **CORS is an explicit-origin allowlist** (not `AllowAnyOrigin()`) combined with
  `AllowCredentials()` — required for cookie auth. Origins come from `Cors:AllowedOrigins`
  (populated by AppHost at run time via `Cors__AllowedOrigins__0`); a `localhost:4200` fallback
  applies only in Development when the config is empty.
- **Antiforgery is scaffolded but OFF**: `Web/Endpoints/Antiforgery.cs`, `AddAntiforgery(...)` in
  `Web/DependencyInjection.cs`, and `app.UseAntiforgeryValidation()` in `Program.cs` are all
  commented out. The frontend already configures matching XSRF cookie/header names in anticipation
  — don't assume server-side enforcement is live.

## AppHost resource graph (`src/AppHost/Program.cs`)

`AddDockerComposeEnvironment("env")` (dashboard on :8080) hosts, in dependency order:
- `databaseServer` (`Services.DatabaseServer`) — SQL Server, secret `sql-password`, TCP 1433, data
  volume, database `Services.Database` (`skestockDb`).
- `cache` (`Services.Cache`) — Redis, secret `redis-password`, TCP 6379, data volume.
- `storage` (`Services.Storage`) — **`AddAzureStorage(...).RunAsEmulator(...)`** (Azurite),
  persistent, blob/queue/table ports 10000/10001/10002. Declares blob container **`app-files`**,
  blob service `Services.BlobService`, queue service `Services.Queues`, and blob CORS rules for the
  frontend origin. **Now actively consumed** (Blob + Queue) — no longer reserved scaffolding.
- `web` (`Services.WebApi`) — the `Web` project; references/waits on database, cache, blob service,
  queue; external HTTP endpoints; `/scalar` dashboard shortcut.
- `worker` (`Services.Worker`) — the `Worker` project; references/waits on database, cache, queue.
- `webfrontend` (`Services.WebFrontend`) — **`AddViteApp(..., "../Client", "dev").WithNpm()`**; waits
  on `web`, gets `ASPNETCORE_URLS` = Web's resolved endpoint, HTTP endpoint on :7001.
- CORS wiring: `web.WithEnvironment("Cors__AllowedOrigins__0", webfrontend.GetEndpoint("http"))`.

Every infra resource is attached with `.WithComputeEnvironment(compose)` and
`.PublishAsDockerComposeService(...)` so `aspire publish` / docker-compose generation works too.

## Testing strategy

- **Domain.UnitTests** — empty (no domain-logic coverage yet).
- **Application.UnitTests** — NUnit + Shouldly + Moq; mirrors `Application/` 1:1 (7 Features
  folders). `Features/GoodsReceipts/.../GoodsReceiptTestDbContext.cs` is a dedicated in-memory
  `IApplicationDbContext` fixture — reuse it before hand-rolling another for a multi-entity handler
  (e.g. `Stock.AdjustStock`).
- **Application.FunctionalTests** — real stack via `TestAppHost` (SQL Server + Redis only), DB reset
  per test via `DatabaseResetter`. Covers `Categories`, `Items`, `Locations`, `SchoolClasses`,
  `Stock`, `StockBatches` (not `GoodsReceipts`). Storage/queue/worker/frontend are **not**
  exercised here.
- **Infrastructure.IntegrationTests** — exercises `ApplicationDbContext`/EF Core directly.
- Don't assume a clean DB outside `DatabaseResetter`. Docker required for functional/integration.

## Frontend (`src/Client`) — Angular 22 + ng-zorro-antd + NgRx SignalStore

A fully implemented SPA (standalone components, no `NgModule`s). Stack: Angular `^22`,
`ng-zorro-antd ^22`, `@ngrx/signals` + `@ngrx/operators ^22` (**SignalStore**, not classic
store/effects/reducers), Tailwind CSS 4 + LESS, `vitest`. npm scripts: `dev` (`ng serve`, used by
Aspire), `build`, `watch`, `test`.

Key conventions to preserve (details in the previous docs and in `.github/skills/{angular-developer,
ngrx-signalstore}` + `.github/instructions/{angular-guidelines,ng-zorro-guidelines}.instructions.md`,
whose `src/Web/ClientApp/**` glob should in practice be read as `src/Client/**`):
- **`features/<name>` vs `shared/<name>`**: `shared/<name>` owns the HTTP client, reusable
  SignalStore/`signalStoreFeature` building blocks, and reusable UI (modals); `features/<name>` owns
  the routed page + its header/filter/table sub-components + its own lazy `*.routes.ts`, composing
  the `shared` blocks into a page store.
- **Stores**: `signalStore(...)` for root stores, `signalStoreFeature(...)` for reusable composables
  (`withItemCollection()`, `withLoadingFeature()`, `withProblemDetailsFeature()`). Async work uses
  `rxMethod<T>(pipe(tap, switchMap))` + `mapResponse({ next, error })` routing into `patchState` —
  **not** manual `.subscribe()` or classic effects.
- **Errors**: `core/models/errors.ts` mirrors the backend `ApiErrorContract`
  (`ProblemDetails`/`ValidationProblemDetails`/`BackendErrorPayload`/`BackendErrorItem` +
  `isValidationProblem()`). Surface failures via `withProblemDetailsFeature(prefix)`
  (`handle<Prefix>Error` in the `mapResponse({ error })` branch, `clear<Prefix>Errors` before a new
  request).
- **Pagination**: `core/models/pagination.ts` (`PaginatedResponseData` with `hasNextPage`/
  `nextCursor`, `BasePaginationFilter`) lines up field-for-field with `Application/Common/Models`.
- **Path aliases**: import across boundaries via `@ske/...` aliases (`tsconfig.json` → `paths`),
  never relative `../../`. Add an alias when you add a `features/<name>` or `shared/<name>` folder.
- **Routing**: `app.routes.ts` loads two layout groups (`simpleRoutes`/`fullRoutes` from
  `@ske/layouts`); feature routes are lazy (`loadComponent`/`loadChildren`) inside `full.routes.ts`,
  guarded by `authGuard`; login is under `simple` guarded by `guestGuard`.
- **i18n / XSRF**: Romanian locale by default (`provideNzI18n(ro_RO)`, `LOCALE_ID: 'ro'`,
  `DEFAULT_CURRENCY_CODE: 'RON'`); `withXsrfConfiguration({ cookieName: 'XSRF-TOKEN', headerName:
  'X-XSRF-TOKEN' })` matches the (currently-off) server antiforgery wiring.
- `.github/instructions/llms-full.txt` is a large ng-zorro-antd API dump — consult it for component
  props instead of guessing; it is **not** a set of project conventions.

## Deviations from a vanilla Jason-Taylor template (call-outs)

- **Mediator source generator** replaces MediatR; **FluentResults `Result<T>`** replaces thrown
  exceptions for expected failures; **Scalar** replaces Swagger UI.
- **.NET Aspire** orchestration + a **`Shared`** constants project; SQL/Redis/Storage/Queues are
  Aspire resources with **no fallback connection strings**.
- **`IEndpointGroup`** auto-discovery instead of manual endpoint mapping.
- **Roles are `Guid`-keyed** (`IdentityRole<Guid>`), cookie scheme is the default, CORS is an
  explicit allowlist with `AllowCredentials()`.
- **Transactional outbox → Azure Queue → Worker** async messaging and **SAS-based blob storage**
  are bespoke additions on top of the template.
- A real **Angular 22 / ng-zorro / NgRx SignalStore** frontend lives in `src/Client` (the old
  `src/Web/ClientApp` is dead).

## Custom agents & skills available
- `.github/agents/CSharpExpert.agent.md` — general .NET/C# assistance.
- `.github/agents/csharp-dotnet-janitor.agent.md` — cleanup/modernization/tech-debt on C#/.NET.
- `.github/skills/{angular-developer, ngrx-signalstore}` — frontend authoring guidance (directly
  applicable to `src/Client`).
- `.github/skills/aspire` — Aspire CLI/AppHost/dashboard guidance (applies to `src/AppHost`).
- `.github/skills/acquire-codebase-knowledge` — repo-mapping/onboarding workflow.
