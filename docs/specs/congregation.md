# Spec: Congregation

Status: APPROVED. Progress is ticked in `tasks/congregation-todo.md` during implementation.

## 1. Objective

Staff manage congregations (name, contacts, whether they can receive students, whether they are small). A congregation can be listed, filtered, created, edited, soft-deleted, and picked through a reusable dropdown. No other entity references congregations yet (CRUD only).

Reference: `Category` (slice `Application/Features/Categories`, `shared/categories`, `features/categories`). Where Category has gaps, Congregation is built fresh, not copied:
- Category has no delete (store `deleteCategory` is a stub, no endpoint, no button).
- Category's realtime `categoryUpdated` reducer is a no-op and `CategoryCreated` is never emitted. Congregation implements working realtime. Category is not touched.
- No soft delete exists anywhere in the repo. Congregation introduces it (explicit user decision), see 3.2.

## 2. Decisions taken (from Phase 2)

| # | Decision |
|---|---|
| Base class | `BaseAuditableEntity` + `IKeysetEntity` (`TrailableEntity` does not exist) |
| Property names | `Name`, `Email`, `Coordinator`, `CoordinatorPhone`, `DessertContactPerson`, `DessertContactPhone`, `CanReceiveStudents`, `IsSmallCongregation`, `Message` ("desert" = dessert) |
| Required | `Name`, `Email`. Everything else optional |
| Max lengths | Name 100, Email 200, Coordinator 100, DessertContactPerson 100, CoordinatorPhone 30, DessertContactPhone 30, Message 1000 |
| Unique | `Name`, case- and accent-insensitive, among non-deleted rows |
| Defaults | `CanReceiveStudents = false`, `IsSmallCongregation = false` |
| Delete | Soft delete |
| References | None (CRUD only) |
| List | Keyset pagination, search by name, sort, filters: text `name`, segmented `canReceiveStudents` and `isSmallCongregation` (All / Yes / No) |
| Dropdown | Same mechanism as Category: get-all with search and infinite scroll; extra optional filter on `canReceiveStudents`; ordered by `Name` asc; supports inline create and edit |
| Realtime | Working SignalR updates for created / updated / deleted |
| Menu | New sidebar entry, route `/congregations` |
| Permissions | Any authenticated user |
| Import | Not included |
| Language | Code, routes, tests, commits, migrations in English. UI copy in Romanian |

## 3. Assumptions to confirm at approval (not asked yet)

1. **Email format** is validated (`EmailAddress`) since Email is required. Phones are free text (max 30).
2. **UI diacritics**: Romanian copy uses proper diacritics ("Congregație", "Șterge"). Existing Category copy mostly omits them; Congregation does not follow that.
3. **Soft delete design** (3.2 below) is my proposal; it is new to the repo.
4. **Deleted rows are never shown** and cannot be restored from the UI. Deleting an already-deleted or missing id returns 404.
5. **`CanReceiveStudents` dropdown filter** goes through the generic `Filters` list (boolean filters are already supported), not a dedicated request field.
6. Sidebar placement: directly after "Categorii" in `core/layouts/full/full.html`.

### 3.1 Data model

`Congregation : BaseAuditableEntity, IKeysetEntity`, namespace `skestock.Domain.Entities.Congregations`.

| Property | Type | DB |
|---|---|---|
| Name | string | nvarchar(100), required |
| Email | string | nvarchar(200), required |
| Coordinator | string? | nvarchar(100) |
| CoordinatorPhone | string? | nvarchar(30) |
| DessertContactPerson | string? | nvarchar(100) |
| DessertContactPhone | string? | nvarchar(30) |
| CanReceiveStudents | bool | default false |
| IsSmallCongregation | bool | default false |
| Message | string? | nvarchar(1000) |
| DeletedDate | DateTimeOffset? | null = active (UTC) |

### 3.2 Soft delete

