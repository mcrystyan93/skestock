# Plan: AverageIntervalDays excludes breaks between classes

Spec: `tasks/purchase-interval-break-spec.md`. Plan written by Opus; implementation by Sonnet.

## Key simplification
Sum of per-pair gaps = `(last - first) - BreakTime(first, last)`. The calculator makes one `ClassBreaks`
call per item; the receipt count stays the denominator, so receipts inside a break still count as points.

## Decision to confirm
"Break" = only the time between merged class ranges (not before the earliest start or after the latest end).
Reason: a receipt after the last class's EndDate with no next class yet would otherwise lose its whole gap,
and "no classes" would need a special case. Identical to the spec for receipts inside the overall class span.

## Components
1. `src/Application/Features/Statistics/ClassBreaks.cs` (new): `record ClassPeriod(DateOnly StartDate, DateOnly EndDate)` + static `ClassBreaks.Between(periods, from, to, timeZone)` returning `TimeSpan`. Pure, depends on `LocalCalendarDay`.
2. `PurchaseStatisticsCalculator.Calculate`: new required last parameter `IReadOnlyCollection<ClassPeriod> classPeriods`, passed to `Aggregate` and `AverageIntervalDays`.
3. `MaterializePurchaseStatisticsCommandHandler`: load periods once, outside `strategy.ExecuteAsync`, from `dbContext.SchoolClasses` (no query filter, `Status` ignored).
4. Comment in `ItemPurchaseStatistic.cs:33`.

## Algorithm (`Between`)
1. `to <= from` gives Zero.
2. Sort by StartDate; merge when `next.StartDate <= current.EndDate.AddDays(1)` (overlap or adjacent); EndDate = max.
3. Gap between consecutive merged ranges `(a, b)` = `[GetStartUtc(a.EndDate+1), GetStartUtc(b.StartDate))`.
4. Sum `max(0, min(to, gapEnd) - max(from, gapStart))`.
5. Day boundaries only via `LocalCalendarDay.GetStartUtc`; UTC arithmetic (a 14-day break over end of DST = 14 days + 1h).

Calculator: `span = max - min - ClassBreaks.Between(...)`; `Math.Round(span.TotalDays / (receipts - 1), 2)`.

## Risks
- Overlapping classes: handled by the merge; no validation added (spec: ask first).
- Integration DB is shared: the test seeds its own two classes and cleans up lists in `TearDown`; add `[NonParallelizable]` if flaky.
- Cost: per item merge, negligible (dozens of classes).

## Tasks (TDD: red test first, then green)
U = `dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~Statistics"`
I = `dotnet test tests/Infrastructure.IntegrationTests --filter "FullyQualifiedName~MaterializePurchaseStatistics"`

- [ ] T0 Branch: new branch `feature/purchase-interval-breaks` (base to be decided, see below). Verify: `git branch --show-current`
- [ ] T1 `ClassBreaks` trivial cases: no periods, single class, `to <= from` give Zero. Files: `ClassBreaks.cs`, `tests/Application.UnitTests/Features/Statistics/ClassBreaksTests.cs`. Verify: U
- [ ] T2 Gaps/merging/DST: 14-day gap across A and B; span fully in gap; span starting in gap; overlapping A,B then C; adjacent classes Zero; unsorted input; DST (A ends 2026-10-18, B starts 2026-11-01, Europe/Bucharest = 14d + 1h). Verify: U
- [ ] T3 Calculator: add parameter, append `[]` to the 7 existing test calls; new tests: weekly A (7), 14-day break, weekly B (7) gives 7.0; same plus one receipt in the break gives 6.5 (91/14). Files: calculator + `PurchaseStatisticsCalculatorTests.cs`. Verify: U
- [ ] T4 Handler loads periods and passes them. Verify: `dotnet build` then I
- [ ] T5 Integration test: two classes with a 14-day gap, 3 weekly receipts each, expected Last365Days interval; make seed/TearDown handle lists. File: `tests/Infrastructure.IntegrationTests/MaterializePurchaseStatisticsTests.cs`. Verify: I
- [ ] T6 Comment in `ItemPurchaseStatistic.cs` updated. Verify: `git diff src/Domain`
- [ ] T7 `dotnet build && U && I` all green.
- [ ] T8 `graphify update .`
- [ ] T9 Haiku diff review (diff + one-line goal only); fix real findings, re-run T7.
- [ ] T10 Commit (conventional message + attribution trailer) and `gh pr create` against `master`.

Not planned (YAGNI): interface for `ClassBreaks`, caching, overlap validation, changes to `PurchaseSegmentClassifier`, migration.
