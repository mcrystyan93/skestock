# Technology Stack

## Core Sections (Required)

### 1) Runtime Summary

| Area | Value | Evidence |
|------|-------|----------|
| Primary language | C# 14 / .NET 10 | `Directory.Build.props` (`<TargetFramework>net10.0</TargetFramework>`) |
| Runtime + version | .NET SDK `10.0.110`, roll-forward `latestFeature` | `global.json` |
| Package manager | NuGet with Central Package Management (CPM) | `Directory.Packages.props` (`ManagePackageVersionsCentrally=true`) |
| Module/build system | MSBuild, `.slnx` solution (not classic `.sln`) | `skestock.slnx` |
| Orchestration | .NET Aspire 13.5.2 (AppHost model, not Docker Compose/K8s for local dev; Docker Compose is used only for production deployment, see `deploy/`) | `src/AppHost/Program.cs`, `aspire.config.json` |
| Frontend | Angular ^22.1.0, TypeScript ~6.0.2, npm (`src/Client`, `Client.esproj`, run via `AddViteApp(...).WithNpm()` from AppHost) | `src/Client/package.json`, `src/AppHost/Program.cs` |

### 2) Production Frameworks and Dependencies

Only high-impact production dependencies listed (see `Directory.Packages.props` for the full, centrally-managed list).

| Dependency | Version | Role in system | Evidence |
|------------|---------|-----------------|----------|
| ASP.NET Core / `Microsoft.AspNetCore.OpenApi` | .NET 10 / 10.0.11 | Minimal API host (`src/Web`), OpenAPI generation | `src/Web/Program.cs` |
| `Microsoft.EntityFrameworkCore` (+ `.Design`, SqlServer via Aspire) | 10.0.11 | ORM, migrations, `ApplicationDbContext` | `src/Infrastructure/Data/ApplicationDbContext.cs` |
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | 10.0.11 | User/role identity store (`IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>`) | `src/Infrastructure/Data/ApplicationDbContext.cs` |
| `Mediator.Abstractions` / `Mediator.SourceGenerator` | 3.0.2 | CQRS mediator (source-generator based, **not MediatR**) | `src/Application/DependencyInjection.cs` |
| `FluentValidation.DependencyInjectionExtensions` | 12.1.1 | Request validators, wired via `ValidationBehaviour` | `src/Application/Common/Behaviours/ValidationBehaviour.cs` |
| `FluentResults` | 4.0.0 | `Result`/`Result<T>` return type for expected business failures | `src/Application/Common/Errors/*Errors.cs` |
| `Ardalis.GuardClauses` | 5.0.0 | Argument guards; source of `NotFoundException` referenced by the exception handler | `src/Infrastructure/DependencyInjection.cs` |
| `Microsoft.Extensions.Caching.Hybrid` | 10.9.0 | `HybridCache` — L1 in-proc + L2 Redis read-through cache | `src/Infrastructure/DependencyInjection.cs` |
| `Aspire.Hosting.*` (AppHost, Redis, Azure.Sql, Storage, Testing, JavaScript) | 13.5.2 | Orchestrates SQL Server, Redis, Azurite, Web, Worker, and the Vite frontend | `src/AppHost/Program.cs`, `tests/TestAppHost/Program.cs` |
| `Aspire.Azure.Storage.Blobs` / `Aspire.Azure.Storage.Queues` | 13.5.3 | Azure Blob (file uploads) and Azure Storage Queue (async import/outbox processing) client registration | `src/Infrastructure/DependencyInjection.cs`, `src/AppHost/Program.cs` |
| `Azure.Storage.Blobs` | 12.28.0 | Blob container access (`app-files` container, SAS-based client-direct uploads) | `src/Infrastructure/Storage/AzureBlobStorageService.cs` |
| `Microsoft.AspNetCore.SignalR.StackExchangeRedis` | 10.0.11 | SignalR backplane over Redis for the realtime hub (`AppHub`) | `src/Infrastructure/DependencyInjection.cs` |
| `Aspire.StackExchange.Redis.DistributedCaching` | 13.5.2 | Redis-backed `IDistributedCache` used as HybridCache's L2 | `src/Infrastructure/DependencyInjection.cs` |
| Document extraction (OpenAI-based client) | n/a (custom `Infrastructure/AI`) | Extracts structured data (e.g. goods receipt / import line items) from uploaded documents | `src/Infrastructure/AI/OpenAiDocumentExtractionClient.cs` |
| `Scalar.AspNetCore` | 2.17.1 | API reference UI served at `/scalar` (replaces Swagger UI) | `src/Web/Program.cs` |
| `OpenTelemetry.*` (Extensions.Hosting, Instrumentation.AspNetCore/Http/Runtime, Exporter.OTLP) | 1.18.0 | Tracing/metrics/logging via `ServiceDefaults` | `src/ServiceDefaults/Extensions.cs` |
| `System.IdentityModel.Tokens.Jwt` | 8.22.0 | Token support for ASP.NET Core Identity bearer scheme | `Directory.Packages.props` |
| `Azure.Identity` / `Azure.Extensions.AspNetCore.Configuration.Secrets` | 1.21.0 / 1.5.2 | Optional Azure Key Vault config source | `src/Web/DependencyInjection.cs` (`AddKeyVaultIfConfigured`) |
| Angular ^22.1.0 (CLI/build ^22.1.6), ng-zorro-antd ^22.0.1, Tailwind/PostCSS 4, NgRx Signals/Operators 22, `@microsoft/signalr` 10.0.11, RxJS 7.8 | — | SPA frontend, UI kit, styling, client-side state (`signalStore`), realtime client | `src/Client/package.json` |

