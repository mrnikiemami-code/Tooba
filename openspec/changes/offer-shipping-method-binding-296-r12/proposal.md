# Proposal — offer-shipping-method-binding-296-r12

## Why
Offer shipping currently persists fulfilment responsibility and shipping type, but cannot bind an offer to the existing market shipping methods. Owner approved the design preview (`/fa/design/offer-shipping`). This change implements that binding without redesigning Shipping.

## What Changes
- SHIP-01..03: owner/type-filtered, searchable/paged read of the authoritative published active Shipping methods with localized display and stable identity.
- SHIP-04..08: Offer-owned `offer.offer_shipping_methods` link rows, Automatic default for legacy, Specific explicit set, atomic save with Offer revision CAS and history.
- SHIP-09..11: admin `fulfillment.manage`; seller self-fulfilled own-method restriction only; no cross-owner/forged/other-type/inactive/unpublished method.
- SHIP-12..14: CAS, identical retry reconciliation, stale-intent rejection, no partial writes.
- SHIP-15..20: cart/quote/checkout eligibility intersects allowlist with actual candidates; group intersection; stale IDs retained with warning; captured orders untouched.
- SHIP-21..23: reuse approved shipping TSX and Design System; registry locale/RTL; states and 390×844 geometry.
- SHIP-24..26: regressions, architecture gates, OpenSpec strict and acceptance verifier.

## Impact
Offer (domain/infrastructure/contracts/endpoints/read model), Shipping public Contracts (read-only lookup seam if required), Order storefront eligibility/capture integration, shared Offer workspace adapters/UI, one additive Offer migration. No Shipping engine redesign, no new Host/DB, no unrelated tabs.

## Authority
Owner design approval 2026-11-28; implementation base HEAD `41c7b9c5b534e6727d2ef75b112b3f256529ae54`; approved presentation `21641c484446a62c0d0a980f0d4b9976e4d396ec`. TASK.md and ACCEPTANCE-MATRIX.json (26 IDs) in `docs/opencode/tasks/TB-296-OFFER-SHIPPING-METHOD-BINDING/`.
