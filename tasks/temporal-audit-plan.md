# Plan: database history and audit (SQL Server temporal tables)

Spec: `temporal-audit-spec.md` (Decisions 1-5 approved). Todo: `temporal-audit-todo.md`.
Engineering level: production grade (schema change applied automatically on deploy, Identity data, admin-only
endpoint). No UI. No new dependencies or patterns: EF model conventions, the existing interceptor, one
Mediator query slice, one `IEndpointGroup`.

## Verified facts (source, not docs)

- `ApplicationDbContext` : `IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>`; `OnModelCreating` calls
  `base`, `UseCollation`, `ApplyConfigurationsFromAssembly`, then loops entity types for GUID v7 keys. The temporal
  convention goes after that loop.
- Entity types: 32 DbSets + `ClassDepartment`, `DepartmentTemplate` (no DbSet) + 2 owned types with their own table
  (`ImportBatchHistory` -> `ItemImportBatchHistory`, `CategoryImportBatchHistory`) + `CategoryIcon` (OwnsOne, shares
  `Categories`) + 7 Identity tables. Minus 5 exclusions = ~38 temporal tables (exact set asserted by test, T3).
- `LastModifiedById` FKs point at `UserProfile.IdentityId` (= `AspNetUsers.Id` = `IUser.Id`), not `UserProfile.Id`.
  Name lookup therefore joins `UserProfiles.IdentityId`.
- Tables with no `LastModifiedById` (need one): `ItemLocationThresholds`, `ClassDepartments`, `DepartmentTemplates`,
  `SharedClassConfigurations` (int key = 1), both owned `*ImportBatchHistory` tables, 7 `AspNet*` tables.
- Store-generated concurrency tokens: `StockBatch.Version`, `OrderList.Version` (rowversion). `ConcurrencyStamp` on
  import batches / Identity is app-managed (unaffected by a raw UPDATE).
- Retries are on: `EnrichSqlServerDbContext` enables `SqlServerRetryingExecutionStrategy`, so an interceptor cannot
  begin its own transaction (EF throws for user transactions outside `strategy.ExecuteAsync`).
  Transactions exist only in `ClassLocking.InTransactionAsync` (class-scoped commands), `OutboxClaimStore`,
  `QueueMessageProcessor`, the two statistics handlers.
- Hard-delete sites: `DeleteOrderList` (rowversion!), `UpdateOrderList` (`RemoveRange` lines), `SupplyListLineMapper`,
  `DeleteDepartment` (admin, no transaction), Identity `UserManager` (role removal etc.). DB-side cascades:
  `ClassDepartments`<-`SchoolClasses`, `DepartmentTemplates`<-`SharedClassConfigurations`, `UserProfiles`<-`AspNetUsers`.
- `ExecuteUpdate` on covered tables: `InitializeSchoolClassConfiguration` (SchoolClasses; the following
  `SaveChanges` does not re-stamp the class because `Departments` is not owned) and seeder/E2E seeder (system actor,
  leave). `ExecuteDelete`: statistics tables only (excluded). `OutboxClaimStore`: excluded table.
- Migrator login is `db_owner` (`DatabaseProvisioner`) -> may `ALTER TABLE ... SET SYSTEM_VERSIONING`, create
  indexes. App login is `db_datareader`/`db_datawriter` -> can read history tables. `MigrateAsync` runs each migration
  in a transaction, so `ALTER DATABASE` is not possible inside it.
- Respawn 7.0.0 has `CheckTemporalTables` (turns versioning OFF, deletes, turns it ON again **without retention**).
  Functional and integration tests each get a fresh, volume-less SQL container (`TestAppHost`); integration tests run
  `MigrateAsync` once and are not Respawned.
- Endpoint conventions: `IEndpointGroup` groups get `RequireAuthorization()`; admin-only is `[Authorize(Roles =
  Roles.Administrator)]` on the request; errors are typed `Error` classes with `ErrorMetadataKeys` (`CongregationErrors`);
  lists return `PaginatedResponse<T>` with an opaque `NextCursor`.

