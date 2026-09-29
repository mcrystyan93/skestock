# Todo: SupplyList – frontend + realtime

Spec: `docs/specs/supply-lists-client.md` · Plan: `tasks/plan.md`

- [x] T1: Backend realtime pentru SupplyList
  - Acceptance: evenimente `SupplyList{Created,Updated,Disabled,Enabled}Event`; handler-ele
    comenzilor le adaugă (Disable/Enable doar la schimbare de stare); event handler-e notifică
    grupul `supply-lists-list` cu `{ SupplyListId }`; constante noi în `RealtimeGroups`/`RealtimeEvents`.
  - Verify: `dotnet build`; `dotnet test tests/Application.UnitTests --filter FullyQualifiedName~SupplyLists`
    (teste noi pentru evenimente + notificări).
  - Files: `src/Domain/Events/SupplyLists/*`, `src/Application/Common/Realtime/*`,
    `Features/SupplyLists/Commands/*/…Handler.cs`, `Features/SupplyLists/EventHandlers/*`, teste.

- [x] T2: Modele client
  - Acceptance: `SupplyListDto`, `SupplyListLineDto`, `SupplyListListItemDto`, request-uri,
    `SupplyListFrequency`, `SUPPLY_LIST_FREQUENCY_OPTIONS`, `formatSupplyListFrequency`,
    `SUPPLY_LIST_TABLE_COLUMNS`, `buildSupplyListFilter`; export în `core/models/index.ts`.
  - Verify: `supply-list.spec.ts` (etichete RO pentru toate frecvențele) cu `npm test`.
  - Files: `core/models/supply-list.ts`, `core/models/supply-list.spec.ts`, `core/models/index.ts`.

- [x] T3: Constante/evenimente realtime client
  - Acceptance: `realtimeGroups.supplyListsList`, nume evenimente, `realtimeEvents.supplyList*`
    mapate de `SignalRBridge`.
  - Verify: `npm run build`.
  - Files: `core/signalr/realtime-groups.ts`, `realtime-event-names.ts`, `services/realtime.events.ts`.

- [x] T4: Feature colecție + store listă
  - Acceptance: `withSupplyListCollection()` (load, loadMore, toggleActive cu `togglingId`,
    loading/problem details); `SupplyListListState` cu `reload` și `withEventHandlers` pe
    `supplyList*`.
  - Verify: `supply-list-collection.feature.spec.ts`, `supply-list-list.store.spec.ts` cu `npm test`.
  - Files: `shared/supply-lists/services/supply-list-collection.feature.ts` (+spec),
    `features/items/services/supply-list-list.store.ts` (+spec), `shared/supply-lists/index.ts`,
    `tsconfig.json` (alias `@ske/shared/supply-lists`).

- [x] T5: HTTP service
  - Acceptance: `SupplyListsHttp` cu `getAll`, `getById`, `create`, `update`, `disable`, `enable`
    pe `/api/SupplyLists`.
  - Verify: `npm run build`.
  - Files: `shared/supply-lists/services/supply-lists.http.ts`, `shared/supply-lists/index.ts`.

- [x] T6: Store detaliu
  - Acceptance: `SupplyListDetailState` – load by id, save (create/update), loading/erori, `saved`.
  - Verify: `supply-list-detail.store.spec.ts` cu `npm test`.
  - Files: `shared/supply-lists/services/supply-list-detail.store.ts` (+spec).

- [x] T7: Formular detaliu (Signal Forms)
  - Acceptance: nume/frecvență/interval (condiționat 2–52)/notă; linii cu `ItemDropdown` (fără creare),
    cantitate implicită 1, unitate = `item.unit`, observații, ștergere, fără duplicate;
    read-only pentru listă inactivă; emite payload-ul de request.
  - Verify: `supply-list-detail-form.spec.ts` cu `npm test`.
  - Files: `shared/supply-lists/ui/modals/detail/form/supply-list-detail-form.{ts,html,spec.ts}`.

- [x] T8: Modal detaliu
  - Acceptance: `NZ_MODAL_DATA { id | null }`, încarcă lista, afișează erori, footer
    Anulează/Salvează (ascuns la inactivă), mesaj de succes, închidere cu rezultat.
  - Verify: `npm run build`.
  - Files: `shared/supply-lists/ui/modals/detail/supply-list-detail-modal.{ts,html}`, `index.ts`.

- [x] T9: Filtru tab
  - Acceptance: căutare (debounce 300) + segment Active/Inactive/Toate (implicit Active) + Resetează;
    emite filtrul inițial cu `isActive = true`.
  - Verify: `supply-list-filter-form.spec.ts` cu `npm test`.
  - Files: `features/items/list/filter/supply-lists/supply-list-filter-form.{ts,html,spec.ts}`.

- [x] T10: Tabel
  - Acceptance: `BaseTableWithFilter`; coloane Nume, Frecvență (RO), Nr. articole, Status,
    Ultima modificare; sortare `name`/`frequency`/`lastModifiedDate`; acțiuni Deschide + toggle
    cu loading și aria-label.
  - Verify: `npm run build`.
  - Files: `features/items/list/tables/supply-lists/table.{ts,html}`.

- [x] T11: Integrare tab în pagina Articole
  - Acceptance: tab „Liste” pe index 1 (Importuri → 2); „Adaugă” pe index 1 deschide modalul
    create; modal `modal-100 modal-lg-75`; confirmare la dezactivare; join/leave `supplyListsList`;
    reload după închiderea modalului.
  - Verify: `npm test`, `npm run build`.
  - Files: `features/items/list/tabs/supply-list-tab.ts`, `header/header.html`,
    `items.page.{ts,html}`.

- [x] T12: Verificare finală
  - Acceptance: toate criteriile de succes din spec.
  - Verify: `dotnet build`; `npm test`; `npm run build`; manual în `dotnet run --project src/AppHost`
    (filtre, paginare, create/edit/disable/enable, modal 100%/75%, realtime în 2 browsere);
    `graphify update .`.
  - Files: —
