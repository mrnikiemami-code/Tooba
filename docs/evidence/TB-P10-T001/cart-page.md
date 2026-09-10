# TB-P10-T001 — Cart page

`/cart` continues to use `StorefrontShopeivaCart` on Host cart truth.

- Lines: title, media, qty policy, remove, Host amounts
- Summary: itemCount + subtotalExclusiveOfTax
- Shipping block: honest non-authoritative copy (`data-testid="cart-shipping-honest"`)
- Coupon: Host checkout preview
- Recommendations: live home feed cards with real ATC (`data-testid="cart-recommendations"`)
- CTA continues to `/checkout` for existing checkout shell (shipping/payment remain later tasks)
