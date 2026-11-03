# Spec: AverageIntervalDays that discounts breaks between classes

## Objective
`ItemPurchaseStatistic.AverageIntervalDays` (Last365Days only) is today
`(last receipt - first receipt) / (receipts - 1)`. Breaks between school classes (~2 weeks, usually no
purchases) inflate it, so an item bought weekly in every class shows ~7.5 days instead of ~7.

Goal: the break time must not count towards the interval, while every goods receipt, including those
received during a break, still counts as a purchase point.

Investigated facts (Haiku, read-only):
- `SchoolClass` has `StartDate`/`EndDate` (`DateOnly`) and `Status` (Upcoming/Active/Paused/Closed). Classes can overlap; nothing validates it.
- A "break" is not modeled. A receipt can be created in any non-Closed class with no date check.
- Only `PurchaseStatisticsCalculatorTests` asserts interval values (5m, 1.33m, 0m, DST case). The UI renders no interval; the backend uses it in `GetPurchaseSegments` / `PurchaseSegmentClassifier`.

### Definition (proposed)
- Class coverage = union of all `[StartDate, EndDate]` ranges (inclusive, local calendar days in the request time zone). A break = any time not covered.
- Receipt date = earliest `PurchasedAt` of the receipt (unchanged); receipts are sorted by date.
- For each pair of consecutive receipts, gap = elapsed time minus the part of it that falls inside a break.
- `AverageIntervalDays = sum(gaps) / (receipts - 1)`; still `null` below 3 receipts.

Example: weekly in class A (7 receipts), 14-day break with no purchase, weekly in class B (7 receipts):
`(42 + 7 + 42) / 13 = 7.0` days. With one receipt in the middle of the break the 7-day A→B step splits into
two pieces whose break part is dropped, and that receipt remains a point.

## Tech Stack
.NET 10, Application layer (`Features/Statistics`), NUnit + Shouldly + Moq. No new dependencies.

## Commands
```
Build:  dotnet build
Test:   dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~Statistics"
Integ:  dotnet test tests/Infrastructure.IntegrationTests --filter "FullyQualifiedName~MaterializePurchaseStatistics"
```

## Project Structure
```
src/Application/Features/Statistics/Commands/MaterializePurchaseStatistics/
  PurchaseStatisticsCalculator.cs        interval computation (pure, stays static)
  MaterializePurchaseStatisticsCommandHandler.cs   loads class ranges next to the lines
src/Application/Features/Statistics/
  ClassBreaks.cs (new)                   pure: merge class ranges, "break time between two instants"
tests/Application.UnitTests/Features/Statistics/Commands/MaterializePurchaseStatistics/
  PurchaseStatisticsCalculatorTests.cs   extend
  ClassBreaksTests.cs (new)
```

## Code Style
Match the surrounding calculator: static pure functions, records for inputs, comments only for the why.
```csharp
public sealed record ClassPeriod(DateOnly StartDate, DateOnly EndDate);
```

## Testing Strategy (TDD, red then green per behavior)
`ClassBreaks` (unit): no classes means zero break time; single class; overlapping classes merge; adjacent
classes with no gap; gap spanning a break; gap fully inside a break is 0; DST day boundaries use `LocalCalendarDay`.
Calculator (unit): the weekly example gives 7.0; a receipt inside the break still counts and shortens
neighbouring gaps; with no class data the result equals today's formula (backward compatible); the existing
interval tests keep passing.
Handler: loads class periods once and passes them in; the integration test covers the real query.

## SOLID notes
- SRP: break math lives in `ClassBreaks`; the calculator only aggregates.
- OCP/DIP: the calculator gets class periods as input data, with no `DbContext` inside it. The handler stays the only I/O point.
- No interface for `ClassBreaks`: one implementation, YAGNI.

## Boundaries
- Always: tests first; keep `null` below 3 receipts; UTC instants; no migration needed (stats are recomputed fully each run).
- Ask first: changing `Calculate` signature beyond one new parameter; touching `PurchaseSegmentClassifier`; adding any class-overlap validation.
- Never: drop receipts that fall in a break; edit applied migrations; run Prettier on client files.

## Success Criteria
- Weekly-in-class with 2-week breaks yields ~7.0 days (not 7.5).
- Receipts during a break are included and verified by a test.
- With no class periods, output is identical to today's.
- `dotnet build` clean (warnings are errors); statistics unit tests and the integration test pass.
- Update the `AverageIntervalDays` comment in `ItemPurchaseStatistic` to the new definition.

## Process (as requested)
Plan: Opus. Investigation: Haiku (done above). Implementation: Sonnet, task by task. Then the Haiku diff review required by CLAUDE.md, a feature branch and a PR.

## Decisions (approved)
1. Class periods are used by dates only, regardless of `Status`.
2. A Paused class counts as covered time.
3. Only gaps between consecutive receipts are measured; time before the first receipt is ignored.
