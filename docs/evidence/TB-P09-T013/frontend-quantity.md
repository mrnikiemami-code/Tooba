# Frontend quantity

- Product workspace: Unit / DecimalPlaces / optional Step (`product-edit-unit`).
- Offer: decimal Min/Max.
- Admin settings: GlobalRoundingMode.
- Cart/PDP: `inputMode=decimal`, `parseQuantityInput` (no parseInt). Step=null does not set HTML step.
- `formatQuantityDisplay` strips `.000000`.
- Unit labels from Host multilingual UoM, not hardcoded FA/EN strings.
- RTL/LTR preserved (`dir=ltr` on numeric fields only).
