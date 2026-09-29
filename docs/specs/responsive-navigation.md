# Spec: Navigare responsive (sider / drawer)

## Obiectiv

Layout-ul principal (`core/layouts/full`) afișează acum un `nz-sider` mereu **collapsed** (doar
iconuri), pe orice lățime. Pe telefon, sider-ul ocupă spațiu degeaba, iar pe desktop iconurile fără
etichete sunt greu de înțeles. Pe ecran mic, breadcrumb-urile și switch-ul de temă poziționat
absolut aglomerează header-ul paginii.

Utilizator: personal administrativ al școlii, pe desktop și pe telefon/tabletă.

### Povești

- Pe desktop (≥ 992px) văd meniul lateral cu iconuri **și etichete** (sider extins, 200px).
- Pe ecran mic (< 992px) nu apare niciun sider. Un buton „☰” în stânga titlului paginii deschide
  un drawer cu meniul.
- Pe ecran mic, switch-ul de dark mode e în drawer, iar breadcrumb-urile sunt ascunse.

## Presupuneri

1. Breakpoint unic `lg` (992px, `gridResponsiveMap` din ng-zorro = Tailwind `lg`) pentru toate
   schimbările: sider/drawer, butonul ☰, switch-ul de temă și breadcrumb-urile.
2. Pe desktop, switch-ul de temă rămâne unde e acum (dreapta sus în header-ul paginii).
3. Sider-ul de pe desktop nu are buton de collapse (mereu extins).
4. **Etapa 1 (acest spec): doar pagina Articole** primește butonul ☰ și ascunde breadcrumb-ul și
   switch-ul de temă sub lg. Celelalte pagini (Categorii, Clase, Clasă, Analiză clase) se fac
   după validarea vizuală, într-o etapă separată. Login (layout `simple`) rămâne neschimbat.
5. Sider-ul extins pe desktop se aplică tuturor paginilor (e în layout-ul comun).
6. Sub lg, layout-ul folosește drawer-ul doar dacă pagina curentă afișează `ske-menu-toggle`
   (componenta se înregistrează în `NavigationDrawerState` la creare și se deînregistrează la
   distrugere). Altfel, rămâne temporar sider-ul collapsed de acum, ca celelalte pagini să aibă
   în continuare meniu pe mobil.

## Design

### Desktop (≥ 992px)

```text
┌────────────┬──────────────────────────────────────────────┐
│ 🏫 Clase   │ Acasa > Articole                    ☀ ◯ ☾   │
│ ☰ Categorii│ Articole                           [+ Adaugă]│
│ 📦 Articole│ …                                            │
└────────────┴──────────────────────────────────────────────┘
```

- `nz-sider` `nzWidth="200px"`, fără `nzCollapsed`/`nzCollapsible`; meniu `nzMode="inline"`
  fără `nzInlineCollapsed`; se păstrează `nzMatchRouter` pentru elementul activ.

### Mobil / tabletă (< 992px)

```text
┌──────────────────────────────┐   ┌──────────────────┐
│ ☰  Articole              (+) │   │ Ske           ✕  │
│ Articole  Liste  Importuri   │   │ 🏫 Clase          │
│ …                            │   │ ☰ Categorii       │
└──────────────────────────────┘   │ 📦 Articole       │
                                   │                  │
                                   │ ☀ ◯ ☾  (temă)    │
                                   └──────────────────┘
```

- Fără sider; conținutul ocupă toată lățimea.
- `nz-drawer` stânga, 260px, titlu „Ske”, buton de închidere, mască care închide la click,
  `nzCloseOnNavigation`; închidere și la click pe un element din meniu. În partea de jos:
  `ske-theme-switcher`.
- Butonul ☰: `nzType="text"`, icon `icons:bars`, `aria-label="Deschide meniul"`,
  `aria-expanded` legat de starea drawer-ului, afișat în stânga titlului paginii.
- Breadcrumb-urile și switch-ul de temă absolut din header-ele paginilor sunt ascunse.
- La trecerea peste 992px cu drawer-ul deschis, drawer-ul se închide.

### Tranziție (etapa 1)

Sub lg, pe paginile fără ☰ (toate în afară de Articole), layout-ul afișează temporar sider-ul
collapsed de acum (doar iconuri). După ce toate paginile au ☰, ramura aceasta dispare.

### Starea comună

