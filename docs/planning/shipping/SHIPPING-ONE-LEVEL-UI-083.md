# SHIPPING-ONE-LEVEL-UI-083 — isolated migration proof and identity compatibility

Task: `TB-SHIPPING-ONE-LEVEL-TABS-083` (parent `TB-SHIPPING-ONE-LEVEL-TABS-082`)
Expected base SHA: `64ff456bf547dc5cdbdee8ffb60a397b25addf67`
Actual base SHA: `e85d2f9537cc5dd2179e49caa007cdc139c0d2e0`
Scope: prove the 082 one-level catalog in an isolated disposable PostgreSQL database; fix only
objectively demonstrated defects in the new source; no live write, no dual-write, no gate enable.

## 0. Preflight

`git fetch origin` → `HEAD == origin/main == e85d2f9537cc5dd2179e49caa007cdc139c0d2e0`.
The expected base `64ff456bf` **is** an ancestor of `HEAD`; the only drift between them is:

```
docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-082/RESULT.bridge.txt
docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-082/post-result.js
```

That is the 082 worker's own result envelope and Bridge delivery script — **nonoverlapping bookkeeping
drift**, not source drift. No `RECOVERY_CONFLICT`. All pre-existing dirty/untracked paths were preserved;
no reset, stash, rebase or force push.

## 1. Migration review (before any database interaction)

`20261015120000_AddShippingOneLevelCatalog` is raw SQL, matching the established module convention
(`20260910170000_AddShippingServiceCatalog` and `20260910120000_AddConsolidatedPackages` are also raw SQL).

Confirmed by inspection **and** then independently by the isolated database:

| expectation | isolated observation |
| --- | --- |
| 9 Fulfillment migrations, new one last in EF history order | `PENDING_MIGRATIONS 9` with `20261015120000_AddShippingOneLevelCatalog` last |
| `shipping_methods` carries all 9 expected columns | `columns=9` |
| `method_code` is `varchar(32)` (B2 width) | `method_code_width=32` |
| 2 unique + 1 plain index + 1 translation unique index | `indexes=4` |
| both canonical-code `CHECK` constraints exist | `checks=2` |
| legacy two-level catalog tables survive | `legacy_catalog_tables=4` |

EF history order is safe: the new migration only adds tables and never references a later migration.

### Stale `FulfillmentDbContextModelSnapshot` — investigated, no broad regeneration

The snapshot omits `shipping_services`, `shipping_service_options` and both translation tables (i.e. it
still describes only `consolidated_packages` / `consolidated_package_members`). The root cause is
architectural, not a 082 defect: the two shipping catalog migrations are **hand-written raw SQL with no
snapshot delta**, so the snapshot has been stale since `20260910170000`. It is therefore **pre-existing
debt**, it is deliberately **not** regenerated in 083 (that would be a broad, out-of-scope diff), and it
is independently proven **harmless at runtime**: `db.Database.MigrateAsync()` applied the full 9-migration
history and produced the exact expected schema, because the SQL migrations do not depend on the snapshot.
Documented limitation: the next *EF-generated* migration for this DbContext must be hand-authored.

## 2. Isolated real-database proof (primary mission)

Harness: `docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-083/probe-isolated-migration-083.cs`
(run from a gitignored `.tmp-onelevel083/` project so no harness file is committed to `src/`).
Full raw output: `log-isolated-migration-083.txt`.

**Provenance, positively confirmed before any DDL:**

```
ISOLATED_DB_CREATED tooba_onelevel083_ae5246cb
PROVENANCE current_db=1 foreign_schemas=0 preexisting_shipping_services=0
```

The target is a **brand-new database** (`CREATE DATABASE tooba_onelevel083_<hex>`): zero of the
`fulfillment`/`shipping`/`order`/`cart` schemas existed and no `shipping_services` table existed. The
credentials were read from the Development connection reference only to reach the local server; the
connection string points at the disposable database, never `tooba_alpha`.

### UP, constraints, reader

```
UP history_total=9 onelevel_applied=1 columns=9 method_code_width=32 indexes=4 checks=2 legacy_catalog_tables=4
INSERT representative_methods=4 translations=1 OK
```

Representative rows: `post:standard` (active), `tipax:standard` (active), `store_courier` (option-less,
active), `snapp_courier` (option-less, inactive) plus one Persian translation.

Negative cases — **7 of 8 correctly rejected by the real database**:

| probe | outcome |
| --- | --- |
| duplicate `method_code` | `23505 / ix_shipping_methods_method_code` |
| duplicate legacy `(service, option)` binding | `23505 / ux_shipping_methods_legacy_binding` |
| code longer than 32 | `22001` (value too long for `varchar(32)`) |
| upper-case code | `23514 / ck_shipping_methods_code_canonical` |
| hyphen separator | `23514 / ck_shipping_methods_code_canonical` |
| blank/whitespace option code | `23514 / ck_shipping_methods_option_code_canonical` |
| duplicate translation language | `23505 / ix_shipping_method_translations_method_lang` |
| translation pointing at a missing method | **not rejected** (before the fix) / `23503 / fk_shipping_method_translations_method` (after) |

