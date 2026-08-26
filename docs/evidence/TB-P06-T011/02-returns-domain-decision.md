# 02 — Returns domain boundary decision (TB-P06-T011)

## Ownership

| Concept | Owner module | Schema |
| --- | --- | --- |
| Order / Paid state | Order | `order` |
| Delivery quantities | Fulfillment | `fulfillment` |
| Return request workflow | Returns | `returns` |
| Money movement (refund) | Payment via gateway | `payment` |

## Rules

- **Order ≠ Return ≠ Refund** — separate aggregates, no cross-module SQL JOIN
- Eligibility reads via `IOrderReturnReader` + `IFulfillmentReturnReader` bridges
- Refund only through `IPaymentRefundGateway`; production fail-closed when unconfigured
- Inventory restock only via `IReturnInventoryGateway` (contract; consumed-reservation restock deferred)

## Return lifecycle

```
Requested → Approved → RefundProcessing → Completed
         ↘ Rejected
         ↘ RefundFailed → (Admin retry-refund)
```

## Return window

30 days from last delivery (`FulfillmentReturnBridge.LastDeliveredAt`).
