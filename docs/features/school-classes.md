# School classes

A school class (clasă) is the unit that owns stock and everything around it: its stock batches and transactions, goods receipts and their imports, order lists, departments, invitation counts, fruit-and-dessert plan and statistics. A class has a name, a date range and a status. Closing a class moves all of its remaining stock to another class and makes the closed class read-only.

This document covers `src/Application/Features/SchoolClasses` and the pages that host it. The overview page is a container for several other features; each is described in its own document and only referenced here: [stock.md](stock.md), [order-lists.md](order-lists.md), [goods-receipts.md](goods-receipts.md). The class-wide default department templates, invitation counts and room seats (`Features/ClassConfiguration`, `ConfigurationPage`) are the source a new class copies from; they are not documented here beyond that. The fruit-and-dessert plan and the statistics tab are not covered.

UI copy is Romanian. Everything below was read from source; gaps and oddities are listed under [Open points](#open-points), not fixed. Related: [items.md](items.md), [locations.md](locations.md), [categories.md](categories.md), [supply-lists.md](supply-lists.md), [stock.md](stock.md), [order-lists.md](order-lists.md), [goods-receipts.md](goods-receipts.md) (each on its own PR until merged, so these links are dead on `master` until then).

## At a glance

| Layer | Where |
|---|---|
| Domain | `src/Domain/Entities/SchoolClasses/` (`SchoolClass`, `ClassDepartment`, `DepartmentTemplate`, `SharedClassConfiguration`), `Domain/Enums/ClassStatus.cs` |
| Application | `src/Application/Features/SchoolClasses/` (commands, queries, models, filter/sort/cursor/cache config), errors in `Application/Common/Errors/SchoolClassErrors.cs`, the closed-class machinery in `Application/Common/Behaviours/` (`IRequiresOpenClass`, `ClassOpenBehaviour`, `ClassLockBehaviour`, `ClassLocking`) |
| Infrastructure | `Data/Configurations/SchoolClassConfiguration.cs`, `ClassDepartmentConfiguration.cs`, `DepartmentTemplateConfiguration.cs`, `SharedClassConfigurationConfiguration.cs` |
| Web | `src/Web/Endpoints/SchoolClasses.cs` |
| Client | `src/Client/src/app/features/school-classes` (list page, overview page and its tabs), `shared/school-classes` (HTTP, stores, form modal, dropdown, read-only token), `core/models/school-class.ts` |

There are no domain events, no realtime events of its own and no Worker or queue involvement for school classes.

## Data model

| Entity | Meaning |
|---|---|
| `SchoolClass` | `Name`, `StartDate`, `EndDate`, `Status`, six nullable invitation counts (`InvitationCount`, `StudentInvitationCount`, `InstructorInvitationCount`, `OpeningPrayerInvitationCount`, `ExperienceInvitationCount`, `ChangeInvitationCount`), and navigations to `Departments`, `BatchesReceived`, `Transactions`, `Balances` and `GoodsReceipts`. |
| `ClassDepartment` | A department of one class: `Name`, `Responsibilities`, optional `ResponsiblePerson`. |
| `DepartmentTemplate` / `SharedClassConfiguration` | The shared (singleton, `Id = 1`) configuration: invitation counts, room seat counts, and department templates. A new class copies the counts and the templates from it. |

`ClassStatus` (persisted as `int`, append only): `Upcoming = 0`, `Active = 1`, `Paused = 2`, `Closed = 3`. Only `Closed` has behaviour: it makes the class read-only. `Upcoming`, `Active` and `Paused` are labels the user can switch between freely.

## Backend

### Endpoints (`/api/SchoolClasses`)

The group requires an authenticated user (`src/Web/Infrastructure/WebApplicationExtensions.cs:28`).

| Method + route | Use case | Notes |
|---|---|---|
| `POST get-all` | `GetAllSchoolClassesQuery` | Keyset-paginated, filter + sort + search. Cached. |
| `GET {id}` | `GetSchoolClassByIdQuery` | Full class including invitation counts and departments (ordered by name). Not cached. |
| `GET {id}/summary` | `GetSchoolClassSummaryQuery` | Counts for the overview header. Not cached. |
| `POST` | `CreateSchoolClassCommand` | 201 with `SchoolClassDto`. |
| `PUT {id}` | `UpdateSchoolClassCommand` | Name, dates and status. |
| `PUT {id}/departments/{departmentId}/responsible-person` | `UpdateClassDepartmentResponsiblePersonCommand` | Sets or clears the responsible person. |
| `POST {id}/initialize-configuration` | `InitializeSchoolClassConfigurationCommand` | Copies the shared configuration into the class. |
| `POST {id}/close` | `CloseSchoolClassCommand` | Administrator only. Body: `targetClassId`. 204. |

There is no delete and no reopen.

### Creating and editing a class

- **Create:** `Name` required, max 100, unique (trimmed, case- and accent-insensitive via `TextSearchCollation.AccentInsensitive`) → `DuplicateName`; `Status` must be a defined value; `StartDate < EndDate` (`InvalidDateRange`). A shared class configuration must already exist, otherwise `ConfigurationRequired` (409). The new class copies the six invitation counts and creates one `ClassDepartment` per template. The name limit is duplicated in the validators and `DataSchemaConstants.DEFAULT_NAME_LENGTH`; keep them in sync.
- **Update:** same name, date and status rules (name uniqueness excludes the class itself). Changing the status **into or out of `Closed`** is refused with `StatusChangeNotAllowed` (409): closing only happens through the close command, and there is no way back. Switching between `Upcoming`, `Active` and `Paused` is allowed. Update is intentionally not `IRequiresOpenClass`, so a closed class can still be renamed or re-dated.
- Neither command has a role attribute.

### Initialize configuration

`InitializeSchoolClassConfigurationCommand` (`IRequiresOpenClass`, so refused on a closed class) copies the shared configuration into a class whose `InvitationCount` is still `null`: the six counts, plus one new `ClassDepartment` per template. A single conditional `ExecuteUpdate` (`WHERE InvitationCount IS NULL`) claims the class, so concurrent requests initialize it at most once; the loser gets `ConfigurationAlreadyInitialized` (409). The update stamps `LastModifiedById` by hand because `ExecuteUpdate` bypasses the audit interceptor. Without a saved shared configuration it fails with `ConfigurationRequired`. Classes created through `Create` already have their counts, so this command is for classes that were never initialized (for example created before the shared configuration existed; that origin is inferred from the nullable column and the migration, not stated in code). The client's `IsConfigurationInitialized` flag is `InvitationCount.HasValue`.

### Responsible person

`UpdateClassDepartmentResponsiblePersonCommand` (`IRequiresOpenClass`) finds the department inside the class and stores the trimmed value, or `null` for a blank. A department that is not in the class returns `ClassDepartmentNotFound` (404); an unknown class returns `school_classes.not_found`. There is no validator on this command or on initialize.

### Closing a class

`CloseSchoolClassCommand` (`[Authorize(Roles = Roles.Administrator)]`) rolls a class's stock over to another class:

1. The validator requires both ids and different classes (`SelfReference`).
2. The handler runs in a transaction and takes an **exclusive lock** on both class rows (`XLOCK, HOLDLOCK`), which waits for in-flight changes to either class and blocks new ones until commit.
3. Source or target missing → `school_classes.not_found`; source already closed → `AlreadyClosed`; target closed → `TargetNotOpen` (all 409/404 as shown under Errors).
4. For every batch received by the source class (zero-quantity batches included): the source `ClassBalance` for that (item, location) gets `ClosingQty` set to 0 if null; if the batch has stock, `ClosingQty` grows by that quantity, the target's `OpeningQty` grows by it, a **new target batch** is created with the same item, location, quantity, expiry date, received date, unit price and goods receipt, the source batch goes to 0, and two `Rollover` transactions are written (negative on the source, positive on the target).
5. The source status becomes `Closed`, and one `SaveChanges` commits everything. A concurrent change to a source batch (its `rowversion`) returns `ConcurrentChange` (409).

`ClassBalance` has no unique index, so the handler looks rows up in memory before adding one. Invalidated tags: the school class list, the stock coarse tag, both classes' stock tags and the stock batch list.

### The closed-class guard

Commands that change a class's data implement `IRequiresOpenClass` with exactly one of `ClassId`, `OrderListId` or `GoodsReceiptImportId`. Two pipeline behaviours enforce it:

- `ClassOpenBehaviour` (before validation) rejects the request early with `SchoolClassClosed` (409) when the class is closed; an unknown class falls through so the handler returns its own not-found error.
- `ClassLockBehaviour` (innermost) opens a transaction and takes a **shared lock** (`HOLDLOCK`) on the class row, then repeats the closed check under the lock, so closing cannot race an in-flight change. It resolves the class from an order list or import when needed.
- The item merge takes `ExclusiveLockAllClasses` for the same purpose and deliberately is not `IRequiresOpenClass` (see [items.md](items.md#merging-duplicates)).

In `CLAUDE.md` terms, the order Logging → UnhandledException → Authorization → ClassOpen → Validation → Performance → Caching → CacheInvalidation → ClassLock must not change.

### Listing, filtering, sorting and the summary

- **List:** filter fields `id`, `name`, `status`, `startDate`, `endDate`, `createdDate`, `lastModifiedDate`; sort keys `name`, `startDate`, `endDate`, `createdDate`, `lastModifiedDate`, `id` (each with `Id` as tie-breaker); default `CreatedDate desc, Id desc`. `searchTerm` is an accent-insensitive `Contains` on the name. Rows carry `Status`, dates and the audit names only: invitation counts, departments and `IsConfigurationInitialized` are not filled in list rows (it is `false` there). The query is cached with tag `school-classes`, sliding expiration about 5 minutes with 30 s jitter; the handler fetches `pageSize + 1` rows to compute `hasNextPage`.
- **By id:** projects the full class with `IsConfigurationInitialized` and departments ordered by name; unknown id → `school_classes.not_found` (404).
- **Summary** (`SchoolClassSummary`): number of goods receipts and their total amount, the receipts as `{Id, ReceivedAt, TotalAmount}` ordered by date, the number of distinct items that ever had a batch in the class, the number of items below their per-location minimum (an item counts once if any location is low; no threshold means never low), the number of perishable items with expired stock (`Quantity > 0` and `ExpiryDate <= today`, UTC), and the number of goods receipt imports by status (`Processing`, `PendingReview`, `Confirmed`, `Failed`).

### Mutation responses

Create returns the full class (counts, departments) but its audit names are `null` (navigations are not loaded after insert). Update returns a partial `SchoolClassDto` without counts, departments or `IsConfigurationInitialized`, so `IsConfigurationInitialized` is `false` in that response even for an initialized class.

### Errors

`school_classes.not_found` (404), `ClassDepartmentNotFound` (404), and 409 for `ConfigurationRequired`, `ConfigurationAlreadyInitialized`, `StatusChangeNotAllowed`, `SchoolClassClosed`, `AlreadyClosed`, `ConcurrentChange` and `TargetNotOpen`. Only `school_classes.not_found` carries an explicit `ErrorCode` constant. Validation failures use `ValidationErrorCodes`.

### Tests

- Unit: `tests/Application.UnitTests/Features/SchoolClasses/` (create, update, close: handlers and validators; get-all handler and validator; get-by-id handler and validator; summary handler).
- Functional: `tests/Application.FunctionalTests/Features/SchoolClasses/` (create, update, get-all, get-by-id, `CloseSchoolClassEndpointTests`, `ClassLockTests`, `ClosedClassRejectsChangesTests`), plus `tests/Application.FunctionalTests/Features/ClassConfiguration/ClassConfigurationSnapshotTests.cs`, which covers initializing a class (including the repeat and concurrent calls) and the responsible-person update.
- Client: colocated specs for the close store, the form, the dropdown and its store, the small list and the header.
- `InitializeSchoolClassConfiguration` and `UpdateClassDepartmentResponsiblePerson` are covered functionally (see above), by the open-class behaviour test cases in `ClassOpenBehaviourTests`, and by `ClosedClassRejectsChangesTests` for the responsible-person command; there are no unit handler or validator tests for them under `tests/Application.UnitTests/Features/SchoolClasses/`.

## SQL

All four tables are SQL Server temporal tables with 2-year history retention (none is in `TemporalExclusions`).

### `SchoolClasses`

| Column | Type | Notes |
|---|---|---|
| `Id` | `uniqueidentifier` | PK |
| `Name` | `nvarchar(100)` | **no unique index** |
| `StartDate`, `EndDate` | `date` | |
| `Status` | `int` | `ClassStatus` |
| `InvitationCount`, `StudentInvitationCount`, `InstructorInvitationCount`, `OpeningPrayerInvitationCount`, `ExperienceInvitationCount`, `ChangeInvitationCount` | `int` null | `null` means "configuration not initialized" |
| audit columns | | `CreatedDate`, `LastModifiedDate`, `CreatedById`, `LastModifiedById` (FK to `UserProfiles.IdentityId`, `Restrict`) |

Indexes: only the two audit foreign-key indexes (`IX_SchoolClasses_CreatedById`, `IX_SchoolClasses_LastModifiedById`). There are no composite keyset indexes; the table is small.

### Other tables

- `ClassDepartment` (singular): `Id`, `SchoolClassId` (FK, **cascade delete** from the class), `Name` (`nvarchar(100)`), `Responsibilities` (required), `ResponsiblePerson` (null; no length limit configured). Indexed on `SchoolClassId`.
- `SharedClassConfigurations`: the singleton row with the counts and room seat counts (`Id` is never generated; the app uses `Id = 1`).
- `DepartmentTemplate` (singular): `Id`, `SharedClassConfigurationId` (FK, cascade), `Name` (`nvarchar(100)`), `Responsibilities`.
- Classes are referenced by `StockBatches`, `StockTransactions`, `ClassBalances`, `GoodsReceipts`, order lists, goods receipt imports and the fruit-and-dessert plan entries; the class foreign keys are `Restrict` in all of those tables and also in `ClassItemStockVisibilities`, `DailyItemConsumptions` and `ItemPurchaseStatistics`, except the fruit-and-dessert plan entries, which **cascade** on delete.

### Migrations

`InitialCreate` (the class table), `AddSharedClassConfiguration` (`InvitationCount`, `ClassDepartment`, `SharedClassConfigurations`, `DepartmentTemplate`), `AddRoomSeatCounts`, `AddInvitationSplit` (the five split counts on both the class and the shared configuration), `SetDepartmentTemplateNameLimit` and `SetClassDepartmentNameLimit` (both change `Name` from `nvarchar(max)` to `nvarchar(100)`), `AddFruitDessertPlanning`, `AddTemporalTables`, `SetTemporalRetention`. Schema changes go through `dotnet ef migrations add` (see `CLAUDE.md` section 4); never reorder `ClassStatus`.

## Frontend

### Routes and list page

`schoolClassesRoutes` is lazy-loaded at `/school-classes` (`core/layouts/full/full.routes.ts`): the list page at the root and the overview page at `:id`. The list page (`SchoolClassesPage`) shows a filterable table or card list by breakpoint, opens the create/edit modal (`SchoolClassDetailModal` and its form) and navigates to `/school-classes/{id}` to open a class.

The form (`shared/school-classes/ui/modals/form.ts`) validates name (required, max 100), start and end dates (required, end after start) and status (required), with Romanian messages. The status options exclude "Închisă" unless the class is already closed, in which case the status field is disabled: closing and reopening are not possible through the form.

### The overview page

`SchoolClassOverviewPage` is the class workspace. It provides `SchoolClassOverviewStore` and a `CLASS_READ_ONLY` token set to the store's `isClosed` signal; components that change class data hide or disable their actions while it is closed: most read it through `injectClassReadOnly()`, while the departments and invitations tabs read `store.isClosed()` directly. It joins the `school-class` SignalR group of the class (and the goods-receipt-import list group). It has eight tabs behind a hidden tab bar, labelled by the header component:

| Tab | Label | Content |
|---|---|---|
| 0 | Stoc | `ClassStock`, see [stock.md](stock.md) |
| 1 | Comenzi | order lists, see [order-lists.md](order-lists.md) |
| 2 | Comenzi recepționate (Recepții) | goods receipts, see [goods-receipts.md](goods-receipts.md) |
| 3 | Comenzi importate (Importuri) | goods receipt imports, see [goods-receipts.md](goods-receipts.md) |
| 4 | Statistici | statistics tab (not covered here) |
| 5 | Departamente | departments and their responsible persons |
| 6 | Invitații | invitation counts |
| 7 | Fructe și desert | fruit-and-dessert plan (not covered here) |

The selected tab and the receipt id are kept in query-string state.

`SchoolClassOverviewStore` loads the class by id, then the summary for the header. It also holds:
- **Departments and initialization:** if the class has not been initialized, the departments tab offers `ClassConfigurationInitialization` (a card with a button); `initializeConfiguration` calls the endpoint and reloads the class, with the message "Departamentele și numărul de invitații au fost copiate în clasă." Saving a responsible person calls its endpoint, updates the department locally and shows "Responsabilul a fost salvat." Both actions are ignored while the class is closed.
- **Close:** the header shows "Închide clasa" while the class is open. `CloseSchoolClassModal` lists open classes other than the current one (a status filter `notEquals closed` and an id filter, via `CloseSchoolClassStore`), requires choosing a target, and posts the close; on success the header reloads. The header does not check the user's role.

### Pieces

| Piece | Role |
|---|---|
| `SchoolClassesHttp` | Wraps every `/api/SchoolClasses` route (`getAll`, `getById`, `getSummary`, `create`, `update`, `updateDepartmentResponsiblePerson`, `close`, `initializeConfiguration`). |
| `withSchoolClassCollection()` / `SchoolClassListState` | Paged class list; reused by the dropdown and the close modal. |
| `SchoolClassDetailState` | Create and update one class. |
| `SchoolClassDropdown` | Reusable class picker. |
| `SchoolClassOverviewStore`, `CloseSchoolClassStore` | Overview state and the close flow. |

Types are in `core/models/school-class.ts`. The client's `ClassStatus` uses lowercase strings (`'upcoming'`, `'active'`, `'paused'`, `'closed'`, plus a filter-only `'all'`); its header comment says enums serialize as ordinals and no `JsonStringEnumConverter` is registered, but `Web/DependencyInjection.cs:75` registers a camel-case one app-wide.

## Open points

Not fixed here; each needs a decision.

1. **A class can be created as `Closed`.** The create validator only checks that the status is a defined value, and the handler does not reject `Closed`, so the close flow (and its stock rollover) can be bypassed by creating a class that is already closed. Update refuses that transition; create does not.
2. **Authorization.** Only `CloseSchoolClass` is administrator-only. Create, Update, Initialize and the responsible-person update have no role attribute, although `CLAUDE.md` section 10 asks for `[Authorize(Roles = Roles.Administrator)]` on commands that write global data, meaning data not scoped to a class the caller can open. Create and Update fit that description (class records are shared across the whole app); Initialize and the responsible-person update are class-scoped, so whether the rule applies to them is a judgment call. The client's close action is shown to every user (no role check found under `features/school-classes` or `shared/school-classes`), so a non-admin gets a server rejection after choosing a target.
3. **Closing drops the batch `Source`.** The rollover creates target batches without copying `Source`, so donated batches become `Order` (the same gap as in stock move; see [stock.md](stock.md#open-points)).
4. **Closing is irreversible.** There is no reopen command, and the status rules block the status from being edited back. Mistakes need a database change.
5. **Name uniqueness is validator-only** (no unique index), the same pattern as categories and locations.
6. **Thin DTOs.** List rows and the update response omit counts and departments, and `IsConfigurationInitialized` is `false` there; only create and get-by-id fill them. Consumers must not treat the list or update response as the full class.
7. **No refresh for other sessions.** Create, update and close raise no domain event and send no realtime notification, so another open session does not see a rename, status change or close until it reloads. Only the stock and order list events reach the `school-class` group.
8. **Missing validators.** The responsible-person update and the initialize command have no validators; the column for the responsible person has no length limit, and the handler stores any length after trimming.
9. **Terminology.** The tab "Comenzi" is the order lists, "Comenzi recepționate" the goods receipts and "Comenzi importate" the receipt imports; the header chart titled "Evolutia comenzilor" plots goods receipt totals (see [order-lists.md](order-lists.md#open-points)).
10. **Stale comment.** `core/models/school-class.ts` says enums serialize as ordinals and no `JsonStringEnumConverter` is registered; one is.
11. **Test gaps** listed under Backend > Tests.
