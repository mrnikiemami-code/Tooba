# Multi-seller

- Projection exposes `sellerCount` and `maxSellerPreparationDays`.
- Formula uses MAX prep across distinct SellerPartyIds (unit-tested with 1d vs 4d sellers).
- Runtime E: single-seller cart (LIVE-A) still applies prep+lead; no Admin package concepts exposed.
