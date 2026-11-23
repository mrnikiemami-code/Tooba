# Design — offer-stage3-shipping-assignment-289

Grounding: see `docs/opencode/tasks/TB-OFFER-STAGE2-CLOSE-STAGE3-SHIPPING-289/stage3-design/STAGE3-ARCHITECTURE.md`.

## Ownership
- Offer: assignment persistence (`SellerOffer.FulfillmentResponsibility`, `FulfillmentShippingTypeId`); command
  `AssignOfferFulfillmentCommand`; read port `IOfferFulfillmentAssignmentLookup`.
- Shipping: method catalog/publication/rates/windows/destinations (`IShippingTypeCatalogReader`,
  `IShippingSelectedCapturedRevisionDirectory`, `IShippingAuthoringDestinationFactsReader`).
- Order: checkout grouping/eligibility (`StorefrontShippingService`, `CartShippingGroupPlanner`,
  `CheckoutShippingGroupGoodsFactsContracts`, captured-group projection).

## Flow
Admin assigns service to Offer → Offer validates against Shipping's published catalog → Checkout resolves, per
(seller × destination) group, the intersection of member offers' assigned services that are currently published and
valid for the destination/window; unassigned/foreign/unpublished contribute nothing.

## Fail-closed rules
No default method; missing assignment ⇒ unavailable; unpublished/expired/foreign ⇒ excluded; group with empty
intersection ⇒ ineligible (DG-01); never infer responsibility/ownership from seller id, weight or name.

## Editions
SingleStore and Marketplace share the same resolution; only the publication scope (store vs per-seller) differs via
the existing commerce context. No edition-specific invention of methods.

## Decision gates (unresolved business policy — do not implement until decided)
DG-01 no-common-method (ineligible vs split); DG-02 warehouse vs seller source; DG-03 fee/threshold/payer;
DG-04 destination granularity; DG-05 window/cutoff/preparation; DG-06 cardinality (single vs ordered list);
DG-07 rate-row selection. See STAGE3-DECISION-GATES.md.

## Non-goals
No schema broadening beyond an approved additive change; no permission broadening; no change to 287 fingerprint,
Inventory policy, AMSC, Cursor, root AGENTS; no Stage-3 product code before architect Stage-2 ACCEPT.
