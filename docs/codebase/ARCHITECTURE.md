# Architecture

## Core Sections (Required)

### 1) Architectural Style

- Primary style: **Layered Clean Architecture** (Jason Taylor `Clean.Architecture.Solution.Template` 10.8.0) with a **CQRS/feature-slice** organization inside the Application layer, orchestrated end-to-end by **.NET Aspire**, plus a **transactional outbox + queue worker** subsystem for async processing and a **SignalR realtime** layer for push notifications to the Angular client.
- Why this classification: dependency direction is strictly inward (`Domain` ← `Application` ← `Infrastructure`/`Web`), confirmed by each project's `ProjectReference`s. `Application` never references an EF Core provider package directly, only the `IApplicationDbContext` abstraction (implemented by `ApplicationDbContext` in `Infrastructure`). Within `Application`, code is organized per business feature (`Features/Categories`, `Features/GoodsReceipts`, `Features/Items`, `Features/Locations`, `Features/SchoolClasses`, `Features/Statistics`, `Features/Stock`, `Features/StockBatches`), each use case a Command/Query + Handler + Validator triple dispatched through **Mediator** (source-generator library, not MediatR).
- Primary constraints: (1) `Application` must never reference an EF Core *provider* package; (2) cross-cutting concerns (logging, auth, validation, perf, caching, cache invalidation) are implemented as ordered Mediator pipeline `Behaviours`; (3) Aspire resource names (`Services.*` constants in `Shared`) must never be hardcoded as strings; (4) async workflows (goods-receipt/category/item imports) go through a transactional outbox rather than direct queue sends inside a handler, to keep the DB write and the queue publish consistent.

### 2) System Flow

```text
HTTP request
  -> Web/Endpoints/<Feature>.cs (IEndpointGroup static handler)
  -> ISender.Send(command/query) — dispatched through the Mediator pipeline:
       LoggingBehaviour -> UnhandledExceptionBehaviour -> AuthorizationBehaviour
       -> ValidationBehaviour -> PerformanceBehaviour -> CachingBehavior -> CacheInvalidationBehavior
  -> Features/<Feature>/{Commands|Queries}/<UseCase>/<UseCase>Handler.cs
       (queries: IApplicationDbContext -> EF Core LINQ, via Filtering/Keyset helpers)
       (commands: IApplicationDbContext.Add/Update + SaveChangesAsync, wrapped by SaveChanges interceptors)
  -> Result<T> (FluentResults) returned up through the pipeline
  -> Endpoint checks result.IsFailed -> ToProblemHttpResult() OR TypedResults.Ok/Created(...)
  -> HTTP response (ProblemDetails w/ ApiErrorContract extension, or typed JSON)
```

Additional flows:
- **Thrown-exception path** (parallel to `Result<T>`): `ValidationException`/`NotFoundException`/`UnauthorizedAccessException`/`ForbiddenAccessException` are caught globally by `Web/Infrastructure/ProblemDetailsExceptionHandler.cs` and mapped to `ProblemDetails` directly.
- **Startup flow** (`src/Web/Program.cs`): `AddServiceDefaults()` → `AddKeyVaultIfConfigured()` → `AddApplicationServices()` → `AddInfrastructureServices()` → `AddWebAuthenticationServices()` → `AddWebServices()` → (dev only) `InitialiseDatabaseAsync()` else `UseHsts()` → HTTPS redirect → CORS (explicit origin allowlist, credentials) → static files → OpenAPI/Scalar → exception handler → `MapEndpoints(assembly)`.
- **Domain event flow**: `BaseEntity.AddDomainEvent(...)` queues events; `DispatchDomainEventsInterceptor` dispatches them around `SaveChangesAsync`; `AuditableEntityInterceptor` stamps audit fields on `BaseAuditableEntity` instances.
- **Transactional outbox + async import flow** (goods receipts, category imports, item imports all follow this shape):
  1. A command creates an import/batch entity and raises a `*CreatedEvent`.
  2. Domain-event dispatch adds an `OutboxMessage` row in the same unit of work (type, JSON payload, originating user, target queue name from `Services.*Queue`).
  3. `Web/BackgroundJobs/OutboxPublisherService.cs` polls periodically, sends a bounded batch of pending messages, tracks retry/error state, gives up after a retry cap.
  4. An Azure Storage Queue sender serializes/base64-encodes the envelope and creates the queue if needed.
  5. `src/Worker`'s `CategoryImportBatchQueueProcessingService` / `GoodsReceiptImportQueueProcessingService` / `ItemImportQueueProcessingService` receive messages with a visibility timeout, check `ProcessedMessages` for idempotency, dispatch the deserialized request through Mediator, and move permanent/exhausted failures to a poison queue.
  - Delivery is **at-least-once**; consumers must stay idempotent (poison-queue + `ProcessedMessages` dedup is the mitigation).
