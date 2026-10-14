# TB-SHIPPING-MVP-PERSISTENCE-052 — first real Shipping migration (review-only, never applied to Tooba)

- Task: `TB-SHIPPING-MVP-PERSISTENCE-052` (Program `TOOBA-SHIPPING-MVP-SIX-TASK-CLOSURE`, milestone 3 of 6)
- Parent: `TB-SHIPPING-MVP-ADMIN-ROUNDTRIP-051`
- Mode: `BOUNDED_SHIPPING_PERSISTENCE_MIGRATION_NO_DEPLOY`
- Pipeline: `BRIDGE-WAKE-V1` / channel `tooba-main` / worker `tooba-worker-01`
- Expected-Base-SHA: `446a3982e99373d3bb85ab652be5c69583493dd5`
- Stop-State: `STOP_FOR_ARCHITECT`

## 1. Baseline

`HEAD == origin/main == 98d8ec4ab…`. Expected-Base is its parent; the single intervening commit
(`98d8ec4ab`) is the parent task's own evidence-SHA bookkeeping → harmless fast-forward. All pre-existing
unrelated dirty/untracked files preserved; no reset/stash/rebase/force-push.

## 2. What changed and why

048 left `ShippingDbContext` as an inert mapping: no migration, no constraints beyond PKs and two plain
unique indexes. The rate/coverage feature cannot become operational while its storage is unenforceable,
because PostgreSQL's default `NULLS DISTINCT` **silently defeats the intended uniqueness** — two
country-scope policies for the same service both insert, and the engine then answers `ScopeConflict`
for every request. This task turns the mapping into a **real, reviewable persistence model**, without
wiring or applying anything.

### Storage invariants now enforced by the database

| Invariant (aggregate guard) | Mechanism |
| --- | --- |
| Stable, application-assigned PKs (`UuidV7` / replay-deterministic) | `ValueGeneratedNever` on both aggregates → `pk_rate_policies`, `pk_coverage_grants` |
| Required service code / currency / amount / country / days / active | `NOT NULL` columns |
| `ChargeAmount ∈ [0, 1_000_000_000]` | `ck_rate_policies_charge_amount_non_negative`, `ck_rate_policies_charge_amount_within_max` |
| `FreeAboveThreshold` null or ≥ 0 | `ck_rate_policies_free_above_threshold_non_negative` |
| `PreparationDays`, `LeadDays ∈ [0, 365]` | `ck_rate_policies_preparation_days_range`, `ck_rate_policies_lead_days_range` |
| Canonical, trimmed, non-empty service code (the engine matches trimmed codes) | `ck_rate_policies_service_code_canonical`, `ck_coverage_grants_service_code_canonical` |
| Canonical 3-letter upper-case currency | `ck_rate_policies_currency_code_canonical` |
| No half-scope (`service_area` without its `city`) | `ck_rate_policies_scope_coherent`, `ck_coverage_grants_scope_coherent` |
| **True** one-policy / one-grant per (service, exact scope) | `ux_rate_policies_service_scope`, `ux_coverage_grants_service_scope` **with `NULLS NOT DISTINCT`** |
| Quote lookup by service (+country) + active | `ix_rate_policies_service_active`, `ix_coverage_grants_service_country_active`, `ix_outbox_messages_pending` |

