# .NET 11 Upgrade Plan (skestock)

Status: plan only, awaiting review. Branch `feature/dotnet-11-upgrade`, one PR, deploy stays manual. Sonnet implements T1-T10. Haiku reviews the diff in T11.
Sources: haiku readiness analysis + opus planner (package versions checked against nuget.org on 2026-11-15).

## Engineering level

Production app: migrations auto-apply at startup, auth and data are live. Data safety (EF model, temporal tables) and auth (Identity) get production-grade checks. Everything else stays lean: bump versions, fix what the compiler and tests report, nothing more. No refactors, no new patterns, no new dependencies.

## Decisions for the user (read first)

- **.NET 11 is STS.** .NET 10 is LTS. Both end support around Nov 2028, so 11 does not extend the support window. The gain is EF 11 and ASP.NET 11 features. Confirm this is still wanted.
- **Aspire has no release targeting .NET 11.** 13.6.5 is the newest on nuget.org (prereleases included) and ships net8/9/10 assets only. Plan: keep Aspire 13.6.5 and let net11 projects consume the net10 assets. Fallback if AppHost/TestAppHost break: pin just those two projects to `net10.0` (ask first). Alternative: wait for an Aspire release with net11.

## Verified package versions

| Package | Current | Latest stable | Action |
|---|---|---|---|
| EF Core (+ Design, InMemory, Relational, Sqlite) | 10.0.14 | 11.0.0 | bump |
| EF Core SqlServer (transitive via Aspire) | 10.0.14 | 11.0.0 | **add explicit PackageVersion pin** (EF 10 provider + EF 11 core breaks at runtime) |
| AspNetCore.Diagnostics.EFCore / Identity.EFCore / Mvc.Testing / OpenApi | 10.0.14 | 11.0.0 | bump |
| AspNetCore.SignalR.StackExchangeRedis | 10.0.15 | 11.0.0 | bump |
| Extensions.Hosting / HealthChecks.EFCore / ApiDescription.Server | 10.0.x | 11.0.0 | bump |
| Extensions.Caching.Hybrid / Http.Resilience / ServiceDiscovery / TimeProvider.Testing | 10.11.0 | 11.0.0 | bump |
| System.Security.Cryptography.Pkcs | 10.0.14 | 11.0.0 | bump |
| Microsoft.OpenApi (transitive) | 2.x | 3.x (required by AspNetCore.OpenApi 11) | major bump; fix 3 transformers |
| dotnet-ef (`.config/dotnet-tools.json`) | 10.0.13 | 11.0.0 | bump |
| OpenTelemetry.* (5) | 1.20.1 | 1.21.0 | bump |
| Scalar.AspNetCore | 2.18.3 | 2.21.0 | bump |
| SkiaSharp + NativeAssets.Linux.NoDependencies | 4.153.1 | 4.156.1 | bump together |
| Microsoft.Build.Tasks.Core / Utilities.Core | 18.11.0 | 18.12.2 | bump |
| Microsoft.NET.Test.Sdk | 18.11.0 | 18.12.0 | bump |
| Aspire.* (13 packages) + Aspire.AppHost.Sdk | 13.6.5 | 13.6.5 | **keep, no net11 asset** |
| FusionCache (4 packages) | 2.9.0 | 2.9.0 | keep, no 11.x release |
| MessagePack (GHSA pin) | 2.5.307 | 2.5.307 (3.x breaking) | keep |
| Mediator 3.0.2, Ardalis, FluentValidation, FluentResults, ClosedXML, Azure.*, JWT, NUnit/Moq/Respawn/Shouldly, coverlet | = latest | | keep |

Images: `mcr.microsoft.com/dotnet/sdk:11.0` and `aspnet:11.0` exist. `actions/setup-dotnet@v4` with `11.0.x` works.

## Dependency graph

```
T1 local SDK + global.json + dotnet-ef
 └─ T2 net11 + Microsoft 11.0.0 pkgs + EF SqlServer pin -> build green
     └─ T3 third-party bumps -> build green
         └─ T4 EF model check + Infrastructure.IntegrationTests
             └─ T5 unit tests
                 ── CHECKPOINT A ──
                 ├─ T6 Dockerfiles + CI + image builds
                 ├─ T7 functional tests (AppHost)
                 └─ T8 e2e + smoke
                     ── CHECKPOINT B ──
                     └─ T9 docs -> T10 graphify -> T11 haiku review -> PR
```

## Tasks

### T1: Branch, local SDK, global.json, dotnet-ef
- `git switch -c feature/dotnet-11-upgrade` from up-to-date master. Leave untracked `tasks/order-wizard-*` alone.
- Record baseline test counts on master first (Application, Worker, Infrastructure.IntegrationTests).
- Install SDK user-scope only: `dotnet-install.sh --channel 11.0 --install-dir ~/.dotnet`. Never touch `/usr/lib64/dotnet`. Each shell: `export DOTNET_ROOT=~/.dotnet PATH=~/.dotnet:$PATH`. Tell the user Rider's toolset path needs pointing at `~/.dotnet`.
- `global.json` -> `11.0.100`, `rollForward: latestFeature`, no `paths` entry.
- `dotnet-ef` -> 11.0.0, `dotnet tool restore`.
- Acceptance: `dotnet --version` is 11.0.1xx; `dotnet ef --version` is 11.0.0.