### 3) Development Toolchain

| Tool | Purpose | Evidence |
|------|---------|----------|
| NUnit 4.6.1 + `NUnit3TestAdapter` 6.3.0 + `NUnit.Analyzers` | Test framework across all .NET test projects (not xUnit) | `Directory.Packages.props`, `tests/*/*.csproj` |
| Shouldly 4.3.0 | Assertion library | `Directory.Packages.props` |
| Moq 4.20.72 | Mocking | `Directory.Packages.props` |
| Respawn 7.0.0 | DB reset between functional-test runs | `tests/Application.FunctionalTests/Infrastructure/DatabaseResetter.cs` |
| `coverlet.collector` 10.0.1 | Code coverage collection | `Directory.Packages.props` |
| Vitest 4 | Client (Angular) unit/component tests, run via `npm test` | `src/Client/package.json` |
| EditorConfig | Formatting/style rules (indent, `dotnet_*` analyzers) | `.editorconfig` |
| `Clean.Architecture.Solution.Template` 10.8.0 (`dotnet new ca-usecase`) | Scaffolds new CQRS command/query folders under `Application/Features/<Name>` | `README.md` |
| Podman (via `run-functional-tests.sh`) | Container runtime substitute for Docker, used to run Aspire-orchestrated functional tests | `run-functional-tests.sh`, `functional-tests.runsettings` |
| GitHub Actions (`deploy-production.yml`) | `workflow_dispatch`-triggered production deployment pipeline (build, containerize, deploy over Cloudflare tunnel/quadlet) — **not** a build/test gate on push or PR | `.github/workflows/deploy-production.yml` |

### 4) Key Commands

```bash
dotnet build                                        # build entire solution
dotnet run --project src/AppHost                    # run app via Aspire (dashboard, SQL/Redis/Azurite containers, Web/Worker/frontend)
dotnet test                                          # run all unit/integration/functional tests (needs Docker or Podman)
./run-functional-tests.sh                            # functional tests specifically, wired for Podman socket activation
dotnet test --settings functional-tests.runsettings  # alternative: force Podman env vars via runsettings file
cd src/Client && npm install && npm run dev          # frontend dev server (also launched by AppHost via Vite)
cd src/Client && npm run build                       # frontend production build
cd src/Client && npm test                            # frontend Vitest suite
cd src/Application && dotnet new ca-usecase --name CreateTodoList --feature-name TodoLists --usecase-type command --return-type int
```

### 5) Environment and Config

- Config sources: `src/Web/appsettings.json` / `appsettings.Development.json`, `src/AppHost/appsettings.json` / `appsettings.Development.json`, `src/Worker/appsettings.json`, Aspire resource references injected as connection strings/env vars at runtime (`WithReference`).
- `src/Web/appsettings.json` still contains a local LocalDB fallback connection string (`ConnectionStrings:skestockDb`) with no equivalent Redis fallback — confirmed still present. This only matters if `Web` is run standalone outside Aspire. `[TODO]` confirm whether this fallback is intentional or stale from template generation (see `CONCERNS.md`).
- No `.env.example`/`.env.template` file exists. Secrets (`sql-password`, `redis-password`, storage/queue keys) are modeled as Aspire `builder.AddParameter(..., secret: true)` parameters (`src/AppHost/Program.cs`), not classic env vars. `[TODO]` confirm exact parameter resolution source (user-secrets vs. environment).
- Production deployment (separate from local Aspire dev flow) uses Docker Compose/Podman quadlet files under `deploy/quadlet/` (`skestock-web`, `skestock-worker`, `skestock-cache`, `skestock-db`, `skestock-storage` containers/volumes) plus `deploy/deploy.sh` and `deploy/production.env.example` (this **is** the one `.env.example`-style file in the repo, but it is deployment-specific, not local-dev). `Dockerfile`s now exist for `src/Web` and `src/Worker`.
- Deployment/runtime constraint: running `dotnet run` on `src/Web` directly (bypassing `AppHost`) has no working Redis/Azurite connection and only a LocalDB SQL fallback — full local dev requires `dotnet run --project src/AppHost` with Docker or Podman available.

### 6) Evidence

- `global.json`, `Directory.Build.props`, `Directory.Packages.props`
- `skestock.slnx`, `aspire.config.json`, `.aspire/settings.json`
- `src/AppHost/Program.cs`, `src/Web/Program.cs`, `src/Infrastructure/DependencyInjection.cs`
- `src/Client/package.json`
- `deploy/`, `.github/workflows/deploy-production.yml`, `src/Web/Dockerfile`, `src/Worker/Dockerfile`

## Extended Sections (Optional)

Not added — repo complexity does not currently warrant a full dependency taxonomy or environment matrix beyond the above.
