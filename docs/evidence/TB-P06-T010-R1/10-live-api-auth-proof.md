# 10 — Live API / auth proof (TB-P06-T010-R1)

| Surface | API | Auth |
| --- | --- | --- |
| Customer | BFF `/api/customer/orders/{checkoutId}/fulfillments` | HttpOnly session via `customerAuthHeaders()` |
| Seller | `/v1/seller/fulfillments*` | `X-Tooba-Seller-Party-Id` + dev actor header pattern (T023) |
| Admin | `/v1/admin/fulfillments*` | `X-Tooba-Admin-Actor-User-Id` dev header (existing admin-api) |

Verified:

- No `localStorage` SessionId for customer auth
- Admin dev actor localStorage is pre-existing dev-only pattern (not customer session)
- Backend authorization unchanged (no backend edits in R1)
- Unauthenticated fulfillment routes return 401 at Host (T009-R1 probes)
