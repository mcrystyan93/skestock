# Todo: Informare fructe și desert (PDF)

Plan: `tasks/fruit-dessert-informare-pdf-plan.md`. Test înainte de implementare.

- [x] T1 Teste roșii pentru tipul `Informare`: seed cu 3 tipuri active, lista din `GET /api/FruitDessertTemplates` include `Informare`, validatorul Send respinge `Informare` [Sonnet]
  - Acceptance: testele compilează după adăugarea enum-ului și pică doar pe comportament.
  - Verify: `dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~SendFruitDessertMessage"` (roșu)
  - Files: `tests/Application.FunctionalTests/Features/FruitDessertPlans/FruitDessertPlanQueryTests.cs`, `tests/Application.FunctionalTests/Features/FruitDessertTemplates/MessageTemplateTests.cs`, `tests/Application.UnitTests/Features/FruitDessertPlans/Commands/SendFruitDessertMessageCommandValidatorTests.cs`

- [x] T2 `Informare = 3` (append), seed `HasData` (Id `0197c0de-0000-7000-8000-000000000003`, text provizoriu din plan), migrare `AddInformareTemplate`, validator Send restrâns la `Reminder`/`Thanks` [Sonnet]
  - Acceptance: T1 verde; migrarea are `InsertData`/`DeleteData`; fără modificări de model în așteptare.
  - Verify: `dotnet build && dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~FruitDessert" && dotnet ef migrations has-pending-model-changes --project src/Infrastructure --startup-project src/Web --context ApplicationDbContext`
  - Files: `src/Domain/Enums/FruitDessertMessageType.cs`, `src/Infrastructure/Data/Configurations/MessageTemplateConfiguration.cs`, `src/Infrastructure/Data/Migrations/*_AddInformareTemplate*.cs` (generat), `src/Application/Features/FruitDessertPlans/Commands/SendFruitDessertMessage/SendFruitDessertMessageCommandValidator.cs`

- [x] T3 Teste roșii pentru `InformarePdfRenderer`: output începe cu `%PDF`; text lung cu multe paragrafe produce PDF valid (fără excepție); fontul conține glifele `ăâîșțĂÂÎȘȚ` [Sonnet]
  - Acceptance: testul pică (tipul nu există).
  - Verify: `dotnet test tests/Infrastructure.IntegrationTests --filter "FullyQualifiedName~InformarePdf"` (roșu)
  - Files: `tests/Infrastructure.IntegrationTests/InformarePdfRendererTests.cs`

- [x] T4 `IInformarePdfRenderer` + `InformarePdfRenderer` (SKDocument A4, titlu + corp, paginare la depășire, comentariu `shortcut:` pentru markup WhatsApp); `Wrap`/`Regular`/`Bold` din `OrderListImageExporter` devin `internal static`; înregistrare DI [Sonnet]
  - Acceptance: T3 verde; testele `OrderListImageExporter` rămân verzi.
  - Verify: `dotnet test tests/Infrastructure.IntegrationTests --filter "FullyQualifiedName~InformarePdf|FullyQualifiedName~OrderListImageExporter"`
  - Files: `src/Application/Features/FruitDessertPlans/Abstractions/IInformarePdfRenderer.cs`, `src/Infrastructure/Export/InformarePdfRenderer.cs`, `src/Infrastructure/Export/OrderListImageExporter.cs`, `src/Infrastructure/DependencyInjection.cs`

- [x] T5 Teste roșii pentru handler și validator `GetInformarePdfQuery`: succes (FileName `Informare-fructe-si-desert-yyyy-MM-dd.pdf`, `application/pdf`, renderer-ele apelate cu valorile zilei), zi inexistentă / altă clasă → `EntryNotFound`, fără congregație → `ContactMissing`, congregație ștearsă → succes, șablon lipsă → `ActiveTemplateMissing`, placeholder necunoscut propagat, niciun `FruitDessertMessage` adăugat; validator: `ClassId`/`EntryId` goale [Sonnet]
  - Acceptance: testele pică doar pentru lipsa implementării.
  - Verify: `dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~GetInformarePdf"` (roșu)
  - Files: `tests/Application.UnitTests/Features/FruitDessertPlans/Queries/GetInformarePdfHandlerTests.cs`, `tests/Application.UnitTests/Features/FruitDessertPlans/Queries/GetInformarePdfQueryValidatorTests.cs`

- [x] T6 Implementare `GetInformarePdfQuery` (fără `IRequiresOpenClass`, fără `ICacheableQuery`) + validator + handler [Sonnet]
  - Acceptance: T5 verde; handlerul nu apelează `SaveChangesAsync`.
  - Verify: `dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~FruitDessert"`
  - Files: `src/Application/Features/FruitDessertPlans/Queries/GetInformarePdf/GetInformarePdfQuery.cs`, `.../GetInformarePdfQueryValidator.cs`, `.../GetInformarePdfQueryHandler.cs`

- [x] T7 Teste funcționale roșii (HttpClient, ca în `MessageTemplateTests`): `GET .../entries/{id}/informare` → 200, `Content-Type: application/pdf`, corpul începe cu `%PDF`, `Content-Disposition` cu numele fișierului; fără congregație → 409 problem details; numărul de `FruitDessertMessages` și `EmailSent` neschimbate; clasă `Closed` → 200 [Sonnet]
  - Acceptance: testele pică cu 404 (ruta lipsă).
  - Verify: `./run-functional-tests.sh --filter "FullyQualifiedName~InformarePdf"` (roșu)
  - Files: `tests/Application.FunctionalTests/Features/FruitDessertPlans/InformarePdfTests.cs`

