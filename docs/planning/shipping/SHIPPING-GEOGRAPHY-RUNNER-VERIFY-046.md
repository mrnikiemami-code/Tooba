# TB-SHIPPING-GEOGRAPHY-RUNNER-VERIFY-046

Bounded disposable verification of the Geography migration metadata end to end (production
`Tooba.MigrationRunner`, EF tools and an ordered metadata/invariant proof) inside a
task-exclusive disposable PostgreSQL. No Tooba database was contacted and no production source
was changed.

- Parent task: `TB-SHIPPING-GEOGRAPHY-MIGRATION-REGISTRY-045`
- Expected base SHA: `6746ca5ae835fa96ef67b99d0ba94095c782b2c2`
- Status: **PASS** (worker). Worker PASS != Architect ACCEPT.
- Stop-State: `STOP_FOR_ARCHITECT`
- Actual elapsed: ~8 minutes of the 30-minute hard timebox (claimed 2026-10-14T12:33:35Z, evidence closed 2026-10-14T12:41Z).

## 1. Isolation provenance (proven before any DDL)

| Fact | Value |
| --- | --- |
| Container | `geo046-verify` (created `2026-10-14T12:35:52Z` by this task) |
| Image | `postgres:17` (server `PostgreSQL 17.10`) |
| Binding | `127.0.0.1:55433 -> 5432/tcp` — loopback only, distinct from the Tooba container's `5432` |
| Storage | task-created anonymous docker volume (removed at the end) |
| Databases | `geo046_scratch` (+ `postgres`, `template0/1`) only |
| Tooba containers | `postgres-db` (0.0.0.0:5432) never connected to, never restarted (`Up 3 days`) |

The runner was executed against a **fresh configuration that cannot name a Tooba database**:

- `ASPNETCORE_ENVIRONMENT=geo046verify`, for which no `appsettings.geo046verify.json` exists, and the
  `appsettings.Development.json` / `appsettings.Production.json` copies were **deleted from the
  isolated build output** before the runner ran (log: `log-runner-config-guard.txt`). The real Tooba
  connection references (`127.0.0.1:5432 tooba_alpha/marketplace`) were therefore never bindable.
- The only connection reference supplied was
  `Tooba__PostgreSQL__ConnectionReferences__geo046scratch=Host=127.0.0.1;Port=55433;Database=geo046_scratch`
  on a synthetic `SingleStore` registry with the single tenant `geo046`.
- `docker run` needed a second attempt: the first candidate port (`55946`) is inside the Windows
  excluded range `55941-56040`; `55433` is outside every excluded range.

## 2. Runner CLI reality (A) — no module-scoped apply exists

Read from the real sources, not assumed:

- `MigrationRunnerCli.TryParse` accepts only `<status|plan|apply>`, `--tenant`, `--tenants`,
  `--all-tenants`. `status --module Geography` is rejected: `Unknown argument '--module'.` (exit 3).
  No-argument invocation prints the same four options (exit 3).
- `MigrationTargetResolver.Resolve` selects **targets (edition/tenant/database)**, never modules; a
  `SingleStore` run without `--tenant`/`--tenants`/`--all-tenants` fails closed (exit 3).
- `MigrationOrchestrator.RunAsync(target, command)` iterates the whole `ModuleMigrationRegistry.All`
  for the resolved target. Therefore **`apply` always acts on every registered descriptor of that
  database**; `--tenant geo046` is what confined the blast radius here, and the disposable database is
  what made it authorized.
- `ModuleMigrationRegistry.All` has 32 descriptors; the `Geography` descriptor exists **exactly once**
  and is the **last** entry (index 31), schema `geography`.
- `ModuleSchemaMigrationOrder.Geography = 30` with no duplicate slot anywhere in the class.

## 3. Runner status / plan (non-mutating)

`log-runner-status-plan.txt` / `.compact.txt`:

- `status --tenant geo046` (exit 0): `Geography` appears **once**, `currentMigration: null`,
  `pendingMigrations: ["20261014113949_InitialGeography"]`, `succeeded: true`.
- `plan --tenant geo046` (exit 0): same single Geography entry.
- Non-mutation proof: the disposable database contained only the `public` schema **before and after**
  `status` + `plan` (33 non-system schemas appear only after `apply`), i.e. no DDL was emitted.

## 4. Runner apply inside the disposable database only (C)

`log-runner-apply.txt` — **all 32 descriptors applied to `geo046_scratch` on 127.0.0.1:55433, and to
nothing else**, because the single resolved target's connection string is the only connection the
orchestrator ever opens (`Program.cs` resolves one `connectionString` per resolved target).

- `apply --tenant geo046` → exit `0`, wall clock **4.1 s** (bounded, no long-tail).
- 0 occurrences of `"succeeded": false`; 0 remaining `pendingMigrations` entries across all 32 modules.
- Post-apply ``status``: `Geography` once, `currentMigration: "20261014113949_InitialGeography"`,
  `pendingMigrations: []`, `succeeded: true`.

Database objects after apply (SQL proof, section 5 log):

- schema `geography` exists; **10** tables = 9 domain tables (`countries`, `administrative_areas`,
  `cities`, `service_areas`, `outbox_messages` + 4 translation tables) + `__ef_migrations_history`;
- history row: `20261014113949_InitialGeography`;
- ancestry guard: function `administrative_areas_ancestry_guard` + both triggers
  `trg_administrative_areas_ancestry_guard_insert`, `trg_administrative_areas_ancestry_guard_update`;
- **9** foreign keys, of which **5** cascade (the four translation tables + `service_areas -> cities`);
- alternate key `ak_administrative_areas_id_country` present (the composite-FK target);
- **19** unique indexes; `ix_outbox_messages_pending` partial index present.

