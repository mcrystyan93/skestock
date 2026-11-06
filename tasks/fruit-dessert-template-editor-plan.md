# Plan: Editor de șabloane mesaje (Fructe și desert)

Spec (approved): `tasks/fruit-dessert-template-editor-spec.md`. Tasks: `tasks/fruit-dessert-template-editor-todo.md`.
Branch: `feature/fruit-dessert-planning` (current). Implementers: Sonnet subagents; final review: haiku subagent (CLAUDE.md section 9).
Engineering level: production grade for data safety (migration, sanitization, server validation), lean everywhere else.

## Resolved open questions (user decisions)

1. Sanitization: server side, manual whitelist `p, br, strong, b, em, i, s, strike, code, ul, ol, li`. Every other tag is dropped (its text kept), every attribute is dropped. No new NuGet package.
2. `MessageTemplate.Body` MaxLength 1000 -> 4000 via a new EF migration (widening `nvarchar(1000)` -> `nvarchar(4000)` only, backward-safe, no data change).
3. Header button: icon + text "Șabloane mesaje"; icon only with `nz-tooltip` on narrow screens.
4. Task 1 is a spike: Tiptap with Angular 22 / this build / Vitest. Default to Tiptap's `Editor` used directly in one component; use `ngx-tiptap` only if its peer deps accept Angular 22 and it saves real code. Adding Tiptap is approved.

## Design decisions (made in this plan)

- **Shared pure code lives in Application, not Infrastructure.** The validator (Application) and the renderer (Infrastructure) both need the placeholder catalog and the HTML -> WhatsApp conversion, and Application cannot reference Infrastructure. So:
  - `src/Application/Features/FruitDessertTemplates/MessagePlaceholders.cs`: static catalog, one entry per placeholder `(Token, Label (RO), Example, MaxLength)`. Single source for the renderer's known set, the validator and the GET response.
  - `src/Application/Features/FruitDessertTemplates/WhatsAppFormatter.cs`: static, dependency-free. `Sanitize(html)` (whitelist) and `ToWhatsApp(html)` (`<strong>/<b>` -> `*x*`, `<em>/<i>` -> `_x_`, `<s>/<strike>` -> `~x~`, `<code>` -> `` `x` ``, `<p>` -> line, `<br>` -> newline, `<ul><li>` -> `- x`, `<ol><li>` -> `1. x`, HTML entities decoded). Input with no tag = legacy plain text, returned unchanged (newlines kept).
  - `MessageTemplateRenderer` keeps its interface and date formatting; it now does `ToWhatsApp` first, then placeholder replacement, and takes its known-token set from `MessagePlaceholders`. Order matters: converting first means placeholder values (names) are never parsed as HTML.
- **Length rule is a worst case, not an example.** Validator: `ToWhatsApp(body)` with each placeholder counted at its `MaxLength` (from the column limits: contact person 100, congregation and class name `DataSchemaConstants.DEFAULT_NAME_LENGTH`, date fixed ~30) must be <= 2000. That guarantees the send-time snapshot (`FruitDessertMessage.Body` max 2000) can never overflow, so no send-handler change is needed.
- **Unknown placeholder at save = FluentValidation error (400)**, message in Romanian, listing the tokens. Send keeps its existing 409 for safety (legacy/hand-edited rows).
- **Update edits the active row of the type in place** (no new row), so `UX_MessageTemplates_Type_Active` is untouched. Missing active row -> reuse `FruitDessertErrors.ActiveTemplateMissing` (409). No new error class.
- **No caching** of templates (they are not cached today and the send handler reads them from DB). No `ICacheableQuery`/`ICacheInvalidation` for this slice; note it in the query with a one-line comment. No `IRequiresOpenClass` (templates are global, not class data).
- **Authorization**: same group-level `RequireAuthorization()` as `FruitDessertPlans` endpoints; no roles (spec decision).
- **Client sanitization**: the editor loads HTML through Tiptap's schema (unknown nodes/marks are discarded by ProseMirror); the HTML is never bound with `[innerHTML]`.
- **Client store**: new small `fruit-dessert-templates.http.ts` + `fruit-dessert-templates.store.ts` in `features/school-classes/services/`, provided by the modal (scoped, not root). Placeholders come from the GET response.
- **Editor component**: `Tab/templates-modal/editor/` (only used here, so not in `shared/ui`). Signal Forms custom control (`FormValueControl`, `value = model<string>()`). StarterKit configured down to: paragraph, text, hardBreak, bold, italic, strike, code, bulletList, listItem (disable heading, blockquote, codeBlock, horizontalRule, orderedList, link, underline and anything else StarterKit enables in the installed version). Placeholder dropdown: `nz-dropdown` + `nz-menu`, item click -> `editor.chain().focus().insertContent(token).run()` (inserted as plain text, so marks are not split by us). Counter: `editor.getText().length` / 2000, labelled approximate; the server is the authority and its error is shown.
- **Header**: case 7 in `action` returns the new action; it inherits the existing "hidden when class is closed" rule (consistent with other actions; templates are global so this is cosmetic, accepted).

## API contract (fixed before parallel work)

