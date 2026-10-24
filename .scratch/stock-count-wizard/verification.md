# Verification: Stock count wizard (2026-10-17)

| Check | Result |
|---|---|
| `dotnet build` | 0 warnings, 0 errors |
| `dotnet test tests/Application.UnitTests` | 857 passed (existing AdjustStock tests untouched) |
| `./run-functional-tests.sh --filter "BulkAdjustStock\|ClosedClassRejectsChanges"` | 9 passed |
| `npm run build` (src/Client) | passes (existing NG8113 warnings only) |
| `npm test -- --watch=false` | all pass (269); `rooms-section.spec.ts` was stale after RoomsSection was split into a container and `RoomsCard`, and was rewritten to test the container |
| `npm run e2e -- stock-count-wizard` (API mocked, `ng serve --port 4300`) | 24 passed, 6 skipped by design (responsive runs on chromium only) |

Responsive: 360/576/768/992/1280/1600 px walked through all three steps. Screenshots reviewed
at 360, 576, 768, 1280 and 1600 px after two layout fixes (clipped "Rezumat" title at 360 px,
no gap under the steps bar).

## Live run against the full AppHost (2026-10-17, later the same day)
SQL Server (Podman), Redis, Azurite, Web, Worker and the Vite client, seeded through the real API
(1 class, 3 items, 2 locations, 4 stock rows). A browser drove the wizard; `/api` calls were
forwarded through an API-authenticated session because the dev HTTP->HTTPS redirect drops the
login cookie in the browser (unrelated to this feature).
- Found and fixed: the store requested `includeHidden: true`, which the API treats as "only hidden
  zero-stock rows", so the wizard saw no stock (the mocked tests could not catch this). It now sends
  `includeHidden: false`, with a unit test.
- Found and fixed: the header summary stayed stale after a save when the SignalR connection was down;
  `HeaderContainer` now also calls `loadSummary` after a saved count.
- Confirmed against the real stack: one `bulk-adjust` request with only the changed lines -> 200;
  stored quantities match; list reloads; header low-stock count 0 -> 1; validation 400s
  (`empty_lines`, `duplicate_receipt_line`, `invalid_reference`) and nothing saved when one line is invalid.
- Not confirmed live: the SignalR `stockAdjusted` push (the hub returns 401 in this local setup),
  the 409 conflict path, the closed-class rejection (functional test covers it).

## Not verified
- Playwright specs in the repo still mock the API; the live run above was a one-off script.
- No real-SQL test of the bulk concurrency (409) path; only a simulated-throw unit test.
- SignalR `stockAdjusted` push not observed (hub 401 locally); the header now also reloads its summary directly.
- No component spec for `StockCountModal`; clearing the quantity input not checked in a browser.
- `Worker.UnitTests`, `Infrastructure.IntegrationTests` not run.
- Validation reuses `DuplicateReceiptLine` / `LessThanOrEqualTo` codes for duplicate and too-many lines.
