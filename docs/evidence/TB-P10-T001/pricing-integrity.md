# TB-P10-T001 — Pricing integrity

- Product card / PDP display amounts are Offer/Pricing presentation only.
- ATC body never sends a trusted final price — only `offerId` + `quantity`.
- `/cart` and mini-cart render `unitAmountExclusiveOfTax`, `lineAmountExclusiveOfTax`, `subtotalExclusiveOfTax` from Host projection.
- Coupon discount shown only after `POST /v1/storefront/checkout/preview` returns Host `discountAmount`.
- Hero discount % is derived from Host coupon discount when present; otherwise 0 (not fabricated list discounts).
- No Rial/Toman silent conversion; `formatOfferAmount` uses Host currency.
- Shipping cost on cart remains non-authoritative label «در تسویه» / computed at shipping stage — never fabricated free shipping.