```
GET  /api/FruitDessertTemplates
  200 { templates: [{ type: "reminder" | "thanks", name, body, lastModified }],
        placeholders: [{ token: "{ContactPerson}", label: "Persoana de contact", example: "..." }] }
PUT  /api/FruitDessertTemplates/{type}      type = reminder | thanks (JsonStringEnumConverter, camelCase)
  body { name, body }
  204 | 400 ValidationProblem (empty/too long name, body > 4000, unknown placeholder, rendered > 2000) | 409 ActiveTemplateMissing
```

Client enum `FruitDessertMessageType` already mirrors the backend enum (`core/models/fruit-dessert.ts`).

## Components and dependency graph

```
T1 Tiptap spike ───────────────────────────────┐
                                               ├─> T10 Editor component ─┐
T2 Catalog + WhatsAppFormatter ─┬─> T3 Renderer │                         │
                                ├─> T5 GET query│                         ├─> T11 Modal ─> T12 Header button
                                └─> T6 PUT cmd  │                         │
T4 Migration (Body 4000) ───────────────────────┤                         │
T3 + T4 + T5 + T6 ─> T7 Endpoint + functional tests                       │
T8 Client models + http ─> T9 Store ──────────────────────────────────────┘
T12 + T7 ─> T13 Full verification ─> T14 Haiku review ─> T15 Fix findings ─> T16 graphify + commit + PR
```

### Parallel vs sequential

- Wave A (parallel): T1 (client spike), T2 (backend pure code), T4 (migration), T8 (client models + http, contract above is fixed).
- Wave B (parallel): T3, T5, T6 (all need T2); T9 (needs T8); T10 (needs T1).
- Wave C: T7 (needs T3-T6); T11 (needs T9, T10).
- Wave D (sequential): T12, T13, T14, T15, T16.
- Constraint: two subagents must never edit the same file in one wave. Shared-file hot spots: `package.json`/`package-lock.json` (T1 only), `core/models/fruit-dessert.ts` (T8 only), `Directory.Packages.props` (nobody). `dotnet build` / `npm` runs from parallel agents can collide on `obj/`; if a build fails with a file lock, rerun before debugging.

## Vertical slices

Backend is shippable at Checkpoint 2 (API usable via Scalar, sending already converts formatting). Client is shippable at Checkpoint 3. Nothing is merged before Checkpoint 4.

## Risks and mitigations

| Risk | Mitigation |
|------|------------|
| Tiptap / ProseMirror does not run in Vitest jsdom or breaks the Angular 22 build | T1 spike before any editor work; fallback = `Editor` directly (no wrapper). If jsdom fails, editor spec mocks the `Editor` and modal spec stubs the editor component; record the outcome in T1. |
| StarterKit (v3) enables more than the WhatsApp subset (link, underline, headings) | Explicitly disable in config; server sanitizer is the real gate anyway. |
| Hand-written sanitizer misses an attack vector (attributes, `<script>`, comments, malformed tags) | Whitelist output is rebuilt from scratch (tags emitted without attributes, text HTML-encoded), not filtered in place; unit tests with `<script>`, `onerror=`, `<a href=javascript:>`, comments, unclosed tags, nested lists. The output is never rendered as raw HTML in the client. |
| WhatsApp markers break when a mark has leading/trailing spaces (`* bold *`) or is empty | Formatter trims whitespace into the outside of the markers and drops empty marks; unit tests. |
| A placeholder split by formatting (`<strong>{Da</strong>te}`) silently stops being a placeholder | Placeholders are inserted as whole text tokens; validator runs on the converted text so a broken token is just text. Accepted, documented in a test. |
| Legacy rows (plain text) change when re-rendered | "No tag" -> passthrough; renderer test asserts the two seeded bodies render exactly as before. |
| Real values longer than examples overflow the 2000 snapshot | Validator counts placeholders at their column max length (decision above). |
| Migration auto-applies on deploy | Widening only (`AlterColumn` 1000 -> 4000); T4 asserts the migration contains nothing else; `has-pending-model-changes` clean. |
| Unrelated uncommitted changes in the working tree end up in the commit/PR | Stage by explicit path only; never `git add -A`/`.`; check `git diff --cached --stat` before commit (T16 lists the unrelated files). |

## Verification checkpoints

- **Checkpoint 0 (after T1):** `npm run build` and `npm test -- --watch=false` green with Tiptap installed; decision (wrapper or direct) written in the todo under T1.
- **Checkpoint 1 (after T2-T4):** `dotnet build` green; `dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~FruitDessert"` green; `has-pending-model-changes` reports none; migration reviewed by eye.
- **Checkpoint 2 (after T5-T7):** `dotnet build`; unit tests; `./run-functional-tests.sh --filter "FullyQualifiedName~FruitDessert"` green (includes send-after-edit).
- **Checkpoint 3 (after T8-T12):** `cd src/Client && npm run build && npm test -- --watch=false` green.
- **Checkpoint 4 (T13):** everything above plus all unit/functional test projects; optional manual smoke via `aspire start --apphost src/AppHost/AppHost.csproj` (open modal, edit, send, check wa.me text).
- **Checkpoint 5 (T14-T16):** haiku review findings resolved or reported; `graphify update .`; PR opened/updated.

## Out of scope (YAGNI)

Template versions/history, per-class templates, preview pane with real values, ordered-list toolbar button (ordered lists are only accepted by the sanitizer), Playwright spec, caching.
