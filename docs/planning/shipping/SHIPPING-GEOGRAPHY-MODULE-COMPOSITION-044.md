# Shipping — Geography Module Composition Report (`TB-SHIPPING-GEOGRAPHY-MODULE-COMPOSITION-044`)

> **STATUS: PARTIAL — module-owned DI composition is ACTIVE (Host-wired, migration-safe, verified); the schema-migration hook is PREPARED-ONLY (deliberately not registered) because any registration would implicitly apply `geography` DDL at Development startup, and the authoritative order constant lives outside this Task's allowed paths.**
> No migration generated/edited/applied, no Tooba database touched, no Host restart, no endpoint, no consumer or shipping-policy change, no certification claimed.

| Field | Value |
| --- | --- |
| Repository / Branch | `mrnikiemami-code/Tooba` / `main` |
| Expected-Base-SHA | `85509d7aa00535d18968099d4d961d5a81c9af61` |
| Base-SHA (verified `HEAD == origin/main`) | `85509d7aa00535d18968099d4d961d5a81c9af61` |
| Parent task | `TB-SHIPPING-GEOGRAPHY-READ-FOUNDATION-043` |
| Mode | `BOUNDED_GEOGRAPHY_MODULE_COMPOSITION_WITHOUT_RUNTIME_MIGRATION` |
| Status | `PARTIAL` (A PASS · B prepared-only) |
| Stop-State | `STOP_FOR_ARCHITECT` |
| automaticNextImplementationTask | `NONE` |

## 1. Activation state (the distinction this Task had to preserve)

| Concern | State | Evidence |
| --- | --- | --- |
| Module composition root `GeographyModule` (`IToobaModule`) | **ACTIVE** | new `DependencyInjection/GeographyModule.cs`; `Name == "Geography"` verified at runtime (check `C1`) |
| `GeographyDbContext` registration (module-owned `geography` schema) | **ACTIVE** | resolves scoped, `Model.GetDefaultSchema() == "geography"`, 9 tables (checks `C4`, `D4`, `D5`) |
| `IGeographyLookup` → `GeographyPlaceLookup` | **ACTIVE** | exactly one scoped registration; resolves (checks `C2`, `D1`) |
| `IGeographyLabelLookup` → `GeographyLabelLookup` | **ACTIVE** | exactly one scoped registration; resolves through the Localization Contracts seam (checks `C3`, `D2`) |
| Host composition entry (`ToobaModuleComposition.Modules`) | **ACTIVE** | `new GeographyModule()` added; Host compiles |
| Schema migrator registration (`AddModuleSchemaMigrator<GeographyDbContext>`) | **PREPARED-ONLY — NOT REGISTERED** | proven absent: `IModuleSchemaMigrator` count from this module is `0` (check `C5`) |
| `ModuleSchemaMigrationOrder.Geography` constant | **NOT ADDED** | file `src/backend/BuildingBlocks/Tooba.Persistence/ModuleSchemaMigrator.cs` is outside the allowed paths and explicitly held for separate approval |
| `Tooba.MigrationRunner.ModuleMigrationRegistry` (production apply path) | **NOT TOUCHED** | 0 `Geography` hits (evidence log) |
| Structure manifest / ARCH-COMPLETE-002 certification | **NOT CLAIMED** | 0 `"module": "Geography"` manifest entries (evidence log) |
| Outbox worker / hosted service / endpoint / seed | **NONE** | checks `C6`, `C7`, `C8` |
| Shipping authority (legacy `StorefrontIranGeography`, serviceability, coverage, Checkout) | **UNCHANGED** | no consumer file touched; diff is 6 inserted lines + 1 new file |

## 2. Preflight

- Claimed exactly one Pending item on `tooba-main` after `BRIDGE-WAKE` (row `00a3bb8e-9a0f-4c25-8d7f-dde618ee7783`); the received artifact was persisted verbatim to `docs/ai/tasks/TB-SHIPPING-GEOGRAPHY-MODULE-COMPOSITION-044.task.md` (receipt only, unstaged, never replayed).
- `git fetch origin` → `HEAD == origin/main == Expected-Base-SHA == 85509d7aa00535d18968099d4d961d5a81c9af61`; no reset/stash/rebase/force-push; all unrelated dirty/untracked work preserved.
- Read `AGENTS.md`, `.cursor/skills/tooba-backend-feature/SKILL.md`, ADR-036, report 043, the real migration-order source, `DevelopmentSchemaMigrator`, `ToobaModuleComposition`, the MigrationRunner registry and the composition guard tests, plus the established module roots (`LocalizationModule`, `CartModule`, `StoreContextModule`). Findings register not consulted.
- Collision check: no `DependencyInjection/` folder existed under Geography, no report 044, no evidence folder 044. **Convention check:** the newest certified module roots place the composition entry under `DependencyInjection/` and require an empty Infrastructure root (StoreContext guard) - followed exactly.

## 3. Changed paths (exactly four)

