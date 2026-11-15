# .NET 11 Upgrade Checklist

Branch: `feature/dotnet-11-upgrade`. Sonnet implements, haiku reviews. See `dotnet11-upgrade-plan.md`.

- [x] T0 User confirms: .NET 11 is STS; Aspire has no net11 asset (keep 13.6.5)
- [x] T1 Branch; record baseline test counts on master; install SDK 11.0.100 to ~/.dotnet; global.json -> 11.0.100; dotnet-ef -> 11.0.0
- [x] T2 Directory.Build.props -> net11.0; drop hardcoded net10.0 in Shared/Worker csproj
- [x] T2a Directory.Packages.props: EF/AspNetCore/Extensions/Pkcs -> 11.0.0; add EF SqlServer 11.0.0 pin with comment
- [x] T2b `dotnet build` green: OpenAPI 3 transformers, EXTEXP0001 pragma, analyzer warnings (no NoWarn)
- [x] T2c `dotnet list package --include-transitive`: no EF/AspNetCore 10.x
- [x] T3 OpenTelemetry 1.21.0, Scalar 2.21.0, SkiaSharp x2 4.156.1, Build.* 18.12.2, Test.Sdk 18.12.0; build green
- [x] T4 `has-pending-model-changes`: if changes, STOP and ask
- [x] T4a Review EF 11 breaking changes vs raw SQL / temporal / ExecuteUpdate
- [x] T4b `dotnet test tests/Infrastructure.IntegrationTests` green
- [x] T5 Application.UnitTests + Worker.UnitTests green, counts match baseline
- [x] CHECKPOINT A: report to user
- [x] T6 Dockerfiles -> sdk:11.0/aspnet:11.0; workflow 11.0.x; podman build Web + Worker
- [x] T7 `aspire ps`; start AppHost; seed admin env (not printed); `./run-functional-tests.sh` green; `aspire stop`
- [x] T8 `./run-e2e.sh` green; smoke (scalar/openapi, login, SignalR, exports, import via Worker); screenshots in screenshot-checks/dotnet11/
- [x] CHECKPOINT B: report to user
- [x] T9 Docs: .NET 10 -> 11 in live rule docs; add Aspire/EF pin note to CLAUDE.md
- [x] T10 `graphify update .`
- [ ] T11 Haiku review of `git diff master...`; fix real issues; commit, push, PR