## 5. Critical invariants on the disposable database (B, reusing the focused 042 validations)

`log-scenarios.txt` + `log-isolation.txt` (script committed as `scenarios.sql`):

| Check | Expected | Observed |
| --- | --- | --- |
| P1 fixture (2 countries, 2 areas, 1 city, 1 service area) | accepted | accepted |
| N1 area whose parent lives in another country | `23503` | `23503` |
| N2 self-parent | `23514` | `23514` |
| N3 two-row `INSERT` closing a cycle A→B, B→A (the **042 amendment** case) | `23514` | `23514` |
| N4 re-parenting a root under its own descendant (`UPDATE` trigger) | `23514` | `23514` |
| N5 stored graph unchanged after the rejected writes | root parent `null`, 2 areas | root parent `null`, 2 areas |
| N6 city pointing at another country's area | `23503` | `23503` |
| N7 service area with a nonexistent city | `23503` | `23503` |
| N8 duplicate `(place, language)` translation | `23505` | `23505` |
| N9 deleting a country that still has areas (Restrict) | `23503` | `23503` |
| N10 ancestry write in `REPEATABLE READ` (isolation fail-closed) | `0A000` | `0A000` |
| P2 valid child area / P3 localized country name | accepted | accepted |

The immediate, non-deferrable triggers are therefore live in the migrated schema, and the ancestry
guard still refuses to run outside `READ COMMITTED` in the real database, not only in test doubles.

## 6. UP / DOWN / UP with the standard EF tools (B)

`log-down-up.txt`:

- `dotnet ef migrations list` → exactly one migration: `20261014113949_InitialGeography`.
- `dotnet ef database update 0` → exit `0`, **1.5 s**: *"Reverting migration
  '20261014113949_InitialGeography'. Done."*
  After down: geography tables `10 -> 1` (the only residue is the EF history table), ancestry triggers
  `2 -> 0`, guard function `1 -> 0`, history rows `1 -> 0`. All **33** non-system schemas of the other
  32 modules were byte-identical before and after, so the down path is Geography-scoped.
  Honest detail: EF keeps the `geography` schema itself plus the empty `__ef_migrations_history`
  table — `Down` drops tables, not the schema; a "schema fully absent" claim would be false.
- `dotnet ef database update` → exit `0`: geography tables `1 -> 10`, triggers `0 -> 2`, history row
  restored.
- Cross-tool agreement: after this EF-driven UP, the **runner** reported
  `current=20261014113949_InitialGeography pendingCount=0` for the same database — the runner
  descriptor and the EF migration assembly describe the same single migration.

## 7. Host non-activation (A) — 8/8 PASS

`log-host-nonactivation.txt`, produced by an external reflection harness run against **freshly built**
output of the current source (build redirected to an isolated output directory; the running Host was
never rebuilt or restarted):

- **H1** `Tooba.Host` references 153 assemblies; `Tooba.MigrationRunner` is **not** one of them.
- **H2/H3** runner descriptors = 32; `geography` descriptors = 1, at index 31 (last slot).
- **H4** `ModuleSchemaMigrationOrder.Geography = 30`; no duplicate slot groups in the class.
- **H5** Host composition lists 34 modules with exactly one `Geography` (last).
- **H6** `GeographyModule.AddServices` registers **0** `IModuleSchemaMigrator` and 2 geography lookups
  (`IGeographyLookup`, `IGeographyLabelLookup`) — startup therefore executes **no** geography DDL.
- **H7** `GeographyDbContext` advertises exactly `20261014113949_InitialGeography`.
- **H8** `GeographyDbContext.Schema == "geography"` and the runner descriptor advertises the same schema.

## 8. Cleanup

`log-provenance-cleanup.txt`: container `geo046-verify` removed, its anonymous volume
`c552498d…feef` removed, residual `geo046` containers/volumes = `[]`, the external temporary harness
and runner build directories deleted, the generated password file deleted. `postgres-db` (Tooba dev)
remained `Up 3 days` throughout; the running Host process (`PID 37028`, started `2026-10-13 18:11`)
was never restarted.

## 9. Remaining activation blockers (observed)

1. **No module-scoped runner invocation.** `apply` always targets every registered descriptor of the
   resolved database. Running it against a real Tooba tenant database is therefore an
   all-modules migration, not a Geography-only one — the exact authorization that has not been given.
2. **Host startup runs no geography DDL.** `Tooba.Host` has no reference to `Tooba.MigrationRunner`
   and `GeographyModule` registers no `IModuleSchemaMigrator`, so the `geography` schema must be
   created out of band (runner/EF) before any runtime Geography read can succeed.
3. **The running Host binary predates the composition work.** The `GeographyModule` composition and
   the order-30 constant are source-only; the live Host process started 2026-10-13, so runtime
   `IGeographyLookup` DI does not exist in the running process. A Host restart/redeploy would be
   required and is not authorized.
4. **No data.** The migrated schema is empty; there is no geography seed and no wired read path, so
   consumers would observe an empty reference dataset.
5. **Editions/tenants not exercised here.** Only `SingleStore` with one synthetic tenant was verified;
   `--all-tenants` fan-out and `Marketplace` targets were not run.

## 10. Recommended next bounded task (one)

`TB-SHIPPING-GEOGRAPHY-DEPLOY-AUTHORIZATION-BOUNDARY-047` — define, without executing it, the
authorized deployment boundary for activating Geography on the real Development tenant database:
who may run `MigrationRunner apply` given that the CLI cannot filter modules, the exact ordered
commands and pre/post verification queries, the rollback statement set, and the explicit Architect
authorization record required. No Tooba database contact inside that task either.
