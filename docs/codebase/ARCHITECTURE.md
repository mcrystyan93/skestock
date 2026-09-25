# Architecture

## Core Sections

### 1) Architectural Style

- Primary style: layered Clean Architecture with feature-slice CQRS, plus event-driven outbox/queue processing and SignalR realtime notifications.
- Dependency direction: `Domain <- Application <- Infrastructure` and `Domain <- Application <- Web`; `Worker` composes Application and Infrastructure; `AppHost` orchestrates resources; `Client` is independent.
- Constraints: Application uses `IApplicationDbContext`, all external adapters live in Infrastructure, and resource names come from `skestock.Shared.Services`.

### 2) System Flow

```text
HTTP endpoint group
  -> ISender.Send(command/query)
  -> ordered Mediator pipeline
  -> Application handler
  -> IApplicationDbContext / application abstraction
  -> Infrastructure adapter or SQL Server
  -> typed result or ProblemDetails response
```

For imports, a command writes domain data and an `OutboxMessage`; Web publishes the envelope to Azure Queue; Worker checks `ProcessedMessages`, dispatches the original Mediator request in a transaction, and deletes or poisons the queue message. For realtime changes, domain event handlers use `IRealtimeNotifier` and SignalR.

### 3) Layer/Module Responsibilities

| Layer | Owns | Must not own | Evidence |
|-------|------|--------------|----------|
| Domain | Entities, `BaseEntity`/`BaseAuditableEntity`, events, queue contracts | Other project references or providers | `src/Domain` |
| Application | CQRS handlers/validators, behaviors, typed errors, pagination/filtering/keyset/caching, interfaces | Concrete infrastructure | `src/Application` |
| Infrastructure | `ApplicationDbContext`, EF configurations/migrations/interceptors, Identity, Redis/cache, Blob/Queue, SignalR, AI | HTTP endpoint mapping | `src/Infrastructure` |
| Web | Composition root, endpoint groups, OpenAPI/Scalar, auth/CORS, ProblemDetails, outbox publisher | Direct EF/business logic | `src/Web` |
| Worker | Azure queue polling, scoped dispatch, idempotency, poison queues, daily schedule | HTTP | `src/Worker` |
| AppHost | SQL/Redis/Azurite/Web/Worker/Vite graph | Business logic | `src/AppHost/Program.cs` |
| Client | Angular routes, UI, HTTP services, signal stores, SignalR bridge | .NET references | `src/Client/src/app` |

### 4) Reused Patterns

| Pattern | Where | Why |
|---------|-------|-----|
| Mediator CQRS + ordered behaviors | `src/Application/DependencyInjection.cs` | Centralized logging, authorization, validation, performance, caching |
| Reflection endpoint discovery | `IEndpointGroup`, `MapEndpoints` | New endpoint groups require no central registration |
| FluentResults + typed errors | `src/Application/Common/Errors`, `ResultProblemDetailsMapper` | Stable API error codes and metadata |
| Keyset pagination/filter allowlists | `src/Application/Common/Keyset`, `Filtering` | Stable cursors, no arbitrary property expressions |
| HybridCache tag invalidation | `src/Application/Common/Caching` | Read-through cache with feature-level invalidation |
| SaveChanges interceptors | `src/Infrastructure/Data/Interceptors` | Audit fields and post-commit domain events |
| Transactional outbox + idempotent queue consumer | `src/Web/BackgroundJobs`, `src/Worker/Queues` | At-least-once async processing without losing messages |
| Client SignalStore features | `src/Client/src/app/shared`, `src/Client/src/app/features` | Reusable loading/error/collection state |

### 5) Known Architectural Risks

- Cookie authentication is present, but application requests currently do not use role-bearing `[Authorize]` attributes; `Users` exposes Identity API endpoints with the group authorization disabled.
- Antiforgery registration, token endpoint, and middleware are commented out.
- Outbox and Azure Queue delivery is intentionally at-least-once; consumers must remain idempotent.
- `ApplicationDbContextInitialiser` performs development/production startup migration and seed work; startup availability depends on SQL Server.

### 6) Evidence

- `src/Application/DependencyInjection.cs`
- `src/Web/Program.cs`
- `src/Web/Infrastructure/WebApplicationExtensions.cs`
- `src/Infrastructure/Data/ApplicationDbContext.cs`
- `src/Web/BackgroundJobs/OutboxPublisherService.cs`
- `src/Worker/Queues/QueueProcessingService.cs`
- `src/Client/src/app/core/signalr/signalr-bridge.ts`
