# Plan: Tailwind-token theme (spec: docs/specs/tailwind-theme.md)
Defaults applied to open questions: compact follows same tokens; 6px radius everywhere.

## Order (sequential; each ends with `npm run build`)
1. Tokens file (foundation, no visual change yet).
2. Light theme (`default.less`) — biggest visual win, verify first.
3. Dark theme (`dark.less`) — note dark.less has ~300 lines, mostly commented; only add mappings.
4. Compact theme + `base.less` radius tokens.
5. `tailwind.css` @theme primary colours.
6. Verify: build, e2e, manual light/dark/compact check, contrast, no stray hex.

## Risks
- Import order: overrides must precede ng-zorro's theme import or derived colours (hover/active) won't recompute. Mitigate: import tokens first, define vars, then import ng-zorro theme (Less lazy-evaluates, so variable definitions after import also win; confirm in build).
- Hardcoded colours in app styles/charts (apexcharts, `styles.less`) — grep after.
