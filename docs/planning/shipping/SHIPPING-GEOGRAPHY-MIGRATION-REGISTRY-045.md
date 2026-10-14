# Shipping — Geography Migration Registry Report (`TB-SHIPPING-GEOGRAPHY-MIGRATION-REGISTRY-045`)

> **STATUS: PASS — the canonical migration-order constant and the production MigrationRunner descriptor are CONFIGURED metadata only. Host startup still cannot migrate Geography (runtime-verified), no Tooba database was contacted, no migration was applied, and no schema changed.**

| Field | Value |
| --- | --- |
| Repository / Branch | `mrnikiemami-code/Tooba` / `main` |
| Expected-Base-SHA | `5e07617c04c4098efb7d1a4af99f5d6a619c8448` |
| Base-SHA (verified `HEAD == origin/main`) | `5e07617c04c4098efb7d1a4af99f5d6a619c8448` |
| Code-SHA / Evidence-SHA | `c316c7f46d2ba55226bdcc8dc5157833bda82263` |
| Parent task | `TB-SHIPPING-GEOGRAPHY-MODULE-COMPOSITION-044` |
| Mode | `BOUNDED_GEOGRAPHY_MIGRATION_METADATA_NO_APPLY` |
| Status | `PASS` |
| Stop-State | `STOP_FOR_ARCHITECT` |
| automaticNextImplementationTask | `NONE` |

## 1. Preflight

- Claimed exactly one Pending item on `tooba-main` after `BRIDGE-WAKE` (row `f6ba8924-1ff8-423c-be38-88cb0b6f5dce`); the received artifact was persisted verbatim to `docs/ai/tasks/TB-SHIPPING-GEOGRAPHY-MIGRATION-REGISTRY-045.task.md` (receipt only, never staged, never replayed).
- `git fetch origin` → `HEAD == origin/main == Expected-Base-SHA == 5e07617c04c4098efb7d1a4af99f5d6a619c8448`; no stash/reset/rebase/force-push. Pre-existing unrelated dirty work (`src/backend/Modules/Order/Tooba.Order.Infrastructure/Checkout/Abuse/CheckoutAbuseGate.cs`) was preserved, untouched and unstaged.
- Read `AGENTS.md`, `.cursor/skills/tooba-backend-feature/SKILL.md`, ADR-036, reports 042-044, the canonical order source, the runner registry, the orchestrator, the CLI entry, and every guard test that reads either file. Findings register not scanned.
- **Confirmed no Geography constant or descriptor already existed** (0 hits in both files before the change), and **verified candidate slot 30 against the actual definitions**: the used values are `1..12`, `14..29` (13 is intentionally retired and explicitly guarded against reuse). `30` is unused, so the addition is append-only and renumbers nothing.
- **Inspected every affected startup/runtime path** (see §3) before writing anything.

## 2. Changed paths (exactly two files, no csproj change needed)

| Path | Change |
| --- | --- |
| `src/backend/BuildingBlocks/Tooba.Persistence/ModuleSchemaMigrator.cs` | +2 lines: one XML summary line and `public const int Geography = 30;` appended after `Support = 29`. Nothing else in the file changed; no literal `13`, no renumbering. |
| `src/backend/Host/Tooba.MigrationRunner/ModuleMigrationRegistry.cs` | +1 `using Tooba.Geography.Infrastructure.Persistence;` and +5 lines at the end of `All`: an explanatory comment and `Descriptor<GeographyDbContext>("Geography", GeographyDbContext.Schema),` appended after `Story`. All 31 pre-existing descriptors untouched and in their original order. |

Diff: **8 inserted lines, 0 removed, 0 modified**. No `Tooba.MigrationRunner.csproj` change was necessary: `GeographyDbContext` is already reachable transitively through the `Tooba.Host` project reference that now includes `Tooba.Geography.Infrastructure` (the build proves it), so no reference was added for its own sake.

Explicitly **not** touched: `GeographyModule.cs` (no migrator registration), `Program.cs`, `DevelopmentSchemaMigrator.cs`, any migration source/Designer/snapshot, `GeographyDbContext`, `Tooba.slnx`, `Program.cs` of the runner, the event/authorization/permission flows, `docs/architecture/tmar-module-structure-manifests.json`, `tmar-current-state.json`, every guard test, `appsettings*`, any Order/AddressBook/Fulfillment/Checkout/frontend file. The unrelated `CheckoutAbuseGate.cs` modification is pre-existing and was not staged.

## 3. Runtime non-activation proof (does descriptor presence execute migration?)

**Path analysis, in order of the actual runtime graph:**

