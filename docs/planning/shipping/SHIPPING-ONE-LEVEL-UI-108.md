# SHIPPING-ONE-LEVEL-UI-108 — Narrow sparse-safe default-variant seam (Option A)

Task: `TB-SHIPPING-ONE-LEVEL-TABS-108` (Parent: `TB-SHIPPING-ONE-LEVEL-TABS-107`)
Mode: `BOUNDED_CATALOG_SPARSE_DEFAULT_VARIANT_SEAM`
Stop-State: `STOP_FOR_ARCHITECT` · `automaticNextImplementationTask: NONE`
Expected-Base-SHA: `1ff62762ba7679418db53208f47bb2ee6d412bc5` · Base-SHA / HEAD at claim: `9f8ee6c6a0e3c8767ddb86f734db30733583c756`

## Verdict

**PASS (source + focused tests only).** The Architect-authorized Option A is implemented: one narrow Catalog-owned
default-variant operation that reuses the directory's existing `ClearDefaultFlags` + `SetDefault(true)` invariant,
touches exactly one sparse product, applies **no** cartesian matrix, and creates, archives or moves nothing. The
Development attribute-schema seed now designates the first of its two created variants (`PHONE-BLK-128`) as default
before publishing. Runtime scratch proof was intentionally deferred to a separate task.

## What changed (5 files, 80 insertions, 1 deletion)

```text
IProductVariantDirectory.SetDefaultAsync(productId, variantId, ct) : Result   // Catalog.Application port   (+11)
  ProductVariantDirectory.SetDefaultAsync(...)                                // Catalog.Infrastructure    (+49)
ICatalogDirectory.SetProductVariantDefaultAsync(productId, variantId, ct)     // facade contract            (+9)
  CatalogDirectory.SetProductVariantDefaultAsync(...) -> Unwrap(...)          // facade impl                (+7)
CatalogAttributeSchemaDevelopmentSeed: capture first variant + set it default // Development seed         (+5/-1)
CatalogSparseDefaultVariantTests.cs (new)                                     // focused tests (3 facts)
```

`SetDefaultAsync` reuses the existing private helpers verbatim — `ClearDefaultFlags(variants, now)` followed by
`target.SetDefault(true, now)` — the same pair the matrix reconcile uses, plus the same `_guard.EnsureCanMutateAsync`,
the same `product.UpdatedAt` touch and the same `QueueProductHistory(EventVariantsChanged / SectionVariants)` path, so
no new invariant, no duplicate uniqueness algorithm, no generic framework and no new error code were introduced.

Ordering inside the method is deliberately: guard → product existence → variant must belong to the product → archived
target refused → idempotence check → single `SaveChangesAsync`. Because the rejection checks precede any mutation, a
refused call cannot leave a partial write. Because `ClearDefaultFlags` runs before `SetDefault(true)` inside one
tracked change set saved by one `SaveChanges`, a successful call can never leave two defaults or none. The method is a
true no-op (no write, no history entry) when the requested variant already carries the only default flag, so repeat
seed passes cannot oscillate the default.

Failures reuse existing idioms: `catalog.product.missing` (NotFound), `catalog.variant.default.missing` (Business) for
missing, mismatched or foreign-product variants, and `catalog.variant.default.archived_forbidden` (Business) for an
archived target. All three codes already exist, are registered in the Catalog error catalog and localized in
`CatalogErrors.resx` / `CatalogErrors.fa.resx`; no Contracts, guard or resource file was modified.

`ApplyProductVariantMatrixAsync` is **not** called anywhere in the seed path (verified by review and by the tests: the
variant set stays exactly two).

## Focused tests (real PostgreSQL containers, `Tooba.Host.Tests`)

`CatalogSparseDefaultVariantTests` — 3 facts, **3/3 passed** (`focused-tests-final.txt`):

1. `Sparse_set_keeps_identity_and_gains_exactly_one_default` — a sparse two-variant diagonal (2 axes declared, 2 of 4
   possible combinations). Before the call the existing rule already refuses the product (`IsValid=false`,
   `NoDefaultVariant=true`), reproducing 106's finding at unit level. After the call: exactly one default (the
   requested one), still exactly **2** variants, identical combination fingerprints, identical axis definition ids and
   display order, both variants still `Draft`, `PHONE-A` seam preserved; readiness flips to `IsValid=true` with
   `NoDefaultVariant=false` and empty missing/invalid/duplicate lists. Re-assigning the same variant leaves one default
   and exactly **one** variants-changed history entry (proving the no-op). Switching to the second variant clears the
   first and still leaves exactly one.
2. `Invalid_targets_are_refused_and_leave_state_untouched` — missing product, missing variant, variant belonging to
   another product, and archived variant are all refused with the exact codes above, and afterwards the sparse product
   still has two variants, none default, two axes, and zero variants-changed history entries.
3. `Attribute_schema_seed_publishes_its_sparse_product_with_one_default_and_is_repeat_safe` — runs the **real**
   `CatalogAttributeSchemaDevelopmentSeed.ApplyAsync` against an empty container DB with only `ICatalogDirectory` +
   `CatalogDbContext` registered (the sellable enricher is optional via `GetService`), so it covers the seed's own
   variant defaulting and publish. Asserts `Status == Published`, exactly two variants
   (`PHONE-BLK-128`, `PHONE-BLU-256`), exactly one default = `PHONE-BLK-128`, axes `[color, storage]`, readiness valid,
   and that a **second** seed pass adds no variant and does not oscillate the default.

### Negative control (proves the test actually covers the defect)

With the new seed call temporarily disabled, fact 3 fails with
`InvalidOperationException : محصول برای انتشار آماده نیست.` from `CatalogDirectory.UnwrapLifecycle` — the exact
TB-105/106 symptom. Restored and re-run: 3/3 pass (`focused-tests-negative-control.txt`).

## Guards / build

- `dotnet build src/backend/Tooba.slnx -c Debug` → **Build succeeded, 0 errors**.
- Near-neighbour guard sweep: `--filter "FullyQualifiedName~Architecture"` → **1458 tests, 1415 passed, 43 failed**.
  All 43 failures are **pre-existing repo drift, not caused by this change**, verified two ways
  (`near-neighbour-architecture-guards.txt`, `pre-existing-guard-baseline.txt`): every distinct failure cause points at
  files or counts that are already absent/wrong at `HEAD` (e.g. `CatalogCategoryTreeRules.cs`,
  `CatalogCategorySlugNormalizer.cs`, `CatalogDomain.cs`, `IPartyAdminSellerReadGateway.cs`,
  `IPartyDevelopmentSeedGateway.cs`, `WishlistPresentationComposer.cs` all `ABSENT_AT_HEAD`; Host/Admin file count is
  **17 at HEAD** while several guards still assert 15/52), and none of the 43 failing assertions references any file
  this task modified.

## What is not proven (honest scope)

- **No runtime scratch Host startup** (forbidden in 108): no claim of a real live publish, sellable Offer creation,
  storefront label rendering or commercial readiness. The latest live observation for this product remains TB-106's
  isolated run (`missing=[variants]`, default count 0); the 105 Development diagnostic is left in place for the
  deferred runtime proof.
- No HTTP/admin endpoint, no frontend action, no enricher/offer/pricing change, no readiness-rule weakening, no raw EF
  write in the seed.
- The seed's pre-existing early-return branch (when the demo product already exists) is unchanged and still does not
  assert a default; on a database that already holds a default-less demo product the next bootstrap publish would still
  be refused. Flagged for the Architect, not changed here (out of the mission's "its two created variants" scope).