- [x] T8 Endpoint `GetInformarePdf` în `FruitDessertPlans.cs` → `result.ToFile()`, cu `EndpointSummary`/`EndpointDescription` [Sonnet]
  - Acceptance: T7 verde, inclusiv testele FruitDessert existente.
  - Verify: `./run-functional-tests.sh --filter "FullyQualifiedName~FruitDessert"`
  - Files: `src/Web/Endpoints/FruitDessertPlans.cs`

- [x] T9 Client: `FruitDessertMessageType.Informare = 'informare'`, `TAB_TITLES` „Informare"; test în spec-ul modalului că al treilea tab apare [Sonnet]
  - Acceptance: modalul afișează tabul „Informare" și îl poate salva.
  - Verify: `cd src/Client && npm test -- --watch=false --include="src/app/features/school-classes/**/templates-modal*.spec.ts"`
  - Files: `src/Client/src/app/core/models/fruit-dessert.ts`, `.../tabs/fruit-dessert/templates-modal/templates-modal.ts`, `.../templates-modal/templates-modal.spec.ts`

- [x] T10 Teste roșii pentru store și componente: `downloadInformare(entryId)` apelează HTTP-ul, setează/resetează `downloading`, declanșează descărcarea, iar la eroare setează `actionProblemDetail`; row și card: buton „PDF" prezent, dezactivat fără `congregationId` sau când `downloading`, activ pe `readOnly`, click emite `downloadInformare` [Sonnet]
  - Acceptance: testele pică doar pe comportament.
  - Verify: `cd src/Client && npm test -- --watch=false --include="src/app/features/school-classes/**/*.spec.ts"` (roșu)
  - Files: `src/Client/src/app/features/school-classes/services/fruit-dessert.store.spec.ts`, `.../tabs/fruit-dessert/row/fruit-dessert-row.spec.ts`, `.../tabs/fruit-dessert/card/fruit-dessert-card.spec.ts`

- [x] T11 Extrage `triggerBrowserDownload` + `parseContentDispositionFileName(header, fallback)` în `shared/ui/browser-download.ts` (folosite și de `order-list-export.service.ts`); `FruitDessertPlansHttp.downloadInformare` (blob, observe response); store `downloading` + `downloadInformare` (`exhaustMap`) [Sonnet]
  - Acceptance: testele store din T10 verzi; testele de export comenzi rămân verzi.
  - Verify: `cd src/Client && npm test -- --watch=false --include="src/app/**/{fruit-dessert.store,order-list-export*}.spec.ts"`
  - Files: `src/Client/src/app/shared/ui/browser-download.ts`, `src/Client/src/app/shared/order-lists/services/order-list-export.service.ts`, `src/Client/src/app/features/school-classes/services/fruit-dessert-plans.http.ts`, `src/Client/src/app/features/school-classes/services/fruit-dessert.store.ts`

- [x] T12 Tabel: `FruitDessertEntry` primește `downloading = input(false)` și `downloadInformare = output<void>()`; coloana „Informare" în row (buton `nzSize="small"`, `icons:file-pdf`, „PDF", `aria-label`); `columnLayout.pdf = { width: '90px' }` + `<th>`; output-ul trecut mai departe din tabel [Sonnet]
  - Acceptance: testele row din T10 verzi; build OK.
  - Verify: `cd src/Client && npm test -- --watch=false --include="src/app/features/school-classes/**/fruit-dessert-row.spec.ts"`
  - Files: `.../tabs/fruit-dessert/fruit-dessert-entry.ts`, `.../row/fruit-dessert-row.html`, `.../row/fruit-dessert-row.ts`, `.../table/fruit-dessert-table.html`, `.../table/fruit-dessert-table.ts`

- [x] T13 Card + list-small + tab: buton `nzBlock nzSize="large"` „Descarcă PDF" în card; output-ul trecut din list-small; `fruit-dessert-tab.html` leagă `[downloading]="store.downloading()"` și `(downloadInformare)="store.downloadInformare($event.entryId)"` pentru tabel și listă [Sonnet]
  - Acceptance: testele card din T10 verzi; `npm run build` OK.
  - Verify: `cd src/Client && npm run build && npm test -- --watch=false --include="src/app/features/school-classes/**/*.spec.ts"`
  - Files: `.../card/fruit-dessert-card.html`, `.../card/fruit-dessert-card.ts`, `.../list-small/fruit-dessert-list-small.html`, `.../list-small/fruit-dessert-list-small.ts`, `.../fruit-dessert-tab.html`

- [x] T14 Verificare responsive la 375, 768, 1024 (card) și 1440 px (tabel), conform planului; PDF deschis manual (diacritice, încadrare, nume fișier) [Sonnet]
  - Acceptance: buton vizibil, fără overflow în card, scroll orizontal doar în containerul tabelului, rânduri neschimbate; buton dezactivat fără congregație; capturi la cele 4 lățimi.
  - Verify: Chrome DevTools (device toolbar sau MCP `resize_page`) la 375/768/1024/1440 px pe tabul „Fructe și desert"
  - Files: niciunul (doar verificare)

- [x] T15 Verificare completă [Sonnet]
  - Acceptance: toate comenzile trec; fără modificări de model în așteptare.
  - Verify: `dotnet build && dotnet test tests/Application.UnitTests && ./run-functional-tests.sh --filter "FullyQualifiedName~FruitDessert" && dotnet ef migrations has-pending-model-changes --project src/Infrastructure --startup-project src/Web --context ApplicationDbContext && (cd src/Client && npm run build && npm test -- --watch=false)`
  - Files: niciunul
