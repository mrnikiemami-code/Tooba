# Tooba Messaging Operations — PostgreSQL SQL Transport

## Canonical decision

```text
MassTransit 8.5.10
Transport = PostgreSQL SQL / Database Transport (MassTransit.SqlTransport.PostgreSQL)
Module Outbox = T006 per-module transactional outbox → OutboxDispatcher → IBus
RabbitMQ / AMQP = FORBIDDEN
```

Supersedes incorrect TB-P06-T003 RabbitMQ assumptions.

## Required configuration

| Key | Purpose |
|---|---|
| `Tooba:Messaging:Enabled` | `true` to start bus |
| `Tooba:Messaging:Transport` | Must be `PostgreSql` |
| `Tooba:Messaging:ConnectionReference` | Logical ref → dedicated messaging PostgreSQL DB |
| `Tooba:Messaging:Schema` | Infrastructure schema (default `transport`) |
| `Tooba:Outbox:*` | Dispatcher poll/retry/dead-letter settings |

Credentials via `Tooba:PostgreSQL:ConnectionReferences` — never commit production secrets.

## Transport topology

- One messaging database per deployment (not per tenant)
- Endpoint: `tooba-integration` (stable, module-agnostic adapter)
- Transport tables owned by MassTransit SQL Transport infrastructure — not business modules

## Outbox flow

1. Module `SaveChanges` → outbox row in module schema (same transaction)
2. `OutboxDispatcher` polls → `IIntegrationEventPublisher`
3. `MassTransitIntegrationEventPublisher` → SQL transport envelope
4. `ToobaIntegrationTransportConsumer` → `IIntegrationEventHandler<T>`

## Retry / redelivery

| Layer | Policy |
|---|---|
| Consumer (MassTransit) | 2 immediate + intervals 5s/15s/30s |
| Outbox dispatcher | exponential backoff, max attempts → dead-letter table |

## Error / skipped inspection

Failed consumer messages remain in SQL transport delivery tables (`transport.message_delivery` etc.). Outbox dead-letters are separate (publish failures). Do not conflate layers.

## Replay / recovery

- Outbox: re-dispatch pending rows after fix (idempotent handlers required)
- Transport: use MassTransit/SQL transport operational procedures; do not blindly replay side-effecting handlers without idempotency

## Health / readiness

- `/health/live` — always OK (no transport dependency)
- `/health/ready` — when messaging enabled: config + `IBusControl.CheckHealth()`; labels include `messaging-transport=postgresql-sql`

## Observability

- Activity sources: `MassTransit`, `Tooba`
- Spans: `tooba.messaging.publish`, `tooba.messaging.consume`, `tooba.outbox.dispatch`
- Structured logs include event type, tenant, edition, event id — never payload secrets

## Shutdown

- `MassTransitHostOptions.StopTimeout = 30s`
- Outbox dispatcher honors cancellation token

## Development

- PostgreSQL via docker-compose (`postgres` service only — no RabbitMQ)
- Development appsettings: messaging enabled against `tooba_messaging` DB
