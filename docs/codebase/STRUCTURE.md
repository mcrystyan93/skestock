# Codebase Structure

> See also `AGENTS.md` and `.github/copilot-instructions.md` for a narrative walkthrough of this
> same layout — this document is the tabular/quick-reference companion.

## Core Sections (Required)

### 1) Top-Level Map

| Path | Purpose | Evidence |
|------|---------|----------|
| `skestock.slnx` | Solution file (XML-based `.slnx`, not classic `.sln`) | root |
| `Directory.Build.props` | Shared MSBuild props: `net10.0`, `Nullable` on, `TreatWarningsAsErrors=true` | root |
| `Directory.Packages.props` | Central Package Management — every NuGet version pinned here | root |
| `global.json` | Pins .NET SDK `10.0.110`, `rollForward: latestFeature` | root |
| `aspire.config.json`, `.aspire/settings.json` | Aspire CLI/tooling config pointing at `src/AppHost/AppHost.csproj` | root, `.aspire/` |
| `AGENTS.md` | Canonical agent quick-reference (layers, DI convention, Mediator order, endpoint pattern) | root |
| `.github/copilot-instructions.md` | Deeper file-by-file agent instructions | `.github/` |
| `.github/workflows/deploy-production.yml` | `workflow_dispatch`-only production deployment pipeline (build, containerize, push, deploy) | `.github/workflows/` |
| `run-functional-tests.sh`, `functional-tests.runsettings` | Podman-based runner/settings for `Application.FunctionalTests` | root |
| `deploy/` | Production deployment assets: `deploy.sh`, `production.env.example`, Podman quadlet unit files for web/worker/cache/db/storage containers and volumes/network | `deploy/` |
| `src/AppHost/` | .NET Aspire orchestrator — SQL Server, Redis, Azurite (blob/queue), Web, Worker, and the Vite frontend resources | `src/AppHost/Program.cs` |
| `src/Domain/` | Entities, value objects, domain events, queue message contracts — zero project references | `src/Domain/Domain.csproj` |
| `src/Application/` | CQRS (Mediator) commands/queries, validators, cross-cutting behaviours, storage/queue interfaces; references `Domain` + `Shared` | `src/Application/Application.csproj` |
| `src/Infrastructure/` | EF Core `ApplicationDbContext`, Identity, HybridCache/Redis, Azure Blob/Queue adapters, SignalR (`AppHub`), OpenAI document extraction | `src/Infrastructure/Infrastructure.csproj` |
| `src/Web/` | Minimal API host, endpoint groups, OpenAPI/Scalar, exception handling, `OutboxPublisherService` — the composition root | `src/Web/Program.cs` |
| `src/Worker/` | Non-HTTP host consuming Azure Storage Queue messages (goods-receipt/category/item import processing) | `src/Worker/Program.cs`, `src/Worker/Queues/*.cs` |
| `src/ServiceDefaults/` | Shared OpenTelemetry/health-check/service-discovery extensions (`AddServiceDefaults()`) | `src/ServiceDefaults/Extensions.cs` |
| `src/Shared/` | Cross-cutting resource/service-name constants (`skestock.Shared.Services`) | `src/Shared/Services.cs` |
| `src/Client/` | Angular ^22.1.0 SPA (`Client.esproj`), independent npm project, no .NET project references | `src/Client/package.json`, `Client.esproj` |
| `tests/Domain.UnitTests/` | Project shell exists; **still contains zero test files** (confirmed) | only `Domain.UnitTests.csproj` present |
| `tests/Application.UnitTests/` | NUnit unit tests, mirrors `src/Application/` feature-for-feature (now includes Categories, GoodsReceipts, Items, Locations, OrderLists, SchoolClasses, Statistics, Stock, StockBatches, Storage) | directory listing |
| `tests/Application.FunctionalTests/` | Full HTTP-level tests via a real Aspire-hosted stack (`TestAppHost`) | `tests/Application.FunctionalTests/FunctionalTestSetup.cs` |
| `tests/Infrastructure.IntegrationTests/` | EF Core/`ApplicationDbContext`-level tests | `tests/Infrastructure.IntegrationTests/Infrastructure.IntegrationTests.csproj` |
| `tests/Worker.UnitTests/` | NUnit tests for the queue-processing pipeline (`Queues/QueueProcessingServiceTests.cs`, `QueueProcessingTestHarness.cs`) | `tests/Worker.UnitTests/Worker.UnitTests.csproj` |
| `tests/TestAppHost/` | Slimmed Aspire host (SQL Server + Redis only) used only by functional tests | `tests/TestAppHost/Program.cs` |
| `.github/agents/`, `.github/instructions/`, `.github/skills/` | Custom agent definitions, path-scoped coding-style instructions, and Copilot skills | `.github/` |
| `docs/codebase/` | This codebase map (generated/maintained by the `acquire-codebase-knowledge` skill) | `docs/codebase/` |
| `graphify-out/` | Generated knowledge-graph artifacts (`graph.json`, `GRAPH_REPORT.md`) for AI-assisted code navigation | `graphify-out/` |

