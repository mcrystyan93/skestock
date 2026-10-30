# Tasks: per-location low-stock thresholds

Backend tasks: write failing tests first. Details in `per-location-thresholds-plan.md`.

- [ ] T0 Branch `feature/per-location-thresholds`; commit SPEC.md + plan/todo
- [ ] T1 `ItemLocationThreshold` entity/config/DbSet/interface default member + migration `AddItemLocationThresholds` with cross-join backfill `Sql`
- [ ] Checkpoint A: build, no pending model changes, migration script reviewed
- [ ] T2 `ItemLocationThresholdDto`; `ItemDto.LocationThresholds`; mapper + GetItemById fill; GetAllItems drop; DbSet in Item test contexts
- [ ] T3 Create: request/command/endpoint/validator/handler + `DuplicateLocation`; unit + functional tests
- [ ] T4 Edit: add/update only, stop writing `Item.MinThreshold`; unit + functional tests
- [ ] T5 GetClassLocationStock/AdjustStock/GetSchoolClassSummary read per-location thresholds; test helpers; A-low/B-not tests; functional `LowStockOnly` test
- [ ] T6 Seed: threshold row per location in both demo item loops
- [ ] Checkpoint B: build, all .NET tests incl. functional, no pending model changes, `MinThreshold` grep clean
- [ ] T7 Client: `item.ts`, remove list column, `setItemImage` sends `[]`, `LocationsHttp.getAllPages()`, `error-codes.ts`, spec fixtures
- [ ] T8 Client form: locations rxResource, `applyEach` thresholds grid, skeleton/error states, modal `mapSaveRequest`, `form.spec.ts`
- [ ] Checkpoint C: `npm run build`, `npm test -- --watch=false`, prettier, manual AppHost run-through
- [ ] T9 Local-DB backfill count checks, `graphify update .`, Haiku review of diff, fix findings, open PR
