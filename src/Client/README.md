# Ske

This project was generated using [Angular CLI](https://github.com/angular/angular-cli) version 22.1.6.

## Development server

To start a local development server, run:

```bash
ng serve
```

Once the server is running, open your browser and navigate to `http://localhost:4200/`. The application will automatically reload whenever you modify any of the source files.

## Code scaffolding

Angular CLI includes powerful code scaffolding tools. To generate a new component, run:

```bash
ng generate component component-name
```

For a complete list of available schematics (such as `components`, `directives`, or `pipes`), run:

```bash
ng generate --help
```

## Building

To build the project run:

```bash
ng build
```

This will compile your project and store the build artifacts in the `dist/` directory. By default, the production build optimizes your application for performance and speed.

## Running unit tests

To execute unit tests with the [Vitest](https://vitest.dev/) test runner, use the following command:

```bash
ng test
```

## Running end-to-end tests

E2E tests use [Playwright](https://playwright.dev/) (`playwright.config.ts`, specs in `e2e/`). Playwright does not start the app: run `dotnet run --project src/AppHost` from the repo root first.

```bash
npx playwright install --with-deps chromium   # first run only
npm run e2e                                   # all specs, chromium + mobile
npm run e2e -- --project=chromium e2e/specs/login.spec.ts
npm run e2e:ui                                # interactive
npm run e2e:report                            # last HTML report
```

Set `PLAYWRIGHT_BASE_URL` to target another URL. Specs that log in for real (`login`, `room-configuration`, `congregations`) run only when `PLAYWRIGHT_LOGIN_EMAIL` and `PLAYWRIGHT_LOGIN_PASSWORD` are set to an existing administrator.

## Additional Resources

For more information on using the Angular CLI, including detailed command references, visit the [Angular CLI Overview and Command Reference](https://angular.dev/tools/cli) page.
