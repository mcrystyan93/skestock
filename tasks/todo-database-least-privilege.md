# Tasks: Database least privilege

- [x] Task 1: DatabaseProvisioner
  - Acceptance: creates DB/logins/users/roles idempotently; app login = datareader+datawriter; migrator = db_owner; password change applied; quotes escaped.
  - Verify: `dotnet test tests/Infrastructure.IntegrationTests --filter "FullyQualifiedName~Provision"` (CRUD ok, DDL fails, rerun ok).
  - Files: Infrastructure/Data/DatabaseProvisioner.cs, tests/Infrastructure.IntegrationTests/Data/DatabaseProvisionerTests.cs

- [x] Task 2: `--migrate` mode + no startup migration outside Development  (depends on 1)
  - Acceptance: `dotnet skestock.Web.dll --migrate` provisions (sa), migrates (migrator), exit 0/1; needs only DB config; Production startup seeds without migrating.
  - Verify: integration test running the runner on an empty DB; `./run-functional-tests.sh`; build.
  - Files: Infrastructure/Data/DatabaseMigrationRunner.cs, Infrastructure/Data/ApplicationDbContextInitialiser.cs, Web/Program.cs, test file

- [x] Task 3: AppHost publish wiring  (depends on 2)
  - Acceptance: parameters `sql-app-password`/`sql-migrator-password`; `db-migrate` service; webapi/worker wait for its completion and use app-only encrypted connection string; no `SKESTOCKDB_*` sa vars; `aspire run` unchanged.
  - Verify: `dotnet build`; `aspire publish` to a temp dir and grep the compose file.
  - Files: src/AppHost/Program.cs (+ Shared/Services.cs for names)

- [x] Task 4: Regenerate compose and verify  (depends on 3)
  - Acceptance: `aspire-output/docker-compose.yaml` regenerated; `.env` documented (`SQL_APP_PASSWORD`, `SQL_MIGRATOR_PASSWORD`); scratch stack starts healthy; `sqlcmd` as app user cannot DDL.
  - Verify: `docker compose -p skestock-verify up` health + manual DDL check; then tear down scratch project.
  - Files: aspire-output/docker-compose.yaml (generated), aspire-output/.env (local, ignored)

- [x] Task 5 (optional): stop publishing port 1433
  - Acceptance: `dbserver` has no host port; app still connects over the compose network.
  - Files: src/AppHost/Program.cs, regenerated compose
