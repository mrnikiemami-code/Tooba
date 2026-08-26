# 08 — Customer proxy Bearer injection (TB-P06-T007)

## Route

`src/frontend/app/api/customer/[...path]/route.ts`

Maps `/api/customer/{suffix}` → Host `/v1/customer/{suffix}` preserving query string.

## Auth propagation

`forwardToHost()` in `lib/server/host-client.ts`:

1. Reads `tooba_session` from cookie jar.
2. Sets `Authorization: Bearer {sessionId}` on upstream request.
3. Falls back to dev actor header in non-production when unauthenticated.

## Methods

GET (read-only, no CSRF) + POST/PUT/PATCH/DELETE (CSRF + origin check).

## Updated browser clients

| Client | BFF base |
|---|---|
| `customer-api.ts` | `/api/customer/dashboard`, `/profile`, `/orders`, … |
| `customer-profile-api.ts` | `/api/customer/profile` |
| `customer-address-api.ts` | `/api/customer/addresses`, … |
| `storefront-wishlist-api.ts` | `/api/customer/wishlist/*` |
| `storefront-api.ts` | `/api/customer/reviews`, `/api/customer/product-questions` |

All use `credentials: "include"` for cookie propagation.
