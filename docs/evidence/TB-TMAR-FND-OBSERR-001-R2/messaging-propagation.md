# Messaging / MassTransit correlation — TB-TMAR-FND-OBSERR-001-R2

## Producer (`MassTransitIntegrationEventPublisher`)

- Resolves CorrelationId via `MessagingCorrelation.ResolveForPublish` (metadata → ambient → EventId)
- `CorrelationIdContext.BeginScope` for publish lifetime
- Sets MassTransit `context.CorrelationId` to business correlation GUID (not EventId)
- Headers: `X-Correlation-ID`, optional `traceparent`/`tracestate` backup
- Enriches ambient Activity; creates `tooba.messaging.publish` only if Activity absent
- Relies on MassTransit + OpenTelemetry W3C propagation as primary

## Consumer (`ToobaIntegrationTransportConsumer`)

- `BeginConsumeScope` from envelope / transport CorrelationId / EventId
- Restores prior AsyncLocal on dispose
- Enriches MassTransit-owned Activity when present; fallback child only if Activity null
- Structured log scope with CorrelationId

## In-process test double

`InProcessIntegrationEventPublisher` uses the same ResolveForPublish + BeginScope + enrich contract.
