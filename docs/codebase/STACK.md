# Technology Stack

## Core Sections (Required)

### 1) Runtime Summary

| Area | Value | Evidence |
|------|-------|----------|
| Primary language | C# 14 / .NET 10 | `Directory.Build.props` (`<TargetFramework>net10.0</TargetFramework>`) |
| Runtime + version | .NET SDK `10.0.110`, roll-forward `latestFeature` | `global.json` |
| Package manager | NuGet with Central Package Management (CPM) | `Directory.Packages.props` (`ManagePackageVersionsCentrally=true`) |
| Module/build system | MSBuild, `.slnx` solution (not classic `.sln`) | `skestock.slnx` |
| Orchestration | .NET Aspire 13.5.2 (AppHost model, not Docker Compose/K8s) | `src/AppHost/Program.cs`, `aspire.config.json`, `.aspire/settings.json` |

### 2) Production Frameworks and Dependencies

Only high-impact production dependencies listed (see `Directory.Packages.props` for the full, centrally-managed list).

| Dependency | Version | Role in system | Evidence |
|------------|---------|-----------------|----------|
| ASP.NET Core / `Microsoft.AspNetCore.OpenApi` | .NET 10 / 10.0.11 | Minimal API host (`src/Web`), OpenAPI generation | `Directory.Build.props`, `Directory.Packages.props`, `src/Web/Program.cs` |
| `Microsoft.EntityFrameworkCore` (+ `.Design`, SqlServer via Aspire) | 10.0.11 | ORM, migrations, `ApplicationDbContext` | `src/Infrastructure/Data/ApplicationDbContext.cs` |
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | 10.0.11 | User/role identity store (`IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>`) | `src/Infrastructure/Data/ApplicationDbContext.cs` |
| `Mediator.Abstractions` / `Mediator.SourceGenerator` | 3.0.2 | CQRS mediator (source-generator based, **not MediatR**) — commands/queries/pipeline behaviours | `src/Application/DependencyInjection.cs` |
| `FluentValidation.DependencyInjectionExtensions` | 12.1.1 | Request validators, wired into the Mediator pipeline via `ValidationBehaviour` | `src/Application/Common/Behaviours/ValidationBehaviour.cs` |
| `FluentResults` | 4.0.0 | `Result`/`Result<T>` return type for expected business failures | `src/Application/Common/Errors/CategoryErrors.cs` |
| `Ardalis.GuardClauses` | 5.0.0 | Argument guards (`Guard.Against.Null`, etc.); also the source of `NotFoundException` referenced by the exception handler | `src/Infrastructure/DependencyInjection.cs`, `src/Web/Infrastructure/ProblemDetailsExceptionHandler.cs` |
| `Microsoft.Extensions.Caching.Hybrid` | 10.9.0 | `HybridCache` — L1 in-proc + L2 Redis read-through cache used by `CachingBehavior`/`CacheInvalidationBehavior` | `src/Infrastructure/DependencyInjection.cs` |
| `Aspire.Hosting.*` (AppHost, Redis, Azure.Sql, Testing, JavaScript) | 13.5.2 | Orchestrates SQL Server + Redis containers and the Web project resource graph, for both `dotnet run` and `TestAppHost` | `src/AppHost/Program.cs`, `tests/TestAppHost/Program.cs` |
| `Aspire.StackExchange.Redis.DistributedCaching` | 13.5.2 | Redis-backed `IDistributedCache` used as HybridCache's L2 | `src/Infrastructure/DependencyInjection.cs` (`AddRedisDistributedCache`) |
| `Scalar.AspNetCore` | 2.17.1 | API reference UI served at `/scalar` (replaces Swagger UI) | `src/Web/Program.cs` |
| `OpenTelemetry.*` (Extensions.Hosting, Instrumentation.AspNetCore/Http/Runtime, Exporter.OTLP) | 1.18.0 | Tracing/metrics/logging via `ServiceDefaults` | `src/ServiceDefaults/Extensions.cs` |
| `System.IdentityModel.Tokens.Jwt` | 8.22.0 | Bearer token support for ASP.NET Core Identity | `Directory.Packages.props` |
| `Azure.Identity` / `Azure.Extensions.AspNetCore.Configuration.Secrets` | 1.21.0 / 1.5.2 | Optional Azure Key Vault config source | `src/Web/DependencyInjection.cs` (`AddKeyVaultIfConfigured`) |

