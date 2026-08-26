# 21 — Final runtime preview (TB-P06-T009)

Backend build/test green after Fulfillment module registration.

New operational routes (auth required):

- `GET /v1/seller/fulfillments`
- `GET /v1/admin/fulfillments`
- `GET /v1/customer/orders/{checkoutId}/fulfillments`

Fulfillment handoff consumes `payment.succeeded.v1` after Order Paid projection.

No UI redesign in this Task; storefront panels unchanged.