## Design decisions

### (a) Convention: what is temporal

In `OnModelCreating`, after the GUID loop, iterate `builder.Model.GetEntityTypes()` (mutable metadata, so owned and
Identity types are included) and for every type whose CLR type is not in `TemporalExclusions`:
1. `entityType.SetIsTemporal(true)` (EF default shadow `PeriodStart`/`PeriodEnd` datetime2, history table
   `<Table>History`, same schema). Owned types sharing the owner's table (`CategoryIcon`) are marked too, because EF
   requires every type mapped to a table to agree.
2. If the type has no `LastModifiedById` and owns its table (not table-sharing with its owner), add shadow property
   `LastModifiedById` (`Guid?`, no FK, no index: a FK to `UserProfiles` would block user deletion and create cycles
   with Identity tables; nobody filters by it).

`TemporalExclusions` (private static `HashSet<Type>` in `ApplicationDbContext`, one comment line each):
`OutboxMessage`, `ProcessedMessage`, `ScheduledJobRun`, `DailyItemConsumption`, `ItemPurchaseStatistic`.
`__EFMigrationsHistory` is not an entity type. New entity types become temporal automatically (documented).
Period columns are datetime2 in UTC (SQL Server-managed): a documented exception to the `DateTimeOffset` rule.
Fallback if `SetIsTemporal` via metadata does not create period properties for some type (Unknown 1): call
`builder.Entity(type).ToTable(t => t.IsTemporal())` for non-owned types and `OwnsMany(..., o => o.ToTable(name,
t => t.IsTemporal()))` in the two import-batch configurations.

### (b) Migration and retention

- `dotnet ef migrations add AddTemporalTables`. EF converts existing tables in place: adds period columns with
  defaults (`0001-01-01` / `9999-12-31 23:59:59.9999999`) so existing rows are valid, `ADD PERIOD FOR SYSTEM_TIME`,
  `SYSTEM_VERSIONING = ON (HISTORY_TABLE = ...)`; plus `AddColumn LastModifiedById` (nullable, default null) on the
  13 tables above. Snapshot/designer are generated, never edited.
- Append `migrationBuilder.Sql(...)` at the end of `Up` only (no snapshot impact):
  1. Assert, not set (ALTER DATABASE is illegal in the migration transaction):
     `IF (SELECT is_temporal_history_retention_enabled FROM sys.databases WHERE name = DB_NAME()) = 0
     THROW 50000, 'TEMPORAL_HISTORY_RETENTION is OFF; run ALTER DATABASE CURRENT SET TEMPORAL_HISTORY_RETENTION ON', 1;`
     A failed assert rolls the migration back and the deploy fails loudly (safe).
  2. Per temporal table, explicit and reviewable (generate the lines once from the model with a throwaway script, paste):
     `ALTER TABLE [dbo].[X] SET (SYSTEM_VERSIONING = ON (HISTORY_RETENTION_PERIOD = 2 YEARS));`
     `CREATE INDEX [IX_XHistory_<Key1>] ON [dbo].[XHistory] ([<Key1>], [PeriodStart] DESC);`
     (the index serves the history endpoint's filter; repo rule "index new filter paths"; SQL Server's default
     history clustered index is `(PeriodEnd, PeriodStart)` only).
- `Down`: EF-generated (drops versioning, period columns, history tables, indexes with them). Never run in prod.
- Guard against silent retention loss: EF re-enables versioning **without** retention after some future column
  operations (drop/alter/rename). Integration test (T3) asserts every temporal table has `history_retention_period
  = 2`, unit `YEAR`, and the history index; CLAUDE.md pitfall tells authors to re-append the retention line.

### (c) Interceptor: stamping and delete attribution

