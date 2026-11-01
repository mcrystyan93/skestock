# Spec: Review products of predefined lists in the order wizard

## Objective
When creating an order (comandă) from predefined lists (`supplyList` source), the user wants to **see the products of the chosen lists, with their pictures, and untick the ones they do not want, before typing the order details** (name/note).

Today the wizard is `Sursă → Listă predefinită → Detalii → Articole comandă`. The products only appear in the last step (the editor), after the details. The user cannot cull products early.

Success = after picking lists, a product review is shown before "Detalii"; unticked products never reach the order.

## Findings (from exploration)
- Wizard: `src/Client/src/app/shared/order-lists/` (`services/order-list-wizard.store.ts`, `services/order-list-wizard.utils.ts`, `ui/modals/wizard/order-list-wizard-container.ts`, `steps/*`).
- `selectSupplyLists(ids)` already loads full `SupplyListDto[]` (with lines) into `selectedSupplyLists`. Each line has `itemId`, `itemName`, `itemSku`, `itemImageFileId`, `quantity`, `unit`, `notes`. **No backend change needed.**
- `supplyListsToLines()` merges lines by item across lists (quantities add up). It is what `buildPrefill()` uses.
- Images: reuse `ske-item-thumbnail` (`shared/items/ui/thumbnail`), already used by the editor lines. Checkbox rows: reuse `checkedRowsModel` / `emitCheckedIdsOnChange` (`steps/checked-rows.ts`) and the "Selectează tot" pattern from `suggestions-step.ts`.
- The "new / low-stock" source already has this behaviour (`suggestions-step`, `selectedItemIds`), without images.

## Assumptions (correct me now or I proceed)
1. New **step "Produse" between "Listă predefinită" and "Detalii"** (supply-list source only; the low-stock source keeps its flow).
2. Rows are the **merged distinct items** (same as the final order), not one row per list. Each row shows thumbnail, name (+SKU), summed quantity + unit, and which list(s) it comes from.
3. All products start ticked; unticked ones are excluded from the order lines. "Selectează tot" toggle included. At least one product must stay ticked to continue.
4. Unticking is remembered when going back/forth; **changing the list selection resets it** (all ticked again), via the existing `prefillKey` logic.
5. Frontend only; no API, DB or migration change.
6. Quantities are shown read-only here; editing stays in the final step.

## Design
- Store: new step id `'products'` in `ORDER_LIST_WIZARD_STEPS`; new state `excludedItemIds: string[]` (exclusion list so new lists default to ticked); `setSelectedProducts`-style method; `canGoNext` for `'products'` requires ≥1 included item; `buildPrefill()` filters merged lines by exclusion; reset exclusions when the supply-list selection changes. `prefillKey` includes exclusions so the editor rebuilds only when the choice changes.
- Utils: small helper to derive review rows (`itemId`, name, sku, imageFileId, quantity, unit, source list names) from `selectedSupplyLists`.
- UI: new `steps/products-step.ts` (`ske-order-list-products-step`), NG-ZORRO `nz-checkbox`, `ske-item-thumbnail`, `nz-tag` for list names, `nz-empty` fallback, count summary `X din Y selectate`. Container titles/steps updated (`Sursă, Listă predefinită, Produse, Detalii, Articole comandă`), mobile layout kept.
- Romanian copy only.

## Commands
```
cd src/Client && npm run build
cd src/Client && npm test -- --watch=false
graphify update .
```

## Project Structure
```
src/Client/src/app/shared/order-lists/services/order-list-wizard.{store,utils}.ts (+ .spec.ts)
src/Client/src/app/shared/order-lists/ui/modals/wizard/steps/products-step.ts (new, + steps.spec.ts)
src/Client/src/app/shared/order-lists/ui/modals/wizard/order-list-wizard-container.ts (+ spec)
```

## Code style
Standalone component, `input()`/`output()`, signal forms via `checkedRowsModel`, native control flow, no Prettier, per `AGENTS.md`.

## Testing
- Store spec: step order; exclusions drop lines from `buildPrefill`; list change resets exclusions; cannot advance with 0 included.
- Utils spec: review rows merge across lists, list names collected.
- Steps/container spec: renders thumbnail + name per row; unticking emits; "Selectează tot".
- Manual: run via AppHost, create order from 2 lists with overlapping items, untick, confirm final editor.

## Boundaries
- Always: reuse existing components/helpers, Romanian UI, run build + tests, work on a feature branch + PR.
- Ask first: backend/API changes, new dependencies, changing the low-stock flow.
- Never: add an unticked-state that survives a changed list selection silently; commit to `master`.

## Success Criteria
1. After choosing lists and pressing Next, a "Produse" step shows every distinct product with its picture.
2. Unticked products are absent from the editor lines and the saved order.
3. Going back and changing lists re-ticks everything; going back without changing keeps the choices.
4. Next is disabled when no product is ticked.
5. `npm run build` and `npm test -- --watch=false` pass.

## Open Questions
1. Separate step (assumption 1) vs. showing the product list on the same screen under the list picker (fewer steps, but long on mobile)?
2. Should quantities be editable in this step, or only in the last one?
