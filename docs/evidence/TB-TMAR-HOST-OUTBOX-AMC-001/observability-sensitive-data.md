# observability-sensitive-data — TB-TMAR-HOST-OUTBOX-AMC-001

## Logging fields

Allowed operational: TenantId, EventType, Schema, ErrorType (exception type name).

Absent: payload, connection string/reference, exception.Message in logs, Authorization, cookies.

## Metrics

Canonical `ToobaTelemetry.Meter` counters only:

- `tooba.outbox.tenant_failures`
- `tooba.outbox.retries`
- `tooba.outbox.dead_letters`
- `tooba.outbox.processed`

No custom Meter; no high-cardinality dimensions on counters.

## Correlation / tracing

- `MessagingCorrelation.ResolveForPublish`
- `CorrelationIdContext.BeginScope`
- `ToobaTelemetry.ActivitySource.StartActivity("tooba.outbox.dispatch")`
- `ToobaTraceEnricher.Enrich`
- Tags: event_type, tenant_id, module_schema

Canonical only; no parallel correlation; tags are operational identifiers.

## Error sanitizer

Persistence `OutboxErrorSanitizer`: strips password/secret/token/connectionstring patterns, stack cues, JSON payload → `payload-omitted`, max 256 chars. No `Exception.ToString()`.

## Sensitive-data state

`CANONICAL_OPERATIONAL_ONLY` (sanitize debt none for Host logging; sanitizer owned by Persistence).