`AuditableEntityInterceptor.UpdateEntities` keeps its `BaseAuditableEntity` loop unchanged, then:
- **Shadow modifier**: for every other entry whose `Metadata.FindProperty("LastModifiedById")` exists and state is
  Added/Modified, set `entry.Property("LastModifiedById").CurrentValue = user.Id`. Covers Identity tables (same
  context, `UserManager` saves through it), owned own-table types, the 4 non-audited domain tables.
- **Delete stamp (UPDATE-before-DELETE)**: only when `Database.IsRelational()` (unit tests use InMemory). For each
  `Deleted` entry of a temporal type with `LastModifiedById`, run before EF's DELETE:
  `UPDATE {table} SET [LastModifiedById] = @p0 [OUTPUT inserted.{rowversion}] WHERE {key cols} = @k... [AND {rowversion} = @orig]`.
  Identifiers come only from EF metadata (`GetTableName/GetSchema/GetColumnName`) delimited with EF's
  `ISqlGenerationHelper.DelimitIdentifier`; all values are parameters. For rowversion types (`StockBatch`, `OrderList`)
  read the new token via `SqlQueryRaw<byte[]>` and set `Property(Version).OriginalValue`, otherwise EF's DELETE
  would raise a false `DbUpdateConcurrencyException`; 0 rows -> leave it, EF raises the real conflict.
  Sync and async paths. Stamp even when `user.Id` is null (null = system, truthful).
  - Atomic when a transaction is open (all class-scoped commands via `ClassLocking`, queue processing).
    Otherwise (admin `DeleteDepartment`, Identity) the UPDATE autocommits before EF's own transaction: if the DELETE
    then fails, the row survives with the would-be deleter as modifier plus one extra version; no data loss.
    Mark with `// shortcut: stamp is not atomic without an ambient transaction, wrap callers if misattribution matters`.
  - One UPDATE per deleted row (`shortcut:` comment; deletes are tens of rows at most here).
  - Not stamped (documented): DB-side cascades listed above, `ExecuteDelete` (excluded tables only), raw SQL.
    The parent's stamped version shares the deletion instant, which is enough to attribute cascades by hand.
- **Identity tables can be stamped**: yes, through the shadow property; `SignInManager`/lockout writes
  (`AccessFailedCount`) are anonymous -> null modifier (storage note in Risks).
- **ExecuteUpdate**: `InitializeSchoolClassConfigurationCommandHandler` gets `IUser` + `TimeProvider` and adds
  `SetProperty(LastModifiedById, user.Id)` / `SetProperty(LastModifiedDate, now)`.

### (d) Generic history endpoint

`GET /api/EntityHistory/{entity}/{id}?cursor=&pageSize=` -> `PaginatedResponse<EntityHistoryVersionDto>`.

- Application slice `Features/EntityHistory/Queries/GetEntityHistory/`: `GetEntityHistoryQuery`
  `[Authorize(Roles = Roles.Administrator)]` (Entity, Id as string, Cursor, PageSize), validator (non-empty, max
  lengths, `PageSize` 1..`DEFAULT_PAGE_SIZE`, cursor = non-negative int), handler -> `IEntityHistoryReader`
  (`Application/Common/Interfaces`), maps "unknown entity" / "bad id" to `EntityHistoryErrors.UnknownEntity` (404)
  / `InvalidEntityId` (400). **Not** `ICacheableQuery`: no command could invalidate it generically and stale audit
  data is wrong data; admin-only traffic is tiny.
