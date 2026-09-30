# Todo: Navigare responsive – etapa 1 (Articole)

- [x] T1: `NavigationDrawerState` + `MenuToggle`
  - Acceptance: toggle/close/register/unregister funcționează; butonul are `aria-label="Deschide meniul"`,
    `aria-expanded` legat de stare, icon `icons:bars`, ascuns la ≥ lg; exportate din `@ske/layouts`.
  - Verify: `npx ng test --include 'src/app/core/layouts/**/*.spec.ts'`; screenshot 360/1280 (fără schimbări vizibile încă).
  - Files: `core/layouts/full/navigation-drawer.state.ts` (+spec), `core/layouts/full/menu-toggle/menu-toggle.ts` (+spec), `core/layouts/index.ts`

- [x] T2: Layout `full` – sider extins ≥ lg, drawer / sider collapsed < lg
  - Acceptance: ≥ lg sider 200px cu etichete pe toate paginile; < lg fără ☰ pe pagină → sider collapsed;
    drawer stânga 260px „Ske”, meniu, temă jos; se închide la ✕/mască/Esc/navigare/≥ lg.
  - Verify: teste layout; build; screenshot-uri 360/768/1280 pe /items și /categories.
  - Files: `core/layouts/full/full.{ts,html}`

- [x] T3: Header Articole – ☰ lângă titlu, breadcrumb + temă ascunse < lg
  - Acceptance: < lg ☰ în stânga titlului, deschide drawer-ul; breadcrumb și switch-ul absolut ascunse;
    ≥ lg neschimbat.
  - Verify: `npx ng test --include 'src/app/features/items/**/*.spec.ts'`; screenshot-uri 360/768/1280 (închis, deschis, temă dark din drawer).
  - Files: `features/items/list/header/header.{ts,html}`

- [x] T4: Verificare finală
  - Acceptance: toate criteriile de succes din spec; fără scroll orizontal la 360px.
  - Verify: `npm test` (fără eșecuri noi), `npm run build`, screenshot-uri finale, `graphify update .`.
  - Files: `tasks/todo.md`
