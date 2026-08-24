# TB-P04-GATE — Architecture Invariant Summary

| Invariant | Status | Evidence |
| --- | --- | --- |
| Product ≠ Variant ≠ Offer | HOLD | Catalog Product has no commercial Price/Stock fields; Offer amount + Inventory units compose presentation |
| Offer ≠ Price / Pricing ≠ Promotion ≠ Tax | HOLD | Storefront payable uses Order/Tax totals; Promotion not invented in UI |
| Cart ≠ Inventory / Cart ≠ Checkout | HOLD | Cart holds reservations; Checkout creates CheckoutGroup + seller Orders |
| Checkout ≠ Order / Payment ≠ Order | HOLD | Payment module initiates/verifies; Order becomes Paid via projection |
| Payment Provider ≠ Payment domain | HOLD | `fake` sandbox adapter replaceable; secrets server-side |
| Order snapshot ≠ live Product truth | HOLD | Checkout lines snapshot title/amounts at submit |
| Frontend ≠ commercial / payment authority | HOLD | Result page polls Host; no client `Succeeded`/`Paid` invention |
| No Product.Price / Product.Stock | HOLD | Grep + product-workspace tests |
| No cross-module SQL JOIN / foreign ORM nav | HOLD | ArchitectureBoundaryTests still in 128-pass suite |
| No frontend direct DB | HOLD | Next proxies `/v1/storefront` to Host |
| No frontend authoritative totals | HOLD | Cart/Checkout/Payment mappers use Host amounts |
| one CartId → at most one CheckoutGroup | HOLD | Submit creates CheckoutGroup; replay semantics remain server-side (stale cart version after submit returns conflict; first group remains durable) |

Backend architecture tests included in full suite: **128 passed / 0 failed / 0 skipped**.
