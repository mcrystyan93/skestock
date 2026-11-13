# Locations

A location is a physical place where stock is kept (the seed data has "Frigider", "Congelator", "Dulapuri", "Rafturi", "Camara", "Depozit"). Stock batches, stock transactions, class balances, class item stock visibility, per-item thresholds, goods-receipt import lines and daily consumption statistics all reference a location, and an item can name one as its default.

Unlike categories and items, locations have **no management page in the client**: the API can create and update them, but the Angular app only reads them (dropdowns). See [Open points](#open-points).

UI copy is Romanian. Everything below was read from source; gaps and oddities are listed under Open points, not fixed. Related: [categories.md](categories.md), [items.md](items.md) (each on its own open PR until merged).

## At a glance

| Layer | Where |
|---|---|
| Domain | `src/Domain/Entities/Locations/Location.cs` |
| Application | `src/Application/Features/Locations/` (create/update commands, three queries, filter/sort/cursor/cache config), `Application/Common/Errors/LocationErrors.cs` |
| Infrastructure | `Data/Configurations/LocationConfiguration.cs`; seeding in `Data/ApplicationDbContextInitialiser.cs` |
| Web | `src/Web/Endpoints/Locations.cs` |
| Client | `src/Client/src/app/shared/locations` (HTTP, store feature, dropdown), `core/models/location.ts` |

There are no domain events, no realtime events and no queue/Worker involvement for locations.

## Backend

### Endpoints (`/api/Locations`)

The group requires an authenticated user (`src/Web/Infrastructure/WebApplicationExtensions.cs:28`). No Locations command or query carries `[Authorize]`.

| Method + route | Use case | Notes |
|---|---|---|
| `POST get-all` | `GetAllLocationsQuery` | Keyset-paginated, filter + sort + search. Cached. |
| `GET default` | `GetDefaultLocationQuery` | The location flagged `IsDefault`, or `200 OK` with `null` when none. Cached. |
| `GET {id}` | `GetLocationByIdQuery` | 404 `locations.not_found`. Not cached. |
| `POST` | `CreateLocationCommand` | 201 with `LocationDto`. |
| `PUT {id}` | `UpdateLocationCommand` | Replaces name, type and parent. |

There is no delete. Every foreign key to `Locations` is `Restrict`.

### Fields and validation

| Field | Rule |
|---|---|
| `Name` | required, max 100, trimmed; must be unique (trimmed, case- and accent-insensitive via `TextSearchCollation.AccentInsensitive`; own id excluded on update) → `DuplicateName` |
| `Type` | required, max 50, trimmed. Free text (the code comment suggests values like "Kitchen", "StorageRoom"; the seed data uses Romanian words such as "Bucatarie"). It is not an enum or a lookup. |
| `ParentLocationId` | optional; if given, the parent must exist (`InvalidReference`) |

Update adds two hierarchy rules (`UpdateLocationCommandValidator`): a location cannot be its own parent (`SelfReference`), and the new parent's ancestor chain must not contain the location (`CircularReference`). The chain walk is capped at 100 levels; a deeper or runaway chain is treated as circular. Create needs no cycle check because the new location has no children yet.

The name limit is duplicated in the validators (`NameMaxLength = 100`) and `DataSchemaConstants.DEFAULT_NAME_LENGTH` (Application cannot reference Infrastructure); keep them in sync.

### The default location

`Location.IsDefault` marks the location that forms pre-select (see Frontend). **Nothing in the app sets it**: Create and Update have no `IsDefault` input, and there is no "set default" command (the `GetDefaultLocationQuery` code comment calls its dedicated `location:default` tag future-proofing for a possible "set default" command). It is set only by seeding, where `Camara` is the default; the seeder adds missing names only, so it never changes an existing database's default. `GetDefaultLocation` returns the first row with `IsDefault`; nothing prevents several.

### Listing, filtering and sorting

- Filter fields (`LocationFilterConfiguration`): `id`, `name`, `type`, `parentLocationId`, `createdDate`, `lastModifiedDate`. There is no `isDefault` filter.
- Sort keys (`LocationSortConfiguration`): `name`, `type`, `createdDate`, `lastModifiedDate`, `id`, each with `Id` as the keyset tie-breaker. Default: `CreatedDate desc, Id desc`.
- `searchTerm` is an accent-insensitive `Contains` on `Name`.
- The page size is validated by `GetAllLocationsQueryValidator` (the handler also clamps it); the handler fetches `pageSize + 1` rows to compute `hasNextPage`. The cursor encodes the sort it was issued for.
- `LocationDto` carries `ParentLocationName` (resolved from the parent) and the audit names.

### Caching

- `GetAllLocationsQuery` (tag `locations`) and `GetDefaultLocationQuery` (tags `locations`, `location:default`) are `ICacheableQuery`, sliding expiration about 5 minutes with 30 s jitter. `GetLocationByIdQuery` is not cached.
- Create and Update invalidate `locations`, which also clears the default entry.
- Unlike categories and items, an update does not invalidate stock caches or notify other sessions, so stock listings that embed a location name or parent can stay stale until their own expiry. (Whether those listings embed location data was not traced.)

### Mutation responses

Create and Update return a `LocationDto` built by hand after `SaveChanges`:

- `IsDefault` is not set, so it is always `false` in these responses, even for the default location.
- The audit names (`CreatedByName`, `LastModifiedByName`) come from navigations that are not loaded, so they are normally `null`; the comment in the code says this is by design and mirrors the category handler.
- `ParentLocationName` is resolved with a follow-up query.

Reading the location again (`GET {id}`) returns the full values.

### Where locations are used

- **Items**: an optional default location (`Item.LocationId`, pre-filled when receiving or adding stock) and per-location minimum thresholds (`ItemLocationThreshold`). See [items.md](items.md).
- **Stock**: batches, transactions, class balances and class item stock visibility are per location. `GetClassLocationStockQuery` (`POST /api/Stock/class/{classId}`) builds the per-class stock report across locations.
- **Statistics**: `GetClassLocationStockByItemQuery` drills the class stock chart into one location (`[Authorize]`, cached per class and location). `DailyItemConsumption` also carries a location.
- **Goods receipts**: import lines carry a location, pre-filled with the default in the review table.

### Errors

Only `locations.not_found` (404). Validation failures use `ValidationErrorCodes`.

### Tests

- Unit: `tests/Application.UnitTests/Features/Locations/` (create, update, get-all, get-by-id, get-default: handlers and validators), plus `Stock/Queries/GetClassLocationStock` and `Statistics/Queries/GetClassLocationStockByItem`.
- Functional: `tests/Application.FunctionalTests/Features/Locations/` (create, update, get-all, get-by-id, get-default) and `Stock/Queries/GetClassLocationStock`.
- Client: colocated specs for the dropdown component and the dropdown store.
- The functional update tests cover both the self-reference and the circular-hierarchy rules (`UpdateLocationCommandTests.cs`). No test was found for the seeder's default flag.

## SQL

`Locations` is a SQL Server temporal table with 2-year history retention (`SetTemporalRetention`).

| Column | Type | Notes |
|---|---|---|
| `Id` | `uniqueidentifier` | PK |
| `Name` | `nvarchar(100)` | not null; **no unique index** |
| `Type` | `nvarchar(50)` | not null |
| `IsDefault` | `bit` | not null, default `0` |
| `ParentLocationId` | `uniqueidentifier` null | self-reference, `Restrict` (SQL Server rejects cascade here) |
| `CreatedDate`, `LastModifiedDate` | `datetimeoffset` | UTC |
| `CreatedById`, `LastModifiedById` | `uniqueidentifier` null | FK to `UserProfiles.IdentityId`, `Restrict` |

Indexes in `InitialCreate`: `IX_Locations_CreatedById`, `IX_Locations_LastModifiedById`, `IX_Locations_ParentLocationId`. There are **no composite keyset indexes** (no `(CreatedDate, Id)`, `(Name, Id)`, `(Type, Id)`), unlike categories and items; the table is small, so this is probably fine.

Tables referencing `Locations` (each `LocationId` column): `Items` (default location, `AddItemDefaultLocation`), `ItemLocationThresholds` (`AddItemLocationThresholds`), `StockBatches`, `StockTransactions`, `ClassBalances`, `ClassItemStockVisibilities` (`AddLocationToClassItemStockVisibility`), `GoodsReceiptImportLines`, `DailyItemConsumptions` (`AddDailyItemConsumption`).

Migrations touching the table or its references: `InitialCreate`, `AddLocationToClassItemStockVisibility`, `AddDailyItemConsumption`, `AddItemLocationThresholds`, `AddTemporalTables`, `SetTemporalRetention`, `AddItemDefaultLocation`, and `AddStockReportQueryIndexes` (adds two indexes that include `LocationId`, on `StockTransactions` and `StockBatches`). Schema changes go through `dotnet ef migrations add` (see `CLAUDE.md` section 4).

## Frontend

There is no locations route, page or modal: locations are managed nowhere in the UI. The client only reads them.

### Pieces (`shared/locations`)

| Piece | Role |
|---|---|
| `LocationsHttp` | Wraps every `/api/Locations` route (`getAll`, `getAllPages`, `getById`, `getDefault`, `create`, `update`). `create` and `update` exist but nothing in the app calls them. `getAllPages` fetches every page, for screens that need all locations at once. |
| `withLocationCollection()` | Paged location list, filter and load more; also loads the default location. Shared with the dropdown store. |
| `LocationDropdownStore` | Backs the dropdown. |
| `LocationDropdown` (`ske-location-dropdown`) | Searchable select with inputs `disabled`, `allowClear`, `placeholder` ("Selectați o locație"), `prePopulateWithDefault`, `excludedLocationId`, `loadOnInit`. |

Types are in `core/models/location.ts` (mirror `LocationDto` and the request records).

### Where the dropdown appears

Item form (default location), stock batch form, stock list filter, stock move form (`excludedLocationId` hides the item's current location as a destination), goods-receipt review lines (`prePopulateWithDefault`, both the table and the small layout) and the class statistics daily-consumption card. The item form also loads all locations through `getAllPages` to render one threshold input per location.

## Open points

Not fixed here; each needs a decision.

1. **No UI to manage locations.** Create and Update exist in the API (and in `LocationsHttp`) but no screen uses them, so new locations can only be added through the API or the seeder. Whether that is intended was not established.
2. **Authorization on writes.** Create and Update have no role attribute. `CLAUDE.md` section 10 asks for `[Authorize(Roles = Roles.Administrator)]` on commands that write global/shared data; locations are global.
3. **`IsDefault` has no writer.** No command sets it, several rows can carry it, and `GetDefaultLocation` just takes the first. Changing the default today needs a database edit.
4. **Mutation responses are incomplete.** `IsDefault` is always `false` and the audit names are `null` in Create/Update responses (see above).
5. **Name uniqueness is validator-only** (no unique index), so concurrent creates can duplicate a name. Same pattern as categories.
6. **No cache or realtime fan-out on update.** Renaming or re-parenting a location does not invalidate stock caches or notify other open sessions, unlike category and item updates.
7. **`Type` is free text** with no list of allowed values or UI to pick from, so spelling variants ("Bucatarie" vs "Bucătărie") are possible.
