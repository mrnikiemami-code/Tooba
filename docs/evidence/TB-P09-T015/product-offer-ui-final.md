# Product / Offer UI final — TB-P09-T015

## Product

Admin Product form fields:

- Unit of Measure (active units only for new assignment)
- Decimal Places
- Quantity Step optional (empty = null)

Places = 0 → integer UX. Places > 0 → fractional input.
Variant has no duplicate Unit / Places / Step.

## Offer

Seller offer form:

- MinQuantity / MaxQuantity optional decimals
- Product unit displayed (`offer-product-unit`)
- No Unit selector
- No DecimalPlaces / Step duplicate

Weighted fixture: Arman offer `01a030d1-40f1-7000-95f6-b8efc58e2619` Min=0.50 Max=20.
Kg offer `01a03826-9936-7000-b499-ff26a6123a8c` Min/Max set to 0.50 / 20.00 for E2E.
