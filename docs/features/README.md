# Feature workflows

This directory documents user-facing features end to end. Use one directory per
feature, with a `README.md` for the feature overview and focused pages for longer
workflows. Follow the path from the client UI and HTTP calls through Web endpoints,
Application commands/queries, infrastructure, and persisted data. Describe current
behavior, including important boundaries and gaps; link claims to the source.

| Feature | Workflows |
|---------|-----------|
| [Categories](categories/README.md) | [List, read, create, update](categories/README.md); [upload, extract, review, confirm an import](categories/import.md) |

For cross-feature layer boundaries and runtime architecture, see
[Codebase architecture](../codebase/ARCHITECTURE.md).

## Evidence

- `src/Client/src/app/features/categories/list/categories.page.ts`
- `src/Web/Endpoints/Categories.cs`
- `src/Web/Endpoints/CategoryImportBatches.cs`
