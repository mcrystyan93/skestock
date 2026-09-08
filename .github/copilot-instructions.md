# skestock Copilot Instructions

Read `AGENTS.md` first. It is the canonical quick reference for layer boundaries, DI, Mediator
ordering, endpoint discovery, caching, and scaffolding. This file supplements it with the current
project layout and the storage, queue, worker, realtime, and Angular conventions. Also follow
`.github/agents/*`, `.github/skills/*`, and `.github/instructions/*`. The Angular instruction globs
still mention `src/Web/ClientApp`; apply their rules to the real client at `src/Client`.

## Overview

`skestock` is a school inventory system based on Jason Taylor's Clean Architecture template 10.8.0.
It targets .NET 10 (`global.json`: SDK `10.0.110`, `rollForward: latestFeature`) and is orchestrated
with .NET Aspire 13.5.x. `Directory.Build.props` applies `net10.0`, nullable reference types,
implicit usings, and `TreatWarningsAsErrors=true` to the solution. The solution is the XML
`skestock.slnx`, not a classic `.sln`.

The current stack includes SQL Server, Redis, Azure Storage/Azurite blobs and queues, ASP.NET Core
Identity, SignalR, OpenTelemetry, optional document extraction providers (Nutrient, Gemini, OpenAI),
and an Angular 22 SPA. The backend uses Mediator source generation, not MediatR.

## Architecture & Layer Responsibilities

Dependencies point inward: `Domain <- Application <- Infrastructure` and
`Domain <- Application <- Web`; `Web` is the composition root. `Shared` contains only
cross-project service/resource constants. `Client` is an independent npm/Angular project and
references no .NET project.

- `src/Domain`: entities, enums, value objects, domain events, audit bases, and queue contracts
  (`MessageEnvelope`, `OutboxMessage`, `ProcessedMessage`). It has no project references.
- `src/Application`: feature-slice CQRS handlers, FluentValidation validators, Mediator behaviours,
  `Result`/typed error contracts, filtering, keyset pagination, caching abstractions, storage
  commands, queue interfaces, document-extraction interfaces, and `IApplicationDbContext`.
  It may use EF Core abstractions through the application context interface, but must not depend on
  an EF provider or concrete infrastructure.
- `src/Infrastructure`: `ApplicationDbContext`, EF configurations and save-change interceptors,
  Identity (`ApplicationUser`, `IdentityService`), Redis/FusionCache-backed caching, Azure Blob and
  Queue adapters, SignalR notifier, and document extraction implementations.
- `src/Web`: ASP.NET Core host, minimal API endpoint groups, OpenAPI/Scalar, error mapping,
  `CurrentUser`, SignalR hub, CORS/authentication wiring, and `OutboxPublisherService`.
- `src/Worker`: non-HTTP worker. `Program.cs` composes Application, Infrastructure, ServiceDefaults,
  and an ambient per-message user; `Queues/GoodsReceiptImportQueueProcessingService.cs` consumes
  Azure Storage Queue messages. `Worker.cs` is a leftover sample loop; do not add new processing
  there.
- `src/ServiceDefaults`: service discovery, standard HTTP resilience, health checks, and
  OpenTelemetry (`AddServiceDefaults`).
- `src/Shared`: `skestock.Shared.Services`; use these constants for every Aspire resource, queue,
  database, cache, volume, and configuration section name. Do not duplicate string literals.
- `src/AppHost`: Aspire resource graph only; it is not the HTTP application.

The domain model includes `Category`, `Item`, `Location`, `SchoolClass`, `ClassBalance`,
`GoodsReceipt`, `GoodsReceiptImport`, `GoodsReceiptImportLine`, `StockBatch`, `StockTransaction`,
`FileMetadata`, `OutboxMessage`, `ProcessedMessage`, and `UserProfile`. Feature slices currently
live under `src/Application/Features/{Categories,Items,Locations,SchoolClasses,GoodsReceipts,Stock,StockBatches}`.
Storage and queues are separate application areas.

## Solution / Project Layout

