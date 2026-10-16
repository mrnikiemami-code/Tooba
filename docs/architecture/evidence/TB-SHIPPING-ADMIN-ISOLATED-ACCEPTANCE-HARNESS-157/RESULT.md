# TB-SHIPPING-ADMIN-ISOLATED-ACCEPTANCE-HARNESS-157 — result

Status: **PARTIAL** — STOP_FOR_ARCHITECT
Base/Expected SHA: `2b798560b9d3dd18e5d5a8c92c919c5fcbf82838`
Code/Evidence SHA: `d97d53e28fd498e9b755841fae5c2905ab3f74ba` (subsequent commits are evidence-text records only)
Channel `tooba-main` · Worker `tooba-worker-01` · Mode: BOUNDED_REUSABLE_PRIVATE_ADMIN_RUNTIME_AND_REAL_MONEY_WRITE_READ_ACCEPTANCE

## Verdict

The **reusable private Admin acceptance harness exists and works**, and the previously missing real
authorization + HTTP + money write/read proof **was produced end to end over a private isolated runtime**:
19/19 measured assertions PASS. The verdict is PARTIAL and **not** PASS because two components were not
proven inside the timebox: the **real private Admin browser leg** (the private Next frontend was never
launched) and the **storefront quote dependency** of the edited amount. Both are reported below as exact
blockers rather than claimed.

## What was actually run (real, private, disposable)

| Layer | Endpoint | Ownership |
| --- | --- | --- |
| Postgres 16.15 | `127.0.0.1:55475`, `tooba_157_scratch` + `tooba_157_messaging` | container `tooba-tb157-pg` (removed after evidence) |
| Host (this HEAD) | `http://127.0.0.1:5417`, Edition `SingleStore` | task-owned `Tooba.Host.exe` (PID 41828, stopped by recorded path) |
| Next frontend | `http://127.0.0.1:3111` | **not launched** — the remaining blocker |

`isolation-guard.json` (fail-closed, PASS): one Active tenant (`store-alpha` → `tenant-157-scratch`), no
`tooba_alpha`, no `tenant-alpha`, no `Port=5432`, no prior scratch port, `ShippingRateAuthority`/`ShippingOperationalShadow`
absent, Edition `SingleStore` (no swap), `AllowResetAndSeed=false`, `RunLegacyBootstraps=true`. The scratch
password came from the environment and was never written into evidence.

## The missing piece this task found and closed: admin authorization in scratch

Earlier shipping acceptance tasks could not produce authorized Admin evidence because **no script seeded
admin authorization in a scratch runtime**. The supported path was found and is now in the harness:

1. `GET /v1/admin/dev-context` → the Host's own **Development-only** seam returning the sanctioned dev
   admin actor (created by `AdminDevActorBootstrap` with a real `user#member@tenant:store-alpha` tuple).
2. `POST /v1/admin/access-control/bootstrap` → the product's own access-control bootstrap: system
   `platform-admin` role + real permission catalog + assignment, which writes the real
   `user#granted@permission:<id>` tuples through `IAuthorizationTupleWriter`.
3. `GET /v1/admin/access-control/me/capabilities` → **55 grants observed, including `fulfillment.view`
   and `fulfillment.manage`**.

No authorization was disabled, no fabricated header, no global allowlist, no privilege fallback.

## Measured assertions (real HTTP, private Host, `assertion-matrix-157.json`: 19/19 PASS)

| # | Assertion | Observed |
| --- | --- | --- |
| 1 | dev-context available | `200` |
| 2 | anonymous read fail-closed | `401 admin.actor.missing` |
| 3 | unknown actor fail-closed | `403 admin.authorization.denied` |
| 4 | access-control bootstrap | `204` |
| 5 | `fulfillment.view` granted | `200`, 55 grants |
| 6 | `fulfillment.manage` granted | `200`, 55 grants |
| 7 | second actor fail-closed | `403 admin.authorization.denied` |
| 8 | authoritative one-level binding | `200 serviceCode=tipax optionCode=standard` |
| 9 | authorized `GET …/financial-status?optionCode=standard` | `200 state=NeedsConfiguration reason=shipping.financial.no_owned_rules` |
| 10 | provider/service-level (`optionCode=null`) write accepted | `200` |
| 11 | legacy row is not this method's price | `200 state=LegacySharedUnresolved mode=None`, no currency |
| 12 | owned fixed **IRR** rule saved via the existing seam | `200 ratePolicyId=01a14664-a0ce-7000-af99-032b6e042171` |
| 13 | read back from the server | `200 state=FinanciallyConfigured currency=IRR amount=185000 reason=shipping.financial.owned_rules_present` |
| 14 | legacy row preserved after the owned write | `ruleCount=1 serviceLevelRules=1` |
| 15 | sibling `tipax:express` unchanged | `200 state=LegacySharedUnresolved ruleCount=0 siblingRules=1` |
| 16 | conflicting duplicate denied | `400 shipping.rate_policy.scope_duplicate` |
| 17 | rejected write did not mutate | `ruleCount=1 amount=185000` |
| 18 | unauthorized write rejected | `403 admin.authorization.denied` |
| 19 | unauthorized write did not mutate | `ruleCount=1 amount=185000` |

