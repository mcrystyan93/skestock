# 02: UpdateSchoolClass can no longer close or reopen a class

Status: resolved
Type: task
Blocked by: —

## Description

`PUT /api/SchoolClasses/{id}` currently sets `Closed` with no stock handling. The handler returns
`SchoolClassErrors.StatusChangeNotAllowed` (409, `school_classes.status_change_not_allowed`) when
`(request.Status == Closed) != (current.Status == Closed)`.

## Acceptance criteria

- [x] Active→Closed and Closed→Active return 409 with the code above. Nothing is saved.
- [x] Editing the name or dates of a closed class that keeps `Status = Closed` succeeds.
- [x] Non-closed transitions (Upcoming↔Active↔Paused) work as before.

## Verification

```bash
dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~UpdateSchoolClass"
```

## Files

- `src/Application/Common/Errors/SchoolClassErrors.cs`
- `src/Application/Features/SchoolClasses/Commands/UpdateSchoolClass/UpdateSchoolClassCommandHandler.cs`
- `tests/Application.UnitTests/Features/SchoolClasses/Commands/UpdateSchoolClass/UpdateSchoolClassCommandHandlerTests.cs`
