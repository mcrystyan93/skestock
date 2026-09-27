# Categories

Categories can be managed directly or proposed by documents and added after
review. The Angular page is at `/categories`, under
[`CategoriesPage`](../../../src/Client/src/app/features/categories/list/categories.page.ts)
and the [category routes](../../../src/Client/src/app/core/layouts/full/full.routes.ts).
Its two tabs show categories and category import batches. The
[import workflow](import.md) is asynchronous and does **not** create categories
when the files are uploaded or extracted.

## Direct category workflow

```text
CategoriesPage -> CategoryDetailModal / CategoryDetailState -> CategoriesHttp
    -> /api/Categories -> ISender -> category command/query handler
    -> IApplicationDbContext.Categories -> SQL Server Categories
```

| Action | Client and API | Application and database result |
|--------|----------------|---------------------------------|
| List and search | The [filter form](../../../src/Client/src/app/features/categories/list/filter/regular/filter-form.ts) starts the first load; [CategoryListState](../../../src/Client/src/app/features/categories/services/category-list.store.ts) uses [withCategoryCollection](../../../src/Client/src/app/shared/categories/services/category-collection.feature.ts) and `CategoriesHttp.getAll`. `POST /api/Categories/get-all` takes `searchTerm`, `filters`, `sort`, `cursor`, `pageSize`. | [`GetAllCategoriesHandler`](../../../src/Application/Features/Categories/Queries/GetAllCategories/GetAllCategoriesHandler.cs) reads `Categories` without tracking, projects `CategoryDto` (including item count, icon, audit names/dates), filters/searches by name, and returns a keyset page plus `hasNextPage`/`nextCursor`. Default page size is 50; the server [allowlists filters](../../../src/Application/Features/Categories/CategoryFilterConfiguration.cs) and [sort keys](../../../src/Application/Features/Categories/CategorySortConfiguration.cs). The collection store appends subsequent pages. |
| Read one | Opening an existing category calls `CategoriesHttp.getById` via [CategoryDetailState](../../../src/Client/src/app/shared/categories/services/category-detail.store.ts); `GET /api/Categories/{id}`. The same endpoint resolves selected dropdown categories when necessary. | [`GetCategoryByIdHandler`](../../../src/Application/Features/Categories/Queries/GetCategoryById/GetCategoryByIdHandler.cs) projects one `CategoryDto` or returns `categories.not_found` (404). |
| Create | **Add** opens [CategoryDetailModal](../../../src/Client/src/app/shared/categories/ui/modals/detail/category-detail-modal.ts) with the new-category sentinel; its [form](../../../src/Client/src/app/shared/categories/ui/modals/detail/form.ts) collects name and optional icon. `CategoryDetailState.saveCategory` calls `CategoriesHttp.create`: `POST /api/Categories` with `{ "name": "...", "icon": null }`. | [`CreateCategoryCommandValidator`](../../../src/Application/Features/Categories/Commands/CreateCategory/CreateCategoryCommandValidator.cs) requires a name of at most 100 characters and checks for an existing trimmed name. [`CreateCategoryCommandHandler`](../../../src/Application/Features/Categories/Commands/CreateCategory/CreateCategoryCommandHandler.cs) trims the name, maps the optional icon, inserts into `Categories`, saves, and returns `{id, name, icon}` (201). |
| Update | **Edit** opens the same modal; it loads the category by ID, then `CategoryDetailState.saveCategory` calls `CategoriesHttp.update`: `PUT /api/Categories/{id}` with `{name, icon}`. | [`UpdateCategoryCommandValidator`](../../../src/Application/Features/Categories/Commands/UpdateCategory/UpdateCategoryCommandValidator.cs) checks ID, name length and uniqueness excluding the category itself. [`UpdateCategoryCommandHandler`](../../../src/Application/Features/Categories/Commands/UpdateCategory/UpdateCategoryCommandHandler.cs) loads the row, replaces the trimmed name and optional icon, emits `CategoryUpdatedEvent`, saves, and returns `{id, name, icon}` (200); unknown IDs return `categories.not_found` (404). |

The name uniqueness check is at the application layer; the
[EF configuration](../../../src/Infrastructure/Data/Configurations/CategoryConfiguration.cs)
defines length 100 and seek indexes, not a unique name index. `Category.Icon`
is an optional JSON-owned value (name, file name, path) in the `Categories`
record. [`ApplicationDbContext`](../../../src/Infrastructure/Data/ApplicationDbContext.cs)
maps the `Categories` DbSet; the audit interceptor stamps created/modified
fields. The [commands](../../../src/Application/Features/Categories/Commands/CreateCategory/CreateCategoryCommand.cs)
invalidate the category-list HybridCache tag on create/update.
[`CategoryUpdatedEventHandler`](../../../src/Application/Features/Categories/EventHandlers/CategoryUpdatedEventHandler.cs)
also sends a `CategoryUpdated` SignalR event and invalidates category/stock
cache tags on update. The page reloads the list when the edit modal closes;
the current [realtime reducer](../../../src/Client/src/app/shared/categories/services/category-collection.feature.ts)
receives `CategoryUpdated` but does not itself reload the list.

**Current boundary:** the page has a Delete confirmation, but
[`CategoryListState.deleteCategory`](../../../src/Client/src/app/features/categories/services/category-list.store.ts)
only sets a local deleting ID; [`Categories` endpoints](../../../src/Web/Endpoints/Categories.cs)
do not expose DELETE. Do not describe deletion as a persisted operation.

## HTTP and persistence boundary

[`Categories` endpoint group](../../../src/Web/Endpoints/Categories.cs) maps
requests to Mediator and returns a typed success or ProblemDetails; it does
not query EF directly. Endpoint groups
[require authentication by default](../../../src/Web/Infrastructure/WebApplicationExtensions.cs).
Validation is handled by the [Application pipeline](../../../src/Application/DependencyInjection.cs).
All direct reads and writes use `IApplicationDbContext`, implemented by
[`ApplicationDbContext`](../../../src/Infrastructure/Data/ApplicationDbContext.cs).
The optional icon belongs to the category, not to the uploaded import files.

## Evidence

- `src/Client/src/app/shared/categories/services/categories.http.ts`
- `src/Client/src/app/shared/categories/services/category-detail.store.ts`
- `src/Application/Features/Categories/Commands/CreateCategory/CreateCategoryCommandHandler.cs`
- `src/Application/Features/Categories/Commands/UpdateCategory/UpdateCategoryCommandHandler.cs`
- `src/Infrastructure/Data/Configurations/CategoryConfiguration.cs`
