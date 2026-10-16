# 03: CloseSchoolClass command (stock rollover + status)

Status: resolved
Type: task
Blocked by: 01

## Description

Generate it with `dotnet new ca-usecase -n CloseSchoolClass -fn SchoolClasses -ut command -rt Result` from `src/Application`,
then adapt it to the `MoveStock` style. `[Authorize(Roles = Roles.Administrator)]`, `ICacheInvalidation` (Stock coarse
tag + class tag for the source and target, `StockBatchListTag`, the SchoolClasses tags). Adds `SchoolClassErrors.AlreadyClosed` and
`TargetNotOpen`.

Handler: load the source and target → check status → for every source batch with `Quantity > 0`, create a copy in the target
(`ReceivedClassId = TargetClassId`, all other fields copied), set the source to 0, write paired `Rollover`
transactions → `Status = Closed` → one `SaveChangesAsync`; `DbUpdateConcurrencyException` → `StockErrors.ConcurrencyConflict`.

## Acceptance criteria

- [x] Validator: `Id` and `TargetClassId` are required, `TargetClassId != Id`.
- [x] Missing source or target → 404 `school_classes.not_found`; source already closed → `already_closed`; target closed →
      `target_not_open`.
- [x] Positive batches move with every field copied. The source ends at 0. Zero-qty batches create no batch and no transaction.
- [x] Each moved batch gets a `-qty` `Rollover` (source class, source batch) and a `+qty` `Rollover` (target class, new batch),
      with `UserId` set to the current user.
- [x] A class with no stock still closes.

## Verification

```bash
dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~CloseSchoolClass"
dotnet build
```

## Files

- `src/Application/Common/Errors/SchoolClassErrors.cs`
- `src/Application/Features/SchoolClasses/Commands/CloseSchoolClass/CloseSchoolClassCommand.cs`
- `src/Application/Features/SchoolClasses/Commands/CloseSchoolClass/CloseSchoolClassCommandValidator.cs`
- `src/Application/Features/SchoolClasses/Commands/CloseSchoolClass/CloseSchoolClassCommandHandler.cs`
- `tests/Application.UnitTests/Features/SchoolClasses/Commands/CloseSchoolClass/CloseSchoolClassCommandHandlerTests.cs`
