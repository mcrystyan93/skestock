# CLAUDE.md

`AGENTS.md` holds the full Angular/NG-ZORRO rules (copied from `.github/instructions/`); `.github/copilot-instructions.md` holds extra backend detail; `docs/codebase/*.md` holds generated architecture/concerns notes (treat source as truth if they disagree). Read the relevant one before non-trivial work.

## 1. Project overview

skestock is a school-class stock/inventory app: staff manage items, categories, locations, stock batches, goods receipts (incl. AI-assisted document import), order lists, supply lists, and per-class configuration, with open/closed school classes and usage statistics. UI copy is Romanian.

## 2. Tech stack

- Backend: .NET 10 (`global.json` SDK 10.0.110), ASP.NET Core minimal APIs, Mediator (source-generated, **not** MediatR), FluentValidation, FluentResults, Ardalis.GuardClauses, Scalar for API docs.
- Data: EF Core 10 + SQL Server; ASP.NET Identity with cookie auth; Redis (HybridCache/FusionCache, distributed lock, SignalR backplane); Azure Storage (Azurite locally) blobs + queues; OpenAI for document extraction; ClosedXML/SkiaSharp exporters.
- Orchestration: .NET Aspire 13.6 (`src/AppHost`), OpenTelemetry via `src/ServiceDefaults`.
- Frontend (`src/Client`): Angular 22 (standalone, signals), NG-ZORRO 22, Tailwind 4, `@ngrx/signals` stores, SignalR client, ApexCharts, Vitest, Playwright, Prettier.
- Tests: NUnit, Shouldly, Moq, Respawn.

## 3. Repository layout

```
src/Domain          entities, enums (no deps)
src/Application     CQRS slices in Features/<Feature>/{Commands|Queries}/<UseCase>, Common/ (behaviours, caching, keyset, filtering, errors)
src/Infrastructure  EF (Data/, Configurations/, Migrations/), Identity, Storage, Queues, AI, Export, Realtime, Distributed
src/Web             host + Endpoints/ (IEndpointGroup), BackgroundJobs/ (outbox publisher)
src/Worker          queue consumer (goods-receipt imports) + daily statistics job
src/Shared          skestock.Shared.Services = all Aspire resource/queue/db/cache names
src/AppHost         Aspire resource graph only
src/Client          Angular app: app/core (auth, layouts, models, signalr, theme), app/features, app/shared
tests/              Application.UnitTests, Domain.UnitTests (empty), Infrastructure.IntegrationTests, Application.FunctionalTests, Worker.UnitTests, TestAppHost
```

## 4. Commands

Solution file is `skestock.slnx`. NuGet versions live only in `Directory.Packages.props`.

```bash
dotnet build                                   # warnings are errors; this is the only .NET lint gate
dotnet test tests/Application.UnitTests        # also: Worker.UnitTests, Infrastructure.IntegrationTests
dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~StockBatch"
./run-functional-tests.sh [dotnet test args]   # functional tests via Podman (sets DOCKER_HOST etc.)
dotnet run --project src/AppHost               # full stack; client :7001, Aspire dashboard :18080, API docs /scalar
cd src/Client && npm ci                        # install
cd src/Client && npm run build                 # dev build (prebuild regenerates icon catalog)
cd src/Client && npm test -- --watch=false     # Vitest via ng test
cd src/Client && npx prettier --write <files>  # format (printWidth 100, single quotes)
cd src/Client && npm run e2e                   # Playwright
```

New use case (run in `src/Application`): `dotnet new ca-usecase -n <Name> -fn <Feature> -ut command|query -rt <ReturnType>` (install `Clean.Architecture.Solution.Template::10.8.0` if missing). Add `CacheConstants.cs`, `<Feature>FilterConfiguration.cs`, `<Feature>SortConfiguration.cs` by hand for paginated slices.

Migrations:

```bash
dotnet ef migrations add <Name> --project src/Infrastructure --startup-project src/Web --context ApplicationDbContext --output-dir Data/Migrations
dotnet ef migrations has-pending-model-changes --project src/Infrastructure --startup-project src/Web --context ApplicationDbContext
```

Never run `dotnet run --project src/Web` directly; use AppHost.

