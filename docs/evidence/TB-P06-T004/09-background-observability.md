# 09 — Background observability (TB-P06-T004)

## Structured logs

JSON console logging with trace/span correlation (`Program.cs`). Worker logs include:

- Outbox: tenant poll failures, dead-letter events, loop errors (`ErrorType` only, sanitized errors in DB).
- Cart expiry: cycle completion with `ExpiredCount`, per-tenant failures.

## Metrics (OpenTelemetry meter `Tooba`)

| Metric | Worker |
|---|---|
| `tooba.outbox.processed` | Outbox dispatcher |
| `tooba.outbox.retries` | Outbox dispatcher |
| `tooba.outbox.dead_letters` | Outbox dispatcher |
| `tooba.outbox.tenant_failures` | Outbox dispatcher |
| `tooba.cart_expiry.expired` | Cart expiry |
| `tooba.cart_expiry.tenant_failures` | Cart expiry |
| `tooba.messaging.published` / `.consumed` | MassTransit path |

Exported via OTLP when `Tooba:Observability:OtlpEndpoint` is set.

## BackgroundWorkerRegistry

In-memory, no DB scan:

- `RecordSuccess(workerName, processedCount)` — last success UTC, last processed count
- `RecordFailure(workerName, errorType)` — last failure UTC, error type name

Registered workers: `outbox-dispatcher`, `cart-expiry`.

## OTel activities

| Activity name | Source |
|---|---|
| `tooba.outbox.dispatch` | Outbox per-message dispatch |
| `tooba.cart_expiry.reconcile` | Cart expiry per-tenant cycle |
| `tooba.messaging.publish` / `.consume` | MassTransit integration path |
| MassTransit internal | `MassTransit` activity source |

Tags include `tooba.tenant_id`, `tooba.event_type`, `tooba.module_schema` where applicable.

## Backlog / stuck visibility

- **Cheap:** metrics counters, registry last-run state, outbox row columns (`locked_until`, `next_attempt_at`, `dead_lettered_at`).
- **Not in Host:** automated backlog depth gauge or stuck-count API (avoid expensive cross-tenant scans).
