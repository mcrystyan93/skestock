# Spec: Panou de sugestii în editorul comenzii

## Objective
În modalul de comandă (`order-list-detail-modal`), pe ecrane mari, ultima secțiune se împarte vertical: stânga (1/3) un panou de sugestii cu 3 tab-uri, dreapta (2/3) lista de produse curentă. Formularul (nume/notă) și autocomplete-ul rămân fixe deasupra. Utilizatorul adaugă rapid produse din sugestii fără wizard.

- Tab **Stoc redus**: produsele cu stoc scăzut (aceleași ca la crearea unei liste de la 0; `StockHttp.getLowStockItems`).
- Tab **Produse după frecvență**: produsele din segmentele de achiziție (`ClassStatisticsHttp.getPurchaseSegments`: săptămânal, la 2 săptămâni, lunar, o dată pe clasă), afișate ca listă grupată pe frecvență (nu sub-tab-uri).
- Tab **Liste predefinite**: listele predefinite active (`SupplyListsHttp.getAllActive` + `getById` pentru linii), listă grupată după numele listei; click pe un rând adaugă linia cu cantitatea și unitatea din lista predefinită (merge pe `itemId` ca `supplyListsToLines`).
- Click pe un rând adaugă o linie (cantitate = `averageQuantity` din segment, altfel 1; unitate din item/`buc`). Produsul deja în listă apare marcat/dezactivat (dedup pe `itemId`, ca la autocomplete).

## Decizii (confirmate)
- Prefill-ul wizard-ului (stoc redus pre-adăugat) **se păstrează**; panoul arată aceleași produse, cele deja adăugate marcate.
- Adăugare: click/buton pe rând.
- Se aplică oriunde comanda e editabilă (comandă nouă + Draft); ascuns când `editable()` e false.
- Sub `lg`: panoul ascuns, layout-ul actual neschimbat.

## Tech Stack
Angular 22 signals, NG-ZORRO 22 (`nz-tabs`, `nz-list`/`nz-empty`/`nz-skeleton`; API verificat în `.github/instructions/llms-full.txt`), Tailwind 4, Vitest.

## Commands
- `cd src/Client && npm run build`
- `cd src/Client && npm test -- --watch=false`
- `graphify update .`

## Project Structure
- `src/Client/src/app/shared/order-lists/ui/modals/detail/form/order-list-detail-form.{ts,html}` – layout 2 coloane, `addSuggestedItem`.
- nou: `.../detail/form/suggestions/order-list-suggestions-panel.ts` (+ `.spec.ts`) – panou prezentațional (inputs: `lowStock`, `segments`, `addedItemIds`, `loading`; output: `add`).
- Date: reutilizăm `groupLowStockItems` din `order-list-wizard.utils.ts`; încărcare cu `rxResource` în form (ca în `order-list-lines-container.ts`) pe `classId`, doar când `editable()` și `lg`.
- Reuse conversie sugestie→linie din `suggestionsToLines` unde se potrivește.

## Code Style
Standalone implicit, `inject()`, `input()/output()`, `computed()`, control flow nativ, fără `ngClass`/`ngStyle`, texte în română, fără Prettier.

## Testing Strategy
Spec Vitest colocat pentru panou (grupare pe frecvență, marcare adăugat, emite `add`) și extindere `order-list-detail-form.spec.ts` (adăugare din sugestie, dedup). Verificare manuală la 1024/1440/1920 px.

## Boundaries
- Always: dedup pe `itemId`, panou ascuns când needitabil, stări loading/empty/error (eroarea de segmente nu blochează stocul redus).
- Ask first (aprobat de utilizator: spec, `lg` ca prag, branch cu modificările necomise purtate pe ea): endpoint nou backend, dependențe noi, schimbarea wizard-ului.
- Never: modificări backend/migrații, Prettier, editarea modificărilor existente necomise din worktree (4 fișiere) în afara necesarului.

## Success Criteria
1. ≥ `lg`: 1/3 sugestii + 2/3 linii sub form+autocomplete; < `lg`: ca acum.
2. Click pe un produs îl adaugă o dată; apare marcat; re-click nu duplică.
3. Tab 2 grupează produsele pe frecvență; tab 3 pe numele listei predefinite, cu cantitatea din listă.
4. Build + teste verzi.

## Status
Aprobat de utilizator.

## Open Questions
- (rezolvat: `lg` aprobat) Modalul `modal-100` e full-width (CSS `modal-100` negăsit în `src` — de verificat).
- Etichetă tab 2: „Produse după frecvență".