### 3) Development Toolchain

| Tool | Purpose | Evidence |
|------|---------|----------|
| NUnit 4.6.1 + `NUnit3TestAdapter` 6.3.0 + `NUnit.Analyzers` | Test framework across all 4 test projects (not xUnit) | `Directory.Packages.props`, `tests/*/*.csproj` |
| Shouldly 4.3.0 | Assertion library | `Directory.Packages.props` |
| Moq 4.20.72 | Mocking | `Directory.Packages.props` |
| Respawn 7.0.0 | DB reset between functional-test runs | `tests/Application.FunctionalTests/Infrastructure/DatabaseResetter.cs` |
| `coverlet.collector` 10.0.1 | Code coverage collection | `Directory.Packages.props` |
| EditorConfig | Formatting/style rules (indent, `dotnet_*` analyzers) | `.editorconfig` |
| `Clean.Architecture.Solution.Template` 10.8.0 (`dotnet new ca-usecase`) | Scaffolds new CQRS command/query folders under `Application/Features/<Name>` | `README.md` |
| Podman (via `run-functional-tests.sh`) | Container runtime substitute for Docker, used to run Aspire-orchestrated functional tests | `run-functional-tests.sh`, `functional-tests.runsettings` |

### 4) Key Commands

```bash
dotnet build                                        # build entire solution
dotnet run --project src/AppHost                    # run app via Aspire (opens dashboard, starts SQL/Redis containers)
dotnet test                                          # run all unit/integration/functional tests (needs Docker or Podman)
./run-functional-tests.sh                            # functional tests specifically, wired for Podman socket activation
dotnet test --settings functional-tests.runsettings  # alternative: force Podman env vars via runsettings file
cd src/Application && dotnet new ca-usecase --name CreateTodoList --feature-name TodoLists --usecase-type command --return-type int
```

### 5) Environment and Config

- Config sources: `src/Web/appsettings.json` / `appsettings.Development.json`, `src/AppHost/appsettings.json` / `appsettings.Development.json`, Aspire resource references injected as connection strings/env vars at runtime (`WithReference`).
- **Discrepancy found:** `src/Web/appsettings.json` *does* contain a local LocalDB fallback connection string (`ConnectionStrings:skestockDb`), contrary to the assumption (recorded in `AGENTS.md`/`.github/copilot-instructions.md`) that "no local fallback connection strings exist." This LocalDB string only matters if `Web` is run standalone outside Aspire — Redis still would not be configured in that mode. `[TODO]` confirm whether this fallback is intentional or stale from template generation.
- No `.env.example`/`.env.template` file exists — no scan hits. Secrets (`sql-password`, `redis-password`) are modeled as Aspire `builder.AddParameter(..., secret: true)` parameters (`src/AppHost/Program.cs`), not classic env vars; Aspire resolves them via its parameter/user-secrets mechanism at run time. `[TODO]` confirm exact parameter resolution source (user-secrets vs. environment) since no `secrets.json`/`launchSettings.json` value was inspected for it.
- Deployment/runtime constraint: running `dotnet run` on `src/Web` directly (bypassing `AppHost`) has no working Redis connection and only a LocalDB SQL fallback — full local dev requires `dotnet run --project src/AppHost` with Docker or Podman available for the SQL Server/Redis containers.

### 6) Evidence

- `global.json`, `Directory.Build.props`, `Directory.Packages.props`
- `skestock.slnx`, `aspire.config.json`, `.aspire/settings.json`
- `src/AppHost/Program.cs`, `src/Web/Program.cs`, `src/Infrastructure/DependencyInjection.cs`
- `run-functional-tests.sh`, `functional-tests.runsettings`

## Extended Sections (Optional)

Not added — repo complexity does not currently warrant a full dependency taxonomy or environment matrix beyond the above.
