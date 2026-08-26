# 17 — Database transport integration tests (TB-P06-T003-R1)

## Testcontainers PostgreSQL (`MassTransitPostgresTests`)

| Test | Proves |
|---|---|
| `Outbox_publishes_into_sql_transport_and_consumer_keeps_tenant` | commit → dispatch → consume |
| `Tenant_isolation_*` | tenant from envelope not HTTP host |
| `Consumer_failure_uses_transport_retry_not_outbox_dead_letter` | layer separation |
| Publisher adapter type check | MassTransit path |

## Additional

| Suite | Coverage |
|---|---|
| `OutboxPostgresTests` | outbox dispatcher / rollback semantics |
| `MassTransitFoundationTests` | version lock, RabbitMQ forbidden, transport validator |
| `PaymentFoundationTests` | payment→order inbox idempotency |

Backend: **209** Host tests + **4** migration runner = **213** total, 0 failed/skipped.

No RabbitMQ Testcontainers (forbidden).
