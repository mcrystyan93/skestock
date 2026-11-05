# Spec: Tab „Fructe și desert" (planificare pe zile)

## Objective
În pagina de prezentare a clasei (`features/school-classes/overview`, `SchoolClass` = „școala" din cerință) apare un tab nou „Fructe și desert". Conține un tabel cu o linie pentru fiecare zi Luni–Vineri între `StartDate` și `EndDate`. Personalul alege congregația care aduce fructe și desert, bifează „Email trimis" și trimite pe WhatsApp mesajele de aducere aminte și de mulțumire către persoana de contact. UI-ul este în română.

Utilizator: personalul care administrează clasa. Succes: fiecare zi are congregație, email bifat și mesaje trimise, cu istoric păstrat.

## Decizii confirmate
| # | Decizie |
|---|---|
| 1 | WhatsApp prin link `wa.me` cu text precompletat. Utilizatorul apasă „Trimite" în WhatsApp. Statusul din istoric este „Deschis", fără confirmare de livrare. Trimiterea este totuși în spatele `IMessageSender`, ca Cloud API să poată înlocui implementarea. |
| 2 | Contactul vine din `Congregation.DessertContactPerson` și `DessertContactPhone`, cu legătură live (nu snapshot). |
| 3 | Rândurile se creează la prima listare, idempotent, pentru zilele lipsă din interval. Generarea zilelor este în spatele `IWorkingDayProvider`. |
| 4 | Prefix `ske` (`tr[ske-fruit-dessert-row]`). Fără `OnPush` explicit, conform `CLAUDE.md`. În Angular 22 este oricum implicit. |
| 5 | Template-uri în baza de date. Acum includem doar model, seed și randare, fără ecran de editare. |
| 6 | Placeholdere: `{ContactPerson}`, `{Date}`, `{Congregation}`, `{ClassName}`. `{Date}` este formatat ro-RO. |
| 7 | Clasă `Closed` ⇒ comenzile sunt respinse (`IRequiresOpenClass`). Retrimiterea este permisă. „Trimis" în UI înseamnă cel puțin o intrare în istoric. |
| 8 | Dropdown: toate congregațiile nesterse (`DeletedDate == null`). |
| 9 | Telefon normalizat în backend la format internațional (RO: `07xx…` ⇒ `407xx…`). Dacă e invalid sau lipsește, butoanele sunt dezactivate cu tooltip. „Email trimis" este doar un checkbox manual, fără trimitere reală de email. |

## Tech stack
Cel din `CLAUDE.md`: .NET 10, minimal APIs, Mediator (source-generated), FluentValidation, FluentResults, EF Core 10 + SQL Server, Angular 22 (Signal Forms, `@ngrx/signals`), NG-ZORRO 22, Tailwind 4, NUnit + Shouldly + Moq, Vitest. Fără dependențe noi.

## Model de date
Nume finale se stabilesc în plan. Convenții: Guid Id, `BaseAuditableEntity`, `DateTimeOffset` UTC, enum-uri ca int doar cu adăugare la coadă.

**`FruitDessertPlanEntry`** — o zi planificată pe clasă
- `Id`, `SchoolClassId` (FK ⇒ `SchoolClass`, cascade), `Date` (`DateOnly`), `CongregationId` (`Guid?`, FK ⇒ `Congregation`, restrict), `EmailSent` (bool, default false).
- Unic `(SchoolClassId, Date)`. Garantează idempotența generării și blochează dublurile la rulări concurente. Index pe `CongregationId` pentru FK.
- De ce rând propriu și nu coloane pe `Congregation`: planul aparține perechii clasă + zi, iar o congregație poate apărea în mai multe zile.

**`FruitDessertMessage`** — istoric de trimiteri (append-only)
- `Id`, `EntryId` (FK ⇒ entry, cascade), `Type` (`Reminder = 1`, `Thanks = 2`), `Status` (`Opened = 1`; `Sent/Delivered/Failed` rezervate pentru Cloud API, nu se implementează acum), `SentAt`, `RecipientPhone` (snapshot normalizat), `Body` (text randat, snapshot), `TemplateId` (FK ⇒ `MessageTemplate`, restrict).
- Index `(EntryId, Type)`. De ce istoric în loc de boolean: păstrează ce text a plecat, spre ce număr și când, chiar dacă template-ul sau contactul se schimbă; „Trimis" se deduce din existența unui rând.

**`MessageTemplate`**
- `Id`, `Type` (același enum), `Name` (max 100), `Body` (max 1000), `IsActive` (bool).
- Index unic filtrat pe `(Type)` unde `IsActive = 1`: un singur template activ per tip. Template nou ⇒ rând nou, fără modificări de cod. Seed prin migrare: un rând pentru fiecare tip, cu text provizoriu.

Migrare doar prin `dotnet ef migrations add AddFruitDessertPlanning`. Aditivă, deci backward-safe. Seed-ul de template-uri intră în migrare (`InsertData`), nu în cod de startup.

## Abstractizări backend (SOLID)
- `IWorkingDayProvider` — `IEnumerable<DateOnly> GetWorkingDays(DateOnly start, DateOnly end)`. Implementare: Luni–Vineri. Sărbătorile pot fi adăugate ulterior fără modificarea consumatorilor.
- `IMessageTemplateRenderer` — înlocuiește placeholderele cunoscute. Placeholder necunoscut ⇒ eroare de validare la randare, nu text corupt.
- `IPhoneNumberNormalizer` — returnează număr internațional sau eșec.
- `IMessageSender` — `Result<MessageDispatch>` cu URL-ul de deschis și statusul. Implementare `WhatsAppLinkSender` (wa.me). Cloud API ar fi o a doua implementare.
- Înregistrare în `DependencyInjection.cs` al stratului proprietar, nu în `Program.cs`.

## Endpointuri (grup nou `FruitDessertPlans` în `src/Web/Endpoints`)
| Metodă | Rută | Efect |
|---|---|---|
| GET | `/school-classes/{classId}/fruit-dessert-plan` | Creează zilele lipsă, apoi returnează lista (dată, congregație, contact, telefon, `emailSent`, mesaje trimise per tip) |
| PUT | `…/entries/{entryId}/congregation` | Setează sau șterge congregația |
| PUT | `…/entries/{entryId}/email-sent` | Setează checkbox-ul |
| POST | `…/entries/{entryId}/messages` (body: `type`) | Randează template-ul activ, înregistrează istoricul, returnează URL-ul wa.me |

Comenzile implementează `IRequiresOpenClass` și `ICacheInvalidation`. GET-ul scrie rânduri lipsă, deci nu este `ICacheableQuery` (de decis în plan: query cu efect lateral vs. comandă dedicată de sincronizare apelată din GET). Erori tipizate în `Application/Common/Errors` (ex. `FruitDessertErrors`): contact lipsă, telefon invalid, template activ lipsă, clasă închisă, congregație inexistentă.

## Frontend
- Tab nou în `school-class-overview.page.html`, containere în `features/school-classes/overview/tabs/fruit-dessert/`.
- `FruitDessertStore` (`@ngrx/signals`, `withLoadingFeature`, `withProblemDetailsFeature` ca în `SchoolClassOverviewStore`). HTTP în `*.http.ts`, lângă `school-classes.http.ts`.
- Container: gestionează store-ul. Prezentare: tabel `<table>` nativ. Rând: `tr[ske-fruit-dessert-row]` cu `input()` pentru date și `output()` pentru evenimente (`congregationChange`, `emailSentChange`, `sendMessage`).
- Dropdown și checkbox cu NG-ZORRO (`nz-select`, `nz-checkbox`), verificate în `.github/instructions/llms-full.txt`. Control de rând cu Signal Forms.
- După POST de trimitere, clientul deschide URL-ul wa.me (`noopener`), iar rândul afișează icon + „Trimis".
- Data: „Luni, 10 Noiembrie 2026" (locale ro-RO, cu inițiale mari conform exemplului). Stări: loading, empty (clasă fără interval), error. Tabelul rămâne utilizabil la lățime mică (scroll orizontal în container).
- Clasă `Closed` ⇒ controalele dezactivate (folosește `CLASS_READ_ONLY`).

## Commands
```
dotnet build
dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~FruitDessert"
./run-functional-tests.sh --filter "FullyQualifiedName~FruitDessert"
dotnet ef migrations add AddFruitDessertPlanning --project src/Infrastructure --startup-project src/Web --context ApplicationDbContext --output-dir Data/Migrations
dotnet ef migrations has-pending-model-changes --project src/Infrastructure --startup-project src/Web --context ApplicationDbContext
cd src/Client && npm run build && npm test -- --watch=false
```

## Project structure
```
src/Domain/Entities/FruitDessert/           entități + enum-uri
src/Application/Features/FruitDessertPlans/ {Queries,Commands}/<UseCase>, abstracțiuni, CacheConstants
src/Infrastructure/                         configurări EF, WhatsAppLinkSender, WorkingDayProvider, normalizer
src/Web/Endpoints/FruitDessertPlans.cs
tests/Application.UnitTests/Features/FruitDessertPlans/
tests/Application.FunctionalTests/Features/FruitDessertPlans/
src/Client/src/app/features/school-classes/overview/tabs/fruit-dessert/
```

## Code style
Ca în repo: file-scoped namespaces, `Guard.Against.*`, `CancellationToken` peste tot, `Result<T>` pentru eșecuri de business. TypeScript strict, fără `any`, `inject()`, `input()/output()`, control flow nativ. Nu rulăm Prettier pe client. Comentarii doar pentru „de ce".

## Testing strategy (TDD, teste scrise primele)
- Unit (NUnit/Shouldly/Moq): `WorkingDayProvider` (săptămâni, capete de interval, interval inversat), renderer (toate placeholderele, necunoscut), normalizer (formate RO, invalid), handlere (succes, clasă închisă, congregație inexistentă, contact lipsă), validatori.
- Funcționale (Respawn): listare creează rândurile și a doua listare nu dublează; extinderea intervalului adaugă doar zilele noi; unicitate `(SchoolClassId, Date)`; mesaj ⇒ rând de istoric cu snapshot; retrimitere ⇒ al doilea rând; clasă închisă respinge; un singur template activ per tip.
- Client (Vitest, colocat): store (load, schimbare congregație, bifare, trimitere), componenta rând (date, stare „Trimis", butoane dezactivate fără telefon valid).
- Playwright: opțional, doar dacă planul îl justifică.

## Boundaries
- **Always:** teste înainte de implementare; `dotnet build` și teste relevante; migrare doar prin EF; UI în română; `graphify update .` după modificări; branch nou + PR, nu commit pe `master`.
- **Ask first:** orice dependență nouă; trecerea la WhatsApp Cloud API; ecran de editare a template-urilor; sărbători legale; trimitere reală de email.
- **Never:** parole sau token-uri în fișiere; editarea migrărilor deja aplicate; editarea manuală a snapshot-ului EF.

## Success criteria
1. Tab-ul listează exact zilele Luni–Vineri dintre `StartDate` și `EndDate`, formatate „Luni, 10 Noiembrie 2026".
2. A doua listare nu creează duplicate; schimbarea intervalului adaugă doar zilele lipsă.
3. Alegerea congregației completează contactul și telefonul din `DessertContact*` și se salvează.
4. „Email trimis" persistă după reîncărcare.
5. Fiecare buton WhatsApp înregistrează un rând de istoric (tip, dată, status, telefon, text randat) și deschide un link wa.me cu numărul normalizat și textul randat; apoi rândul arată icon + „Trimis".
6. Un template nou activat se folosește fără modificări de cod.
7. Pe clasă închisă nicio modificare nu trece, nici în UI, nici în API.
8. `dotnet build`, testele unit/funcționale și `npm run build` + `npm test` trec.

## Riscuri și puncte deschise
1. **Popup blocker:** `window.open` după un apel async poate fi blocat. Plan: deschidere sincronă pe click și completare ulterioară a URL-ului, sau rezolvarea în plan.
2. **„Trimis" = „Deschis":** nu putem ști dacă utilizatorul a apăsat efectiv „Trimite" în WhatsApp.
3. **GET cu efect lateral:** concurența la prima listare e acoperită de indexul unic, dar tratarea conflictului (retry/ignore) se decide în plan.
4. **Template-uri în DB fără UI:** schimbarea textului cere SQL până la un ecran dedicat (scope separat).
5. **Congregație ștearsă ulterior:** FK restrict + soft delete ⇒ rândul își păstrează referința; UI trebuie să afișeze numele chiar dacă e marcată ștearsă (de confirmat).
6. **Locul fișierului `*.http.ts`:** propus în `shared/school-classes/services`; confirmat la plan.
7. **Nume pagină:** pagina reală este `school-class-overview`, nu `school-overview`.

## Pașii următori (după aprobare)
Plan și task-uri scrise cu Opus în `tasks/fruit-dessert-planning-plan.md` și `tasks/fruit-dessert-planning-todo.md` (nume prefixate, ca să nu suprascriem `plan.md`/`todo.md`). Implementare cu Sonnet, în ordine TDD, pe un branch dedicat, cu review final de la un sub-agent Haiku.
