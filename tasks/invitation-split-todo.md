# Todo: Impărțirea invitațiilor

Spec: `tasks/invitation-split-spec.md` · Plan: `tasks/invitation-split-plan.md`.
Field names (confirm before task 2): `StudentInvitationCount` (Cursanți), `InstructorInvitationCount` (Instructori), `OpeningPrayerInvitationCount` (Rugăciune deschidere), `ExperienceInvitationCount` (Experiențe), `ChangeInvitationCount` (Schimbări).

- [x] 1. Create branch `feature/invitation-split` from `master`
  - Acceptance: work happens on the branch; `master` untouched.
  - Verify: `git branch --show-current`
  - Files: none

- [x] 2. Schema: add the 5 columns to shared configuration and school class, one migration
  - Acceptance: `SharedClassConfiguration` has 5 `int` (`IsRequired().HasDefaultValue(0)`); `SchoolClass` has 5 `int?`; migration `AddInvitationSplit` adds all 10 columns additively (`defaultValue: 0` on shared, nullable on classes); if EF toggles `SYSTEM_VERSIONING` on either table, the 2-year retention line is re-appended for it.
  - Verify: `dotnet build`; `dotnet ef migrations has-pending-model-changes --project src/Infrastructure --startup-project src/Web --context ApplicationDbContext`; `dotnet test tests/Infrastructure.IntegrationTests --filter "FullyQualifiedName~TemporalSchema"`
  - Files: `src/Domain/Entities/SchoolClasses/SharedClassConfiguration.cs`, `src/Domain/Entities/SchoolClasses/SchoolClass.cs`, `src/Infrastructure/Data/Configurations/SharedClassConfigurationConfiguration.cs`, `src/Infrastructure/Data/Migrations/<ts>_AddInvitationSplit.cs` (+ generated Designer/snapshot)

- [x] 3. Backend: query + command + validator for the global split
  - Acceptance: `GetInvitationSplitQuery` returns `InvitationSplitDto` (0s when the singleton is missing, as rooms does), cached with `ConfigurationTag` + `InvitationSplitTag`; `SaveInvitationSplitCommand` is `[Authorize(Roles = Roles.Administrator)]`, 5 × `int?` validated NotNull/0..200 with Romanian messages, upserts the singleton touching only its 5 fields, invalidates both tags.
  - Verify: `dotnet build`
  - Files: `src/Application/Features/ClassConfiguration/CacheConstants.cs`, `.../Models/InvitationSplitDto.cs`, `.../Queries/GetInvitationSplit/{GetInvitationSplitQuery,GetInvitationSplitQueryHandler}.cs`, `.../Commands/SaveInvitationSplit/{SaveInvitationSplitCommand,SaveInvitationSplitCommandHandler,SaveInvitationSplitCommandValidator}.cs`

- [x] 4. Backend: unit tests for the split slice
  - Acceptance: validator (null, -1, 0, 200, 201 per field); handler creates the singleton, updates it, keeps `InvitationCount`/rooms/departments; query returns saved values and 0s when missing.
  - Verify: `dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~InvitationSplit"`
  - Files: `tests/Application.UnitTests/Features/ClassConfiguration/InvitationSplitValidatorTests.cs`, `InvitationSplitCommandTests.cs`, `InvitationSplitQueryTests.cs` (reuse `ClassConfigurationTestDbContext.cs`)

- [x] 5. Backend: endpoint + functional tests
  - Acceptance: `GET /api/ClassConfiguration/invitation-split` (authenticated), `PUT` (administrator); round trip; 403 non-admin; 400 invalid; GET after PUT is fresh (cache); saving rooms/invitations does not reset the split and vice versa.
  - Verify: `./run-functional-tests.sh --filter "FullyQualifiedName~InvitationSplit"` (AppHost running)
  - Files: `src/Web/Endpoints/ClassConfiguration.cs`, `tests/Application.FunctionalTests/Features/ClassConfiguration/InvitationSplitEndpointTests.cs`, `InvitationSplitTests.cs`

- [x] 6. Per-class copy: create + initialize + DTO
  - Acceptance: `CreateSchoolClassCommandHandler` copies the 5 values and returns them; `InitializeSchoolClassConfigurationCommandHandler` sets them in the existing claim `ExecuteUpdate` (keeping the `LastModifiedById`/`LastModifiedDate` stamps); `SchoolClassDto` has 5 `int?`, mapped in Create and `GetSchoolClassByIdHandler`.
  - Verify: `dotnet build`
  - Files: `src/Application/Features/SchoolClasses/Models/SchoolClassDto.cs`, `.../Commands/CreateSchoolClass/CreateSchoolClassCommandHandler.cs`, `.../Commands/InitializeSchoolClassConfiguration/InitializeSchoolClassConfigurationCommandHandler.cs`, `.../Queries/GetSchoolClassById/GetSchoolClassByIdHandler.cs`

