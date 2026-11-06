# Spec: Editor de șabloane mesaje (Fructe și desert)

## Objective

Permite editarea șabloanelor de mesaj (Reminder, Mulțumire) direct din UI, cu editor rich text și placeholdere alese dintr-un dropdown (valorile vin de pe backend). Azi șabloanele există doar ca rânduri în DB (seed în migrare) și se schimbă doar cu SQL.

Utilizator: personalul care gestionează planificarea fructe și desert pe o clasă.

Flux: tab „Fructe și desert” → buton „Șabloane mesaje” în header → modal cu cele 2 șabloane → editezi, inserezi placeholdere, salvezi. La următoarea trimitere (wa.me) mesajul folosește textul nou.

## Decizii luate (de la utilizator)

- Editor WYSIWYG (nu textarea). Șabloanele sunt globale, nu legate de clasă.
- Un singur șablon activ per tip; modalul editează pe loc, fără listă de versiuni.
- Edit permis oricui are acces la clasă (fără roluri noi).
- Limita Body se mărește printr-o migrare.

## Assumptions (corectează-mă dacă greșesc)

1. Librărie: **Tiptap** (`@tiptap/core`, `starter-kit`, `extension-placeholder` nu e necesar). Motiv: cea mai populară opțiune headless actuală, ProseMirror, ușor de limitat la un subset de formatare. Cu `ngx-tiptap` sau integrare directă prin `Editor` într-o componentă. De confirmat la plan cu verificare de compatibilitate Angular 22 (dacă `ngx-tiptap` nu suportă v22, folosim `Editor` direct, fără wrapper).
2. WhatsApp nu interpretează HTML. Deci editorul e limitat la ce are echivalent WhatsApp: **bold, italic, strike, cod inline, paragrafe/linii noi, liste cu buline**. Fără titluri, linkuri formatate, culori, imagini.
3. **Stocare: HTML (sanitizat) în `Body`**. Conversia HTML → text WhatsApp (`*bold*`, `_italic_`, `~strike~`, `` `cod` ``, `- ` pentru liste) se face pe server în `MessageTemplateRenderer` la trimitere. Motiv: conversia inversă (WhatsApp → editor) e lossy.
4. Rândurile existente sunt text simplu fără taguri. Randarea tratează „fără taguri HTML” ca text simplu (liniile noi rămân linii noi). Editorul le încarcă împachetate în `<p>`. Fără backfill de date.
5. Placeholderele rămân `{ContactPerson}`, `{Date}`, `{Congregation}`, `{ClassName}` (regex existent `\{[A-Za-z]+\}`), inserate ca text în editor. Lista (nume, etichetă RO, exemplu) vine din backend dintr-o sursă unică folosită și de renderer, ca dropdown-ul să nu poată diverge.
6. Limita: `MessageTemplate.Body` crește de la 1000 la **4000** (HTML ocupă mai mult). Validarea reală se face pe **lungimea textului WhatsApp rezultat ≤ 2000**, limita existentă a `FruitDessertMessage.Body` (snapshot-ul trimis). Nu schimbăm coloana snapshot.
7. Placeholder necunoscut la salvare = eroare de validare (nu doar la trimitere), ca să nu se salveze șabloane care pică la trimitere cu 409.

## Tech Stack

- Backend: .NET 10, Mediator (source-generated), FluentValidation, FluentResults, EF Core 10 + SQL Server. Sanitizare HTML: de ales la plan (vezi Open Questions).
- Frontend: Angular 22 (signals, Signal Forms), NG-ZORRO 22 (modal, dropdown/menu, button, tooltip), `@ngrx/signals`, Tailwind 4, Tiptap (dependență nouă, aprobată de utilizator prin cerința de editor rich text).
- Teste: NUnit + Shouldly + Moq (backend), Vitest (client), Playwright (opțional).

## Commands

```bash
dotnet build
dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~FruitDessert"
./run-functional-tests.sh --filter "FullyQualifiedName~MessageTemplate"
dotnet ef migrations add WidenMessageTemplateBody --project src/Infrastructure --startup-project src/Web --context ApplicationDbContext --output-dir Data/Migrations
dotnet ef migrations has-pending-model-changes --project src/Infrastructure --startup-project src/Web --context ApplicationDbContext
cd src/Client && npm run build
cd src/Client && npm test -- --watch=false
```

## Project Structure

