# Plan: Navigare responsive – etapa 1 (Articole)

Spec: `docs/specs/responsive-navigation.md`

## Componente și dependențe

```text
T1 NavigationDrawerState + MenuToggle  (core/layouts, fără UI în layout)
        │
        ▼
T2 Layout full: sider extins ≥ lg; < lg drawer (dacă hasToggle) / sider collapsed (altfel)
        │
        ▼
T3 Header Articole: ☰ lângă titlu, breadcrumb + temă ascunse < lg
        │
        ▼
T4 Verificare finală (teste, build, screenshot-uri 360/768/1280, graphify update)
```

Secvențial: T2 depinde de starea din T1, T3 de butonul din T1 și de drawer-ul din T2.

## Decizii

- `NavigationDrawerState` (`@Service()`): `open`, `toggle()`, `close()`, `register()` /
  `unregister()` → `hasToggle` (computed pe un contor).
- `MenuToggle` (`ske-menu-toggle`): se înregistrează în constructor, se deînregistrează prin
  `DestroyRef`; randează butonul doar sub lg (`*skeLayoutBreakpoint` negat → folosim
  `NzBreakpointService` / clasa `lg:hidden`). Se folosește `lg:hidden` pe host: simplu, fără JS.
  Înregistrarea rămâne activă și la ≥ lg, dar acolo layout-ul oricum arată sider-ul extins.
- Layout: `*skeLayoutBreakpoint="'lg'; else small"` → sider 200px extins. În `small`:
  `@if (hasToggle())` → `nz-drawer`, altfel sider-ul collapsed de acum. Un singur `menuTemplate`
  cu parametru `collapsed`.
- Drawer se închide la: ✕, mască, Esc (ng-zorro), navigare (`nzCloseOnNavigation`), click în
  meniu, trecere la ≥ lg (effect pe breakpoint).

## Riscuri

| Risc | Mitigare |
|---|---|
| `nzInlineCollapsed` rămâne „lipit” la schimbarea de breakpoint | instanțe separate de meniu per ramură (template randat în contexte diferite) |
| Pagina se schimbă și ☰ dispare cu drawer-ul deschis | `unregister()` închide drawer-ul când contorul ajunge la 0 |
| Titlul `nzTitle` string nu permite buton | se folosește `nz-page-header-title` cu conținut |

## Checkpoint-uri

După fiecare task: teste țintite + screenshot-uri Playwright (360 / 768 / 1280px) inspectate vizual.
