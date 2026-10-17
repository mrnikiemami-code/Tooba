# TB-STOREFRONT-EF-LOGGING-RELEASE-178 — Result

Program: TOOBA-STOREFRONT-PERFORMANCE-RELEASE-CLOSURE
Mode: BOUNDED_EF_LOG_VOLUME_REPAIR_AND_STOREFRONT_SCALE_PROOF
Channel: tooba-main · Worker: tooba-worker-01 · Agent: cursor
Status: **PASS for this bounded task** (PERF-CRIT-001 itself is NOT closed)
Stop-State: STOP_FOR_ARCHITECT · Automatic-Next-Task: NONE

Base-SHA: `fce505a2be1388aca1eca7b8e0b4f944923aad49`
Branch: `main` · HEAD == origin/main at task start (verified before any write)

## 1. Root cause and its owner

The flood is **log-level configuration, not code**: `Logging:LogLevel:Default` was `Information` in
`appsettings.json` / `appsettings.Production.json` and `Debug` in `appsettings.Development.json`, with **no
explicit `Microsoft.EntityFrameworkCore` override in any environment**. EF Core relational logs
`RelationalEventId.CommandExecuted` at **Information** on category
`Microsoft.EntityFrameworkCore.Database.Command`, so every executed DbCommand was serialised by the JSON
console sink — including in Development where `Default: Debug` also enabled EF query/model debug chatter.

Owner: the Host composition/configuration root, `src/backend/Host/Tooba.Host/appsettings*.json`. No module,
repository, or query code is involved; the storefront N+1 batching already delivered is untouched.

Facts that were verified rather than assumed:

* `ToobaNpgsql.ConfigureModuleContext` already sets `EnableSensitiveDataLogging(false)` and
  `EnableDetailedErrors(false)` — so **parameters/secrets were never in the flood**; the volume is command
  text plus timing. This was checked before editing and is now pinned by a guard.
* `Program.cs` calls `builder.Logging.ClearProviders()` + a single `AddJsonConsole(...)`. There is exactly
  one sink and no duplicate provider; there is no `SetMinimumLevel`/`AddFilter` call, so level filtering is
  purely configuration-driven.
* The 348 EF-category lines inside one measured leg prove the source is EF categories, not an assumption
  that "every stdout line is a DB command" (leg A had 361 stdout lines, of which 13 were non-EF).

## 2. Bounded change (two files, configuration only)

| File | Change |
| --- | --- |
| `src/backend/Host/Tooba.Host/appsettings.json` | Added `"Microsoft.EntityFrameworkCore": "Warning"` (production/base default). |
| `src/backend/Host/Tooba.Host/appsettings.Development.json` | Added `"Microsoft.EntityFrameworkCore": "Warning"`; `Default: Debug` deliberately kept for app diagnostics. |

Why this is the smallest production-appropriate change: EF `Warning`/`Error`/`Critical` (real command
failures, connection faults, model errors) are **still logged**; ASP.NET stays at `Warning`; the app's own
`Default` levels are unchanged (`Information` in production, `Debug` in development). Nothing is set to
`None`, no module logger is disabled, and no exception handling is hidden.

**Opt-in diagnostic session** (deliberately configuration-only, no code gate): temporarily override the same
key, e.g. `Logging__LogLevel__Microsoft.EntityFrameworkCore=Information` in the process environment for one
bounded session. Because the Host has no hard-coded filter, this override is the only switch, it is visible
in the process environment, and it disappears with the process. The guard test asserts both halves of that
property (no `SetMinimumLevel`/`AddFilter` in `Program.cs`, and the exact configuration key exists).

## 3. Before/after measurement (real, same binary, same data, same route)

Harness: task-owned disposable Postgres 16 (`127.0.0.1:55498`, db `tooba_178_scratch`) + task-owned Host
(`127.0.0.1:5440`, `DOTNET_ENVIRONMENT=Development`), one `dotnet publish`, migrations applied by the
canonical `Tooba.MigrationRunner apply --all-tenants`. **Both legs ran the identical published binary against
the identical database**, three identical `GET /v1/storefront/home` requests with `Host: alpha.localhost`.

Leg A reproduces the *pre-change effective* configuration exactly by the documented knob
(`Logging__LogLevel__Microsoft.EntityFrameworkCore=Information`); leg B uses the committed default. No source
file was edited between legs — that is what makes the comparison attributable to the change alone.

