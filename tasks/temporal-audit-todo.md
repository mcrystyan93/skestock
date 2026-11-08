# Tasks: database history and audit (temporal tables)

Details and rationale in `temporal-audit-plan.md`. Write failing tests first where a test is listed.

## Phase 0
- [x] T0 Branch `feat/temporal-audit` (already checked out by user; no commit/PR by the agent); commit spec/plan/todo.
  - Acceptance: on branch, not `master`. Verify: `git branch --show-current`. Files: `tasks/temporal-audit-*.md`.

## Phase 1: schema
- [x] T1 Temporal convention + shadow `LastModifiedById` + `TemporalExclusions` in `OnModelCreating`.
  - Acceptance: every entity type except the 5 exclusions `IsTemporal()` (incl. Identity, `CategoryIcon`, both owned
    import-history types); every temporal table has `LastModifiedById`; no FK/index on the shadow column.
    Resolve Unknown 1 (fallback in plan (a)).
  - Verify: `dotnet build`; new model unit test; `dotnet test tests/Application.UnitTests`.
  - Files: `src/Infrastructure/Data/ApplicationDbContext.cs`,
    `tests/Application.UnitTests/Infrastructure/Data/TemporalModelTests.cs`.
- [x] T2 Migration `AddTemporalTables` + appended SQL (retention-flag assert, per-table 2-year retention, history index).
  - Acceptance: generated via `dotnet ef migrations add`; snapshot/designer untouched by hand; existing rows keep
    data; `has-pending-model-changes` clean. Resolve Unknown 3.
  - Verify: `dotnet ef migrations script <previous> AddTemporalTables` reviewed; `has-pending-model-changes`.
  - Files: `src/Infrastructure/Data/Migrations/<ts>_AddTemporalTables{,.Designer}.cs`, `ApplicationDbContextModelSnapshot.cs`.
- [x] T3 Respawn `CheckTemporalTables = true`; integration test for the temporal schema.
  - Acceptance: test asserts set of temporal tables == model's, each `history_retention_period = 2` / `YEAR`, each
    history table has the key index; functional reset still works.
  - Verify: `dotnet test tests/Infrastructure.IntegrationTests --filter TemporalSchema`; `./run-functional-tests.sh`.
  - Files: `tests/Application.FunctionalTests/Infrastructure/DatabaseResetter.cs`,
    `tests/Infrastructure.IntegrationTests/TemporalSchemaTests.cs`.
- [ ] Checkpoint A: `dotnet build`; unit, integration, functional suites green; no pending model changes; dev DB
  (seeded) migrated by the stack with row counts unchanged and `FOR SYSTEM_TIME ALL` working on one item.

## Phase 2: who changed it (parallel with Phase 3)
- [x] T4 Interceptor: shadow stamping for non-`BaseAuditableEntity` entries; UPDATE-before-DELETE with rowversion
  refresh; `shortcut:` comments (non-atomic without transaction, one UPDATE per row).
  - Acceptance: Added/Modified on `ItemLocationThreshold`/Identity entries get `user.Id`; null user -> null; deleting
    an `OrderList` (rowversion) succeeds and history has a version with the deleter; same for
    `ItemLocationThreshold` and `AspNetUserRoles` via `UserManager.RemoveFromRoleAsync`; with and without ambient
    transaction; identifiers only from EF metadata, values parameterized.
  - Verify: unit tests (InMemory, stamping); integration tests (real SQL, delete paths).
  - Files: `src/Infrastructure/Data/Interceptors/AuditableEntityInterceptor.cs`,
    `tests/Application.UnitTests/Infrastructure/Data/Interceptors/AuditableEntityInterceptorTests.cs`,
    `tests/Infrastructure.IntegrationTests/TemporalDeleteStampTests.cs`.
- [x] T5 `InitializeSchoolClassConfiguration` `ExecuteUpdate` also sets `LastModifiedById`/`LastModifiedDate`.
  - Acceptance: history version written by the claim carries the acting user.
  - Verify: existing handler tests updated/green; `dotnet test tests/Application.UnitTests --filter InitializeSchoolClass`.
  - Files: `.../InitializeSchoolClassConfiguration/InitializeSchoolClassConfigurationCommandHandler.cs`, its unit test.
- [x] T6 Functional "who" test: update an item via API as a user; `TemporalAll` returns 2 versions, newest
  `LastModifiedById` = that user.
  - Verify: `./run-functional-tests.sh --filter FullyQualifiedName~ItemHistory`.
  - Files: `tests/Application.FunctionalTests/Features/Items/ItemHistoryTests.cs`.
- [ ] Checkpoint B: build; unit + integration + functional green.