- Infrastructure `Data/EntityHistoryReader.cs` (scoped, registered in Infrastructure `DependencyInjection`):
  - **Allowlist from the EF relational model**, built once (static lazy): for each table in
    `Model.GetRelationalModel().Tables` whose entity mapping is temporal, key = table name (unique, also unambiguous
    for the shared-CLR owned `ImportBatchHistory`), value = schema, table, history table, first PK column + CLR type,
    column list (all table columns incl. table-sharing owned ones) minus period columns minus denylist.
    Table denylist: `AspNetUserTokens` (authenticator keys / recovery codes). Column denylist: `PasswordHash`,
    `SecurityStamp`, `ConcurrencyStamp`, and any column containing `Token` (safety net). Lookup is
    `OrdinalIgnoreCase` dictionary access; the request string is never put in SQL.
  - Id: parsed to the first PK column's CLR type (`Guid`/`int`/`string`), else `InvalidEntityId`. Filtering on the
    first PK column also covers composite keys (`AspNetUserRoles` by `UserId`, owned history by owner id).
  - **Raw SQL, not `TemporalAll`**: rows are dynamic (no CLR type for owned/Identity/projection), and
    `UNION ALL` of the current and history table also returns zero-duration versions (the delete stamp inside a
    transaction is exactly one: start = end), which `FOR SYSTEM_TIME` semantics may hide (Unknown 2):
    `SELECT {cols},[PeriodStart],[PeriodEnd] FROM {t} WHERE {k}=@id UNION ALL SELECT ... FROM {h} WHERE {k}=@id
    ORDER BY [PeriodStart] DESC, [PeriodEnd] DESC OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY` (`@take` = size + 1).
    Read with a `DbCommand` on `Database.GetDbConnection()` into `Dictionary<string, object?>`.
  - **Paging = offset, not keyset (justified)**: history rows have no unique key; zero-duration versions from one
    transaction tie on both period columns, so a keyset cursor can skip rows at a page boundary. Offset over one
    row's versions (bounded by 2-year retention, served by the new history index) can at worst repeat a row when a
    new version arrives between page requests, never drop one. `NextCursor` = next offset as string.
  - DTO: `ValidFrom` (UTC `DateTimeOffset`), `ValidTo` (null when current), `Operation` hint
    (`Deleted` = newest version and no current row, i.e. the stamped pre-delete version; `Created` = oldest retained
    version, last page; else `Updated`), `ModifiedById`, `ModifiedByName`, `Values`.
  - **Names at that time**: distinct modifier ids of the page ->
    `UserProfiles.TemporalAll().Where(ids.Contains(IdentityId))` projecting `FirstName`, `LastName`, period columns
    (typed EF, no raw SQL); pick the profile version valid at `ValidFrom`, else the latest. `FirstName + " " +
    LastName` avoids depending on the computed `FullName` in history (Unknown 3).
- Web `Endpoints/EntityHistory.cs` (`MapGet(GetEntityHistory, "{entity}/{id}")`, `ToOk()`).

### (e) Tests and Respawn

- `DatabaseResetter`: `CheckTemporalTables = true` (one line; ignoring ~38 history tables by name instead would
  rot). Keep `MessageTemplates` ignored. Retention loss in the functional DB is irrelevant (asserted in integration DB).
- Unit (`Application.UnitTests`): model test (temporal set == all minus exclusions; every temporal table has
  `LastModifiedById`), interceptor shadow stamping Added/Modified/system actor (InMemory), validator, handler error
  mapping. Mirror folders `Infrastructure/Data/...` and `Features/EntityHistory/...`.
- Integration (`Infrastructure.IntegrationTests`, real SQL): retention + history index on every temporal table and
  the temporal table set; delete stamp for `OrderList` (rowversion, no concurrency error), `ItemLocationThreshold`,
  `AspNetUserRoles` via `UserManager.RemoveFromRoleAsync`, with and without ambient transaction; reader: allowlist
  rejects `Items]; DROP TABLE x--`, denylisted columns absent for `AspNetUsers`, int key `SharedClassConfigurations`,
  paging, `Deleted` hint, renamed profile resolves old name.
- Functional: admin updates an item via API -> history endpoint returns 2 versions with modifier id/name;
  non-admin 403; unknown entity 404; full suite green with Respawn change.
- Migration on existing data: dev DB (seeded, has volume) — record row counts of a few tables, run the stack
  (`DatabaseMigrationRunner` applies it), compare counts, `SELECT ... FOR SYSTEM_TIME ALL` on one item.
  Also review `dotnet ef migrations script` for the new migration.

