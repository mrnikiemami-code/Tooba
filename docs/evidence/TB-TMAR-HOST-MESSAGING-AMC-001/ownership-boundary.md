# ownership-boundary — TB-TMAR-HOST-MESSAGING-AMC-001

| Owner | Responsibility | Current state |
|---|---|---|
| Host/Messaging | composition, options, publisher selection, retry topology, test double, disabled publisher | CORRECT |
| Host/Transport | envelope (`ToobaIntegrationTransportMessage`), consumer, `SqlTransportOptionsMapper` | CORRECT; Messaging→Transport adjacent OK |
| BuildingBlocks | `IIntegrationEvent` / `IIntegrationEventPublisher` / correlation+telemetry helpers | CORRECT |
| Persistence | `IIntegrationEventSerializer`, outbox store/dispatcher primitives, `IDatabaseConnectionResolver` | CORRECT consume |

Transport→Messaging: ZERO (no circular folder dependency).

Foreign module Application/Infrastructure/Domain/DbContext in Messaging: **ZERO**.

Npgsql + MassTransit in MessagingRegistration: legitimate Host platform transport composition (not business persistence).
