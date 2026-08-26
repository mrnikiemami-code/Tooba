# 07 — Outbox transaction boundary proof (TB-P06-T003-R1)

## Flow

```
SaveChanges (module DbContext)
  → OutboxSaveChangesInterceptor inserts outbox row (same transaction)
  → commit
OutboxDispatcher (background)
  → IIntegrationEventPublisher.PublishAsync
  → IBus (SQL transport)
```

## Verified

- Modules use `IIntegrationEventPublisher` only — no direct `IBus` in business code
- `MassTransitIntegrationEventPublisher` does not invoke handlers
- No publish-before-commit path in scoped integration tests (`OutboxPostgresTests`, `MassTransitPostgresTests`)

## Layer separation

| Failure | Retried by |
|---|---|
| Publish to transport | Outbox dispatcher |
| Consumer handler | MassTransit consumer retry |

Proven: `Consumer_failure_uses_transport_retry_not_outbox_dead_letter` in `MassTransitPostgresTests`.
