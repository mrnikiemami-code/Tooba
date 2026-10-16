# SHIPPING-ONE-LEVEL-UI-117 — Isolated Runtime Acceptance After Commerce Context Repair

Parent task: `TB-SHIPPING-ONE-LEVEL-TABS-117` (Parent-Task `TB-SHIPPING-ONE-LEVEL-TABS-116`).
Mode: observation only — `BOUNDED_ISOLATED_DEVELOPMENT_LEGACY_CATALOG_RUNTIME_PROOF`.
Answer to the Architect's question: **the former `platform.edition.unconfigured` startup barrier is gone
in real isolated runtime**, all six legacy Catalog bootstraps complete, and the isolated Host reaches
`/health` 200 with verified scratch provenance.

Evidence: `docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-117/**`.

## Result

`PASS` (startup barrier only; see Non-claims).

| Item | Value |
| --- | --- |
| Expected-Base-SHA | `709e474951c0aa6f5481e2ff4658d24c02533dea` |
| Actual Base-SHA | `709e474951c0aa6f5481e2ff4658d24c02533dea` (drift: none; `git diff --stat` empty) |
| Code-SHA | NONE — no source file was modified in TB-117 |
| Evidence-SHA | this commit (docs/evidence only) |
| Measured minutes | ~35 min wall-clock incl. 55 s publish; Host startup 45 s of a 180 s watchdog |
| Isolated launches | exactly 1 (`PID 15724`, absolute exe path, watchdog-bounded) |

## What the run proved

1. `DevelopmentSchemaMigrator.ApplyAsync(app.Services)` completes on the empty scratch database
   (112 EF migration events, 8 module schemas).
2. Control enters the TB-116 gateway
   `DevelopmentTenantCommerceContext.RunForDevelopmentTenantAsync(app.Services, "legacy-catalog-dev-seed", ...)`
   and a trusted, scope-local `CommerceContext` exists **before** any legacy seed resolves its scoped
   `CatalogDbContext`.
3. `FashionTemplateCatalogSeed` resolves `CatalogDbContext` successfully. `platform.edition.unconfigured`,
   `Cannot resolve scoped service ... from root provider`, `catalog.attribute.code.duplicate` and
   `catalog-dev.attribute.incompatible-reuse` all occur **0 times** in the 1861-line log; stderr is empty.
4. All six legacy Catalog bootstraps complete: `template_products` 150, `template_categories` 560,
   `template_brands` 60, `store_templates` 10 (all 0 before this repair).
5. `GET /health` → 200 `{"status":"ok"}` and `GET /__platform-commerce` → 200 with
   `deploymentId=isolated-117`, `connectionReference=tenant-117-scratch`, `tenantId=store-alpha`
   — i.e. the isolated scratch target, not shared `tooba_alpha`/`tenant-alpha`.
6. Scratch readback also confirms the schema/legacy single-identity property at real startup: one `color`
   definition and one black option, reused across `schema-mobile-demo-phone` and `workspace-live-shirt`.
7. Cleanup and shared parity are clean (no INCIDENT).

## First remaining (non-blocking) observations

Not startup blockers, recorded for the Architect's next decision:

- `OutboxDispatcher` logs `Outbox claim failed for one module` for `Schema=returns` and `Schema=settlement`
  (`42P01 relation does not exist`) **after** the Host is already healthy. Those schemas are not part of the
  Development scratch set; the dispatcher warns and continues. Expected-SHA behaviour unchanged by TB-117.
- Two pre-existing EF query-shape warnings (Skip/Take and First without OrderBy) — untouched here.

## Non-claims

This is a **startup acceptance**, not production or storefront acceptance. It does **not** claim storefront
labels, cart reachability, checkout cutover, one-level shipping tab behaviour, or commercial shipping
readiness. Row presence in scratch is not storefront eligibility. `ShippingOneLevelCatalog`,
`ShippingRateAuthority` and `ShippingOperationalShadow` stayed OFF and no cart/order was created.

## Stop state

`STOP_FOR_ARCHITECT`; `automaticNextImplementationTask = NONE`. No TB-118 is authorized.
