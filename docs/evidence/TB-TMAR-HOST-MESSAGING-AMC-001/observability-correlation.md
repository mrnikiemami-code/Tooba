# observability-correlation — TB-TMAR-HOST-MESSAGING-AMC-001

| Mechanism | In-process | MassTransit publisher |
|---|---|---|
| ActivitySource | `ToobaTelemetry.ActivitySource` (`tooba.outbox.publish`) | reuse Current or start `tooba.messaging.publish` |
| Meter | `tooba.outbox.published` | `tooba.messaging.published` |
| Correlation | `MessagingCorrelation` + `CorrelationIdContext` | same |
| Enricher | `ToobaTraceEnricher` | same |
| Parallel ActivitySource/Meter | ZERO custom | ZERO custom |

Metric name divergence (`outbox.published` vs `messaging.published`) = **intentional path semantics** (test double vs transport) — document for CERT; not blocker for analyze.

Header sensitivity: metadata IDs only; no secrets/payload/connection strings.
Sensitive logging of payloads in Messaging publishers: ZERO.
