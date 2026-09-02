# Copilot Instructions — skestock

## Overview

`skestock` is a .NET 10 school stock/inventory system built from the Jason Taylor Clean Architecture template and orchestrated with .NET Aspire. Keep AGENTS.md as the primary rule source; use this file for the repo-specific details AGENTS.md does not spell out, especially the real project layout, the Angular frontend, test harnesses, and the Aspire resource graph.

Key deviations from the vanilla template:
- `Shared` is a real project used for cross-cutting constants (`skestock.Shared.Services`), not just a placeholder.
- The app is wired through Aspire (`src/AppHost`) with SQL Server, Redis, and Azure Storage/Azurite resources; the frontend runs as a Vite/Angular app via `AddViteApp`.
- `Web` uses Minimal API endpoint groups (`IEndpointGroup`) rather than controllers.
- `Application` uses `Mediator` (`Mediator.Abstractions` + `Mediator.SourceGenerator`), not MediatR.
- `src/Client` is the active Angular 22 SPA; do not treat `src/Web/ClientApp` as the real frontend.
- `src/Worker` is present in the solution and wired into AppHost, so do not ignore it when editing the solution graph.

## Architecture & Layer Responsibilities

Follow the inward dependency direction: `Domain` <- `Application` <- `Infrastructure` and `Web`; `AppHost` orchestrates runtime resources; `ServiceDefaults` contains shared hosting/telemetry setup; `Shared` holds cross-cutting constants only; `Client` talks to `Web` over HTTP only.

- `src/Domain`: entities, value objects, enums, domain events. It references only `Mediator.Abstractions` for request markers in a few places and has no project references.
- `src/Application`: CQRS handlers, validators, filters, keyset pagination, caching behaviors, and shared application models. It depends on `Domain` only plus package-level abstractions. `IApplicationDbContext` is the EF boundary.
- `src/Infrastructure`: EF Core implementation (`ApplicationDbContext`), Identity, persistence, caching/distributed services, and storage integrations.
- `src/Web`: Minimal API endpoints, OpenAPI/Scalar, auth setup, exception handling, and HTTP composition.
- `src/AppHost`: Aspire resource graph and orchestration for the web app, worker, SQL Server, Redis, Azurite, and the Angular frontend.
- `src/ServiceDefaults`: shared host defaults for OpenTelemetry, HTTP resilience, and service discovery.
- `src/Shared`: shared service/resource name constants used by AppHost and runtime projects.
- `src/Worker`: a separate worker project in the solution; currently wired into AppHost and should be treated as part of the runtime graph.
- `src/Client`: Angular 22 SPA using standalone components, NgRx SignalStore, ng-zorro-antd, Tailwind CSS, and Vite/Angular build tooling.

## Solution / Project Layout

Root files of note:
- `skestock.slnx` — solution file, not a classic `.sln`.
- `Directory.Build.props` — `net10.0`, nullable enabled, implicit usings enabled, warnings as errors.
- `Directory.Packages.props` — central package management; package versions live here only.
- `global.json` — pins SDK `10.0.110` with `rollForward: latestFeature`.
- `aspire.config.json` — AppHost path for Aspire tooling.

Backend projects:
- `src/Application`, `src/Domain`, `src/Infrastructure`, `src/Web`, `src/AppHost`, `src/ServiceDefaults`, `src/Shared`, `src/Worker`.

Test projects:
- `tests/Application.UnitTests`
- `tests/Application.FunctionalTests`
- `tests/Infrastructure.IntegrationTests`
- `tests/Domain.UnitTests`
- `tests/TestAppHost`

### Application feature slices

Feature folders live under `src/Application/Features/<FeatureName>/` and usually include:
- `Commands/<CommandName>/`
- `Queries/<QueryName>/`
- `Models/`
- supporting filter/sort/cache classes where needed

Current backend slices:
- `Categories`
- `Items`
- `Locations`
- `SchoolClasses`
- `GoodsReceipts`
- `Stock`
- `StockBatches`

Notable slice-specific conventions:
- `Categories`, `Items`, `Locations`, `SchoolClasses`, and `StockBatches` use paginated `GetAll*` queries plus filter/sort/cache boilerplate.
- `Stock` is intentionally non-paginated for `GetClassLocationStock` and returns `List<StockItemDto>`.
- `GoodsReceipts` is create/read-only and fans out into `StockBatch` and `StockTransaction` rows inside one handler transaction.
- `StockBatches` is read-only on the query side but has full filter/sort/cache boilerplate.

### Web endpoints

Endpoint groups live in `src/Web/Endpoints/*.cs` and implement `IEndpointGroup`. There are active groups for `Categories`, `GoodsReceipts`, `Items`, `Locations`, `SchoolClasses`, `Stock`, `StockBatches`, `Users`, plus `Storage`; `Antiforgery.cs` is currently commented out.

### Angular frontend layout

The SPA lives under `src/Client/src/app`:
- `core/` for auth, layouts, models, and theme
- `features/` for routed pages
- `shared/` for HTTP services, SignalStores, reusable UI, and feature-level store features
- `shared/tables/base-table.ts` for the generic virtual-scroll table base class
- `shared/errors` and `shared/loader` for cross-cutting store features
- `src/styles/` for Less/Tailwind theme bundles

