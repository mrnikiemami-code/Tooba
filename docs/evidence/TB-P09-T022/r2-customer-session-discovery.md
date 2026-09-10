# TB-P09-T022-R2 — customer session discovery

## Supported session path (no new auth mechanism)

1. **Register (Host):** `POST /v1/auth/register` with `{ identifierKind: "Email", identifier, password }` → `userId`
2. **Login (BFF):** `GET /api/auth/csrf` → cookie `tooba_csrf`; then `POST /api/auth/login` with `X-Tooba-Csrf` → HttpOnly cookies `tooba_session` + `tooba_refresh`
3. **Customer Panel BFF:** browser calls `/api/customer/*` with `credentials: "include"`; BFF reads `tooba_session` and forwards `Authorization: Bearer {session}` to Host
4. **Ownership:** Host customer order/fulfillments require session user matching `checkout.placed_by_user_id` (or valid guest secret — see guest boundary)

Dev-Actor header alone is **not** a substitute for the owned customer UI session.

## Routes

| Surface | Path |
| --- | --- |
| Customer order + tracking UI | `/fa/customer-panel/orders/{checkoutId}` |
| BFF order | `GET /api/customer/orders/{checkoutId}` |
| BFF fulfillments | `GET /api/customer/orders/{checkoutId}/fulfillments` |
| Host fulfillments | `GET /v1/customer/orders/{checkoutId}/fulfillments` |

## R1 gap addressed

T022-R1 proved guest Host fulfillments + customer-panel shell HTTP 200. Owned-customer tracking UI required a real registered session + `placed_by_user_id` rebind (fixture), not guest Host headers alone.

`USER_VISUAL_ACCEPTED=NO`
