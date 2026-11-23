# Tasks — offer-stage3-shipping-assignment-289 (DESIGN ONLY until Stage-2 ACCEPT)

## 0. Preconditions
- [ ] Architect issues explicit Stage-2 ARCHITECT_ACCEPT.
- [ ] Resolve decision gates DG-01..DG-07 (owner/architect) for any policy-dependent behavior.

## 1. Contracts (Offer)
- [ ] Extend `AssignOfferFulfillmentCommand`/DTO with the shipping service binding per DG-06 cardinality.
- [ ] Validate against Shipping published catalog (read contract only); refuse unpublished/foreign/expired.
- [ ] Extend `IOfferFulfillmentAssignmentLookup` snapshot for checkout resolution (batch, fail-closed).

## 2. Checkout resolution (Order)
- [ ] Resolve per (seller×destination) group the intersection of member offers' valid assigned services.
- [ ] No phantom/default; unassigned/unpublished/foreign excluded; empty intersection per DG-01.
- [ ] Destination (DG-04) and window (DG-05) evaluation from Shipping facts.

## 3. Editions
- [ ] SingleStore parity test; Marketplace multi-seller isolation test.

## 4. Verification
- [ ] Run STAGE3-ACCEPTANCE-MATRIX D01–D13 (policy-gated stay BLOCKED_ON_DECISION until decided).
- [ ] Backend build 0 errors; connected HTTP+DB; Playwright UI/RTL desktop + 360x640 if UI changes.

## 5. Evidence
- [ ] observed JSON + checker + RESULT/RECEIPTS; no fabricated PASS.
