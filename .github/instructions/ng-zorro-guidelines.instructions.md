---
applyTo: 'src/Web/ClientApp/**'
---
# ng-zorro-antd Component Documentation (English)

Use this guidance when generating Angular UI code with `ng-zorro-antd` in this repository.

## Version and Platform Expectations

- Target Angular `^22.0.0` and `ng-zorro-antd` matching Angular major versions.
- Prefer standalone components and application-level providers in `app.config.ts`.
- Use Angular native control flow (`@if`, `@for`, `@switch`) and modern signal-based patterns used by this codebase.

## Core ng-zorro Rules

- Import only required component APIs and icons; avoid broad imports that increase bundle size.
- Prefer immutable state updates for inputs passed to NG-ZORRO components.
- Use global config via `provideNzConfig(...)` for shared defaults instead of repeating per-component settings.
- For overlays inside custom scroll containers, ensure CDK scrolling support (`CdkScrollable` / `ScrollingModule`) so positioning updates correctly.
- For service-based UI (`NzModalService`, `NzDrawerService`, `NzMessageService`, `NzNotificationService`), pass strongly typed options and keep side effects in component/store methods.

## Animations

- For disabling animations globally or locally, use `provideNzNoAnimation()`.
- For component-level template control, use the `nzNoAnimation` directive.
- Use `provideNzWave({ disabled: true })` (or `provideNzNoAnimation`) to disable wave effects when required.

## Date Components (Critical in v22)

- Date components require an explicit `NzDateAdapter` provider at application root.
- Configure one of:
  - `provideNzDateFnsAdapter(...)`
  - `provideNzNativeDateAdapter(...)`
  - `provideNzDateAdapter(CustomAdapter, ...)`
- If locale changes at runtime, update adapter locale via `dateAdapter.setLocale(...)`.
- Ensure date format tokens match the selected adapter/date library.

## Internationalization and Direction

- Configure `provideNzI18n(...)` and Angular locale data registration together.
- Respect runtime RTL/LTR requirements via `dir` or component/service config (`nzDirection`).

## Theming

- Prefer official theming/global config hooks (`provideNzConfig`, `NzConfigService.set`) for dynamic theme updates.
- Keep theme strategy consistent (CSS variables vs Less-based) within a feature.

## Component Usage Standards

- Prefer documented component APIs over custom wrappers when a native NG-ZORRO option exists.
- For forms, pair NG-ZORRO controls with Angular Reactive/Signal Forms and explicit validation states (`nzStatus`, form errors).
- Use accessibility-friendly patterns: semantic labels, keyboard support, visible focus, and appropriate ARIA attributes.

## Recommended Documentation Source

When behavior is unclear, follow NG-ZORRO’s official docs first:

- https://ng.ant.design/llms.txt
- https://ng.ant.design/llms-full.txt
- Single component docs: `https://ng.ant.design/components/<component>.en.md`

Favor current, documented NG-ZORRO APIs and avoid deprecated patterns when equivalent modern APIs exist.

