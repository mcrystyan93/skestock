# Plan: Database least privilege (see SPEC-database-least-privilege.md)

Order: provisioning code -> migrate mode -> AppHost/compose wiring -> regenerate + verify. Each step is testable alone.

1. **DatabaseProvisioner** (Infrastructure): idempotent T-SQL (create DB, logins, users, roles) via `Microsoft.Data.SqlClient`. Tested against real SQL Server. Riskiest part (SQL escaping, permissions) -> first.
2. **Migration runner + `--migrate` mode**: provision as `sa`, migrate as migrator, exit code. Web skips startup migration outside Development. Depends on 1. Risk: `Program.cs` early branch must not build the full host (no Redis/Blob/OpenAI config in the one-shot).
3. **AppHost publish wiring**: parameters, `db-migrate` service (`WaitForCompletion` for web/worker), app-only connection strings, drop `WithReference(database)` in publish mode, `restart: "no"`. Depends on 2. Risk: Aspire compose API coverage for restart policy / shared image.
4. **Regenerate & verify**: `aspire publish`, inspect compose, add new `.env` values, scratch `docker compose -p skestock-verify up` check. Depends on 3.
5. **(Optional, on approval)** stop publishing 1433.

Checkpoints: `dotnet build` (warnings = errors) after each task; integration tests after 1-2; compose diff review after 3.
