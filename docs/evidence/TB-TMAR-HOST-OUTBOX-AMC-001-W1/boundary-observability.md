# boundary-observability — TB-TMAR-HOST-OUTBOX-AMC-001-W1

Persistence seams only: `IOutboxDispatcherStore`, `IOutboxModuleRegistration`, `IIntegrationEventSerializer`, `IDatabaseConnectionResolver`, `OutboxMessage`, `OutboxPollTarget`, `OutboxErrorSanitizer`.

Messaging: `IIntegrationEventPublisher` only. MassTransit / IBus ZERO in Host/Outbox.

StoreContext: Contracts.Current only.

Observability preserved: ToobaTelemetry counters (tenant_failures, retries, dead_letters, processed), MessagingCorrelation, CorrelationIdContext, ToobaTraceEnricher.

Logging: TenantId / EventType / Schema / ErrorType only. No payload / connection string / exception.Message.

Foreign App/Infra/Domain/DbContext / business authority: ZERO.

Protected certifications preserved (Observability/Messaging/Health/MT/Errors/Security/Admin).
