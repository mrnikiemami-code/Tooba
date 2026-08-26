# 05 — Paid order handoff (TB-P06-T009)

## Trigger

```text
payment.succeeded.v1
  → FulfillmentPaymentSucceededHandler
  → FulfillmentDirectory.CreateFromPaidSellerOrdersAsync
```

## Handoff contract

- Reader: `IOrderFulfillmentReader` (implemented by `OrderFulfillmentBridge` in Order.Infrastructure)
- Snapshot: `OrderFulfillmentHandoffSnapshot` with address, shipping method, lines, `IsPaid` flag
- Guard: fulfillment created only when `handoff.IsPaid == true`

## Per SellerOrder

1. Skip if `fulfillment.fulfillments.seller_order_id` already exists (unique index).
2. Load handoff from Order schema via bridge (no Fulfillment query into Order tables).
3. `FulfillmentUnit.CreateFromPaidOrder(...)` copies immutable address snapshot and line quantities/reservations.
4. Emit `fulfillment.created.v1` via outbox on save.

## Multi-seller checkout

- `PaymentSucceededIntegrationEvent.SellerOrderIds` may contain multiple IDs.
- One `FulfillmentUnit` per Paid SellerOrder (marketplace: N sellers → N fulfillments).
