# 20 — Browser evidence (TB-P06-T021)

**Status:** CAPTURED (functional browser proof). Visual USER ACCEPT remains open.

Same commercial transaction as `19-real-product-sale-e2e.md` / `e2e-sale-api.json`:
slug `t021-sale-mtbcb87t`, Offer `01a0429e-e68d-7000-9139-65f062c1c15d`,
Paid seller order `TB-20260827094827-01-8fb91c`.

## Captures

| # | File | URL | Notes |
|---|---|---|---|
| 01 | `captures/01-seller-products.png` | `/vendor-panel/products` | Offer list LIVE; T021 rows price 250,000 + stock |
| 02 | `captures/02-seller-products-new.png` | `/vendor-panel/products/new` | Create Offer form (Catalog RO + price/stock) |
| 03 | `captures/03-seller-offer-detail.png` | `/vendor-panel/products/{offerId}` | Offer detail price/inventory editable |
| 04 | `captures/04-seller-order.png` | `/vendor-panel/orders/{sellerOrderId}` | Order **Paid** `TB-20260827094827-01-8fb91c` |
| 05 | `captures/05-listing-fa.png` | `/fa/products?sort=newest` | Discovery — محصول فروش T021 |
| 06 | `captures/06-pdp-fa.png` | `/fa/products/t021-sale-mtbcb87t` | PDP buy box LIVE |
| 07 | `captures/07-cart-fa.png` | `/fa/cart` | Cart line for T021 after add-to-cart |
| 08 | `captures/08-checkout-fa.png` | `/fa/checkout` | Checkout totals (taxed) |
| 09 | `captures/09-customer-orders.png` | `/customer-panel/orders` | Customer orders include T021 Paid order |
| 10 | `captures/10-admin-orders.png` | `/admin/orders` | Admin order operations surface |
| 11 | `captures/11-pdp-mobile.png` | PDP @ 390×844 | Mobile PDP |
| 12 | `captures/12-shopeiva-home.png` | `http://127.0.0.1:3001/` | Shopeiva reference home |

Index: `browser-proof.json`.

## Side-by-side / fidelity

Touched seller surfaces reuse prior Wave panel shell (Shopeiva-derived). Storefront PDP/cart/checkout preserve locked Shopeiva geometry; bindings live. Full pixel side-by-side matrix not claimed as USER_VISUAL_ACCEPTED.

## Runtime note

FE must listen on default Next host (not `-H 127.0.0.1` only) to avoid `127.0.0.1`↔`localhost` locale redirect loops during capture.
