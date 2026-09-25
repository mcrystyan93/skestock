# Codebase Structure

## Core Sections

### 1) Top-Level Map

| Path | Purpose | Evidence |
|------|---------|----------|
| `src/Domain` | Pure domain entities, enums, value objects, events, queue contracts | `src/Domain/Domain.csproj` |
| `src/Application` | Feature-slice CQRS, validators, behaviors, filtering/keyset/caching, storage/queue abstractions | `src/Application/Application.csproj` |
| `src/Infrastructure` | EF Core, Identity, Redis/cache, Azure adapters, SignalR, OpenAI extraction | `src/Infrastructure/DependencyInjection.cs` |
| `src/Web` | ASP.NET Core composition root, endpoints, auth, OpenAPI, outbox publisher | `src/Web/Program.cs` |
| `src/Worker` | Non-HTTP queue consumers and daily scheduled service | `src/Worker/Program.cs` |
| `src/AppHost` | Aspire resource graph and local/publish orchestration | `src/AppHost/Program.cs` |
| `src/ServiceDefaults` | Shared health, telemetry, service discovery, resilience | `src/ServiceDefaults/Extensions.cs` |
| `src/Shared` | Resource-name constants and small cross-project helpers | `src/Shared/Services.cs` |
| `src/Client` | Independent Angular SPA, launched by Aspire as a Vite app | `src/Client/package.json`, `src/AppHost/Program.cs` |
| `tests` | Unit, functional, integration, and supporting Aspire hosts | `skestock.slnx` |

### 2) Entry Points

- Orchestrated runtime: `src/AppHost/Program.cs`.
- HTTP API/static host: `src/Web/Program.cs`.
- Queue/scheduled worker: `src/Worker/Program.cs`.
- Angular bootstrap: `src/Client/src/main.ts`.
- Functional/integration Aspire host: `tests/TestAppHost/Program.cs`.
- Production deployment: `.github/workflows/deploy-production.yml` and `deploy/deploy.sh`.

### 3) Module Boundaries

| Boundary | Belongs here | Must not be here |
|----------|--------------|------------------|
| Domain | Types and invariants that do not need infrastructure | EF provider, HTTP, external services |
| Application | Use cases, business validation, abstractions, DTOs | Concrete SQL/Redis/Azure adapters |
| Infrastructure | Implementations of Application abstractions | Endpoint routing or use-case orchestration |
| Web | HTTP mapping, composition, auth middleware, background outbox publishing | Direct business/data queries |
| Worker | Queue transport lifecycle and scoped Mediator dispatch | HTTP endpoints or direct feature logic |
| Client | Routes, UI, HTTP clients, SignalStore state, SignalR client | .NET project references |

### 4) Naming and Organization Rules

- .NET source uses PascalCase file/type names and file-scoped namespaces.
- Application use cases live under `Features/<Feature>/{Commands|Queries}/<UseCase>/`.
- A typical use case contains `<UseCase>Command|Query.cs`, `Handler.cs`, and `Validator.cs`.
- Paginated features additionally have `CacheConstants.cs`, filter configuration, and sort configuration.
- Web endpoint groups are PascalCase classes under `src/Web/Endpoints`.
- Angular uses `src/app/core`, `src/app/features/<feature>`, and `src/app/shared/<feature>`, with `@ske/...` TypeScript aliases.
- Generated/build output (`bin`, `obj`, `dist`, `.angular`, graph artifacts) is not source convention.

### 5) Evidence

- `skestock.slnx`
- `Directory.Build.props`, `Directory.Packages.props`, `global.json`
- `src/AppHost/Program.cs`
- `src/Web/Program.cs`
- `src/Worker/Program.cs`
- `src/Application/Features`
- `src/Client/tsconfig.json`
- `tests/TestAppHost/Program.cs`
