# Spec: SupplyList – frontend (tab „Liste” pe pagina Articole)

## Objective

Utilizatorul gestionează listele de aprovizionare (`SupplyList`, backend deja implementat –
vezi `docs/specs/supply-lists.md`) direct din pagina **Articole**, fără pagină nouă.

User stories / acceptance:

1. Pe pagina Articole apare un tab nou **„Liste”**, poziționat imediat după primul tab
   („Articole”). Ordinea devine: Articole · Liste · Importuri articole.
2. Tab-ul conține un filtru cu:
   - text de căutare (debounce 300 ms, trimis ca `searchTerm`);
   - `nz-segmented` cu **Active / Inactive / Toate**, preselectat **Active**
     (`isActive = true` / `false` / fără filtru);
   - buton „Resetează” (revine la text gol + Active).
3. Sub filtru, un tabel paginat (keyset, virtual scroll + „load more” la scroll, sortare pe
   coloane) – același model ca tabelul de Articole (`BaseTableWithFilter`). Doar tabel, pe toate
   dimensiunile de ecran (fără listă compactă pentru mobil).
   Coloane: Nume (sortabil), Frecvență (sortabil, text în română – vezi tabelul de mai jos),
   Nr. articole, Status (activ/inactiv), Ultima modificare (sortabil, time-ago), Acțiuni.
   Etichete frecvență (folosite și în select-ul din modal):

   | Valoare | Afișare |
   |---|---|
   | `Weekly` | Săptămânal |
   | `EveryXWeeks` | La fiecare N săptămâni (ex. „La fiecare 2 săptămâni”; în select: „La fiecare X săptămâni”) |
   | `Monthly` | Lunar |
   | `StartOfSchoolYear` | La începutul anului școlar |
   | `EndOfSchoolYear` | La sfârșitul anului școlar |
   | `MiddleOfSemester` | Mijlocul clasei |
   | `StartOfMonth` | La începutul lunii |
   | `EndOfMonth` | La sfârșitul lunii |
   | `Once` | O singură dată |

4. Acțiuni pe rând: **Deschide** (modal) și **Dezactivează/Activează**. Dezactivarea cere
   confirmare (`nzModalService.confirm`, danger), activarea nu. Rândul afișează loading cât timp
   cererea e în curs; după succes lista se reîncarcă cu filtrul curent.
5. Butonul „Adaugă” din header, când tab-ul „Liste” e selectat, deschide modalul de creare.
6. Modalul (creare/deschidere): `nzWrapClassName: 'modal-100 modal-lg-75'` → 100% pe ecrane
   < `lg`, 75% de la `lg` în sus; centrat, `nzMaskClosable: false`.
   - Câmpuri listă: Nume (obligatoriu, max 200), Frecvență (select, obligatoriu),
     Interval săptămâni (vizibil și obligatoriu 2–52 doar la `EveryXWeeks`; altfel trimis `null`),
     Notă (opțional, max 1000).
   - Linii: adăugare articol prin `ItemDropdown` (`allowCreate=false`, doar articole existente; backend-ul respinge articolele inactive nou adăugate); la selectare
     cantitatea = 1 și unitatea = `item.unit`; câmpuri editabile Cantitate (> 0), Unitate
     (obligatoriu, max 50), Observații (max 500); ștergere linie; același articol nu poate fi
     adăugat de două ori.
   - Salvare → `POST /api/SupplyLists` sau `PUT /api/SupplyLists/{id}`; la succes mesaj
     (`NzMessageService`), închide modalul, tabelul se reîncarcă.
   - Listă inactivă deschisă → formular read-only, fără buton Salvează (backend-ul refuză 409).
   - Erorile (ProblemDetails / validare, inclusiv nume duplicat) se afișează în modal cu
     `ske-error-display`.
7. Formularele (filtru + modal) sunt **Signal Forms** (`form`, `FormField`, `applyEach`,
   `required`, `min`, `max`, `maxLength`, `hidden`/`disabled` unde e cazul).
8. State: SignalStore cu **feature reutilizabil** `withSupplyListCollection()` (listare,
   load more, toggle active) + store de detaliu pentru modal. `patchState`, `rxMethod`,
   `mapResponse`, `withLoadingFeature`, `withProblemDetailsFeature`.

