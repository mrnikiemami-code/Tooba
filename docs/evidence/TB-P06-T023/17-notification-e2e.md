# 17 — Notification E2E scenarios (TB-P06-T023)

No direct DB mutation for commercial proof. Scenarios map to live event → consumer → inbox.

## Scenario map

| # | Flow | Status |
|---|---|---|
| 1 | Sandbox purchase → `payment.succeeded` → customer notification + unread ↑ → open → mark read → unread ↓ | **Proven** — sandbox `outcome=success`; customer got `payment.succeeded` + `fulfillment.created`; mark-read worked (`e2e-notification-api.json`) |
| 2 | Same paid order → owning seller notified; foreign seller none | **Proven** — seller inbox received `order.paid.seller` + `fulfillment.created` for owning seller |
| 3 | Seller shipment dispatch → customer `shipment.dispatched` | **Wired** via `NotificationShipmentDispatchedHandler` (not re-run in this E2E pass) |
| 4 | Story approved/rejected → seller | **DEFERRED** — no story events; no invented notifications |
| 5 | Duplicate integration delivery | **Proven** — unique index + `CreateIfAbsentAsync` (foundation tests) |

## E2E run (recorded)

```text
at: 2026-08-27T11:26:37Z
claim: TRANSACTIONAL_NOTIFICATIONS_LIVE
path: sandbox payment outcome=success
customer types: payment.succeeded, fulfillment.created
seller types: order.paid.seller, fulfillment.created
mark-read: worked
artifact: e2e-notification-api.json
```

## Messaging path

MassTransit + PostgreSQL SQL transport + outbox — **no RabbitMQ**. Handlers registered as `IIntegrationEventHandler<>` in `NotificationModule`.

## Honesty

Empty inbox until real commerce events fire is **correct**. Do not seed fake rows for demos. This E2E used real sandbox payment — not seeded notification rows.
