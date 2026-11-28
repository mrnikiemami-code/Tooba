# TB-296 Offer Shipping Method Binding — Preflight / Status

Status: **BLOCKED — IMPLEMENTATION NOT EXECUTED.** No product code, schema or migration was changed. Owner design remains approved.

## Base / identity
- Implementation base HEAD `41c7b9c5b534e6727d2ef75b112b3f256529ae54`; approved presentation `21641c484446a62c0d0a980f0d4b9976e4d396ec`; preview `/fa/design/offer-shipping` verified at `SHIPPING-DESIGN-VERIFICATION.md`.
- Existing Host 5088 / Next 3000 / `tooba_alpha` available; no new Host/DB.

## Preflight source map (read-only)
- Offer fulfilment assignment: `OfferFulfillmentAssignment*`, `AssignOfferFulfillmentCommand`, `OfferFulfillmentAdminEndpoints` (`fulfillment.manage`), migration `20261027120000_AddOfferFulfillmentAssignment`.
- Shipping authority lives in `Tooba.Shipping.Contracts` (`Authoring/ShippingAuthoringPublicationContracts`, `Authoring/ShippingAuthoringDraftContracts`); publication `Selected` is not runtime eligibility.
- Checkout eligibility/capture seams: `Order/Storefront/Shipping/...` (`ProjectStorefrontShippingQuery`, `StorefrontShippingOwnedRateResolver`, `StorefrontCapturedSelectionWriter`).
- Do not equate Fulfillment `ShippingServiceId` with Shipping authoring method identity; a stable owner-scoped read seam must be proven before writing links.

## OpenSpec frozen
- `openspec/changes/offer-shipping-method-binding-296-r12` (proposal/design/tasks/spec): REQ-REF/STORE/AUTH/CAS/ELIG/UI/REG mapped to SHIP-01..26; strict validation valid.

## Not executed (requires full implementation goal)
- W2 Offer model/DB/migration `offer.offer_shipping_methods`, contracts, command/validator/query, history, endpoints, typed errors.
- W3 approved shipping-tab integration through typed adapters/CAS/draft.
- W4 storefront eligibility/selection/checkout/group intersection.
- W5 migration + clean builds/restart.
- W6 SHIP-01..26 connected execution, affected Offer/Shipping/Fulfillment/Cart/Order suites, AQ-01..08, ledger/machine gate.
- No PASS is claimed for any SHIP ID; all remain NOT_RUN.

## Next
Resume from this preflight in a single implementation goal: implement W2–W6, apply the additive migration only after preflight identity/backup checks, and execute the 26 immutable IDs. Architect owns acceptance.