9. **SignalR realtime:** când orice utilizator creează / modifică / dezactivează / activează o
   listă, tabelul din tab-ul „Liste” se reîncarcă automat (cu filtrul curent) la toți clienții
   aflați pe pagina Articole.
   - Backend (singura modificare de backend): evenimente de domeniu
     `SupplyListCreatedEvent`, `SupplyListUpdatedEvent`, `SupplyListDisabledEvent`,
     `SupplyListEnabledEvent` (`src/Domain/Events/SupplyLists/`), adăugate în handler-ele
     comenzilor (Disable/Enable doar la schimbare efectivă de stare) și handler-e
     `INotificationHandler` în `Features/SupplyLists/EventHandlers/` care apelează
     `IRealtimeNotifier.NotifyGroupAsync(RealtimeGroups.SupplyListsList, RealtimeEvents.SupplyList*,
     new { SupplyListId })`.
   - Constante noi: `RealtimeGroups.SupplyListsList = "supply-lists-list"`,
     `RealtimeEvents.SupplyListCreated/Updated/Disabled/Enabled`.
   - Client: `realtimeGroups.supplyListsList`, nume în `realtime-event-names.ts`,
     `realtimeEvents.supplyList*` (`type<{ supplyListId: string }>()`), `withEventHandlers` în
     `SupplyListListState` → `reload()`; `ItemsPage` face join/leave pe grup în
     `ngOnInit`/`ngOnDestroy`.

Out of scope: export, duplicare liste, listă compactă pentru mobil, alte modificări de backend
în afara evenimentelor realtime, actualizarea live a unui modal deja deschis.

## Tech Stack

Angular 22 (standalone, signals, Signal Forms), ng-zorro-antd, @ngrx/signals (+ events,
rxjs-interop), @ngrx/operators, RxJS, lodash-es, Vitest. Fără dependențe noi.

## Commands

```bash
cd src/Client
npm ci                      # doar dacă lipsesc dependențele
npm run build               # build dev
npm run build:prod          # build producție
npm test                    # Vitest
dotnet run --project src/AppHost   # verificare manuală full-stack (client pe :7001)
```

## Project Structure

```text
src/Client/src/app/
  core/models/supply-list.ts              DTO-uri, request-uri, frecvențe + etichete, coloane tabel,
                                          buildSupplyListFilter; export din core/models/index.ts
  shared/supply-lists/
    index.ts                              barrel (@ske/shared/supply-lists, alias în tsconfig)
    services/supply-lists.http.ts         getAll, getById, create, update, disable, enable
    services/supply-list-collection.feature.ts   withSupplyListCollection()
    services/supply-list-detail.store.ts  load/create/update pentru modal
    ui/modals/detail/supply-list-detail-modal.{ts,html}
    ui/modals/detail/form/supply-list-detail-form.{ts,html}  Signal Form (antet + linii)
  features/items/
    services/supply-list-list.store.ts    signalStore(withSupplyListCollection(), reload)
    list/tabs/supply-list-tab.ts          filtru + erori + tabel
    list/filter/supply-lists/supply-list-filter-form.{ts,html}
    list/tables/supply-lists/table.{ts,html}
    list/header/header.html               tab nou + „Adaugă” pe index 1
    list/items.page.{ts,html}             provider store, tab, deschidere modal, toggle
```

Realtime:

```text
src/Domain/Events/SupplyLists/SupplyList{Created,Updated,Disabled,Enabled}Event.cs
src/Application/Common/Realtime/{RealtimeGroups,RealtimeEvents}.cs      constante noi
src/Application/Features/SupplyLists/Commands/*/…Handler.cs            AddDomainEvent
src/Application/Features/SupplyLists/EventHandlers/SupplyList*EventHandler.cs
src/Client/src/app/core/signalr/{realtime-groups,realtime-event-names}.ts
src/Client/src/app/core/signalr/services/realtime.events.ts
```

Indexul tab-urilor se schimbă: Articole = 0, Liste = 1, Importuri = 2 (header `@switch` și
`nz-tabs` din pagină se actualizează împreună).

## Code Style

Urmează `order-lists` / `item-import` ca model. Exemplu (feature):