```
src/
  AppHost/            Aspire graph: SQL Server, Redis, Azurite, Web, Worker, Vite frontend
  Application/        CQRS/features, behaviours, errors, caching, filtering, keyset, storage, queues
  Domain/             Entities, enums, events, value objects, message contracts
  Infrastructure/     EF Core, Identity, Redis, Blob/Queue, SignalR, extraction providers
  ServiceDefaults/    Aspire defaults: OTEL, health, discovery, resilience
  Shared/             Services.cs constants
  Web/                Minimal API, auth, error handling, SignalR, outbox publisher
  Worker/             Queue consumer host
  Client/             Angular 22 `Client.esproj`; real frontend (not `src/Web/ClientApp`)

tests/
  Application.UnitTests/          NUnit unit tests, Moq/Shouldly; mirrors Application
  Application.FunctionalTests/    HTTP tests through WebApiFactory and TestAppHost
  Domain.UnitTests/               NUnit project currently without test files
  Infrastructure.IntegrationTests/EF/infrastructure test project
  TestAppHost/                    slim Aspire host exposing SQL Server and Redis
```

Feature use cases normally follow
`Features/<Feature>/Commands|Queries/<UseCase>/<UseCase>Command|Query.cs`,
`Handler.cs`, and `Validator.cs`. Paginated list features also have `CacheConstants.cs`,
`<Feature>FilterConfiguration.cs`, and `<Feature>SortConfiguration.cs`. Check the slice before
copying it: `Items` has Create/Edit/Disable/Enable; `Locations` and `SchoolClasses` use Update;
`GoodsReceipts` includes the receipt/import flow; `Stock` has non-paginated stock queries and
adjustment logic; `StockBatches` is read-only and paginated.

## Critical Workflows (build / run / test)

```bash
dotnet build
dotnet run --project src/AppHost
dotnet test
dotnet test tests/Application.UnitTests
dotnet test tests/Application.FunctionalTests
dotnet test tests/Infrastructure.IntegrationTests
./run-functional-tests.sh
dotnet test --settings functional-tests.runsettings
cd src/Client && npm install && npm run dev
cd src/Client && npm run build
cd src/Client && npm test
```

`dotnet run --project src/AppHost` is the supported full-stack path. It requires Docker or a
compatible container runtime and Node/npm. AppHost starts SQL Server, Redis, persistent Azurite
(blob/queue/table ports 10000/10001/10002), Web, Worker, and the Vite frontend. The Aspire
dashboard is forwarded to port 8080, the frontend to port 7001, and Web exposes Scalar at
`/scalar` (`/` redirects there).

Functional tests start `TestAppHost` through `DistributedApplicationTestingBuilder`, wait up to
90 seconds for `Services.Database` and `Services.Cache`, then use `WebApiFactory`. They reset
database state with `DatabaseResetter`/Respawn; never assume a clean database outside that helper.
`TestAppHost` intentionally provides only SQL Server and Redis, so functional tests do not exercise
Azurite, queues, Worker, or browser UI. `run-functional-tests.sh` configures Podman socket support;
the `.runsettings` file provides the same environment variables.

## Conventions & Patterns

### Packages, formatting, and usings

All NuGet versions belong in `Directory.Packages.props`; never add inline versions to project files.
Important versions are EF Core/ASP.NET Core/Identity 10.0.11, Mediator 3.0.2, FluentValidation
12.1.1, FluentResults 4.0.0, Aspire hosting 13.5.2, Azure Storage Aspire integrations 13.5.3,
Scalar 2.17.1, HybridCache 10.9.0, OpenTelemetry 1.18.0, NUnit 4.6.1, Shouldly 4.3.0, Moq
4.20.72, and Respawn 7.0.0. Keep the root `.editorconfig` clean; warnings fail the build.

Each project owns a `GlobalUsings.cs`. Preserve existing imports and file-scoped namespaces.
Use `Guard.Against.*` for argument/configuration guards, follow least exposure for new members,
and do not edit generated files or add broad catch-and-ignore error handling.

### DI registration

Layer registrations are extension methods in the layer's own namespace:
`skestock.Application.DependencyInjection.AddApplicationServices`,
`skestock.Infrastructure.DependencyInjection.AddInfrastructureServices`,
`AddWebAuthenticationServices`, `skestock.Web.DependencyInjection.AddWebServices`, and
`skestock.ServiceDefaults.Extensions.AddServiceDefaults`. `Web/Program.cs` composes them in this
order: service defaults, optional Key Vault, Application, Infrastructure, web auth, Web services.
Keep Worker-safe infrastructure registration separate from endpoint-bound web authentication.

