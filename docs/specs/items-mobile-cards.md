# Spec: Articole – carduri pe ecran mic

## Obiectiv

Pe ecrane mici (sub breakpoint-ul `md`, < 768px), tab-ul **„Articole”** din pagina Articole
afișează acum un `nz-list` (`ItemListSmall`) care e dens și greu de atins cu degetul. Recomandarea
de design este `nz-card`. Înlocuim lista cu carduri compacte și adaptăm filtrele și tab-urile
din header ca să încapă pe ecran mic.

Utilizator: personal administrativ al școlii care caută și editează articole de pe telefon.

### Povești

- Ca utilizator pe telefon, văd fiecare articol într-un card clar (nume, SKU, categorie,
  status) și îl deschid pentru editare atingând cardul.
- Ca utilizator pe telefon, am căutarea pe toată lățimea și un buton „Filtre” care îmi arată
  câte filtre sunt active; categoria și resetarea sunt într-un drawer de jos.
- Ca utilizator pe telefon, văd toate cele 3 tab-uri fără scroll orizontal, iar butonul de
  acțiune (Adaugă/Importă) nu ocupă spațiul titlului.

## Scop

**În scop**

1. `ItemListSmall` (tab „Articole”, `< md`): `nz-list` → carduri `nz-card`.
2. `FilterForm` (tab „Articole”, `< md`): căutare full-width + buton „Filtre” cu badge +
   `nz-drawer` (`nzPlacement="bottom"`) cu categorie + „Resetează”.
3. `Header` pagina Articole (`< md`): etichete scurte de tab („Articole”, „Liste”,
   „Importuri”) și butoanele de acțiune doar cu icon (cu `aria-label` + tooltip).

**În afara scopului**: tab-urile „Liste” și „Importuri articole” (conținut/filtre), tabelul
desktop, backend, store-uri, alte pagini (Categorii, Clase) – pot urma același model ulterior.

## Design

### Card articol (`< md`)

```text
┌───────────────────────────────────────┐
│ Caiet dictando A5            [Activ]  │  nume (bold, truncat) + tag status
│ SKU-0012 · Papetărie                  │  secundar, truncat; SKU omis dacă lipsește
└───────────────────────────────────────┘
```

- `nz-card` `nzSize="small"` `nzHoverable`, spațiere verticală 8px între carduri, padding
  lateral 16px (aliniat cu filtrele).
- Tot cardul e interactiv: `role="button"`, `tabindex="0"`, `Enter`/`Space`/click → `onEdit`,
  `aria-label="Modifică articolul {nume}"`, focus vizibil.
- Tag: `Activ` (`success`) / `Inactiv` (`default`); cardurile inactive au textul principal
  secundar (fără opacitate pe tot cardul, pentru contrast AA).
- Categorie lipsă → `—`. Text lung → `truncate` + `title` cu valoarea completă.
- Păstrăm virtual scroll-ul CDK (înălțime fixă a rândului) și `BaseList.onScroll` pentru
  încărcare incrementală.
- Stări: încărcare inițială (loading și listă goală) → 4 carduri `nz-skeleton`;
  listă goală → `nz-empty`; încărcare pagină următoare → `nz-spin` mic sub ultimul card.

### Filtre (`< md`)

```text
┌───────────────────────────────┐ ┌──────────┐
│ 🔍 Căutați...                 │ │ Filtre ①│
└───────────────────────────────┘ └──────────┘
```

- Un singur `FilterForm` (fără a duplica instanța – are efectul de încărcare inițială);
  layout-ul se schimbă cu `*skeLayoutBreakpoint="'md'"`.
- Butonul „Filtre” (icon `icons:filter` dacă există în catalog, altfel `sliders`) cu
  `nz-badge` = număr filtre active (în afara căutării; azi: categorie → 0/1).
- `nz-drawer` jos, titlu „Filtre”, conține `ske-category-dropdown` (lățime completă); footer:
  „Resetează” (default) + „Gata” (primary, închide drawer-ul). Filtrele se aplică
  imediat la schimbare, ca pe desktop.
- `≥ md`: layout-ul actual neschimbat.

