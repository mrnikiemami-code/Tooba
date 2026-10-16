# SHIPPING-ONE-LEVEL-UI-111 — isolated two-phase migration runtime + sellable proof

- Task: `TB-SHIPPING-ONE-LEVEL-TABS-111` (parent `TB-SHIPPING-ONE-LEVEL-TABS-110`)
- Mode: `BOUNDED_ISOLATED_TWO_PHASE_MIGRATION_RUNTIME_AND_SELLABLE_PROOF`
- Expected base SHA: `bc8c87e2853ee6958420ed6e31631eca3c6211d0` (verified ancestor of HEAD)
- Status: **PARTIAL** — 109 barrier cleared, real sellable readback obtained, but a NEW later blocker
  (`catalog.attribute.code.duplicate`) prevented startup completion. `STOP_FOR_ARCHITECT`, no code change.
- Code-SHA: `NONE` — zero production source modifications.

## 1. The TB-109 barrier is RESOLVED (observed, one isolated run)

The TB-110 two-phase ordering is confirmed at runtime on an empty database:

- all module schemas migrated before any post-migration step: catalog 29, offer 3, pricing 1, inventory 5,
  tax 1, party 2, promotion 2, localization 1, support 1 applied migrations; 20 module schemas present;
  110 EF migration commands executed;