### Mediator pipeline

`src/Application/DependencyInjection.cs` registers the scoped Mediator pipeline in this exact order:
`LoggingBehaviour`, `UnhandledExceptionBehaviour`, `AuthorizationBehaviour`, `ValidationBehaviour`,
`PerformanceBehaviour`, `CachingBehavior`, `CacheInvalidationBehavior`. Treat order as behavior,
not style. Queries implement `ICacheableQuery`; commands implement `ICacheInvalidation`. Use
HybridCache tag invalidation with sensible expirations and coarse plus entity-specific tags.

### Endpoints and errors

Endpoints are not controllers. Add an exported class under `src/Web/Endpoints` implementing
`IEndpointGroup` with `static void Map(RouteGroupBuilder)`. `MapEndpoints` discovers groups by
reflection and defaults to `/api/{ClassName}`. Map static named methods, not anonymous lambdas;
inject `ISender`, pass `CancellationToken`, return typed `Results<...>`, and dispatch all business
work through Application handlers. `result.IsFailed` must become
`result.ToProblemHttpResult()`.

Expected business failures are `FluentResults` typed `Error` subclasses with metadata keys from
`Application/Common/Errors/ErrorMetadataKeys.cs`; Web maps them to the frontend-facing
`ApiErrorContract`. Use exceptions only where the existing exception handler expects them
(validation/authorization/unhandled infrastructure cases). Do not replace typed domain failures
with generic exceptions.

### Data, pagination, and persistence

Handlers use `IApplicationDbContext`, EF LINQ, and `SaveChangesAsync`; concrete EF configuration
belongs in Infrastructure. Audit fields and domain events are applied by
`AuditableEntityInterceptor` and `DispatchDomainEventsInterceptor`. Reuse common filtering and
keyset helpers. A standard paginated list fetches `pageSize + 1`, returns `hasNextPage` and
`nextCursor`, and normally participates in cache tagging.

## Scaffolding / Codegen Tools

From `src/Application`, prefer the Clean Architecture template:

```bash
dotnet new ca-usecase --name CreateTodoList --feature-name TodoLists --usecase-type command --return-type int
dotnet new ca-usecase -n GetTodos -fn TodoLists -ut query -rt TodosVm
```

If unavailable, install `Clean.Architecture.Solution.Template::10.8.0`. The template does not
create every repository convention (pagination filter/sort/cache files or feature-specific tests);
complete those manually by copying the closest existing slice.

## Storage and asynchronous messaging

Blob uploads are client-direct SAS flows. `RequestUploadCommand` creates pending `FileMetadata` and
returns an upload SAS; `ConfirmUploadCommand` verifies the blob and marks it completed. Files use
the `app-files` container. Do not proxy file bytes through Web.

Goods-receipt imports use a transactional outbox:

1. Create the import and raise `GoodsReceiptImportCreatedEvent`.
2. Domain-event dispatch adds an `OutboxMessage` in the same unit of work, with a small message
   record's assembly-qualified type, JSON payload, originating user, and
   `Services.GoodsReceiptImportQueue`.
3. Web's `OutboxPublisherService` polls every 5 seconds, sends at most 50 messages, records
   success or retry/error state, and stops after five retries.
4. `AzureQueueSender` serializes/base64-encodes the envelope and creates the queue if needed.
5. Worker receives up to 10 messages with a 30-second visibility timeout, performs idempotency
   checks through `ProcessedMessages`, dispatches the deserialized Mediator request, and moves
   permanent/exhausted failures to the `goods-receipt-import-poison` queue.

Delivery is at-least-once: publisher rows are not claimed atomically and a successful send can be
resent if state persistence fails. Consumers must remain idempotent and preserve cancellation.

## Auth