## Critical Workflows (build / run / test)

Use these actual repo commands:

```bash
dotnet build
dotnet run --project src/AppHost
dotnet test
```

Frontend commands (from `src/Client`):

```bash
npm install
npm run dev
npm run build
npm test
```

Notes:
- `dotnet run --project src/AppHost` starts the Aspire dashboard and the containerized SQL Server/Redis/Azurite stack, then launches `Web`, `Worker`, and the Angular frontend.
- Running via AppHost requires Docker and Node/npm.
- `Web` alone has no local DB/cache fallback; it is meant to run through Aspire. In Development, `Web/Program.cs` falls back to localhost CORS origins so a manually started frontend can still talk to it.
- `Application.FunctionalTests` boot a real Aspire-hosted stack through `TestAppHost` and require Docker.
- Functional tests use `DatabaseResetter` (Respawn) to reset state between tests/fixtures; do not assume a clean database otherwise.
- The frontend dev server uses `ng serve` under `src/Client/package.json` and is also launched by Aspire via `AddViteApp`.

## Conventions & Patterns

### DI and hosting

Follow the layer-level extension pattern:
- `src/Application/DependencyInjection.cs` exposes `AddApplicationServices(this IHostApplicationBuilder builder)`.
- `src/Infrastructure/DependencyInjection.cs` exposes `AddInfrastructureServices(...)`.
- `src/Web/DependencyInjection.cs` exposes `AddWebServices(...)` and `AddKeyVaultIfConfigured(...)`.
- `src/ServiceDefaults/Extensions.cs` (per AGENTS.md) owns shared hosting setup.

`src/Web/Program.cs` composes services in this order:
`AddServiceDefaults()` -> `AddKeyVaultIfConfigured()` -> `AddApplicationServices()` -> `AddInfrastructureServices()` -> `AddWebServices()`.

### Mediator pipeline

`src/Application/DependencyInjection.cs` registers the pipeline in this order:
`LoggingBehaviour<,>` -> `UnhandledExceptionBehaviour<,>` -> `AuthorizationBehaviour<,>` -> `ValidationBehaviour<,>` -> `PerformanceBehaviour<,>` -> `CachingBehavior<,>` -> `CacheInvalidationBehavior<,>`.
Do not reorder these casually.

### Global usings and guards

Each project has its own `GlobalUsings.cs`. Common imports include `Ardalis.GuardClauses`, `Mediator`, `FluentResults`, `FluentValidation`, and `Microsoft.EntityFrameworkCore` depending on layer. Use `Guard.Against.*` for argument validation rather than manual null checks where the codebase already does.

### Endpoints

- Endpoints are not controllers.
- Implement `IEndpointGroup` with a static `Map(RouteGroupBuilder)` method.
- `app.MapEndpoints(typeof(Program).Assembly)` discovers and maps them.
- Endpoint handlers typically translate request DTOs into commands/queries, send them through `ISender`, and return `TypedResults` or `ToProblemHttpResult()`.

### Validation, filtering, pagination, and caching

- `Application` uses FluentValidation validators alongside handlers.
- Paginated queries implement `ICacheableQuery<T>` and usually inherit `BasePaginationFilter`.
- Cache invalidation commands implement `ICacheInvalidation` and use tag-based invalidation through `HybridCache`.
- `Categories` is the reference for keyset pagination, sorting, filtering, and cache key normalization.
- `Stock` is the reference for a deliberate non-paginated query.
- `StockBatches` shows the full paginated/filterable slice boilerplate.

### Auth and security

- `Infrastructure` configures Identity with both cookie and bearer support.
- `Web/Endpoints/Users.cs` maps `MapIdentityApi<ApplicationUser>()` plus a custom logout endpoint.
- Cookie auth is the default browser flow; bearer token auth remains available for non-browser clients.
- `Web` CORS must use explicit origins because cookie auth requires `AllowCredentials()`.
- Antiforgery support is scaffolded but currently commented out server-side; the frontend already sends the matching XSRF cookie/header names.

### Shared and constants

Use `skestock.Shared.Services` for resource names and service identifiers. Do not hardcode Aspire resource names or connection-string keys when a shared constant exists.

### Code style

- Prefer small, focused files and feature folders over wide utility classes.
- Keep async method names suffixed with `Async`.
- Preserve the repo’s existing use of file-scoped namespaces, records for DTOs where appropriate, and explicit `init` setters for request models.
- Avoid adding abstractions unless they are used for external dependencies or testing.

## Scaffolding / Codegen Tools

### Backend CQRS scaffolding

Use the template from `src/Application` when creating new use cases:

```bash
dotnet new ca-usecase --name CreateTodoList --feature-name TodoLists --usecase-type command --return-type int
dotnet new ca-usecase -n GetTodos -fn TodoLists -ut query -rt TodosVm
```

If the template is missing:

```bash
dotnet new install Clean.Architecture.Solution.Template::10.8.0
```

### Angular generation

