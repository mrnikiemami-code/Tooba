# Proposal — offer-stage3-shipping-assignment-289

## Why
Offers carry an explicit fulfillment responsibility (Seller|Platform) and a shipping type id, but Checkout does not
yet resolve the concrete shipping **service** an offer is eligible for per shop/destination/window. Without this, a
group can present phantom or cross-seller methods, or silently default. Stage 3 makes shipping-service assignment on
the Offer drive Checkout eligibility truthfully in both SingleStore and Marketplace.

## What Changes (design; no product code before architect Stage-2 ACCEPT)
- Offer: extend the existing `PUT /v1/admin/offers/{offerId}/fulfillment` assignment to bind a real, published
  Shipping method/revision owned by the offer's seller/store (reusing `OfferFulfillmentResponsibility` +
  `FulfillmentShippingTypeId`; no parallel model).
- Offer read: extend `IOfferFulfillmentAssignmentLookup` snapshot so Checkout can resolve eligibility in batch,
  fail-closed on missing/unassigned.
- Order/Checkout: `StorefrontShippingService`/`CartShippingGroupPlanner` resolve only the intersection of methods
  assigned to all member offers, validating published/destination/window/ownership; no phantom, no default, no
  cross-seller leakage; groups without a common method are ineligible (DG-01).
- Shipping remains the sole owner of method catalog/publication/rates/windows/destinations.

## Capabilities
### New Capabilities
- `offer-shipping-assignment`: an offer's assigned shipping service(s) determine checkout-eligible methods per
  shop/destination/window, fail-closed.
### Modified Capabilities
<!-- Offer fulfillment assignment extended; Shipping/Order semantics clarified, not owned here. -->

## Impact
Offer (assignment command/DTO/read port), Order (checkout shipping resolution), Shipping (read contracts only).
Additive, architect-approved migration only; no permission broadening; no 287/Inventory/AMSC/Cursor/AGENTS change.
Open business policy (see STAGE3-DECISION-GATES.md) must be resolved before product code.