1. `ModuleMigrationRegistry.All` is declared `internal` in *`Tooba.MigrationRunner`*, a standalone CLI executable project.
2. It is consumed in exactly one place in the whole repository: `MigrationOrchestrator.RunAsync` (`foreach (var descriptor in ModuleMigrationRegistry.All)`). Production `status`/`plan` only read `GetAppliedMigrationsAsync`/`GetPendingMigrationsAsync`; only the explicit `apply` command calls `MigrateAsync`, and only `Tooba.MigrationRunner.Program.Main` reaches the orchestrator.
3. `Tooba.MigrationRunner` **references** `Tooba.Host`; nothing in the Host graph references it back. This is now proven at the assembly level, not by reading source: loading the built `Tooba.Host.dll` shows **no** `Tooba.MigrationRunner` reference (check `N5`).
4. Therefore a descriptor's *presence* executes nothing. It becomes visible only when a human explicitly runs the runner CLI against an explicitly resolved target — which this Task did not do.
5. Independently, the Development startup auto-migration path is unchanged and still cannot reach Geography: `Host/Tooba.Host/Development/DevelopmentSchemaMigrator.MigrateOrderedAsync` enumerates `IModuleSchemaMigrator`, and `GeographyModule` still registers **zero** of them (runtime check `N1`), zero `IModuleSchemaMigrationStep` and zero `IHostedService` (`N2`). `0` changed files exist under `Program.cs`/`Development` (`log-diff.txt`).

