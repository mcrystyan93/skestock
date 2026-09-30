# Plan: Order list PNG export

Spec: `SPEC-order-list-image-export.md`

## Dependency graph

```text
T1 Package + embedded font + IOrderListImageExporter + OrderListImageExporter (+ DI, integration tests)
        │
        ▼
T2 Query Format (Xlsx|Png) + handler/validator + endpoint ?format= (+ unit & functional tests)
        │
        ▼
T3 Client: OrderListExportService format param + "Descarcă imagine" action (+ tests)
```

Sequential: each slice builds on the previous one; T1 is verifiable alone via integration tests, T2 makes it
reachable via API, T3 exposes it in the UI.

## Risks
- Skia native assets / fonts in the Docker image → use `SkiaSharp.NativeAssets.Linux.NoDependencies`, embed font;
  verify by running the exporter test on Linux and (optionally) a docker build.
- Text wrapping/measuring in Notes column → measure with `SKPaint`/`SKFont`, compute row height before allocating bitmap.
- Warnings are errors → keep analyzers clean (disposal of Skia objects via `using`).

## Checkpoints
- After T1: `dotnet test tests/Infrastructure.IntegrationTests --filter OrderListImageExporter`; view a sample PNG manually.
- After T2: unit + functional export tests green; Excel default unchanged.
- After T3: `npm test` and `npm run build` in `src/Client`.
