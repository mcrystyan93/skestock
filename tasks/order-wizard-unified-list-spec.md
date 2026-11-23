# Spec: Listă unificată de produse în wizardul „Comandă nouă"

## Objective

Pasul 2 al wizardului („Produse") și panoul de sugestii din ultimul pas (editorul comenzii) înlocuiesc taburile cu **o singură listă** a tuturor item-urilor active, grupată pe locație, cu căutare fixă la scroll. Selectarea / adăugarea unui item îl pune în comandă cu **ultima cantitate achiziționată**.

Utilizator: personalul care pregătește comenzi. Succes = poate bifa rapid ce îi trebuie, vede dintr-o privire ce e pe stoc redus și ce se comandă des, și nu mai ajustează cantitățile manual în cazul obișnuit.

### Rând de item (pasul 2 și panoul din ultimul pas)
- poză (`ske-item-thumbnail`), nume, `(SKU)`
- icon „stoc redus" (dacă item-ul e pe stoc redus în clasă)
- icon „comandat frecvent" (segment weekly sau everyTwoWeeks)
- ultima cantitate achiziționată + unitate
- pasul 2: checkbox. Ultimul pas: fără checkbox, buton „adaugă" / text „Adăugat" (comportamentul de azi, `order-list-suggestion-groups.ts`)

### Listă
- Tot catalogul activ, grupat pe locație. Un item cu stoc în mai multe locații apare în fiecare grup, dar e un singur id și se bifează împreună.
- Item fără stoc în clasă: locația lui implicită, altfel grupul „Fără locație", la final.
- Pasul 2: „Selectează tot" pe fiecare grup de locație (fără unul global).
- Se șterg endpointurile `GET /api/statistics/purchases/segments` și `GET /api/Stock/class/{classId}/low-stock`.
- Mobil: panoul din ultimul pas apare doar de la `lg`, deci screenshot-urile mobile acoperă pasul 2.
- În grup: ordonare după interval mediu crescător (`averageIntervalDays`); fără interval la final, apoi alfabetic.
- Search fix la scroll (nume / SKU, fără diacritice), ca în `order-list-suggestion-groups.ts`.

### Cantitate
- Bifare în pasul 2 / adăugare în ultimul pas → linie cu `lastQuantity` (întreg ≥ 1 prin `wholeQuantity`).
- Fără istoric → 1 (ca azi).

## Decizii confirmate
1. Ultima cantitate: coloană nouă `LastQuantity` în `ItemPurchaseStatistic` + migrare, calculată în `PurchaseStatisticsCalculator`.
2. Conținut: tot catalogul activ.
3. „Frecvent" = segmentele existente weekly + everyTwoWeeks (fără modificarea clasificatorului).
4. Sortare = interval mediu crescător.

## Ipoteze (corectează-mă acum)
1. „Ultima cantitate" = cantitatea ultimei recepții (Last365Days, toate clasele), nu media.
2. (confirmat) Locația unui item = locațiile din stocul clasei curente (`getClassLocationStock`). Dacă endpointul de stoc nu dă toate item-urile active, adaug un endpoint/query dedicat „catalog de comandă" (vezi Open Questions).
3. „Stoc redus" folosește aceeași regulă ca azi (cantitate < prag per item+locație; item fără prag nu e niciodată redus).
4. Iconițele (confirmat): Font Awesome nu e instalat. Folosim catalogul SVG existent din `src/Client` (`nz-icon nzType="icons:<name>"`), cu nume din setul FA deja în `assets/icons` (ex. `triangle-exclamation` pentru stoc redus, `arrows-rotate` / `repeat` pentru frecvent), fără dependență nouă.
5. Toate taburile dispar, inclusiv „Predefinite" din panoul ultimului pas: rămâne doar lista unică (confirmat).
6. Pasul „Produse" pentru sursa „stoc redus" rămâne omis ca azi.

## Tech Stack
Angular 22 (signals, Signal Forms), NG-ZORRO 22, Tailwind 4, `@ngrx/signals`; .NET 10, EF Core 10, SQL Server, Mediator (nu MediatR).

## Commands
```
cd src/Client && npm run build
cd src/Client && npm test -- --watch=false
./run-e2e.sh -- --project=chromium e2e/specs/order-wizard-segments.spec.ts
dotnet build
dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~PurchaseStatistic"
dotnet ef migrations add AddItemPurchaseStatisticLastQuantity --project src/Infrastructure --startup-project src/Web --context ApplicationDbContext --output-dir Data/Migrations
dotnet ef migrations has-pending-model-changes --project src/Infrastructure --startup-project src/Web --context ApplicationDbContext
```

## Project Structure
- Backend: `src/Domain/Entities/Statistics/ItemPurchaseStatistic.cs`, `src/Application/Features/Statistics/{Commands/MaterializePurchaseStatistics,Queries/GetPurchaseSegments (clasificator)}`, `src/Application/Features/OrderLists/Queries/GetOrderCatalog`, `src/Infrastructure/Data/{Configurations,Migrations}`
- Client: `src/Client/src/app/shared/order-lists/{services,ui/modals/wizard/steps,ui/modals/detail/form/suggestions}`, `src/Client/src/app/core/models/class-statistics.ts`
- Teste: colocate `*.spec.ts`, `tests/Application.UnitTests/Features/Statistics`, `src/Client/e2e/specs`

## Code Style
Componente standalone, `input()`/`output()`, `computed()`, control flow nativ, text UI în română, fără Prettier, comentarii doar pentru „de ce". Un rând comun (poză + checkbox/buton + nume + iconițe + cantitate) reutilizat în ambele locuri, nu două copii.

## Testing Strategy
- Unit backend: `PurchaseStatisticsCalculatorTests` (LastQuantity = cantitatea ultimei recepții, per receipt), `GetOrderCatalog` handler (flaguri, locații, fallback, lastQuantity), `PurchaseSegmentClassifier`.
- Unit client: model de listă (grupare pe locație, sortare, dedupe, „Fără locație", căutare), `suggestionsToLines` (cantitate = lastQuantity, fallback 1), spec-urile pasului 2 și ale panoului actualizate.
- E2E: `order-wizard-segments.spec.ts` rescris pentru lista unică. Screenshot-uri în `screenshot-checks/<spec>/` pe desktop și mobil.
- Final: `/dotnet-data:optimizing-ef-core-queries` pe query-urile atinse (materializare, segmente, query de catalog).

## Boundaries
- Always: migrare backward-safe (coloană nullable / default), `AsNoTracking` pe citiri, build + teste înainte de PR, branch nou + PR.
- Ask first: modificarea clasificatorului.
- Never: editat migrări aplicate, Prettier, dependență nouă (Font Awesome), commit pe master.

## Success Criteria
1. Pasul 2 nu are taburi. O listă pe grupuri de locație, cu search sticky.
2. Fiecare rând arată poză, checkbox, nume, `(SKU)`, iconițele corecte și ultima cantitate.
3. Bifarea produce linii cu `lastQuantity`. Fără istoric = 1.
4. Panoul din ultimul pas arată aceeași listă și rânduri, fără checkbox, cu stare „Adăugat". Adăugarea folosește `lastQuantity`.
5. Un item low-stock în 2 locații apare în ambele grupuri și se bifează împreună.
6. Build, teste unit și e2e trec. Screenshot-urile arată bine pe desktop și mobil.
7. Revizuire EF Core făcută și constatările reale rezolvate.

## Decizii suplimentare (confirmate)
- Endpoint nou „catalog de comandă" aprobat (un singur request).
- Item cu istoric dar fără stoc în clasă → locația lui implicită (`item-default-location`); „Fără locație" doar dacă nu are nici aceea.

## Open Questions
Niciuna.
