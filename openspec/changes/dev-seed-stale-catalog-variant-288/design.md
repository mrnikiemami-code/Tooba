# Design — dev-seed-stale-catalog-variant-288

## Context
`CatalogDemoResetService.ResetAsync` removes demo products and their variants (`_catalog.Variants.RemoveRange`).
A later `reset-and-seed` regenerates variants with NEW ids. Offers are not owned by Catalog and survive.
`OfferDevelopmentSeedGateway.EnsureActiveCloneFromAnyActiveAsync` previously returned any surviving Offer
matching `SellerSku` with no variant validity check, so the OOS seed Offer kept a dangling `CatalogVariantId`.
On the next bootstrap `MerchandisingCampaignDevelopmentSeed.EnsureZeroStockAsync` calls
`InventoryDirectory.OpenPositionAsync`, whose `FindVariantAsync` returns null ⇒ `inventory.catalog_variant.missing`.

## Decision
Ownership stays with the Offer module (it owns Offer writes) using the existing cross-module contract
`ICatalogVariantLookup` (already referenced by Offer.Infrastructure and used elsewhere).

- Reuse a surviving seed Offer only if `ICatalogVariantLookup.FindVariantAsync(existing.CatalogVariantId)` is non-null.
- Otherwise select a live target variant: prefer `GetPublishedVariantIdsAsync()`; fall back to an Active template
  Offer whose variant still exists. If none, return null (unchanged "no template" behaviour).
- Remove the stale marked dev-seed row and recreate it on the live variant (exactly one Offer per SellerSku).
  This is bounded rebuild of marked dev-seed data only.

## Alternatives rejected
- Bypassing/relaxing Inventory's `CatalogVariantMissing` guard: forbidden (would hide real integrity defects).
- Disabling Development seeding wholesale or mass catalog reset: forbidden.
- Changing the domain aggregate to mutate `CatalogVariantId` in place: variant identity is immutable by design.
- Cross-domain direct table writes in product code: forbidden.

## Invariants preserved
- Inventory fails closed on invalid variants (guard untouched).
- No duplicate Offer per `SellerSku`; second bootstrap run reuses the rebuilt row (idempotent).
- No 287 fingerprint, public API, schema, or Pricing/Inventory policy change.

## Risks
- Deleting the stale row leaves soft references (inventory positions / campaign memberships). Mitigated: this is
  marked dev-seed data; the Promotion seed rebuilds campaign membership with the new Offer id each run and the
  rebuilt row is stable thereafter.
