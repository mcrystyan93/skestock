# Spec: Order list image (PNG) export

## Objective
Submitted order lists can be exported to `.xlsx` (grouped by category). Add a PNG export with the
same content and layout so users can share/print it without Excel (e.g. phone/WhatsApp).

Content (same as Excel): list name (title), optional `Notă: …`, then per category a shaded header
row followed by a `Produs | Cantitate | U.M. | Observații` table. Uncategorized lines go under
"Alte articole" last. Only `Submitted` lists are exportable (same errors as Excel).

## Assumptions (correct me before implementation)
1. Format is PNG only; single tall image (no pagination).
2. Reuse the grouping logic in `ExportOrderListHandler` — no duplicated query.
3. Rendering library: **SkiaSharp** (+ `SkiaSharp.NativeAssets.Linux.NoDependencies`) — the runtime
   image (`aspnet:10.0`) has no fontconfig/fonts. Alternative: SixLabors.ImageSharp.Drawing (pure managed,
   but split/commercial license). Either way a font with Romanian diacritics (ăâîșț) is **bundled as an
   embedded resource** (e.g. Noto Sans / DejaVu Sans, OFL) since the image has no system fonts.
4. Client: add a second "Descarcă imagine" action beside the existing Excel download.

## Tech Stack
.NET 10, Clean Architecture (Application interface, Infrastructure impl), Mediator, Angular 22.

## Design
- `IOrderListImageExporter { byte[] Export(OrderListExportModel model); }` in `Application/Common/Interfaces`.
- `OrderListImageExporter` in `Infrastructure/Export`, registered in `Infrastructure/DependencyInjection.cs`.
- Refactor `ExportOrderListQuery` with a `Format` (`Xlsx` default | `Png`) so the handler builds the model once
  and picks the exporter, content type (`image/png`) and file extension. Validator rejects unknown formats.
- Endpoint: `GET /api/OrderLists/{id}/export?format=png` (default remains xlsx → backward compatible).
  Update `[EndpointSummary]/[EndpointDescription]`.
- Client: extend `OrderListExportService.download(id, format)` with separate downloading state per format.
- Layout: fixed width (~1000px), 2x scale for sharpness, white background, gray category header rows,
  column widths mirroring Excel (40/12/10/40 ratios), text wrapped/ellipsized in Notes column, row heights grow
  for wrapped notes; empty list → header only.

## Commands
```
Build:  dotnet build
Unit:   dotnet test tests/Application.UnitTests
Integ:  dotnet test tests/Infrastructure.IntegrationTests
Func:   ./run-functional-tests.sh --filter ExportOrderList
Client: cd src/Client && npm test && npm run build
```

## Project Structure
```
src/Application/Common/Interfaces/IOrderListImageExporter.cs
src/Application/Features/OrderLists/Queries/ExportOrderList/   (Query, Handler, Validator)
src/Infrastructure/Export/OrderListImageExporter.cs (+ Fonts/*.ttf embedded)
src/Web/Endpoints/OrderLists.cs
src/Client/src/app/shared/...OrderListExportService + order-lists UI
tests/{Application.UnitTests,Infrastructure.IntegrationTests,Application.FunctionalTests}
```

## Code Style
File-scoped namespaces, `Guard.Against.*`, named static endpoint handlers, versions only in
`Directory.Packages.props`; Angular: signals, `inject()`, no `ngClass`.

## Testing Strategy
- Unit: handler picks PNG exporter/content type/`.png` name; default format still xlsx; validator rejects bad format.
- Integration (`OrderListImageExporterTests`): output starts with PNG signature, decodes, width > 0, height grows with
  more lines; renders diacritics/long notes/empty groups without throwing.
- Functional: `GET …/export?format=png` → 200, `image/png`, `.png` filename; non-submitted → same error as Excel.
- Client: service test for format param and per-format downloading state.

## Boundaries
- Always: keep Excel behavior unchanged; run tests; keep Skia types out of Application.
- Ask first: new NuGet packages (Skia + font asset), Dockerfile changes.
- Never: hand-edit generated files; rely on system fonts; put rendering in Web/Application.

## Success Criteria
1. Exporting a submitted list as PNG downloads a valid image with title, note and all categories/lines legibly rendered,
   including Romanian diacritics.
2. Excel export and default endpoint behavior are unchanged.
3. Works in the Docker production image (no system fonts/libs required) and in AppHost dev.
4. All tests above pass; `dotnet build` clean (warnings are errors).

## Open Questions
1. OK with SkiaSharp + bundled OFL font (recommended)?
2. Any max image height / line count limit needed for very large lists?
