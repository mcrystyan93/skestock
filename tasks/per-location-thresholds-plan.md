# Plan: per-location low-stock thresholds (replaces `Item.MinThreshold`)

Source: `SPEC.md` + Opus planner review of the code. Items marked **Δ spec** deviate from SPEC.md and need approval.

## Deviations from the spec (need approval)
1. **Δ Edit adds/updates thresholds, never deletes** (spec: "replace the set"). `item-list.store.ts` `setItemImage` sends the list row back to `PUT /api/Items/{id}`; list rows carry no thresholds, and a failed location load would send an empty list. Replace-the-set would wipe thresholds in both cases. Locations cannot be deleted and the form always sends all locations, so delete logic is never needed. Rule: locations not sent keep their value; empty list changes nothing.
2. **Δ Entity has `Id` (`BaseEntity`) + unique index (ItemId, LocationId)**, not a composite PK. ~39 unit-test DbContexts use EF conventions only; a composite key without configuration fails there. Matches `ClassItemStockVisibility`.
3. **Δ AI import: no change.** The extraction JSON schema has no threshold property (only a stray prompt line) and the confirm handler never sets one; imported items get no rows (= 0, same as today). Spec decision 3 has no value to apply. Removing the prompt line would be an AI behaviour change, so it stays untouched unless you say otherwise.
4. No cache changes needed: `ItemUpdatedEvent`/`ItemCreatedEvent` handlers already invalidate item list, category list and the coarse stock tag used by `GetClassLocationStockQuery` (also serves `/low-stock`). `GetSchoolClassSummaryQuery` is not cacheable.

## Choices from the code
| Topic | Choice |
|---|---|
| Entity | `Domain/Entities/Items/ItemLocationThreshold : BaseEntity { Guid ItemId; Guid LocationId; int MinThreshold }`, no navigations |
| Configuration | `ItemLocationThresholdConfiguration`: unique index (ItemId, LocationId); FKs to Item and Location with `DeleteBehavior.Restrict` (pattern: `ClassItemStockVisibilityConfiguration`) |
| DbContext interface | Default interface member `ItemLocationThresholds => throw new NotSupportedException(...)` in `IApplicationDbContext` (pattern: `SharedClassConfigurations`); real context returns `Set<>()`; only the ~8 test contexts that need it add one line |
| `Item.MinThreshold` | Keep property + column, add comment `// shortcut: no longer read; dropped in a later migration`. No `[Obsolete]` (TreatWarningsAsErrors → CS0618 in tests/seed). Handlers stop writing it |
| Wire DTO | `Items/Models/ItemLocationThresholdDto { Guid LocationId; int MinThreshold }` shared by requests, commands, `ItemDto` |
| Request/command | `int MinThreshold` → `List<ItemLocationThresholdDto> LocationThresholds { get; init; } = []` in Create/Edit request + command, `Items.cs` mapping |
| Validators | NotNull; each `MinThreshold >= 0`; distinct LocationIds → new `ValidationErrorCodes.DuplicateLocation`; all ids exist → `InvalidReference` |
| `ItemDto` | `LocationThresholds` filled by `ItemDtoMapper` and `GetItemByIdHandler`; left empty in `GetAllItems` (column removed; edit is add/update-only so safe) |
| Stock handlers | `GetClassLocationStockHandler`: drop `i.MinThreshold` from projection, add `thresholdByItemLocation` dictionary next to `visibilityByItemLocation`, `GetValueOrDefault` at the two comparisons. `AdjustStockCommandHandler`: lookup by (ItemId, LocationId). `GetSchoolClassSummaryHandler`: join `ItemLocationThresholds` on (ItemId, LocationId) |
| Migration | `dotnet ef migrations add AddItemLocationThresholds`, then add one `migrationBuilder.Sql("INSERT INTO [ItemLocationThresholds] ([Id],[ItemId],[LocationId],[MinThreshold]) SELECT NEWID(), i.[Id], l.[Id], i.[MinThreshold] FROM [Items] i CROSS JOIN [Locations] l;")`. Designer/snapshot not hand-edited |
| Seed | Both item loops keep `MinThreshold = random` (simulation reads it, :557); after `AddRange`, add one row per location with that value |
| Client locations | `LocationsHttp.getAllPages()` copied from `SupplyListsHttp.getAllActive()` (expand/reduce, page cap 50); `rxResource` in `Form` |
| Client form | `locationThresholds: {locationId,label,minThreshold}[]` via `linkedSignal`; `applyEach` validation (required, min 0); `@for` of `nz-input-number [nzMin]="0"`, `nzXs=24 nzSm=12` cols, heading "Prag minim pe locație"; label = `parent / name`; skeleton while locations load; error text "Locațiile nu au putut fi încărcate." |
| Client list | Remove `minThreshold` column + `th`/`td`; `setItemImage` sends `locationThresholds: []` |

