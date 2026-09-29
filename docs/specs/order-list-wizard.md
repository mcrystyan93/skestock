# Spec: Comenzi – wizard de creare și editor direct la deschidere

## Objective

Creăm o comandă (`OrderList`) printr-un wizard cu 3 pași (`nz-steps`), iar deschiderea unei
comenzi existente afișează direct lista de articole, editabilă. Statusurile rămân neschimbate
(`Draft`, `Submitted`, `Cancelled`; doar `Draft` e editabil).

User stories:

1. **Comandă nouă** (buton „Adaugă” din header / tab Comenzi) → modal cu `nz-steps`:
   1. **Sursă**: alegere între **Comandă nouă** și **Listă predefinită** (2 carduri selectabile).
   2. **Articole sugerate / Listă**:
      - *Comandă nouă*: articolele cu stoc scăzut din clasa curentă (`GET /api/Stock/class/{classId}/low-stock`),
        **deduplicate după `ItemId`** (dacă e sub minim în 2 locații, apare o singură dată; se
        afișează locațiile afectate ca tag-uri). Toate bifate implicit; utilizatorul debifează ce nu vrea.
      - *Listă predefinită* (rev. 2): **listă de checkbox-uri** cu `SupplyList`-urile **active** (`POST /api/SupplyLists/get-all`,
        toate paginile); fiecare rând arată numele, frecvența și **numărul de articole**. Se pot selecta **mai multe liste**;
        sub listă un rezumat „N liste selectate · M articole distincte” (`GET /api/SupplyLists/{id}` pentru cele selectate).
        Liniile se previzualizează în pasul 3.
   3. **Articole comandă**: editorul de linii (același ca la comandă deschisă) preîncărcat cu
      alegerea din pasul 2; utilizatorul poate adăuga/șterge/edita linii, seta nume și notă,
      apoi **Salvează** (creează comanda `Draft`).
2. **Deschidere comandă existentă** → modal fără pași: direct nume/notă + lista de articole.
   `Draft` → editabil (Salvează / Salvează și închide); `Submitted`/`Cancelled` → read-only,
   cu Descarcă Excel unde există deja. Acțiunile de status rămân în tab (neschimbate).
3. Pasul 1 poate fi sărit prin *Înapoi/Următorul*; navigarea între pași păstrează starea.
   Pe modificări nesalvate, închiderea cere confirmare (comportament existent).

## Tech Stack

Angular 22 (standalone, signals, Signal Forms), ng-zorro-antd (`nz-steps`, `nz-card`,
`nz-radio`, `nz-select`, `nz-checkbox`, `nz-empty`, `nz-tag`, `nz-modal`, `nz-segmented` doar dacă e nevoie),
NgRx SignalStore (`patchState`, `rxMethod`, `mapResponse`), Vitest. Backend: .NET 10 – **fără
schimbări planificate** (endpoint-urile necesare există; dedupe-ul se face pe client).

## Commands

```
cd src/Client && npm ci
cd src/Client && npm test
cd src/Client && npm run build
dotnet run --project src/AppHost
```

## Project Structure (container–presentation)

```
src/Client/src/app/shared/order-lists/
  services/
    order-list-detail.store.ts          (existent; extins pentru sugestii deduplicate)
    order-list-wizard.store.ts          (NOU – stare wizard: step, source, selecție, supplyListId)
  ui/modals/
    detail/order-list-detail-modal.*    (CONTAINER: existent; comută între wizard / editor)
    wizard/
      order-list-wizard-container.ts    (CONTAINER: injectează stores, dispatch, salvare)
      steps/
        source-step.ts                  (PREZENTARE: input selected, output selectedChange)
        suggestions-step.ts             (PREZENTARE: input items, selectedIds; outputs)
        supply-list-step.ts             (PREZENTARE: input lists, preview, loading; outputs)
    detail/form/…                       (editorul de linii existent = pasul 3 și modul „deschis”)
```

Reguli: componentele de prezentare folosesc doar `input()`/`output()`, fără stores/HTTP;
containerele injectează store-urile și mapează evenimente. Importuri prin aliasurile `@ske/*`.

## Code Style

```ts
@Component({
  selector: 'ske-order-list-source-step',
  imports: [NzCardComponent, NzRadioGroupComponent],
  template: `...`
})
export class OrderListSourceStep {
  public readonly selected = input<'new' | 'supplyList' | null>(null);
  public readonly selectedChange = output<'new' | 'supplyList'>();
}
```

Fără `standalone: true`, fără `ngClass`/`HostBinding`; `@if/@for`; texte în română; WCAG AA
(carduri = radio group cu label, focus vizibil, `aria-live` pe erori/încărcare); pe mobil pașii
folosesc `nzSize="small"` / `nzDirection` vertical sub `md`.

## Testing Strategy

- Vitest: `order-list-wizard.store.spec.ts` (tranziții de pași, validare „Următorul”),
  spec pentru dedupe articole stoc scăzut (același `itemId` în 2 locații → 1 linie),
  mapare listă predefinită → linii, componente de prezentare (emit evenimente).
- Backend neatins → fără teste noi .NET (dacă apar schimbări, teste în `Application.UnitTests`).
- Verificare manuală în browser (Playwright screenshots 360/768/1280) pentru fiecare pas.

## Boundaries

- Always: păstrăm statusurile și regulile `IsEditable`; validări existente (cantitate > 0, nume max);
  nu dublăm articole deja în listă; erori prin `ske-error-display`.
- Ask first: orice endpoint/handler nou pe backend (ex. deduplicare server-side), adăugarea
  vreunui câmp în `OrderList` (ex. legătură `SupplyListId`), dependențe noi.
- Never: modificăm migrații generate; scoatem teste; hard-codăm stringuri de resurse.

## Success Criteria

1. Comandă nouă deschide wizardul la pasul 1; „Următorul” dezactivat până la alegerea sursei.
2. Pasul 2 (nouă): un articol sub minim în N locații apare **o singură dată**; bifat implicit.
3. Pasul 2 (predefinită): doar `SupplyList` active; previzualizare linii; „Următorul” cere selecție.
4. Pasul 3: linii preîncărcate corect (`itemId`, nume, cantitate, unitate); se pot adăuga/șterge; Salvează creează `Draft` și listează în tab.
5. Comanda existentă se deschide direct pe listă; `Draft` editabil, celelalte read-only.
6. `npm test` și `npm run build` verzi; UI utilizabil la 360px și de la tastatură.

## Decizii (rezolvate)

1. Articolele sugerate (stoc scăzut) se adaugă cu **cantitate 1** (editabilă în pasul 3).
2. Liniile din lista predefinită folosesc `SupplyListLine.Quantity` (+ unitate/note din linie).
3. Sursele sunt **exclusive**; în pasul 3 utilizatorul poate adăuga manual articole.
6. (rev. 2) Mai multe liste predefinite se **îmbină pe `itemId`**: cantitățile se **adună**, notele distincte se concatenează cu „ · ”.
   Numele comenzii = numele listelor unite cu „ + ” (max 200 caractere); nota = nota listei doar dacă e selectată una singură.
7. (rev. 2) Formularele din pașii wizardului folosesc **Signal Forms** (`form()` + `[formField]`), fără `ngModel`/Reactive Forms.
8. (rev. 2) Cardurile selectabile folosesc utilitare Tailwind cu variante `dark:`; după modificări e nevoie de recompilarea dev server-ului.
4. Dedupe după `itemId` pe **client**; fără schimbări de backend.
5. **Nu** reținem `SupplyListId` pe comandă; fără schimbări de schemă.