Infrastructure registers `ApplicationUser` with `IdentityRole<Guid>`, EF stores, and core
authorization. Web adds `IdentityConstants.ApplicationScheme` cookie authentication as the default,
plus the bearer-token scheme for non-browser clients, and maps Identity API endpoints in
`Web/Endpoints/Users.cs`. `CurrentUser` reads the `NameIdentifier` claim.

AppHost injects explicit frontend origins into `Cors:AllowedOrigins`; Web uses
`WithOrigins(...).AllowCredentials()` and a development localhost fallback. Do not change this to
`AllowAnyOrigin()` when cookie credentials are required. Antiforgery configuration and middleware
are currently scaffolded but commented out; the Angular XSRF names are configured in anticipation,
not proof that server enforcement is active.

## Testing strategy

Use NUnit, Shouldly, and Moq for isolated unit tests. Mirror source feature/use-case folders under
`tests/Application.UnitTests`; test validators, handlers, behaviours, filtering, keyset, caching,
storage, and queue classification. Use real EF/Redis/Aspire resources only in functional or
infrastructure integration tests. Keep functional tests HTTP-level and reset state through
`TestBase`/`DatabaseResetter`. `Domain.UnitTests` is currently an empty project, so add domain
coverage when domain behavior is introduced. Coverlet is available, but no coverage threshold is
enforced in the repository.

## Frontend (`src/Client`)

The client is Angular `^22.1.0` with CLI/build `^22.1.6`, TypeScript `~6.0.2`, RxJS 7.8,
ng-zorro-antd `^22.0.1`, Tailwind/PostCSS 4, NgRx Signals/Operators 22, SignalR 10.0.11, and
Vitest 4. Its scripts are `dev`, `build`, `watch`, and `test`; Aspire uses `AddViteApp(...).WithNpm()`.

Use standalone components (do not set `standalone: true`), signals, `computed`, `input()`/`output()`,
`inject()`/`@Service()`, native `@if`/`@for`/`@switch`, and accessible WCAG AA markup. Do not use
`ngClass`, `ngStyle`, `@HostBinding`, or `@HostListener`; use host bindings and `class`/`style`
bindings. Prefer Signal Forms for new forms and Reactive Forms for existing complex forms.

`src/app/features/<name>` owns routed pages, lazy route files, headers, filters, tables, and page
stores. `src/app/shared/<name>` owns HTTP services, reusable `signalStoreFeature` composables,
collection/detail stores, and reusable UI. Core contains auth, layouts, models, theme, SignalR, and
shared error/pagination contracts. Import across boundaries through the aliases in `tsconfig.json`
(`@ske/...`), not deep relative paths.

SignalStore rules are strict: use `patchState()` for every state update, `rxMethod()` plus RxJS
operators for async work, `mapResponse({ next, error })` for success/error branches, named exports,
and `withEntities()` for true entity collections. Reusable loading/error behavior belongs in
features such as `withLoadingFeature` and `withProblemDetailsFeature`; clear errors before requests.
Do not introduce classic NgRx actions/reducers/effects for new code.

Routing has simple and full layout groups. Login is lazy-loaded under `simple` and guarded by
`guestGuard`; authenticated features are lazy-loaded under `full` and guarded by `authGuard`.
`app.config.ts` configures Romanian locale (`ro_RO`), `RON`, date-fns adapter, SignalR event maps,
credentialed API requests, and XSRF names `XSRF-TOKEN`/`X-XSRF-TOKEN`.

For ng-zorro APIs, use `.github/instructions/llms-full.txt` and official docs rather than guessing.
Import only required component APIs/icons, keep overlay configuration typed, configure global defaults
through providers, and preserve keyboard/focus/ARIA behavior.

## Template deviations to remember

- Mediator source generator replaces MediatR.
- FluentResults typed errors are the normal expected-failure path.
- Scalar replaces Swagger UI.
- Aspire owns SQL Server, Redis, Azurite, service discovery, and orchestration.
- `Shared.Services` centralizes resource names.
- `IEndpointGroup` reflection replaces a manual endpoint list.
- Web uses cookie auth by default while retaining bearer tokens.
- Transactional outbox, Azure Blob SAS uploads, Storage Queue processing, SignalR, and document
  extraction are custom additions.
- The real frontend is `src/Client`, not the stale `src/Web/ClientApp` location.
