# External Integrations

## Core Sections (Required)

### 1) Integration Inventory

| System | Type | Purpose | Auth model | Criticality | Evidence |
|--------|------|---------|------------|--------------|----------|
| SQL Server (containerized via Aspire, `Services.DatabaseServer`) | Relational DB | Primary datastore for all domain entities + ASP.NET Core Identity tables | Container-level password parameter (`sql-password`) | High | `src/AppHost/Program.cs`, `src/Infrastructure/Data/ApplicationDbContext.cs` |
| Redis (containerized via Aspire, `Services.Cache`) | Distributed cache + SignalR backplane | L2 backing store for `HybridCache`; also backs the SignalR realtime hub across instances (`Microsoft.AspNetCore.SignalR.StackExchangeRedis`) | Container-level password parameter (`redis-password`) | Medium-High (caching degrades gracefully; SignalR backplane matters more once scaled beyond one instance) | `src/AppHost/Program.cs`, `src/Infrastructure/DependencyInjection.cs` |
| Azurite (Azure Storage emulator via Aspire, persistent, ports 10000/10001/10002) | Blob + Queue storage emulator | Local/dev stand-in for Azure Blob Storage (file uploads) and Azure Storage Queues (async import/outbox processing) | Emulator connection string | High (imports and file uploads depend on it) | `src/AppHost/Program.cs` |
| Azure Blob Storage (`app-files` container) | Object storage | Client-direct SAS uploads for imported documents/attachments; `RequestUploadCommand` issues an upload SAS, `ConfirmUploadCommand` verifies the blob | SAS tokens generated server-side; container CORS configured via `AzureBlobCorsInitializer` | High | `src/Infrastructure/Storage/{AzureBlobStorageService,AzureBlobCorsInitializer}.cs` |
| Azure Storage Queues | Message queue | Transport for the transactional-outbox-driven async import flows (goods receipts, category imports, item imports) between `Web`'s `OutboxPublisherService` and `Worker`'s queue-processing services | Storage connection string (Aspire-managed) | High for the import features; app remains usable for other features if this is down | `src/Web/BackgroundJobs/OutboxPublisherService.cs`, `src/Worker/Queues/*.cs`, `src/Domain/Queues/OutboxMessage.cs` |
| SignalR (`AppHub`) | Realtime push | Server → client notifications (e.g. import batch progress, stock changes) consumed by the Angular client's `signalr-bridge.ts` and routed into feature `signalStore`s | Cookie/bearer auth (same as REST API) | Medium | `src/Infrastructure/Realtime/{AppHub,SignalRRealtimeNotifier}.cs`, `src/Client/src/app/core/signalr/*` |
| Document extraction (OpenAI-backed) | AI/LLM API | Extracts structured line-item data from uploaded documents (e.g. goods receipt imports) | API key configured via app settings/secrets | Medium — feature-specific, not required for core CRUD | `src/Infrastructure/AI/OpenAiDocumentExtractionClient.cs` |
| ASP.NET Core Identity (self-hosted, same SQL Server) | Auth/identity provider | User accounts, Guid-keyed roles, cookie + bearer authentication | Application cookie default; bearer for non-browser clients | High | `src/Infrastructure/Identity/{ApplicationUser,IdentityService}.cs` |
| Azure Key Vault (optional) | Secrets/config source | Conditionally adds Key Vault as a configuration provider | Azure Identity | Low/optional | `src/Web/DependencyInjection.cs` (`AddKeyVaultIfConfigured`) |
| OpenTelemetry OTLP exporter | Observability/telemetry sink | Traces/metrics/logs export (destination configured via standard OTEL env vars) | N/A | Medium | `src/ServiceDefaults/Extensions.cs` |
| Scalar (`/scalar`) | API documentation UI | Interactive OpenAPI reference; also the Aspire dashboard's shortcut URL for the Web resource | N/A | Low | `src/Web/Program.cs` |
| Cloudflare Tunnel (`cloudflared`) | Ingress/edge | Used only by the production deployment pipeline (`deploy-production.yml`, `deploy/`), not local dev | N/A | Low for dev, High for prod availability | `.github/workflows/deploy-production.yml` |

### 2) Data Stores

