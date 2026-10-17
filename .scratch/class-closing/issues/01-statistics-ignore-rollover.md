# 01: Statistics ignore Rollover transactions

Status: resolved
Type: task
Blocked by: —

## Description

Daily consumption must not count `Rollover` transactions. Otherwise a closed class records all of its remaining stock
as consumed. This task comes first because closing a class depends on it, and it is safe to ship alone (no data
produces `Rollover` yet).

Source: [approved spec](../spec.md), [plan](../../../tasks/class-closing-backend-plan.md).

## Acceptance criteria

- [x] `MaterializeDailyConsumptionCommandHandler` excludes `Transfer` and `Rollover`.
- [x] `GetConsumptionBackfillStartHandler` excludes `Transfer` and `Rollover`.
- [x] One unit test per handler: a negative `Rollover` transaction does not count.

## Verification

```bash
dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~MaterializeDailyConsumption|FullyQualifiedName~GetConsumptionBackfillStart"
```

## Files

- `src/Application/Features/Statistics/Commands/MaterializeDailyConsumption/MaterializeDailyConsumptionCommandHandler.cs`
- `src/Application/Features/Statistics/Queries/GetConsumptionBackfillStart/GetConsumptionBackfillStartHandler.cs`
- `tests/Application.UnitTests/Features/Statistics/Commands/MaterializeDailyConsumption/*`
- `tests/Application.UnitTests/Features/Statistics/Queries/GetConsumptionBackfillStart/*`
