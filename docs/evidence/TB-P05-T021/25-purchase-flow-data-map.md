# 25 — Purchase Flow Data Map

| UI field | Source of truth | Notes |
| --- | --- | --- |
| Line title / seller / image | Cart read model from Host | Catalog display only |
| Unit / line amount | Pricing quote on Offer | Not Product.Price |
| Quantity | Cart line versioned ops | Fail-closed on conflict |
| Cart subtotal | Sum of line exclusives from Host | Tax not claimed at cart |
| Coupon | None (capability gap) | UI disabled; no fake discount |
| Shipping method label | Checkout Host | No multi-carrier invention |
| Recipient / address | Guest fields or AddressBook | Ownership validated on submit |
| Tax / discount / payable | Checkout preview/submit | Tax classification required on Offer |
| Payment state | Checkout/Order Host | Paid never frontend-authored |
| Order reference | Seller order numbers | Snapshot immutable after submit |

Demo tax coverage: `EnsureDemoTaxCoverageAsync` assigns standard IR-NAT 9% rule to `DEMO-*` offers so live Checkout is usable in Development without inventing tax on the frontend.
