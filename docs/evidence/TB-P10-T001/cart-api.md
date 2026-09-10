# TB-P10-T001 — Cart API

Storefront uses existing Host endpoints (Next rewrite):

- `POST /v1/storefront/cart` — create guest; returns `guestSecret` once
- `GET /v1/storefront/cart/{cartId}` — requires guest secret
- `POST /v1/storefront/cart/{cartId}/lines` — `{ offerId, quantity }`
- `PATCH .../lines/{lineId}` — `{ quantity }`
- `DELETE .../lines/{lineId}`

Client: `src/frontend/app/storefront/storefront-cart-api.ts`

- Session keys: `tooba.storefront.cartId`, `tooba.storefront.guestSecret`
- Headers: `X-Tooba-Guest-Secret`, `X-Tooba-Cart-Version`
- Events: `tooba-cart-changed` refreshes header badge + mini-cart

Module ownership remains `ICartDirectory` / `CartAccess`; CartId is not bearer.