- [x] 7. Per-class copy: tests
  - Acceptance: unit: create copies the 5 values; get-by-id maps them. Functional: create and initialize copy the current values once; a later configuration change does not alter the class.
  - Verify: `dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~CreateSchoolClass|FullyQualifiedName~GetSchoolClassById"`; `./run-functional-tests.sh --filter "FullyQualifiedName~ClassConfigurationSnapshot"`
  - Files: `tests/Application.UnitTests/Features/SchoolClasses/Commands/CreateSchoolClass/CreateSchoolClassCommandHandlerTests.cs`, `tests/Application.UnitTests/Features/SchoolClasses/Queries/GetSchoolClassById/GetSchoolClassByIdHandlerTests.cs`, `tests/Application.FunctionalTests/Features/ClassConfiguration/ClassConfigurationSnapshotTests.cs`

- [x] 8. Client data: models, http, store feature + specs
  - Acceptance: `InvitationSplitDto`/`SaveInvitationSplitRequest` types; `getInvitationSplit()`/`saveInvitationSplit()` hit `/invitation-split`; `withInvitationSplit()` (load on init, save then reload, loading/problemDetail/validationErrors) added to `ConfigurationStore`; `MAX_INVITATION_SPLIT_COUNT = 200`.
  - Verify: `cd src/Client && npm test -- --watch=false`
  - Files: `src/Client/src/app/core/models/class-configuration.ts`, `src/Client/src/app/features/class-configuration/services/configuration.http.ts` (+ `configuration.http.spec.ts`), `services/invitation-split.feature.ts` + `invitation-split.feature.spec.ts`, `services/configuration.store.ts`, `services/configuration.constants.ts`

- [x] 9. Client UI: card + section on the configuration page + specs
  - Acceptance: section „Impărțirea invitațiilor” after the invitations section; card with 5 required `nz-input-number` (0..200, `w-full!`, aria-invalid/aria-describedby, Romanian errors), own „Salvează” (as in rooms, no UI role gating; the server enforces administrator), informational „Total alocat: X din Y” (X = live form sum, Y = `store.invitationCount()`), no blocking; NG-ZORRO API checked in `.github/instructions/llms-full.txt`.
  - Verify: `cd src/Client && npm run build && npm test -- --watch=false`
  - Files: `src/Client/src/app/features/class-configuration/invitation-split/invitation-split-card.{ts,html,spec.ts}`, `invitation-split/invitation-split-section.{ts,html,spec.ts}`, `page/configuration.page.html` (+ `configuration.page.ts` imports)

- [x] 10. E2E spec + screenshots
  - Acceptance: `e2e/specs/invitation-split.spec.ts` (mocked `**/api/ClassConfiguration/invitation-split` and `/invitations`, login as in `room-configuration.spec.ts`): loads values, tab order, clamp to 200, empty-field error, total line updates, save sends the PUT and persists after reload, server error state, loading state; `saveScreenshotsAfterEach()` writes to `screenshot-checks/invitation-split/` (git-ignored); screenshots checked for alignment, labels, spacing, focus, contrast, no overflow on desktop and mobile.
  - Verify: `./run-e2e.sh -- --project=chromium --project=mobile e2e/specs/invitation-split.spec.ts`; view the PNGs (Read with `limit`)
  - Files: `src/Client/e2e/specs/invitation-split.spec.ts`

- [x] 11. Independent review (Haiku)
  - Acceptance: sub-agent with `model: "haiku"` gets only `git diff master...` and the task goal; real issues fixed, rest queued or reported.
  - Verify: re-run the affected build/tests after fixes
  - Files: as findings require

- [x] 12. Full verification + graphify
  - Acceptance: `dotnet build` clean; Application.UnitTests, functional (ClassConfiguration + SchoolClasses), TemporalSchema, client build + tests, e2e all green; graph updated.
  - Verify: commands above; `graphify update .` from repo root
  - Files: `graphify-out/` (generated)

- [ ] 13. Commit, push, open PR
  - Acceptance: commits on `feature/invitation-split`; PR to `master` describing the change, migration (additive, temporal retention checked) and test results; ends with the Claude Code attribution line.
  - Verify: `gh pr view --web` / PR URL returned
  - Files: none
