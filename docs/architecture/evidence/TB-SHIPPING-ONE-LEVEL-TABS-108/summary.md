# TB-SHIPPING-ONE-LEVEL-TABS-108 — summary

**Status: PASS (source + focused tests only). Stop-State: STOP_FOR_ARCHITECT. automaticNextImplementationTask: NONE.**

The Architect accepted 107's `BLOCKED_DEFAULT_VARIANT_APPLICATION_SEAM` and authorized Option A only: one narrow,
Catalog-owned default-variant operation on an existing sparse product that **reuses** the existing directory-level
`ClearDefaultFlags` + `SetDefault(true)` invariant, without applying the cartesian matrix and without adding, removing
or archiving any variant or axis — and a seed change making `PHONE-BLK-128` the explicit default before publish.

## Delivered

- `IProductVariantDirectory.SetDefaultAsync(productId, variantId, ct)` (+11) — new port member; sparse-safe, idempotent,
  fail-closed (`catalog.product.missing`, `catalog.variant.default.missing`, `catalog.variant.default.archived_forbidden`
  — all pre-existing, registered and localized codes; no Contracts change).
- `ProductVariantDirectory.SetDefaultAsync` (+49) — guard → existence → ownership → archived refusal → idempotence →
  `ClearDefaultFlags` + `SetDefault(true)` → `UpdatedAt` → variants-changed history → **one** `SaveChangesAsync`.
  It never calls `ApplyMatrixAsync`, never builds combinations and never creates/archives anything.
- `ICatalogDirectory.SetProductVariantDefaultAsync` (+9) and `CatalogDirectory` facade impl (+7) — the minimum exposure
  the Development seed needs; no admin HTTP route and no frontend action.
- `CatalogAttributeSchemaDevelopmentSeed` (+5/−1) — captures the first created variant and sets it default before
  `PublishProductAsync`; both diagonal variants and both axes (color + storage) preserved; the 105 diagnostic kept.
- `CatalogSparseDefaultVariantTests` (new, `Tooba.Host.Tests`) — 3 focused facts on real `postgres:16-alpine` containers.

## Proof

- Focused tests: **3/3 passed** (`focused-tests-final.txt`; earlier iterations in `focused-tests-run1..3.txt`).
- Negative control: with the seed call disabled, the seed fact fails with `محصول برای انتشار آماده نیست.`
  (`focused-tests-negative-control.txt`) — the exact TB-105/106 symptom, then 3/3 pass again after restore.
- Sparse preservation: 2 variants before and after, identical fingerprints, identical axis definition ids/order, both
  still `Draft`, exactly one default; second seed pass adds no variant and does not oscillate the default.
- Full backend build `src/backend/Tooba.slnx -c Debug`: **0 errors**.
- Architecture guard sweep `--filter "FullyQualifiedName~Architecture"`: 1458 tests, 1415 passed, **43 pre-existing**
  failures, none attributable to this diff (`pre-existing-guard-baseline.txt`).
- Per-claim file+line citations: `source-line-evidence.txt`.

## Not proven / deferred

Runtime scratch Host startup is intentionally deferred (forbidden in 108): no live-publish, sellable-offer, storefront
label or commercial-readiness claim. Latest live observation for this product remains TB-106 (`missing=[variants]`,
default count 0). Also flagged, not changed: the seed's pre-existing early-return branch still does not assert a default,
so an already-present default-less demo product would still refuse publish on the next bootstrap.
