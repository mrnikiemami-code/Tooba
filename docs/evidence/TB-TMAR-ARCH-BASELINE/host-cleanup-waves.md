# Host Cleanup Waves

## Wave 1 — Dangerous writes (Critical)

Candidates: StoreLandingPageComposer, StoreMenuComposer, StoreAppearance*, Checkout*Settings endpoints, Hold/Reservation policy endpoints, CatalogDemo seeds (dev), UnitOfMeasure/Quantity settings, many Admin composers with SaveChanges.

Destination: owning module Directory/Command handlers (Catalog→later StorefrontSettings/PageComposition, Order/Payment for checkout settings, etc.)

Pattern: Stage A Handler → Directory; then Directory owns persistence.

Risk: High · Effort: L · Tests: focused write APIs + architecture allowlist · Rollback: feature flag / revert PR

## Wave 2 — Business decisions (Critical/High)

Candidates: StorefrontPrimaryOfferResolver, StorefrontComposer offer selection, campaign eligibility in Host, inventory availability math in Host.

Destination: Pricing / Inventory / Offer / Promotion query contracts.

Risk: High · Effort: L · Tests: PDP/PLP parity · Rollback: keep Host adapter calling new gate

## Wave 3 — Read-side contracts (High)

Candidates: ProductWorkspaceComposer, Admin grids, StorefrontComposer multi-DbContext reads.

Destination: I*ReadGateway in Contracts; in-process adapters first.

Risk: Medium · Effort: L · Tests: composition parity · Rollback: dual-run adapters

## Wave 4 — Host simplification (Medium)

Host retains transport/auth/middleware/DI/endpoints/serialization/minimal view mapping only.

Risk: Medium · Effort: M · Tests: smoke + architecture · Rollback: N/A incremental