After the §3 fix the probe reports `NEGATIVE_SUMMARY rejected=8/8`.

The pre-fix gap was a **verified finding**, not an assumption: `shipping_method_translations` had **no**
foreign key to `shipping_methods`. It was *consistent with module convention* — a grep over every
Fulfillment migration shows **zero** `FOREIGN KEY`/`REFERENCES` clauses anywhere, so no Fulfillment table
enforced FKs at the database level. Deleting a method therefore left orphan translation rows. Mission B
authorizes fixing only *verified critical* issues, and an orphan-row leak on a table that is written
exclusively through the aggregate is a real (if narrow) integrity gap, so it was fixed — see §3.

`OPTIONLESS_ROWS_COEXIST_OK` — two option-less methods coexist, confirming the partial unique index
(`WHERE legacy_option_code IS NOT NULL`) does not collapse service-level fallback rows.

Reader (`ShippingOneLevelCatalogReader` over the real database):

```
READER all=5 active=4 get_case_insensitive=post:standard get_missing=null get_malformed=null
       post_translation=پست معمولی optionless_binding=null inactive_present=yes
```

Case-insensitive lookup resolves; unknown and malformed codes fail closed with `null` (no exception, no
guess); the Persian translation round-trips; the option-less binding reads back `NULL`; the inactive
method is present in the unfiltered list and correctly absent from the active list.

### 32-character persisted identity boundary

```
BOUNDARY persisted_32_chars_readback=32
BOUNDARY max_persisted_code_length=32
```

A 32-character code persists and reads back at full length, and no stored code ever exceeds 32.

### Legacy catalog untouched

```
LEGACY_UNTOUCHED services=0 options=0 new_methods=6
```

The new tables were populated while the legacy `shipping_services` / `shipping_service_options` tables
stayed empty in this fresh database — the one-level catalog never wrote to, and never depended on, them.

### DOWN, then re-UP

```
DOWN new_tables_remaining=0 legacy_tables_remaining=2 onelevel_history_rows=0
REUP new_tables_after_reapply=2
```

`Down` drops exactly the two new tables, leaves the legacy catalog tables in place, and removes the
migration row from EF history. Re-applying `Up` recreates both tables, proving the rollback is not a
one-way door. Disposal:

```
ISOLATED_DB_DROPPED tooba_onelevel083_ae5246cb
LEFTOVER_ISOLATED_DATABASES 0
```

## 3. Narrow code fix (Mission B)

`src/backend/Modules/Fulfillment/Tooba.Fulfillment.Infrastructure/Persistence/Migrations/20261015120000_AddShippingOneLevelCatalog.cs`

Added a database-level referential integrity constraint for the child translation table, and the matching
`Down`:

```sql
CONSTRAINT fk_shipping_method_translations_method FOREIGN KEY (shipping_method_id)
    REFERENCES fulfillment.shipping_methods (shipping_method_id) ON DELETE CASCADE
```

This is the minimal, non-speculative repair for the one verified defect. It is deliberately scoped to the
**new** table only: retrofitting FKs onto the pre-existing Fulfillment tables would be an out-of-scope,
schema-wide change. Because the table is always written through the aggregate, the constraint is a
durability net, not a behaviour change; the option-less/partial-index semantics are unaffected.

`src/backend/Modules/Fulfillment/Tooba.Fulfillment.Infrastructure/Persistence/FulfillmentDbContext.cs`

The database constraint alone was **not** sufficient — and this was caught by re-running the isolated
probe rather than by assumption. Without a mapped relationship, EF orders inserts arbitrarily and inserted
the translation **before** its method, so the new constraint rejected a legitimate write with
`23503/fk_shipping_method_translations_method`. Fixed by mapping the relationship without changing the
domain shape (no navigation property added):

```csharp
entity.HasOne<ShippingMethod>()
    .WithMany()
    .HasForeignKey(x => x.ShippingMethodId)
    .OnDelete(DeleteBehavior.Cascade);
```

After this, the same write succeeds and the negative probe is rejected: `rejected=8/8`. The model change is
metadata-only — no column, index, schema or public contract changed.

No other source change was made. Verified while here: Contracts expose no EF model (`IShippingOneLevelCatalogReader`
returns Contracts records only), no Host business authority was added, no foreign `DbContext` is touched,
and the legacy catalog routes are untouched.

### Live Development compatibility audit (read-only)

`readonly-legacy-code-audit.sql` — five `SELECT`s, no DDL/DML, run against `tooba_alpha`:

