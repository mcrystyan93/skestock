# Testing Patterns

## Core Sections (Required)

### 1) Test Stack and Commands

- Primary .NET test framework: **NUnit 4.6.1** (`NUnit3TestAdapter` 6.3.0, `NUnit.Analyzers`) — not xUnit, across `Application.UnitTests`, `Application.FunctionalTests`, `Domain.UnitTests`, `Infrastructure.IntegrationTests`, and `Worker.UnitTests`; `TestAppHost` is a supporting Aspire host project, not a test project.
- Assertion/mocking tools: **Shouldly 4.3.0**, **Moq 4.20.72**, **Respawn 7.0.0** (DB reset for functional tests), `coverlet.collector` 10.0.1 (no enforced threshold found).
- Client test framework: **Vitest 4** (`src/Client`), run via `npm test`.
- Commands:

```bash
dotnet test                                          # run all .NET test projects (needs Docker or Podman for functional/integration tests)
dotnet test tests/Application.UnitTests               # unit tests only — no external deps required
dotnet test tests/Application.FunctionalTests         # full HTTP-level tests — requires Aspire-orchestrated SQL Server + Redis
dotnet test tests/Infrastructure.IntegrationTests     # EF Core / ApplicationDbContext-level tests
./run-functional-tests.sh                             # functional tests via Podman socket activation
dotnet test --settings functional-tests.runsettings   # alternative: force Podman env vars via runsettings file
cd src/Client && npm test                             # Angular client Vitest suite
```

### 2) Test Layout

- Placement pattern: **mirrors the source tree feature-for-feature**, not co-located with source files. `tests/Application.UnitTests/` and `tests/Application.FunctionalTests/` replicate `src/Application/`'s folders 1:1, now covering `Common/{Behaviours,Caching,Filtering,Keyset}` and `Features/{Categories,GoodsReceipts,Items,Locations,OrderLists,SchoolClasses,Statistics,Stock,StockBatches,Storage}`. `tests/Worker.UnitTests/Queues/` mirrors `src/Worker/Queues/`.
- Naming convention: `<UseCase><Role>Tests.cs`, e.g. `GetAllCategoriesHandlerTests.cs` (unit), `GetAllCategoriesQueryTests.cs` (functional). Import-batch flows follow the same pattern (e.g. `ConfirmCategoryImportBatchCommandHandlerTests.cs`), with per-use-case `*TestDbContext.cs` fixtures (e.g. `CategoryImportBatchTestDbContext`, `ProcessGoodsReceiptImportTestDbContext`).
- Setup files: `tests/Application.FunctionalTests/FunctionalTestSetup.cs` is an NUnit `[SetUpFixture]` — boots `TestAppHost` via `DistributedApplicationTestingBuilder`, waits (90s timeout) for `Services.Database`/`Services.Cache` health, builds a `WebApiFactory`, creates a `DatabaseResetter`. `tests/Application.FunctionalTests/Infrastructure/{TestApp,TestBase,WebApiFactory,DatabaseResetter}.cs` provide shared scaffolding.

### 3) Test Scope Matrix

| Scope | Covered? | Typical target | Notes |
|-------|----------|------------------|-------|
| Unit | Yes | `Application` layer — handlers, validators, filtering/keyset/caching helpers, all 8 feature slices including import-batch commands | Moq-based `IApplicationDbContext`/`IRealtimeNotifier`/queue-sender substitution; mocking mechanics not independently re-verified per test file this pass |
| Integration | Yes | `tests/Infrastructure.IntegrationTests` — exercises `ApplicationDbContext`/EF Core directly against a real database | Requires Docker/Podman |
| Functional (HTTP end-to-end) | Yes | `tests/Application.FunctionalTests` — full HTTP pipeline via `WebApiFactory`, driven by a live Aspire-hosted SQL Server + Redis (`TestAppHost`) | `TestAppHost` provides only SQL Server + Redis, so functional tests do **not** exercise Azurite/queues/Worker/SignalR/browser UI |
| Domain unit tests | **No** | `tests/Domain.UnitTests` project exists but **still contains zero test files** (confirmed again this pass — only the `.csproj` present) | Domain now has richer entities (`ClassItemStockVisibility`, `CategoryImportBatch`, etc.) but none are independently unit-tested at the Domain layer |
| Worker/queue processing | Yes (unit) | `tests/Worker.UnitTests/Queues/QueueProcessingServiceTests.cs` with `QueueProcessingTestHarness.cs` | Processor logic (dedup, retry, poison-queue classification) is unit-tested in isolation; the real publisher→Azure Queue→consumer wiring is still **not** covered end-to-end (no Azurite/Worker in `TestAppHost`) |
| Client unit/component tests | Yes | Vitest (`src/Client`, `npm test`) | No dedicated browser/E2E runner declared |
| E2E (browser/UI) | No dedicated suite declared | — | — |

### 4) Mocking and Isolation Strategy

- Main mocking approach: **Application.UnitTests** use Moq to substitute `IApplicationDbContext` and other Application-layer interfaces (including newer interfaces like `IRealtimeNotifier` and queue/storage abstractions for import-batch flows), keeping unit tests free of real DB/network/queue dependencies. **Application.FunctionalTests** deliberately use **zero mocks** for DB/cache — they exercise the real `ApplicationDbContext` against a containerized SQL Server and Redis via `TestAppHost`.
- Isolation guarantees: `DatabaseResetter` (Respawn-based) resets DB state between functional test runs — a clean DB is **not** assumed automatically outside that helper.
- Common failure mode: functional/integration tests require Docker or Podman reachable — `run-functional-tests.sh` exists specifically to work around Podman-socket activation quirks.

### 5) Coverage and Quality Signals

- Coverage tool + threshold: `coverlet.collector` is pinned, but **no coverage threshold/gate** was found in any `.csproj`/`.runsettings`, and the one GitHub Actions workflow (`deploy-production.yml`) is `workflow_dispatch`-only (production deploy), not a build/test gate on push/PR — there is currently no CI enforcement of `dotnet test` or `npm test` passing before merge.
- Current reported coverage: `[TODO]` — not measured in this pass.
- Known gaps/flaky areas: `Domain.UnitTests` remains zero-coverage; Worker/queue processing now has unit coverage (`Worker.UnitTests`) but no end-to-end publisher→queue→worker integration test; functional/integration tests depend on Docker/Podman availability and are the most environment-sensitive tier.

### 6) Evidence

- `Directory.Packages.props` (NUnit/Shouldly/Moq/Respawn/coverlet versions), `src/Client/package.json` (Vitest)
- `tests/Application.FunctionalTests/FunctionalTestSetup.cs`, `tests/Application.FunctionalTests/Infrastructure/*.cs`
- `tests/TestAppHost/Program.cs`
- `run-functional-tests.sh`, `functional-tests.runsettings`
- Directory listings of `tests/Application.UnitTests/Features/*` (10 feature slices incl. OrderLists), `tests/Worker.UnitTests/Queues/*`, and `tests/Domain.UnitTests/` (confirmed still empty)
- `.github/workflows/deploy-production.yml` (confirmed `workflow_dispatch`-only, no build/test gate)

## Extended Sections (Optional)

Not added — no framework-specific suite patterns beyond NUnit/Vitest conventions already covered.
