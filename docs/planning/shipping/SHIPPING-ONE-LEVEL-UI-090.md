# TB-SHIPPING-ONE-LEVEL-TABS-090 — Development one-level rollout and live proof

Parent: `TB-SHIPPING-ONE-LEVEL-TABS-089` · Program `TOOBA-SHIPPING-ONE-LEVEL-TABS`
Mode `BOUNDED_DEVELOPMENT_ONE_LEVEL_ROLLOUT_AND_LIVE_PROOF` · Stop-State `STOP_FOR_ARCHITECT`
Expected-Base-SHA `9c233ae5167334047757683c45f3727ee8975b29`

## 1. Environment provenance (positively identified, read-only)

| Fact | Observed |
|---|---|
| `GET /__platform-commerce` (running Host `127.0.0.1:5088`) | `edition=SingleStore`, `deploymentId=dev-singlestore`, `tenantId=store-alpha`, `connectionReference=tenant-alpha`, `resolvedHost=127.0.0.1`, `themeReference=theme-alpha`, `defaultMarketReference=IR` |
| Database | PostgreSQL 17.10, database `tooba_alpha`, user `admin` |
| Backup file | `.tmp-090-backup/tooba_alpha-pre-090.dump` — `pg_dump -Fc`, 2,492,453 bytes, 1,190 TOC entries, SHA256 `A3D924BA…D9439` (manifest in `backup-manifest.txt`) |
| Schema-only backups | `fulfillment-schema-pre-090.sql` (37,814 B), `order-cart-shipping-drafts-pre-090.sql` (6,542 B) |
| Migration status pre | `Tooba.MigrationRunner status --tenant store-alpha` → Fulfillment `current=20260910170000_AddShippingServiceCatalog pendingCount=0` (the one-level migration was **not** in the previously built runner) |
| Pre data | `shipping_methods=0`, `shipping_method_translations=0`, legacy `shipping_services=5`, `shipping_service_options=4`, `cart_shipping_drafts=14`, `fulfillments=9`, `shipments=5` |

No secret value is recorded anywhere in the evidence, the commit, or the logs.

## 2. What was actually done

1. **Provenance + backup first.** Full logical backup of `tooba_alpha` plus schema-only exports, all
   hashed before any write. No reset, no destructive down migration on populated data.
2. **Persisted backfill executor added** (`ShippingOneLevelBackfillStore`, module-owned):
   reads the existing one-level rows, delegates every decision to the existing pure planner
   `ShippingMethodBackfillPlanner`, and applies the plan **inside one transaction** (all-or-nothing).
   It refuses a plan that carries any violation, never deletes a row, and never rewrites a historical
   code/binding — it only creates rows and aligns activation.
3. **Planner translation fix.** `ShippingMethodDesiredRow` now carries per-language
   `Translations` (`ShippingMethodDesiredTranslation`), so the executor can no longer collapse every
   language onto the first translation. The planner remains a pure function.
4. **Real migration applied** through the canonical tooling
   (`dotnet build …Tooba.MigrationRunner` → `Tooba.MigrationRunner.exe apply --tenant store-alpha`):
   Fulfillment `current=20261015120000_AddShippingOneLevelCatalog pendingCount=0 result=ok`.
   Schema read-back: 2 new tables, 9 columns, `method_code` width 32, 4 indexes, 2 check constraints,
   1 FK. Legacy catalog tables and all historical rows untouched.
5. **Dry-run → commit.** Dry-run produced 7 `Create` items, 0 violations; the plan was committed;
   read-back showed exactly those 7 methods and 14 translations. Second dry-run: **7 items, all
   `Skip`, 0 violations**; re-applying wrote nothing (still 7/14). Per-method result:

| method_code | legacy service | legacy option | active | persisted label (fa / en) |
|---|---|---|---|---|
| `post:express` | `post` | `express` | **false** | پیشتاز / Express |
| `post:standard` | `post` | `standard` | true | معمولی / Standard |
| `tipax:express` | `tipax` | `express` | true | پیشتاز / Express |
| `tipax:standard` | `tipax` | `standard` | true | معمولی / Standard |
| `snapp_courier` | `snapp_courier` | *null* | **false** | اسنپ / پیک آنلاین · Snapp / Online courier |
| `store_courier` | `store_courier` | *null* | true | پیک فروشگاه / Store courier |
| `in_person` | `in_person` | *null* | true | تحویل حضوری / In person |

   `post:express` is historically inactive and stays inactive; `snapp_courier` is inactive and stays
   inactive. No bare `post`/`tipax` service-level row was invented (option-bearing services never
   become a second sellable method). No synthetic tariff, grant, schedule, or packaging row was added.
