# Multi-seller

- Projection exposes `sellerCount` and `maxSellerPreparationDays`.
- Formula uses MAX prep across distinct SellerPartyIds (unit-tested with 1d vs 4d sellers).
- **R1 runtime E (PASS):** real 2-seller cart (KG prep 1d + Arman prep 3d) → `sellerCount=2`, `maxPrep=3`, min delivery `today+5` with `post:express` lead 2. See `r1-multiseller-*.md`.
- No Admin package concepts exposed on Storefront.
