# Plan: SupplyList – frontend + realtime

Spec: `docs/specs/supply-lists-client.md` (aprobat).

## Componente și dependențe

```text
T1 Backend realtime (events + handlers + constants)      ─┐
T2 Client models (supply-list.ts + frequency labels)      ├─> T4 Collection feature + list store (+ realtime)
T3 Client realtime constants/events ──────────────────────┘        │
T2 ─> T5 HTTP service ─> T4                                         v
T2,T5 ─> T6 Detail store ─> T7 Detail form (Signal Forms) ─> T8 Detail modal
T4 ─> T9 Filter form ─┐
T4 ─> T10 Table ──────┴─> T11 Tab + header + page wiring (modal, toggle, join/leave)
T11 ─> T12 Verificare finală (build, teste, manual AppHost)
```

## Ordine

1. **Backend realtime (T1)** – mic, izolat; verificat cu `dotnet build` + teste unitare.
2. **Fundația client (T2, T3, T5)** – modele, constante, HTTP. Paralelizabile.
3. **State (T4, T6)** – feature reutilizabil de colecție + store listă (cu event handlers) și
   store de detaliu.
4. **UI modal (T7, T8)** – formular Signal Forms cu linii (`applyEach`, `ItemAutocomplete`), apoi
   modalul care îl găzduiește (create/update/read-only).
5. **UI tab (T9, T10, T11)** – filtru, tabel, integrarea în pagină (tab index 1, header, modal
   `modal-100 modal-lg-75`, confirmare dezactivare, SignalR join/leave).
6. **Verificare (T12).**

## Riscuri

| Risc | Mitigare |
|---|---|
| Reindexarea tab-urilor strică butoanele din header | Actualizare simultană `header.html` + `items.page.html`; verificare manuală. |
| Signal Forms cu array de linii și câmp condiționat (`intervalWeeks`) | Copiem tiparul din `order-list-detail-form.ts` (`applyEach`, `required` cu `when`). |
| Enum-ul `frequency` – răspuns PascalCase, request acceptă orice caz | Modelul TS folosește PascalCase; trimitem identic. |
| Evenimente duplicate la Disable/Enable idempotente | Eveniment doar la schimbarea efectivă a stării; test unitar. |
| Tabel virtual în tab ascuns (dimensiuni 0) | Același container `absolute inset-0` ca tab-urile existente. |

## Checkpoints

- După T1: `dotnet build` + `dotnet test tests/Application.UnitTests --filter FullyQualifiedName~SupplyLists`.
- După T6: `npm test` (store-uri).
- După T8: `npm run build`.
- După T11/T12: `npm test`, `npm run build`, verificare manuală în AppHost (2 browsere pentru realtime).
