# Spec: Database least privilege and encrypted connections (production)

## Objective
In the published docker-compose stack (`aspire-output/`), `webapi` and `worker` connect to SQL Server as
`sa` (sysadmin, also leaked through `SKESTOCKDB_USERNAME/PASSWORD/URI/JDBC...` env vars) and Web applies EF
migrations at startup. Goal: a compromised app process can only read/write data in `skestockDb`, cannot change
schema or touch the server, and the connection is encrypted.
Deployment path in scope: `aspire publish` -> `aspire-output/docker-compose.yaml` + `.env` (gitignored).
Out of scope: GitHub workflow / `deploy/` Quadlet (not used now), local `aspire run`, seed passwords, Redis/Azurite exposure.

## Assumptions
1. Everything runs on one host in the compose network `aspire`; encryption uses `Encrypt=True;TrustServerCertificate=True` (agreed).
2. `sa` stays only in the `dbserver` container and in the one-shot `db-migrate` service.
3. `docker-compose.yaml` is generated: changes must be made in `src/AppHost/Program.cs`, then regenerated.
4. Web startup seeding keeps running with the app login (writes only).

## Design
- Logins (created idempotently, passwords re-applied every run so rotation = redeploy):
  - `skestock_app`: `db_datareader` + `db_datawriter` on `skestockDb`. Used by `webapi` and `worker`.
  - `skestock_migrator`: `db_owner` on `skestockDb`. Used only to apply EF migrations.
- New one-shot compose service `db-migrate` (same image as `webapi`, args `--migrate`, `restart: "no"`):
  1. connects as `sa` -> creates the database if missing, creates/updates the two logins/users/roles;
  2. connects as `skestock_migrator` -> `MigrateAsync()`; exits 0/1.
  `webapi` and `worker` `depends_on: db-migrate: service_completed_successfully`; a failed migration blocks the start.
- `--migrate` runs before the normal host is built (only configuration + DbContext options, no Redis/Blob/OpenAI needed).
- Web in non-Development environments no longer migrates at startup (seed only).
- In publish mode `webapi`/`worker` get only `ConnectionStrings__skestockDb=Server=dbserver,1433;User ID=skestock_app;Password=...;Encrypt=True;TrustServerCertificate=True;Initial Catalog=skestockDb`
  (no `WithReference(database)`, so no `SKESTOCKDB_*` sa variables).
- New Aspire secret parameters `sql-app-password`, `sql-migrator-password` -> `.env`; `sql-password` (sa) is used by `dbserver` and `db-migrate` only.

## Commands
- Build: `dotnet build`
- Unit: `dotnet test tests/Application.UnitTests`
- Integration: `dotnet test tests/Infrastructure.IntegrationTests --filter "FullyQualifiedName~Provision"`
- Functional (Podman): `./run-functional-tests.sh`
- Regenerate compose: `cd src/AppHost && aspire publish -o ../../aspire-output` (needs `PublicOrigins`, `StoragePublicBlobEndpoint`)

## Project Structure
- `src/Infrastructure/Data/DatabaseProvisioner.cs` (new), `DatabaseMigrationRunner.cs` (new)
- `src/Infrastructure/Data/ApplicationDbContextInitialiser.cs` (split migrate vs seed)
- `src/Web/Program.cs` (`--migrate` branch, no startup migration outside Development)
- `src/AppHost/Program.cs`, `aspire-output/docker-compose.yaml` (regenerated)
- tests in `tests/Infrastructure.IntegrationTests/Data/`

## Code Style
Existing conventions (file-scoped namespaces, `Guard.Against`, primary constructors). Preserve CRLF in files that use it (`Program.cs`).
Passwords are never logged; T-SQL for `CREATE/ALTER LOGIN` escapes single quotes (parameters are not allowed there); identifiers via `QUOTENAME`.

## Testing Strategy
- Integration (real SQL Server from TestAppHost): provision on empty server; app login can CRUD but `CREATE TABLE`/`ALTER TABLE` fail; second run is idempotent; changed password takes effect; migrate runner creates the schema as migrator.
- Functional: existing suite still passes; non-Development startup does not migrate.
- Publish check: generated compose has `db-migrate`, completion dependencies, no `User ID=sa`/`SKESTOCKDB_*` in `webapi`/`worker`.
- Manual on host: `docker compose up`, health OK, `sqlcmd` as `skestock_app` cannot create tables.

## Boundaries
- Always: idempotent provisioning; migrations only via `dotnet ef`; secrets only in `.env`/parameters.
- Ask first: new NuGet packages, DB image/version change, exposing/removing published ports.
- Never: `sa` or migrator credentials in `webapi`/`worker` env; committing `.env`; hand-editing generated migrations or `docker-compose.yaml`.

## Success Criteria
1. `webapi` and `worker` connect as `skestock_app`; no `sa` credentials in their env.
2. `skestock_app` cannot run DDL (automated test).
3. `docker compose up` on a DB with pending migrations applies them via `db-migrate` before `webapi` starts; failure aborts startup.
4. Connection strings contain `Encrypt=True`.
5. Re-running is idempotent; existing data keeps working; Development flow (`aspire run`) unchanged.

## Open Questions
1. Port `1433` is published on all host interfaces (`isExternal`). Recommend not publishing it (use `docker exec dbserver ... sqlcmd`). Same applies to Redis 6379, Azurite 10000-10002, redis-commander 8081. Change 1433 in this work? (default: yes, ask before touching the others)
2. Bad-migration rollback stays manual (restore backup).
