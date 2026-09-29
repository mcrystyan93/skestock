# Todo: Supply lists (backend)

Spec: `docs/specs/supply-lists.md` · Plan: `tasks/plan.md`

- [x] T1: Domain + persistence
  - Acceptance: `SupplyList` (Name, Note, Frequency, IntervalWeeks, IsActive, Lines),
    `SupplyListLine` (SupplyListId, ItemId, Quantity, Unit, Notes), `SupplyListFrequency`;
    DbSets in `IApplicationDbContext`/`ApplicationDbContext`; configurations (string enum,
    unique Name, unique (SupplyListId, ItemId), decimal(18,3), lengths, cascade lines,
    restrict item, interval check constraint); migration `AddSupplyLists`.
  - Verify: `dotnet build`; `dotnet ef migrations has-pending-model-changes ...`
  - Files: Domain entities/enum, IApplicationDbContext, ApplicationDbContext, 2 configurations, migration.
- [x] T2: Shared slice pieces
  - Acceptance: `SupplyListErrors` (NotFound, NameAlreadyExists, NotEditable, ItemInactive),
    `CacheConstants`, DTOs/request models/`SupplyListLineInput`, line validation extension
    (quantity > 0, unit/notes length, item exists, no duplicates), line mapper (default
    quantity 1, unit from item), projection.
  - Verify: `dotnet build`
  - Files: Common/Errors/SupplyListErrors.cs, Features/SupplyLists/{CacheConstants,Models/*,SupplyListLine*,SupplyListProjection}.cs
- [x] T3: CreateSupplyList + unit tests
  - Acceptance: creates active list; defaults applied; duplicate name → 409; inactive item → error; frequency/interval validation.
  - Verify: `dotnet test tests/Application.UnitTests --filter FullyQualifiedName~SupplyLists`
- [x] T4: UpdateSupplyList + unit tests
  - Acceptance: replaces metadata + lines; 404; inactive list → 409; duplicate name (other list) → 409; lines with later-disabled items kept.
  - Verify: same filter.
- [x] T5: DisableSupplyList / EnableSupplyList + unit tests
  - Acceptance: toggles IsActive, idempotent, 404 when missing, invalidates cache tags.
  - Verify: same filter.
- [x] T6: GetSupplyListById + unit tests
  - Acceptance: returns lines with item name/sku/category; 404; cacheable with entity tag.
  - Verify: same filter.
- [x] T7: GetAllSupplyLists + unit tests
  - Acceptance: keyset pagination, filters (name, frequency, isActive), sort (name, frequency, created, lastModified), search by name, line count; cacheable with list tag.
  - Verify: same filter.
- [x] T8: Web endpoints `SupplyLists`
  - Acceptance: get-all, {id}, create (201), update, disable, enable; named static handlers, summaries, `Results<...>`.
  - Verify: `dotnet build`
- [x] T9: Functional tests
  - Acceptance: lifecycle (create → get → update → disable → update fails → enable) and get-all pagination/filter.
  - Verify: `./run-functional-tests.sh --filter FullyQualifiedName~SupplyLists`; then `graphify update .`
