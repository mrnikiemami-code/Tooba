# 16 — Notification observability (TB-P06-T023)

## Telemetry

`NotificationInstrumentation` (singleton counters):

| Counter | When |
|---|---|
| `RecordCreated(sourceType, recipientKind)` | New row persisted |
| `RecordDuplicateSuppressed(sourceType)` | Idempotent hit / race unique index |
| `RecordReadTransition()` | Successful unread → read |

## Safe practices

- Does **not** log title/body/private message content
- Does **not** log secrets or full sensitive payloads
- Counters expose aggregates for tests (`CreatedCount`, `DuplicateSuppressedCount`)

## Scope note

Notification read telemetry is **not** a substitute for domain audit (payment/order/fulfillment remain owners of business audit).
