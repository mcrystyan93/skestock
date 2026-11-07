# Plan: Reliable Playwright e2e environment

Spec: `tasks/e2e-environment-spec.md`. Todo: `tasks/e2e-environment-todo.md`. Written by opus from haiku analysis.

## Findings that shrink scope
- Development already runs `InitialiseDatabaseAsync` (migrate + admin, categories, locations + `SeedDemoClassAsync`, `ApplicationDbContextInitialiser.cs:41-44`, 428-835). A fresh DB already has the demo class, items (`DEMO-NN`), stock, receipts, statistics, user profiles.
- Unseeded domain tables: Congregations, MessageTemplates, FruitDessertPlanEntries/Messages, OrderLists/Lines, SupplyLists/Lines, SharedClassConfigurations, ClassItemStockVisibilities, Category/ItemImportBatches(+Files), GoodsReceiptImports(+Lines), FileMetadata.
- `/health` is mapped in all environments (`ServiceDefaults/Extensions.cs:126-130`); seeding finishes before `app.Run`, so healthy webapi = seeded. Run mode only lacks `WithHttpHealthCheck`.
- Aspire CLI 13.6.1 has `start`, `wait <res> --status healthy --timeout N`, `stop`; `wait` names the failing resource.
- `src/Client/.gitignore` already ignores `/playwright/.auth/`.
- AppHost host ports are fixed (1433, 6379, 8081, 7001, 10000-10002) and Azurite is `Persistent` + `storage-data`: without a guard e2e would write into the dev Azurite volume.

## Design
1. **E2E flag**: constant `Services.E2EMode = "E2E"`. When set, AppHost skips `WithDataVolume` on SQL/Redis, uses session lifetime and no volume for Azurite, adds `WithHttpHealthCheck` on web (`/health`) and webfrontend (`/`), passes `E2E=true` to web. Ports stay fixed; script fails fast if 7001/1433 busy.
2. **Credentials**: script generates per-run values (`openssl rand`, Identity-policy-safe) and exports `Parameters__SeedAdministratorEmail/Password`, sql/redis passwords, dummy OpenAI key; env overrides user-secrets so dev secrets are never read or changed. Same values mirrored into `PLAYWRIGHT_LOGIN_*`. Never echoed or written to files.
3. **Seed**: partial class file `ApplicationDbContextInitialiser.E2e.cs` with `SeedE2eAsync()`, called only when `IsDevelopment() && E2E`, after `SeedDemoClassAsync`. Idempotent (name/SKU markers), inserts via DbContext (not Mediator), audit stamps via existing `ExecuteUpdateAsync` pattern. Scope = domain tables only; OutboxMessages, ProcessedMessages, ScheduledJobRuns excluded (would trigger publisher / hide daily job).
4. **Playwright**: `globalSetup` probes frontend and `/api/Users/manage/info`, logs in once, saves storageState to `playwright/.auth/admin.json`; on 401 sets `PLAYWRIGHT_LOGIN_STALE=1`. Config `timeout: 60_000`, `workers: CI ? 1 : '50%'`, retries unchanged. Shared `e2e/auth.ts` `skipUnlessLoggedIn` skips on missing creds or stale flag (decision 5).
5. **`run-e2e.sh`**: podman-socket block copied from `run-functional-tests.sh`; port + node_modules checks; export env; `aspire start --apphost src/AppHost/AppHost.csproj --non-interactive`; `trap` to `aspire stop` unless `--keep`; `aspire wait webapi --timeout 300`, `aspire wait webfrontend --timeout 120`; run `npx playwright test "$@"` (both chromium and mobile) with `PLAYWRIGHT_BASE_URL=http://127.0.0.1:7001`. Never `--force/--volumes`.

## Order and checkpoints
Services constant -> AppHost e2e mode -> run-e2e.sh (**A**: stack up/down, dev volumes untouched) -> Playwright setup/helper -> live specs (**B**: no skips for live specs) -> seed groups (**C**: every in-scope table has rows) -> seeded-data spec -> docs.

## Risks
- `aspire start` may not pass caller env to AppHost; if so use launch profile / args, not a `dotnet run` fallback.
- `WithHttpHealthCheck` on the Vite resource may be unavailable in 13.6; fallback `aspire wait --status up` + globalSetup probe.
- FileMetadata rows without blobs: seed imports in terminal statuses only; no spec downloads them.
- Seeding Fruit/Order/Supply rows could hit ClassLock/ClassOpen: insert via DbContext.
- `MaterializePurchaseStatisticsCommand` already runs in demo seed: do not add receipts after it (or re-run it).
- storageState holds a session cookie, not the password; ignored dir, overwritten each run.
- E2E and dev stacks cannot run simultaneously (fixed ports); script says so.
