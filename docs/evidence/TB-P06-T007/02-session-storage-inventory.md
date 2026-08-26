# 02 — Session storage inventory (TB-P06-T007)

## Pre-task browser session patterns

| Client | Path | Pre-task storage |
|---|---|---|
| Customer dashboard API | `src/frontend/app/customer-panel/customer-api.ts` | localStorage session/access token |
| Customer profile | `src/frontend/app/customer-panel/customer-profile-api.ts` | Bearer from localStorage via customer-api |
| Customer address | `src/frontend/app/customer-panel/customer-address-api.ts` | Bearer from localStorage |
| Storefront wishlist | `src/frontend/app/storefront/storefront-wishlist-api.ts` | localStorage session token |
| Storefront reviews/Q&A | `src/frontend/app/storefront/storefront-api.ts` | localStorage session token |

## Unchanged (out of scope)

| Client | Path | Storage |
|---|---|---|
| Seller panel | `src/frontend/app/vendor-panel/seller-api.ts` | localStorage actor/party (dev seam) |
| Admin panel | `src/frontend/app/admin/admin-api.ts` | localStorage actor (dev seam) |

## Target architecture

Browser → same-origin BFF (`/api/auth/*`, `/api/customer/*`) → Host with server-side `Authorization: Bearer {SessionId}` from HttpOnly `tooba_session` cookie.
