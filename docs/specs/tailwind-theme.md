# Spec: Tailwind-token theme for ng-zorro (SaaS look)

## Objective
Give the Angular client a Linear/Vercel-style SaaS look by driving ng-zorro's Less
theme variables from Tailwind's palette, so ng-zorro components and Tailwind
utilities share one visual language (light + dark).

## Tech Stack
Angular 22, ng-zorro-antd (Less themes), Tailwind v4 (`@tailwindcss/postcss`), Less 4.

## Design decisions
- Tailwind v4 colours are CSS `oklch` variables; ng-zorro Less needs compile-time
  literals (`tint/shade/fade`). So: one `themes/tailwind-tokens.less` holds hex copies
  of the Tailwind palette (`@tw-slate-50 … @tw-slate-950`, `@tw-indigo-500/600/700`,
  status colours). It is the only place hex values live.
- Primary = indigo-600; greys = slate; radius 6px; subtle shadows (Tailwind `shadow-sm`/`md`).
- `default.less`, `dark.less`, `compact.less` import tokens BEFORE ng-zorro's theme
  where a variable must be overridden pre-derivation, and map ng-zorro vars:
  `@primary-color`, `@success/warning/error/info-color`, `@body-background`,
  `@component-background`, `@layout-*-background`, `@border-color-base/split`,
  `@text-color*`, `@heading-color`, `@border-radius-*`, `@shadow-*`, `@font-size-base`.
- `tailwind.css` `@theme` overrides `--color-primary-*` (indigo) so `bg-primary-600`
  etc. match; existing utilities unchanged.
- `base.less`: replace hardcoded `4px` radius with `@border-radius-*` tokens.

## Commands
- Build: `cd src/Client && npm run build`
- Test: `cd src/Client && npm test`
- Dev: `dotnet run --project src/AppHost` (client at :7001)

## Project Structure
- `src/Client/src/styles/themes/tailwind-tokens.less` (new)
- `src/Client/src/styles/themes/{default,dark,compact,base}.less` (edited)
- `src/Client/src/styles/tailwind.css` (edited)

## Code Style
```less
@import './tailwind-tokens';
@primary-color: @tw-indigo-600;
@body-background: @tw-slate-50;
@border-radius-base: 6px;
```

## Testing Strategy
No unit tests for styles. Verify: production build succeeds for all three
bundles; manual check of light/dark/compact on login, items table, modals, cards,
charts; Playwright e2e (`src/Client/e2e`) still passes.

## Boundaries
- Always: use tokens, never raw hex outside `tailwind-tokens.less`; keep the
  three bundles building; keep WCAG AA contrast for text/primary buttons.
- Ask first: adding dependencies, changing bundle/theme-switching logic, changing spacing/heights globally.
- Never: hand-edit `node_modules`, use `!important` to fight the theme, touch component templates for colour.

## Success Criteria
1. `npm run build` passes; `default`, `dark`, `compact` bundles emitted.
2. Primary buttons, links, active menu, focus rings render indigo-600 (dark: indigo-500).
3. Light bg slate-50 / cards white / borders slate-200; dark bg slate-950 / cards slate-900 / borders slate-800.
4. Radius 6px on buttons, inputs, cards, modals.
5. No hex literals outside `tailwind-tokens.less`.
6. Text/primary-button contrast ≥ 4.5:1 in both themes.

## Open Questions
- Keep `compact.less` in sync (assumed yes)?
- Rounding: 6px everywhere, or larger (8px) for cards/modals?
