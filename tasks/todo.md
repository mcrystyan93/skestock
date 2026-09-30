# Todo: Order list PNG export

- [x] T1: Image exporter (Application interface + Infrastructure Skia implementation)
  - Acceptance: `IOrderListImageExporter.Export(model)` returns a valid PNG with title, optional `Notă:`, category
    header rows (gray) and `Produs/Cantitate/U.M./Observații` rows; Romanian diacritics render via embedded font;
    wrapped notes grow the row; empty model renders without throwing; registered in DI.
  - Verify: `dotnet test tests/Infrastructure.IntegrationTests --filter OrderListImageExporter` (PNG signature, decodes,
    height grows with lines, diacritics/long-note cases); `dotnet build` clean; eyeball one output.
  - Files: `Directory.Packages.props`, `src/Infrastructure/Infrastructure.csproj`, `src/Application/Common/Interfaces/IOrderListImageExporter.cs`,
    `src/Infrastructure/Export/OrderListImageExporter.cs`, `src/Infrastructure/Export/Fonts/*.ttf`, `src/Infrastructure/DependencyInjection.cs`,
    `tests/Infrastructure.IntegrationTests/OrderListImageExporterTests.cs`

- [x] T2: Format-aware query + endpoint
  - Acceptance: `ExportOrderListQuery.Format` (`Xlsx` default, `Png`); handler builds the model once and picks exporter,
    content type (`image/png`) and `.png`/`.xlsx` extension; validator rejects unknown values; endpoint accepts
    `?format=png`; default request identical to today; endpoint docs updated.
  - Verify: `dotnet test tests/Application.UnitTests --filter ExportOrderList`;
    `./run-functional-tests.sh --filter ExportOrderList` (png 200 + `image/png`, non-submitted error, default xlsx).
  - Files: `src/Application/Features/OrderLists/Queries/ExportOrderList/*`, `src/Web/Endpoints/OrderLists.cs`,
    `tests/Application.UnitTests/.../ExportOrderListHandlerTests.cs`, `tests/Application.FunctionalTests/.../ExportOrderListQueryTests.cs`

- [x] T3: Client image download
  - Acceptance: `OrderListExportService.download(id, format)` (default xlsx) sends `format` param; downloading state tracked
    per format; a "Descarcă imagine" action next to the Excel one in the order-lists tab and detail modal, accessible label.
  - Verify: `cd src/Client && npm test && npm run build`; manual download from UI via AppHost.
  - Files: `src/Client/src/app/shared/order-lists/services/order-list-export.service.ts` (+spec),
    `src/Client/src/app/shared/order-lists/ui/modals/detail/order-list-detail-modal.ts`,
    `src/Client/src/app/features/school-classes/overview/tabs/order-lists/order-lists-tab.ts` (+ list item action component)
