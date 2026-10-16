# TB-SHIPPING-ONE-LEVEL-TABS-121 — Isolated runtime proof of the Fulfillment one-level Development bootstrap

Parent: TB-SHIPPING-ONE-LEVEL-TABS-120 · Channel: `tooba-main` · Worker: `tooba-worker-01`
Mode: **BOUNDED_ISOLATED_ONE_LEVEL_CATALOG_RUNTIME_LABEL_PROOF** (observation only — no code change)

## 1. Question this task answers

TB-119 proved seven real legacy projected shipping methods on a scratch database while the
enabled one-level catalog was **empty**. TB-120 then installed the Fulfillment-owned,
Development-only, gate-controlled backfill (`ShippingOneLevelCatalogDevelopmentSeed`) driven by the
canonical `ShippingMethodBackfillPlanner` + `IShippingOneLevelBackfillStore` inside the trusted
`DevelopmentTenantCommerceContext` scope.

TB-121 asks one thing: on a **freshly booted, fully isolated Development Host** with the one-level
gate enabled in the disposable overlay, does the catalog populate **by itself** and do the actual
Fulfillment and Storefront projection seams serve labels sourced from those catalog rows?

## 2. Isolation and provenance

| Item | Value |
| --- | --- |
| Scratch PostgreSQL | `127.0.0.1:55452` (unique; not `5432`, not any prior scratch port) |
| Scratch databases | `tooba_121_scratch`, `tooba_121_messaging` (task-owned, no volume, disposable) |
| Isolated Host | `.tmp-121-host-out\Tooba.Host.exe`, bound to `http://127.0.0.1:5400` |
| Effective tenant | `store-alpha:Active:tenant-121-scratch` (exactly one, active) |
| Provenance proof | `GET /__platform-commerce` → `connectionReference=tenant-121-scratch`, `deploymentId=isolated-121` |
| Overlay gate | `Tooba:ShippingOneLevelCatalog:Enabled=true` **only** in the disposable overlay |
| `RunLegacyBootstraps` / `AllowResetAndSeed` | `true` / `false` (unchanged) |
| `ShippingRateAuthority` / `ShippingOperationalShadow` | absent / off |
| Shared `:5088`, `:3000`, `tooba_alpha`, containers | read-only, never started/stopped/migrated |

Offline effective-configuration guard: `docs/architecture/evidence/.../isolation-guard.json`
(`failClosed=true`, `containsSharedAlphaDatabase=false`, `oneLevelCatalogEnabledInOverlay=true`).
The scratch password came from the environment (`TOOBA_121_SCRATCH_PASSWORD`) and is not committed.

Launch discipline: **exactly one** `Start-Process` of the absolute exe path, watchdog 180 s, bounded
150 s keep-alive. Startup reached `/health` 200 in **41 s** (watchdog never hit). Shutdown was
performed only on the recorded PID after re-verifying its absolute path.

## 3. Startup stages observed (single boot)

From `startup-stages.txt`, `shipping-migration-markers.txt`, `seed-execution-evidence.txt`:

1. Development migrations ran, including `20261015120000_AddShippingOneLevelCatalog` — the one-level
   catalog schema is created by the ordinary isolated startup, with no manual SQL and no bypass.
2. The legacy shipping catalog bootstrap seeded `fulfillment.shipping_services` /
   `shipping_service_options` (5 services, 5 options, all legacy rows active).
3. **TB-120's Development bootstrap then ran**: the log shows a batched
   `INSERT INTO fulfillment.shipping_methods (...)` command — one command carrying seven inserts —
   followed by the translation inserts, i.e. `ShippingMethodBackfillPlanner.Plan` produced seven
   non-skip actions and `IShippingOneLevelBackfillStore.ApplyAsync` applied them.
4. `Now listening on: http://127.0.0.1:5400`, `Application started`, `Hosting environment: Development`.
5. `fail:`/`crit:`/`Unhandled exception` markers: **0**. stderr lines: **0**.

## 4. Catalog proof (authorized read endpoint)

`GET /v1/admin/shipping-services/shipping-methods/one-level?language=fa` with the canonical
Development admin actor from `GET /v1/admin/dev-context`:

```
gate = "Enabled"    ready = true    rows = 7
post:express     fa="پیشتاز"                en="Express"                isActive=true sort=10
post:standard    fa="معمولی"                en="Standard"               isActive=true sort=11
tipax:express    fa="پیشتاز"                en="Express"                isActive=true sort=20
tipax:standard   fa="معمولی"                en="Standard"               isActive=true sort=21
snapp_courier    fa="اسنپ / پیک آنلاین"      en="Snapp / Online courier"  isActive=true sort=30
store_courier    fa="پیک فروشگاه"            en="Store courier"          isActive=true sort=40
in_person        fa="تحویل حضوری"            en="In person"              isActive=true sort=50
```

