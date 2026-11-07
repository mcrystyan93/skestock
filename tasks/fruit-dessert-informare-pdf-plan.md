# Plan: Informare fructe și desert (PDF)

Spec: `tasks/fruit-dessert-informare-pdf-spec.md` (aprobat, opțiunea A). Context: `tasks/fruit-dessert-planning-plan.md`.

## Constatări din cod (schimbă scope-ul față de spec)

1. **Tabelul apare doar de la `xl`** (`fruit-dessert-tab.html`, `*skeLayoutBreakpoint="'xl'"`, ~1200 px). Sub `xl` se folosește `list-small` + `card`. Deci la 375/768/1024 px butonul PDF trebuie pus în **card**; tabelul primește coloana (verificat la 1440 px).
2. **`SendFruitDessertMessageCommandValidator` folosește `IsInEnum()`**. Cu `Informare = 3`, POST `/messages` cu `type=informare` ar trimite pe WhatsApp și ar scrie istoric. Validatorul se restrânge la `Reminder`/`Thanks`.
3. **`templates-modal` iterează șabloanele din DB**, deci noul rând apare singur. `TAB_TITLES` este `Record<FruitDessertMessageType, string>`: fără intrarea `Informare` nu compilează. Ruta `PUT /api/FruitDessertTemplates/{type}` acceptă deja orice valoare de enum.
4. **Renderer-ul (`MessageTemplateRenderer`) nu depinde de tip**, iar `MessageTemplateValues` are deja tot ce trebuie. Rezultatul lui este markup WhatsApp (`*bold*`, `_italic_`).
5. **Fonturile sunt deja încorporate** (`src/Infrastructure/Export/Fonts/LiberationSans-*.ttf`, `EmbeddedResource`), iar `OrderListImageExporter` are `Wrap` + `LoadTypeface`. Sunt obligatorii cele încorporate, nu cele din container.
6. **Testele funcționale folosesc `TestApp.SendAsync` și `HttpClient`** (`MessageTemplateTests`). Respawn ignoră `MessageTemplates` (`DatabaseResetter.cs`), deci rândul seedat rămâne între teste.
7. **Teste existente care se schimbă:** `FruitDessertPlanQueryTests.MessageTemplates_AreSeededWithOneActivePerType`, `MessageTemplateTests` (lista de tipuri, ~linia 70).

## Componente și dependențe

| Strat | Componentă | Depinde de |
|---|---|---|
| Domain | `FruitDessertMessageType.Informare = 3` (append) | - |
| Infrastructure | seed `HasData` (Id `0197c0de-0000-7000-8000-000000000003`, `SeedInstant`) + migrare `AddInformareTemplate` | enum |
| Application | `Abstractions/IInformarePdfRenderer` (`byte[] Render(string title, string body)`) | - |
| Infrastructure | `Export/InformarePdfRenderer` (SkiaSharp `SKDocument.CreatePdf`, A4 595×842 pt, margine 56 pt, titlu bold 16 pt, corp 12 pt); refolosește `OrderListImageExporter.Wrap/Regular/Bold` devenite `internal static`; DI singleton | abstracție |
| Application | `Queries/GetInformarePdf/` query + validator + handler → `Result<FileExportResult>` | renderer-e, seed |
| Web | `FruitDessertPlans.cs`: `MapGet(GetInformarePdf, "{classId:guid}/entries/{entryId:guid}/informare")` → `result.ToFile()` | query |
| Client | enum `Informare = 'informare'`, `TAB_TITLES`, `shared/ui/browser-download.ts` (extras din `order-list-export.service.ts`), `FruitDessertPlansHttp.downloadInformare`, store `downloading` + `downloadInformare` | endpoint |
| Client | `FruitDessertEntry.downloadInformare = output<void>()`, input `downloading`; coloană în row/table; buton în card; legătura list-small/table/tab → store | store |

### Decizii

