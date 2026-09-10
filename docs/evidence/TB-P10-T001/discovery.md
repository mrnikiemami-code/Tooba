# TB-P10-T001 — Discovery

## Frontend classification

| Surface | Path | Class |
|---|---|---|
| Product card ATC | `storefront-product-card.tsx` | Was template label inside Link → **wired** to `addOfferToCart` |
| PDP ATC | `storefront-pdp.tsx` | Already real |
| Header badge | `storefront-header.tsx` | Already real `itemCount` |
| Mini-cart | missing | **Added** Shopeiva-shaped drawer (`storefront-mini-cart.tsx`) |
| `/cart` | `storefront-cart.tsx` | Already real lines/totals; recommendations **added** |
| Cart client | `storefront-cart-api.ts` | Already real session + Host APIs |
| Zustand | — | None (sessionStorage cartId/guestSecret only) |
| Coupon UI | cart summary | Connected to checkout preview (existing Promotions eval) |
| Shipping on cart | cart page | Honest placeholder — no fake free shipping |

## Backend

- `POST/GET/PATCH/DELETE /v1/storefront/cart...` live via `StorefrontCartComposer`
- Guest: `X-Tooba-Guest-Secret`; version: `X-Tooba-Cart-Version`
- Pricing on projection: quoted unit/line + subtotalExclusiveOfTax
- Merchandising: `/v1/storefront/merchandising/{kind}`; home feed for recommendations

## Ports

FE `:3000` rewrites `/v1/*` → Host `:5088`.
