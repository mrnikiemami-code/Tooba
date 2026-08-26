# 16 — Fulfillment observability (TB-P06-T009)

## Instrumentation

Class: `FulfillmentInstrumentation` (singleton, `ToobaTelemetry.Meter`)

| Metric | Type | When incremented |
|---|---|---|
| `tooba.fulfillment.created` | Counter | Fulfillment created from paid order |
| `tooba.fulfillment.transition` | Counter | Status transition (`outcome` tag: `processing`, `packed`) |
| `tooba.fulfillment.shipment.created` | Counter | Shipment created |
| `tooba.fulfillment.tracking.assigned` | Counter | Tracking assigned |
| `tooba.fulfillment.dispatched` | Counter | Shipment dispatched |
| `tooba.fulfillment.delivered` | Counter | Shipment delivered |

## PII policy

- Metrics carry no recipient name, address, mobile, or tracking values.
- Only aggregate counters and transition outcome labels.

## Outbox events (downstream observability)

- `fulfillment.created.v1`
- `shipment.dispatched.v1`
- `shipment.delivered.v1`

## Not added

- Dedicated fulfillment health probe (uses shared `/health/ready`).
- Structured log schema beyond existing host logging.
