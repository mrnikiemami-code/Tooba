# ROOT-CAUSE — TB-DEV-SEED-STALE-CATALOG-VARIANT-288

## Symptom
A normal Development (SingleStore) Host boot, after a Catalog demo `reset-and-seed`, crashes during
`DevelopmentSchemaMigrator.MigrateOrderedAsync → MerchandisingCampaignDevelopmentSeed.EnsureAsync` with
`Tooba.BuildingBlocks.ContractOperationException: inventory.catalog_variant.missing` at
`InventoryDirectory.OpenPositionAsync`. Observed stack committed under
`../TB-SELLER-OFFER-IMMUTABLE-CREATE-INTENT-287/evidence/fixture-crash-normal-boot*.err.log`.

## Proven causal chain (source + observed)
1. **Catalog demo reset deletes demo variants.**
   `CatalogDemoResetService.ResetAsync` → `DeleteProductsAsync`:
   `_catalog.Variants.RemoveRange(variants)` (`CatalogDemoResetService.cs:196`). The later `reset-and-seed`
   regenerates products/variants with **new GUID ids**; the prior variant ids no longer exist.
2. **Offers are not owned by Catalog and survive the reset.** The `offer.offers` rows are untouched by the
   Catalog reset; a seed Offer keeps its old `catalog_variant_id`.
3. **The Offer dev-seed gateway reused a surviving Offer by SellerSku only.**
   `OfferDevelopmentSeedGateway.EnsureActiveCloneFromAnyActiveAsync` (pre-fix) did
   `var existing = ...SingleOrDefaultAsync(x => x.SellerSku == sellerSku); if (existing != null) { activate; return existing.OfferId; }`
   — with **no check that `existing.CatalogVariantId` still exists**. So the OOS seed Offer
   (`SellerSku = DEV-SEED-OOS`) was returned even though its variant had been deleted.
4. **Promotion drains stock on that reused Offer.**
   `MerchandisingCampaignDevelopmentSeed.EnsureOosOfferAsync` → `EnsureActiveCloneFromAnyActiveAsync`
   (`MerchandisingCampaignDevelopmentSeed.cs:457`) → `EnsureZeroStockAsync` → `DrainDevelopmentStockAsync`
   (`:491`) → `InventoryDirectory.OpenPositionAsync` (`InventoryDirectory.cs`).
5. **Inventory fails closed (correct) on the missing variant.**
   `OpenPositionAsync`: `if (await FindVariantAsync(offer.CatalogVariantId) is null) throw ContractOperationException(InventoryErrorCodes.CatalogVariantMissing)`
   (`InventoryDirectory.cs:91`) → `inventory.catalog_variant.missing`. This is the crash.

## Ownership decision
The defect is **Offer-owned**: the Offer Development seed gateway treated a stale seed Offer as valid. Inventory
is behaving correctly (fail-closed). The repair belongs in `OfferDevelopmentSeedGateway` (Offer owns Offer writes),
using the existing cross-module contract `ICatalogVariantLookup`. No Inventory guard weakening, no Catalog reset
change, no domain/API/287 change.

## Reproducible before/after facts
- BEFORE (pre-fix): surviving `offer.offers` row with `seller_sku=DEV-SEED-OOS` and a `catalog_variant_id` that is
  absent from `catalog.variants` ⇒ reused ⇒ Inventory throws `inventory.catalog_variant.missing` on bootstrap.
- AFTER (fix): the gateway reuses the row only when `ICatalogVariantLookup.FindVariantAsync(existing.CatalogVariantId)`
  is non-null; otherwise it rebuilds the marked dev-seed row on a live (preferably published) variant, keeping one
  Offer per SellerSku. Verified by focused tests `OfferDevelopmentSeedGatewayStaleVariantTests` (stale rebuild +
  idempotent second run; valid reuse).

Note: the connected replay of steps 1→5 on a live Host is NOT_RUN in this task (no authorized isolated fixture;
see RESULT.md), but steps 1–5 are individually evidenced by source and by the real 287 crash logs.