| source | code | len | rows |
| --- | --- | --- | --- |
| `order.checkouts` | `post:express` / `tipax:express` / `tipax:standard` | 12 / 13 / 14 | 11 / 1 / 1 |
| `order.cart_shipping_drafts` | `post:express` / `tipax:express` / `tipax:standard` | 12 / 13 / 14 | 12 / 1 / 1 |
| `fulfillment.fulfillments` | `post:express` / `tipax:express` / `tipax:standard` | 12 / 13 / 14 | 4 / 2 / 3 |
| `fulfillment.shipments` | `post` / `snapp_courier` | 4 / 13 | 3 / 2 |
| `fulfillment.consolidated_packages` | `post` / `snapp_courier` | 4 / 13 | 1 / 1 |

Three facts this establishes, now durably encoded as a focused test
(`Live_development_persisted_legacy_codes_resolve_exactly_where_the_model_intends`):

1. **The 32-character contract is sufficient.** The longest code in live data is 14 characters; no
   column widening is needed and no historical code needs rewriting.
2. **`post:standard` does not occur in live data at all**, while the currently **inactive**
   `post:express` does (11 paid checkouts, 12 drafts). Activation must therefore follow the catalog, never
   historical usage — 082 already had this right, and it is now regression-locked.
3. **`fulfillment.shipments`/`fulfillments`/`consolidated_packages` persist the bare `post`**, which by
   B1 is deliberately *not* a one-level method (a service-level fallback policy is not a second sellable
   method). Resolving it through the one-level catalog fails closed with `null` instead of matching
   something else. This is the intended safe outcome and is exactly why the legacy catalog must stay the
   operational read authority — recorded as an explicit Architect-visible boundary, not a hidden hole.

## 4. Tests actually run

| command | result |
| --- | --- |
| `dotnet test src/backend/Modules/Fulfillment/Tooba.Fulfillment.Tests` | **Passed! Failed: 0, Passed: 95, Skipped: 0** (was 94 in 082; +1 live-code compatibility test) |
| `dotnet test src/backend/Host/Tooba.Host.Tests --filter FullyQualifiedName~FulfillmentModuleAmsc001W3CertGuardTests -p:OutDir=.tmp-083-host-out\bin\` | **Passed! Failed: 0, Passed: 14, Skipped: 0** |
| isolated PostgreSQL probe | all stages green except the 8th negative probe, which correctly exposed the missing FK (now fixed) |

The Host guard was **not** blocked this time. `-p:BaseIntermediateOutputPath`/`-p:BaseOutputPath` fails
with 159 `CS0579` duplicate-attribute errors (the repo's Directory.Build.props pins `obj/`, so a
divergent intermediate path is unusable). Redirecting only `OutDir` works cleanly and was used instead.
The running Host (`Tooba.Host`, pid 33548) was never restarted and is still alive.

## 5. Live environment unchanged

```
fulfillment tables named shipping_method% in tooba_alpha : 0
fulfillment.__ef_migrations_history in tooba_alpha        : 8 rows, latest 20260910170000_AddShippingServiceCatalog
running Host process                                     : Tooba.Host alive
leftover isolated databases                              : 0
```

`tooba_alpha` received **no** DDL and **no** DML from this task; the new migration is absent from its EF
history, so the one-level catalog remains UNMIGRATED in Development.

## 6. Isolated migration safety in principle

The migration is **production-safe in principle**: additive, idempotent `CREATE TABLE IF NOT EXISTS`, no
data rewrite, no dependency on other schemas or modules, and a `Down` that removes exactly its own two
tables (now including the new FK). It is **not** called live-ready: no live rollout is authorized, the
backfill executor does not exist yet, and §1's stale-snapshot debt means the next generated migration must
be hand-authored.

## 7. Rollback

1. Keep `Tooba:ShippingOneLevelCatalog` OFF ⇒ zero behavioural change, zero data touched.
2. `Down` of `20261015120000_AddShippingOneLevelCatalog` ⇒ drops `shipping_method_translations` then
   `shipping_methods`; proven reversible by the isolated re-UP.
3. No other module references the new tables, so dropping them cannot affect Order, Shipping, Geography,
   Payment or Settlement.

## 8. Recommended 084 scope (not issued, not run)

Admin five-tab shell and service list on `/admin/shipping-services`, behind the still-OFF
`Tooba:ShippingOneLevelCatalog` gate:

- context bar plus «سرویس‌ها»، «تعرفه و پوشش»، «بسته‌بندی و کرایه»، «برنامه کاری و ظرفیت»، «بررسی هزینه»;
- **one independent row per method** — `post:standard`, `post:express`, `tipax:standard`, `tipax:express`,
  `store_courier`, `in_person`, `snapp_courier` — with `provider_kind` hidden and **no** second-level
  selection;
- inactive methods stay visible and inactive (never silently activated), and the screen must surface that
  `post:express` is inactive although historical paid rows reference it;
- user-visible labels become independently editable **only in a later authorized task**, and by editing
  the translation row — never by changing a historical `method_code`.

The planned 11-task ceiling (081–091) is unchanged. No 084 task was created or executed.