- **Realtime flow**: Application-layer `IRealtimeNotifier` (implemented by `Infrastructure/Realtime/SignalRRealtimeNotifier.cs`) pushes notifications through `AppHub` (SignalR) to Angular clients; the client subscribes via `src/Client/src/app/core/signalr/*` (group manager, event map, `signalr-bridge.ts`) and routes events into feature `signalStore`s (e.g. `stock-collection.feature.ts`, `category-collection.feature.ts`).
- **Storage flow**: file uploads are client-direct SAS flows — `RequestUploadCommand` creates a pending `FileMetadata` row and returns an upload SAS; `ConfirmUploadCommand` verifies the blob and marks it completed. Web never proxies file bytes.

### 3) Layer/Module Responsibilities

| Layer or module | Owns | Must not own | Evidence |
|-----------------|------|--------------|----------|
| `Domain` | Entities (`Category`, `CategoryIcon`, `CategoryImportBatch(+File)`, `Item`, `ItemImportBatch(+File)`, `ImportBatchHistory`, `Location`, `SchoolClass`, `ClassBalance`, `ClassItemStockVisibility`, `GoodsReceipt(Import/Line)`, `StockBatch`, `StockTransaction`, `FileMetadata`, `UserProfile`), `BaseEntity`/`BaseAuditableEntity`, domain events, queue contracts (`OutboxMessage`, `MessageEnvelope`, `ProcessedMessage`) | Any dependency on other layers, persistence/HTTP concerns | `src/Domain/Entities/*`, `src/Domain/Queues/*` |
| `Application` | Mediator commands/queries + handlers, FluentValidation validators, pipeline `Behaviours`, `Result`/`Error` catalog, filtering/keyset/caching helpers, storage commands, queue interfaces, document-extraction interfaces, `IApplicationDbContext`/`IIdentityService`/`IUser`/`IRealtimeNotifier` | EF Core provider references, ASP.NET Core types, concrete Identity/Blob/Queue/SignalR implementation | `src/Application/Common/Interfaces/*` |
| `Infrastructure` | `ApplicationDbContext`, EF `IEntityTypeConfiguration<T>` per entity, SaveChanges interceptors, `ApplicationUser`/`IdentityService`, HybridCache + Redis, Azure Blob (`AzureBlobStorageService`, `AzureBlobCorsInitializer`) and Queue adapters, `SignalRRealtimeNotifier`/`AppHub`, `OpenAiDocumentExtractionClient` | Mediator/CQRS logic, HTTP endpoint definitions | `src/Infrastructure/{Data,Identity,Storage,Realtime,AI}/*` |
| `Web` | `IEndpointGroup` classes (Categories, CategoryImportBatches, GoodsReceipts, Items, ItemImportBatches, Locations, SchoolClasses, Statistics, Stock, StockBatches, Storage, Users), OpenAPI/Scalar wiring, `ProblemDetailsExceptionHandler`, `OutboxPublisherService` (background job), `CurrentUser : IUser` | Direct EF Core access, business/validation logic | `src/Web/Endpoints/*.cs`, `src/Web/BackgroundJobs/OutboxPublisherService.cs` |
| `Worker` | Queue-consuming `BackgroundService`s per import type, idempotency checks, poison-queue routing | HTTP endpoints, ad hoc processing outside the Mediator dispatch path (`Worker.cs` template leftover excluded) | `src/Worker/Queues/*.cs` |
| `Shared` | `Services` static class — Aspire/service resource-name string constants (database, cache, storage/queue names) | Business logic, EF/HTTP code | `src/Shared/Services.cs` |
| `AppHost` | Aspire resource graph: SQL Server, Redis, Azurite (blob+queue, ports 10000/10001/10002), Web, Worker, Vite frontend, dashboard shortcut to `/scalar` | Application/business logic | `src/AppHost/Program.cs` |
| `ServiceDefaults` | `AddServiceDefaults()` (OpenTelemetry, health checks, service discovery, HTTP resilience) | Feature-specific logic | `src/ServiceDefaults/Extensions.cs` |
| `Client` | Angular routed features (`categories`, `class-analytics`, `home`, `items`, `login`, `school-classes`), shared HTTP services/`signalStore`s, core (auth, layouts, SignalR bridge, theme) | Anything needing a .NET project reference | `src/Client/src/app/*` |

### 4) Reused Patterns

