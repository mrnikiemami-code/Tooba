# R2 — Outbox / event transport proof

- Producer: module EF SaveChanges + `OutboxSaveChangesInterceptor` writes `outbox_messages`.
- Publisher: `MassTransitIntegrationEventPublisher` publishes `ToobaIntegrationTransportMessage` to MassTransit PostgreSQL SQL Transport.
- Consumer: `ToobaIntegrationTransportConsumer` on endpoint `tooba-integration` invokes registered `IIntegrationEventHandler<T>`.
- Config: `Tooba:Messaging:Enabled=true`, `Transport=PostgreSql`, `ConnectionReference=messaging`, `Schema=transport`, `UseInProcessTestDouble=false`.
- Live `tooba_messaging` has `transport.message`, `message_delivery`, `queue*`, `topic*` — no RabbitMQ artifacts.
- `MassTransitPostgresTests.Publisher_adapter_is_masstransit_and_handler_has_no_masstransit_type` asserts MassTransit publisher + transport schema.
- Settlement handlers have no MassTransit dependency; consumer adapter is Host-only.
