# TB-SHIPPING-OPTION-COVERAGE-MIGRATION-162 — RESULT

Task: `TB-SHIPPING-OPTION-COVERAGE-MIGRATION-162`
Parent: `TB-SHIPPING-OWNER-OPTION-COVERAGE-161`
Channel: `tooba-main` · Worker: `tooba-worker-01` · Agent: `cursor`
Expected-Base-SHA: `37f71ccc7591adc5a68e62a901aa33eaf96111c6` (verified `HEAD == origin/main`)
Production Code-SHA: `PENDING_COMMIT` (one narrow `Down` fix — see §4)
Evidence-SHA: `PENDING_COMMIT`
Verdict: **PASS** — real isolated PostgreSQL 17.10 Up → Down → Up proven, with one reproduced
rollback defect found and narrowly fixed in Shipping-owned migration source.

---

## 1. Environment (isolation, task-owned)

| Item | Value |
|---|---|
| Container | `tooba-tb162-pg` — ID `f5b8e6203a8a…`, label `task=TB-SHIPPING-OPTION-COVERAGE-MIGRATION-162` |
| Image | `postgres:17` → server `PostgreSQL 17.10` |
| Host bind | `127.0.0.1:55432` → `5432/tcp` (loopback only, port verified free before start) |
| Database | `tb162_shipping_scratch` (task-unique) |
| Migration pathway | real `dotnet ef database update` against `ShippingDbContext` + `TOOBA_DESIGN_TIME_CONNECTION` (the repository's design-time factory → `ToobaNpgsql.ConfigureModuleContext`), i.e. the same EF/Npgsql configuration the module ships, not a hand-written DDL approximation |
| Shared services | `postgres-db` (:5432), `rabbitmq`, `:3000`, `:5088`, `tooba_alpha`, `store-alpha` — **not referenced, not touched, not restarted** |
| Guard | `isolation-guard-162.txt` (fail-closed verdict `PROCEED` before any DDL) |

The harness itself also re-asserts `current_database() = tb162_shipping_scratch` and exits `90` otherwise,
so no owner-code query can run against a non-scratch database even if the connection string were wrong.

## 2. Migration chain confirmed from source

```
20261014144742_InitialShipping
20261015030159_OptionLevelRatePolicy
20261015045130_OperatingSchedule
20261015063838_WeightAndCountRateTiers
20261015071836_ShippingPackagingPolicy            <- previous state used as the Down target
20261016211322_OptionScopedCoverageRestriction    <- under test
```

History table is `shipping.__ef_migrations_history` (snake_case, module schema) — read from the actual
`ConfigureModuleContext` call, not assumed.

## 3. Legs actually executed

### 3.1 Baseline (Up to previous state + legacy seed)

`dotnet ef database update 20261015071836_ShippingPackagingPolicy` applied the five pre-existing
migrations on the empty scratch DB. Then 5 legacy coverage grants + 3 rate policies were seeded
(`seed-legacy-162.sql`), deliberately spanning all four scope depths and both activation states:

| Grant | service | scope | active |
|---|---|---|---|
| `1111…` | tipax | country | yes |
| `2222…` | tipax | administrative area | yes |
| `3333…` | tipax | city X | yes |
| `4444…` | tipax | service area (city X) | **no** |
| `5555…` | tooba-post | country | yes |

Baseline facts: `option_code` column absent (count `0`), `shipping.method_coverages` absent
(`to_regclass` → null), 5 grants (4 active), 3 rates.

### 3.2 Up — `20261016211322_OptionScopedCoverageRestriction`

`dotnet ef database update` → `Applying migration '20261016211322_OptionScopedCoverageRestriction'. Done.`

Catalog proof (`schema-after-up-162.txt`):

* history has **6** rows, the new one last;
* `coverage_grants.option_code` = `character varying(64)`, `YES` nullable;
* `ck_coverage_grants_option_code_canonical` = `option_code IS NULL OR (char_length>0 AND = btrim(...))`;
* `ux_coverage_grants_service_option_scope` = UNIQUE over
  `(service_code, option_code, country_id, administrative_area_id, city_id, service_area_id) NULLS NOT DISTINCT`;
  the old `ux_coverage_grants_service_scope` is gone;
* `shipping.method_coverages` exists with `method_coverage_id, service_code, option_code, mode, created_at, updated_at`;
* `ck_method_coverages_mode_known` = `mode = ANY (ARRAY[0,1])`;
* `ux_method_coverages_service_option` = UNIQUE `(service_code, option_code) NULLS NOT DISTINCT`;
* **all 5 legacy grants still present with `option_code = NULL`** — no backfill to `standard`/`express`, no
  row loss, and all 3 rates keep their exact `charge_amount`/`currency_code` (no money change).

### 3.3 Upgraded-schema constraint + identity proofs (`db-constraint-and-identity-proof-162.txt`)

| Case | Expected | Actual |
|---|---|---|
| Duplicate service-level grant identity (same page scope, `option_code NULL`) | rejected | `23505 duplicate key … ux_coverage_grants_service_option_scope` (proves `NULLS NOT DISTINCT`) |
| `mode = 2` | rejected | `ck_method_coverages_mode_known` |
| `option_code = ' standard '` | rejected | `ck_method_coverages_option_code_canonical` |
| Two declarations, same service, different methods (`standard` Restricted, `express` Unrestricted) | accepted | `INSERT 0 2` |
| Exact duplicate declaration identity | rejected | `23505` on `ux_method_coverages_service_option` |
| Option-scoped grant at a scope that already has a service-level grant | accepted as a **distinct** row | `INSERT 0 1` (grant identity now includes the option) |

Observation (recorded, not a blocker): a declaration inserted by raw SQL with a differently-cased
`service_code` (`TIPAX`) is *not* rejected, because the unique index is case-sensitive. The application
write path always sends the route service code and `BelongsToMethod`/the directory duplicate guard compare
ordinally-ignore-case, so this is only reachable via raw SQL — the same pre-existing property as
`rate_policies`/`packaging_policies`. A functional `lower(...)` index would be a cross-cutting schema change
and was deliberately **not** made here.

### 3.4 Owner-code integration on the upgraded scratch schema

A throwaway console harness (outside the repo, `owner-code-harness-162.cs.txt`) referenced
`Tooba.Shipping.Infrastructure` and ran the **real** `ShippingRateQuoteLookup` + `ShippingRatePolicyEngine`
against the migrated scratch database. **16/16 assertions PASS** (`owner-code-quote-proof-162.txt`):

* `tipax:standard` Restricted with one own grant at city X → quoted `205000 IRR` at city X;
* same method in city Y → `NotCovered` / `shipping.coverage.method_restricted`, charge `0`, currency `null`;
* `tipax:express` Unrestricted → quoted `260000` in city Y **and** city X (never narrowed by the sibling's
  restriction, never required a grant);
* provider-level legacy grants do **not** authorize the restricted method;
* Restricted with **zero** own grants → denies everywhere;
* absent declaration → unrestricted *and* priced (owned rate `150000`) when the rate exists; with no rate at
  all → `shipping.service.not_priced` fail-closed;
* conditioned owned rate with no trusted measurement → `MeasurementRequired`; with weight `1500` → quoted `175000`;
* LegacyFallback still requires a **service-level** grant (`coverage.missing` when only an option-scoped
  grant exists), ignores declarations entirely, prefers option-specific rates, and still exhibits the
  documented TB-159 provider-rate leak for an unpriced option (`saver` → `185000`) while strict mode
  refuses the same request (`shipping.quote.owned_option_not_priced`, charge `0`, currency `null`).

### 3.5 Down — reproduced defect, fix, re-run

**First attempt failed** (recorded verbatim in §4): the generated `Down` could not drop
`option_code` because two rows that differ only by option collapse into the same identity under the
restored `(service, country, area, city, service_area)` unique index:

```
23505: could not create unique index "ux_coverage_grants_service_scope"
```

The failure aborted inside the migration transaction, so the database rolled back cleanly and stayed on the
Up state (verified before retrying). A Down that cannot run once option-scoped grants exist is a genuine
acceptance blocker for a real rollback, so a **narrow source fix** was made (see §4). After the fix:

`dotnet ef database update 20261015071836_ShippingPackagingPolicy` → `Reverting migration … Done.`

`schema-after-down-162.txt` proves:

* history back to **5** rows;
* `option_code` column count `0`, `shipping.method_coverages` → `to_regclass` null;
* `ux_coverage_grants_service_scope` restored (UNIQUE, `NULLS NOT DISTINCT`) and the wide index gone;
* **all 5 legacy rows preserved** with identical ids/scopes/activation (including the inactive
  service-area row);
* all 7 rate rows untouched (amounts unchanged);
* the 2 option-scoped grants created in §3.3 were discarded — the documented, non-lossless downgrade for
  new-format data (their `option_code` has no representation in the previous schema).

### 3.6 Re-apply (Up again)

`dotnet ef database update` → `Applying migration … Done.` (`reapply-proof-162.txt`):

* history 6 rows again, no collision, no EF drift (the model snapshot was never touched by the Down fix);
* the same 5 legacy grants remain `option_code = NULL` — legacy data survives a full cycle;
* `ux_coverage_grants_service_option_scope` and `ux_method_coverages_service_option` rebuilt identically;
* new-format rows are legitimately absent after the cycle (`0` declarations, `0` option-level grants).

## 4. The reproduced defect and the production diff

Symptom: `Down` of `20261016211322_OptionScopedCoverageRestriction` throws
`23505 could not create unique index "ux_coverage_grants_service_scope"` as soon as any option-scoped
grant exists for a scope that also has a service-level grant (the normal case after using the feature).

Fix (`production-diff-162.txt`), inside the Shipping-owned migration only, before the index is re-created:

```csharp
// Option-scoped grants have no representation in the previous schema: the only legal identity
// there is (service, destination). Promoting them to service-level would silently WIDEN coverage
// for every option of that service, and keeping them is impossible once option_code is dropped
// (two rows that differed only by option would collide on the restored unique index). Deleting
// exactly the option-scoped rows is therefore the honest downgrade; legacy service-level rows
// (option_code IS NULL) are never touched.
migrationBuilder.Sql(
    "DELETE FROM shipping.coverage_grants WHERE option_code IS NOT NULL;");
```

Why this and not something else:

* **Promoting** option-scoped rows to service-level would silently widen coverage for every option — an
  authority bug, strictly worse than data loss.
* **Keeping** them is impossible: the column is gone and the restored unique index rejects the collision.
* The `Down` already drops `shipping.method_coverages` (declarations are discarded), so deleting the
  option-scoped grants is consistent with the documented non-lossless downgrade.
* Legacy rows are untouched by construction (`WHERE option_code IS NOT NULL`).

No `Up`, designer, or model-snapshot change was made; the migration has never run against a shared or
production database, so editing it is safe.

Focused validation after the change: `ShippingPersistenceModelTests` + `ShippingMethodCoverageRestrictionTests`
+ `ShippingRateCoverageAdminTests` → **64 passed, 0 failed** (log §6).

## 5. Files

Production (1 file, migration only):

```
src/backend/Modules/Shipping/Tooba.Shipping.Infrastructure/Persistence/Migrations/20261016211322_OptionScopedCoverageRestriction.cs
```

Evidence (all sanitized; the scratch password appears nowhere):

```
docs/ai/tasks/TB-SHIPPING-OPTION-COVERAGE-MIGRATION-162.task.md
docs/architecture/evidence/TB-SHIPPING-OPTION-COVERAGE-MIGRATION-162/
  RESULT.md
  isolation-guard-162.txt
  commands-162.txt
  seed-legacy-162.sql
  seed-scenario-162.sql
  q-baseline.sql · q-after-up.sql · q-db-constraints.sql · q-pre-down.sql · q-post-failed-down.sql
  q-after-down.sql · q-after-reapply.sql
  schema-after-up-162.txt
  db-constraint-and-identity-proof-162.txt
  schema-after-down-162.txt
  reapply-proof-162.txt
  owner-code-quote-proof-162.txt
  owner-code-harness-162.cs.txt
  legacy-row-preservation-matrix-162.md
  ownership-assertion-matrix-162.md
  cleanup-proof-162.txt
  production-diff-162.txt
  test-runs-162.txt
  RESULT.bridge.txt
```

No Order/storefront, Admin UI, financial-mode, or unrelated Shipping source was modified. Pre-existing dirty
paths (`docs/architecture/evidence/TB-SHIPPING-FULL-OPERATIONS-075/-079/*.txt`, the Admin work-queue
frontend file) were left untouched and uncommitted.

## 6. Counts, timing, parity

| Item | Value |
|---|---|
| Owner-code assertions on real PostgreSQL | 16 passed / 0 failed |
| DB constraint & identity cases | 6 (4 rejections, 2 acceptances) as specified |
| Focused Shipping tests after the fix | 64 passed / 0 failed |
| Migration legs | Up (prev→head) ✔, Down (head→prev, after fix) ✔, Up again ✔ |
| Container / port cleanup | container removed by exact name, port `55432` free, harness dir deleted |
| Elapsed | ~26 min of the hard 45-minute box |
| Origin parity at report time | `HEAD == origin/main == <evidence commit>` |

## 7. Residual risk / next dependency

1. The pinned `Down` is a **destructive** downgrade for option-scoped rows; an operator should treat a
   rollback of this migration as "option-level coverage decisions are lost". This is now explicit in code.
2. Case-sensitive uniqueness of `service_code` (see §3.3 observation) is module-wide and pre-existing; if the
   Architect wants it hardened, it belongs in one cross-cutting task across `rate_policies`,
   `coverage_grants`, `method_coverages`, `packaging_policies`, `operating_schedules` — not here.
3. Next architectural dependency (not authorized here): Order's default-OFF, Shipping-owned
   `StrictOwnedOption` customer quote integration with trusted measurement / `MeasurementRequired`
   fail-closed and an authenticated isolated purchase-preview proof.