## Phase 3: admin history endpoint
- [x] T7 `IEntityHistoryReader` + `EntityHistoryVersionDto` + Infrastructure `EntityHistoryReader` (allowlist from
  relational model, table/column denylist, UNION ALL raw SQL, offset paging, operation hints, names via
  `UserProfiles.TemporalAll()`); register in Infrastructure DI. Resolve Unknown 2.
  - Acceptance: hostile entity names -> unknown; bad id -> invalid; `AspNetUsers` rows never contain
    `PasswordHash`/`SecurityStamp`/`ConcurrencyStamp`; `AspNetUserTokens` unknown; int key works; paging stable;
    `Deleted` hint on stamped version (after T4); renamed profile shows old name for old versions.
  - Verify: `dotnet test tests/Infrastructure.IntegrationTests --filter EntityHistoryReader`.
  - Files: `src/Application/Common/Interfaces/IEntityHistoryReader.cs`,
    `src/Application/Features/EntityHistory/Queries/GetEntityHistory/EntityHistoryVersionDto.cs`,
    `src/Infrastructure/Data/EntityHistoryReader.cs`, `src/Infrastructure/DependencyInjection.cs`,
    `tests/Infrastructure.IntegrationTests/EntityHistoryReaderTests.cs`.
- [x] T8 Query slice: `GetEntityHistoryQuery` (`[Authorize(Roles = Roles.Administrator)]`, not cacheable),
  validator, handler, `EntityHistoryErrors` (UnknownEntity 404, InvalidEntityId 400).
  - Verify: validator + handler unit tests; `dotnet test tests/Application.UnitTests --filter EntityHistory`.
  - Files: `GetEntityHistoryQuery.cs`, `GetEntityHistoryQueryValidator.cs`, `GetEntityHistoryHandler.cs`,
    `src/Application/Common/Errors/EntityHistoryErrors.cs`, `tests/Application.UnitTests/Features/EntityHistory/...Tests.cs`.
- [x] T9 Endpoint `EntityHistory` (`GET {entity}/{id}`) + functional tests (admin 200 with 2 versions and modifier
  name; default user 403; unknown entity 404).
  - Verify: `./run-functional-tests.sh --filter FullyQualifiedName~EntityHistory`; `/scalar` shows the route.
  - Files: `src/Web/Endpoints/EntityHistory.cs`, `tests/Application.FunctionalTests/Features/EntityHistory/EntityHistoryTests.cs`.
- [ ] Checkpoint C: build; all .NET suites green (`Application.UnitTests`, `Worker.UnitTests`,
  `Infrastructure.IntegrationTests`, functional); no pending model changes; `./run-e2e.sh` still green.

## Phase 4: finish
- [x] T10 Docs: CLAUDE.md section 10 temporal bullet; `docs/codebase/ARCHITECTURE.md` "Data history" section with SQL
  snippet and limits (cascades, non-atomic stamp, null = system).
  - Files: `CLAUDE.md`, `docs/codebase/ARCHITECTURE.md`.
- [ ] T11 `graphify update .`; Haiku review of the diff (task goal + diff only); fix real findings; open PR noting
  the pre-deploy checks (retention flag, table sizes) and the open user decision on Unknown 5.

## Implementation notes and plan deviations (agent run)

- Unknown 1 resolved: `SetIsTemporal(true)` via metadata works for Identity and own-table owned types. `CategoryIcon` is
  JSON-mapped (`ToJson`), not table-sharing: it has no table, so it is skipped (temporal-marking it breaks model validation).
- Unknown 3 resolved: system-versioning `UserProfiles` (persisted computed `FullName`) applies cleanly.
- Unknown 2 resolved: `FOR SYSTEM_TIME ALL` hides the zero-duration delete-stamp version (verified: 1 row via ALL vs 2 in the
  history table). The reader's UNION ALL shows it; documented in ARCHITECTURE.md.
- Deviation (T2): EF enables versioning at the END of its migration, so the appended retention SQL failed ("period not
  defined"). Retention/indexes/flag assert moved to a second EF-generated (empty) migration `SetTemporalRetention`.
- Deviation (T3): Respawn `CheckTemporalTables = true` also needs `MessageTemplatesHistory` in `TablesToIgnore`
  (history of an ignored table cannot be deleted from).
- Deviation (T5 side effect): history columns are HIDDEN, so `ClassLocking`'s `SELECT *` FromSql lost `PeriodEnd`
  (6 CloseSchoolClass functional failures). Fixed with `SELECT *, [PeriodStart], [PeriodEnd]`.
- Deviation (T4): the `AspNetUserRoles` delete test removes the `IdentityUserRole` through the context (what `UserManager`
  does) instead of going through `UserManager.RemoveFromRoleAsync` (needs the full DI host with Mediator).
- Deviation (T5): no unit test (`ExecuteUpdate` is unsupported by the InMemory provider); covered by existing functional
  school-class tests.
- Unknown 5 decided by user: non-atomic delete stamp accepted for `DeleteDepartment`/Identity; documented in the
  interceptor comment and ARCHITECTURE.md.
- Unknown 6/7 not investigated further (no Worker-specific test).
- Not done (T11): Haiku review and PR are left to the user; `graphify update .` run at the end.
