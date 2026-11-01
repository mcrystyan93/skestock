# Spec: Product segments in the "Comandă nouă" wizard

## Objective
When preparing a new order (source "Comandă nouă"), the user picks products from **segments** instead of only the flat low-stock list. Each segment shows its products with picture; products can be ticked across any segments and end up in one order (no duplicates).

Segments:
1. **Stoc redus**: low-stock items, grouped by location (existing data, new grouped UI).
2. **Comandate săptămânal**: avg. interval between goods receipts ≤ 10 days.
3. **La 2 săptămâni**: 11–21 days.
4. **Lunar**: 22–45 days.
5. **O dată pe clasă**: bought about once per class (classes last 1 or 2 months, most 2).

Success = the user opens "Comandă nouă", sees the 5 segments, ticks products from several of them, and the editor step opens with exactly those lines.

## Decisions (confirmed by user)
- Cadence = **average interval between goods receipts** over the last 365 days, min. 3 distinct receipts.
- "O dată pe clasă" and class duration are **derived from data, no new field on classes**.
- The segments step **replaces** "Articole sugerate"; low stock becomes one segment. The `supplyList` source and its flow stay untouched.
- Line quantity = **AverageQuantity per receipt** (from statistics); low-stock lines keep today's quantity behavior.

## Assumptions (correct me now or I proceed)
1. Only goods-receipt data counts (same as `MaterializePurchaseStatisticsCommandHandler`); manual Order transactions are excluded.
2. "O dată pe clasă" = item bought in ≥ 2 classes AND in ≥ 80% of those classes with exactly 1 receipt (Class-scope rows). **Per-class takes precedence**: such an item is not also shown in the cadence segments.
3. An item appears in at most one of the 4 purchase segments, but may also appear in "Stoc redus". Ticking it anywhere ticks it everywhere (shared `selectedItemIds`).
4. Disabled items are not listed. Items with no history simply appear in no purchase segment.
5. Cadence values are filled by the daily statistics job (21:00). Until the first run after deploy the 4 purchase segments are empty ("Nu există date încă"). Stoc redus works immediately.
6. DROPPED by user: no class-duration hint. Purchase-segment items start unticked; low-stock items keep starting ticked.
7. Thresholds (10/21/45 days, 3 receipts, 80%) are constants in code, not configurable.

## Design
**Backend**
- `ItemPurchaseStatistic`: new nullable `AverageIntervalDays` (decimal), set on `Last365Days` rows only: `(lastReceiptDate − firstReceiptDate) / (distinctReceipts − 1)`, null when < 3 receipts. Computed in `PurchaseStatisticsCalculator` (unit-tested). Migration via `dotnet ef`, nullable, no backfill.
- New query `GetPurchaseSegmentsQuery` (`Features/Statistics/Queries/GetPurchaseSegments`), `ICacheableQuery`, tags consistent with `CacheConstants` (invalidated by `MaterializePurchaseStatisticsCommand`). Endpoint `GET /api/statistics/purchases/segments` in `Statistics.cs`. Returns `Weekly`, `EveryTwoWeeks`, `Monthly`, `OncePerClass` lists of `{ itemId, name, sku, imageFileId, unit, averageQuantity, purchaseCount, averageIntervalDays }`. Classification happens in the handler (reads Last365Days + Class rows + active Items).
- Low stock: reuse `GET /api/Stock/class/{classId}/low-stock`, no change.

**Frontend** (`src/Client/src/app/shared/order-lists/`)
- `steps/suggestions-step.ts` → segments step: NG-ZORRO `nz-tabs` (or `nz-collapse` below `md`; decide in task 5 after checking `llms-full.txt`), each tab has count badge. Rows: `nz-checkbox`, `ske-item-thumbnail` (large), name + SKU, quantity + unit. Low-stock tab groups rows under location headings. "Selectează tot" per tab.
- Store: `selectedItemIds` stays; new state `segments` (loaded once per wizard open via `StatisticsHttp`/new http method) and a `quantityByItemId` lookup so `buildPrefill()` uses AverageQuantity for segment items.
- Container step titles unchanged for the new-order flow except "Articole sugerate" → "Produse".
- Romanian copy only.

## Commands
```
dotnet build
dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~Statistics"
dotnet ef migrations add AddPurchaseCadence --project src/Infrastructure --startup-project src/Web --context ApplicationDbContext --output-dir Data/Migrations
dotnet ef migrations has-pending-model-changes --project src/Infrastructure --startup-project src/Web --context ApplicationDbContext
./run-functional-tests.sh --filter "FullyQualifiedName~PurchaseSegments"
cd src/Client && npm run build && npm test -- --watch=false
cd src/Client && npm run e2e
graphify update .
```

## Project Structure
```
src/Domain/Entities/Statistics/ItemPurchaseStatistic.cs
src/Infrastructure/Data/Configurations/ItemPurchaseStatisticConfiguration.cs + Migrations/
src/Application/Features/Statistics/Commands/MaterializePurchaseStatistics/PurchaseStatisticsCalculator.cs
src/Application/Features/Statistics/Queries/GetPurchaseSegments/
src/Web/Endpoints/Statistics.cs
src/Client/src/app/shared/order-lists/{services,ui/modals/wizard/steps}
src/Client/e2e/specs/order-wizard-segments.spec.ts
tests/Application.UnitTests/Features/Statistics/
```

## Code Style
Follow existing: file-scoped namespaces, `Guard.Against`, `Result<T>`, source-generated Mediator, Signal stores + `*.http.ts`, no Prettier on client, `inject()`/`input()`/signals, native control flow, UI strings in Romanian.

## Testing Strategy
- Unit: calculator interval (0/1/2/3+ receipts, same-day receipts, DST window), segment classifier (thresholds at 10/11/21/22/45/46 days, per-class precedence, 80% rule, disabled items).
- Functional: new endpoint returns segments for seeded receipts.
- Client Vitest: store (cross-tab selection, dedupe, quantity from segment, empty segments), step component (tabs, select-all, location groups).
- Playwright: wizard e2e (none exists today for the creation wizard) at viewports 360×740, 768×1024, 1280×800: tabs/collapse usable, no horizontal scroll, pictures visible, tick across segments, editor shows chosen lines.

## Boundaries
- Always: migration nullable and backward-safe; run build + tests; branch + PR (never commit to `master`); check `llms-full.txt` before any `nz-*` use.
- Ask first: configurable thresholds, a class-duration field, extra segments, changing the `supplyList` flow.
- Never: edit applied migrations or the snapshot by hand; reorder the Mediator pipeline; use `Item.MinThreshold` (dead); add dependencies.

## Success Criteria
- 4 purchase segments classify correctly per the thresholds above (unit-tested).
- Statistics job populates `AverageIntervalDays` and the migration applies cleanly (`has-pending-model-changes` clean).
- Wizard shows 5 segments with pictures; low stock grouped by location; cross-segment ticks produce one deduplicated order with the right quantities.
- Works at 360, 768 and 1280 px widths with no horizontal page scroll.
- `dotnet build`, unit/functional tests, `npm run build`, `npm test`, e2e all green.

## Open Questions
- Are 10/21/45 days and the 80% rule acceptable defaults?
- Per-class precedence over cadence (assumption 2): OK?
- Low-stock quantity: keep today's value, or switch to deficit vs. threshold?

## Workflow (per your instruction)
Plan + tasks: Opus. Exploration: Haiku. Implementation: Sonnet. Independent review: Haiku on the diff.