An empty catalog is no longer the outcome: TB-119's "`ready` catalog with zero rows" is resolved.

### Identity mapping

`GET .../one-level/{methodCode}/binding` (read-only, same seam family) for all seven codes:

```
post:express   -> resolved=true serviceCode=post    optionCode=express    isActive=true
post:standard  -> resolved=true serviceCode=post    optionCode=standard   isActive=true
tipax:express  -> resolved=true serviceCode=tipax   optionCode=express    isActive=true
tipax:standard -> resolved=true serviceCode=tipax   optionCode=standard   isActive=true
snapp_courier  -> resolved=true serviceCode=snapp_courier                  isActive=true
store_courier  -> resolved=true serviceCode=store_courier                  isActive=true
in_person      -> resolved=true serviceCode=in_person                      isActive=true
```

Read-only read-back of the task-owned scratch DB confirms the same identity and adds the legacy
column values (`scratch-legacy-activation-readback.txt`), proving the seeded one-level rows mirror
the legacy activation state rather than forcing activation.

## 5. Storefront projection proof (the label seam)

One bounded, authorized scratch projection (guest cart + one real scratch offer line, canonical
`GuestAllowed` policy seam — no order, checkout, payment or shipment):

`POST /v1/storefront/shipping/projection { cartId, language: 'fa' }`

```
projected[post:express]   label="پیشتاز"             catalogName="پیشتاز"             match=true
projected[post:standard]  label="معمولی"             catalogName="معمولی"             match=true
projected[tipax:express]  label="پیشتاز"             catalogName="پیشتاز"             match=true
projected[tipax:standard] label="معمولی"             catalogName="معمولی"             match=true
projected[snapp_courier]  label="اسنپ / پیک آنلاین"   catalogName="اسنپ / پیک آنلاین"   match=true
projected[store_courier]  label="پیک فروشگاه"         catalogName="پیک فروشگاه"         match=true
projected[in_person]      label="تحویل حضوری"         catalogName="تحویل حضوری"         match=true

projected_labels_matching_one_level_catalog = 7
projected_labels_not_matching               = 0
```

TB-119's projected labels were the composed legacy labels (`"پست — معمولی"`, `"پست — پیشتاز"`).
With the same scratch recipe and the gate enabled, every projected label is now the one-level
catalog's own Persian name: `"معمولی"`, `"پیشتاز"`, `"اسنپ / پیک آنلاین"`, `"پیک فروشگاه"`,
`"تحویل حضوری"`. The change is observable and is attributable to the one-level catalog rows, since
prices, `serviceCode`, `optionCode`, `leadDays` and `estimationLabel` are unchanged from TB-119.

## 6. Non-interference

Shared Host PID `35624` at `.tmp-dev-host-out-5088-101\Tooba.Host.exe`, `:5088` `/health` 200 and
`tenant-alpha` provenance, `:3000` 308, and the `postgres-db`/`rabbitmq` containers are identical
before and after. Read-only `tooba_alpha` counts are byte-identical:

```
methods 7 | translations 14 | carts 50 | cart_lines 78 | drafts 14 | seller_orders 26 | products 567 | offers 3223
```

No tracked `appsettings` file changed; no shared port, database, reference or credential was used.

## 7. Cleanup

Isolated Host stopped by recorded PID after path verification (`STILL_RUNNING_AFTER_STOP=false`,
`LISTENERS_ON_5400_AFTER_STOP=0`); scratch container removed; `.tmp-121-run` and `.tmp-121-host-out`
deleted; scratch port has zero listeners and no `.tmp-121*` Host process survives. No secret is
retained in committed evidence.

## 8. Explicitly NOT proven

- **Frontend DOM/browser parity: NOT PROVEN.** No isolated frontend render was performed; the label
  proof is API-level only, taken from the authorized projection response.
- Shipping selection persistence, checkout, payment, shipment lifecycle and commercial readiness.
- Production/`store-alpha` behaviour: the gate stays off outside the disposable Development overlay.

## 9. Rollback

No repository code or configuration changed. Rollback is: nothing to revert; the disposable
container, overlay, publish output and run directory are already deleted, and the isolated Host PID
is terminated. The only repository additions are task documentation and evidence.
