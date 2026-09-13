# R16 network

Cart load:

1. `GET /v1/storefront/cart/{id}` (existing Active Cart)
2. `POST /v1/storefront/pending-payments` once

Allowed extra:

- one POST when local countdown hits 00:00
- one POST after retry error refresh
- user navigation / CART_CHANGED does not poll reservation

Forbidden (verified in source):

- 1.5s reservation poll
- per-second pending-payments
- `setInterval(() => fetch...)`

Request count for a still-open hold: **2** on first paint, then **0** until expiry or user action.