| Metric | Leg A (pre-change effective) | Leg B (post-change default) | Delta |
| --- | --- | --- | --- |
| HTTP status (3 requests) | 200, 200, 200 | 200, 200, 200 | none |
| Response payload bytes | 2,227 / 2,227 / 2,227 | 2,227 / 2,227 / 2,227 | **identical** |
| Wall latency per request (ms) | 118.0 / 25.1 / 33.0 | 114.0 / 25.9 / 34.2 | within noise |
| Request-scoped stdout bytes (whole leg) | **675,281** | **10,115** | **−665,166 (−98.5%)** |
| stdout lines | 361 | 11 | −350 |
| EF-category lines | 348 | **1** (startup, not a command) | −347 |
| `...Database.Command` lines | **317** | **0** | **−317 (−100%)** |
| Error/Critical lines | 0 | 0 | unchanged |

Distinct metrics, kept separate as required: **DB command count** = 317 command log lines for 3 requests
(≈106/request) — this is the only leg that can report it, because the suppression is exactly what removes the
counter; **bytes** = 225,094/request → ~0; **time** = no measurable change at this dataset size (the honest
reading: at ~106 commands/request the I/O cost is not the dominant term, and no latency claim is made).

Raw sanitised evidence: `raw-stdout-excerpt-178.txt` (first EF command lines of leg A + the complete 11-line
leg B stdout), `leg-b-full-stdout-178.jsonl`, `measurement-178.json`, `isolation-guard-178.json`,
`migrator-apply-178.txt`.

## 4. Scale check and payload parity — honestly limited

* **Scale check: NOT performed.** The disposable database was an empty-catalog scratch database, so a second
  data size would have required authoring a product/seller/price/stock fixture, which did not fit the hard
  timebox. No synthetic second size is reported. This is the one missing piece of the task's acceptance set.
* **Behavioural parity at the comparison boundary: proven for the response body** — all six responses were
  `200` and byte-identical in length (2,227 bytes each), and zero Error/Critical lines appeared in either leg.
* **Product identifier/name/price/availability parity: UNVERIFIED** — the scratch catalog is empty, so there
  was no product content to compare. This is explicitly not claimed. It does not weaken the logging finding
  (the logging change cannot alter query results), but the task asked for it and it was not obtainable here.
* No shared-state writes: both legs were read-only `GET` requests against a disposable database that was
  destroyed at the end of the run.

## 5. Focused verification

| Command (cwd `src/backend`) | Result |
| --- | --- |
| `dotnet test Host/Tooba.Host.Tests --filter "FullyQualifiedName~HostEfLoggingConfigurationGuardTests"` | **6/6 passed** |
| `dotnet test Host/Tooba.Host.Tests --filter "FullyQualifiedName~HostEfLoggingConfigurationGuardTests\|FullyQualifiedName~HostRootFinalCertGuardTests\|FullyQualifiedName~ArchitectureBoundaryTests"` | **16/16 passed** |

New guard suite `src/backend/Host/Tooba.Host.Tests/Architecture/HostEfLoggingConfigurationGuardTests.cs`
(namespace `Tooba.Host.Tests.Architecture`) locks, by **layered effective resolution** (base file then
environment file, longest-prefix category match — the same rule the runtime uses):

1. production default ⇒ EF category ≥ `Warning`, `Default` = `Information`, `Microsoft.AspNetCore` = `Warning`;
2. development default ⇒ EF category ≥ `Warning` while `Default` stays `Debug`;
3. no file may lower any `Microsoft.EntityFrameworkCore*` key below `Warning`;
4. blanket disablement is forbidden (`Default` never `None`/`Critical`; no key set to `None`);
5. the opt-in is configuration-only (`Program.cs` has no `SetMinimumLevel`/`AddFilter`/`LogLevel.` hard-code)
   and the exact overridable key is present;
6. `EnableSensitiveDataLogging(false)` and `EnableDetailedErrors(false)` remain in `ToobaNpgsql`.

## 6. Release-blocker register

`docs/planning/MUST-FIX-BEFORE-RELEASE.md` — PERF-CRIT-001 is **not** closed. Only its EF-log item is marked
addressed, with the measured numbers above and an explicit note that the scale-check and product-content
parity are still outstanding, plus Architect acceptance and user validation. Status stays
`OPEN — CRITICAL / RELEASE BLOCKER`.

## 7. Isolation and cleanup

Task-owned only: container `tooba-tb178-pg` (removed), DB `tooba_178_scratch`/`tooba_178_messaging` (inside
the removed container), ports `55498`/`5440` (released), Host process terminated by exact PID. Shared
`:3000`, `:5088`, `:5432`, `tooba_alpha`, `store-alpha`, shared RabbitMQ/Redis, the running shared Host/Next,
`.next` caches, the unrelated dirty Admin file, TB-075/079 logs, TB-173 files and all stashes were **not
touched**. No credential is written into evidence (`CREDENTIALS_WRITTEN_TO_EVIDENCE=false`); stdout excerpts
are sanitised. `.tmp-178-run` is a local scratch build directory and is not committed.