**Honest boundary of the claim:** the new `Geography = 30` constant is *also* still unconsumed — `GeographyModule` deliberately does not reference it (044's decision, restated in the activation plan of §7). So this Task configured metadata for a module the running Host still cannot migrate.

## 4. Focused checks

**Single consolidated metadata harness, 15/15 PASS** (`log-migration-metadata.txt`; harness source archived at `harness/`, temp project and its OutDir deleted afterwards). No service startup, no database contact, no Docker.

| Group | Check | Result |
| --- | --- | --- |
| Order table (compiled public API, reflection) | `M1` `Geography` exists on the single canonical source and equals `30` | PASS |
| | `M2` all 29 order values unique | PASS (`29 constants, 29 distinct`) |
| | `M3` all 28 pre-existing constants keep their exact previous values (append-only) | PASS |
| | `M4` exactly one constant was added (29 total) | PASS |
| Runner registry (compiled **internal** type, reflection over the built assembly) | `R1` all 31 pre-existing descriptors preserved in exact original order | PASS |
| | `R2` exactly one descriptor appended, it is `Geography`, names unique | PASS (32 total) |
| | `R3` descriptor declares schema `geography` | PASS |
| | `R4` descriptor factory builds a `GeographyDbContext` against a **closed port** with no exception → no database contact | PASS |
| | `R5` descriptor sees exactly the one geography migration from 042 (`20261014113949_InitialGeography`) → metadata complete, no pending-only model | PASS |
| | `R6` descriptor schema matches the module's own `GeographyDbContext` model | PASS |
| Non-activation (Host-loaded module) | `N1` `GeographyModule` registers **zero** `IModuleSchemaMigrator` | PASS |
| | `N2` zero `IModuleSchemaMigrationStep`, zero `IHostedService` | PASS |
| | `N3` the two read seams are still the only module services | PASS |
| | `N4` module graph still resolves without database contact (`ValidateOnBuild` + `ValidateScopes`) | PASS |
| | `N5` `Tooba.Host` references no `Tooba.MigrationRunner` (registry unreachable from startup) | PASS |

**Builds** — `Tooba.MigrationRunner` builds clean with the new descriptor (`Build succeeded, 0 Error(s)`), which also compiles `Tooba.Host`, `Tooba.Persistence` and `Tooba.Geography.Infrastructure` in the same graph.

**Build-output disclosure:** the running Development Host (PID 37028) holds its default `bin/Debug/net8.0` assemblies, so a default-output build cannot copy (`MSB3027/MSB3021`). Because restarting the Host is forbidden, the runner was built with a redirected output directory twice (an external temp one for the first run, then the repo-local `src/backend/Host/Tooba.MigrationRunner/bin/chk045/` so the archived harness is reproducible). The Host was never stopped, restarted, or otherwise affected; both redirected output directories were deleted afterwards.

**Architecture tests that pin order/registry — assessed, none weakened, none edited:**

| Guard | What it pins | Impact of this change |
| --- | --- | --- |
| `PlatformProbeAmcW2DetachGuardTests` | order file: `DoesNotContain PlatformProbe`, `DoesNotContain "= 13;"` | unaffected — the added line is `= 30;` and the file still contains `0` occurrences of `= 13;` |
| `PlatformProbeAmcW3StructureGuardTests` | same, plus `Contains Promotion=12 / Reviews=14 / ProductQnA=15 / Support=29` | unaffected — all four literal lines are untouched |
| `PlatformProbeAmcW4CertGuardTests` | both files: `DoesNotContain PlatformProbe` / `Tooba.PlatformProbe` | unaffected |
| `HostDevelopmentMigrationSeamGuardTests` | counts registrations over a fixed 28-path list; seam file has no `*DbContext` names | unaffected — the added constant is a plain `int` named `Geography` |
| `WalletFoundationTests` / `SupportFoundationTests` | registry `Contains "WalletDbContext"` / `"SupportDbContext"` | unaffected — additive |
| `ProductWorkspace*CertGuardTests` | registry `DoesNotContain "ProductWorkspace"` | unaffected |
| `WalletModuleAmsc001W1MigrateGuardTests` | host-scan allowlist naming the registry file | unaffected |
| `MigrationRunnerIntegrationTests` (Docker, **not run** — needs a live container and is out of scope) | `Assert.All(..., s => s.Succeeded)` then Catalog-pending assertions | logically unaffected: the appended Geography descriptor applies the already-existing 042 migration, and no assertion pins the descriptor count or total module list |

`log-diff.txt` records the exact diff, the touched-file inventory, and the machine-verified facts (`0` occurrences of `= 13;`, `29` `public const int` declarations, `0` `AddModuleSchemaMigrator` occurrences outside doc comments in `GeographyModule`, `0` changed files under `Program.cs`/`Development`).

## 5. Configured metadata vs applied schema (explicit distinction)

| Layer | State after 045 |
| --- | --- |
| Canonical order constant `Geography = 30` | **CONFIGURED** (unique, append-only, unused by any registration) |
| Production runner descriptor (`status` / `plan` / `apply`) | **CONFIGURED** — Geography is now *visible* to the CLI on the next explicit invocation |
| `geography` schema rows in any Tooba database | **NOT APPLIED** — no migration ran, no database was contacted |
| Host startup migration of Geography | **STILL IMPOSSIBLE** — no `IModuleSchemaMigrator` is registered by the module |
| Shipping authority (legacy `StorefrontIranGeography`, serviceability, Checkout, Order snapshots) | **UNCHANGED** |
| ARCH-COMPLETE-002 / manifest | **NOT CLAIMED, NOT TOUCHED** |

Consequence to be explicit about: the next explicit `Tooba.MigrationRunner apply` for a tenant **would now create the geography schema** in that tenant database. Nothing does that automatically, and this Task did not do it, but the operator command path is now armed. That is the intended, Architect-authorized meaning of this slice.

## 6. Time, commit, push

- Claim `12:23:29Z` → validated push `12:28:41Z`: **≈5 minutes** of the 30-minute hard timebox.
- One bounded commit of task-owned paths only: `c316c7f46d2ba55226bdcc8dc5157833bda82263` (order constant + runner descriptor + report + evidence). `git push origin main`; `git fetch origin`; `HEAD == origin/main` verified.
- No force-push, no history rewrite, no unrelated staging, no `MigrationRunner apply`, no SQL, no Tooba database connection, no Host restart, no Results-API probe, no automatic next task, no idle polling.

## 7. Residual risks and the single next recommendation

1. **Armed operator path.** The runner can now apply the geography schema to whatever target an operator resolves. That is deliberate and Architect-authorized — but it means the remaining go/no-go is now a *policy* decision, not a code gap.
2. **Still not registered in Host.** `GeographyModule` intentionally has no migrator registration, so the Development startup path cannot create the schema; a development tenant that a future consumer needs will require either the activation slice (044 §8 step 2) or an explicit runner `apply`.
3. **Root ordering is authoritative, order 30 is not yet consumed.** The constant exists but no module references it. If the Architect later authorizes the Host activation slice, the constant and the module line must land together or the composite collides (`13` stays retired; `30` stays reserved for Geography).
4. **Integration test not re-run** (Docker-dependent, out of scope). Its assertions were reviewed and are not count-pinned; the Architect may want one Docker-backed `MigrationRunner status/plan/apply` run before any real deployment.

**One recommended next slice:** a Docker-backed, disposable-container `MigrationRunner plan` + `apply` verification against a scratch database (never a Tooba database) that proves the appended descriptor applies exactly `20261014113949_InitialGeography`, reports zero pending afterwards, and leaves all pre-existing modules' history untouched — with no Host change and no consumer switch. Only after that evidence exists should the Architect decide whether to authorize the 044 §8 Host activation slice.
