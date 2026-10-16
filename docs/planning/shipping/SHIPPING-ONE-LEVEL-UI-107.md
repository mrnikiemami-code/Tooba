# SHIPPING-ONE-LEVEL-UI-107 — Default-variant seam for the Catalog schema demo seed

Task: `TB-SHIPPING-ONE-LEVEL-TABS-107` (Parent: `TB-SHIPPING-ONE-LEVEL-TABS-106`)
Mode: `BOUNDED_CATALOG_DEMO_DEFAULT_VARIANT_REPAIR`
Stop-State: `STOP_FOR_ARCHITECT` · `automaticNextImplementationTask: NONE`
Expected-Base-SHA: `aceeb46acbfc89024e2def182da3ade73d20714a` · Base-SHA / HEAD: `995e485d23216e34320374002812cdc0c1b5913a`

## Verdict

**BLOCKED_DEFAULT_VARIANT_APPLICATION_SEAM** — the required repair cannot be made with an existing Catalog operation
without expanding the demo dataset (2 → 4 variants and offers) or duplicating/bypassing the single-default invariant.
No source file was modified; the deliverable is the evidence and the proposed smallest adaptation.

## The one-paragraph reason

106 observed `missing=[variants]` and the scratch SQL showed the real cause: the Development attribute-schema seed
creates its two variants with `CreateVariantAsync`, which never marks a default, and publish readiness requires
`noDefault != true`. The only Catalog operation that enforces single-default is `ApplyProductVariantMatrixAsync`, and
it is a **whole-product cartesian reconcile**: it cross-multiplies each axis' options, replaces the axis set and
archives every active variant outside the desired fingerprint set. The seed's variant set is a **sparse diagonal**
(2 axes × 2 options declared, only 2 variants created on purpose), and no cartesian product equals that set. Choosing
both axes' options yields the 4-combination full matrix (2 new variants, and — through the enricher's one-offer-per-
variant loop — 4 offers instead of 2); choosing one axis archives an existing variant and drops the other axis;
choosing one combination archives the other variant. The domain `CatalogVariant.SetDefault` does not enforce
uniqueness by itself ("uniqueness is enforced in the directory"), so using it from the seed would either allow two
defaults or re-implement the invariant — both forbidden.

## Recommended smallest adaptation

Add one narrow Catalog-owned seam that **reuses** the existing invariant instead of re-creating it:

```text
IProductVariantDirectory.SetDefaultAsync(productId, variantId, ct) : Result        // Catalog.Infrastructure impl
  -> reuse existing private ClearDefaultFlags(variants, now) + target.SetDefault(true, now)
  -> same use-case guard / actor / product-history path as ApplyMatrixAsync
ICatalogDirectory.SetProductVariantDefaultAsync(productId, variantId, ct)          // facade
  optional: POST /v1/admin/catalog/products/{productId}/variants/{variantId}/default // Admin gap closure
```

Then the seed calls it for the first created variant (`PHONE-BLK-128`) before publishing. No reconcile, readiness,
publish, lifecycle or enricher semantics change; the seed keeps exactly two variants.

Alternative (Option B, needs explicit authorization): let the seed use the canonical reconcile and accept 4 variants /
4 offers. Rejected alternative (Option C): seed-side `SetDefault` + manual clearing (duplicates the invariant, needs
direct EF writes).

## Evidence

- `docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-107/source-line-evidence.txt` — every claim with file+line
  citations, including the repo's own matrix test that pins cartesian behaviour.
- `.../seam-analysis-and-blocker.txt` — requirement conflict, rejected options, proposed adaptation.
- `.../shared-and-cleanup-facts.txt`, `.../shared-parity-preflight.*`, `.../shared-parity-postflight.*` — shared state
  unchanged, nothing to clean.
- `.../summary.md` — verdict summary.

## What is not proven

No live scratch confirmation of a repair (`LIVE_SCRATCH_NOT_VERIFIED`, nothing new to observe while blocked); no
sellable-offer, storefront-label or checkout claim. TB-106's isolated run remains the latest live evidence for this
product and the latest observed default count is **0**.
