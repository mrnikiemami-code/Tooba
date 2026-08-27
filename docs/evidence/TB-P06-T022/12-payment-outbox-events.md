# 12 — Payment outbox events

**Task:** TB-P06-T022

## Messaging architecture (locked)

```text
MassTransit
PostgreSQL SQL / Database Transport
Outbox (OutboxSaveChangesInterceptor on PaymentDbContext)
NO RabbitMQ
```

## Canonical events

| Event | When |
|---|---|
| `payment.succeeded.v1` | First verified success applied on aggregate |
| `payment.failed.v1` | Definitive verified failure |
| Refunded event | Only when refund foundation emits (Production refund fail-closed today) |

## Coupling

- Paid Order state updates from the **consumer** of `payment.succeeded.v1`, not from browser return or raw webhook text.
- Duplicate Succeeded application is guarded so downstream Order Paid is not double-emitted from a second success apply.

## Registration

`PaymentOutboxRegistration` via `IOutboxModuleRegistration` in `PaymentModule`.

## Honest claim

Outbox integration for payment success/failure remains LIVE from prior foundation; T022 did not replace the transport. No real-bank event stream is claimed.
