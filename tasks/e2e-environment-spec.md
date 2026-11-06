# Spec: Reliable Playwright e2e environment

Status: APPROVED. Analysis by haiku; plan to be written by opus; implementation by sonnet.

## Objective

One command brings up a clean, fully seeded stack in containers, runs Playwright against it, and tears it down, with no manual credential copying. Today devs start AppHost by hand, copy `SeedAdministrator*` from user-secrets into `PLAYWRIGHT_LOGIN_*`, and hope the persistent DB/Redis volumes match.

User: developers (and Claude agents) running e2e locally. Success: the same command gives the same result on a fresh clone and on a machine with old volumes.

### Root causes found (from code analysis)

1. No readiness gate: Playwright has no `webServer`; `WaitFor(web)` in `src/AppHost/Program.cs:329` may wait for Running, not healthy; Web healthcheck exists only in publish mode (`Program.cs:102-114`).
2. Stale persistent state: volumes `skestock-db-data`, `skestock-cache-data`, `storage-data` survive runs. `EnsureAdministratorAsync` never updates an existing admin's password (`ApplicationDbContextInitialiser.cs:147-153`), so a changed secret gives a 401 in live specs instead of a skip.
3. Manual credential hand-off (no script; documented only in `CLAUDE.md:47`, `src/Client/README.md:59`).
4. No demo data: items/classes/receipts seeding is commented out (`ApplicationDbContextInitialiser.cs:137-139`), so most specs mock `**/api/**` and only 3 specs hit the real backend.
5. `fullyParallel` with unlimited local workers and 0 local retries; default 30s test timeout can be exceeded on a first migrate-from-empty start.
6. Fixed port 7001 and reliance on `*.dev.localhost` resolution.

## Assumptions (correct me now)

1. Container runtime is Podman, as in `run-functional-tests.sh`; Docker should also work.
2. The e2e stack is a separate, ephemeral AppHost run mode: no data volumes, fresh DB every run. It never touches the dev volumes or the dev DB.
3. E2E credentials are fixed, non-secret test values generated per run (random password, passed via env to both AppHost and Playwright), so user-secrets are not needed for e2e. Dev credentials in user-secrets stay untouched.
4. Seed data for e2e is limited to a small deterministic dataset (a few categories, locations, items, one open class), applied only in this mode, not in normal Development.
5. Existing mocked specs keep working unchanged; new real-backend specs are added incrementally, not a rewrite.
6. No CI wiring in this task (repo has no PR CI gate); the script must be CI-friendly though.

## Commands (target)

```
./run-e2e.sh                                   # up -> wait -> playwright -> down
./run-e2e.sh --keep                            # leave stack running for debugging
./run-e2e.sh -- --project=chromium e2e/specs/login.spec.ts
cd src/Client && npx playwright install --with-deps chromium   # one-time
```

Under the hood: `aspire start --apphost src/AppHost/AppHost.csproj` (e2e mode), `aspire wait webapi`, `aspire wait webfrontend`, `npm run e2e`, `aspire stop`.

## Project structure (touched areas)

```
run-e2e.sh                                   new: orchestration script (root, beside run-functional-tests.sh)
src/AppHost/Program.cs                       e2e mode: no volumes, health-gated WaitFor, params from env
src/Infrastructure/Data/ApplicationDbContextInitialiser.cs   e2e-only demo seed (guarded)
src/Client/playwright.config.ts              timeouts, workers, retries, global readiness check
src/Client/e2e/                              fixtures: shared login/storageState; real-backend specs
src/Client/README.md, CLAUDE.md              replace manual credential steps with run-e2e.sh
```

## Code style

Follow existing: file-scoped namespaces, `skestock.Shared.Services` for resource names, no Prettier on client files, Romanian UI strings, TypeScript strict with no `any`. Shell: `set -euo pipefail`, mirror `run-functional-tests.sh`.

## Testing strategy

- E2E (Playwright): add a `globalSetup` that fails fast with a clear message if the API/frontend is unreachable; one login per run saved as `storageState`, reused by real-backend specs (removes parallel-login contention).
- Verify reliability by running the full suite 3 times in a row on a fresh stack and once against a machine with old dev volumes present (must not read or modify them).
- Backend: unit test for the e2e seed guard (seeds only in e2e mode; idempotent). No migration is expected; if one becomes necessary, ask first.

## Boundaries

- Always: keep e2e data isolated from dev volumes; generate credentials per run and never write them to files or logs; keep seeding idempotent; run `dotnet build` and `npm run build` + `npm test -- --watch=false`.
- Ask first: new NuGet/npm dependencies; changing the Aspire pipeline order or normal Development behaviour; any migration; CI workflow changes; changing `DatabaseMigrationRunner`.
- Never: run Respawn/`DatabaseResetter` against the dev DB; commit secrets; change an existing admin's password in normal (non-e2e) mode; edit applied migrations.

## Success criteria

1. `./run-e2e.sh` on a clean machine: containers up, stack healthy, `chromium` project passes, containers removed, exit code reflects test result.
2. Re-running works with zero manual steps and no leftover state; dev volumes are untouched (verified by checksum of `podman volume ls` + dev DB admin still logs in).
3. Specs that previously skipped for missing credentials now run in this script.
4. If the stack is not healthy within the timeout, the script fails with the failing resource named (not a generic Playwright timeout).
5. At least one real-backend spec exercises seeded data (e.g. item list shows seeded items) without `page.route`.
6. 3 consecutive full runs: 0 flaky failures.

## Decisions (approved)

1. E2E mode = env var on the existing AppHost (no new project).
2. Seed data covers all tables (every entity gets deterministic demo rows), e2e mode only.
3. Orchestrator = `aspire start` only; no `dotnet run` fallback.
4. `run-e2e.sh` runs both chromium and mobile projects.
5. Fix the 401: live specs skip with a clear message when login returns 401 (stale admin password) instead of failing.
