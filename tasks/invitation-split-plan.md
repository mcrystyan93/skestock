# Plan: Impărțirea invitațiilor

Spec: `tasks/invitation-split-spec.md`. Pattern to copy: the rooms slice (`SaveRoomConfiguration`, `GetRoomConfiguration`, `rooms.feature.ts`, `rooms-section`, `rooms-card`, `room-configuration.spec.ts`).

## Components

1. **Schema** — `SharedClassConfiguration` +5 `int` (default 0), `SchoolClass` +5 `int?` (null, like `InvitationCount`), one migration `AddInvitationSplit`.
2. **Global API** — `InvitationSplitDto`, `GetInvitationSplitQuery` (cacheable, `ConfigurationTag` + `InvitationSplitTag`), `SaveInvitationSplitCommand` (admin, 5 × `int?` NotNull 0..200, invalidates both tags), `GET/PUT /api/ClassConfiguration/invitation-split`.
3. **Per-class copy** — `CreateSchoolClassCommandHandler` sets the 5 values from the singleton; `InitializeSchoolClassConfigurationCommandHandler` adds 5 `SetProperty` calls to the existing claim `ExecuteUpdate`; `SchoolClassDto` +5 `int?`, mapped in Create and `GetSchoolClassByIdHandler`.
4. **Client data** — models in `core/models/class-configuration.ts`, `getInvitationSplit`/`saveInvitationSplit` in `configuration.http.ts`, `invitation-split.feature.ts` (`withInvitationSplit()`), added to `ConfigurationStore`.
5. **Client UI** — `invitation-split/invitation-split-card` (5 `nz-input-number`, signal form, max `MAX_ROOM_SEAT_COUNT`-style constant `MAX_INVITATION_SPLIT_COUNT = 200`, informational „Total alocat: X din Y”), `invitation-split-section` (error display + save), placed right after `<ske-configuration-invitations-section>` in `configuration.page.html`.
6. **E2E** — `e2e/specs/invitation-split.spec.ts`, mocked API, `saveScreenshotsAfterEach()` → `screenshot-checks/invitation-split/`.

## Dependency order

```
1 Schema ──► 2 Global API ──► 4 Client data ──► 5 Client UI ──► 6 E2E
        └──► 3 Per-class copy
```
Then: Haiku review → fixes → `graphify update .` → PR.

## Parallelizable

- After step 1: step 2 and step 3 are independent (different features/files).
- Step 4/5 only need the API contract (route + JSON shape), so the client can start once the DTO and route names are fixed, in parallel with step 3.
- In practice the work is small; run it sequentially unless split across agents.

## Risks

| Risk | Mitigation |
|---|---|
| First column add since `SetTemporalRetention`: EF may toggle `SYSTEM_VERSIONING` on `SharedClassConfigurations` / `SchoolClasses` and drop the 2-year retention. | Read the generated migration; if it emits versioning off/on, re-append `ALTER TABLE ... SET (SYSTEM_VERSIONING = ON (HISTORY_RETENTION_PERIOD = 2 YEARS))` for both tables. Run `TemporalSchemaTests`. |
| Migration auto-applies on deploy. | Additive only: NOT NULL with `defaultValue: 0` on shared, nullable on classes. `has-pending-model-changes` clean. |
| Other PUTs reset the new columns (or vice versa). | Each handler loads the singleton and sets only its own fields; covered by a handler test and a functional test. |
| `ExecuteUpdate` on temporal `SchoolClasses` bypasses the interceptor. | Keep the existing `LastModifiedById`/`LastModifiedDate` stamps in the same call; only add `SetProperty` lines. |
| Cache: GET serves stale data after save. | New `InvitationSplitTag` in `CacheConstants.cs`; command invalidates `ConfigurationTag` + `InvitationSplitTag`; functional refresh test. |
| `IsConfigurationInitialized` still keys off `InvitationCount`. | Unchanged on purpose; the 5 values are written in the same claim, so they are null exactly when `InvitationCount` is. |
| `nz-input-number` clamps on blur, so >200 never reaches the validator. | E2E asserts clamp (as rooms does); validator unit test covers 201 server side. |

## Verification checkpoints

1. After schema: `dotnet build`; `dotnet ef migrations has-pending-model-changes ...` reports none; migration reviewed for retention.
2. After global API: `dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~InvitationSplit"`; functional `./run-functional-tests.sh --filter "FullyQualifiedName~InvitationSplit"` (AppHost up).
3. After per-class copy: unit `~CreateSchoolClass|~GetSchoolClassById`, functional `~ClassConfigurationSnapshot`; `dotnet test tests/Infrastructure.IntegrationTests --filter "FullyQualifiedName~TemporalSchema"`.
4. After client: `cd src/Client && npm run build && npm test -- --watch=false`.
5. After e2e: `./run-e2e.sh -- --project=chromium --project=mobile e2e/specs/invitation-split.spec.ts`; inspect screenshots in `screenshot-checks/invitation-split/`.
6. Before PR: full `dotnet build`, all unit + functional + client tests green; Haiku review addressed.
