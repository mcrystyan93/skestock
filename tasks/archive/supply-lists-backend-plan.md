# Plan: Supply lists (backend)

Spec: `docs/specs/supply-lists.md`. Previous initiative archived in
`tasks/archive/observability-gaps-*.md`.

## Dependency graph

```
T1 domain + EF config + migration
   └─► T2 errors, models, line mapper/validation, cache constants
          ├─► T3 CreateSupplyList ─► T4 UpdateSupplyList
          ├─► T5 Disable/Enable
          └─► T6 GetSupplyListById ─► T7 GetAllSupplyLists (filter/sort/cursor)
                                           └─► T8 Web endpoints ─► T9 functional tests
```

T3–T7 are vertical slices over the shared T2 pieces; each ships with its unit tests.

## Order and checkpoints

1. T1 → **Checkpoint A:** `dotnet build`, migration generated,
   `has-pending-model-changes` clean.
2. T2, T3, T4, T5 → **Checkpoint B:** build + `SupplyLists` unit tests green.
3. T6, T7 → **Checkpoint C:** build + unit tests green.
4. T8, T9 → **Checkpoint D:** functional `SupplyLists` tests green, endpoints in Scalar,
   `graphify update .`.

## Risks

- Unique name must be case-insensitive on SQL Server and in the handler check: use a unique
  index on `Name` (default CI collation) plus a pre-check in handlers returning a typed 409.
- Replacing lines on update: remove + re-add (as `UpdateOrderList`); unique index
  `(SupplyListId, ItemId)` must not trip — EF deletes before inserts in the same SaveChanges.
- Keeping lines whose item was disabled later: the "must be active" check applies only to
  item ids not already on the list (handler check, not the validator).
- `IntervalWeeks` coherence enforced in validator and a DB check constraint.
