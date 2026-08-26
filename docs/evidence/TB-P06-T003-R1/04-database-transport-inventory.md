# 04 — Database transport inventory (TB-P06-T003-R1)

| Component | Value |
|---|---|
| MassTransit | 8.5.10 |
| SQL Transport | MassTransit.SqlTransport.PostgreSQL 8.5.10 |
| RabbitMQ | **NOT PRESENT** |
| Transport DB | dedicated via `ConnectionReference` (e.g. `tooba_messaging`) |
| Schema | `transport` (configurable) |
| Endpoint | `tooba-integration` |
| Publisher | `MassTransitIntegrationEventPublisher` → `IBus.Publish` |
| Consumer | `ToobaIntegrationTransportConsumer` → `IIntegrationEventHandler<T>` |
| Module Outbox | T006 per-module tables + `OutboxDispatcher` |
| MassTransit EF Outbox | NOT USED (module outbox retained) |
| Retry (consumer) | 2 immediate + 5s/15s/30s intervals |
| Retry (outbox) | exponential, max 5 attempts → dead-letter |
| Idempotency | Order `payment_inbox`; Party handler unchanged at HEAD |
| Health | `IBusControl.CheckHealth()` when enabled |
| OTel | ActivitySource `MassTransit` + `Tooba` custom spans |

Production handlers: `OrderPaymentSucceededHandler`, `PartyMembershipProjectionHandler`.