The `NULLS NOT DISTINCT` flag is the load-bearing part: without it the unique index tolerates any number
of same-scope rows that differ only in a NULL level. It requires **PostgreSQL 15+** (this task verified on
**17.10**, the same major as the project's Development container). EF Core 8.0.11 exposes it as
`HasIndex(...).IsUnique().AreNullsDistinct(false)`, which emits `Npgsql:NullsDistinct=false` into the
snapshot and the literal `NULLS NOT DISTINCT` into the DDL. On an older server the migration fails loudly
instead of degrading to a partial constraint — documented rather than papered over.

Nothing was invented: every predicate is the storage mirror of an existing 048 guard
(`NormalizeCharge`, `NormalizeDays`, `NormalizeCurrency`, `NormalizeServiceCode`, `EnsureScopeIsCoherent`).
The pure fail-closed engine was **not touched**, no default rate/grant was seeded, and the legacy
`AllowedProvinces`-means-unrestricted semantics in the storefront path are untouched (so no coverage is
inferred from it).

### Boundaries deliberately kept

- **No cross-module FK.** Destination and service identity stay as plain IDs (0 foreign keys in the
  `shipping` schema, asserted in both the test and the DB probe) because Geography/Fulfillment owner
  tables are outside this module.
- **No migration runner hook.** Still no `IModuleSchemaMigrator` in the Shipping assembly (asserted by a
  test), so no running process applies this schema.
- **The module stays unwired** from Host/`ToobaModuleComposition`.

## 3. Artifacts produced

```text
A src/backend/Modules/Shipping/Tooba.Shipping.Infrastructure/Persistence/Migrations/20261014144742_InitialShipping.cs
A src/backend/Modules/Shipping/Tooba.Shipping.Infrastructure/Persistence/Migrations/20261014144742_InitialShipping.Designer.cs
A src/backend/Modules/Shipping/Tooba.Shipping.Infrastructure/Persistence/Migrations/ShippingDbContextModelSnapshot.cs
M src/backend/Modules/Shipping/Tooba.Shipping.Infrastructure/Persistence/ShippingDbContext.cs
A src/backend/Modules/Shipping/Tooba.Shipping.Tests/Persistence/ShippingPersistenceModelTests.cs
```

`ShippingDbContext.cs` is the only modified production file; `Tooba.Shipping.Infrastructure.csproj` already
referenced `Microsoft.EntityFrameworkCore.Design`, so **no csproj change was needed** and no design-time
factory change was needed (the 048 factory already reads `TOOBA_DESIGN_TIME_CONNECTION`).

## 4. Isolated database verification (never Tooba's data)

Destination: a task-exclusive disposable container `ship052-verify` (`postgres:17`, db `ship052`, host port
55433) created for this run and **removed at the end**. The runner refuses to start unless the connection
string points at 127.0.0.1:55433, and the container credential is supplied through the environment so no
credential value is committed. Runner: `run-isolated-verification.ps1`. Logs below.

### Lifecycle UP → DOWN → UP (`log-migration-lifecycle.txt`, `log-downtime-down-up.txt`)

- Before DDL: no `shipping` schema existed.
- UP: `Applying migration '20261014144742_InitialShipping'` → exactly one row in
  `shipping.__ef_migrations_history` (product version 8.0.11); 4 tables
  (`rate_policies`, `coverage_grants`, `outbox_messages`, `__ef_migrations_history`); 4 PKs; **10 CHECK
  constraints**; 9 indexes; `ux_*_service_scope` reported by `pg_index.indnullsnotdistinct = true`;
  **0 outbound foreign keys**.
- DOWN (`database update 0`): `Reverting migration` → only `__ef_migrations_history` remains,
  `residual_module_tables = 0`.
- UP again on the same database: schema/constraints/indexes identical after round-tripping,
  `applied_migrations = 1` (no duplicate history rows).
- Teardown: container removed; the only remaining containers are the three pre-existing ones.

### Constraint scenarios (`log-constraint-scenarios.txt`)

Accepted (4 policies, 3 grants survived, exactly the intended ones):

- country-scope policy with every deeper level `NULL` → `INSERT 0 1`
- city-scope policy for the same service → `INSERT 0 1`
- area-scope policy (`city_id` NULL) for the same service → `INSERT 0 1` (does not collide with either)
- a **different** service code at the same country scope → `INSERT 0 1`
- city-scope coverage grant beside the country-scope grant for the same service → `INSERT 0 1`
- a coverage grant whose service has no policy yet → `INSERT 0 1` (coverage and pricing are separate
  decisions, and fail-closed means absent-row-is-uncovered)

Rejected, each with the exact constraint name in the error:

| Scenario | SQLSTATE | Constraint |
| --- | --- | --- |
| duplicate country-scope policy for the same service (all deeper levels NULL) | `23505` | `ux_rate_policies_service_scope` |
| duplicate country-scope coverage grant for the same service | `23505` | `ux_coverage_grants_service_scope` |
| negative charge | `23514` | `ck_rate_policies_charge_amount_non_negative` |
| charge above the model maximum | `23514` | `ck_rate_policies_charge_amount_within_max` |
| negative preparation days | `23514` | `ck_rate_policies_preparation_days_range` |
| lead days > 365 | `23514` | `ck_rate_policies_lead_days_range` |
| negative free-above threshold | `23514` | `ck_rate_policies_free_above_threshold_non_negative` |
| whitespace-padded service code (`' post '`) | `23514` | `ck_rate_policies_service_code_canonical` |
| two-letter currency code | `23514` | `ck_rate_policies_currency_code_canonical` |
| service area without its owning city | `23514` | `ck_rate_policies_scope_coherent` / `ck_coverage_grants_scope_coherent` |

The `23505` pair is the proof that `NULLS NOT DISTINCT` is doing real work: both rows had
`(post, <country>, null, null, null)`.

### No real database touched (`log-no-real-db-touch.txt`)

`tooba_alpha`, `MyDatabase`, `tooba_codex_dev` inside the pre-existing `postgres-db` container all report
`shipping_schema=0` and `rate_policies_or_coverage_grants=0` after the run. No DDL/DML/migration was ever
issued against them, the Host was neither restarted nor reconfigured, and the committed evidence contains
`0` credential values.

## 5. Focused tests (`log-shipping-tests.txt`)

```text
dotnet build src/backend/Modules/Shipping/Tooba.Shipping.Tests  -> 0 warnings, 0 errors
dotnet test  src/backend/Modules/Shipping/Tooba.Shipping.Tests  -> Passed! 58 passed, 0 failed
```

34 pre-existing tests remain green; the new `ShippingPersistenceModelTests` adds 24 cases. All are
**offline** (no PostgreSQL needed in CI): the model is read through `IDesignTimeModel` (the runtime model
is read-optimized and drops check constraints), the migration script is generated in memory, and the model
is checked against the snapshot with `HasPendingModelChanges()`. Deliberately, the tests assert the
generated **DDL text** for `NULLS NOT DISTINCT` and for each constraint name — a constraint that exists in
the model but does not reach the script is exactly the failure this task exists to prevent.

## 6. Build note

`dotnet build src/backend/Tooba.slnx` cannot complete while the Development Host is running: all 320 errors
are MSB3027/MSB3021 file-copy locks naming `Tooba.Host/bin/Debug/net8.0` (the live Host holds its own
output), with **zero** compiler diagnostics. The Task forbids restarting the Host, so it was left running
and the lock was not worked around. Because `Tooba.Shipping.Infrastructure` has exactly one consumer
(`Tooba.Shipping.Tests`, green) and the only external Shipping reference in the repository is
`Tooba.Order.Application` → `Tooba.Shipping.Contracts` (untouched, builds clean), the locked build cannot
be hiding a compile error here — see `log-solution-build-note.txt`.

## 7. Limitations / outstanding blockers

- The migration is **review-only**. Until the Architect authorizes Host wiring, `ShippingModule`
  registration, `ModuleSchemaMigrationOrder` insertion and an actual `database update` against the
  Development database, no Shipping rate or coverage can be stored, so 048's engine still has no data
  source and ship-time rates remain non-persisted.
- `NULLS NOT DISTINCT` requires PostgreSQL 15+. The project's Development container is 17.x (verified
  17.10 locally), so this is compatible, but a future deployment on an older server would need the
  alternative (a `COALESCE`-expression unique index or per-level partial indexes) instead.
- Coverage data entry is still absent by design: this task created the container for grants, not a single
  grant row, so the fail-closed default (uncovered) is preserved.
- Editing the model without regenerating the migration will now fail
  `Model_and_the_initial_migration_are_in_sync`, which is intended.