## 5. Architecture and patterns

- Dependencies point inward: `Web`/`Infrastructure` → `Application` → `Domain`. `Application` talks to data only through `IApplicationDbContext` and other `Application/Common/Interfaces` abstractions.
- Mediator pipeline order in `src/Application/DependencyInjection.cs` is significant: Logging → UnhandledException → Authorization → ClassOpen → Validation → Performance → Caching → CacheInvalidation → ClassLock. Don't reorder. Commands that mutate a class implement `IRequiresOpenClass`.
- Queries implement `ICacheableQuery`; commands implement `ICacheInvalidation` (HybridCache tags, collection-level + entity-level).
- Expected business failures: return `Result`/`Result<T>` with typed errors from `Application/Common/Errors` (+ `ErrorMetadataKeys`). Reserve exceptions for cross-cutting failures (validation, auth, unhandled).
- Endpoints: class in `src/Web/Endpoints` implementing `IEndpointGroup` with static `Map(RouteGroupBuilder)`; auto-discovered. Map failed results with `ToProblemHttpResult()`.
- DI: register in the owning layer's `DependencyInjection.cs`, never ad hoc in `Program.cs`.
- Cross-process work goes Web → outbox → queue → Worker. Delivery is at-least-once: keep consumers idempotent; don't add logic to the sample loop in `src/Worker/Worker.cs`.
- Frontend: standalone components, signals, `@ngrx/signals` stores in `services/*.store.ts` with `*.http.ts` for API calls, lazy feature routes. Import across areas via `@ske/...` aliases (`src/Client/tsconfig.json`), each area exposes an `index.ts`. Reusable UI lives in `app/shared/ui`; domain-specific shared pieces in `app/shared/<domain>`.

## 6. Code conventions

- C#: file-scoped namespaces, nullable on, `Guard.Against.*` for guards, keep each project's `GlobalUsings.cs`, `CancellationToken` through async chains, `.editorconfig` rules (LF, 4 spaces).
- Name the file after its primary type. Name commands after the nearest slice (`Items`: Edit/Disable/Enable; Locations/Classes/Categories: Update; imports: Create/Confirm/Process).
- Use `skestock.Shared.Services` for every resource/queue/db/cache/config-section name. Never duplicate those literals.
- Persisted instants are UTC `DateTimeOffset`.
- Enums persisted as ints (e.g. `StockTransactionType`) must only be appended, never reordered.
- TypeScript: strict; never `any` (use `unknown`); `inject()`, `input()`/`output()`, `computed()`; native control flow; no `ngClass`/`ngStyle`, no `@HostBinding`/`@HostListener`; do not set `standalone: true` or `OnPush` explicitly; prefer Signal Forms; `@Service()` for new singletons. Details in `AGENTS.md`.
- Keep UI strings in Romanian, matching surrounding components.
- Comments: only the non-obvious why; match surrounding density.

## 7. Database rules

- Schema changes only via EF migrations (`dotnet ef migrations add`). Change entity/`Infrastructure/Data/Configurations`, then generate. Never hand-edit migration designer or `ApplicationDbContextModelSnapshot.cs`.
- Never edit or delete an already-applied migration; add a new one. Backfill existing rows with a column `defaultValue` in the migration (no startup code).
- Add indexes for new filter/sort/keyset paths (see `AddItemListingIndexesAndCollation`, `AddStockReportQueryIndexes`). Paginated lists use keyset cursors (`Application/Common/Keyset`).
- Concurrency tokens exist on stock batches; preserve them when editing stock writes.
- Migrations are applied automatically at startup (`DatabaseMigrationRunner`), so a merged migration reaches production on deploy: make it backward-safe and review it before merging.
- Never run migrations, seed, or Respawn resets against a production connection string. Seeding is in `ApplicationDbContextInitialiser.cs`.

## 8. Testing

- NUnit + Shouldly + Moq. `Application.UnitTests` mirrors `Application` folders (handlers, validators, filters, keyset, cache). `Worker.UnitTests` covers queue processing. Functional/integration tests need containers and reset via Respawn (`TestBase`).
- Client specs are colocated `*.spec.ts` (Vitest). Angular schematics default to `skipTests`, so add specs by hand.
- Add `Domain.UnitTests` tests when logic moves into entities.
- New backend logic: add a validator + handler unit test; add a functional test for new endpoints. New queue handlers: test duplicate delivery. New client stores/components with behavior: add a colocated spec.