- **Handler**: entry după `(Id, SchoolClassId)` → `EntryNotFound`. Fără `CongregationId` → `ContactMissing` (409). Congregația se citește cu `IgnoreQueryFilters()`, ca în Send (una ștearsă este permisă). Apoi șablonul activ `Informare` → `ActiveTemplateMissing`, `renderer.Render(...)` (eroarea se propagă) și `pdfRenderer.Render(template.Name, body)`. `ContactPerson` lipsă → `string.Empty`. Nume fișier: `Informare-fructe-si-desert-{Date:yyyy-MM-dd}.pdf` (InvariantCulture), `application/pdf`.
- **Read-only**: nu este `IRequiresOpenClass` (merge și pe clasă `Closed`), nu este `ICacheableQuery` (o editare de șablon nu trebuie să lase PDF vechi), nu apelează `SaveChangesAsync`.
- **Rută fără `.pdf`**: o extensie în path poate interfera cu fallback-ul SPA sau cu static files. Numele fișierului vine din `Content-Disposition`.
- **Paginare**: `RenderedMaxLength = 2000` ține textul obișnuit pe o pagină, dar multe paragrafe goale pot depăși pagina. Renderer-ul începe o pagină nouă când `y` depășește marginea de jos, în loc să trunchieze.
- **Seed (text provizoriu)**: Name `Informare fructe și desert`, Body `<p>Bună ziua, {ContactPerson}!</p><p>Vă informăm că {Date} congregația {Congregation} aduce fructe și desert pentru {ClassName}.</p><p>Vă mulțumim!</p>`.
- **Buton**: dezactivat dacă `!entry().congregationId` sau `downloading()`. Nu depinde de `readOnly`. Text „PDF" + `icons:file-pdf`, `aria-label="Descarcă informarea PDF pentru {date}"`. Coloană `pdf: { width: '90px' }`, antet „Informare".
- **Eroare la descărcare**: `store.handleActionError(error)` (eroarea este Blob, deci mesajul poate fi generic; acceptat).

## Ordine de implementare

1. Backend enum + seed + migrare + validator Send (T1–T2).
2. Renderer PDF (T3–T4), **în paralel** cu 1.
3. Query + handler (T5–T6), după 1 și 2.
4. Endpoint + teste funcționale (T7–T8).
5. Client (T9–T13), **în paralel** cu 3–4 după ce contractul este fixat (ruta de mai sus, enum `'informare'`). T9 depinde doar de enum.
6. Verificare responsive (T14), apoi verificare completă (T15).

## Riscuri

| Risc | Mitigare |
|---|---|
| Diacritice (ș ț ă î â) lipsă în PDF | Fonturi LiberationSans încorporate; test `SKFont.ContainsGlyphs("ăâîșțĂÂÎȘȚ")`. |
| Încadrarea textului în SkiaSharp | Se refolosește `Wrap` existent (cuvinte + rupere pe caractere); paginare la depășire. |
| Migrare: rând nou + valoare enum | `InsertData` cu Id fix, `Down` = `DeleteData`. Rollback de cod vechi cu rândul Type=3 prezent strică modalul, deci rollback-ul se face cu `Down`. |
| Send acceptă `Informare` | Validator restrâns + test (T1). |
| Modalul nu afișează noul tip | `TAB_TITLES` + test în `templates-modal.spec.ts`. |
| Markup WhatsApp în PDF (`*text*`) | `shortcut:` comentariu în renderer; seed fără formatare. |
| DB funcțional de test | `MessageTemplates` exclus din Respawn; seed-ul vine din migrare, iar testele nu modifică șablonul Informare. |
| Eroare blob neinterpretată | Mesaj generic; acceptat (identic cu exportul de comenzi). |

## Checkpoint-uri

- După T2: `dotnet build`, unit `FruitDessert`, `has-pending-model-changes` fără modificări.
- După T4: `dotnet test tests/Infrastructure.IntegrationTests --filter "FullyQualifiedName~InformarePdf"`.
- După T8: `./run-functional-tests.sh --filter "FullyQualifiedName~FruitDessert"`. Deschide manual un PDF generat (diacritice, încadrare).
- După T13: `npm run build` + `npm test`.

## Verificare responsive

Tab „Fructe și desert" pe o clasă cu zile, una cu congregație și una fără. Lățimi 375, 768, 1024 (card) și 1440 px (tabel):
- butonul „PDF" este vizibil, activ cu congregație, dezactivat fără;
- cardul nu are overflow orizontal; butonul este pe rând propriu, 40 px înălțime;
- la 1440 tabelul are scroll orizontal în container, rândul nu se rupe, celelalte coloane își păstrează lățimea;
- click descarcă `Informare-fructe-si-desert-<data>.pdf`.
Nu se adaugă breakpoint-uri CSS.
