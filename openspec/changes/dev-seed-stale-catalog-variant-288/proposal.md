# Proposal — dev-seed-stale-catalog-variant-288

## Why

After a Development Catalog demo reset, the Promotion-owned `MerchandisingCampaignDevelopmentSeed` crashes the
Host with `inventory.catalog_variant.missing`. The demo reset deletes and regenerates Catalog product variants,
but the Offer-owned Development seed gateway still reuses a surviving Offer solely by matching `SellerSku`
(`DEV-SEED-OOS`), which now points at a deleted variant. Inventory correctly fails closed on the invalid
variant, so the bootstrap cannot complete and the Development Host is not restartable.

## What Changes

- The Offer Development seed gateway SHALL treat a surviving seed Offer as reusable ONLY when the Catalog
  variant it references still exists (via the existing `ICatalogVariantLookup` contract).
- When the referenced variant is stale (or missing), the gateway SHALL rebuild ONLY that marked dev-seed Offer
  on a still-live (preferably published) variant, keeping exactly one Offer per `SellerSku`.
- Inventory's missing-variant guard, Pricing/Inventory policies, public Offer API, Offer 287 fingerprint
  semantics, Host module ownership, and production data are NOT changed.

## Capabilities

### New Capabilities

- `dev-seed-stale-catalog-variant`: Development seed Offers must never be considered valid on a deleted Catalog
  variant; a stale dev-seed Offer is rebuilt on a live variant so the Development bootstrap stays repeatable.

### Modified Capabilities

<!-- None. -->

## Impact

`Tooba.Offer.Infrastructure/Adapters/OfferDevelopmentSeedGateway.cs` (Development-only seed mutation) and a
focused Offer test. No schema, migration, public contract, Inventory domain, Host ownership or live data change.