## 9. Workflow rules for Claude

**Work queue**
- Treat all work as a queue of todos.
- Append mid-task tweaks, changes, and corrections to the queue. Never break the queue to handle a correction immediately.
- Stop only when 100% of the queue is clear.

**Mistakes**
- Never apologize. Mistakes usually point to a gap in the instruction files (CLAUDE.md, skills, docs).
- When something goes wrong, state plainly what happened, name the instruction gap that allowed it, and propose a concrete fix to the instruction files.

**Scope and engineering level (YAGNI)**
- Hold a YAGNI mentality. Never overengineer.
- Never expand scope without permission. Suggest worthwhile scope improvements and let the user decide.
- Justify all work.
- At the start of each task, assess where the project sits between "quick and dirty" and "production grade" and match the engineering to it. This repo is a deployed production app with a manual deploy workflow and no PR CI gate: treat it as production grade for data safety and auth, lean elsewhere.
- Follow existing patterns. Never introduce new patterns or dependencies without asking.

**Independent review**
- After making changes, spawn a sub-agent with minimal context on a cheap model (`model: "haiku"`) whose only job is to review the changes. Give it the diff and the task goal, never your reasoning.
- Evaluate its findings, fix real issues, and queue or report the rest.

**Safety and verification**
- Run `dotnet build` and the relevant tests after changes (client changes: `npm run build` and `npm test -- --watch=false`) and report results, including failures.
- After modifying code, run `graphify update .`.
- Create a branch for every new feature and open a PR when finished (never commit to `master` directly). The only automated workflow is the manual deploy, so run build and tests yourself before the PR.
- Ask before destructive actions: deleting files, dropping data, force-pushing, changing applied migrations.
- Never commit secrets; use user-secrets or environment variables.
- Ask before implementing when requirements are unclear.

## 10. Known pitfalls

- Cookie auth: keep the explicit CORS origin allowlist with credentials; never use `AllowAnyOrigin()`. Antiforgery is scaffolded but commented out (`src/Web/Endpoints/Antiforgery.cs`), so CSRF is not enforced.
- Public Identity registration exists (`src/Web/Endpoints/Users.cs`) and must be disabled/not relied on; no request uses role-based `[Authorize]`. Antiforgery will be enabled later: don't build features that assume it is absent.
- Functional tests use `TestAppHost` (waits ≤90 s for SQL/Redis/queue). Don't assume a clean DB outside `TestBase`. Use `./run-functional-tests.sh`, not bare `dotnet test`, under Podman.
- Feature-wide cache tags invalidate many list variants; keep tag changes consistent with `CacheConstants.cs`.
- Fragile areas: `Application/Common/{Behaviours,Filtering,Keyset,Caching}`, `ApplicationDbContextInitialiser.cs`, the outbox/Worker queue path, `shared/items/ui/autocomplete/item-autocomplete.ts`. Test before refactoring.
- Ignore generated/stale paths when searching: `graphify-out`, `src/Client/{dist,.angular,node_modules}`, `bin`, `obj`, `artifacts`. Path globs mentioning `src/Web/ClientApp` are stale; the frontend is `src/Client`.
- `docs/adr/` does not exist although older docs reference `docs/adr/0001-...`; UTC `DateTimeOffset` is enforced by migration `UseDateTimeOffsetForPersistedInstants`.
- `tasks/` and `SPEC.md` are working notes for in-flight features, not durable rules.

## 11. Open questions

- Is there an intended lint step (ESLint, `dotnet format`)? None is configured.

## graphify

A knowledge graph exists at `graphify-out/`. For codebase questions, first run `graphify query "<question>"` (also `graphify path "<A>" "<B>"`, `graphify explain "<concept>"`) when `graphify-out/graph.json` exists; use `graphify-out/wiki/index.md` for broad navigation if present, and `GRAPH_REPORT.md` only for broad architecture review. When the user types `/graphify`, use the graphify skill first.
