# Guest/customer access — T021-R1

Endpoint: `GET /v1/customer/orders/{checkoutId}/fulfillments`

Proof headers:
- `X-Tooba-Guest-Secret` (storefront cart credential)
- and/or authenticated/`X-Tooba-Dev-Actor-User-Id` matching `PlacedByUserId`

FE: `loadCustomerFulfillments` sends guest secret from cart session; BFF `api/customer/[...path]` forwards guest/actor headers to Host.
