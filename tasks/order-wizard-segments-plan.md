# Plan: Product segments in the "Comandă nouă" wizard

Source spec (approved): `tasks/order-wizard-segments-spec.md`. Approved defaults: 10/21/45 days, min. 3 receipts, 80% rule, per-class precedence over cadence, low-stock lines keep today's quantity (`1`, see `suggestionsToLines`).

Engineering level: production grade for the migration (auto-applied at startup by `DatabaseMigrationRunner`) and the cache path; lean elsewhere. No new dependencies, no new patterns.

## Components (verified against the code)

| # | Layer | What | Real files |
|---|---|---|---|
| A | Domain + EF | `ItemPurchaseStatistic.AverageIntervalDays` (`decimal?`), `decimal(18,2)`, migration `AddPurchaseCadence` | `src/Domain/Entities/Statistics/ItemPurchaseStatistic.cs`, `src/Infrastructure/Data/Configurations/ItemPurchaseStatisticConfiguration.cs`, `src/Infrastructure/Data/Migrations/` (latest today: `20261031195815_AddCongregations`) |
| B | Calculator | Interval on `Last365Days` rows only, null when < 3 distinct receipts | `src/Application/Features/Statistics/Commands/MaterializePurchaseStatistics/PurchaseStatisticsCalculator.cs` (`Calculate` / `Aggregate`), tests in `tests/Application.UnitTests/Features/Statistics/Commands/MaterializePurchaseStatistics/PurchaseStatisticsCalculatorTests.cs` |
| C | Query | `GetPurchaseSegmentsQuery` + handler + pure classifier, `ICacheableQuery` tagged `CacheConstants.PurchaseStatisticsTag` | `src/Application/Features/Statistics/Queries/GetPurchaseSegments/`, DTOs in `src/Application/Features/Statistics/Models/StatisticsDto.cs` |
| D | Endpoint | `GET /api/statistics/purchases/segments` | `src/Web/Endpoints/Statistics.cs` (next to `purchases/top`) |
| E | Client data | http method + DTOs + wizard store state | `src/Client/src/app/shared/class-statistics/services/class-statistics.http.ts` (class is `ClassStatisticsHttp`), `src/Client/src/app/core/models/class-statistics.ts`, `src/Client/src/app/shared/order-lists/services/order-list-wizard.store.ts`, `order-list-wizard.utils.ts` |
| F | Client UI | Segments step replaces suggestions step; title "Articole sugerate" → "Produse" | `src/Client/src/app/shared/order-lists/ui/modals/wizard/steps/suggestions-step.ts`, `steps/checked-rows.ts`, `order-list-wizard-container.ts`, `ske-item-thumbnail` (`src/Client/src/app/shared/items/ui/thumbnail/item-thumbnail.ts`, has `large` input) |
| G | E2E | Wizard spec at 360/768/1280, API mocked with `page.route` like `stock-count-wizard.spec.ts` | `src/Client/e2e/specs/order-wizard-segments.spec.ts` |

## Dependency order

```
A ─► B ─► C ─► D ─► E ─► F ─► G ─► PR
```
- C does not need B at runtime (it only reads the column), only A. B and C can run in parallel after A.
- E can start once the DTO shape of C is fixed (write it in task 3), in parallel with D.
- F needs E. G needs F.

## Parallel vs sequential

- Sequential: A first (one migration, one snapshot change). F after E. G after F. PR last.
- Parallel after A: B (calculator) ‖ C (query/classifier). After C's DTO is committed: D (endpoint + functional test) ‖ E (client store).
- Single branch `feature/order-wizard-segments`; parallel agents must not both touch `StatisticsDto.cs` or the snapshot. If run in parallel, use worktrees and merge in order A, B, C, D, E, F, G.

## Risks and mitigations

1. **Migration backward-safety.** Applied on deploy. Column must be nullable, no default, no backfill, no index (not filtered/sorted). Old code ignores the column. Verify `has-pending-model-changes` clean and read the generated `Up`/`Down`. Never hand-edit the snapshot.
2. **Cache tags.** New query must use `CacheConstants.PurchaseStatisticsTag` (already invalidated by `MaterializePurchaseStatisticsCommand.Tags`). Cache key `"{PurchaseStatisticsTag}:segments"`. Do not add a new tag constant unless the key needs one. Item enable/disable does not invalidate this tag: a disabled item may linger until the next 21:00 run or sliding expiry (10 min, as `GetTopPurchasesQuery`). Acceptable; mention in PR.
3. **Fragile areas (CLAUDE.md §10).** Do not touch `Application/Common/{Behaviours,Filtering,Keyset,Caching}`, `ApplicationDbContextInitialiser.cs`, Worker/outbox path, `item-autocomplete.ts`. The Worker `DailyStatisticsJob` already calls `MaterializePurchaseStatisticsCommand`; no Worker change needed.
4. **Shared `selectedItemIds` across tabs.** `emitCheckedIdsOnChange` (checked-rows.ts) emits only the rows of one list. Per-tab use would wipe ticks from other tabs. Each tab must emit `otherSelected ∪ thisTabChecked`, or the step keeps one id set and derives per-tab state. Store spec + step spec must cover "tick in tab A, untick in tab B, A still ticked".
5. **Low stock grouped by location.** `groupLowStockItems` collapses an item to one row with `locations[]`. Grouping under location headings shows the same item in several groups; rows keyed by item id must stay in sync and dedupe on output. Keep `SuggestedItem` and derive the grouping in a computed; do not change `suggestionsToLines` dedupe.
6. **Quantity precedence.** Order line `quantity` is `number` client-side and `decimal` server-side, so AverageQuantity (2 decimals) fits as is. If an item is in "Stoc redus" and a purchase segment, use the segment's AverageQuantity (spec: lookup wins); low-stock-only lines stay `1`.
7. **nz-tabs vs nz-collapse on small screens.** Check `.github/instructions/llms-full.txt` before choosing. Default proposal: `nz-tabs` with `nzSize="small"` and scrollable nav at all widths (one component, less code); fall back to `nz-collapse` below `md` only if tabs overflow badly at 360 px in e2e. Whole-wizard scroll on small screens was fixed in PR #22: do not reintroduce inner fixed heights.
8. **Empty statistics after deploy.** Purchase segments are empty until the first 21:00 job; show "Nu există date încă". Low stock works immediately.
9. **"O dată pe clasă" bias.** `Class` scope rows include the currently open class (partial history), which can lower the 80% ratio. Accepted by spec; note in PR.
10. **Class duration hint.** The wizard container only receives `classId`, not dates. Either pass the class's `startDate`/`endDate` from the opener as an input, or drop the hint. Decide in task 6 with the smaller diff; do not add a backend field for it.

## Verification checkpoints

- After task 1: `dotnet build`; `dotnet ef migrations has-pending-model-changes ...` clean; migration reviewed (nullable add/drop column only).
- After task 2-3: `dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~Statistics"` green.
- After task 4: `./run-functional-tests.sh --filter "FullyQualifiedName~PurchaseSegments"` green.
- After task 5-6: `cd src/Client && npm run build && npm test -- --watch=false` green.
- After task 7: `npm run e2e` green, full `dotnet build` + all unit tests + functional suite + client build/test.
- Every task: Haiku reviews the diff (task goal + diff only), findings fixed or queued; `graphify update .` after code changes.