| Pattern | Where found | Why it exists |
|---------|-------------|-----------------|
| CQRS + Mediator pipeline behaviours | `src/Application/Common/Behaviours/*` | Centralizes logging, auth, validation, perf timing, caching/invalidation without per-handler boilerplate; order is load-bearing |
| Reflection-based endpoint auto-discovery | `IEndpointGroup` + `WebApplicationExtensions.MapEndpoints(assembly)` | New `Web/Endpoints/*.cs` classes are picked up automatically; derives OpenAPI `operationId` from the handler method name |
| `Result<T>` (FluentResults) + typed `Error` classes | `src/Application/Common/Errors/*Errors.cs`, `ResultProblemDetailsMapper.cs` | Lets handlers return "expected" business failures as data, carrying a stable `Code` via `ApiErrorContract` for frontend localization |
| Keyset (cursor) pagination + column filtering + tag-based HybridCache | `src/Application/Common/{Keyset,Filtering,Caching}/*` | Reused across every paginated `GetAll<Feature>` query (Categories, GoodsReceipts, Items, Locations, SchoolClasses, StockBatches, plus the import-batch list queries) |
| Transactional outbox + queue worker | `src/Domain/Queues/*`, `Web/BackgroundJobs/OutboxPublisherService.cs`, `src/Worker/Queues/*` | Keeps the DB write and the async queue publish atomic; decouples slow/at-least-once processing (goods-receipt/category/item imports) from the request/response cycle |
| SaveChanges interceptors for audit + domain events | `src/Infrastructure/Data/Interceptors/{AuditableEntityInterceptor,DispatchDomainEventsInterceptor}.cs` | Centralizes audit stamping and domain-event dispatch |
| Client-direct SAS blob upload | `Application/Features` storage commands (`RequestUploadCommand`/`ConfirmUploadCommand`), `AzureBlobStorageService` | Avoids proxying file bytes through Web |
| Central Package Management | `Directory.Packages.props` | Single source of truth for every NuGet version |
| NgRx SignalStore (`signalStoreFeature`) + SignalR bridge | `src/Client/src/app/shared/*/services/*.feature.ts`, `src/Client/src/app/core/signalr/*` | Reactive client-side state kept in sync with server push events without classic NgRx actions/reducers/effects |

### 5) Known Architectural Risks

- **Two parallel error-to-HTTP mechanisms** (`Result<T>`/`Error` vs. thrown exceptions caught by `ProblemDetailsExceptionHandler`) coexist by design; only the `Result` path attaches the richer `ApiErrorContract` the frontend depends on. See `CONCERNS.md`.
- **`NotFoundException` still referenced but not the dominant "not found" path** — most current handlers use `Result<T>`/`Error` for not-found cases instead. `[TODO]` re-verify whether any newer handler (e.g. import-batch confirm/process flows) now throws it, given the significant feature growth since the last pass.
- **Outbox delivery is at-least-once, not exactly-once**: `OutboxPublisherService` rows are not claimed atomically, so a successful send can be resent if state persistence fails partway; Worker-side idempotency (`ProcessedMessages`) is the correctness guarantee, not the publisher.
- **Antiforgery enforcement is still disabled**: `app.UseAntiforgeryValidation()` in `src/Web/Program.cs` and the `AddAntiforgery` registration in `src/Web/DependencyInjection.cs` remain commented out, while the Angular client already sends matching `XSRF-TOKEN`/`X-XSRF-TOKEN` names. Confirmed still true on this pass.
- **LocalDB connection-string fallback in `appsettings.json`** persists with no Redis/Azurite equivalent — still a latent trap for a developer trying `dotnet run` on `Web` directly.

### 6) Evidence

- `src/Web/Program.cs`, `src/Application/DependencyInjection.cs`, `src/Infrastructure/DependencyInjection.cs`, `src/AppHost/Program.cs`, `src/Worker/Program.cs`
- `src/Application/Common/Behaviours/*.cs`
- `src/Web/Infrastructure/{IEndpointGroup,WebApplicationExtensions,ProblemDetailsExceptionHandler,ResultProblemDetailsMapper}.cs`
- `src/Web/BackgroundJobs/OutboxPublisherService.cs`, `src/Worker/Queues/*.cs`, `src/Domain/Queues/OutboxMessage.cs`
- `src/Infrastructure/Realtime/{AppHub,SignalRRealtimeNotifier}.cs`, `src/Application/Common/Interfaces/IRealtimeNotifier.cs`
- `src/Application/Features/{Categories,GoodsReceipts,Items,Stock,StockBatches}/*`

## Extended Sections (Optional)

Not added — the outbox/queue/realtime topology above already covers the async event flow; a full anti-pattern catalog is in `CONCERNS.md` given repo size.
