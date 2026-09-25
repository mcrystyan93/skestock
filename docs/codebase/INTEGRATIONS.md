# External Integrations

## Core Sections

### 1) Integration Inventory

| System | Type | Purpose | Auth/config | Criticality | Evidence |
|--------|------|---------|-------------|-------------|----------|
| SQL Server | Relational DB | Domain, audit, outbox, scheduled-run, and Identity data | Aspire secret SQL password/connection string | High | `src/AppHost/Program.cs`, `src/Infrastructure/Data/ApplicationDbContext.cs` |
| Redis | Cache/backplane/lock | HybridCache L2, FusionCache backplane, SignalR backplane, scheduled-job lock | Aspire password/connection string | High | `src/Infrastructure/DependencyInjection.cs`, `src/Infrastructure/Distributed/RedisDistributedLock.cs` |
| Azure Blob/Azurite | Object storage | Client-direct SAS uploads/downloads; `app-files` container | Aspire/Azurite connection string | High for storage/imports | `src/Infrastructure/Storage/AzureBlobStorageService.cs`, `src/AppHost/Program.cs` |
| Azure Storage Queue/Azurite | Queue | Outbox transport for category/item/goods-receipt imports | Aspire/Azurite connection string | High for imports | `src/Infrastructure/Queues/AzureQueueSender.cs`, `src/Worker/Queues` |
| OpenAI API | External HTTP API | Document extraction services | `OpenApiSettings:ApiKey` and model | Feature-specific | `src/Infrastructure/AI`, `src/Infrastructure/DependencyInjection.cs` |
| SignalR | Realtime transport | Push domain-event notifications to Angular | Web auth; Redis backplane | Medium | `src/Web/Program.cs`, `src/Infrastructure/Realtime`, `src/Client/src/app/core/signalr` |
| ASP.NET Core Identity | In-process auth provider | Users, Guid-keyed roles, cookie/bearer API endpoints | SQL Server stores | High | `src/Infrastructure/Identity`, `src/Web/Endpoints/Users.cs` |
| Azure Key Vault | Optional secret provider | Adds configuration when `AZURE_KEY_VAULT_ENDPOINT` is present | `DefaultAzureCredential` | Optional | `src/Web/DependencyInjection.cs` |
| OTLP | Observability exporter | Optional telemetry export | `OTEL_EXPORTER_OTLP_ENDPOINT` | Medium | `src/ServiceDefaults/Extensions.cs` |

### 2) Data Stores

| Store | Access layer | Main risk | Evidence |
|-------|--------------|-----------|----------|
| SQL Server | `ApplicationDbContext` through `IApplicationDbContext` | Startup depends on migration/database availability | `src/Infrastructure/Data` |
| Redis | `HybridCache`, FusionCache backplane, `IDistributedLock`, SignalR | Cache/lock/backplane outages affect resilience and multi-instance behavior | `src/Infrastructure/DependencyInjection.cs`, `src/Infrastructure/Distributed` |
| Blob storage | `IBlobStorageService` and storage commands | Pending file metadata can outlive an abandoned direct upload | `src/Application/Storage`, `src/Infrastructure/Storage` |
| Queue storage | `IQueueSender`, outbox publisher, Worker processors | At-least-once delivery and poison-queue recovery | `src/Web/BackgroundJobs`, `src/Worker/Queues` |

### 3) Secrets and Credentials Handling

- Local Aspire secrets are modeled with `AddParameter(..., secret: true)` and injected by resource references.
- Production secrets are supplied by GitHub Environment secrets and rendered into a protected env file by `.github/workflows/deploy-production.yml`.
- `deploy/production.env.example` is a contract/example, not a real secret file.
- Do not commit API keys, passwords, SAS tokens, or generated runtime env files.

### 4) Reliability and Failure Behavior

- OpenAI HTTP uses standard resilience with a two-minute attempt timeout, five-minute total timeout, exponential retries, and circuit breaker settings.
- Outbox publishing claims bounded batches, uses leases/retry counts, and records failures.
- Azure queue consumers use visibility timeouts, duplicate detection through `ProcessedMessages`, permanent/retryable classification, and poison queues.
- Daily statistics uses a Redis lock, persisted `ScheduledJobRun`, startup catch-up, and retries at 1/5/15 minutes.
- Aspire `.WaitFor(...)` gates dependent services on resource readiness; it is startup ordering, not a runtime fallback.

### 5) Observability

- `ServiceDefaults` configures OpenTelemetry logging, ASP.NET Core/HTTP/runtime metrics and traces, and optional OTLP export.
- Queue, outbox, AI, lock, and scheduling components use structured `ILogger` logs.
- `[TODO]` No dedicated EF Core/Azure Storage instrumentation package was found; add it only if production diagnostics require query/SDK-level spans.

### 6) Evidence

- `src/AppHost/Program.cs`
- `src/Infrastructure/DependencyInjection.cs`
- `src/Infrastructure/Storage`, `src/Infrastructure/Queues`, `src/Infrastructure/Distributed`
- `src/Web/BackgroundJobs/OutboxPublisherService.cs`
- `src/Worker/Queues`, `src/Worker/Statistics`
- `.github/workflows/deploy-production.yml`
- `deploy/production.env.example`
