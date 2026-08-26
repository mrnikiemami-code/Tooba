# 11 — localStorage removal audit (TB-P06-T007)

## Removed session token usage

| File | Change |
|---|---|
| `src/frontend/app/customer-panel/customer-api.ts` | Uses `/api/customer/*` + `credentials: "include"`; no localStorage session |
| `src/frontend/app/customer-panel/customer-profile-api.ts` | BFF profile PUT via `/api/customer/profile` |
| `src/frontend/app/customer-panel/customer-address-api.ts` | BFF address CRUD via `/api/customer/addresses/*` |
| `src/frontend/app/storefront/storefront-wishlist-api.ts` | BFF wishlist via `/api/customer/wishlist/*` |
| `src/frontend/app/storefront/storefront-api.ts` | Reviews/Q&A via `/api/customer/reviews`, `/api/customer/product-questions` |

## Verification

Grep of customer-panel and storefront auth paths: no `localStorage` session/access-token reads for customer flows.

## Remaining localStorage (intentional, out of scope)

- `vendor-panel/seller-api.ts` — dev seller actor/party
- `admin/admin-api.ts` — dev admin actor

## Auth headers

`customerAuthHeaders()` / `bffFetchHeaders()` supply CSRF header only; Bearer injected server-side by BFF.