| Path | Change |
| --- | --- |
| `src/backend/Modules/Geography/Tooba.Geography.Infrastructure/DependencyInjection/GeographyModule.cs` | **New** module composition root: `IToobaModule`, one scoped `GeographyDbContext` (canonical `ToobaNpgsql.ResolveForContext` + `ConfigureModuleContext`), the two read seams. No migrator, no hosted service, no outbox registration. |
| `.../Tooba.Geography.Infrastructure/Tooba.Geography.Infrastructure.csproj` | +1 `ProjectReference` to `Tooba.ModuleContracts` (required by `IToobaModule`). |
| `src/backend/Host/Tooba.Host/Tooba.Host.csproj` | +1 `ProjectReference` to `Tooba.Geography.Infrastructure` (composition only, with the reason in a comment). |
| `src/backend/Host/Tooba.Host/Composition/ToobaModuleComposition.cs` | +1 `using`, +1 `new GeographyModule(),` in the explicit module list. |

Diff size: **6 inserted lines** across the three edited files, plus the new 90-line module file. Explicitly **not** touched: any migration source/Designer/snapshot, `GeographyDbContext`, Domain/Contracts, `ModuleSchemaMigrator.cs` (`ModuleSchemaMigrationOrder`), `ModuleMigrationRegistry.cs`, `Program.cs`, `DevelopmentSchemaMigrator.cs`, `Tooba.slnx`, every guard test, every Order/AddressBook/Fulfillment/Checkout file, `appsettings*`.

## 4. Migration safety (why Host wiring is safe and why the migrator is not)

**Proof that registration would auto-apply DDL at startup** (`log-migration-order-map.txt`):

```text
Program.cs:361: await DevelopmentSchemaMigrator.ApplyAsync(app.Services);
Program.cs:368: await DevelopmentSchemaMigrator.MigrateSchemaOnlyAsync(app.Services);
DevelopmentSchemaMigrator.cs:121: var migrators = provider.GetServices<IModuleSchemaMigrator>()
DevelopmentSchemaMigrator.cs:139: await migrator.MigrateAsync(cancellationToken);
```

The Development startup path resolves **every** registered `IModuleSchemaMigrator` and calls `MigrateAsync`, which for `EfModuleSchemaMigrator<TContext>` is `Database.MigrateAsync()`. Registering `AddModuleSchemaMigrator<GeographyDbContext>` inside a Host-loaded module would therefore emit `CREATE SCHEMA/TABLE/TRIGGER` against the Development tenant database on the next start - an implicit, unauthorized migration. That is why it is absent.

**Proof that the current Host wiring cannot do that** (runtime, not by inspection): the module's own registration set contains **zero** `IModuleSchemaMigrator`, zero `IModuleSchemaMigrationStep`, zero `IHostedService`, zero outbox registration (checks `C5`-`C8`). Nothing else in Host enumerates DbContexts - readiness evaluates configuration/messaging only (`HostReadinessEvaluator`), and the outbox dispatcher enumerates only `IOutboxModuleRegistration`. Therefore the only DDL path is structurally unreachable for Geography in the current build.

**Why the canonical slot could not be written:** the single authoritative order source is `ModuleSchemaMigrationOrder` inside `src/backend/BuildingBlocks/Tooba.Persistence/ModuleSchemaMigrator.cs` - not an allowed path in this Task, and the Task explicitly holds changes to `ModuleSchemaMigrationOrder`/the migrator for separate approval. The used values are `1-12` and `14-29` (13 free since `Reviews=14`), so an append-safe slot exists (**30**), but inventing a literal order number inside the Geography module would create the second contradictory ordering model that this seam exists to prevent.

## 5. Focused checks

**DI construction verification without service startup or database contact** — `log-di-composition.txt`, 15/15 PASS (harness kept at `harness/`, removed from temp afterwards):

| Check | Result |
| --- | --- |
| `C1` module `Name == "Geography"` | PASS |
| `C2`/`C3` exactly one scoped registration per lookup, correct implementation types | PASS |
| `C4` exactly one scoped `GeographyDbContext` | PASS |
| `C5` zero `IModuleSchemaMigrator` from this module (no implicit DDL path) | PASS |
| `C6` zero `IOutboxModuleRegistration`; `C7` zero `IHostedService`; `C8` zero `IModuleSchemaMigrationStep` | PASS |
| `D1`/`D2` both lookups resolve to the real adapters | PASS |
| `D3` context resolves against an **unreachable** connection string (`Port=1`) with no exception → no database contact | PASS |
| `D4`/`D5` schema `geography`, 9 mapped tables (matches the 042 initial migration) | PASS |
| `D6` lookups and context share one scoped instance | PASS |
| `D7` `BuildServiceProvider` with `ValidateOnBuild` + `ValidateScopes` (the Development defaults) succeeds → the resolved graph is complete | PASS |

**Builds** — `Tooba.Geography.Infrastructure` builds clean; **`Tooba.Host` compiles with the new module wired** (`Build succeeded, 0 Error(s)`).