Header-ele paginilor sunt în feature-uri, iar drawer-ul e în layout. Starea (`open`, `toggle`,
`close`) stă într-un serviciu mic cu signal, în `core/layouts`
(`NavigationDrawerState`, `@Service()`), care ține și `toggleCount` (câte butoane ☰ sunt
randate); `hasToggle = computed(() => toggleCount() > 0)`. Butonul ☰ e o componentă reutilizabilă
(`ske-menu-toggle`) care se ascunde singură la ≥ lg; în etapa 1 e folosită doar în header-ul
Articole.

## Tech stack

Angular 22 (standalone, signals, `@Service()`), ng-zorro-antd (`layout`, `menu`, `drawer`,
`button`, `icon`), `NzBreakpointService`, Tailwind, Vitest. Fără dependențe noi.

## Commands

```bash
cd src/Client
npx ng test --include 'src/app/core/layouts/**/*.spec.ts'
npm test
npm run build
```

## Project structure

```text
src/Client/src/app/core/layouts/full/
  full.{ts,html}                          sider (≥ lg) / drawer (< lg) + temă în drawer
  navigation-drawer.state.ts (+ spec)     signal open/toggle/close
  menu-toggle/menu-toggle.ts (+ spec)     butonul ☰ (vizibil doar < lg)
  header/                                 header vechi, nefolosit (comentat) – neatins
src/Client/src/app/features/items/list/header/header.{ts,html}
                                          ☰ lângă titlu, breadcrumb + temă ascunse < lg
```

`MenuToggle` și `NavigationDrawerState` se exportă din `@ske/layouts` (`core/layouts/index.ts`).

## Code style

Reguli din `.github/instructions/angular-guidelines.instructions.md` și
`ng-zorro-guidelines.instructions.md` (pentru `src/Client`): `inject()`, `@Service()`, signals,
`host` în loc de decoratori, import doar componentele ng-zorro folosite, texte în română.

```ts
@Service()
export class NavigationDrawerState {
  private readonly _open = signal(false);
  public readonly open = this._open.asReadonly();
  public toggle() { this._open.update(open => !open); }
  public close() { this._open.set(false); }
}
```

## Testing strategy

Vitest:
- `navigation-drawer.state.spec.ts`: `toggle()` / `close()` / înregistrare (`hasToggle`).
- `menu-toggle.spec.ts`: click → `toggle()`, `aria-expanded` urmează starea.
- Manual, cu Playwright, **cu screenshot după fiecare task** (inspectate vizual), la 360, 768 și
  1280px: sider extins pe desktop, fără sider pe mobil pe Articole, ☰ deschide drawer-ul,
  navigarea îl închide, tema se schimbă din drawer, breadcrumb-urile sunt ascunse, fără scroll
  orizontal; pe Categorii/Clase la 360px rămâne sider-ul collapsed.

## Boundaries

- **Always**: breakpoint `lg` peste tot; aceleași elemente de meniu; accesibilitate
  (`aria-label`, focus, tastatură); rulează teste și build.
- **Ask first**: adăugarea de elemente noi în meniu (ex. „Analiză clase”, comentat acum),
  un buton de collapse pe desktop, schimbări în layout-ul `simple`/Login.
- **Never**: dependențe noi, logică de business în layout, dublarea listei de meniu (un singur
  `menuTemplate` pentru sider și drawer).

## Success criteria

1. ≥ 992px: `nz-sider` de 200px cu iconuri și etichete, niciun buton ☰, breadcrumb și switch de
   temă ca acum.
2. < 992px pe Articole: niciun `nz-sider` randat; ☰ în stânga titlului. Pe celelalte pagini:
   sider-ul collapsed de acum (meniu disponibil în continuare).
3. ☰ deschide un drawer din stânga cu „Ske”, meniul și switch-ul de temă; click pe mască, pe ✕
   sau pe un element din meniu îl închide; `Esc` îl închide.
4. < 992px pe Articole: breadcrumb-ul și switch-ul de temă absolut nu sunt vizibile în header.
5. Tema se poate schimba din drawer și se păstrează (același `ThemeService`).
6. Fără scroll orizontal la 360px; `npm test` (fără eșecuri noi) și `npm run build` trec.

## Open questions

- Niciuna blocantă. Etapa 2 (celelalte 4 pagini) se specifică/planifică după validarea etapei 1.
