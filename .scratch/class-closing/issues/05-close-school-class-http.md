# 05: POST /api/SchoolClasses/{id}/close endpoint + functional tests

Status: resolved
Type: task
Blocked by: 02, 04

## Description

Add `MapPost(CloseSchoolClass, "{id}/close")` to the existing `SchoolClasses` group, with body `{ targetClassId }` → 204,
mapping failures through the existing problem-result mapper. Functional tests on SQL Server (TestAppHost).

## Acceptance criteria

- [x] Admin `POST` → 204. The target's class stock query shows the transferred quantities; the source shows none and is `Closed`.
- [x] Non-admin → 403. Invalid body (empty target, target == source) → 400. Source already closed / target closed → 409.
- [x] `ClassBalance` rows are persisted as in the spec, and repeating the close returns 409 without double-counting.
- [x] PUT with `Status = Closed` on an open class → 409 (covers task 02 end to end).

## Verification

```bash
./run-functional-tests.sh --filter "FullyQualifiedName~CloseSchoolClass|FullyQualifiedName~UpdateSchoolClass"
```

## Files

- `src/Web/Endpoints/SchoolClasses.cs`
- `src/Application/Features/SchoolClasses/Models/SchoolClassRequests.cs` (request body record, if the group uses one)
- `tests/Application.FunctionalTests/Features/SchoolClasses/Commands/CloseSchoolClass/CloseSchoolClassCommandTests.cs`
- `tests/Application.FunctionalTests/Features/SchoolClasses/Commands/UpdateSchoolClass/UpdateSchoolClassCommandTests.cs`
