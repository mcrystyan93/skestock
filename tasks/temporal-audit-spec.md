# Spec: Database history and audit (SQL Server temporal tables)

## Objective
Keep a queryable history of every change to business data, retained 2 years, with "who changed it" readable
without extra work. Users: administrators/staff investigating "who changed stock/item/order X and when".
Success: for any covered row, `FOR SYSTEM_TIME ALL` returns every prior version with the modifying user; history older
than 2 years is purged automatically by SQL Server; no behavior change for existing features.

## Findings from analysis (current state)
- 30 app tables + 7 Identity tables. ~22 derive `BaseAuditableEntity` (CreatedById/CreatedDate/LastModifiedById/LastModifiedDate, Guid -> UserProfiles).
- `AuditableEntityInterceptor` stamps those from `IUser.Id` (Web: claim; Worker: `AmbientUser` from queue envelope; statistics job/seeding: null).
- No existing temporal/history tables. 30 migrations, SQL Server 2022 in production (temporal + retention supported).
- Gaps: tables without audit columns: ItemLocationThresholds, ClassDepartment, DepartmentTemplate, SharedClassConfigurations, DailyItemConsumptions, ItemPurchaseStatistics, OutboxMessages, ProcessedMessages, ScheduledJobRuns, Identity.
- `ExecuteUpdate`/`ExecuteDelete` bypass the interceptor (statistics rebuild, InitializeSchoolClassConfiguration, outbox claims, seeding).
- Respawn 7 in functional tests; only `MessageTemplates` ignored. TRUNCATE is illegal on system-versioned tables.
- Large nvarchar(max) columns: import batch JSON (3 tables), `OutboxMessages.Payload`, `Responsibilities`.

## Design (proposed)
1. **Temporal on by convention**: in `ApplicationDbContext.OnModelCreating`, after `ApplyConfigurationsFromAssembly`, mark every entity type temporal except an explicit exclusion list. Shadow period columns (`PeriodStart`/`PeriodEnd`), history table `<Table>History`, same schema.
2. **2-year retention** via `migrationBuilder.Sql` per table: `ALTER TABLE ... SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = ..., HISTORY_RETENTION_PERIOD = 2 YEARS))` (EF has no retention API). Database flag `TEMPORAL_HISTORY_RETENTION` is ON by default on SQL Server 2022; assert it in the migration.
3. **Who changed it**: history rows already carry `LastModifiedById` (versioned with the row), so the user is recoverable by joining `UserProfiles` (itself temporal, so the name *at that time* is available via `FOR SYSTEM_TIME AS OF`). We do **not** copy the user name into every table (30 denormalized columns, stale on rename, bloat). Instead add a small read view/query helper that resolves id -> name.
4. **Close the "who" gaps**:
   - Add `LastModifiedById` (nullable Guid, default null) to the non-audited, non-excluded tables; stamp in the existing interceptor via a new tiny interface (`IHasModifier`) or by moving them onto `BaseAuditableEntity` where fully sensible.
   - Hard deletes: the history row records the last *modifier*, not the *deleter*. Option: interceptor turns a `Deleted` entry into an UPDATE of `LastModifiedById` immediately before the DELETE in the same transaction (one extra history version per delete, deleter recoverable).
   - `ExecuteUpdate` sites that touch covered tables set `LastModifiedById` explicitly; `ExecuteDelete` sites (derived statistics) are excluded tables so no change.
5. **Exclusions (justified)**: `OutboxMessages`, `ProcessedMessages`, `ScheduledJobRuns` (high-churn operational/ephemeral, no business value, `Payload` bloat); `DailyItemConsumptions`, `ItemPurchaseStatistics` (derived, deleted and rebuilt daily -> would double-write the whole table into history); `__EFMigrationsHistory`. All business tables and `StockTransactions`/`StockBatches` are included.
6. **System actors**: statistics job / seeder write null `LastModifiedById`; keep null = "system" (document it), do not invent a fake user.
7. **Tests**: add history tables to Respawn `TablesToIgnore`; verify DELETE-based reset works with system-versioned parents; unit test the interceptor; one functional test: update an item, `TemporalAll` returns 2 versions with right modifier.
8. **Reading history**: out of scope for v1 beyond EF `TemporalAsOf/TemporalAll` queries and a documented SQL snippet (see Open Questions).

