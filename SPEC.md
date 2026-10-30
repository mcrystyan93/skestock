# Spec: Per-location low-stock threshold

## Objective
Low stock is currently one number per item (`Item.MinThreshold`). Make it one number per (item, location). In the item create/edit form, below the existing fields, show one numeric box per location to set that location's threshold. Existing item values are copied to every existing location by the migration.

Success = staff set different thresholds per location; "stoc redus" badges/filters/KPIs/order-list wizard use the threshold of the location being evaluated.

## Findings (from exploration)
- `Item.MinThreshold` (int, `src/Domain/Entities/Items/Item.cs:12`), column since InitialCreate. Read/written in: Create/EditItem command+validator+handler, `ItemRequests`, `ItemDto`/`ItemDtoMapper`, `GetAllItems`/`GetItemById` handlers, `Endpoints/Items.cs`.
- Low-stock evaluation already happens per (item, location) row: `GetClassLocationStockHandler` (:107, :158-161, :221), `AdjustStockCommandHandler` (:61, :90), `GetSchoolClassSummaryHandler` (:45-56). Only the threshold source changes.
- Locations: global (not class-scoped), hierarchical (`ParentLocationId`), no active flag. Listing: `POST /api/Locations/get-all` (paginated).
- Client: `core/models/item.ts`, `shared/items/ui/modals/detail/{form.ts,form.html,item-detail-modal.ts}`, `features/items/services/item-list.store.ts`, item list table column "Prag minim".
- Seed (`ApplicationDbContextInitialiser.cs:481,712,557`) uses `MinThreshold`. AI extraction schema prompt mentions it (`ItemExtractionSchemaFactory.cs:21`; mapping back to item unverified).
- ~40 test files reference `MinThreshold`.

## Design
**Data**: new entity `ItemLocationThreshold { ItemId, LocationId, MinThreshold }`, composite PK (ItemId, LocationId), FKs to Item/Location, index on LocationId. Missing row = threshold 0 (never low).

**Migration** (via `dotnet ef migrations add AddItemLocationThresholds`, backward-safe, expand step): create table; `INSERT ... SELECT i.Id, l.Id, i.MinThreshold FROM Items i CROSS JOIN Locations l`. `Item.MinThreshold` column is kept for this release (see Open Question 2).

**Backend**
- Create/Edit item: replace `int MinThreshold` with `LocationThresholds: [{ locationId, minThreshold }]`; validator: each >= 0, locationIds exist and are unique. Edit replaces the set (upsert/delete).
- `ItemDto`: return `LocationThresholds`.
- Stock handlers: join `ItemLocationThresholds` on (ItemId, LocationId) instead of `Item.MinThreshold`.
- Cache: item commands already invalidate item tags; ensure stock/summary tags are also invalidated on threshold change (check `CacheConstants.cs`).

**Frontend** (NG-ZORRO, signals, Romanian copy)
- Item form: new section "Prag minim pe locație" under the form with one `nz-input-number` (min 0) per location, labelled with location name, responsive grid. Locations loaded via existing `locations.http.ts`. Signal Forms per AGENTS.md.
- `item.ts` models, `item-list.store.ts`, list column "Prag minim" updated (see Open Question 4).
- Update colocated specs.

## Commands
```
dotnet build
dotnet test tests/Application.UnitTests
./run-functional-tests.sh
dotnet ef migrations add AddItemLocationThresholds --project src/Infrastructure --startup-project src/Web --context ApplicationDbContext --output-dir Data/Migrations
dotnet ef migrations has-pending-model-changes --project src/Infrastructure --startup-project src/Web --context ApplicationDbContext
cd src/Client && npm run build && npm test -- --watch=false && npx prettier --write <files>
```
After code changes: `graphify update .`. Work on a feature branch, open PR, run Haiku reviewer on the diff.

## Testing
- Unit: validators (negative, duplicate/unknown location), Create/Edit handlers (upsert/delete), stock + summary handlers using per-location thresholds (location A low, B not).
- Functional: item create/edit endpoint with thresholds; low-stock query.
- Migration: verify on a DB with existing items that every item × location row gets the old value.
- Client: form spec (renders a box per location, submits thresholds), store spec.

## Boundaries
- Always: follow CLAUDE.md patterns, EF migration generated (never hand-edit snapshot), UTC/ints append-only rules, Romanian UI.
- Ask first: dropping `Item.MinThreshold`, changing AI extraction behavior, any new dependency.
- Never: edit applied migrations, run migrations against production.

## Success Criteria
1. After migration every existing item has a threshold row per existing location equal to its old `MinThreshold`.
2. Item form shows one box per location; save/reload round-trips values.
3. Low-stock flags/counts/filter/order wizard match per-location thresholds.
4. `dotnet build`, unit + functional tests, client build + tests all pass.

## Assumptions
1. Locations created after this change default to threshold 0 until set.
2. Whole-row evaluation stays per (item, location); no cross-location aggregation.
3. Plan authored on Opus, implementation on Sonnet, exploration on Haiku (as requested).

## Decisions (answered)
1. Form shows all locations, flat (parent name in label for children).
2. `Item.MinThreshold` column kept this release (mark obsolete, no longer read); dropped by a later migration.
3. AI import: extracted value applied to all locations. Seed: same value on every location.
4. Item list "Prag minim" column removed.

## Open Questions
None blocking. Note: with the column kept but unread, new items still need a value written to `Item.MinThreshold` (non-null int); handlers will write 0.
