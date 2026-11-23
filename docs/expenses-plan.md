# Plan: Cheltuieli (Expenses) pe school-classes

Stare: **draft, așteaptă aprobare**. Statisticile pe cheltuieli NU fac parte din acest plan.

## Decizii deja luate
- Cheltuiala aparține unei clase (`SchoolClassId` obligatoriu). Scrierile cer clasă deschisă (`IRequiresOpenClass`).
- Un fișier = o cheltuială (AI-ul extrage un singur document; dacă vede mai multe, ia primul și îl marchează în `ErrorMessage`/avertisment, nu creează N entry-uri).
- Statusuri vizibile: `Processing` (În procesare), `Processed` (Procesat), `Failed` (Eroare).

## Pattern reutilizat (GoodsReceiptImport)
Entitate cu tranziții în domeniu → handler de creare scrie entitatea + `OutboxMessage` într-un singur `SaveChanges` → `OutboxPublisherService` → coadă → `QueueProcessingService<T>` în Worker (idempotență prin `ProcessedMessages`) → handler AI cu `IDocumentExtraction…` → domain events → `IRealtimeNotifier` → store Angular cu `withEventHandlers`.

## Constatări din analiză care schimbă planul
1. `GoodsReceiptImportFailedEventHandler` aruncă `NotImplementedException` (verificat). Pentru Expense scriem handler-ul de `Failed` real; altfel, în tranzacția din Worker, excepția poate anula statusul `Failed` și cheltuiala rămâne `Processing`. Test obligatoriu.
2. Handler-ul Completed al importurilor notifică doar grupul listei. Pentru Expense notificăm grupul clasei (`RealtimeGroups.SchoolClass(id)`), pe care pagina overview îl are deja alăturat.
3. `CreateGoodsReceiptImport` nu verifică `FileMetadata.Status == Completed`. Noul handler o face.
4. Posibil: evenimentele de domeniu rulează înainte de commit când Worker-ul folosește tranzacție explicită (de verificat într-un test, nu presupus).

## Model de date

### `Expense` (Domain/Entities/Expenses)
Moștenește `BaseAuditableEntity`, `IKeysetEntity`.

| Câmp | Tip | Note |
|---|---|---|
| `SchoolClassId` | Guid FK, Restrict | indexat |
| `Source` | enum `ExpenseSource`: Manual=0, Receipt=1, Invoice=2 | doar adăugare la coadă |
| `Status` | enum `ExpenseStatus`: Processing=0, Processed=1, Failed=2 | persistat ca int |
| `ErrorMessage` | string(2000)? | doar la Failed |
| `SupplierName` | string(300)? | agregare pe furnizor; normalizare ulterioară (vezi întrebări) |
| `DocumentNumber` | string(100)? | |
| `ExpenseDate` | DateOnly? | data de pe document, pentru agregări pe perioadă; nullable cât timp e Processing |
| `Currency` | string(3)? | ISO 4217, implicit RON |
| `TotalAmount` | decimal(18,2)? | cu TVA |
| `VatAmount` | decimal(18,2)? | |
| `ExpenseCategoryId` | Guid? FK | vezi întrebări |
| `FileMetadataId` | Guid? FK | null la manuală; fișierul rămâne în `FileMetadata`, ca la goods receipts |
| `ExtractedDataJson` | string? | snapshot brut AI, `private set`, nu se editează niciodată |
| `UploadedByUserId`, `UploadedAt`, `ProcessedAt` | ca la `GoodsReceiptImport` | |
| `Lines` | `ExpenseLine` | opțional, vezi mai jos |

Separare AI vs confirmat: coloanele tipate sunt datele „curente” (completate de AI la procesare, editabile de utilizator). `ExtractedDataJson` păstrează originalul AI, pentru comparație și audit. Fără a doua copie a câmpurilor (YAGNI); istoricul modificărilor vine gratis din tabelele temporale.