Shipping DB read-back (`db-readback-157.txt`, real persisted rows):

```
tipax|option=<null service-level>|amount=99000.00|currency=IRR|active=true
tipax|option=standard|amount=185000.00|currency=IRR|active=true
```

Sanitized responses: `financial-status-initial-157.json` (NeedsConfiguration / `no_owned_rules`),
`financial-status-after-legacy-157.json` (LegacySharedUnresolved / `mode=None`),
`financial-status-after-owned-157.json` (FinanciallyConfigured / `FixedAmount` / IRR / 185000),
`financial-status-sibling-157.json`. No `0` was ever interpreted as free and no missing tariff was made free.

## Scratch-only data required (and why)

* **Shipping/Geography schema**: the Development Host deliberately does **not** migrate the Shipping
  schema (`ShippingModuleCompositionGuardTests` asserts the module registers no `IModuleSchemaMigrator`);
  the real product tool `Tooba.MigrationRunner apply --all-tenants` owns it. Before that step every
  shipping read answered `500 platform.unexpected` with `42P01: relation "shipping.rate_policies" does not
  exist` — a genuine gap in the TB-152/154 scratch recipe, now fixed in `seed-scratch-157.ps1`.
  Applied result: `schema=shipping current=20261015071836_ShippingPackagingPolicy result=ok`, exit 0.
* **Destination fixture**: the write seam validates its destination through the read-only Geography
  contract (`IGeographyLookup.GetByIdsAsync`); with an empty scratch geography every write failed
  `400 shipping.destination_scope.invalid`. `scratch-geography-fixture-157.sql` inserts exactly one
  consistent chain (country → area → city → service area) plus names **in the disposable database only**.
  No schema change, no migration, no production data.

## Remaining gaps (explicit)

1. **Real private Admin browser leg — NOT DONE (blocker).** The private Next frontend was never launched,
   so there is **no screenshot** and no DOM read of the Admin Rates & Coverage / General screens, and the
   save→reload visual cycle is unproven. The HTTP+DB proof above is real; the pixel/visual claim is not
   made. `launch-isolated-frontend-157.ps1` is committed so the next run is a single command.
2. **Storefront quote dependency — UNVERIFIED.** Whether the storefront calculator consumes this owner
   seam was not exercised (it needs an authenticated cart journey). No production rate was invented and no
   financial readiness is claimed on its behalf.
3. **"Valid actor without the capability" probe** is order-dependent: the pre-grant probe for the
   sanctioned dev actor returned `401/403` in the first, pre-bootstrap run but becomes `200` after the
   idempotent bootstrap, so it is recorded here as an observation and the durable assertion uses an
   unknown actor (`403`) plus anonymous (`401`). Isolating "has `tenant#view` but not `fulfillment.view`"
   would need a second persisted role/actor, which was not built inside the timebox.
4. The harness (`acceptance-157.js`) expects a fresh scratch shipping state; re-runs report the second
   write as the denied duplicate. Documented in `README.md`.

## Files changed

* `docs/architecture/evidence/TB-SHIPPING-ADMIN-ISOLATED-ACCEPTANCE-HARNESS-157/` — harness
  (`prepare-isolated-target-157.js`, `launch-isolated-host-157.ps1`, `launch-isolated-frontend-157.ps1`,
  `seed-scratch-157.ps1`, `cleanup-157.ps1`, `acceptance-157.js`, `scratch-geography-fixture-157.sql`,
  `README.md`) + evidence (`isolation-guard.json`, `host-launch-157.txt`, `assertion-matrix-157.json`,
  `acceptance-157-trace.txt`, `financial-status-*.json`, `db-readback-157.txt`, `cleanup-157.txt`).
* `docs/ai/tasks/TB-SHIPPING-ADMIN-ISOLATED-ACCEPTANCE-HARNESS-157.task.md`.

No production code, contract, schema, migration, global CSS or Host business authority was changed; no
unrelated dirty file, stash or TB-075/079 log was touched; no rebase/reset/autostash/force-push occurred.

## Cleanup (proven)

`cleanup-157.txt`: task-owned Host stopped by recorded PID with an executable-path match
(`STOPPED_PID=41828 PATH_MATCH=True STILL_ALIVE=False`), the only task-owned container removed
(`docker rm -f tooba-tb157-pg`), `:5417` / `:3111` / `:55475` no longer listening, scratch container
absent, **`:3000` / `:5088` / `:5432` untouched**, `.tmp-157-run` and `.tmp-157-host-out` deleted (gitignored).

## Next smallest fix

Launch `launch-isolated-frontend-157.ps1` on top of this harness, open the private Admin
`shipping-services` Rates & Coverage + General tabs in a real browser with `prepareAdminDevActor()`, and
capture the save→reload screenshot + DOM read for the exact amount already proven over HTTP. Then run the
storefront quote check with an authenticated cart.