Follow the existing Angular structure in `src/Client` rather than the legacy `src/Web/ClientApp` guidance. Use standalone components, route-level lazy loading, SignalStore-based state, and the shared feature/store patterns already used in `src/Client/src/app/features/*` and `src/Client/src/app/shared/*`.

## Auth

Identity is set up in `src/Infrastructure/DependencyInjection.cs` and used in `src/Web/Endpoints/Users.cs`. Browser auth uses the application cookie scheme; bearer tokens are also enabled. The SPA is already configured for credentialed CORS and XSRF via `src/Client/src/app/app.config.ts`.

## Testing Strategy

### Test stack

All test projects use NUnit. Assertion libraries vary by project but `Shouldly` is common.
- `Application.UnitTests`: NUnit + Moq + Shouldly + coverlet + EF Core InMemory.
- `Application.FunctionalTests`: NUnit + Moq + Shouldly + Respawn + Aspire.Hosting.Testing + WebApplicationFactory.
- `Infrastructure.IntegrationTests`: NUnit + coverlet.
- `Domain.UnitTests`: NUnit + Shouldly.

### Functional tests

`tests/Application.FunctionalTests/FunctionalTestSetup.cs` starts `Projects.TestAppHost`, waits for `Services.Database` and `Services.Cache`, creates `WebApiFactory`, and wires a `DatabaseResetter`.
`tests/Application.FunctionalTests/Infrastructure/WebApiFactory.cs` overrides connection strings and mocks `IUser`.
`tests/Application.FunctionalTests/Infrastructure/DatabaseResetter.cs` uses Respawn to reset SQL Server between tests.

### Unit test shape

- Mirror the production folder structure under `tests/Application.UnitTests/Features/...`.
- Keep tests behavior-focused and use AAA.
- Use per-test unique prefixes when seeding data that may interact with cached queries.
- For multi-entity handlers like `CreateGoodsReceipt`, prefer a dedicated in-memory `IApplicationDbContext` test fixture (see `GoodsReceiptTestDbContext.cs`) rather than ad hoc DbContext mocks.
- For pagination/filtering, validate sort order, cursor round-trips, and validation failures explicitly.

### Running tests

Prefer targeted runs first, then broaden only if needed:
- `dotnet test`
- `dotnet test tests/Application.UnitTests`
- `dotnet test tests/Application.FunctionalTests`

If running functional tests, make sure Docker is available first.

## Frontend

`src/Client` is the real SPA. It uses:
- Angular `^22.1.0`
- `ng-zorro-antd ^22.0.1`
- `@ngrx/signals` and `@ngrx/operators ^22.0.0`
- Tailwind CSS 4 + Less theme bundles
- `vitest` for unit testing
- npm (`packageManager: npm@11.16.0`)

### Frontend structure and conventions

- `src/app/app.config.ts` wires router, HTTP client, XSRF, app initializer, i18n, and date adapter.
- `src/app/app.routes.ts` only composes `simpleRoutes` and `fullRoutes`.
- `src/app/core/layouts/simple` hosts guest flows such as login.
- `src/app/core/layouts/full` hosts authenticated routes.
- `src/app/core/auth` contains the auth store, HTTP client, interceptor, guards, and antiforgery client.
- `src/app/shared/*` holds reusable HTTP services, SignalStore features, modals, dropdowns, tables, loaders, errors, and storage helpers.
- `src/app/features/*` owns routed page composition and lazy route definitions.
- `src/app/shared/tables/base-table.ts` is the generic table/virtual-scroll base class; reuse it for paged list UIs.

### Angular rules in this repo

Follow the Angular instruction files that apply to this codebase, but note the real frontend is `src/Client`:
- Prefer standalone components.
- Use signals/SignalStore for state.
- Prefer lazy-loaded feature routes.
- Use `input()`, `output()`, `computed()`, and `inject()`.
- Keep templates accessible and small.
- Use ng-zorro components and official APIs for dialogs, tables, forms, and overlays.
- `app.config.ts` already provides `provideNzI18n(ro_RO)`, `provideNzDateFnsAdapter()`, `LOCALE_ID = 'ro'`, and `DEFAULT_CURRENCY_CODE = 'RON'`.
- XSRF cookie/header names are `XSRF-TOKEN` and `X-XSRF-TOKEN`.

### Routing and state

- Authenticated routes live under `core/layouts/full/full.routes.ts` and are guarded by `authGuard`.
- Guest routes live under `core/layouts/simple/simple.routes.ts` and use `guestGuard`.
- Feature stores are composed from reusable `signalStoreFeature` helpers in `shared/*/store-features` and page-specific stores in `features/*/services`.
- HTTP-backed stores use `rxMethod` + `mapResponse`; do not introduce manual `subscribe()` patterns.

### API contracts

Frontend DTOs under `src/app/core/models` mirror backend contracts closely. Error handling uses `ProblemDetails` / `ValidationProblemDetails` from `src/app/core/models/errors.ts`; `withProblemDetailsFeature(prefix)` is the standard way to surface server errors.

### UI library conventions

`ng-zorro-antd` is the main component library. Prefer documented component APIs and global config over custom wrappers when a native option exists. Date components require an explicit date adapter provider, which is already configured in `app.config.ts`.