```ts
export function withSupplyListCollection() {
  return signalStoreFeature(
    withState(initialState),
    withLoadingFeature('supplyLists'),
    withProblemDetailsFeature('supplyLists'),
    withComputed((store) => ({
      hasNextPage: () => store.paginationData()?.hasNextPage ?? false
    })),
    withProps(() => ({ supplyListsHttp: inject(SupplyListsHttp) })),
    withMethods((store) => ({ load: rxMethod<GetAllSupplyListsRequest>(/* ... */) }))
  );
}
```

- Fără `standalone: true`, `ngClass`, `@HostBinding`; `inject()`, `input()/output()`,
  `@if/@for/@switch`, `host: {}`.
- Importuri prin alias-uri `@ske/...`; exporturi numite; texte UI în română.
- Modelele TS oglindesc DTO-urile C# (comentariu `/** Mirrors ... */`). `frequency` vine ca
  PascalCase (`'Weekly'`), se trimite la fel (converterul acceptă).

## Testing Strategy

Vitest, colocat (`*.spec.ts`):

- `supply-list-collection.feature.spec.ts`: load setează filtrul și datele, loadMore concatenează,
  toggleActive apelează disable/enable și reîncarcă, erorile ajung în problem details.
- `supply-list-detail.store.spec.ts`: create vs update, eroare păstrată.
- `supply-list-detail-form.spec.ts`: la selectarea unui articol → cantitate 1 + unitatea articolului;
  `intervalWeeks` obligatoriu doar la `EveryXWeeks`; articol duplicat respins; payload corect.
- `supply-list.spec.ts` (model): `formatSupplyListFrequency` pentru toate valorile, inclusiv
  `EveryXWeeks` cu interval.
- `supply-list-list.store.spec.ts`: evenimentele realtime `supplyList*` declanșează `reload`.
- Backend: teste unitare că handler-ele comenzilor adaugă evenimentul corect (și Disable/Enable
  idempotente nu adaugă eveniment) și că event handler-ele apelează `NotifyGroupAsync` cu grupul/
  evenimentul corect.
- `supply-list-filter-form.spec.ts`: primul filtru emis conține `isActive = true`; „Toate” scoate filtrul.
- Verificare manuală în AppHost: tab, filtre, paginare, modal 100%/75%, create/edit/disable/enable.

## Boundaries

- **Always:** Signal Forms, `patchState`, `rxMethod` + `mapResponse`, alias-uri `@ske`, `npm test` și
  `npm run build` înainte de a declara gata; accesibilitate (label-uri, aria pe butoane icon).
- **Ask first:** dependențe noi, modificări backend în afara evenimentelor realtime, schimbarea tab-urilor existente dincolo de
  reindexare, clase CSS globale noi.
- **Never:** NgRx clasic (actions/reducers/effects), `AllowAnyOrigin`, editarea `dist/.angular`,
  ștergerea testelor existente.

## Success Criteria

- Tab-ul „Liste” e al doilea; celelalte tab-uri funcționează ca înainte (Adaugă pe Articole,
  Importă pe Importuri).
- La deschiderea tab-ului se încarcă doar listele active; segmentul și căutarea filtrează corect.
- Scroll-ul la final încarcă pagina următoare; sortarea trimite cheile `name`, `frequency`,
  `lastModifiedDate`.
- Create/edit/disable/enable funcționează end-to-end și tabelul se actualizează.
- Linie nouă: cantitate 1, unitate = unitatea articolului, ambele editabile.
- Modal 100% sub `lg`, 75% de la `lg`.
- O modificare făcută într-un alt browser apare în tabel fără refresh manual (SignalR).
- `npm test` și `npm run build` trec; `dotnet build` și testele unitare backend trec.

## Decizii

- Doar tabel pe toate ecranele (fără listă compactă).
- Dezactivare/activare doar din tabel, cu confirmare la dezactivare.
- Listă inactivă → modal read-only.

- Breakpoint „ecran mic” = `lg` (`modal-100 modal-lg-75`).
- Frecvența se afișează în română, lizibil (tabelul de etichete de mai sus), printr-o funcție
  pură `formatSupplyListFrequency(frequency, intervalWeeks)` în `core/models/supply-list.ts`.
- SignalR inclus (evenimente de domeniu + notificare pe grupul `supply-lists-list`).
