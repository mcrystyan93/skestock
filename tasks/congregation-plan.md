# Congregation: implementation plan

Spec: `docs/specs/congregation.md`. Tasks: `tasks/congregation-todo.md`.

## Order and dependencies

```
B1 ─┬─ B2 ─ B3 ─┬─ B4 ─┐
    │           ├─ B5 ─┼─ B8 ─┐
    │           ├─ B6 ─┤      │
    │           └─ B7 ─┤      ├─ B10 ─ B-GATE ─ F1 ─ F2 ─┬─ F3 ─ F4 ─┐
    └─ B9 ─────────────┴──────┘                          ├─ F5 ─ F6 ─┴─ F7 ─ F-GATE ─ W1..W4
                                                          └───────────┘
```

- B4, B5, B6, B7 are independent after B3 and can be done in any order; B8 needs B3, B4 and B5.
- B9 (EF configuration and migration) only needs B1, but the query and handler tests use the in-memory test DbContext, so it can wait until B10 needs it. Do it before B10.
- The frontend starts after B-GATE so it builds against the real contract.

## Verification checkpoints

1. After B3: first full vertical slice (create) green; confirms the TDD loop and test DbContext setup.
2. After B9: read the generated migration (table, filtered unique index, no unrelated changes).
3. B-GATE: build, unit tests, functional tests, no pending model changes.
4. F-GATE: client build, Vitest, manual walk-through in AppHost (create, edit, delete, filters, dropdown, two browsers for realtime).
5. W4: every AC in spec section 5 reported met / not met.

## Risks and mitigations

| Risk | Mitigation |
|---|---|
| Soft delete is a new pattern (global query filter hides rows from every query; unique index must be filtered) | Cover with handler and functional tests (AC-B3, AC-B5); read the migration; keep it local to `Congregation` |
| Boolean filters may not be supported by `Common/Filtering` (fragile area) | Check first in B6; if missing, add tests in the filtering tests before changing it, and ask before wider refactors |
| Index collation vs validator collation (case/accent-insensitive uniqueness) | Validator is the primary check; unique index is a race backstop. Verify against SQL Server in the functional test |
| Category's stubbed delete and no-op realtime reducer could be copied by mistake | Spec 1 says build fresh; F2 and F5 specs assert that realtime events change state |
| Realtime wiring touches shared constants and groups | Additive only; no change to existing Category events |
| Migration reaches production on deploy (auto-applied) | New table only, so backward-safe; review before merging |

## Out of scope (ask first)

References from other entities, restore of deleted rows, import flow, changes to Category, wider refactors of Filtering / Keyset / Caching / Behaviours.