- `relation "party.parties" does not exist` occurrences in the whole run: **0** (TB-109's exact blocker);
- `PublishReadiness` diagnostics: **0**; `محصول برای انتشار آماده نیست.`: **0** — nothing to diagnose;
- the Catalog module-owned post-migration step **completed**: `schema-mobile-demo-phone` = Published,
  exactly two sparse variants, exactly one default = `PHONE-BLK-128`, distinct fingerprints, no Cartesian
  growth (products 1 / variants 2 / defaults 1 / attribute_definitions 4 / product_history_entries 6).

Causal ordering proof from scratch row timestamps (UTC) and referential integrity (`bad` = 0 for every
offer→party, price→offer, stock→offer, offer→variant join):

| relation | first row created |
| --- | --- |
| catalog.products | 01:21:01.016 |
| catalog.variants | 01:21:01.192 |
| party.parties | 01:21:01.370 |
| offer.offers | 01:21:01.412 |
| pricing.prices | 01:21:01.460 |
| inventory.stock_positions | 01:21:01.564 |

The post-migration step created the product/variant, then the sellable enricher created the seller party,
then offers, then prices, then stock — i.e. it ran **after** Party's schema already existed.

## 2. Real sellable readback (presence, not a readiness claim)

| seller_sku | offer status | base price | currency | price status | available |
| --- | --- | --- | --- | --- | --- |
| SCHEMA-PHONE-PHONE-BLK-128 (default variant) | Active | 12,500,000 | IRR | Active | 5.0 |
| SCHEMA-PHONE-PHONE-BLU-256 | Active | 13,000,000 | IRR | Active | 5.0 |
| DEV-SEED-OOS (deliberate development OOS seed) | Active | — | — | — | 0.0 |

Totals: offers 3 (all Active), prices 4 (2 Active Base), stock_positions 3 (2 with positive on_hand),
seller parties 2, locations 1, carts 0, seller_orders 0, shipping_methods 0.

Eligibility limitation (explicit): this is **row presence** of a published product + active offer + active
Base price + strictly positive available stock for both variants. It is **not** a storefront, cart,
checkout, price-cutover or commercial-readiness claim; no cart, checkout, activation or write was performed.

## 3. FIRST NEW BLOCKER (startup did not reach healthy state)

`Unhandled exception. System.InvalidOperationException: catalog.attribute.code.duplicate`

```
CatalogDirectory.Unwrap (CatalogDirectory.cs:894)
CatalogDirectory.CreateAttributeDefinitionAsync (CatalogDirectory.cs:987)
WorkspaceDemoProductSeed.SeedProductAsync (WorkspaceDemoProductSeed.cs:103)
WorkspaceDemoSeed.SeedNewProductAsync (WorkspaceDemoSeed.cs:40)
DevelopmentSchemaMigrator.ApplyCoreAsync (line 98) -> ApplyCoreAsync (line 112)
Program.<Main>$ (Program.cs:367)
```

Cause (source-proven, both sides read-only): the module-owned attribute-schema seed creates attribute
definition `color` first (`CatalogAttributeSchemaDevelopmentSeed.cs:39`), then the **legacy** Host bootstrap
`WorkspaceDemoProductSeed` (`ColorAttributeCode = "color"`, line 46) calls
`CreateAttributeDefinitionAsync("color", …)` at line 103 and is refused. Scratch readback shows the
already-present definitions: `color, ram, screen_size, storage`.

Classification: a Development **seed collision** between two bootstraps active in the same startup
(`RunLegacyBootstraps=true` on an empty DB) — **not** the 109 ordering barrier and **not** a migration,
Catalog-readiness or Shipping-price defect. It occurs at `Program.cs:367` after `MigrateOrderedAsync`
(line 70) already returned, i.e. *after* the whole registered two-phase sequence (Catalog + Promotion +
Localization steps) completed — the Promotion step is independently evidenced by the `DEV-SEED-OOS` offer
and the promotion campaign tables. No fix was implemented (Mission A forbids coding).

## 4. Isolation, one-run watchdog, cleanup, parity

- Scratch: disposable `postgres:17` container `tooba-111-scratch-pg`, loopback-only `127.0.0.1:55447`,
  task-generated 24-char credential held only in an ignored file; databases `tooba_111_scratch` +
  `tooba_111_messaging`; no shared volume.
- Offline effective-config guard (`.tmp-111-run/isolation-guard.json`, reproduced in `prepare-output.txt`):
  exactly 1 tenant (`store-alpha:Active:tenant-111-scratch`), references `tenant-111-scratch` +
  `messaging-111-scratch` only, `containsSharedAlphaDatabase=false`, `containsSharedAlphaReference=false`,
  `containsSharedAlphaPort=false`; ShippingOneLevelCatalog / ShippingRateAuthority /
  ShippingOperationalShadow all OFF; `RunLegacyBootstraps=true`, `AllowResetAndSeed=false` (no reset/seed,
  no 545-product seeding). Credentials written to evidence: **false**.
- Build: `dotnet publish Tooba.Host -c Debug -o .tmp-111-host-out` — 0 errors; published from HEAD
  `d791131e` (base `bc8c87e2` + docs only), exe present.
- ONE launch, no retry, external watchdog ≤180s: PID 26940, exe verified as the task-owned publish path,
  `ELAPSED_SECONDS=54`, `PROCESS_EXITED=True` (natural crash), `KILLED_BY_WATCHDOG=False`,
  `HEALTH_200_OBSERVED=False`, `LISTENERS_ON_5395_AFTER_STOP=0`, `PARTY_SCHEMA_REGRESSION_SEEN=False`.
- Cleanup: no task-owned Host process remained; container removed (listeners on 55447 = 0, no
  `tooba-111*` container left); `.tmp-111-run`, `.tmp-111-host-out` and the scratch credential file deleted;
  1,929,043 bytes stdout / 1,877 bytes stderr dropped after a programmatic credential scan (password absent,
  0 `Password=` / `Username=scratch` hits). **No name/wildcard termination anywhere**; `:5088` and `:3000`
  never stopped.
- Shared parity READ-ONLY and identical before/after: PID `:5088`=35624, `:3000`=31580,
  `/health` = `{"status":"ok"}`, `/__platform-commerce` identical (edition SingleStore, deploymentId
  dev-singlestore, tenantId store-alpha, connectionReference tenant-alpha, theme-alpha, IR),
  `:3000` HTTP 308; tooba_alpha counts 7 / 14 / 50 / 78 / 14 / 26 / 567 / 3223 exactly as in preflight
  (and as in TB-109). No shared change, no incident.

## 5. Evidence map

`watchdog-result.txt`, `startup-stage-summary.txt`, `scratch-inspection.{sql,txt}`,
`sellable-chain-evidence.txt`, `redacted-diagnostic-excerpt.txt`, `prepare-output.txt`,
`prepare-isolated-target-111.js`, `isolated/launch-isolated-111.ps1`, `isolated/cleanup-111.ps1`,
`isolated/parity-postflight-111.ps1`, `cleanup-and-parity.txt`, `shared-parity-preflight.{sql,txt}`,
`shared-parity-postflight.{sql,txt}`, `build-record.txt`, `claim-task.js`, `post-result.js`,
`task-receipt.json`, `task.md`, `summary.md`, `RESULT.bridge.txt`.

## 6. Rollback

Nothing to roll back in source: zero `src/**` modifications (`Code-SHA: NONE`). All runtime resources were
disposable and are already destroyed. Reverting only the task-owned docs is sufficient.

## 7. Next exact check (for the Architect)

The 109/110 migration-ordering line is closed. The next barrier is the Development seed collision on
attribute code `color` between `CatalogAttributeSchemaDevelopmentSeed` and the legacy `WorkspaceDemoProductSeed`
when `RunLegacyBootstraps=true` runs on an empty database. Any repair is a separate authorized task; this
task implemented nothing. Startup healthy state, storefront labels and one-level tab rendering remain
unverified.
