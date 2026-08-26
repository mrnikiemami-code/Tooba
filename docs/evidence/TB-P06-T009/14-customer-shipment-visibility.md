# 14 — Customer shipment visibility (TB-P06-T009)

## Scope

- Backend API only. **No frontend/UI files changed.**

## Endpoint

```http
GET /v1/customer/orders/{checkoutId}/fulfillments
```

## Authorization

- Requires authenticated customer (`CurrentAuthenticatedSession`) or dev actor header in Development/Testing.
- Ownership check: `checkout.PlacedByUserId == actorUserId` via Order DbContext (404 if not owned).

## Response

- Array of `FulfillmentSnapshot` for checkout, including items and shipments with tracking/dispatch/delivery timestamps.

## Not in scope (this task)

- Customer panel UI for shipment tracking.
- Push/email notifications on dispatch.

## Seller/admin contrast

- Seller: full mutate lifecycle under `/v1/seller/fulfillments/*`.
- Admin: read-only list/get under `/v1/admin/fulfillments/*`.
- Customer: read-only list by checkout.