| Store | Role | Access layer | Key risk | Evidence |
|-------|------|---------------|----------|----------|
| SQL Server (`skestockDb`) | System of record for all Domain entities + Identity | `ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>, IApplicationDbContext` | `src/Web/appsettings.json` still has a LocalDB fallback connection string with no Redis/Azurite equivalent | `src/Infrastructure/Data/ApplicationDbContext.cs`, `src/Web/appsettings.json` |
| Redis | HybridCache L2 + SignalR backplane | `HybridCache` injected into `CachingBehavior`/`CacheInvalidationBehavior`; SignalR wired via `AddStackExchangeRedis` | Cache invalidation is tag-based and lazy (`RemoveByTagAsync` marks a watermark, doesn't physically evict) | `src/Application/Common/Behaviours/CachingBehavior.cs`, `src/Infrastructure/DependencyInjection.cs` |
| Azurite Blob container (`app-files`) | Uploaded document/attachment storage | `AzureBlobStorageService`, accessed only through Application storage commands | Delivery is client-direct — a client that never calls `ConfirmUploadCommand` leaves an orphaned pending `FileMetadata` row | `src/Infrastructure/Storage/AzureBlobStorageService.cs` |
| Azurite Queue storage | Outbox message transport | `AzureQueueSender` (Infrastructure) / Worker's `*QueueProcessingService`s | At-least-once delivery — consumers must be idempotent (mitigated via `ProcessedMessages` + poison queues) | `src/Worker/Queues/*.cs`, `src/Domain/Queues/{OutboxMessage,ProcessedMessage}.cs` |

### 3) Secrets and Credentials Handling

- Credential sources: Aspire secret **parameters** (`builder.AddParameter("sql-password", secret: true)`, `"redis-password"`, plus Azure Storage/queue connection strings) rather than plain environment variables. `[TODO]` confirm exact resolution path (user-secrets vs. environment) — not independently re-verified this pass.
- Production deployment secrets: `deploy/production.env.example` documents the required env vars for the Docker Compose/Podman quadlet-based production deployment (separate from local Aspire dev).
- Hardcoding check: no hardcoded passwords/API keys found in scanned source; the LocalDB connection string in `src/Web/appsettings.json` uses Windows Integrated Auth (`Trusted_Connection=True`), not a stored password.
- Document extraction (OpenAI) API key: `[TODO]` confirm exact configuration key/secret source in `Infrastructure/AI/OpenAiDocumentExtractionClient.cs` — not independently re-verified this pass.

### 4) Reliability and Failure Behavior

- Retry/backoff: document extraction HTTP clients configure standard resilience (exponential-backoff retries, per-attempt/total timeouts, circuit breaker) in `src/Infrastructure/DependencyInjection.cs`; `ServiceDefaults` applies standard resilience defaults to `HttpClient` generally.
- Outbox retry policy: `OutboxPublisherService` polls periodically, sends a bounded batch, and stops retrying a given message after a retry cap — permanent failures are surfaced via message state rather than retried forever.
- Worker retry/poison-queue: each `*QueueProcessingService` uses a visibility timeout, retries a bounded number of times, and routes exhausted/poison messages to a dedicated poison queue instead of blocking the main queue.
- Timeout policy: `FunctionalTestSetup.OneTimeSetUp` uses an explicit 90-second timeout waiting for `Services.Database`/`Services.Cache` health when booting the test Aspire host.
- Circuit-breaker/fallback: Aspire's `WaitFor(...)` dependencies on the `webapi`/`worker` resources delay startup until SQL Server/Redis/Azurite report healthy — a startup-order guarantee, not a request-time fallback.

### 5) Observability for Integrations

- `ServiceDefaults.AddServiceDefaults()` wires OpenTelemetry instrumentation for ASP.NET Core, HTTP client, and .NET runtime metrics across Web and Worker.
- `[TODO]` — no EF Core-specific OpenTelemetry instrumentation package confirmed in `Directory.Packages.props`; DB query-level tracing may rely on ASP.NET Core span auto-instrumentation only.
- `[TODO]` — no explicit Azure Storage Queue/Blob or SignalR-specific tracing instrumentation confirmed beyond generic HTTP client instrumentation.

### 6) Evidence

- `src/AppHost/Program.cs` (resource graph: SQL Server, Redis, Azurite, secret parameters, health-wait chain)
- `src/Infrastructure/DependencyInjection.cs` (SQL Server, Redis, HybridCache, SignalR backplane, Blob/Queue, Identity wiring)
- `src/Infrastructure/{Storage,Realtime,AI}/*.cs`
- `src/Web/BackgroundJobs/OutboxPublisherService.cs`, `src/Worker/Queues/*.cs`
- `deploy/`, `.github/workflows/deploy-production.yml`

## Extended Sections (Optional)

Not added — no other third-party API integrations beyond the datastore/cache/storage/queue/realtime/AI-extraction sinks documented above.