### `ExpenseLine` (opțional în v1 — decizie în întrebări)
`ExpenseId`, `Description`, `Quantity?`, `UnitPrice?`, `LineTotal`, `VatRate?`. Fără legătură la `Item`.

### `ExpenseCategory` (dacă se aprobă)
`Id`, `Name` unic. Gestionată de admin (`[Authorize(Roles = Administrator)]`, scrie date globale). Seed mic în `ApplicationDbContextInitialiser`.

### Indexuri (pentru agregări viitoare + listare keyset)
- `(SchoolClassId, ExpenseDate, Id)`
- `(SchoolClassId, Status, CreatedDate, Id)` pentru lista keyset
- `(ExpenseCategoryId, ExpenseDate)`
- `(SupplierName, ExpenseDate)`

Migrație: `AddExpenses` (backward-safe, doar tabele noi; temporale automat).

## Backend (src/Application/Features/Expenses)

Comenzi/interogări (nume după slice-ul cel mai apropiat):
- `CreateExpenseFromDocument` (command, `IRequiresOpenClass`): validează clasa și `FileMetadata` (există, `Completed`); creează `Expense` `Processing` + `OutboxMessage` pe coada nouă; întoarce id.
- `ProcessExpenseDocument` (command din coadă): idempotent (dacă nu mai e `Processing` → Ok); descarcă blob → `IExpenseDocumentExtractionService` → `ApplyExtractionResult`. `UnprocessableDocumentException` → `MarkAsFailed` + Ok; `Transient` → propagă pentru retry.
- `CreateExpense` (manual, `IRequiresOpenClass`): direct `Processed`, `Source=Manual`.
- `UpdateExpense` (`IRequiresOpenClass`, `ICacheInvalidation`): editează câmpurile; funcționează și pe `Failed` (completare manuală → devine `Processed`).
- `RetryExpenseProcessing` (`IRequiresOpenClass`): doar din `Failed` cu fișier; revine la `Processing` + outbox nou.
- `DeleteExpense` (`IRequiresOpenClass`) — necesită confirmare dacă vrem ștergere (vezi întrebări).
- `GetAllExpenses` (query keyset: `ExpenseFilterConfiguration` cu `schoolClassId`, `status`, `source`, `expenseDate`; `ExpenseSortConfiguration`; `ExpenseCursor`), `GetExpense` (detaliu).
- `CacheConstants.cs` pentru Expenses; liste necache-uite ca la importuri.

Domeniu: `Expense.Create…`, `CreateManual`, `ApplyExtractionResult`, `MarkAsFailed`, `Update`, `Retry`; evenimente `ExpenseCreated/Processed/Failed` publicate din `DispatchDomainEventsInterceptor`.

AI: `IExpenseDocumentExtractionService` (Application/Documents/Interfaces), `OpenAiExpenseDocumentExtractionService` + `ExpenseExtractionSchemaFactory` (Infrastructure/AI), reutilizând `OpenAiDocumentExtractionClient`. Rezultat: tip (bon/factură), furnizor, număr, dată, monedă, total, TVA, linii.

Queue: constantă nouă `ExpenseImportQueue` în `skestock.Shared.Services`; `ExpenseQueueProcessingService : QueueProcessingService<…>` în Worker + înregistrare în `Worker/Program.cs`; coadă `…-poison` conform convenției.

SignalR: `RealtimeEvents.ExpenseCreated/Processed/Failed`; event handlers publică în `RealtimeGroups.SchoolClass(classId)` payload `{ expenseId, status }`.

Endpoints: `src/Web/Endpoints/Expenses.cs` (`IEndpointGroup`): `POST /api/Expenses/from-document`, `POST /api/Expenses`, `POST /api/Expenses/get-all`, `GET /api/Expenses/{id}`, `PUT /api/Expenses/{id}`, `POST /api/Expenses/{id}/retry`, `DELETE /api/Expenses/{id}`. `ToProblemHttpResult()` la eșec. Erori noi în `ExpenseErrors`.