**Direct-build note (honest disclosure):** the running Development Host (PID 37028) holds its `bin/Debug/net8.0` assemblies, so an in-place Host build cannot copy outputs (`MSB3027/MSB3021` file-lock errors). Because restarting the Host is forbidden, the compile was verified with a redirected output directory (`-p:OutDir=<temp>`), which compiles and copies the entire Host graph **without touching the running process**; the temp directory was then deleted. The running Host was never stopped, restarted, or otherwise affected.

**Narrow architecture guards reviewed against the change** (text-level, no guard modified): `HostDevelopmentMigrationSeamGuardTests` counts registrations over a fixed 28-path list that does not contain Geography → the `Assert.Equal(28, registrations)` count is unaffected; `HostCompositionAmcGuardTests` asserts the Composition folder file set (unchanged: still the 5 allowlisted files) and forbids `MapGet(`/`Database.MigrateAsync` in the composition file (still absent); `ArchitectureBoundaryTests` uses `Assert.Contains` on `Modules` (additive change is compatible); `PlatformProbe*` guards assert absence of PlatformProbe only. Precedent for a Host-wired module without any schema/migrator: `ProductWorkspaceModule` (its cert guard asserts exactly that: "the module owns no schema and registers no migration").

## 6. Certification and structure gaps (reported, not invented)

- **No ARCH-COMPLETE-002 claim.** Geography has **no Application project and no Endpoints project**, and no manifest entry (`tmar-module-structure-manifests.json`: 0 hits). Whether a read-only reference module needs an Application/Endpoints project at all, or should follow the certified `StoreContext` `INTERNAL_ONLY` / `PLATFORM_CONTEXT_REFERENCE_PATTERN` precedent, is an Architect decision - not made here, and no guard was weakened to imply otherwise.
- **`Tooba.MigrationRunner.ModuleMigrationRegistry`** (production `status/plan/apply`) has no Geography descriptor, so production migration cannot see the module yet. The file is outside this Task's allowed paths.
- The structure manifest, SoT (`tmar-current-state.json`) and the fixed guard module list are all untouched by design.

## 7. Risks of the state as delivered

1. **Wired but unmigrated.** After the next authorized Host start, the Geography module will be present in the DI graph while the `geography` schema still does not exist on any Tooba database. Any future consumer that resolves a Geography lookup before the migration slice lands would get a PostgreSQL "schema does not exist" failure. Nothing resolves these seams today, so the current build is inert; the report states this explicitly rather than implying the module is usable.
2. **Activation is a two-place change.** The migrator line alone is not enough: the order constant must exist first, and the production runner registry must be extended for real deployments. Both are listed below.
3. **Guard bookkeeping.** If the Architect wants Geography in the migration-seam guard list, that is a separate, deliberate guard edit (`ModuleCompositionRoots` + the `28` expectation) - not performed here.

## 8. Exact activation plan (blocked steps, ready to apply)

1. `src/backend/BuildingBlocks/Tooba.Persistence/ModuleSchemaMigrator.cs`: add `public const int Geography = 30;` (append-safe; existing values are `1-12`, `14-29`; no renumbering).
2. `GeographyModule.AddServices`: add `services.AddModuleSchemaMigrator<GeographyDbContext>("Geography", ModuleSchemaMigrationOrder.Geography);` (place it before `AddDbContext`, matching `LocalizationModule`).
3. `src/backend/Host/Tooba.MigrationRunner/ModuleMigrationRegistry.cs`: append `Descriptor<GeographyDbContext>("Geography", GeographyDbContext.Schema)` for the production `status/plan/apply` path.
4. Decide explicitly whether step 2 may run against the **Development tenant** at startup (it will) and whether the `geography` DDL is authorized there - this is the actual go/no-go for activation.
5. Optional bookkeeping in the same approved slice: extend `HostDevelopmentMigrationSeamGuardTests.ModuleCompositionRoots` to 29 entries and update the `28` expectation; add the Geography structure manifest entry.

## 9. Time, commit, push

- Claim `12:13:35Z` → validated push: see the Result block (≈22 minutes of the 30-minute hard timebox).
- Task-owned changes only; `git push origin main`; `git fetch origin`; `HEAD == origin/main` verified. No force-push, no history rewrite, no unrelated staging.
- Temp verification project and redirected build output both deleted; the running Development Host (PID 37028) was never stopped or restarted; no Tooba database was connected to; no Results-API probe; no automatic next task.

## 10. Recommendation (exactly one next step)

Authorize **one activation slice** that first adds `ModuleSchemaMigrationOrder.Geography = 30` and the module migrator registration, adds the MigrationRunner descriptor, and *explicitly* rules on whether the `geography` DDL may be applied to the Development tenant at startup - still without switching any shipping/Checkout consumer to the new module. Until that ruling exists, Geography must stay exactly as delivered: Host-wired for resolvability, migration-free, and not a shipping authority.
