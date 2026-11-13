# Supply lists

A supply list is a named, reusable shopping template: a set of items with a quantity and unit each, plus how often it is meant to be bought ("weekly", "every X weeks", "start of the school year", ...). Staff create and edit lists on the Items page; the order list wizard can start a new order from one or more active supply lists, merging their lines into a single order.

Supply lists are global data (not tied to a school class), and they do not change stock by themselves: they only hold item references and quantities.

UI copy is Romanian. Everything below was read from source; anything not traced is marked as such. Gaps and oddities are listed under [Open points](#open-points), not fixed. Related: [items.md](items.md), [stock.md](stock.md), [goods-receipts.md](goods-receipts.md), [locations.md](locations.md), [categories.md](categories.md) (each on its own PR until merged).

## At a glance

| Layer | Where |
|---|---|
| Domain | `src/Domain/Entities/SupplyLists/` (`SupplyList`, `SupplyListLine`), `Domain/Events/SupplyLists/` (created, updated, enabled, disabled), `Domain/Enums/SupplyListFrequency.cs` |
| Application | `src/Application/Features/SupplyLists/` (commands, queries, event handlers, models, shared helpers, filter/sort/cursor/cache config), errors in `Application/Common/Errors/SupplyListErrors.cs` |
| Infrastructure | `Data/Configurations/SupplyListConfiguration.cs`, `SupplyListLineConfiguration.cs` |
| Web | `src/Web/Endpoints/SupplyLists.cs` |
| Client | `src/Client/src/app/shared/supply-lists` (HTTP, stores, detail modal and form), `features/items` (the **Liste** tab: list store, filter, tables), `core/models/supply-list.ts`; consumed by the order list wizard in `shared/order-lists` |

There is no Worker, queue or AI involvement.

## Data model

| Entity | Meaning |
|---|---|
| `SupplyList` | `Name` (unique), optional `Note`, `Frequency`, `IntervalWeeks` (only for `EveryXWeeks`), `IsActive`, and its `Lines`. |
| `SupplyListLine` | `ItemId`, `Quantity` (decimal, default 1), `Unit`, optional `Notes`. At most one line per item in a list. |

`SupplyListFrequency` (stored as text, see SQL): `Weekly`, `EveryXWeeks`, `Monthly`, `StartOfSchoolYear`, `EndOfSchoolYear`, `MiddleOfSemester`, `StartOfMonth`, `EndOfMonth`, `Once`. The API registers an app-wide `JsonStringEnumConverter` with camel-case naming (`Web/DependencyInjection.cs:75`), the DTOs expose `Frequency` as the enum name string (`ToString()`), for example `"EveryXWeeks"`, and the client sends that same PascalCase string. Whether request parsing is case-insensitive was not checked.

## Backend

### Endpoints (`/api/SupplyLists`)

The group requires an authenticated user (`src/Web/Infrastructure/WebApplicationExtensions.cs:28`). The four commands also carry `[Authorize]` (any authenticated user, no role). No command is `IRequiresOpenClass`, since supply lists are not class data.

| Method + route | Use case | Notes |
|---|---|---|
| `POST get-all` | `GetAllSupplyListsQuery` | Keyset-paginated `SupplyListListItemDto` rows (with `LineCount`). Cached. |
| `GET {id}` | `GetSupplyListByIdQuery` | Full `SupplyListDto` with lines. Cached. |
| `POST` | `CreateSupplyListCommand` | Creates an **active** list (201). |
| `PUT {id}` | `UpdateSupplyListCommand` | Updates metadata and replaces the lines; only for an active list. |
| `POST {id}/disable` | `DisableSupplyListCommand` | Returns the updated list. |
| `POST {id}/enable` | `EnableSupplyListCommand` | Returns the updated list. |

Enable and disable are `POST` here (items use `PATCH`). There is no delete endpoint: a list is retired by disabling it.

### Validation (shared by create and update)

Create and Update share `SupplyListPayloadValidator`; Update excludes its own id from the duplicate-name check and requires a non-empty `Id`.

| Field | Rule |
|---|---|
| `Name` | required, max 200 (checked before trimming; the handlers trim afterwards); unique (trimmed, case- and accent-insensitive via `TextSearchCollation.AccentInsensitive`) → `DuplicateName` |
| `Note` | optional, max 1000; blank becomes `null` |
| `Frequency` | must be a defined value |
| `IntervalWeeks` | required and between 2 and 52 when `Frequency` is `EveryXWeeks`; must be null for any other frequency |
| `Lines` | an item may appear only once; each line needs an existing `ItemId`, `Quantity > 0`, `Unit` max 50 (blank is replaced by the item's unit), `Notes` max 500 (blank becomes `null`). The list may be empty. |

A name conflict that slips past the validator (a concurrent request) is caught in `SupplyListCommandSupport.SaveAsync`: if saving fails with a `DbUpdateException` and the name is indeed taken, it is surfaced as the same `DuplicateName` validation error; otherwise the exception is rethrown. The database also enforces the name with a unique index and the interval rule with a check constraint (see SQL).

### Lines and inactive items

`SupplyListLineMapper.ApplyLinesAsync` applies the request lines to the list:

- Lines are matched by item. Existing lines are updated in place (`Quantity`, `Unit`, `Notes`), lines missing from the request are removed, new ones are added. Line order is not stored; reads sort lines by item name then id.
- A blank `Unit` falls back to the item's unit; `Quantity` defaults to 1 in the input model.
- **Only newly added items must be active.** Adding an inactive item fails with `supply_lists.item_inactive` (400). An item already on the list that has since been disabled is kept, and its line can still be updated.
- An unknown item id returns `items.not_found` (404); the validator normally rejects it first with `InvalidReference`.

This is its own error (`SupplyListErrors.SupplyListItemInactive`), not the `items.inactive` (409) used by stock and goods receipts ([items.md](items.md#errors)).

### Active toggle

`SupplyListCommandSupport.SetActiveAsync` backs both Enable and Disable: an unknown id returns `supply_lists.not_found`; if the state already matches, nothing is saved and no event is raised (the current list is just returned); otherwise it flips `IsActive`, raises `SupplyListEnabledEvent`/`SupplyListDisabledEvent` and saves. A disabled list is read-only: Update returns `supply_lists.not_editable` (409) until the list is enabled again.

### Listing, filtering and sorting

- Filter fields (`SupplyListFilterConfiguration`): `id`, `name`, `frequency`, `isActive`, `createdDate`, `lastModifiedDate`.
- Sort keys (`SupplyListSortConfiguration`): `createdDate`, `lastModifiedDate`, `name`, `frequency`, `id`, each with `Id` as tie-breaker. Default sort: `Name asc, Id asc`. `Frequency` is stored as text, so sorting by it orders by the stored name, not by enum order (the translation to SQL was not traced).
- `searchTerm` is an accent-insensitive `Contains` on `Name`.
- Page size is validated by `GetAllSupplyListsQueryValidator`; the handler clamps it and fetches `pageSize + 1` rows to compute `hasNextPage`. The cursor encodes the sort it was issued for.

### Caching, events and realtime

- `GetAllSupplyListsQuery` (tag `supply_lists`) and `GetSupplyListByIdQuery` are `ICacheableQuery`, sliding expiration about 5 minutes with 30 s jitter. The by-id entry carries the tags `supply_list:{id}`, `items` and `categories`, because the DTO embeds item and category names; an item or category change therefore evicts it.
- Invalidation through `ICacheInvalidation`: Create → `supply_lists`; Update, Enable, Disable → `supply_lists` and `supply_list:{id}`.
- The four event handlers (`SupplyListCreated/Updated/Enabled/DisabledEventHandler`) only send a SignalR notification (`SupplyListCreated`, `SupplyListUpdated`, `SupplyListEnabled`, `SupplyListDisabled`) to the `supply-lists-list` group with `{ SupplyListId }`. Unlike the item and category handlers they do not touch the cache.
- Item merges re-point supply list lines: `MergeItemsCommand` invalidates `supply_lists`, and `SupplyListLineItemMerger` handles the rows ([items.md](items.md#merging-duplicates)). A collision is a list that already has a line for both items; the survivor's `Quantity` is increased by the other line's, with no check that the units match.

### Errors

`supply_lists.not_found` (404), `supply_lists.not_editable` (409), `supply_lists.item_inactive` (400); also `items.not_found` (404). Validation failures use `ValidationErrorCodes`.

### Tests

- Unit: `tests/Application.UnitTests/Features/SupplyLists/` (command handlers, `SupplyListCommandSupport`, query handlers, validators, realtime events and handlers; shared `SupplyListTestDbContext` and test data). Cases include default quantity and unit from the item, inactive item rejected on create and on adding to an existing list, disabled existing items kept, a disabled list blocking updates, enable/disable events only on a state change, the unique-index race mapped to `DuplicateName`, and the by-id cache tags.
- Functional: `tests/Application.FunctionalTests/Features/SupplyLists/SupplyListLifecycleTests.cs` (create, update, disable, enable; duplicate name; paging and filtering).
- Client: colocated specs for the models, the list store, filter form, small list, detail store and detail form.
- No Playwright spec for supply lists was found; the e2e seed data (`ApplicationDbContextInitialiser.E2e.cs`) creates demo lists.

## SQL

Both tables are SQL Server temporal tables with 2-year history retention (`SetTemporalRetention`: `SupplyLists` and `SupplyListLines`); neither is in `TemporalExclusions`.

### `SupplyLists`

| Column | Type | Notes |
|---|---|---|
| `Id` | `uniqueidentifier` | PK; `Create` sets it with `Guid.CreateVersion7()` |
| `Name` | `nvarchar(200)` | **unique index** `IX_SupplyLists_Name` (default collation; the app-level check is accent-insensitive) |
| `Note` | `nvarchar(1000)` null | |
| `Frequency` | `nvarchar(30)` | enum name stored as text |
| `IntervalWeeks` | `int` null | |
| `IsActive` | `bit` | |
| audit columns | | `CreatedDate`, `LastModifiedDate` (`datetimeoffset`), `CreatedById`, `LastModifiedById` (FK to `UserProfiles.IdentityId`, `Restrict`) |

Check constraint `CK_SupplyLists_IntervalWeeks`: `Frequency = 'EveryXWeeks'` requires `IntervalWeeks BETWEEN 2 AND 52`, any other frequency requires `IntervalWeeks IS NULL`.

### `SupplyListLines`

`Id`, `SupplyListId` (FK, **cascade delete** from the list), `ItemId` (FK to `Items`, `Restrict`), `Quantity` (`decimal(18,3)`), `Unit` (`nvarchar(50)`), `Notes` (`nvarchar(500)` null), audit columns. Unique index `IX_SupplyListLines_SupplyListId_ItemId`, plus indexes on `ItemId` and the audit foreign keys.

### Migrations

`AddSupplyLists` creates both tables; `AddTemporalTables` and `SetTemporalRetention` add history. No other migration is specific to supply lists. Schema changes go through `dotnet ef migrations add` (see `CLAUDE.md` section 4).

## Frontend

### Where it lives

Supply lists are the **Liste** tab of the Items page (`features/items/page`; the tab bar is hidden and driven by the header). `ItemsPage` provides `SupplyListListState`, joins the `supply-lists-list` SignalR group for its lifetime, and opens the detail modal for **Adaugă listă** and for a row.

### Pieces

| Piece | Role |
|---|---|
| `SupplyListsHttp` | Wraps every route. `getAllActive()` reads the whole active set by following the cursor (the API caps the page size), sorted by name. |
| `withSupplyListCollection()` | Paged list, filter, load more and `toggleActive`; the initial filter is `isActive = true`. |
| `SupplyListListState` | Page store: reloads the list on any of the four `supplyList*` realtime events. |
| `SupplyListDetailState` | Loads one list, creates or updates it; emits a `saveSuccess` event carrying an operation id. |
| `SupplyListDetailModal` + `SupplyListDetailForm` | The editor. A disabled list opens read-only (all fields disabled). Closing with unsaved changes asks "Renunțați la modificări?". |
| `SupplyListTab`, filter form, table and small list | The list UI. The filter form has a search box and a segmented control **Active / Inactive / Toate** (default Active); the table is sortable by name, frequency and last modified, and shows the line count and a per-row enable/disable action. |

Form rules mirror the backend (Romanian messages): name required, max 200; note max 1000; frequency required; interval weeks required and 2 to 52 only for "every X weeks"; each line needs quantity greater than 0 and a unit (max 50), notes max 500. Frequency labels are in `SUPPLY_LIST_FREQUENCY_LABELS`, for example `StartOfSchoolYear` is shown as "La începutul clasei".

### Behaviour worth knowing

- **Disabling asks for confirmation** ("Lista „…” va fi dezactivată și nu va mai putea fi modificată până la reactivare."); enabling does not. After a toggle the list reloads and a message says "Lista a fost activată." or "Lista a fost dezactivată.".
- **Order list wizard:** when an order list is created "from a supply list", `OrderListWizardStore` loads the active lists (`getAllActive`) and, for the selected ones, each full list (`getById`). `supplyListsToLines` merges them **in the browser** by item: quantities are added up, distinct notes are joined, the unit is the first list's. The order name is the list names joined with " + " (cut to the order name limit). The order list suggestions panel also reads active supply lists. The backend order list code has no reference to supply lists (no `SupplyList` reference was found under `Features/OrderLists`).
- **Stock:** nothing in the stock feature reads supply lists ([stock.md](stock.md)); the only stock link is indirect, through items.

## Open points

Not fixed here; each needs a decision.

1. **No role on writes.** Create, Update, Enable and Disable are `[Authorize]` only. Supply lists are global data, and `CLAUDE.md` section 10 asks for `[Authorize(Roles = Roles.Administrator)]` on commands that write global/shared data (same finding as for items, locations and categories).
2. **Summing quantities across units.** Both the item merge (`SupplyListLineItemMerger.Combine`) and the order wizard (`supplyListsToLines`) add quantities for the same item without comparing units, so "2 kg" and "3 buc" become 5 in one unit.
3. **Misleading error code for duplicate items.** The "an item can appear only once" rule reuses `ValidationErrorCodes.DuplicateName`, so a client cannot tell a duplicate list name from a duplicate item by code alone.
4. **Disabled items stay on lists.** By design, existing lines keep items that were disabled later (and can still be edited), only new additions are checked. Whether the order wizard or the order list rejects such items when a list is turned into an order was not traced.
5. **Frequency sort order.** The column is text, so sorting by frequency is alphabetical by enum name ("Weekly" after "StartOfSchoolYear"), not logical order; not verified how EF translates the sort.
6. **Frequency labels vs enum names.** `StartOfSchoolYear`, `EndOfSchoolYear` and `MiddleOfSemester` are labelled "La începutul clasei", "La sfârșitul clasei" and "Mijlocul clasei" ("class" rather than school year or semester). It may be intentional (a class is the school-year unit in this app); not confirmed.
7. **Cache eviction relies on the commands.** The event handlers do not clear cache tags (unlike item and category handlers); correctness depends on each command's `ICacheInvalidation` tags. A new code path that changes a list without going through these commands would not evict the cached list.
8. **No delete and no e2e coverage.** Lists can only be disabled, never removed, and no Playwright spec covers the Liste tab.
