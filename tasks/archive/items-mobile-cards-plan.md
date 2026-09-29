# Plan: Articole – carduri pe ecran mic

Spec: `docs/specs/items-mobile-cards.md` (aprobat).

## Componente și dependențe

```text
T1 Carduri ItemListSmall ─┐
T2 Filtre mobile + drawer ├─> T4 Verificare finală (teste, build, manual 360/390px)
T3 Header mobil ──────────┘
```

T1–T3 sunt independente (fișiere diferite) și pot fi făcute în orice ordine; fiecare e o felie
verticală vizibilă. Ordinea aleasă: T1 (cea mai mare valoare), T2, T3.

## Decizii tehnice

- **Carduri**: `nz-card` în `cdk-virtual-scroll-viewport` cu `itemSize` fix (înălțime card + gap);
  gap-ul se face cu `padding-bottom` pe wrapper-ul fiecărui rând, nu `margin`, ca înălțimea să fie
  deterministă pentru virtual scroll. Skeleton/empty/spin randate în afara `cdkVirtualFor`.
- **Filtre**: aceeași instanță `FilterForm`; categoria e randată fie inline (`≥ md`), fie în
  `nz-drawer` (`< md`) prin `*skeLayoutBreakpoint`. `activeFilterCount = computed(...)` din
  valoarea formularului. Drawer-ul folosește `nzVisible` legat de un `signal`.
- **Header**: `*skeLayoutBreakpoint="'md'"` pentru etichete și buton; titlurile tab-urilor vin
  dintr-un `computed` ca să nu dublăm `nz-tabs` (evită resetarea indexului).

## Riscuri

| Risc | Mitigare |
|---|---|
| Virtual scroll cu înălțime greșită → sărituri | `itemSize` = înălțimea măsurată; conținut truncat pe un rând |
| `nz-drawer` + dropdown categorie (overlay în overlay) | verificare manuală; `nzHeight="auto"` |
| Theme switcher absolut suprapus peste buton | verificare manuală la 360px; ajustare spațiere doar în header |
| `LayoutBreakpoint` nu e disponibil în teste (breakpoint real) | testele unitare vizează logica (count, emit), nu layout-ul |

## Checkpoint-uri

- După fiecare task: `npm test` pe fișierele items + `npm run build`.
- Final: verificare manuală DevTools 360×740, 390×844 și desktop ≥ 1024px.
