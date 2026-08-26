# 02 — Post-payment inventory (TB-P06-T009)

## Flow

```text
Checkout submit → OrderLine.ReservationId set (Inventory hold)
→ Payment verified → SellerOrderStatus.Paid
→ payment.succeeded.v1 → FulfillmentPaymentSucceededHandler
→ FulfillmentUnit created with ReservationId copied per line
→ Inventory consumed on shipment dispatch (not at fulfillment creation)
```

## Locks

| Concern | Owner | When |
|---|---|---|
| Reservation hold | Inventory module | Checkout / order submit |
| Reservation reference | Order line snapshot | Paid handoff |
| Reservation consume | `IFulfillmentInventoryGateway` → `IInventoryDirectory.ConsumeAsync` | Shipment dispatch when line fully shipped |

## Evidence

- `FulfillmentItem.ReservationId` stores order-line reservation GUID (no Inventory FK).
- `FulfillmentDirectory.ConsumeInventoryForShipmentAsync` calls gateway only when `QuantityShipped >= QuantityOrdered`.
- Fulfillment does not open Inventory DbContext directly.
