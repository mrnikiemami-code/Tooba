# boundary-observability — TB-TMAR-HOST-MESSAGING-AMC-001-W1

Boundaries preserved:

- Transport envelope types remain in `Tooba.Host.Transport` (not moved)
- Persistence serializer / connection resolver consumption unchanged
- Outbox production code untouched
- Health consumes `MessagingHostOptions` + `IBusControl` only (namespace import repair)

Observability / correlation canonical only:

- ToobaTelemetry
- MessagingCorrelation
- CorrelationIdContext
- ToobaTraceEnricher

Header sensitivity: metadata headers only (event type, tenant, edition, deployment, event id, correlation, traceparent/tracestate). No payload/secrets/connection refs.

Exception message classification: ZERO (`ex.Message` branching absent).

Foreign App/Infra/Domain/DbContext: ZERO across Host/Messaging.

User-facing hardcoded presentation text: ZERO (operator/startup validation prose only).