## Frontend (src/Client)
- Modele `app/core/models/expense.ts` (+ `index.ts`): statusuri, etichete RO („În procesare”, „Procesat”, „Eroare”), culori (blue/green/red), DTO-uri.
- `app/shared/expenses/` (alias nou `@ske/shared/expenses`): `expenses.http.ts`, `expense-list.store.ts` (keyset ca la goods-receipt-imports; `withEventHandlers` pe evenimentele noi → `load`), `ui/add/add-expense-modal` (refolosește `ske-file-upload` + `FileStorageState`), `ui/table`, `ui/form` (formular Signal Forms), `ui/modals/expense-modal` (detaliu: formular + fișier descărcabil + mesaj eroare + „Reîncearcă”).
- Tab nou „Cheltuieli” la **sfârșitul** listei (index 8): `tabs/expenses/expenses-tab.ts`, intrare în `header.ts` (`tabs` + `case 8` → „Adaugă cheltuială”, null dacă clasa e închisă), modal în `header-container.ts`, `nz-tab` în `school-class-overview.page.html`. Opțiune secundară în modal: „Introdu manual”.
- `realtime-event-names.ts`, `realtime.events.ts`, `eventMap` în `app.config.ts`.
- NG-ZORRO: verificat în `.github/instructions/llms-full.txt` înainte de orice `nz-*` nou.

## Teste
- Backend: validatori + handlere (Create, Process incl. duplicate delivery și `Failed`, Update, Retry, GetAll), filtre/sort/cursor; test pentru handler-ul de `Failed` în tranzacția Worker; functional test pe endpoint-uri; test schemă temporală (`TemporalSchemaTests`).
- Frontend: spec-uri colocate pentru `expenses.http`, store (SignalR → reload), model.
- E2E: o specificație Playwright cu API mockat (tab, upload, status).

## Pași (fiecare: build + teste + rezumat)
1. Domain: entități, enum-uri, evenimente, `Domain.UnitTests` pentru tranziții.
2. Infrastructure: configurații EF, `DbSet`, migrația `AddExpenses` + indexuri; `has-pending-model-changes`.
3. Application: Create/Process/Update/Retry/Delete + validatori + erori + cache + teste.
4. Application: `GetAll`/`Get` (keyset) + teste.
5. AI + queue + Worker + SignalR event handlers (inclusiv `Failed`), teste duplicate delivery.
6. Endpoints + functional tests.
7. Frontend: modele, http, store, SignalR.
8. Frontend: tab, modal upload, listă, detaliu/formular, manual, retry.
9. Spec-uri, e2e, `graphify update .`.

Livrare: branch `feature/expenses`, PR la final (fără commit pe `master`). Review independent (Haiku) pe diff după fiecare pas major.

## Întrebări deschise (răspunde o dată, grupat)
1. **Linii (`ExpenseLine`) în v1?** Recomand NU: antet + total acoperă statisticile pe perioadă/furnizor/clasă; liniile se adaugă când vrem statistici pe produs.
2. **Categorii:** tabel `ExpenseCategory` (admin, seed) sau câmp liber? Recomand tabel mic, `ExpenseCategoryId` nullable, AI-ul nu îl completează în v1.
3. **Furnizor:** string simplu acum (recomandat) sau entitate `Supplier` normalizată de la început?
4. **Ștergere:** hard delete (cu istoric temporal) sau fără ștergere în v1?
5. **Monedă:** doar RON în v1 (coloană păstrată) sau multi-monedă din start?
6. **Cine editează:** orice utilizator care poate deschide clasa, sau doar anumite roluri?

## Riscuri
- Calitatea extracției pe poze slabe: mitigată prin `Failed` + completare manuală + retry.
- Bug existent în `GoodsReceiptImportFailedEventHandler`: în afara scope-ului; îl raportez, nu îl repar fără acord.
- Fișiere mari/PDF multipagină: limită de pagini/dimensiune la fel ca la goods receipts.