- `Congregation.DeletedDate` (nullable UTC). No extra interface (YAGNI: single entity).
- `CongregationConfiguration`: `HasQueryFilter(c => c.DeletedDate == null)`.
- Unique filtered index `UX_Congregations_Name` on `Name` WHERE `[DeletedDate] IS NULL`, so a deleted name can be reused. The validator check is case/accent-insensitive via `TextSearchCollation.AccentInsensitive`; verify the index collation matches (column collation, or rely on validator plus the index as a race backstop).
- Keyset indexes: `IX_Congregations_{CreatedDate|LastModifiedDate|Name}_Id` (same pattern as Category).
- Delete handler sets `DeletedDate = UtcNow` (via `TimeProvider` if the repo uses it; otherwise follow what audit code uses) and saves, so `LastModified*` audit fields update.
- Migration: new table only, so backward-safe. Name `AddCongregations`.

## 4. API contract

Group `Congregations`, prefix `/api/Congregations`, default `RequireAuthorization()` applies.

| Verb + route | Request | Response |
|---|---|---|
| POST `get-all` | `GetAllCongregationsRequest { Filters, Sort, Cursor, SearchTerm, PageSize }` | 200 `PaginatedResponse<CongregationDto>` |
| GET `{id}` | none | 200 `CongregationDto`, 404 `congregations.not_found` |
| POST `` | `CreateCongregationRequest` (all editable fields) | 201 `CongregationMutationDto`, 400 validation (incl. `DuplicateName`) |
| PUT `{id}` | `UpdateCongregationRequest` (all editable fields) | 200 `CongregationMutationDto`, 404, 400 |
| DELETE `{id}` | none | 204, 404 |

`CongregationDto`: `Id, Name, Email, Coordinator, CoordinatorPhone, DessertContactPerson, DessertContactPhone, CanReceiveStudents, IsSmallCongregation, Message, CreatedByName, LastModifiedByName, CreatedDate, LastModifiedDate`.
`CongregationMutationDto`: the same minus audit names.

Filters (`CongregationFilterConfiguration`): `id`, `name`, `canReceiveStudents` (bool), `isSmallCongregation` (bool), `createdDate`, `lastModifiedDate`. Sort keys: `Name`, `CreatedDate`, `LastModifiedDate` (default `CreatedDate desc, Id desc`, same as Category). Dropdown call uses `Sort = Name asc` and, to show only congregations that can receive students, `Filters = [{ field: "canReceiveStudents", operator: "Equals", value: true }]` (no dedicated request field: the generic boolean filter already works).

Caching: `GetAllCongregationsQuery : ICacheableQuery` (tag `congregations`, 5 min randomized sliding, same as Category). Create / Update / Delete implement `ICacheInvalidation` with tag `congregations`. `GetCongregationByIdQuery` uncached (same as Category).

Realtime: domain events `CongregationCreatedEvent`, `CongregationUpdatedEvent`, `CongregationDeletedEvent` raised by the handlers; one event handler each sends `RealtimeEvents.Congregation{Created|Updated|Deleted}` to group `congregations-list` (`RealtimeGroups`).

Errors: `CongregationErrors.NotFound` (`congregations.not_found`, 404).

## 5. Acceptance criteria

Backend
- AC-B1 Create persists all fields, trims strings, defaults booleans to false, returns 201 with id.
- AC-B2 Create/Update reject empty or >100 Name, empty or invalid or >200 Email, and over-length optional fields, with `ValidationErrorCodes` (Required / MaxLength).
- AC-B3 Create/Update reject a duplicate Name (case- and accent-insensitive) among non-deleted congregations; a name of a deleted congregation can be reused; Update may keep its own name.
- AC-B4 Update changes all fields and updates audit fields; unknown or deleted id returns `congregations.not_found`.
- AC-B5 Delete sets `DeletedDate`; row no longer appears in get-all or get-by-id; unknown or already-deleted id returns not found.
- AC-B6 Get-all supports keyset pagination, name search, sorting, filters on name and both booleans, (a `canReceiveStudents = true` filter returns only matching rows, used by the dropdown).
- AC-B7 Cache tag `congregations` is invalidated by Create / Update / Delete.
- AC-B8 Events are raised and realtime messages sent for create / update / delete.
- AC-B9 `dotnet ef migrations has-pending-model-changes` reports none; `dotnet build` clean.

