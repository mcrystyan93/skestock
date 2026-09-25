# Testing Patterns

## Core Sections

### 1) Test Stack and Commands

- .NET framework: NUnit `4.6.1`, NUnit3 adapter/analyzers, Shouldly `4.3.0`, Moq `4.20.72`, Respawn `7.0.0`, coverlet collector.
- Client framework: Vitest `4.0.8` through Angular CLI.

```bash
dotnet test
dotnet test tests/Application.UnitTests
dotnet test tests/Worker.UnitTests
dotnet test tests/Infrastructure.IntegrationTests
dotnet test tests/Application.FunctionalTests
./run-functional-tests.sh
dotnet test --settings functional-tests.runsettings
cd src/Client && npm ci && npm test
```

### 2) Test Layout

- `Application.UnitTests` mirrors Application features/common helpers and uses focused test contexts.
- `Application.FunctionalTests` contains HTTP-level feature tests and shared setup under `Infrastructure`.
- `Infrastructure.IntegrationTests` starts the test Aspire host and tests EF/Infrastructure behavior against SQL Server.
- `Worker.UnitTests` covers queue processing and daily schedule/worker behavior.
- `Domain.UnitTests` is a project shell with no test files currently.
- Client specs are colocated under `src/Client/src/**/*.spec.ts`.

### 3) Test Scope Matrix

| Scope | Status | Target | Notes |
|-------|--------|--------|-------|
| Unit | Present | Application handlers, validators, filters, keyset, cache, queue processor, daily schedule | No external resources |
| Functional HTTP | Present | Web API through `WebApiFactory` and test Aspire resources | `FunctionalTestSetup` waits up to 90 seconds for database/cache/queues |
| Infrastructure integration | Present | `ApplicationDbContext`, outbox claims, exporters, data protection | Requires containerized dependencies |
| Worker integration | Partial | Worker processor unit tests and functional host includes Worker/Azurite | Inspect individual tests before assuming every queue path is covered |
| Domain unit | Missing | `tests/Domain.UnitTests` | Add when domain invariants/behavior grow |
| Browser E2E | Not configured | — | No dedicated Playwright/Cypress project found |

### 4) Mocking and Isolation

- Unit tests mock Application abstractions with Moq or use lightweight EF in-memory/SQLite contexts.
- Functional tests use the real Web service and test Aspire host; `WebApiFactory` replaces `IUser` and blob storage with test doubles where needed.
- `TestBase.SetUp()` calls `TestApp.ResetState()`, which uses Respawn through `DatabaseResetter`; do not assume clean state outside that base.
- `tests/TestAppHost/Program.cs` currently provisions SQL Server, Redis, Azurite Blob/Queue resources, and a Worker. It does not start the Angular browser UI.

### 5) Coverage and Quality Signals

- Coverlet is available, but no coverage threshold/gate was found.
- The only GitHub Actions workflow is manually triggered production deployment; it validates build/unit/client tests during deployment but is not a push/PR gate.
- Functional/integration tiers are environment-sensitive because they require Docker/Podman and Aspire resource health.
- Domain behavior and browser-level behavior are the clearest coverage gaps.

### 6) Evidence

- `tests/Application.FunctionalTests/FunctionalTestSetup.cs`
- `tests/Application.FunctionalTests/Infrastructure/{TestApp,TestBase,WebApiFactory,DatabaseResetter}.cs`
- `tests/TestAppHost/Program.cs`
- `tests/Infrastructure.IntegrationTests/IntegrationTestSetup.cs`
- `tests/Worker.UnitTests`
- `tests/Domain.UnitTests/Domain.UnitTests.csproj`
- `src/Client/package.json`
- `run-functional-tests.sh`, `functional-tests.runsettings`