### 2) Entry Points

- **Main runtime entry (orchestrated dev run):** `src/AppHost/Program.cs` — builds the SQL Server + Redis + Azurite (blob/queue) + Web + Worker + frontend resource graph and starts the Aspire dashboard.
- **Actual HTTP app entry:** `src/Web/Program.cs` — the ASP.NET Core composition root; only fully functional when started via `AppHost` (no standalone Redis/Azurite wiring otherwise).
- **Worker entry:** `src/Worker/Program.cs` — composes Application + Infrastructure + ServiceDefaults and starts `CategoryImportBatchQueueProcessingService`, `GoodsReceiptImportQueueProcessingService`, `ItemImportQueueProcessingService` as hosted `BackgroundService`s. `src/Worker/Worker.cs` is a leftover template sample loop — do not add new processing there.
- **Frontend entry:** `src/Client/src/main.ts` (Angular bootstrap), served by Vite; Aspire launches it via `AddViteApp(...).WithNpm()`.
- **Test entry points:** `tests/TestAppHost/Program.cs` (slim Aspire host for functional tests, started via `DistributedApplicationTestingBuilder.CreateAsync<Projects.TestAppHost>`).
- Selection mechanism: which "app" boots is purely determined by which `.csproj` is passed to `dotnet run`/`dotnet test`.

### 3) Module Boundaries

| Boundary | What belongs here | What must not be here |
|----------|--------------------|------------------------|
| `Domain` | Entities, enums, domain events, queue contracts (`MessageEnvelope`, `OutboxMessage`, `ProcessedMessage`) | Any project reference, EF Core attributes/fluent config, business orchestration logic |
| `Application` | Mediator commands/queries + handlers + validators, pipeline `Behaviours`, `Result`/`Error` types, filtering/keyset/caching helpers, storage commands, queue interfaces, document-extraction interfaces, `IApplicationDbContext` | Concrete EF Core provider usage, ASP.NET Core types |
| `Infrastructure` | `ApplicationDbContext`, EF configurations, SaveChanges interceptors, Identity, Redis/HybridCache, Azure Blob/Queue adapters, SignalR notifier, document extraction | Mediator commands/queries, endpoint/route definitions |
| `Web` | Minimal API `IEndpointGroup` classes, OpenAPI/Scalar, exception-to-`ProblemDetails` mapping, `OutboxPublisherService`, `Program.cs` composition root | Direct EF Core queries, business/validation logic |
| `Worker` | Azure Storage Queue consumers with idempotency (`ProcessedMessages`), retry/poison-queue handling | HTTP endpoints, direct business logic outside Mediator dispatch |
| `Shared` | Aspire/service resource-name string constants only (`Services.cs`) | Business logic, EF Core, HTTP handling |
| `AppHost` / `TestAppHost` | Aspire resource graph definitions | Any application logic |
| `Client` | Angular SPA — routed features, shared services/stores, core (auth/layouts/SignalR) | Anything requiring a .NET project reference |

### 4) Naming and Organization Rules

- **Feature-slice organization** inside `Application`: `Features/<FeatureName>/{Commands,Queries}/<UseCase>/{<UseCase>Command|Query.cs, Handler.cs, Validator.cs}`, plus per-feature `<FeatureName>FilterConfiguration.cs`, `<FeatureName>SortConfiguration.cs`, `CacheConstants.cs` for paginated slices. Implemented feature slices: `Categories`, `GoodsReceipts`, `Items`, `Locations`, `OrderLists`, `SchoolClasses`, `Statistics`, `Stock`, `StockBatches` — all under `src/Application/Features/`.
- **File naming**: PascalCase throughout, one primary type per file, file name matches the type name.
- **Test mirroring**: `tests/Application.UnitTests/` and `tests/Application.FunctionalTests/` mirror `src/Application/`'s folder tree feature-for-feature.
- **No import-alias system** in .NET — namespaces mirror folder paths 1:1. The Angular client, however, uses TS path aliases (`@ske/...`, see `tsconfig.json`) — map those before assuming a deep relative import path.

### 5) Evidence

- `skestock.slnx` (authoritative project list and grouping)
- `src/AppHost/Program.cs`, `src/Web/Program.cs`, `src/Worker/Program.cs`, `tests/TestAppHost/Program.cs` (entry points)
- Direct `find`/`ls` listings of `src/Application/Features/*`, `src/Worker/Queues/*`, `tests/Application.UnitTests/Features/*`
- `.github/workflows/deploy-production.yml`, `deploy/`

## Extended Sections (Optional)

Not needed at current repo size/complexity (single small-to-mid solution + one Angular SPA, no monorepo/workspaces beyond that).
