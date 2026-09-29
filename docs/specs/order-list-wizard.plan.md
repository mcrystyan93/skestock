# Plan: Wizard comenzi

Spec: `docs/specs/order-list-wizard.md`

## Dependențe

```text
T1 wizard store (stare + dedupe + mapare linii)
   ├─► T2 pași de prezentare (source / suggestions / supply-list)
   │        │
   │        ▼
   └─► T3 wizard container + integrare în OrderListDetailModal (nou → wizard, existent → editor)
                │
                ▼
             T4 UX/a11y/responsive + verificare (teste, build, screenshots 360/768/1280, graphify update)
```

## Decizii

- `OrderListWizardStore` (SignalStore, provided în container): `step` (0..2), `source`, `selectedItemIds`,
  `supplyListId`, computed `canNext`, `suggestions` (low-stock deduplicat pe `itemId`, cu `locations[]`),
  `initialLines` (mapate în `OrderListLineInput`).
- Datele vin din `StockHttp` (low-stock) și `SupplyListsHttp` (get-all activ + get-by-id); nu se schimbă backend.
- Modalul existent rămâne container: `id` prezent → editor direct; `id` absent → wizard; pasul 3 = formularul existent
  cu linii preîncărcate (`orderList` input inițializat din `initialLines`).
- `nz-steps` orizontal ≥ md, vertical/small sub md; footer cu Înapoi / Următorul / Salvează.

## Riscuri

- Preîncărcarea liniilor în formularul existent (Signal Forms): verificăm cum primește `orderList` → mitigare: T3 începe cu un spike/test.
- Modal deja încărcat: păstrăm confirmarea la modificări nesalvate (`dirty`).

## Checkpoints

După T1 (teste store), după T3 (flux complet manual), după T4 (build + teste verzi).
