# Eligibility

Backend-authoritative at create:
- Shipment belongs to checkout fulfillments
- Status == Created (not cancelled/dispatched/delivered)
- Not already in another active package (`ReleasedAt IS NULL`)
- Selected set has ≥2 distinct SellerPartyIds
- Shipping method code from `ShippingMethodRegistry`
- No duplicate shipment IDs

Tracking not required at create; dispatch still requires tracking per existing shipment rules (central tracking may seed missing member tracking during orchestration).