Frontend
- AC-F1 `/congregations` is lazy-loaded and reachable from a new sidebar item.
- AC-F2 At large breakpoint the page shows a table; below it a list. Both show name, email, coordinator, flags, and offer edit and delete.
- AC-F3 The segmented filters (All / Yes / No) for "poate primi cursanți" and "congregație mică" and the name search filter the list server-side.
- AC-F4 "Adaugă" opens a modal form (Signal Forms) with required Name and Email, max lengths matching the backend; save creates and refreshes the list; a duplicate-name server error shows on the field.
- AC-F5 Edit opens the same modal prefilled; save updates the row.
- AC-F6 Delete opens a confirm dialog; confirming calls DELETE and removes the row; cancelling does nothing.
- AC-F7 `<ske-congregation-dropdown>` selects by id (FormValueControl), searches with debounce, infinite scroll, inline create/edit like Category, and input `canReceiveStudents` limits options to congregations that can receive students.
- AC-F8 Another client sees create / update / delete live through SignalR.
- AC-F9 `npm run build` and `npm test -- --watch=false` pass; new store, dropdown and list components have colocated specs.

## 6. Frontend architecture

Mirrors Category layout, container / presentation split:
- `shared/congregations/` (barrel `index.ts`, alias `@ske/shared/congregations`)
  - `services/congregations.http.ts` (get-all, getById, create, update, delete)
  - `services/congregation-collection.feature.ts` (shared `withCongregationCollection` incl. loadMore and realtime reducers that actually update/remove/insert)
  - `services/congregation-detail.store.ts`, `services/congregation-dropdown.store.ts` (NgRx Signal Stores)
  - `ui/modals/detail/` modal shell (container, loads detail, saves) + `form.ts` (presentation, Signal Forms: `required`, `email`, `maxLength`)
  - `ui/dropdown/congregation-dropdown.ts` (presentation + store; inputs `allowClear`, `allowEdit`, `allowCreate`, `placeholder`, `canReceiveStudents`)
- `core/models/congregation.ts` (DTOs, table columns, `buildCongregationListFilter`)
- `features/congregations/` (barrel, lazy route registered in `core/layouts/full/full.routes.ts`)
  - `list/congregations.page.ts/html` (container: provides `CongregationListState`, joins `congregations-list` group, owns delete confirm)
  - `list/filter/` filter form (`nz-segmented` x2 + search)
  - `list/tables/large/table.ts` (nz-table) and `small/congregation-list-small.ts` (nz-list), presentational, outputs `edit` and `delete`
  - `services/congregation-list.store.ts` with working `deleteCongregation` (HTTP DELETE, error handling)
- UI strings Romanian; NG-ZORRO APIs checked in `.github/instructions/llms-full.txt` before use.

## 7. Backend tests to write first (NUnit + Shouldly + Moq, in-memory test DbContext like Category)

Unit (`tests/Application.UnitTests/Features/Congregations/...`):
1. `CreateCongregationCommandValidatorTests`: required Name/Email, invalid email, each max length, duplicate name (case/accent), name of soft-deleted allowed.
2. `CreateCongregationCommandHandlerTests`: persists + trims, booleans default false, returns id, raises created event.
3. `UpdateCongregationCommandValidatorTests`: same rules, own name allowed, duplicate with another rejected.
4. `UpdateCongregationCommandHandlerTests`: updates all fields, not found, deleted → not found, raises updated event.
5. `DeleteCongregationCommandHandlerTests`: sets DeletedDate, not found, double delete → not found, raises deleted event.
6. `GetAllCongregationsHandlerTests`: excludes deleted, search, boolean filters (incl. `canReceiveStudents`), keyset paging across pages, default sort.
7. `GetAllCongregationsQueryValidatorTests` (invalid sort / cursor / page size, as Category).
8. `GetCongregationByIdHandlerTests`: found, not found, deleted → not found.
9. Cache-invalidation tag tests for the three commands; `SortConfigurationConsistencyTests` gets the new sort config.
10. Event handler tests: tags cleared, realtime message sent to `congregations-list`.

Functional (`tests/Application.FunctionalTests/Features/Congregations/`): create→get→update→delete round trip over HTTP, duplicate name 400, get-all excludes deleted, unauthenticated 401. (Run through `./run-functional-tests.sh`.)

## 8. Tech stack, commands, structure and style

