# Fruit & dessert message template editor: task list

Spec: `tasks/fruit-dessert-template-editor-spec.md`. Plan: `tasks/fruit-dessert-template-editor-plan.md` (read "Design decisions" and "API contract" first).
Branch: `feature/fruit-dessert-planning` (current, do not create another). TDD where there is logic: failing test, implement, refactor. Tick a box only after its Verify passes.
Implementer per task in `[brackets]`. Parallel waves are in the plan; never let two agents edit the same file at once.
Paths: `App/` = `src/Application/Features/FruitDessertTemplates/`, `UT/` = `tests/Application.UnitTests/Features/FruitDessertTemplates/`,
`FT/` = `tests/Application.FunctionalTests/Features/FruitDessertTemplates/`, `Tab/` = `src/Client/src/app/features/school-classes/overview/tabs/fruit-dessert/`,
`Svc/` = `src/Client/src/app/features/school-classes/services/`.
Client rules: `AGENTS.md`; no Prettier, match surrounding formatting by hand; Romanian UI; check every `nz-*` API in `.github/instructions/llms-full.txt`.

## Phase 0 — Spike

- [x] T1 Tiptap spike `[Sonnet]`
  - Outcome: `@tiptap/core`, `@tiptap/starter-kit`, `@tiptap/pm` all 3.32.2 (Tiptap v3). Direct `Editor`, no wrapper (ngx-tiptap peers accept Angular >=22 but add nothing needed here, so not installed). jsdom works with no mocks: `Editor` + StarterKit, `setContent('<p><strong>a</strong></p>')` -> `getHTML()` returned the same string (spec passed, then deleted). StarterKit v3 bundles bold, italic, strike, code, bulletList, orderedList, listItem, blockquote, codeBlock, heading, horizontalRule, hardBreak, paragraph, link and underline (v3 includes the last two; configure `link: false`/`underline: false` to exclude), plus undo/redo, dropcursor, gapcursor, listKeymap, trailingNode. Build: no warnings. Tests: 93 files / 450 passed.
  - Acceptance: `@tiptap/core`, `@tiptap/starter-kit`, `@tiptap/pm` installed (exact versions pinned by npm in lockfile); check `ngx-tiptap` peer deps for Angular 22 and decide wrapper vs direct `Editor` (default direct); a throwaway spec creates an `Editor` with StarterKit in Vitest/jsdom, sets `<p><strong>a</strong></p>`, reads back `getHTML()`; spec deleted after (or kept only if it becomes T10's spec). Outcome (versions, wrapper or direct, jsdom works or needs mocking, which StarterKit extensions exist in this version) written as a note under this task.
  - Verify: `cd src/Client && npm run build && npm test -- --watch=false`
  - Files: `src/Client/package.json`, `src/Client/package-lock.json`, temporary spec under `Tab/templates-modal/editor/`

**Checkpoint 0:** client build + tests green with Tiptap. Stop and report if Tiptap cannot work; do not pick another library without asking.

## Phase 1 — Backend core

- [ ] T2 Placeholder catalog + `WhatsAppFormatter` (tests first) `[Sonnet]`
  - Acceptance: `MessagePlaceholders` static catalog with `{ContactPerson}`, `{Date}`, `{Congregation}`, `{ClassName}`, each `(Token, Label RO, Example, MaxLength)`; MaxLength taken from the column limits (read `CongregationConfiguration`, `SchoolClassConfiguration`, `DataSchemaConstants`; date = longest Romanian formatted date). `WhatsAppFormatter.Sanitize(html)` rebuilds output from whitelist `p, br, strong, b, em, i, s, strike, code, ul, ol, li` with no attributes, text HTML-encoded, other tags dropped but their text kept (`script`/`style` content dropped entirely), comments dropped. `ToWhatsApp(html)` per plan mapping, entities decoded, whitespace moved outside markers, empty marks dropped, no-tag input returned unchanged. Tests: each mark, nested marks, paragraphs/br, bullet + ordered list, legacy plain text with newlines, `<script>`, `onerror=`, `javascript:` link, comment, unclosed tag, entities (`&amp;`, `&nbsp;`, `&lt;`).
  - Verify: `dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~FruitDessertTemplates"`
  - Files: `App/MessagePlaceholders.cs`, `App/WhatsAppFormatter.cs`, `UT/WhatsAppFormatterTests.cs`
- [ ] T3 Renderer uses catalog + formatter `[Sonnet]` (after T2)
  - Acceptance: `MessageTemplateRenderer.Render` converts with `WhatsAppFormatter.ToWhatsApp` first, then replaces placeholders; known set comes from `MessagePlaceholders` (no duplicated token literals); interface unchanged; unknown placeholder still fails with `UnknownPlaceholder`. New tests: HTML template renders WhatsApp markup with values substituted; values containing `<`/`*` are inserted verbatim; both seeded legacy bodies render exactly as before.
  - Verify: `dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~MessageTemplateRenderer"`
  - Files: `src/Infrastructure/FruitDessert/MessageTemplateRenderer.cs`, `tests/Application.UnitTests/Infrastructure/FruitDessert/MessageTemplateRendererTests.cs`
- [ ] T4 Migration `WidenMessageTemplateBody` `[Sonnet]` (parallel with T2)
  - Acceptance: `MessageTemplateConfiguration` Body `HasMaxLength(4000)`; migration generated only by the command; `Up` contains only one `AlterColumn` (1000 -> 4000) and `Down` the reverse; designer/snapshot not hand-edited; no pending model changes.
  - Verify: `dotnet ef migrations add WidenMessageTemplateBody --project src/Infrastructure --startup-project src/Web --context ApplicationDbContext --output-dir Data/Migrations && dotnet ef migrations has-pending-model-changes --project src/Infrastructure --startup-project src/Web --context ApplicationDbContext && dotnet build`
  - Files: `src/Infrastructure/Data/Configurations/MessageTemplateConfiguration.cs`, `src/Infrastructure/Data/Migrations/*_WidenMessageTemplateBody.cs` (+ generated Designer and snapshot)

**Checkpoint 1:** `dotnet build` green; `dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~FruitDessert"` green; migration reviewed by eye (widening only). Commit (explicit paths only).

## Phase 2 — Backend slice

- [ ] T5 `GetMessageTemplates` query `[Sonnet]` (after T2)
  - Acceptance: scaffold with `dotnet new ca-usecase -n GetMessageTemplates -fn FruitDessertTemplates -ut query -rt MessageTemplatesDto` (in `src/Application`); returns active templates ordered by `Type` as `{ type, name, body, lastModified }` plus `placeholders` projected from `MessagePlaceholders` (token, label, example; not MaxLength); `AsNoTracking`; not cached (one-line comment why). Handler test with the existing in-memory test DbContext pattern (`tests/Application.UnitTests/Features/FruitDessertPlans/FruitDessertTestDbContext.cs`): inactive rows excluded, placeholders equal the catalog.
  - Verify: `dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~GetMessageTemplates"`
  - Files: `App/Queries/GetMessageTemplates/*.cs` (query+handler as scaffolded), `App/Models/MessageTemplatesDto.cs`, `UT/Queries/GetMessageTemplatesQueryHandlerTests.cs`
- [ ] T6 `UpdateMessageTemplate` command + validator `[Sonnet]` (after T2)
  - Acceptance: command `(FruitDessertMessageType Type, string Name, string Body)`; validator: Type is a defined enum value, Name required <= 100, Body required <= 4000, no unknown placeholder in `ToWhatsApp(Body)` (Romanian message listing tokens), worst-case rendered length (placeholders at MaxLength) <= 2000 (Romanian message with the limit); handler loads the active row of the type (missing -> `ActiveTemplateMissing`), sets `Name` and `Body = WhatsAppFormatter.Sanitize(Body)`, saves; returns `Result`. Tests: validator each rule (boundary 2000 exact passes, 2001 fails), handler sanitizes and updates in place (row count unchanged), missing active -> error.
  - Verify: `dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~UpdateMessageTemplate"`
  - Files: `App/Commands/UpdateMessageTemplate/{UpdateMessageTemplateCommand,UpdateMessageTemplateCommandValidator,UpdateMessageTemplateCommandHandler}.cs`, `UT/Commands/UpdateMessageTemplateCommandValidatorTests.cs`, `UT/Commands/UpdateMessageTemplateCommandHandlerTests.cs`
- [ ] T7 Endpoint + functional tests `[Sonnet]` (after T3-T6)
  - Acceptance: `FruitDessertTemplates : IEndpointGroup` with `GET /` and `PUT /{type}` (body `{ name, body }`, 204 on success, failed results via `ToProblemHttpResult()`), same authorization as `FruitDessertPlans.cs`; request record in `App/Models` if the sibling pattern does so. Functional tests: GET returns 2 templates + 4 placeholders; PUT persists sanitized HTML (GET shows it); PUT with `{Foo}` -> 400; PUT with body > 4000 -> 400; send after edit: `SendFruitDessertMessage` on an entry stores a `FruitDessertMessage.Body` with the new text in WhatsApp markup and substituted values (reuse setup from `tests/Application.FunctionalTests/Features/FruitDessertPlans/FruitDessertPlanCommandsTests.cs`).
  - Verify: `dotnet build && ./run-functional-tests.sh --filter "FullyQualifiedName~FruitDessert"`
  - Files: `src/Web/Endpoints/FruitDessertTemplates.cs`, `App/Models/UpdateMessageTemplateRequest.cs` (only if needed), `FT/MessageTemplateTests.cs`

**Checkpoint 2:** `dotnet build`; `dotnet test tests/Application.UnitTests`; `./run-functional-tests.sh --filter "FullyQualifiedName~FruitDessert"` all green. API visible in `/scalar`. Commit.

## Phase 3 — Client

- [ ] T8 Models + http `[Sonnet]` (parallel with Phase 1, contract in plan)
  - Acceptance: types `MessageTemplateDto`, `MessagePlaceholderDto`, `MessageTemplatesDto`, `UpdateMessageTemplateRequest` added to `core/models/fruit-dessert.ts` (reuse `FruitDessertMessageType`), exported via the existing models index if there is one; `FruitDessertTemplatesHttp` with `get()` and `update(type, request)` following `fruit-dessert-plans.http.ts` (base URL, `@Service()`/`inject` as there).
  - Verify: `cd src/Client && npm run build`
  - Files: `src/Client/src/app/core/models/fruit-dessert.ts`, `Svc/fruit-dessert-templates.http.ts`
- [ ] T9 Store + spec `[Sonnet]` (after T8)
  - Acceptance: `FruitDessertTemplatesStore` (`@ngrx/signals`, pattern of `fruit-dessert.store.ts`), not `providedIn: 'root'` (provided by the modal): `load()`, `save(type, request)` updating the local template on success, `loading`/`saving` signals, server error message exposed (ProblemDetails `errors`/`detail`). Spec: load fills templates + placeholders; save success updates state; save 400 exposes message and keeps state.
  - Verify: `cd src/Client && npm test -- --watch=false`
  - Files: `Svc/fruit-dessert-templates.store.ts`, `Svc/fruit-dessert-templates.store.spec.ts`
- [ ] T10 Rich text editor component `[Sonnet]` (after T1)
  - Acceptance: standalone component per T1 decision; Signal Forms custom control (`FormValueControl<string>`, `value = model<string>()`); StarterKit limited to the subset in the plan; toolbar `nz-button` group: B, I, S, cod, listă (active state reflects `editor.isActive`, `aria-label` + `nz-tooltip` in Romanian); placeholder dropdown `nz-dropdown` + `nz-menu` from a `placeholders` input (label shown, token inserted at cursor via `insertContent`); counter "≈ N / 2000"; editor destroyed in `DestroyRef.onDestroy`; external `value` changes update content without echo loops. Spec: initial value loads, toolbar toggles bold, placeholder click inserts token at cursor, counter updates (mock `Editor` if T1 found jsdom unusable).
  - Verify: `cd src/Client && npm run build && npm test -- --watch=false`
  - Files: `Tab/templates-modal/editor/template-editor.ts`, `.html`, `.spec.ts`

**Checkpoint 3a:** client build + tests green.

- [ ] T11 Templates modal `[Sonnet]` (after T9, T10)
  - Acceptance: component opened with `NzModalService.create` (pattern `features/class-configuration/departments/modal/department-modal.ts` + `form.ts`); provides the store; `nz-tabs` "Reminder" / "Mulțumire"; per template a Signal Form with Name (`nz-input`, required, max 100) and the editor; "Salvează" per tab, disabled while saving/invalid; `NzMessageService` success "Șablon salvat"; server validation error shown in the tab (`nz-alert`); loading state with `nz-spin`. Spec: loads and shows both templates; save calls store with form values; server error is displayed.
  - Verify: `cd src/Client && npm run build && npm test -- --watch=false`
  - Files: `Tab/templates-modal/templates-modal.ts`, `.html`, `.spec.ts`
- [ ] T12 Header button `[Sonnet]` (after T11)
  - Acceptance: `case 7` in `action` returns `{text: 'Șabloane mesaje', label: 'Șabloane mesaje', icon: <existing icon from the icon catalog, e.g. a message/edit icon>, run: opens the modal}`; the header template shows text on wide screens and icon only + `nz-tooltip` on narrow screens (extend the existing action button markup, reuse Tailwind breakpoint classes already used in the header); hidden when class is closed (existing rule). Spec in `header.spec.ts`: tab 7 shows the button and clicking it opens the modal (mock `NzModalService`).
  - Verify: `cd src/Client && npm run build && npm test -- --watch=false`
  - Files: `src/Client/src/app/features/school-classes/overview/header/{header.ts,header.html,header.spec.ts}`

**Checkpoint 3:** `npm run build` + `npm test -- --watch=false` green. Commit.

## Phase 4 — Verify, review, ship

- [ ] T13 Full verification `[Sonnet]`
  - Acceptance: all green and results reported (including any failure): `dotnet build`; `dotnet test tests/Application.UnitTests`; `dotnet test tests/Worker.UnitTests`; `dotnet test tests/Infrastructure.IntegrationTests`; `./run-functional-tests.sh`; `dotnet ef migrations has-pending-model-changes ...`; `cd src/Client && npm run build && npm test -- --watch=false`. Optional smoke: `aspire start --apphost src/AppHost/AppHost.csproj`, edit a template with bold + list + placeholder, send a message, check the wa.me text, `aspire stop`.
  - Verify: the commands above
  - Files: none
- [ ] T14 Independent review `[haiku]` (CLAUDE.md section 9)
  - Acceptance: haiku subagent gets only the task goal (spec "Objective" + "Success Criteria") and `git diff master...HEAD -- <feature paths>` (exclude the unrelated files listed in T16); no implementer reasoning. Returns findings list.
  - Verify: findings list received
  - Files: none
- [ ] T15 Resolve review findings `[Sonnet]`
  - Acceptance: real issues fixed with tests; the rest listed in the PR description as follow-ups; relevant build/tests rerun.
  - Verify: the Checkpoint 2 and 3 commands
  - Files: as findings require (max ~5 per fix)
- [ ] T16 graphify + commit + PR `[Sonnet]`
  - Acceptance: `graphify update .` run from repo root; commits stage feature files by explicit path only. Never stage these unrelated working-tree changes: `src/Client/src/app/features/categories/list/tables/import/large/table.ts`, `src/Client/src/app/features/items/list/tables/import/large/table.ts`, `src/Client/src/app/features/login/login.page.ts`, `src/Client/src/app/shared/class-statistics/services/class-statistics.http.ts`, `src/Client/src/app/shared/order-lists/services/order-list-wizard.store.ts`, `src/Client/src/app/shared/order-lists/ui/modals/detail/form/suggestions/order-list-suggestions-panel.ts`, `src/Client/src/app/shared/stock-batches/ui/table/table.ts`, `src/Client/src/styles/tailwind.css`. `git diff --cached --stat` checked before each commit. Push `feature/fruit-dessert-planning`; if `gh pr list --head feature/fruit-dessert-planning` shows an open PR, update its description with a "Message template editor" section instead of opening a new one; otherwise open a PR to `master`. Description: summary, migration note (widening only, auto-applied on deploy), test results from T13, review follow-ups from T15, attribution line.
  - Verify: `git status` shows only the unrelated files still modified; PR URL
  - Files: none (git only)
