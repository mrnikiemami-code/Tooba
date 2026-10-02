# boundary-observability — TB-TMAR-HOST-OUTBOX-AMC-001-W2-CERT

Persistence: neutral seams only (`IOutboxDispatcherStore`, module registration, serializer, connection resolver, OutboxMessage/PollTarget, sanitizer).

Messaging: `IIntegrationEventPublisher` only — MassTransit/IBus ZERO in Outbox.

StoreContext: Contracts.Current only.

Observability: ToobaTelemetry counters + MessagingCorrelation + CorrelationIdContext + ToobaTraceEnricher.

Logging sensitivity: TenantId/EventType/Schema/ErrorType only.

Foreign App/Infra/Domain/DbContext / business authority: ZERO.

Prior Host certs preserved.
