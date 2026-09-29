# Todo: Articole – carduri pe ecran mic

Spec: `docs/specs/items-mobile-cards.md` · Plan: `tasks/plan.md`

- [x] T1: Carduri `nz-card` în `ItemListSmall`
  - Acceptance: card cu nume + tag Activ/Inactiv, SKU · categorie (`—` dacă lipsește); tot
    cardul e `role="button"`, click/Enter/Space emit `onEdit`; skeleton la încărcare inițială,
    `nz-empty` la listă goală, spin la loading-more; virtual scroll + `onLoadMore` funcționează.
  - Verify: `item-list-small.spec.ts` extins (randare, emit click/Enter, empty, skeleton, load
    more); `npm run build`.
  - Files: `tables/regular/small/item-list-small.{ts,html,spec.ts}`

- [x] T2: Filtre mobile cu drawer
  - Acceptance: `< md` căutare full-width + buton „Filtre” cu badge (nr. filtre active);
    drawer jos cu categorie + „Resetează” + „Gata”; `≥ md` neschimbat; o singură instanță.
  - Verify: `filter-form.spec.ts` nou (`activeFilterCount` 0/1, `clear()` emite);
    `npm run build`; manual 360px.
  - Files: `filter/regular/filter-form.{ts,html,spec.ts}`

- [x] T3: Header mobil
  - Acceptance: `< md` tab-uri „Articole/Liste/Importuri”, butoane acțiune icon-only cu
    `aria-label` + tooltip; fără suprapunere cu theme switcher; `≥ md` neschimbat.
  - Verify: `npm run build`; manual 360px și desktop.
  - Files: `header/header.{ts,html}`

- [x] T4: Verificare finală
  - Acceptance: toate criteriile de succes din spec bifate.
  - Verify: `npm test`, `npm run build`, manual 360×740 / 390×844 / desktop; `graphify update .`.
  - Files: —