Stack: .NET 10, ASP.NET Core minimal APIs, Mediator (source-generated), FluentValidation, FluentResults, EF Core 10 + SQL Server, HybridCache, SignalR; Angular 22 (standalone, signals), NG-ZORRO 22, `@ngrx/signals`, Vitest; tests NUnit + Shouldly + Moq.

### 8.1 Commands

```
dotnet build
dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~Congregation"
./run-functional-tests.sh --filter "FullyQualifiedName~Congregation"
dotnet ef migrations add AddCongregations --project src/Infrastructure --startup-project src/Web --context ApplicationDbContext --output-dir Data/Migrations
dotnet ef migrations has-pending-model-changes --project src/Infrastructure --startup-project src/Web --context ApplicationDbContext
cd src/Client && npm run build && npm test -- --watch=false
```

### 8.2 Project structure

```
src/Domain/Entities/Congregations/Congregation.cs
src/Domain/Events/Congregations/Congregation{Created,Updated,Deleted}Event.cs
src/Application/Features/Congregations/
  Commands/{CreateCongregation,UpdateCongregation,DeleteCongregation}/   command, handler, validator
  Queries/{GetAllCongregations,GetCongregationById}/                     query, handler, validator
  EventHandlers/Congregation{Created,Updated,Deleted}EventHandler.cs
  Models/{CongregationDto,CongregationMutationDto,CongregationRequests}.cs
  CacheConstants.cs  CongregationFilterConfiguration.cs  CongregationSortConfiguration.cs  CongregationCursor.cs
src/Application/Common/Errors/CongregationErrors.cs
src/Infrastructure/Data/Configurations/CongregationConfiguration.cs
src/Infrastructure/Data/Migrations/<timestamp>_AddCongregations.cs      generated, never hand-edited
src/Web/Endpoints/Congregations.cs
tests/Application.UnitTests/Features/Congregations/{Commands,Queries,EventHandlers}/<UseCase>/
tests/Application.FunctionalTests/Features/Congregations/
src/Client/src/app/core/models/congregation.ts
src/Client/src/app/shared/congregations/{services,ui/dropdown,ui/modals/detail}/ + index.ts
src/Client/src/app/features/congregations/{list,list/filter,list/tables,services}/ + index.ts
docs/specs/congregation.md   tasks/congregation-plan.md   tasks/congregation-todo.md
```

### 8.3 Code style (backend, from `CreateCategoryCommandHandler`; frontend follows `shared/categories`)

```csharp
namespace skestock.Application.Features.Congregations.Commands.CreateCongregation;

public class CreateCongregationCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<CreateCongregationCommand, Result<CongregationMutationDto>>
{
    public async ValueTask<Result<CongregationMutationDto>> Handle(
        CreateCongregationCommand request, CancellationToken cancellationToken)
    {
        var congregation = new Congregation { Name = request.Name.Trim(), /* ... */ };

        dbContext.Congregations.Add(congregation);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok(new CongregationMutationDto { Id = congregation.Id, Name = congregation.Name /* ... */ });
    }
}
```

Conventions: file-scoped namespaces, primary constructors, file named after its primary type, `Result`/typed errors for business failures, `CancellationToken` through the chain, English names, comments only for the non-obvious why. Frontend: `inject()`, signals, `input()`/`output()`, native control flow, no explicit `standalone`/`OnPush`, no `any`, NG-ZORRO components, Romanian UI copy.

## 9. Boundaries

- Always: failing test before production code (backend); Mediator pipeline order untouched; `Result` for business failures; `skestock.Shared.Services` for any shared names; UTC `DateTimeOffset`; no Prettier on client files; branch `feature/congregations` and PR at the end.
- Ask first: any change to Category code, shared Behaviours/Filtering/Keyset/Caching (fragile), adding dependencies, any change beyond CRUD (references, import, restore).
- Never: edit applied migrations or the snapshot by hand; commit to `master`; run migrations against production.

## 10. Plan and tasks

Plan: `tasks/congregation-plan.md`. Task list and progress: `tasks/congregation-todo.md` (ticked during implementation).

## 11. Open questions

None blocking; section 3 lists assumptions that become decisions on approval.