## Dependency graph
```
T0 branch
 └─ T1 schema + migration ──┬─ T2 ItemDto read path ──┬─ T3 Create ── T4 Edit ─┐
                            ├─ T5 stock evaluation    │                        │
                            └─ T6 seed                └─ T7 client models/list/http ── T8 form ┘
                                                                                   └─ T9 verify, review, PR
```
Parallel after T1: T2, T5, T6. T3 then T4 (shared files). T7 needs only the DTO shape; T8 needs T7.

## Tasks
**T0** Branch `feature/per-location-thresholds` from up-to-date `master`.

**T1 Schema + migration** — Acceptance: entity, configuration, DbSet, interface default member, migration with backfill `Sql`; no other schema diff. Verify: `dotnet build`; `has-pending-model-changes` clean; `dotnet ef migrations script 20261028201547_AddFileMetadataFileIdIndex AddItemLocationThresholds ...` reviewed (expand-only); unit tests green. Files: `ItemLocationThreshold.cs`, `ItemLocationThresholdConfiguration.cs`, `ApplicationDbContext.cs`, `IApplicationDbContext.cs`, migration, `Item.cs` (comment).
**Checkpoint A:** build green, no pending changes, SQL reviewed.

**T2 ItemDto read path** — Tests first: GetItemById returns rows for A and B; mapper via EnableItem. Acceptance: DTO type, `ItemDto` swap, mapper + GetById fill, GetAllItems drops line, DbSet line in Create/Edit/Enable/Disable/GetById test contexts, remove `MinThreshold` result assertions. Verify: `dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~Items"`.

**T3 Create** — Tests first: validator (negative, duplicate, unknown, empty valid), handler persists rows, functional round trip. Acceptance: command/request/endpoint/validator/handler + `DuplicateLocation`. Verify: unit filter `CreateItem` + `./run-functional-tests.sh --filter "FullyQualifiedName~CreateItem"`.

**T4 Edit (add/update only)** — Tests first: update existing, insert new, omitted unchanged, empty list no-op; functional round trip. Acceptance: handler stops assigning `item.MinThreshold`, still raises `ItemUpdatedEvent`. Verify: filter `EditItem` unit + functional.

**T5 Stock evaluation** — Tests first: same item, A threshold 10/qty 5 and B threshold 1/qty 5 → A low, B not; `LowStockOnly` returns A; summary count 1; AdjustStock uses request location; missing row → not low. Acceptance: three handlers; DbSet in `GoodsReceiptTestDbContext` and summary context; helpers seed threshold rows; one functional `LowStockOnly` test. Verify: unit filter `GetClassLocationStock|GetSchoolClassSummary|AdjustStock` + functional filter.

**T6 Seed** — both loops add per-location rows. Verify: build; local AppHost shows "stoc redus" badges.
**Checkpoint B:** build; Application/Worker/Infrastructure tests; `./run-functional-tests.sh`; no pending model changes; `grep -rn MinThreshold src --include=*.cs | grep -v Migrations` leaves only `Item.cs` and seed.

**T7 Client models/list/http** — `item.ts` types + column removal, table `th`/`td`, `setItemImage` `[]`, `LocationsHttp.getAllPages()`, `error-codes.ts` `duplicateLocation`, 5 spec fixtures, store spec assertion. (Run with T8 before building, or minimally fix `form.ts`/modal types first.)

**T8 Form section** — remove old field; new section; prefill from `item.locationThresholds`; Romanian messages; `item-detail-modal.ts` `mapSaveRequest`; `form.spec.ts` with `LocationsHttp` stub + cases (N boxes with labels, prefill, negative invalid, submit payload, load failure submits `[]`). Verify: `npm run build && npm test -- --watch=false`, manual check in AppHost (round trip, badges, filter, class KPI, order-list wizard).
**Checkpoint C:** client build/tests green, prettier, manual run-through.

**T9 Verify, review, PR** — local DB counts: `ItemLocationThresholds` = items × locations; zero rows where threshold ≠ `Items.MinThreshold`. `graphify update .`; Haiku reviewer on diff + goal; fix real findings; open PR.

## Risks
- Edit endpoint wipe → mitigated by add/update-only (deviation 1).
- Migration: expand-only (new table + insert), auto-applied on deploy; rollback leaves old column at last value, losing only post-deploy edits. Cross join is small.
- ~25 test files reference `MinThreshold`; only threshold-dependent ones change (stock, summary, adjust, create/edit). Incidental ones keep compiling until the later drop-column migration.
- Test contexts missing the DbSet throw a clear `NotSupportedException`; run the whole unit suite at each checkpoint.
- Concurrent edits adding the same new pair → unique index fails one save (rare, data consistent); accepted, no concurrency token.
- Cosmetic: a row of 0 makes negative quantity "low"; no row does not.

## Not verified by the planner
Angular `@for` over a signal-forms array field (only the `order-list-lines-container.ts` precedent was checked); test files read via grep, not line by line.