### Header (`< md`)

- Tab-uri: „Articole”, „Liste”, „Importuri” (desktop rămâne „Importuri articole”).
- Buton acțiune: doar icon, `nzShape="circle"`, `aria-label` + `nz-tooltip` cu textul complet
  („Adaugă articol”, „Adaugă listă”, „Importă”).
- Verificăm manual că theme switcher-ul (poziționat absolut) nu se suprapune cu butonul.

## Tech stack

Angular 22 (standalone, signals, control flow nativ), ng-zorro-antd (`card`, `drawer`,
`badge`, `skeleton`, `empty`, `spin`, `tooltip`), Tailwind, CDK scrolling, Vitest.
Fără dependențe noi.

## Commands

```bash
cd src/Client
npm test -- --include src/app/features/items/**/*.spec.ts
npm run build
npm run dev   # sau dotnet run --project src/AppHost pentru verificare manuală
```

## Project structure

```text
src/Client/src/app/features/items/list/
  tables/regular/small/item-list-small.{ts,html,spec.ts}   carduri
  filter/regular/filter-form.{ts,html}(+ spec nou)         filtre mobile + drawer
  header/header.{ts,html}                                  tab-uri/acțiuni mobile
```

## Code style

Reguli din `.github/instructions/angular-guidelines.instructions.md` și
`ng-zorro-guidelines.instructions.md` (aplicate pe `src/Client`): fără `standalone: true`,
`input()/output()`, `host` în loc de `@HostListener`, import doar componentele ng-zorro folosite,
fără `ngClass`/`ngStyle`, texte UI în română.

```html
<nz-card nzSize="small" nzHoverable role="button" tabindex="0"
         [attr.aria-label]="'Modifică articolul ' + item.name"
         (click)="onEdit.emit(item)"
         (keydown.enter)="onEdit.emit(item)"
         (keydown.space)="$event.preventDefault(); onEdit.emit(item)">
```

## Testing strategy

Vitest (`npm test`), lângă componente:

- `item-list-small.spec.ts`: randează un card per articol cu nume/SKU/categorie/status;
  click și `Enter` emit `onEdit`; listă goală → empty; `loading` + goală → skeleton;
  test existent de `onLoadMore` rămâne verde.
- `filter-form.spec.ts` (nou): `activeFilterCount` = 0 fără categorie, 1 cu categorie;
  `clear()` resetează și emite.
- Header: verificare manuală (depinde de breakpoint real).
- Manual: DevTools 360×740 și 390×844 – fără scroll orizontal, taburi vizibile, drawer
  funcțional, navigare cu tastatura.

## Boundaries

- **Always**: păstrează comportamentul desktop (`≥ md`) identic; rulează testele items și
  `npm run build`; WCAG AA (focus, aria, contrast).
- **Ask first**: extinderea la „Liste”/„Importuri”, adăugare de acțiuni în card
  (ex. Activează/Dezactivează), dependențe noi, schimbări de store/API.
- **Never**: a doua instanță de `FilterForm`, eliminarea virtual scroll-ului/încărcării
  incrementale, modificări backend.

## Success criteria

1. Sub 768px tab-ul „Articole” afișează carduri `nz-card`; niciun `nz-list` în `ItemListSmall`.
2. Card: nume, SKU (dacă există), categorie, tag Activ/Inactiv; tap/`Enter`/`Space` deschide
   modalul de editare.
3. Scroll până jos încarcă pagina următoare; stări skeleton/empty/loading-more afișate.
4. La 360px: căutarea + „Filtre” pe un rând; badge-ul arată 1 când e selectată o categorie;
   drawer-ul permite alegerea categoriei și resetarea.
5. La 360px: cele 3 tab-uri încap fără săgeți de overflow; butonul de acțiune e icon-only cu
   `aria-label`; nimic nu se suprapune în header.
6. `≥ 768px`: UI neschimbat.
7. `npm test` (items) și `npm run build` trec fără erori/avertismente.

## Open questions

- Niciuna blocantă. Iconul pentru „Filtre” se alege din catalogul existent de iconuri.
