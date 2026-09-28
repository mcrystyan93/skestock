# Plan: Observability hardening

Approved spec: `docs/specs/observability-hardening.md`.

## Decisions

- Four independent slices; each is buildable, testable and shippable on its own:
  A request logging, B trace propagation, C production export, D Worker log levels.
- A and D are pure code/config fixes with no schema or deploy impact → done first (low risk,
  immediate value, removes the payload-in-logs exposure).
- B is split by layer along the message path so every step keeps the build green and old
  messages valid: contract + schema → capture → publish → consume.
- C only depends on B for *usefulness* (connected traces), not for correctness; it can ship
  independently.
- No new NuGet packages. `ActivitySource` name `skestock.Messaging`, registered via
  `AddSource("skestock.*")` in ServiceDefaults.

## Dependency graph

```
T1 request logging ─┐
T2 worker log levels├─ independent
                    │
T3 contract+migration ─→ T4 capture interceptor ─→ T5 publisher producer span ─→ T6 worker consumer span
                                                                                   │
T7 production dashboard (independent; verified end-to-end after T6) ◄──────────────┘
T8 final verification (all)
```

Parallelisable: T1, T2, T3, T7. Sequential: T3 → T4 → T5 → T6 → T8.

## Checkpoints

- After T1+T2: `dotnet build`, `dotnet test tests/Application.UnitTests`, no `{@` in `src`.
- After T3: `has-pending-model-changes` clean; legacy envelope test green.
- After T6: `dotnet test tests/Worker.UnitTests`; manual AppHost run shows one trace
  webapi → worker.
- After T7: `bash -n deploy/deploy.sh`; quadlet files reviewed; workflow env render includes
  OTLP endpoint.

## Risks and mitigations

| Risk | Mitigation |
|---|---|
| Removing `{@Request}` loses debugging context | Request name + UserId + trace/span IDs (log correlation) remain; payloads were unsafe anyway. |
| Worker parents to a long-finished Web span → very long traces | Acceptable for a small system; spans are correct W3C parent/child. Revisit with links if traces get unwieldy. |
| Migration on production with in-flight outbox rows | Columns are nullable; no data backfill; publisher/worker tolerate nulls. |
| `StartActivity` returns null when nobody listens | All code null-safe (`activity?.`); tests register an `ActivityListener`. |
| Dashboard container down or restarting | `Wants=` not `Requires=`; OTLP exporter drops silently, app unaffected. |
| Dashboard browser token only in journald | Documented lookup in `deploy/README.md`; UI bound to 127.0.0.1. |
| Timing-based perf test flakiness | Use a 600 ms delay vs 500 ms threshold for the slow case; fast case uses no delay. |

## Task list

Executable tasks with acceptance and verification are in `tasks/todo.md`.
