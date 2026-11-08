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

Separately, Worker's `DailyStatisticsService` schedules a locked, persisted run; `DailyStatisticsJob` dispatches `GetConsumptionBackfillStartQuery`, bounded `MaterializeDailyConsumptionCommand` windows, then `MaterializePurchaseStatisticsCommand`. Daily-consumption windows are re-materialized on retry; purchase-statistics failures are logged without failing the consumption run.

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
| Scheduled materialization | `src/Worker/Statistics/DailyStatisticsJob.cs`, `src/Application/Features/Statistics` | Backfill daily consumption and refresh purchase statistics through Mediator |

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
- `src/Worker/Statistics/DailyStatisticsService.cs`, `src/Worker/Statistics/DailyStatisticsJob.cs`
- `src/Application/Features/Statistics/Commands/MaterializeDailyConsumption/MaterializeDailyConsumptionCommand.cs`

## Data history

Business data keeps a queryable history in SQL Server system-versioned (temporal) tables, retained 2 years.

- Coverage: every entity type, including the Identity tables and the owned `*ImportBatchHistory` tables, except `OutboxMessage`, `ProcessedMessage`, `ScheduledJobRun`, `DailyItemConsumption` and `ItemPurchaseStatistic` (operational or derived and rebuilt daily), plus `IdentityUserToken<Guid>` (`AspNetUserTokens`: authenticator keys and recovery codes, so no secret copies linger in history; added by `ExcludeUserTokensFromTemporal`). JSON-owned types (`CategoryIcon`) have no table of their own. The convention and exclusion list live in `ApplicationDbContext.ConfigureTemporalTables`. History tables are `<Table>History`; period columns `PeriodStart`/`PeriodEnd` are hidden `datetime2` in UTC (SQL Server managed, an exception to the `DateTimeOffset` rule). Each history table has an index on `(<key>, PeriodStart DESC)`.
- Retention: `AddTemporalTables` (EF generated) converts the tables in place; `SetTemporalRetention` then sets `HISTORY_RETENTION_PERIOD = 2 YEARS` and the indexes, and asserts the `TEMPORAL_HISTORY_RETENTION` database option is on (it is by default on SQL Server 2022; it cannot be changed inside a migration transaction). They are two migrations because EF enables versioning at the end of its own migration.
- Who changed it: each row carries `LastModifiedById` (`UserProfiles.IdentityId` = `AspNetUsers.Id`). It is versioned with the row, so every history version names its writer. Tables without `BaseAuditableEntity` got a shadow `LastModifiedById` (no FK) stamped by `AuditableEntityInterceptor`. Null means the system (statistics job, seeding, anonymous sign-in writes). Names are resolved by joining the temporal `UserProfiles`, so the name at the time is available.
- Deletes: the history of a hard delete only names the last modifier, so the interceptor first runs `UPDATE ... SET LastModifiedById = <deleter>` and EF's DELETE follows. Limits: the stamp is atomic with the DELETE only inside an ambient transaction (class-scoped commands, queue processing). `DeleteDepartment` and Identity deletes have none, so if the DELETE then fails the row keeps the would-be deleter and one extra version (no data loss). DB-side cascades (`ClassDepartments`, `DepartmentTemplates`, `UserProfiles`) and raw SQL are not stamped; the parent's stamped version shares the deletion instant, which attributes them by hand. `ExecuteUpdate` bypasses the interceptor and must set `LastModifiedById` itself (`InitializeSchoolClassConfiguration` does).
- Zero-duration versions: a delete stamp written in the same transaction as the DELETE is a version with `PeriodStart = PeriodEnd`. `FOR SYSTEM_TIME ALL` hides it (verified), so the history endpoint reads the current table `UNION ALL` the history table instead.
- Reading: `GET /api/EntityHistory/{entity}/{id}?cursor=&pageSize=` (administrators only). `entity` is a single-key table name taken from an allowlist built from the EF model (composite-key tables such as `AspNetUserRoles` and `AspNetUserLogins` are not readable and answer 404 like unknown entities; `AspNetUserTokens` is also denied as a safety net); the columns `ProviderKey`, `PasswordHash`, `SecurityStamp`, `ConcurrencyStamp` and any `*Token*` are never returned. Paging is by offset (history rows have no unique key; rows are ordered by period then every column, so ties are identical and stable; offset capped at 1,000,000). `Created` is only labeled when the oldest surviving version starts within a minute of the row's `CreatedDate`; tables without it, or pruned/backfilled remainders, show `Updated`. Direct SQL:

```sql
SELECT * FROM dbo.Items FOR SYSTEM_TIME ALL WHERE Id = @id ORDER BY PeriodStart DESC;
SELECT * FROM dbo.Items FOR SYSTEM_TIME AS OF '2026-11-01T00:00:00' WHERE Id = @id;
```

- Operations: history roughly doubles write-heavy tables (`StockBatches`, `StockTransactions`) for 2 years. Before deploying, check `is_temporal_history_retention_enabled` on the production database and the row counts of the largest tables; the first migration takes schema locks while it adds the period columns.

