# 23 — Final runtime (TB-P06-T023)

## Pre-work triad (see `01-runtime-before-notifications.md`)

| Probe | Before |
|---|---|
| Host `/health` | 200 |
| Host `/health/ready` | 200 |
| FE customer / vendor panels | 200 |
| Shopeiva `:3001` | 200 |

## Post-implementation (Worker Result)

| Probe | Result |
|---|---|
| Host.Tests | **269 passed**, 0 failed, 0 skipped |
| FE lint + tsc | green |
| `/customer-panel/notifications` | Live inbox with real rows after sandbox payment |
| `/vendor-panel/notifications` | Live inbox with real rows after sandbox payment |
| Mark-read API | works |
| Realtime / push | **not** implemented — `REALTIME_NOTIFICATIONS = DEFERRED` |

## E2E proof artifacts

```text
e2e-notification-api.json
captures/01-customer-notifications.png
captures/02-seller-notifications.png
browser-proof.json
```

Sandbox `outcome=success` projected:

- Customer: `payment.succeeded`, `fulfillment.created`
- Seller: `order.paid.seller`, `fulfillment.created`

## USER-PREVIEW URLs (typical local)

```text
Customer Notifications: http://localhost:3000/customer-panel/notifications
Seller Notifications:   http://localhost:3000/vendor-panel/notifications
Customer Order:         http://localhost:3000/customer-panel/orders/{checkoutId}
Seller Order:           http://localhost:3000/vendor-panel/orders/{sellerOrderId}
Shopeiva compare:       http://localhost:3001/user-panel/notifications
Persian Home:           http://localhost:3000/
```

Empty inbox until commerce events is honest — not a failure. After sandbox payment in this Task, inboxes were non-empty as proven above.

## Must not claim

```text
REALTIME_NOTIFICATIONS_LIVE
PRODUCTION_GO_LIVE_READY
USER_VISUAL_ACCEPTED
```
