# Congregation: task list

Spec: `docs/specs/congregation.md`. Plan: `tasks/congregation-plan.md`. Branch: `feature/congregations`.
Backend is TDD: write the failing test, implement, refactor. Tick a box only after its Verify step passes.

## Backend

- [x] B1 Domain: `Congregation` entity + `CongregationErrors` (events moved to B3/B4/B5: the Mediator source generator fails the build for an event without a handler)
  - Deps: none
  - Acceptance: entity matches spec 3.1 (incl. `DeletedDate`); events exist
  - Verify: `dotnet build`
  - Files: `Domain/Entities/Congregations/Congregation.cs`, `Application/Common/Errors/CongregationErrors.cs`
- [x] B2 (done together with B3: the Mediator generator rejects a command without a handler) Create: validator tests, then `CreateCongregationCommand` + validator
  - Deps: B1
  - Acceptance: AC-B2, AC-B3 (create side)
  - Verify: `dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~CreateCongregation"`
  - Files: `Commands/CreateCongregation/*`, matching unit tests
- [x] B3 Create: handler tests, then `CongregationCreatedEvent` + its event handler (tests first), handler, `CongregationMutationDto`, `CacheConstants`, cache invalidation, created event raised
  - Deps: B2
  - Acceptance: AC-B1, AC-B7 (create), AC-B8 (create)
  - Verify: same filter as B2
  - Files: handler, `Models/*`, `CacheConstants.cs`, unit tests
- [x] B4 Update: tests, then `CongregationUpdatedEvent` + event handler, `UpdateCongregationCommand` (validator + handler + invalidation + updated event)
  - Deps: B3
  - Acceptance: AC-B2, AC-B3 (update side), AC-B4, AC-B7, AC-B8
  - Verify: `--filter "FullyQualifiedName~UpdateCongregation"`
  - Files: `Commands/UpdateCongregation/*`, unit tests
- [x] B5 Delete: tests, then `CongregationDeletedEvent` + event handler, `DeleteCongregationCommand` (soft delete + invalidation + deleted event)
  - Deps: B3
  - Acceptance: AC-B5, AC-B7, AC-B8
  - Verify: `--filter "FullyQualifiedName~DeleteCongregation"`
  - Files: `Commands/DeleteCongregation/*`, unit tests
- [x] B6 List: tests, then filter and sort configs, cursor, `GetAllCongregationsQuery` (+ validator, handler, cache) (`canReceiveStudents` is a generic boolean filter, no dedicated request field)
  - Deps: B3
  - Acceptance: AC-B6, AC-B7; extend `SortConfigurationConsistencyTests`
  - Verify: `--filter "FullyQualifiedName~GetAllCongregations|FullyQualifiedName~SortConfigurationConsistency"`
  - Files: `Queries/GetAllCongregations/*`, `CongregationFilterConfiguration.cs`, `CongregationSortConfiguration.cs`, `CongregationCursor.cs`
  - Note: first check that `Common/Filtering` supports bool fields. If not, add tests there first (fragile area).
- [x] B7 Get by id: tests, then `GetCongregationByIdQuery`
  - Deps: B3
  - Acceptance: AC-B4 (not found), AC-B5 (deleted not returned)
  - Verify: `--filter "FullyQualifiedName~GetCongregationById"`
  - Files: `Queries/GetCongregationById/*`
- [x] B8 Realtime constants (`RealtimeEvents.Congregation*`, `RealtimeGroups.CongregationsList`): done in B3. Event handlers only notify the SignalR group; cache invalidation stays on `ICacheInvalidation` (Congregation has no cross-feature cache tags)
  - Deps: B3
  - Acceptance: AC-B8
  - Verify: `--filter "FullyQualifiedName~Congregation"`
  - Files: `EventHandlers/*`, `Common/Realtime/*`
- [x] B9 EF (the `DbSet<Congregation>` default interface member in `IApplicationDbContext` and the `ApplicationDbContext` property are already done; no test context needed changes): `DbSet` in `IApplicationDbContext` + `ApplicationDbContext`, `CongregationConfiguration`, generate migration `AddCongregations`
  - Deps: B1
  - Acceptance: AC-B9; query filter + filtered unique index per spec 3.2
  - Verify: `dotnet ef migrations has-pending-model-changes ...` reports none; read the generated migration before moving on
  - Files: `ApplicationDbContext.cs`, `IApplicationDbContext.cs`, `CongregationConfiguration.cs`, migration (generated)
- [x] B10 Endpoints `Congregations.cs` + request models, then functional tests
  - Deps: B4, B5, B6, B7, B8, B9
  - Acceptance: contract in spec 4; functional round trip
  - Verify: `./run-functional-tests.sh --filter "FullyQualifiedName~Congregation"`
  - Files: `Web/Endpoints/Congregations.cs`, `Models/CongregationRequests.cs`, functional tests
- [x] B-GATE `dotnet build`, all unit tests, functional tests, `has-pending-model-changes`

## Frontend

- [x] F1 Models + HTTP: `core/models/congregation.ts`, `congregations.http.ts`
  - Deps: B10 (contract)
  - Acceptance: types mirror spec 4
  - Verify: `npm run build`
- [x] F2 Collection feature + detail store + dropdown store (with working realtime reducers) + specs
  - Deps: F1
  - Acceptance: AC-F7 (store part), AC-F8
  - Verify: `npm test -- --watch=false`
- [x] F3 Modal form (Signal Forms) + modal container + spec
  - Deps: F2
  - Acceptance: AC-F4, AC-F5
  - Verify: `npm test -- --watch=false`
- [x] F4 `ske-congregation-dropdown` incl. `canReceiveStudents` input, inline create/edit + spec
  - Deps: F2, F3
  - Acceptance: AC-F7
  - Verify: `npm test -- --watch=false`
- [x] F5 List store with working delete + realtime + spec
  - Deps: F2
  - Acceptance: AC-F6 (store part), AC-F8
  - Verify: `npm test -- --watch=false`
- [x] F6 Filter form (two `nz-segmented` + search), large table, small list + specs
  - Deps: F5
  - Acceptance: AC-F2, AC-F3
  - Verify: `npm test -- --watch=false`
- [x] F7 Page container, lazy route in `full.routes.ts`, barrels, sidebar item in `full.html`
  - Deps: F3, F6
  - Acceptance: AC-F1, AC-F6
  - Verify: `npm run build`; manual check through AppHost
- [x] F-GATE `npm run build`, `npm test -- --watch=false`, manual walk-through

## Wrap-up

- [x] W1 Independent haiku review of the diff; fix real findings
- [x] W2 `graphify update .`
- [x] W3 Push branch, open PR (never commit to `master`)
- [x] W4 Phase 5: check every AC in the spec and report met / not met
