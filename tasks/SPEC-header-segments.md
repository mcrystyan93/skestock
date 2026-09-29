# Spec: Segmented control instead of header tabs on small screens

## Objective
In page headers, show `nz-segmented` on small screens (< md, existing `isCompact()`), and keep `nz-tabs` on md and up.
Affected headers: Categories list (2 tabs), Items list (3), School-class overview (5).
Users on phones get a control that fits the width without tab overflow/scroll.

## Tech Stack
Angular 22 (standalone, signals), ng-zorro-antd (`tabs`, `segmented`), Tailwind, Vitest.

## Commands
- Dev: `cd src/Client && npm run dev`
- Build: `cd src/Client && npm run build`
- Test: `cd src/Client && npm test`

## Project Structure
- `src/Client/src/app/shared/ui/header-tabs/` → new `HeaderTabs` component (`ske-header-tabs`) + spec
- `features/categories/list/header/`, `features/items/list/header/`, `features/school-classes/overview/header/` → replace `<nz-tabs>` with `<ske-header-tabs>`; drop unused `NzTabs*` imports

## Design
`HeaderTabs` inputs: `options: { label: string; compactLabel?: string }[]`, `compact: boolean`, plus `model<number>` `selectedIndex`.
Template: `@if (compact()) { <nz-segmented [nzOptions] [(ngModel)]/nzValue ... block> } @else { <nz-tabs [(nzSelectedIndex)]> @for <nz-tab [nzTitle]> }`.
Headers keep their `isCompact()` and pass it in. The school-class tab-tightening classes move into the component's tabs branch (or stay via class passthrough).

## Code Style
Standalone component, no `standalone: true`, `input()`/`model()`, native `@if/@for`, aliases (`@ske/shared/ui`), named exports.

## Testing Strategy
Vitest for `HeaderTabs`: renders segmented when compact, tabs otherwise; selection updates `selectedIndex` in both modes; compact labels are used. Manual check at 375px and 1024px for the three pages.

## Boundaries
- Always: WCAG AA (accessible names, keyboard nav), keep `selectedTabIndex` API of the headers unchanged.
- Ask first: new dependencies, changing breakpoints.
- Never: change page content/tab order, hand-edit generated files.

## Success Criteria
- < 768px: all three headers show a full-width segmented control; ≥ 768px: tabs as today.
- Selecting an option switches page content identically in both modes; the header action button still tracks the selected index.
- 5-option school-class control fits at 375px with compact labels (Stoc, Comenzi, Recepții, Importuri, Stat.).
- `npm run build` and `npm test` pass.

## Open Questions
- Does the 5-option school-class control fit at 320–375px with the compact labels? If not, it may need a smaller font or a scroll fallback.
