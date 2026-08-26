# 03 — Scheduling model (TB-P06-T004)

## Strategy

Tooba uses **ASP.NET Core `BackgroundService` poll loops** for periodic durable work and **MassTransit PostgreSQL SQL Transport** for event-driven consumption. No Quartz or third-party scheduler is registered.

## When to use each pattern

| Pattern | Use when | Examples in repo |
|---|---|---|
| **Continuous worker** | Durable backlog must drain; cadence configurable | `OutboxDispatcherHostedService`, `CartExpiryHostedService` |
| **Periodic schedule (timer loop)** | Same as continuous worker; implemented as `while` + `Task.Delay` | Outbox 2s default; cart expiry 5s default |
| **Event-driven consumer** | React to published integration events at-least-once | `ToobaIntegrationTransportConsumer` on `tooba-integration` |
| **One-shot maintenance** | Infrequent operator or startup tasks | `AuthorizationSchemaHostedService`; `MigrationRunner` CLI; MassTransit transport migration hosted service |

## Configuration gates

| Section | Effect when disabled |
|---|---|
| `Tooba:Outbox:Enabled=false` | Outbox hosted service exits immediately (no poll) |
| `Tooba:CartExpiry:Enabled=false` | Cart expiry hosted service exits immediately |
| `Tooba:Messaging:Enabled=false` | No MassTransit bus; `MessagingDisabledPublisher` stub |

## Design constraints

- Poll targets come from **control plane registry**, not HTTP Host header.
- `/health/ready` is **not** tied to outbox queue depth or worker last success.
- Cart expiry config (`Tooba:CartExpiry`) is **independent** from outbox config (`Tooba:Outbox`).
