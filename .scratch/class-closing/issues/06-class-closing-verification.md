# 06: Final verification

Status: resolved
Type: task
Blocked by: 05

## Acceptance criteria

- [x] `dotnet build` is clean (warnings are errors).
- [x] All Application unit tests pass, plus the focused functional tests (or the environment blocker is reported, not hidden).
- [x] `dotnet ef migrations has-pending-model-changes` reports no changes (no migration expected).
- [x] Every success criterion in the spec is checked; review the diff; `graphify update .`.

## Verification

```bash
dotnet build
dotnet test tests/Application.UnitTests
./run-functional-tests.sh --filter "FullyQualifiedName~SchoolClass|FullyQualifiedName~Stock"
dotnet ef migrations has-pending-model-changes --project src/Infrastructure --startup-project src/Web --context ApplicationDbContext
graphify update .
```
