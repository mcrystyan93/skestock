# Taskuri: Wizard comenzi

- [x] T1: `OrderListWizardStore` + funcții pure (dedupe low-stock, mapare SupplyList → linii, sugestii → linii cu qty 1)
  - Acceptance: tranziții pași valide; `canNext` corect; articol în 2 locații apare o dată cu ambele locații
  - Verify: `cd src/Client && npm test -- order-list-wizard`
  - Files: `shared/order-lists/services/order-list-wizard.store.ts` (+spec), `core/models/order-list.ts` (tipuri), `shared/order-lists/index.ts`

- [x] T2: Componente de prezentare `source-step`, `suggestions-step`, `supply-list-step`
  - Acceptance: doar input/output; carduri radio accesibile; checkbox-uri cu tag-uri locație; listă active + previzualizare; stări loading/empty/error
  - Verify: teste componente (emit evenimente) + `npm run build`
  - Files: `shared/order-lists/ui/modals/wizard/steps/*` (+spec)

- [x] T3: `order-list-wizard-container` + integrare în `OrderListDetailModal`
  - Acceptance: comandă nouă → wizard 3 pași; comandă existentă → direct editor (Draft editabil, altfel read-only); salvare creează Draft și reîncarcă tab-ul; confirmare la închidere cu modificări
  - Verify: `npm test`, flux manual în AppHost
  - Files: `wizard/order-list-wizard-container.ts`, `detail/order-list-detail-modal.ts|html`, `services/order-list-detail.store.ts`

- [x] T4: Polish UX/a11y + verificare finală
  - Acceptance: utilizabil la 360px și tastatură; focus/aria-live; `npm test` și `npm run build` verzi; `graphify update .`
  - Verify: Playwright screenshots 360/768/1280 pe cei 3 pași
  - Files: stiluri/template-uri wizard

## Revizia 2 – liste multiple

- [x] R1: store + utils (`supplyListIds`, detalii multiple, îmbinare pe `itemId`, nume/notă) + teste
- [x] R2: `supply-list-step` cu checkbox-uri (Signal Forms) + `suggestions-step` migrat la Signal Forms
- [x] R3: verificare light/dark/mobil în browser
