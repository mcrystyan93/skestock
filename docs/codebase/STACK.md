# Technology Stack

## Core Sections

### 1) Runtime Summary

| Area | Value | Evidence |
|------|-------|----------|
| Backend language/runtime | C# on .NET 10 (`net10.0`) | `Directory.Build.props`, `global.json` |
| SDK | `10.0.110`, `rollForward: latestFeature` | `global.json` |
| .NET package manager | NuGet Central Package Management | `Directory.Packages.props` |
| Frontend | Angular `^22.1.0`, TypeScript `~6.0.2`, npm `11.16.0` | `src/Client/package.json` |
| .NET solution/build | MSBuild XML solution (`skestock.slnx`) | `skestock.slnx` |
| Orchestration | .NET Aspire `13.5.2` AppHost | `src/AppHost/AppHost.csproj`, `aspire.config.json` |

### 2) Production Frameworks and Dependencies

| Dependency | Version | Role | Evidence |
|------------|---------|------|----------|
| ASP.NET Core / Web SDK | .NET 10 / `10.0.11` packages | Minimal API host | `src/Web/Web.csproj`, `src/Web/Program.cs` |
| EF Core + SQL Server Aspire integration | `10.0.11` / `13.5.2` | Persistence, migrations, SQL Server resource | `src/Infrastructure/Infrastructure.csproj`, `src/Infrastructure/Data/ApplicationDbContext.cs` |
| ASP.NET Core Identity | `10.0.11` | Guid-keyed users/roles and Identity API endpoints | `src/Infrastructure/Data/ApplicationDbContext.cs`, `src/Infrastructure/DependencyInjection.cs` |
| Mediator source generator | `3.0.2` | CQRS dispatch and pipeline behaviors; not MediatR | `Directory.Packages.props`, `src/Application/DependencyInjection.cs` |
| FluentValidation | `12.1.1` | Request validators | `src/Application/DependencyInjection.cs` |
| FluentResults | `4.0.0` | Typed expected-failure results | `Directory.Packages.props`, `src/Application/Common/Errors` |
| HybridCache/FusionCache + Redis | `10.9.0` / `2.7.2` / Aspire `13.5.2` | L1/L2 caching, tag invalidation, Redis backplane | `src/Infrastructure/DependencyInjection.cs` |
| Azure Blob/Queue clients | Aspire `13.5.3`, Blob `12.28.0` | SAS file storage and import queues | `src/Infrastructure/Storage`, `src/Infrastructure/Queues` |
| SignalR Redis backplane | `10.0.11` | Realtime server-to-client notifications | `src/Infrastructure/DependencyInjection.cs`, `src/Infrastructure/Realtime` |
| Scalar | `2.17.1` | OpenAPI reference UI at `/scalar` | `src/Web/Program.cs` |
| OpenTelemetry | `1.18.0` | Logs, metrics, traces, optional OTLP export | `src/ServiceDefaults/Extensions.cs` |
| Angular/ng-zorro/NgRx Signals | Angular `22.1`, ng-zorro `22.0.1`, NgRx `22.0.0` | SPA UI, components, signal-based client state | `src/Client/package.json` |

### 3) Development Toolchain

| Tool | Purpose | Evidence |
|------|---------|----------|
| NUnit `4.6.1`, adapter `6.3.0`, analyzers `4.14.0` | .NET tests | `Directory.Packages.props`, `tests/*/*.csproj` |
| Shouldly `4.3.0` | Assertions | `Directory.Packages.props` |
| Moq `4.20.72` | Unit-test mocks | `Directory.Packages.props` |
| Respawn `7.0.0` | SQL reset for functional tests | `tests/Application.FunctionalTests/Infrastructure/DatabaseResetter.cs` |
| Vitest `4.0.8` | Angular unit/component tests | `src/Client/package.json` |
| EditorConfig | Formatting/analyzer preferences | `.editorconfig` |
| Clean Architecture template `10.8.0` | CQRS scaffolding | `README.md`, `Directory.Packages.props` |

### 4) Key Commands

```bash
dotnet restore skestock.slnx
dotnet build
dotnet run --project src/AppHost
dotnet test
dotnet test tests/Application.UnitTests
dotnet test tests/Worker.UnitTests
dotnet test tests/Infrastructure.IntegrationTests
./run-functional-tests.sh
cd src/Client && npm ci && npm run dev
cd src/Client && npm run build
cd src/Client && npm test
```

### 5) Environment and Config

- Aspire supplies SQL Server, Redis, Azure Storage/Azurite, service discovery, and connection strings through `WithReference(...)`.
- `src/Web/appsettings.json` intentionally has no connection-string fallback; full local development must use `src/AppHost` or provide every required external connection/configuration explicitly.
- Aspire secret parameters include SQL and Redis passwords; OpenAI settings are injected by AppHost or deployment.
- Production configuration is rendered from GitHub Environment variables/secrets into a protected runtime env file; see `deploy/production.env.example`.
- Web and Worker Dockerfiles use Node 22 for the Angular build (Web image) and .NET 10 SDK/ASP.NET runtime images.

### 6) Evidence

- `global.json`
- `Directory.Build.props`
- `Directory.Packages.props`
- `skestock.slnx`
- `aspire.config.json`
- `src/AppHost/Program.cs`
- `src/Web/Dockerfile`, `src/Worker/Dockerfile`
- `src/Client/package.json`
- `.github/workflows/deploy-production.yml`
