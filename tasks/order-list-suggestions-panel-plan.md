# Plan: Panou de sugestii în editorul comenzii

Spec: `tasks/order-list-suggestions-panel-spec.md`. Branch: `feature/order-list-suggestions-panel`.

## Decizii de design
- O singură componentă nouă `shared/order-lists/ui/modals/detail/form/suggestions/order-list-suggestions-panel.ts` (+ `.spec.ts`), template inline, un `<ng-template #groupList>` refolosit în cele 3 tab-uri. Inputs: `classId`, `addedItemIds`; output: `add` (`OrderListLineDto`).
- **Deviație de la spec:** panoul își încarcă singur datele (`rxResource`), ca să nu facă request-uri când nu e randat (sub `lg` / needitabil) fără semnale în plus în form.
  - `lowStock`: `getLowStockItems(classId)` + `groupLowStockItems`.
  - `segments`: resursă separată (eroarea apare doar în tab 2).
  - `supplyLists`: pornește la prima deschidere a tab-ului 3: `getAllActive` + `forkJoin(getById)` (N+1, acceptat; endpoint batch = scope nou, doar cu aprobare).
- Click → linie: tab 1/2 prin `suggestionsToLines` (cantitate = `averageQuantity` sau 1, unitate `buc`); tab 3 prin `supplyListsToLines([list])`. Rândul poartă linia exactă adăugată.
- Form: `classId = input<string | null>`, `addedItemIds` (computed), `withSuggestions = editable() && classId != null`, `addSuggestion` refolosește `addLines` cu guard pe `itemId`. Modalul leagă `[classId]="modalData().classId"`.
- Layout: wrapper `lg:grid lg:grid-cols-3 lg:gap-x-4` doar când `withSuggestions()`; panou `*skeLayoutBreakpoint="'lg'"`; linii `lg:col-span-2`. Tab-urile: bara fixă, conținut scrollabil (`modal-100` are body cu înălțime fixă, confirmat în `styles/themes/base.less`).
- `order-list-lines-container`: `narrow` input; grilă `grid-cols-1 md:grid-cols-2 xxl:grid-cols-3` când e îngust.

## Task-uri
- [x] **T1. Panou, tab 1 + 2**
  - Files: `suggestions/order-list-suggestions-panel.ts`, `.spec.ts`
  - Acceptance: tab „Stoc redus" listă; tab „Produse după frecvență" grupat pe frecvență (grupuri goale ascunse); rând deja adăugat dezactivat „Adăugat"; click emite linia; eroare segmente doar în tab 2; stări loading/empty.
  - Verify: `cd src/Client && npm test -- --watch=false` (fișierul spec)
- [x] **T2. Panou, tab 3 „Liste predefinite"**
  - Files: aceleași 2
  - Acceptance: nicio cerere până la selectarea tab-ului; un grup per listă, cantitatea din listă; același item de 2 ori în listă = un rând cu suma; empty/error.
  - Verify: idem
- [x] **T3. Wiring + layout**
  - Files: `order-list-detail-form.ts/.html/.spec.ts`, `lines/order-list-lines-container.ts`, `detail/order-list-detail-modal.html`
  - Acceptance: panou doar la `lg` + editabil + `classId`; `add` adaugă o dată, al doilea cu același `itemId` nu schimbă nimic; testele existente verzi; păstrăm modificarea necomisă `nzWrapClassName: 'modal-100'`.
  - Verify: `npm test -- --watch=false && npm run build`
- [ ] **T4. Finalizare**
  - Manual la 1024/1440/1920 px și < 992 px (comandă nouă prin wizard, Draft, Submitted); `graphify update .`; review haiku pe diff; PR.

## Riscuri
- Panoul există și cât timp editorul e `[hidden]` (wizard): 2 GET-uri în plus (ca în wizard) și ink-bar `nz-tabs` măsurat la lățime 0 → fallback `[nzAnimated]="false"` sau randare doar la pasul editor.
- Cardurile de linie pot fi înghesuite la 1024 px: variantă `lg:grid-cols-1 xl:grid-cols-2`.
- La tab 3, item deja în listă = dezactivat (nu însumează cantitatea); merge-ul rămâne doar în modalul „Adaugă liste predefinite".
- Fișiere cu modificări necomise în worktree: nu le revert.
