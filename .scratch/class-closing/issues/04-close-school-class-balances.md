# 04: CloseSchoolClass fills in ClassBalance

Status: open
Type: task
Blocked by: 03

## Description

In the same `SaveChangesAsync` as task 03, upsert `ClassBalance` rows. There is no unique index, so read the existing
rows for (source, target), then update them or add the missing ones with `OpeningQty = 0`.

## Acceptance criteria

- [ ] Source: every (item, location) with at least one source batch gets `ClosingQty` = the sum of the positive remaining quantity
      (0 for pairs that are already empty).
- [ ] Target: every (item, location) that received stock gets `OpeningQty += transferred`. A new row is created when one is missing,
      and an existing row is added to. `ClosingQty` stays `null`.
- [ ] A failed close writes no balance rows (they are part of the same save).

## Verification

```bash
dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~CloseSchoolClass"
```

## Files

- `src/Application/Features/SchoolClasses/Commands/CloseSchoolClass/CloseSchoolClassCommandHandler.cs`
- `tests/Application.UnitTests/Features/SchoolClasses/Commands/CloseSchoolClass/CloseSchoolClassCommandHandlerTests.cs`
