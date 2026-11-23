# STAGE 3 ARCHITECTURE — Offer shipping-service assignment on Offer and Checkout

Status: DESIGN (source-grounded). NOT implemented. No product Stage-3 change before architect Stage-2 ACCEPT.
Roadmap: `docs/planning/offer/TOOBA-SELLER-OFFER-ARCHITECTURE-AUDIT-AND-ROADMAP-2026-11-22.md` (Stage 3).

## Goal
Per-offer shipping *service* assignment (not just responsibility), resolvable by shop / destination / time window,
surfaced to Checkout with **no phantom methods**, fail-closed on unassigned/foreign/unpublished, and correct in both
SingleStore (Store) and Marketplace editions.

## Existing real surfaces (grounding, current source)
- Offer already stores an explicit fulfillment assignment: `SellerOffer.FulfillmentResponsibility`
  (`Tooba.Offer.Domain.ValueObjects.FulfillmentResponsibility` = Seller|Platform) and `FulfillmentShippingTypeId`.
  Contract: `OfferFulfillmentAssignmentSnapshot(OfferId, SellerPartyId, Responsibility?, ShippingTypeId?)`,
  read port `IOfferFulfillmentAssignmentLookup.GetAssignmentsAsync(offerIds)` (batch; missing id ⇒ consumer must
  refuse, never default). Write: `PUT /v1/admin/offers/{offerId}/fulfillment` → `AssignOfferFulfillmentCommand`.
- Shipping owns authoring/publication: methods, group rate rows, weekly schedule, destinations, publication, and a
  selected captured revision (`IShippingSelectedCapturedRevisionDirectory`, `IShippingTypeCatalogReader`,
  `IShippingAuthoringDestinationFactsReader`). Shipping already models destination + time window + rate groups.
- Order/Checkout consumes shipping via `StorefrontShippingService`, `CartShippingGroupPlanner`,
  `CheckoutShippingGroupGoodsFactsContracts`, captured-group projection, `StorefrontCapturedSelectionWriter`,
  `ShippingGroupGoodsFactsBuilder`. Grouping is per destination/seller.

## Proposed Stage-3 contract (to be finalized by architect matrix)
### Offer side (Offer owns)
- Extend the existing fulfillment assignment to bind a **real Shipping method/revision reference** per offer
  (already have `ShippingTypeId`; Stage 3 adds per shop/destination/window eligibility *service* selection), without
  inventing a new parallel model. Candidate DTO addition (design only):
  `AssignOfferShippingServiceCommand(OfferId, SellerPartyId, ShippingTypeId, Mode)` validated against Shipping's
  published catalog (via a Shipping read contract), refusing unpublished/foreign/expired methods.
- Read: extend `IOfferFulfillmentAssignmentLookup` snapshot with the assigned service id + publication/eligibility
  facts needed by Checkout (still batch, still fail-closed on missing).

### Checkout side (Order owns)
- `StorefrontShippingService`/`CartShippingGroupPlanner` resolve, for each shipping group (seller × destination),
  the set of methods common to ALL member offers. Offers with no assigned service, or whose assigned service is
  unpublished/expired/foreign to the seller, contribute nothing ⇒ group may become ineligible (never a phantom).
- Multi-offer group with no common method ⇒ group ineligible with a typed reason; do not fall back to a default.
- SingleStore (single seller/store) vs Marketplace (multi-seller): grouping key and eligibility identical; the
  difference is only which seller/tenant scope the Shipping publication lives in (resolved by the existing
  commerce context), never a code branch that invents a method.

## Ownership & boundaries
- Offer owns assignment persistence; Shipping owns method catalog/publication/rate/window/destination truth; Order
  owns checkout grouping/eligibility. Cross-module reads via Contracts ports only; no cross-domain table writes.
- No schema broadening beyond an explicit, additive, architect-approved Stage-3 migration; no permission broadening;
  no change to 287 fingerprint, Inventory policy, AMSC, Cursor, root AGENTS.

## Invariants (acceptance-critical)
1. No phantom method: a method appears in Checkout only if (a) assigned to the offer, (b) published & currently valid
   for the destination/window, (c) owned by the offer's real seller (Marketplace) / store (SingleStore).
2. Unassigned offer ⇒ unavailable (typed), never a default method.
3. Foreign/unpublished/expired method ⇒ excluded, and if it leaves a group with no common method ⇒ group ineligible.
4. Two sellers, two same-type methods ⇒ each group shows only its seller's method; never cross-seller leakage.
5. Two same-type methods for the same seller ⇒ both eligible (no dedupe by type alone).
6. SingleStore vs Marketplace produce the same eligibility semantics from their own publication scope.

## Integration test plan (finite; see STAGE3-ACCEPTANCE-MATRIX.json)
Connected HTTP/DB/Playwright: assign → checkout eligibility read; unassigned exclusion; unpublished/foreign exclusion;
two same-type methods; two sellers; no-common-method multi-offer; destination/window; SingleStore vs Marketplace;
fee/threshold ONLY if an approved shipping policy decision exists (else BLOCKED_ON_DECISION).