```
src/Application/Features/FruitDessertTemplates/        slice nou (Get, Update, placeholders)
  Queries/GetMessageTemplates/
  Commands/UpdateMessageTemplate/
src/Infrastructure/FruitDessert/MessageTemplateRenderer.cs   conversie HTML→WhatsApp + sursa placeholderelor
src/Infrastructure/Data/Configurations/MessageTemplateConfiguration.cs   MaxLength 4000
src/Web/Endpoints/FruitDessertTemplates.cs             GET, PUT /{type}
src/Client/src/app/features/school-classes/overview/tabs/fruit-dessert/templates-modal/   modal + editor
src/Client/src/app/features/school-classes/services/   *.store.ts + *.http.ts pentru șabloane
src/Client/src/app/core/models/fruit-dessert.ts        DTO șablon + placeholder
tests/Application.UnitTests/Features/FruitDessertTemplates/
tests/Application.FunctionalTests/...
```

Buton nou în `features/school-classes/overview/header/header.ts` (cazul 7 din `action`).

## API (propus)

- `GET /api/FruitDessertTemplates` → `{ templates: [{ type, name, body, lastModified }], placeholders: [{ token, label, example }] }`
- `PUT /api/FruitDessertTemplates/{type}` body `{ name, body }` → 204 / ProblemDetails (validare, placeholder necunoscut, prea lung).

Update-ul modifică rândul activ al tipului (nu creează rând nou), deci indexul unic filtrat rămâne neatins. Setează `LastModifiedBy` prin auditare existentă.

## Code Style

Urmează slice-urile existente (`ClassConfiguration` DepartmentTemplate ca model) și modalul `department-modal.ts` + `form.ts` (Signal Forms, `NZ_MODAL_DATA`, `Events` din `@ngrx/signals`). UI în română, componente NG-ZORRO verificate în `.github/instructions/llms-full.txt`.

## Testing Strategy

- Unit: validator (lungime pe text WhatsApp, placeholder necunoscut), handler Update, conversie HTML→WhatsApp (bold/italic/strike/cod/liste/linii noi/text simplu legacy/HTML malițios), randare cu placeholdere.
- Functional: GET + PUT, PUT cu placeholder necunoscut → 400/409, trimitere după editare folosește textul nou.
- Client: spec pe store și pe modal (încărcare, salvare, inserare placeholder, contor/eroare lungime).

## Boundaries

- Always: validare server la fiecare PUT; sanitizare HTML; migrare cu `has-pending-model-changes`; `dotnet build` + teste înainte de PR; ramură + PR.
- Ask first: dependențe noi (Tiptap, sanitizer HTML), schimbarea coloanei `FruitDessertMessage.Body`, orice formatare dincolo de subsetul WhatsApp.
- Never: HTML nesanitizat redat în DOM sau în mesaj; editarea migrărilor aplicate; refolosirea literalilor de nume din `skestock.Shared.Services`; Prettier pe fișiere client.

## Success Criteria

1. Header-ul tab-ului Fructe și desert are butonul „Șabloane mesaje”; deschide modalul cu Reminder și Mulțumire.
2. Pot formata (B/I/S/cod/listă), insera un placeholder din dropdown, salva; la reîncărcare textul e păstrat.
3. Dropdown-ul listează exact placeholderele pe care le acceptă renderer-ul (aceeași sursă în cod).
4. Mesajul trimis după editare conține textul nou, cu formatarea convertită în markup WhatsApp și placeholderele înlocuite.
5. Șabloanele vechi (text simplu) se deschid și se trimit neschimbate.
6. Placeholder necunoscut sau mesaj randat > 2000 caractere este respins la salvare cu mesaj clar în română.
7. Un singur șablon activ per tip rămâne garantat (indexul unic nu e încălcat).
8. `dotnet build`, testele backend și `npm run build` + `npm test` trec.

## Open Questions

1. Ce sanitizer HTML pe server? Fără dependență nouă am putea parsa doar un subset de taguri permise (whitelist, regex/`System.Xml`) fiindcă subsetul e mic. Recomand whitelist manual în loc de pachet nou. OK?
2. Limita 4000 pentru HTML e suficientă? Alternativă: `nvarchar(max)`.
3. Butonul din header: text „Șabloane mesaje” cu iconiță, sau doar iconiță cu tooltip pe ecrane înguste?
4. Ordinea: Tiptap necesită verificare de compatibilitate cu Angular 22 în Task 1 (spike scurt) înainte de restul?

## Plan/Tasks

După aprobarea spec-ului, plan și task-uri se scriu cu Opus în `tasks/fruit-dessert-template-editor-plan.md` și `tasks/fruit-dessert-template-editor-todo.md`; implementarea o fac subagenți Sonnet, ținând cont de regula de review (haiku) din CLAUDE.md.