### T2: Retarget net11.0 + Microsoft 11.0.0 packages
- `Directory.Build.props:4` -> `net11.0`. Remove hardcoded `net10.0` in `src/Shared/Shared.csproj:9` and `src/Worker/Worker.csproj:4`.
- `Directory.Packages.props`: bump per table. Add `Microsoft.EntityFrameworkCore.SqlServer` 11.0.0 with a one-line comment (transitive via Aspire 13.6.5; drop once Aspire targets net11). Keep `CentralPackageTransitivePinningEnabled`.
- `dotnet build`; smallest fix per site:
  - OpenAPI.NET 3: `src/Web/Infrastructure/{ApiExceptionOperationTransformer,IdentityApiOperationTransformer,BearerSecuritySchemeTransformer}.cs`. Check the migration notes, don't guess.
  - `#pragma EXTEXP0001` at `src/Infrastructure/DependencyInjection.cs:184,209`: remove if GA, update if the ID changed.
  - New analyzer warnings (warnings are errors): fix code. No `NoWarn` unless generated code (Mediator), and ask first.
- `dotnet list package --include-transitive | grep -E " 10\.0\.[0-9]+"`: no EF/AspNetCore on 10.x.
- Acceptance: build 0 warnings, 0 errors.

### T3: Third-party bumps
- OpenTelemetry 1.21.0, Scalar 2.21.0, SkiaSharp x2 4.156.1, Microsoft.Build.* 18.12.2, Test.Sdk 18.12.0. Leave the rest.
- Acceptance: build green.

### T4: EF 11 model safety + integration tests (production grade)
- `dotnet ef migrations has-pending-model-changes --project src/Infrastructure --startup-project src/Web --context ApplicationDbContext`.
- **If it reports changes: STOP and ask the user.** Migrations auto-apply at startup (`DatabaseMigrationRunner`), so a model diff blocks or alters production. If approved: review the generated SQL for temporal/retention breakage, re-append `SetTemporalRetention` if columns change, never hand-edit the snapshot.
- Read the EF Core 11 breaking-changes page against `ClassLocking.cs`, `AuditableEntityInterceptor.cs` and the two `ExecuteUpdate` sites.
- `dotnet test tests/Infrastructure.IntegrationTests` (includes `TemporalSchemaTests`).
- Acceptance: no pending changes (or approved, reviewed migration); tests green.

### T5: Unit tests
- `dotnet test tests/Application.UnitTests` and `tests/Worker.UnitTests`. Fix only upgrade-caused regressions. Counts equal the master baseline.

**CHECKPOINT A:** clean build, no EF diff, unit + integration green. Report before container/stack phases.

### T6: Docker + CI
- `src/Web/Dockerfile` and `src/Worker/Dockerfile`: `sdk:10.0` -> `sdk:11.0`, `aspnet:10.0` -> `aspnet:11.0`. Leave `node:22`.
- `.github/workflows/deploy-production.yml`: `DOTNET_VERSION: "11.0.x"`.
- `podman build` both images. Worker image `dotnet --list-runtimes` shows 11.0.0. No missing SkiaSharp native libs.

### T7: Functional tests
- `aspire ps` first; if the user's dev stack runs, don't stop it, ask.
- `aspire start --apphost src/AppHost/AppHost.csproj`, `aspire wait webapi` (also the Aspire-on-net11 proof).
- Export `SeedAdministrator__Email/Password` from user-secrets without printing. `./run-functional-tests.sh`. `aspire stop`.

### T8: E2E + smoke
- Dev stack stopped, then `./run-e2e.sh` (port 10000 busy: follow CLAUDE.md section 10).
- Smoke on `--keep` stack, screenshots in `screenshot-checks/dotnet11/`: `/scalar` + `/openapi/v1.json`, login/logout, a SignalR update, Excel + image export, goods-receipt import through the Worker.
- Client untouched, so skip npm build/test unless `src/Client` changed.

**CHECKPOINT B:** full stack works on net11. Report including skips.

### T9: Docs
- ".NET 10" / `net10.0` / "SDK 10.0.110" -> 11 in `CLAUDE.md`, `AGENTS.md`, `.github/copilot-instructions.md`, `.codex/instructions.md`, `docs/codebase/STACK.md`, `docs/codebase/CONVENTIONS.md`. Add one line to CLAUDE.md: Aspire 13.6.5 has no net11 asset; EF SqlServer pinned in `Directory.Packages.props`.
- Leave `docs/specs/*`, `.scratch/*`, vendored skills alone.

### T10: `graphify update .`

### T11: Review + PR
- Haiku reviews `git diff master...` with goal only ("upgrade to .NET 11 + latest packages, no behaviour change"). Fix real issues.
- Commit, push, `gh pr create` with versions, Aspire/STS caveats, test results.

## Risks and rollback

| Risk | Mitigation |
|---|---|
| Aspire 13.6.5 fails on net11 | Pin AppHost + TestAppHost to net10.0 (ask first) |
| EF 11 model diff -> production migration | T4 stops and asks |
| EF SqlServer 10.x with EF 11 core | Explicit 11.0.0 pin, checked in T2 |
| OpenAPI.NET 3 breaks transformers/Scalar | Fix 3 files; smoke `/scalar` |
| New analyzers fail build | Fix code, no blanket NoWarn |
| FusionCache/HybridCache on M.E.Caching 11 | Unit, functional, smoke |
| SkiaSharp natives in aspnet:11.0 | T6 container check + export smoke |
| Deploy | Manual. Rollback = redeploy previous image / revert merge. Safe only if no new migration (T4) |

## Not verified
OpenAPI 3, EF 11 and ASP.NET 11 breaking-change pages, and whether Aspire 13.6.5 builds a net11 project. These surface at build time (T2) and AppHost start (T7), with the fallback above.
