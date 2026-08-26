# 06 — Fulfillment idempotency (TB-P06-T009)

## Event dedup (payment inbox)

| Mechanism | Table | Key |
|---|---|---|
| Payment event dedup | `fulfillment.payment_inbox` | `event_id` (PK) |

- `CreateFromPaidSellerOrdersAsync` returns early if `EventId` already in inbox.
- Replay of same `payment.succeeded.v1` does not create duplicate fulfillments.

## Entity dedup (seller order)

| Mechanism | Index | Key |
|---|---|---|
| One fulfillment per seller order | `ix_fulfillments_seller_order_id` (unique) | `seller_order_id` |

- Per-order skip before insert if fulfillment already exists for `SellerOrderId`.

## Tracking idempotency

- `Shipment.AssignTracking`: same reference re-assignment is no-op; different reference throws.

## Shipment lifecycle idempotency

- `EnsureDispatched` / `EnsureDelivered`: repeat calls on already-dispatched/delivered shipment are no-op.

## Test coverage

- `Paid_order_handoff_is_idempotent_and_shipment_lifecycle_preserves_boundaries`: duplicate event → 1 fulfillment, 1 inbox row.
