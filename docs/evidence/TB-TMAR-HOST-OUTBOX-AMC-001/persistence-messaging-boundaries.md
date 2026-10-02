# persistence-messaging-boundaries — TB-TMAR-HOST-OUTBOX-AMC-001

## Persistence (Tooba.Persistence)

| Seam | Owner |
|---|---|
| `IOutboxDispatcherStore` / `NpgsqlOutboxDispatcherStore` | Persistence |
| `OutboxMessage` / `OutboxPollTarget` / `IOutboxPollTargetSource` | Persistence contracts |
| `IOutboxModuleRegistration` | Persistence contract; modules register schema/table only |
| `IIntegrationEventSerializer` | Persistence |
| `IDatabaseConnectionResolver` | Persistence |
| `OutboxErrorSanitizer` | Persistence |
| `SqlIdentifiers.Quote` | Persistence (Host passes schema/table; store quotes) |

Host: **ZERO** module DbContext / table access beyond registered metadata.

## SQL identifier safety

Store builds `Quote(schema).Quote(table)` — injection via tenant/message payload impossible; registrations are trusted static module metadata.

## Messaging

- Publish only via `IIntegrationEventPublisher` (Messaging CERT surface).
- **ZERO** MassTransit types in Host/Outbox.
- Messaging CERT preserved.

## StoreContext

- Assign via `IStoreCommerceContextAssigner` + `IWorkerStoreCommerceContextFactory`.
- **ZERO** StoreContext Application/Infrastructure/Domain.

## Module registration boundary

`IEnumerable<IOutboxModuleRegistration>` — schema/table metadata only; no switch-by-module-name; no business routing in Host Outbox.
