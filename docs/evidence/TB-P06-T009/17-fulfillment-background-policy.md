# 17 — Fulfillment background policy (TB-P06-T009)

## No new background workers

Fulfillment module does **not** register:

- No `IHostedService` / background polling loop
- No scheduled reconciliation job
- No dedicated fulfillment dispatcher

## Event-driven creation

- Fulfillment units created synchronously inside `FulfillmentPaymentSucceededHandler` when MassTransit delivers `payment.succeeded.v1`.

## Existing shared infrastructure reused

| Component | Role for Fulfillment |
|---|---|
| MassTransit SQL transport | Delivers payment events to handler |
| Outbox dispatcher (host) | Publishes `fulfillment.created.v1`, `shipment.*` from `fulfillment.outbox_messages` |

## Rationale

- Creation is idempotent via payment inbox + unique seller-order index.
- Seller-driven shipment lifecycle is request/response via HTTP.
- No stale-state sweeper required at foundation stage.

## Deferred

- Automatic InTransit polling from carrier APIs.
- Background retry for failed inventory consume.
