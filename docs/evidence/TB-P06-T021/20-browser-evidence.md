# 20 — Browser evidence (TB-P06-T021)

**Status:** PLACEHOLDER for capture artifacts (screenshots / CDP / side-by-side JSON).

## Required captures (same transaction as `19`)

### Seller

- [ ] Product/Offer list `/vendor-panel/products`
- [ ] Create Offer `/vendor-panel/products/new`
- [ ] Offer detail with price + inventory `/vendor-panel/products/{offerId}`
- [ ] Resulting order `/vendor-panel/orders/{sellerOrderId}`
- [ ] Fulfillment / tracking `/vendor-panel/fulfillments/{id}`

### Storefront (desktop + mobile for presentation-critical)

- [ ] Discovery / listing `/fa/products`
- [ ] PDP `/fa/products/{slug}`
- [ ] Cart `/fa/cart`
- [ ] Checkout `/fa/checkout`
- [ ] Payment result `/fa/payment/result`

### Customer

- [ ] Order detail + tracking `/customer-panel/orders/{checkoutId}`

### Admin

- [ ] Order and/or fulfillment operational view

### Shopeiva side-by-side

- [ ] Original Shopeiva Vendor products vs Tooba vendor products (touched UI)
- [ ] Original Shopeiva PDP / cart / checkout vs Tooba (touched public UI)

## Artifact paths (fill when saved)

| Artifact | Path |
|---|---|
| Browser proof JSON | `docs/evidence/TB-P06-T021/browser-proof.json` _(pending)_ |
| Desktop captures dir | `_TBD_` |
| Mobile captures dir | `_TBD_` |
| Side-by-side notes | `_TBD_` |

## Notes

Do not mark visual USER ACCEPT here. Functional captures support Worker PASS; HOME/PDP visual review stays `OPEN_FOR_USER_FEEDBACK`.
