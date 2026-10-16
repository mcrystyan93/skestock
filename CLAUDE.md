# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

`AGENTS.md` is the detailed source of truth (architecture rules, implementation patterns, data/auth gotchas, Angular/NG-ZORRO conventions). Read it first; `.github/copilot-instructions.md` and `.github/instructions/` hold the Angular/NG-ZORRO rules for `src/Client`.

## Commands

.NET 10 solution using the XML solution file `skestock.slnx` (not `.sln`). `TreatWarningsAsErrors` is on; NuGet versions are central in `Directory.Packages.props`.

```bash
dotnet build
dotnet test                                      # all tests (functional tests need containers)
dotnet test tests/Application.UnitTests
dotnet test tests/Infrastructure.IntegrationTests
dotnet test tests/Worker.UnitTests
dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~SomeTestName"   # single test
./run-functional-tests.sh [extra dotnet test args]   # functional tests via Podman (sets DOCKER_HOST etc.)
dotnet run --project src/AppHost                 # full stack via Aspire (needs Docker/Podman + Node)

cd src/Client && npm ci && npm run build         # Angular client
cd src/Client && npm test                        # ng test
cd src/Client && npm run e2e                     # Playwright
```

Full-stack run: client on `7001`, Aspire dashboard on `18080`, Web API reference at `/scalar`. AppHost starts SQL Server, Redis, Azurite, Web, Worker and the Vite/Angular client.

New CQRS use cases: from `src/Application`, `dotnet new ca-usecase -n <Name> -fn <Feature> -ut command|query -rt <ReturnType>` (install `Clean.Architecture.Solution.Template::10.8.0` if missing).

## Architecture (big picture)

- Layers: `Web`/`Infrastructure` → `Application` → `Domain`; `Shared` holds cross-project constants (`skestock.Shared.Services` names for every Aspire resource, queue, DB, cache, config section — never duplicate those literals). `AppHost` is only the Aspire resource graph. `src/Client` is an independent Angular app.
- `Application` uses Mediator (source-generated, not MediatR), FluentValidation, and only `IApplicationDbContext`. EF config and external adapters live in `Infrastructure` (`src/Infrastructure/Data`).
- Mediator pipeline order in `src/Application/DependencyInjection.cs` is significant: Logging → UnhandledException → Authorization → ClassOpen → Validation → Performance → Caching → CacheInvalidation → ClassLock (the last two apply only to `IRequiresOpenClass` commands). Queries implement `ICacheableQuery`, commands `ICacheInvalidation` (HybridCache tags: collection-level and entity-level).
- HTTP endpoints: classes in `src/Web/Endpoints` implementing `IEndpointGroup` with static `Map(RouteGroupBuilder)`; auto-discovered, no manual registration. Map failed `Result`s with the existing problem-result mapper.
- `Web` and `Worker` are separate hosts; `Worker` consumes goods-receipt queue messages (idempotency/retry/poison queue). Don't add logic to the sample loop in `src/Worker/Worker.cs`.
- Persisted instants are UTC `DateTimeOffset` (ADR 0001 in `docs/adr/`). Cookie auth needs an explicit CORS origin allowlist with credentials; antiforgery is scaffolded but not enforced.
- Functional tests use `TestAppHost` (SQL/Redis/queue only — no Azurite, Worker or browser) with Respawn resets.
- Never hand-edit generated EF migration designer/snapshot files.

## graphify

A knowledge graph exists at `graphify-out/`. For codebase questions, first run `graphify query "<question>"` (also `graphify path "<A>" "<B>"`, `graphify explain "<concept>"`) when `graphify-out/graph.json` exists; use `graphify-out/wiki/index.md` for broad navigation if present, and `GRAPH_REPORT.md` only for broad architecture review. After modifying code, run `graphify update .`. When the user types `/graphify`, use the graphify skill first.
