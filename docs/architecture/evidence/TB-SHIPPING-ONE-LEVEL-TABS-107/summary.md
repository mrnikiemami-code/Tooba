# TB-SHIPPING-ONE-LEVEL-TABS-107 — summary

**Status: BLOCKED_DEFAULT_VARIANT_APPLICATION_SEAM** — no safe existing Catalog operation can set exactly one default
on the Development seed's sparse two-variant set. No source file was changed.

## Why

The Architect asked for a single default variant chosen through an existing operation that enforces single-default,
while preserving the seed's two variants. In the current Catalog surface those requirements cannot both hold:

- the **only** operation that enforces single-default is `ProductVariantDirectory.ApplyMatrixAsync`
  (`ProductVariantDirectory.cs:410-447`, helpers at `999-1024`), reachable through
  `ICatalogDirectory.ApplyProductVariantMatrixAsync` / Admin `PUT /variants/apply`;
- that operation is a **whole-product cartesian reconcile**: option lists are cross-multiplied (`870-905`),
  `ProductVariantAxes` are replaced (`324-329`) and every active variant outside the desired fingerprint set is
  **archived** (`372-380`, archive at `379`) — behaviour the repo's own test pins (`ProductVariantMatrixTests`
  150-230, 261-290);
- the seed deliberately declares 2 axes x 2 options but creates only **2** variants, a sparse diagonal
  (`CatalogAttributeSchemaDevelopmentSeed.cs:89, 91-93`, comment «FULL_VARIANT_MATRIX تولید نمیشود»).

Expressible desired sets on this product are therefore {1, colour-only-2, storage-only-2, 4}; none equals the two
existing fingerprints. Using both axes' options creates 2 extra variants (the full matrix), and because
`CatalogAttributeSchemaSellableEnricher` creates one offer + price + tax + stock per variant (`71-140`), the demo
dataset would silently grow from 2 variants/2 offers to 4/4 — outside this task's "not to create offers" intent.
Using one axis would archive an existing variant and drop the other axis. Calling the domain
`CatalogVariant.SetDefault` alone would break the single-default invariant, and clearing the other flags in the seed
would duplicate `ClearDefaultFlags`/`EnforceSingleDefault` (forbidden) or require direct EF writes (forbidden).

## Observed facts used

TB-106's isolated scratch database (read-only): `PHONE-BLK-128` and `PHONE-BLU-256` both have `is_default = false`
→ observed default count = **0**; readiness refused with `missing=[variants]` (`NoDefaultVariant = true`).

## Recommended smallest adaptation (Architect decision)

**Option A (recommended):** add one narrow Catalog-owned seam that reuses the existing invariant —
`IProductVariantDirectory.SetDefaultAsync(productId, variantId)` (implemented with the existing private
`ClearDefaultFlags` + `SetDefault(true)` and the same guard/history path), exposed on `ICatalogDirectory` and
optionally as `POST /v1/admin/catalog/products/{productId}/variants/{variantId}/default` (the Admin surface has no way
to express "make this variant the default" for a sparse matrix today). The seed then marks the first created variant
(PHONE-BLK-128) as default before publishing. No reconcile, readiness, publish or enricher semantics change.

**Option B (no new API, needs explicit authorization):** switch the seed to the canonical matrix reconcile, accepting
a full 4-combination matrix, 4 variants and 4 offers instead of 2.

**Option C (rejected):** seed-side `SetDefault` with manual clearing — duplicates the invariant, needs EF writes.

## Bounds honoured

No `src/**`, `scripts/`, `appsettings`, migration or test change. No isolated scratch startup (nothing new to observe
once the repair is blocked) → `LIVE_SCRATCH_NOT_VERIFIED`. No process started/stopped/killed; shared host pid 35624
left running with its gate unchanged; `:5088` `/health` 200 and provenance identical; `tooba_alpha` counts identical
(7 methods / 14 translations / 50 carts / 78 lines / 14 drafts / 26 seller orders / 567 products / 3223 offers);
`.tmp-10*` absent; containers still only `postgres-db`, `rabbitmq`.

## Rollback

Revert the evidence-only commit for this task. No code, schema, configuration or data change exists to roll back.