## Tech Stack
.NET 10, EF Core 10 (`IsTemporal`, `TemporalAll/AsOf/Between`), SQL Server 2022, NUnit/Shouldly/Moq, Respawn 7.

## Commands
```
dotnet build
dotnet ef migrations add AddTemporalTables --project src/Infrastructure --startup-project src/Web --context ApplicationDbContext --output-dir Data/Migrations
dotnet ef migrations has-pending-model-changes --project src/Infrastructure --startup-project src/Web --context ApplicationDbContext
dotnet test tests/Application.UnitTests
dotnet test tests/Infrastructure.IntegrationTests
./run-functional-tests.sh
```

## Project Structure
- `src/Infrastructure/Data/ApplicationDbContext.cs` — temporal convention + exclusion list
- `src/Infrastructure/Data/Interceptors/AuditableEntityInterceptor.cs` — modifier stamping, delete-stamp
- `src/Infrastructure/Data/Migrations/` — generated migration + retention SQL
- `src/Domain/Common/` — modifier interface if needed
- `tests/Application.FunctionalTests/Infrastructure/DatabaseResetter.cs` — Respawn ignores
- `tests/Infrastructure.IntegrationTests/` — temporal behavior tests

## Code Style
Existing conventions (file-scoped namespaces, `Guard.Against`, UTC `DateTimeOffset`). Example convention hook:
```csharp
foreach (var entityType in modelBuilder.Model.GetEntityTypes()
             .Where(t => !t.IsOwned() && !TemporalExclusions.Contains(t.ClrType)))
    modelBuilder.Entity(entityType.ClrType).ToTable(t => t.IsTemporal());
```

## Testing Strategy
Unit: interceptor stamping (modified/deleted/system actor). Integration (real SQL Server container): temporal versions exist,
retention set (`sys.tables.history_retention_period = 2, unit YEARS`). Functional: full suite passes with Respawn changes.
Migration verified on a DB with existing rows (period columns default to existing data).

## Boundaries
- Always: generate migrations via `dotnet ef`; make the migration backward-safe; run build + all test projects; keep UI strings Romanian (none expected).
- Ask first: dropping/redefining existing audit columns; adding a user-name column to tables; an API/UI to browse history; including Identity tables.
- Never: edit applied migrations or the snapshot by hand; run migrations against production; store password hashes in a place broader than the live table's access.

## Success Criteria
- Every included table is system-versioned with 2-year retention (verified by query on `sys.tables`).
- Updating/deleting a covered row yields a history row whose modifier id is the acting user (Web and Worker paths).
- Existing unit/integration/functional tests green; no pending model changes.
- Migration applies cleanly on a database with existing data.

## Risks
- Storage: history roughly duplicates write-heavy tables (StockBatches, StockTransactions); 2-year retention bounds it.
- Migration on large tables takes a schema lock while adding period columns (tables are small for a school app; run in maintenance window anyway).
- `ExecuteUpdate`/`ExecuteDelete` paths do not stamp modifier unless fixed.

## Decisions (approved by user)
1. Only `LastModifiedById` (+ join to temporal `UserProfiles` for name); no name column.
2. Excluded: OutboxMessages, ProcessedMessages, ScheduledJobRuns, DailyItemConsumptions, ItemPurchaseStatistics.
3. Identity tables INCLUDED (accepted: PasswordHash/SecurityStamp live in history 2 years; history endpoint must be admin-only and must never expose those columns).
4. Stamp deleter via UPDATE-before-DELETE.
5. DB-level access PLUS an admin-only generic history endpoint (usable for any temporal entity, reusable later); exact shape decided in the plan.
