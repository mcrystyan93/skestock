# Testing Patterns

## Core Sections (Required)

### 1) Test Stack and Commands

- Primary test framework: **NUnit 4.6.1** (`NUnit3TestAdapter` 6.3.0, `NUnit.Analyzers` 4.14.0) — **not** xUnit, across the four test suites; `TestAppHost` is a supporting Aspire host project.
- Assertion/mocking tools: **Shouldly 4.3.0** (assertions), **Moq 4.20.72** (mocking), **Respawn 7.0.0** (DB reset for functional tests), `coverlet.collector` 10.0.1 (coverage collection, no enforced threshold found).
- Commands:

```bash
dotnet test                                          # run all 4 test projects (needs Docker or Podman for functional/integration tests)
dotnet test tests/Application.UnitTests              # unit tests only — no external deps required
dotnet test tests/Application.FunctionalTests         # full HTTP-level tests — requires Aspire-orchestrated SQL Server + Redis
dotnet test tests/Infrastructure.IntegrationTests     # EF Core / ApplicationDbContext-level tests
./run-functional-tests.sh                             # functional tests via Podman socket activation (Fedora/systemd-user setup)
dotnet test --settings functional-tests.runsettings   # alternative: force DOCKER_HOST/DOTNET_ASPIRE_CONTAINER_RUNTIME env vars for Podman
```

### 2) Test Layout

- Placement pattern: **mirrors the source tree feature-for-feature**, not co-located with source files. `tests/Application.UnitTests/` and `tests/Application.FunctionalTests/` both replicate `src/Application/`'s folders 1:1 (`Common/Behaviours`, `Common/Caching`, `Common/Filtering`, `Common/Keyset`, and `Features/{Categories,Items,Locations,SchoolClasses}/{Commands,Queries}/<UseCase>/`), confirmed by direct `find` listing of both test trees.
- Naming convention: `<UseCase><Role>Tests.cs`, e.g. `GetAllCategoriesHandlerTests.cs`, `GetAllCategoriesQueryValidatorTests.cs` (unit), `GetAllCategoriesQueryTests.cs` (functional — one test class per use case, no separate handler/validator split since it drives the full HTTP pipeline).
- Setup files: `tests/Application.FunctionalTests/FunctionalTestSetup.cs` is an NUnit `[SetUpFixture]` with `[OneTimeSetUp]`/`[OneTimeTearDown]` — boots `TestAppHost` via `DistributedApplicationTestingBuilder`, waits (90s timeout) for `Services.Database` and `Services.Cache` resource health, builds a `WebApiFactory`, and creates a `DatabaseResetter`. `tests/Application.FunctionalTests/Infrastructure/{TestApp,TestBase,WebApiFactory,DatabaseResetter}.cs` provide the shared HTTP-client/DB-reset scaffolding consumed by individual test classes.

### 3) Test Scope Matrix

| Scope | Covered? | Typical target | Notes |
|-------|----------|------------------|-------|
| Unit | Yes | `Application` layer only — handlers, validators, filtering/keyset/caching helpers (`Common/Behaviours`, `Common/Caching`, `Common/Filtering`, `Common/Keyset`) and all 4 feature slices | No mocked EF Core provider confirmed beyond Moq-based `IApplicationDbContext` substitution — exact mocking mechanics not independently re-verified per test file in this pass (`[TODO]`) |
| Integration | Yes | `tests/Infrastructure.IntegrationTests` — exercises `ApplicationDbContext`/EF Core directly against a real database | Requires Docker/Podman (same as functional tests, since Infrastructure needs a live SQL Server) |
| Functional (HTTP end-to-end) | Yes | `tests/Application.FunctionalTests` — full HTTP pipeline via `WebApiFactory`, driven by a live Aspire-hosted SQL Server + Redis (`TestAppHost`) | This is the repo's closest equivalent to E2E testing — no separate browser/UI E2E layer exists (no frontend yet) |
| Domain unit tests | **No** | `tests/Domain.UnitTests` project exists but contains **zero test files** (only the `.csproj`) | Confirmed via direct listing — don't assume any Domain-layer logic has test coverage |
| E2E (browser/UI) | No dedicated suite declared | `src/Client` Angular application | Frontend unit/component tests use Vitest via `npm test`; no browser E2E runner is declared |

### 4) Mocking and Isolation Strategy

- Main mocking approach: **Application.UnitTests** use Moq to substitute `IApplicationDbContext` and other Application-layer interfaces, keeping unit tests free of any real database/network dependency. **Application.FunctionalTests** deliberately use **zero mocks** — they exercise the real `ApplicationDbContext` against a containerized SQL Server and a real Redis instance, orchestrated by the slimmed `TestAppHost` (SQL Server + Redis only, no Web project reference — the Web app itself runs in-process via `WebApiFactory`, an `WebApplicationFactory`-style test host).
- Isolation guarantees: `DatabaseResetter` (Respawn-based, `tests/Application.FunctionalTests/Infrastructure/DatabaseResetter.cs`) resets the database state — the skill/AGENTS.md convention is explicit that **a clean DB is NOT assumed automatically outside this helper**; tests must invoke the resetter (typically via `TestBase`) between runs.
- Common failure mode in tests: functional tests require Docker or Podman to be running and reachable — `run-functional-tests.sh` exists specifically to work around Podman-socket activation quirks (systemd user unit `podman.socket`), implying Docker-based test runs have been unreliable or unavailable in at least one prior dev environment for this repo.

### 5) Coverage and Quality Signals

- Coverage tool + threshold: `coverlet.collector` is a pinned dependency, but **no coverage threshold/gate** was found in any `.csproj`, `.runsettings`, or CI config (no CI/CD pipeline exists in this repo per the scan). `[TODO]`
- Current reported coverage: `[TODO]` — not measured in this pass (would require running `dotnet test --collect:"XPlat Code Coverage"`).
- Known gaps/flaky areas: `Domain.UnitTests` has **zero** coverage (empty project). Functional/integration tests depend on external container runtime availability (Docker or Podman) and are therefore the most environment-sensitive/flaky-prone tier — see the Podman workaround script as direct evidence this has been a friction point.

### 6) Evidence

- `Directory.Packages.props` (NUnit/Shouldly/Moq/Respawn/coverlet versions)
- `tests/Application.FunctionalTests/FunctionalTestSetup.cs`, `tests/Application.FunctionalTests/Infrastructure/*.cs`
- `tests/TestAppHost/Program.cs`
- `run-functional-tests.sh`, `functional-tests.runsettings`
- Direct directory listings of `tests/Application.UnitTests/Features/*` and `tests/Application.FunctionalTests/Features/*` (mirrored 1:1 across 4 feature slices)
- `tests/Domain.UnitTests/` (confirmed empty beyond the `.csproj`)

## Extended Sections (Optional)

Not added — no framework-specific suite patterns beyond NUnit conventions already covered, and no historical flaky-test data exists to catalog (no CI history, no git history available in this environment).
