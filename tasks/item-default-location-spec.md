# Spec: Item default location + description copied to supply list notes

## Objective
Staff set an optional **default location** on an item (add/edit item modal). It is used as the prefilled location when adding stock and when reviewing goods receipt import lines. Separately, when an item is added to a supply list line, the item's **Description** is prefilled into the line's **Notes** if Notes is empty. Existing items get a default location backfilled from their stock on first deploy.

Decisions (confirmed by user):
- "Comment" = existing `Item.Description` (no new text column).
- Copy happens **in the UI when the line is added**; backend does not copy.
- Location is **optional** (nullable).
- Backfill rule: location with the **highest total batch quantity**; items without stock stay NULL.

## Assumptions
1. One default location per item (not a list). `ItemLocationThreshold` stays unchanged and independent.
2. Prefill only: user can still change location in the stock/goods-receipt forms and edit the copied note.
3. Goods receipt: client review lines use item location when set, else today's default-location behaviour. No backend change to Process/Confirm (LocationId is already per line and required on confirm).
4. Disabled/merged items: merge keeps the target item's location (no merge logic change).
5. Deleting a location that is an item's default must not break; FK is `SetNull` or restricted per existing Location delete rules (to confirm in plan).

## Tech stack
.NET 10 / EF Core 10 / SQL Server; Angular 22 signal forms + NG-ZORRO 22 (see CLAUDE.md).

## Commands
```
dotnet build
dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~Item"
dotnet ef migrations add AddItemDefaultLocation --project src/Infrastructure --startup-project src/Web --context ApplicationDbContext --output-dir Data/Migrations
dotnet ef migrations has-pending-model-changes --project src/Infrastructure --startup-project src/Web --context ApplicationDbContext
cd src/Client && npm run build && npm test -- --watch=false
./run-functional-tests.sh --filter "FullyQualifiedName~Item"
```

## Scope / files (from exploration)
Backend
- `src/Domain/Entities/Items/Item.cs`: add `Guid? LocationId` + `Location? Location`.
- `src/Infrastructure/Data/Configurations/ItemConfiguration.cs`: relationship, index on LocationId.
- New EF migration `AddItemDefaultLocation`: add nullable column + FK, plus `migrationBuilder.Sql` backfill (pattern: `20261030161641_AddItemLocationThresholds`):
  ```sql
  UPDATE i SET LocationId = x.LocationId
  FROM Items i
  CROSS APPLY (SELECT TOP 1 b.LocationId FROM StockBatches b
               WHERE b.ItemId = i.Id
               GROUP BY b.LocationId
               ORDER BY SUM(b.Quantity) DESC, b.LocationId) x;
  ```
  (Exact batch table/column names verified in plan; temporal-table caveat: `LastModifiedById` handling checked for raw SQL.)
- `Application/Features/Items`: `ItemDto`, `CreateItemRequest`/`EditItemRequest`, Create/Edit commands + handlers + validators (location must exist if provided), GetAll/GetById mapping.
- `Application/Features/Items` caching/invalidation unchanged in pattern.

Frontend
- `src/Client/src/app/core/models/item.ts`: `locationId` on DTO/requests.
- `shared/items/ui/modals/detail/form.ts` + `form.html`: `ske-location-dropdown` (allowClear, no default prepopulate) bound to new `location` field; `item-detail-modal.ts` `mapSaveRequest`.
- `shared/stock-batches/ui/modals/add-stock-batch-modal`: prefill location from item when the item is chosen and no location was passed by the caller.
- `shared/goods-receipt-imports/ui/review-lines-table/{review-line,small/review-line-small}`: when a line's matched item has a location, prefill it instead of the store default.
- `shared/supply-lists/ui/modals/detail/form/supply-list-line.ts`: when an item is selected and `notes` is empty, set `notes` to the item's description (trimmed, truncated to 500).

## Code style
Follow surrounding code; Romanian UI copy (label "Locație implicită"); signal forms; no Prettier.

## Testing strategy
- Unit (NUnit): Create/Edit item handler+validator with and without location; invalid location -> validation error.
- Functional: create/edit/get item round-trips LocationId.
- Migration check: `has-pending-model-changes` clean; backfill verified against a seeded DB (item with batches in 2 locations picks the larger sum; item without stock stays NULL).
- Client (Vitest): item form renders/submits location; supply-list line copies description only when notes empty and never overwrites existing notes; add-stock prefill; review-line prefill.

## Boundaries
- Always: new EF migration (never hand edit snapshot), backend-safe nullable column, UI copy in Romanian, build+tests before PR, feature branch + PR.
- Ask first: dropping `Item.MinThreshold`, changing goods receipt Process/Confirm backend, any new dependency.
- Never: edit applied migrations, make location required, overwrite a non-empty supply-line note.

## Success criteria
1. Item add/edit modal shows an optional location dropdown and persists it.
2. Add stock for an item with a location opens with that location prefilled (changeable).
3. Goods receipt import review lines for matched items with a location prefill it; otherwise behave as today.
4. Adding an item to a supply list with empty note prefills note with item Description; non-empty note untouched.
5. After migration, every item with stock has the location holding the highest total quantity; items without stock are NULL.
6. `dotnet build`, backend tests, client build and tests pass.

## Open questions
- When an item is merged (MergeItems), keep the target's location even if null, or inherit the source's? (Proposed: keep target as is.)
- Truncate Description to 500 chars when copying to Notes? Both limits are 500, so no truncation is needed (proposed: none).
