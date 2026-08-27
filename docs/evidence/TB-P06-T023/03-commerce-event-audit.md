# 03 — Commerce event audit (TB-P06-T023)

| Event | Status | Notification use |
|---|---|---|
| `payment.succeeded.v1` | AVAILABLE | Customer paid + Seller new paid order |
| `payment.failed.v1` | AVAILABLE | Customer payment failed |
| `order.checkout_submitted.v1` | AVAILABLE | Optional; prefer payment.succeeded for paid truth |
| `order.seller_order_created.v1` | AVAILABLE | Seller actionable (if not redundant with payment) |
| `fulfillment.created.v1` | AVAILABLE | Customer preparing / seller ops |
| `shipment.dispatched.v1` | AVAILABLE | Customer shipped/tracking |
| `shipment.delivered.v1` | MISSING_BUT_SMALL | Domain exists; outbox translate optional |
| `return.requested.v1` | AVAILABLE | Customer + seller |
| `return.approved.v1` | AVAILABLE | Customer + seller |
| `refund.succeeded.v1` | AVAILABLE | Customer + seller |
| Story review events | NOT_REQUIRED / MISSING | Outbox Translate null — defer story notify |

## Rules

- Do not invent marketing/ticket/gift events just to fill Shopeiva filter chips.
- Filters for offer/ticket may show empty honestly when no rows of that type exist.
- REALTIME_NOTIFICATIONS = DEFERRED
