# Verification: Stock count wizard (2026-10-17)

| Check | Result |
|---|---|
| `dotnet build` | 0 warnings, 0 errors |
| `dotnet test tests/Application.UnitTests` | 857 passed (existing AdjustStock tests untouched) |
| `./run-functional-tests.sh --filter "BulkAdjustStock\|ClosedClassRejectsChanges"` | 9 passed |
| `npm run build` (src/Client) | passes (existing NG8113 warnings only) |
| `npm test -- --watch=false` | 266 passed, 2 failed in `rooms-section.spec.ts` (not touched by this work; not checked on a clean checkout) |
| `npm run e2e -- stock-count-wizard` (API mocked, `ng serve --port 4300`) | 24 passed, 6 skipped by design (responsive runs on chromium only) |

Responsive: 360/576/768/992/1280/1600 px walked through all three steps. Screenshots reviewed
at 360, 576, 768, 1280 and 1600 px after two layout fixes (clipped "Rezumat" title at 360 px,
no gap under the steps bar).

## Not verified
- Not run against a real AppHost (SQL, SignalR); the e2e API is mocked.
- No real-SQL test of the bulk concurrency (409) path; only a simulated-throw unit test.
- Header summary refresh via SignalR `stockAdjusted` not observed end to end.
- No component spec for `StockCountModal`; clearing the quantity input not checked in a browser.
- `Worker.UnitTests`, `Infrastructure.IntegrationTests` not run.
- Validation reuses `DuplicateReceiptLine` / `LessThanOrEqualTo` codes for duplicate and too-many lines.
