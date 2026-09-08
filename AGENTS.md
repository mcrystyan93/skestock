# AGENTS.md

## Overview

`skestock` is a .NET 10 solution built from the **Jason Taylor Clean Architecture** template, orchestrated with **.NET Aspire**. Layers depend inward only: `Web` → `Infrastructure`/`Application` → `Domain`. `Shared` holds cross-cutting constants (e.g. service names) referenced by both `AppHost` and app projects — never hardcode service/connection-string names, use `skestock.Shared.Services`.

- **Domain**: entities, value objects, domain events. No dependencies on other layers.
- **Application**: CQRS via **[Mediator](https://github.com/martinothamar/Mediator)** (source-generator based, `Mediator.Abstractions`/`Mediator.SourceGenerator` — not MediatR). Contains feature-slice commands/queries, **FluentValidation** validators, storage/blob and queue interfaces, and cross-cutting `Behaviours` (pipeline order matters, see below). It references `Domain` and `Shared`, and uses only the `IApplicationDbContext` interface (in `Common/Interfaces`) rather than an EF Core provider.
- **Infrastructure**: EF Core (`ApplicationDbContext`), ASP.NET Core Identity (`ApplicationUser`), SaveChanges interceptors, Redis/FusionCache, Azure Blob/Queue adapters, document extraction providers, and SignalR.
- **Web**: Minimal API endpoints, OpenAPI/Scalar, exception handling middleware.
- **Worker**: queue consumer host for goods-receipt imports; it processes messages from Azure Storage Queue with idempotency, retries, and a poison queue.
- **AppHost**: Aspire orchestrator — defines SQL Server, Redis, Azurite Blob/Queue storage, Web, Worker, and the Angular Vite frontend.

## Solution/Project layout

Uses `.slnx` (`skestock.slnx`), not a classic `.sln`. Central package management via `Directory.Packages.props` — add package versions there, not inline in `.csproj` files.

The solution currently contains nine source projects (`AppHost`, `ServiceDefaults`, `Application`, `Domain`, `Infrastructure`, `Shared`, `Web`, `Client`, `Worker`) and five test projects (`Application.FunctionalTests`, `TestAppHost`, `Application.UnitTests`, `Domain.UnitTests`, `Infrastructure.IntegrationTests`).

## Critical workflows

```bash
dotnet build                              # build entire solution
dotnet run --project src/AppHost          # run app via Aspire (opens dashboard, starts SQL/Redis containers)
dotnet test                               # run all unit/integration/functional tests
```

- Running via `AppHost` requires a container runtime (SQL Server, Redis, and Azurite are containerized via Aspire). Web and Worker receive database, cache, blob, and queue references; the frontend runs as an Aspire-managed Angular/Vite app on port 7001.
- Functional tests (`Application.FunctionalTests`) spin up `TestAppHost` via `DistributedApplicationTestingBuilder` in `FunctionalTestSetup` — they need a container runtime and wait on `Services.Database` health before running.
- Scalar API reference is served at `/scalar` (default root redirect), not Swagger UI.

## Scaffolding new use cases (Application layer)

The template ships a `dotnet new` template for CQRS features. Prefer this over hand-writing boilerplate:

```bash
dotnet new ca-usecase --name CreateTodoList --feature-name TodoLists --usecase-type command --return-type int
dotnet new ca-usecase -n GetTodos -fn TodoLists -ut query -rt TodosVm
```

If `ca-usecase` template is missing: `dotnet new install Clean.Architecture.Solution.Template::10.8.0`. Run this from `src/Application/`.

## Mediator pipeline (order is significant)

Defined in `src/Application/DependencyInjection.cs`:
`LoggingBehaviour` (pre-processor) → `UnhandledExceptionBehaviour` → `AuthorizationBehaviour` → `ValidationBehaviour` → `PerformanceBehaviour` → `CachingBehavior` → `CacheInvalidationBehavior`. When adding a new cross-cutting `Behaviours/*` class, register it here in the intended position, not at the end by default.

## Application feature slices

Current use cases and shared list-query boilerplate are:

| Slice | Use cases | Pagination/filter/sort/cache |
|---|---|---|
| `Categories` | `Create`, `Update`, `GetAll`, `GetById` | Yes |
| `GoodsReceipts` | `CreateGoodsReceipt`, `CreateGoodsReceiptImport`, `ProcessGoodsReceiptImport`, `ConfirmGoodsReceiptImport`, `GetAllGoodsReceipts`, `GetGoodsReceiptById`, `GetAllGoodsReceiptImports`, `GetGoodsReceiptImportById` | Yes, for receipts and imports |
| `Items` | `Create`, `Edit`, `Disable`, `Enable`, `GetAll`, `GetById` | Yes |
| `Locations` | `Create`, `Update`, `GetAll`, `GetDefault`, `GetById` | Yes |
| `SchoolClasses` | `Create`, `Update`, `GetAll`, `GetById`, `GetSchoolClassSummary` | Yes for `GetAll` |
| `Stock` | `AdjustStock`, `GetClassLocationStock` | No; intentionally non-paginated |
| `StockBatches` | `CreateStockBatch`, `GetAllStockBatches` | Yes |

For paginated `GetAll` queries, add the slice's `CacheConstants`, filter configuration, and sort configuration alongside the use-case folders. `Stock.GetClassLocationStock` is the reference for a deliberately non-paginated query.

## Web endpoints pattern

Endpoints are NOT controllers. Each endpoint group is a class implementing `IEndpointGroup` (`src/Web/Infrastructure/IEndpointGroup.cs`) with a static `Map(RouteGroupBuilder)` method, e.g. `src/Web/Endpoints/Users.cs`. They are auto-discovered and mapped via `app.MapEndpoints(typeof(Program).Assembly)` (`EndpointRouteBuilderExtensions.cs`) — no manual registration needed, just drop a new class into `Web/Endpoints/`.

## Global usings

Each project (`Application`, `Web`, `Domain`, etc.) has its own `GlobalUsings.cs` importing `Ardalis.GuardClauses`, `Mediator`, `FluentValidation` (Application only), `Microsoft.EntityFrameworkCore` (Application only, interface-level). Use `Guard.Against.*` (Ardalis.GuardClauses) for argument/null validation instead of manual `if`/`throw`, matching existing usage (e.g. `Guard.Against.Null(connectionString, ...)` in `Infrastructure/DependencyInjection.cs`).

## DI registration convention

Each layer exposes a single `AddXServices(this IHostApplicationBuilder builder)` extension method in a top-level `DependencyInjection.cs` (or `Extensions.cs` for `ServiceDefaults`), in **its own project namespace** (`skestock.Application`, `skestock.Infrastructure`, `skestock.Web`, `skestock.ServiceDefaults`) — not `Microsoft.Extensions.DependencyInjection`. `Web/Program.cs` brings each layer's extension methods into scope with explicit `using skestock.Application;`, `using skestock.Infrastructure;`, `using skestock.ServiceDefaults;`, `using skestock.Web;` statements, then composes them: `AddServiceDefaults()` → `AddKeyVaultIfConfigured()` → `AddApplicationServices()` → `AddInfrastructureServices()` → `AddWebServices()`. Follow this same pattern for any new layer-level service registration: put the extension method in the layer's own namespace and add the `using` in `Program.cs`.

## Caching (`src/Application/Common/Caching/`)

Query-side caching and command-side invalidation are cross-cutting `Behaviours` too, wired
into the same Mediator pipeline (after `PerformanceBehaviour`): `CachingBehavior<,>` then
`CacheInvalidationBehavior<,>`. Both use `HybridCache`'s native **tag-based invalidation**
(not a hand-rolled version counter):
- A query implements `ICacheableQuery` (`Tags`, `BypassCache`, `SlidingExpiration`,
  `BuildCacheKey()`) — `CachingBehavior` calls `HybridCache.GetOrCreateAsync(key, factory,
  options, tags: message.Tags, ...)`, wrapping the `Result<TResponse>` via `ResultCache`/
  `ResultCacheTransformer`.
- A command implements `ICacheInvalidation` (`Tags`) — after a successful `Result`,
  `CacheInvalidationBehavior` calls `HybridCache.RemoveByTagAsync(cacheInvalidation.Tags, ct)`
  once for all tags.
- Tag invalidation is **lazy/logical**: `RemoveByTagAsync` doesn't physically evict entries,
  it marks a "created before this point is stale" watermark per tag; entries are recomputed
  on next read. Always set a sensible `SlidingExpiration`/`Expiration` as a safety net too.
- Use a mix of coarse (collection-level, e.g. `"items"`) and fine-grained (per-entity, e.g.
  `"items:{id}"`) tags so commands can invalidate broadly or narrowly as needed.

## Auth

Identity uses ASP.NET Core Identity's built-in API endpoints (`MapIdentityApi<ApplicationUser>()`) with the application cookie as the default scheme and bearer tokens (`AddBearerToken(IdentityConstants.BearerScheme)`) for non-browser clients. Roles use `IdentityRole<Guid>`; `Administrator` is the only current application role. Custom endpoints like logout (`Users.Logout`) sit alongside the built-in identity endpoints in the same `IEndpointGroup`.

Cookie-authenticated requests use an explicit CORS origin allowlist with credentials. Antiforgery validation is implemented and Angular-compatible, but its service registration and middleware call are currently commented out, so enforcement is disabled.

## Tests

Five test projects are present: `Domain.UnitTests` (currently empty), `Application.UnitTests` (including GoodsReceipts, Storage, and queue tests), `Application.FunctionalTests` (full Aspire-hosted stack via `TestAppHost`, covering Categories, GoodsReceipts, Items, Locations, SchoolClasses, Stock, StockBatches, and Storage), `Infrastructure.IntegrationTests`, and `TestAppHost`. Functional tests reset the DB per test/fixture via `DatabaseResetter` — don't assume a clean DB is provided automatically outside that helper.