6. **Safety guards proven on the real database**, before the real write:
   * a plan carrying a violation → `REFUSED:shipping.one_level.backfill.plan_not_executable`, 0 rows written;
   * a colliding create inside one plan → `ROLLED_BACK:DbUpdateException`, 0 rows left behind
     (real all-or-nothing, not assumed).
7. **Read gate left OFF.** `Tooba:ShippingOneLevelCatalog` has no section in any `appsettings`, so it
   is off by absence. `Tooba:ShippingRateAuthority` and `Tooba:ShippingOperationalShadow` were **not**
   touched or enabled.

## 3. What could NOT be proven, and why (honest NOT VERIFIED)

The running Development Host (pid 33548, started 13:31) is a **stale binary**: its
`Tooba.Fulfillment.Endpoints.dll` (13:07) does not contain the `shipping-methods/one-level` route, and
`GET /v1/admin/shipping-services/shipping-methods/one-level` on it returns **404** (as does the
`/{methodCode}/binding` route). Browser proof of the five-tab page therefore requires rebuilding the
Host and restarting it — and a rebuild is currently **blocked by that same running process**:

* `dotnet build Tooba.Host` fails with `MSB3027/MSB3021` — DLLs in
  `src/backend/Host/Tooba.Host/bin/Debug/net8.0` are locked by `Tooba.Host (33548)`;
* `dotnet build Tooba.MigrationRunner` without an isolated output dir fails the same way
  (the isolated `-o .tmp-090-runner` build succeeded and is what applied the migration);
* the lock is held by a Host process that has been the Development baseline since 13:31 and is also
  the owner of the `079` OFF-relaunch lineage, i.e. a shared, user-managed service. Killing it
  speculatively is explicitly forbidden by the task, and its launch environment could not be
  positively reproduced from evidence alone.

So: **the database rollout is real and proven; the five-tab browser proof is NOT VERIFIED.** This is
reported as PARTIAL rather than claimed as a live PASS. No fake PASS, no invented request/response.

Also unverified: tab 3/4/5 live request/response against the new catalog, and any isolated
reversible write/read-back on a task-owned disposable policy or schedule — all of these sit behind the
same stale-host/rebuild blocker.

## 4. Focused verification

* `Tooba.Fulfillment.Tests`: **120 passed / 0 failed** (includes the new
  `ShippingOneLevelBackfillStoreTests`, 4 facts: dry-run reads the persisted catalog without writing,
  per-language names preserved, violating plan refused before any write, `AlignActivation` keeps
  identity). `log-fulfillment-tests.txt`.
* Real-Database probe `OneLevel090` on `tooba_alpha`: guard/atomicity/dry-run/apply/rerun/legacy/
  historical/per-language/no-service-level-row all green. `log-backfill-run-090.txt`.
* Migration tooling: `plan` then `apply` then read-back, all `result=ok`. `log-apply-090.txt`.
* Frontend was **not changed** in this task, so no frontend test/typecheck was run (task allows
  "limited frontend focused tests/typecheck only if changed").

## 5. Rollback

`rollback-090.sql` — step 1 deletes only the seven backfilled methods and their translations inside a
transaction and asserts the legacy counts stay 5/4; step 2 documents the canonical migration down
(already proven reversible on an isolated DB in `083`). Full-database restore path:
`pg_restore --clean --if-exists -d tooba_alpha .tmp-090-backup/tooba_alpha-pre-090.dump`.

## 6. Readiness

* **Source/test ready:** yes (executor + planner + tests).
* **Development data ready:** yes (migration applied, backfill committed, idempotent, legacy intact).
* **Live/browser ready:** **no** — stale Host binary + locked output; needs a controlled rebuild and
  restart outside this task's safe authority.
* **Commercially ready:** no. The gate is OFF, no checkout/price cutover, no order/payment/settlement
  change, no capacity enforcement, no COD settlement claim.
