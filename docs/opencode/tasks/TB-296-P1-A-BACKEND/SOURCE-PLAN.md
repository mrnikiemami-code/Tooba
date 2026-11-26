# P1-A backend source plan

Parent: PHASE1-EXECUTION-CONTRACT.md. Frozen wire: detail revisions {offer,price,offeredQuantity}, inventoryLocations[].revision, offeredQuantity, capabilities; existing routes preserved; PUT /offers/{id}/offered-quantity; mutation operationId.

## Owning boundaries
- Pricing owns price, expected revision, price facts (pricing.offer_price_changes). SetPrice writes price+fact in one local transaction; idempotent OperationId.
- Inventory owns location owner kind, physical positions, allocation (inventory.offer_allocations), change facts (inventory.offer_inventory_changes), reserve/release/consume and new-sale availability. Unknown owner denies; no null wildcard/default warehouse.
- Offer owns CQRS, DTO/read composition, history merge and capability projection; no second Offer save for price/stock.

## Dependency order
1. Location owner kind + migration + SellerWrite/Provisioning enforcement.
2. Price expected revision/facts; Inventory expected revision/facts; no Offer save for owner mutations.
3. Offered allocation and atomic reserve/release/consume; shared cap per Offer.
4. Offer contracts/routes/validators/capabilities/history merge.
5. Focused builds/tests, adjacent consumers, guards, commit/evidence.

## Backend exit evidence
Actual builds/tests for Offer/Pricing/Inventory; route/validator guards; source checks for revisions, no null grants, allocation SQL, owner transactions. Provider scenarios belong to P1-C; no alternate environment.
