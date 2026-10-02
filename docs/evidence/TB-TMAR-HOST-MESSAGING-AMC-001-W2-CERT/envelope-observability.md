# envelope-observability — TB-TMAR-HOST-MESSAGING-AMC-001-W2-CERT

Envelope fields: EventType, Version, EventId, OccurredAt, TenantId, Edition, DeploymentId, CorrelationId, PayloadJson.

Headers: tooba.event-type, tooba.tenant-id, tooba.edition, tooba.deployment-id, tooba.event-id, correlation header, traceparent/tracestate when present. No payload/secrets/connection refs in headers.

Canonical observability only: ToobaTelemetry, MessagingCorrelation, CorrelationIdContext, ToobaTraceEnricher.

Metrics accepted: in-process `tooba.outbox.published`; MassTransit `tooba.messaging.published`.
