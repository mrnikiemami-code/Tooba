# TB-296-P1-A backend result

Readiness: READY_FOR_INTEGRATION (backend candidate; not parent/owner PASS)
Base: 55de59bf8f7fd0db394794017d988fc831cc6de8

## Implemented
- Inventory location owner kind: Store/Marketplace/Seller; null SellerPartyId is not shared access; seller mutation/provisioning requires explicit Seller ownership; no SELLER-DEFAULT auto-warehouse; admin selected-seller location query scoped.
- Pricing owner revision/facts: expectedRevision mandatory for workspace, "absent" only for first price, operationId idempotency, atomic price+fact transaction, actor/old/new/revision receipt, stale/conflict typed 409.
- Inventory physical owner revision/facts: expected revision mandatory, "absent" for first position, operation replay, physical change fact in the same Inventory transaction; platform physical denied to seller; admin flag only on admin route.
- Offered allocation: Inventory-owned offer_allocations.Quantity/Held/Revision; shared cap across all positions of one Offer; atomic reserve/release/consume updates; allocation cannot go below held or above physical; zero stops new sale; allocation change fact.
- Offer wire: detail revisions {offer,price,offeredQuantity}, price/offered absent revisions, inventoryLocations[].revision, offeredQuantity, capability flags, exact PUT /offers/{id}/offered-quantity, operationId on price/inventory/quantity mutations, history merge from durable price/inventory facts.
- No second Offer save for price/inventory owner mutations; no foreign DbContext; no frontend changes.

## Verification
- Host build: PASS (log evidence/host-build.log; warnings are existing XML-doc warnings).
- Offer tests: 133/133 PASS (evidence/offer-tests.log).
- Pricing tests: 13/13 PASS (evidence/pricing-tests.log).
- Inventory tests: 53/53 PASS (evidence/inventory-tests.log).
- OpenSpec strict: PASS with existing long-requirement warnings (evidence/openspec-strict.log).
- Migrations applied to existing tooba_alpha: Pricing 20261126170000, Inventory 20261126160000 and 20261126180000.
- Frontend diff: empty.

## Left to integration task C
- Actual Chromium→existing Host→PostgreSQL provider scenarios, response-correlated concurrency, failure injection between owner commit/projection, cleanup and final 42+10 gate.
- B must consume the frozen revisions/capabilities/offered-quantity route.

No owner UAT; STOP_FOR_ARCHITECT.
