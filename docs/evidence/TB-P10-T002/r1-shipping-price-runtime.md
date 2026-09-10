# R1 Shipping Price — Runtime (Multi-Seller)

Authoritative quotes from projection Rates (IRR), same multi-seller cart.

| Method | Lead days | Amount |
|---|---|---|
| `post:express` | 2 | **200000** |
| `post:standard` | 4 | **120000** |

Checks:

- Changing method changes quote (`200000` ≠ `120000`)
- Amounts > 0 (no static free fallback)
- Currency `IRR` (no Rial/Toman conversion error observed)
- Client cannot spoof price (selection persists backend `shippingAmount`)

No new allocation model invented; store-level rate applies to the whole multi-seller checkout.

Raw step: `shipping-price` in `r1-runtime-raw.json`.
