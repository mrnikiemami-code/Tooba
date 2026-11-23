# ARCHITECTURE-PLAN — TB-DEV-SEED-STALE-CATALOG-VARIANT-288

## Scope
Root-cause + bounded Development-fixture repair for the stale OOS seed Offer after a Catalog demo reset.

## Owner and boundary
- Owner of Offer writes: `Tooba.Offer.Infrastructure` (the Development seed gateway). Promotion orchestrates but
  must not write Offer tables directly; Inventory keeps its fail-closed guard.
- Cross-module read uses the existing contract `Tooba.Catalog.Contracts.Ports.ICatalogVariantLookup`
  (`FindVariantAsync`, `GetPublishedVariantIdsAsync`). No new cross-module port.

## Change (single method + helper)
`OfferDevelopmentSeedGateway.EnsureActiveCloneFromAnyActiveAsync`:
1. If a surviving Offer matches `SellerSku` AND `ICatalogVariantLookup.FindVariantAsync(existing.CatalogVariantId)`
   is non-null → activate and reuse (unchanged behaviour for valid rows).
2. Else resolve a live target variant: prefer `GetPublishedVariantIdsAsync()`; fall back to an Active template
   Offer whose variant still exists; if none → return null (unchanged "no template" behaviour).
3. Remove the stale marked dev-seed row and recreate it on the live variant (exactly one Offer per SellerSku).

Constructor gains `ICatalogVariantLookup` (already registered by the Catalog module; Offer.Infrastructure already
references `Tooba.Catalog.Contracts`).

## Invariants
- Inventory `CatalogVariantMissing` guard unchanged (fail-closed preserved).
- No duplicate Offer per SellerSku; second bootstrap reuses the rebuilt row (idempotent).
- No 287 fingerprint/public API/schema/Pricing/Inventory/Host/Cursor/AGENTS change.

## Verification strategy (finite)
- SOURCE_VERIFIED: root cause (source trace + 287 crash evidence); Inventory guard unchanged.
- Focused test: stale-variant rebuild + idempotent second run + valid reuse (`Offer.Tests`).
- Build: Host graph 0 errors.
- Connected S02/S03/S05 require a destructive reset on an isolated fixture; none is authorized and reprovision is
  forbidden → NOT_RUN (BLOCKED), never fabricated PASS.