### (f) Docs

- CLAUDE.md section 10, one bullet: tables are temporal by convention (exclusions in `ApplicationDbContext`); never
  TRUNCATE temporal tables; `ExecuteUpdate` on them must set `LastModifiedById`; EF column drops/alters can reset
  retention -> re-append the retention line (test catches it); period columns are datetime2 UTC.
- `docs/codebase/ARCHITECTURE.md`: short "Data history" section: what is covered/excluded, who-changed-it model
  (`LastModifiedById` -> `UserProfiles.IdentityId`, null = system), delete stamp and its limits, endpoint, and a
  DB-level SQL snippet (`FOR SYSTEM_TIME ALL` / `AS OF`).

## Phases, dependencies, parallelism

```
T0 branch
T1 convention ──> T2 migration ──> T3 Respawn + schema tests ──> Checkpoint A
                                     ├─> T4 interceptor ─┐
                                     ├─> T5 ExecuteUpdate ┼─> T6 functional "who" test ──> Checkpoint B
                                     └─> T7 reader ──> T8 slice ──> T9 endpoint ─────────> Checkpoint C
T10 docs (any time after A) ; T11 graphify + Haiku review + PR (last)
```
Parallel after Checkpoint A: {T4, T5} and {T7 -> T8 -> T9} touch disjoint files (T7's delete-hint test needs T4
for the stamped version: run that one assertion after both land). T10 is independent.

## Risks and mitigations

| Risk | Mitigation |
|---|---|
| Deploy migration fails or locks big tables | Check prod sizes + retention flag before deploy (Unknown 4); maintenance window; migration is one transaction, so failure rolls back. |
| Silent retention reset by a later migration | Integration test on `sys.tables`; CLAUDE.md pitfall. |
| False concurrency errors on delete of rowversion entities | Refresh `OriginalValue` from `OUTPUT inserted.Version`; integration test deleting an `OrderList`. |
| Non-atomic delete stamp outside transactions | Only misattribution on a failed delete, never data loss; `shortcut:` comment; ask user (Unknown 5). |
| SQL injection via entity/id | Allowlist dictionary from EF model, identifiers delimited by EF helper, values parameterized; test with hostile names. |
| Secrets leak via history (Identity) | Admin-only request attribute + table/column denylist + integration test; DB access unchanged (app login already reads live tables). |
| Storage growth (StockTransactions, AspNetUsers lockout churn) | 2-year retention; acceptable for school-size data. |
| Respawn re-enable / ignore interplay | Run full functional suite at Checkpoint A. |
| `SELECT *` raw queries (`ClassLocking`) now return period columns | EF maps them (shadow properties); functional suite covers. |

## Remaining unknowns (resolve during the named task)

1. T1: does `IMutableEntityType.SetIsTemporal(true)` create period properties for owned (shared and own-table) and
   Identity types? Fallback in (a).
2. T7: does `FOR SYSTEM_TIME ALL` return zero-duration rows? Design avoids it (UNION ALL); spec criterion
   "`FOR SYSTEM_TIME ALL` returns every version" may not hold for the delete-stamp row — note in docs if so.
3. T2: system-versioning a table with a persisted computed column (`UserProfiles.FullName`): confirm SQL Server
   creates the history column as a plain column and the migration applies.
4. Before deploy (user): prod `is_temporal_history_retention_enabled` and row counts of the largest tables.
5. User decision: accept the non-atomic delete stamp outside transactions, or wrap `DeleteDepartment`/Identity
   callers in `ClassLocking.InTransactionAsync`-style transactions (scope increase).
6. `DispatchDomainEventsInterceptor` runs after stamping: entities changed by event handlers during save would carry
   a stale modifier (pre-existing behavior; check whether any handler mutates tracked entities).
7. Worker path: covered by the `IUser` abstraction (`AmbientUser`); no Worker-specific test planned unless wanted.
