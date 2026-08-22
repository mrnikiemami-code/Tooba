# Tooba — Observability, Logging & Audit Architecture

Status:

```text
P00 architecture design — candidate for later ADR; not an ADR lock
```

Task:

```text
TB-P00-T019
```

Documentation only. No OpenTelemetry packages, collectors, exporters, dashboards, alerts, SIEM, audit schema, or Shopeiva. No vendor lock except conceptual OpenTelemetry.

Modular monolith. No cross-module DB joins. SpiceDB remains the authorization decision system.

```text
Technical Log != Business Audit
Technical Log != Security Audit
Metric != Audit
Trace != Audit
Analytics != Audit
Analytics != Audit != technical logging
Analytics != Authorization
```

```text
Locale != Market != Currency
Backend/module boundary != UI boundary
```

Hard rules:

```text
Technical logging is diagnostic for engineering/operations; it is not evidence of business or security history by itself.
Business Audit is the durable record of significant business actions/state changes.
Security Audit is the durable record of security-sensitive activity.
Analytics is a separate behavioral/observation capability (see 16-first-party-analytics.md).
Authorization decisions remain SpiceDB-owned (see 05-spicedb-authorization.md); observability consumes decision/mutation facts, it does not authorize.
No secrets in logs, traces, metrics, analytics, or audit payloads.
TenantId is an immutable durable identifier, not hostname as canonical identity.
Observability must not license cross-module SQL/ORM/repository access.
```

Related: `03-data-ownership-and-module-contracts.md`, `04-identity-authentication.md`, `05-spicedb-authorization.md`, `11-payment.md`, `14-search-indexing.md`, `15-media-image-pipeline.md`, `16-first-party-analytics.md`, `17-ai-assistant-rag.md`.

## A. Core Separation

| Concern | Meaning | Durability posture |
| --- | --- | --- |
| Technical Logging | Diagnostic structured events for engineering/operations | Rotatable / sampleable |
| Tracing | Causal execution path across requests, messages, jobs | Sampleable |
| Metrics | Numerical signals over time | Aggregated; not history of a business act |
| Business Audit | Durable record of important business actions/state changes | High durability |
| Security Audit | Durable record of security-sensitive activity | High durability; restricted access |
| Analytics | Behavioral/derived observation (T017) | Reliability class varies; not audit |

Analytics remains a separate behavioral capability. Traces and metrics do not substitute for audit. Authorization is not a logging subsystem.

## B. OpenTelemetry Foundation

Confirmed requirement:

```text
OpenTelemetry
```

Vendor-neutral observability foundation. Architecture supports:

```text
Traces
Metrics
Logs correlation
Resource attributes
Baggage where safe
Context propagation
Exporters
Collector
```

Do not lock a telemetry backend. Application code depends on OTel concepts/APIs, not a named APM product.

Baggage is optional and must not carry secrets, PII, or authorization tokens.

## C. Vendor Neutrality

Application/domain architecture must **not** depend directly on:

```text
Datadog SDK
New Relic SDK
Application Insights SDK
Grafana vendor APIs
Elastic APM APIs
```

Vendor-specific exporters/integrations belong at infrastructure edge (collector/exporters). Replacing a vendor must not require business-domain rewrite.

## D. Structured Logging

Technical logs are structured. Prefer semantic fields (not a locked schema):

```text
EventName
TraceId
SpanId
CorrelationId
TenantId
Deployment/Edition
Module
Operation
Entity reference
SellerId where relevant
OrderId where relevant
Duration
Outcome
ErrorCode
```

Avoid unstructured string-only logging as the primary strategy. Exact logging schema: `NEEDS_LATER_P00_DETAIL`.

## E. Correlation Model

Correlate across: HTTP request, background job, domain/application command, integration event, message, checkout, order, payment attempt, inventory reservation, AI request, search indexing, media processing.

| Identifier | Role |
| --- | --- |
| TraceId | Distributed causal graph (OTel) |
| SpanId | One operation in that graph |
| CorrelationId | Business/ops grouping across hops when a trace is absent or incomplete |
| CausationId | Immediate predecessor command/event that caused this one |
| OperationId | Local use-case / job run identity |
| Business entity IDs | OrderId, PaymentId, ReservationId, etc. — domain truth refs |

Do not overload one ID for all meanings. Locale/Market/Currency remain independent dimensions when present; they are not correlation ids.

## F. Distributed Tracing

Even in a Modular Monolith, trace boundaries reflect logical module/application operations. Future microservice extraction preserves the same propagation.

```text
incoming request
→ application use case
→ module contract
→ DB/external call
→ message/outbox
→ consumer
```

No implementation. Cross-module calls remain contracts/events, not shared tables.

## G. Span Design

Avoid a span for every tiny method. Instrument meaningful boundaries:

```text
HTTP request
application use case
database operation
external HTTP call
message publish/consume
background job
search query
payment provider call
AI provider call
media transform
```

Sampling policy: `NEEDS_LATER_P00_DETAIL`. Spans must not contain secrets.

## H. Metrics

Categories (conceptual): request rate, error rate, latency, dependency latency, DB pool/utilization where observable, queue backlog, job failures, cache hit ratio, search latency, indexing lag, payment failures, inventory reservation conflicts, media processing latency, AI latency/token/cost signals, analytics ingestion lag.

Business KPI metrics may exist for operations but must **not** replace authoritative business reporting (Order/Payment/Analytics aggregates as designed in T017).

## I. RED / USE Style

Operational thinking:

```text
Rate / Errors / Duration
```

for services and use cases.

```text
Utilization / Saturation / Errors
```

for resources (pools, queues, disks) where appropriate.

Do not over-instrument arbitrary counters.

## J. SLO / SLI Readiness

Preserve future SLI families: availability, latency, error rate, checkout success, payment provider health, search latency, indexing freshness, media processing freshness, AI availability.

Do not invent exact production SLO numbers in P00. Thresholds: `NEEDS_LATER_P00_DETAIL`.

## K. Error Taxonomy

Conceptual classes (names not locked): Validation error, Business rule rejection, Authorization denial, Not found, Conflict/concurrency, Dependency failure, Timeout, Transient infrastructure error, Permanent infrastructure error, Unexpected defect.

Logs, metrics, and traces must be able to distinguish them (ErrorCode / outcome class). Do not expose internal details directly to users. Authorization denials remain SpiceDB decisions; telemetry records outcome, not a second policy engine.

## L. Exception Logging

Avoid logging the same exception at every layer (duplicate noisy logs).

Principle:

- enrich context as the error propagates;
- log once at a meaningful boundary where the outcome is known;
- preserve trace/span error state;
- treat expected business failures as structured outcomes, not stack traces everywhere.

## M. Request Logging

Safe request telemetry may include: route template, method, status, duration, trace, tenant, authenticated subject **opaque** id where appropriate.

Never log raw: password, OTP, tokens, Authorization headers, cookies, payment secrets, full sensitive bodies.

## N. PII / Secret Redaction

Hard requirement. Sensitive values include: password, OTP, session/token secrets, API keys, payment secrets, PAN/CVV, national ID, phone/email where unnecessary, addresses, private AI context.

Need: redaction, allowlist-based logging where practical, sensitive-field annotations/policy, structured logging filters.

Do not rely on developers remembering manually every time. No secrets in logs.

## O. Tenant Context

Single-Store observability carries `TenantId` where safe/useful. Tenant identity is an immutable durable ID, not raw hostname as canonical identity.

Never log raw DB connection strings or secrets.

Marketplace uses deployment/resource/seller context appropriately — do not invent fake tenant semantics for marketplace rows.

## P. User / Subject Context

Where justified, carry opaque: IdentityId, PartyId, OrganizationId, SellerId.

Do not log excessive PII. Authorization-related telemetry may include subject/resource/permission **references** and decision outcome — not relationship graph dumps or tokens.

## Q. Business Audit

Durable, queryable history for significant business operations. Examples: Product approved, Seller approved/suspended, Price changed, Inventory adjusted, Order cancelled, Refund requested/completed, Content published, Promotion activated, Tenant setting changed, Organization membership changed.

Each record conceptually includes: Who/Actor, What action, Target, When, Tenant/Scope, Reason where applicable, Before/After summary or changed fields where safe, Correlation, Source.

Do **not** use technical logs as the only audit record.

## R. Security Audit

Security-sensitive events include: login success/failure, password changed, MFA enabled/disabled, recovery completed, identifier changed, external identity linked, session revoked, authorization denial where security-relevant, privileged permission change, tenant admin change, suspicious access.

Identity owns identity security events (T005). Authorization owns decision/mutation audit (T006). Security audit has different retention and access than general logs. Access via SpiceDB.

## S. Audit Immutability Direction

Audit records are append-oriented and tamper-resistant **in architecture**. Do not imply they are literally impossible to alter without designing storage.

Preserve: append-oriented history, restricted mutation, integrity/reconciliation, export/retention.

Exact cryptographic tamper evidence: `NEEDS_LATER_P00_DETAIL`.

## T. Audit Ownership

Each business module owns semantic meaning of its auditable business actions. A shared Audit capability/infrastructure may persist **normalized audit envelopes**. Do not centralize business logic into an Audit module.

```text
module action
→ audit fact/envelope
→ durable audit store
```

Observability platform is `NOT_OWNER` of Order/Payment/Inventory truth.

## U. Technical Log Retention vs Audit Retention

```text
technical log retention
!=
audit retention
```

Logs may be sampled/rotated differently. Audit may require longer durable retention. Exact periods: later legal/business policy (`NEEDS_LATER_P00_DETAIL`).

## V. Analytics Separation

Do not use analytics events as audit evidence by default.

`AddToCart` analytics event is **not** equivalent to `Admin changed payment configuration`.

Analytics may be lossy/best-effort for some events. Audit may require durability. See `16-first-party-analytics.md`.

## W. Outbox / Messaging Observability

Future outbox/inbox/message processing needs: publish lag, consumer lag, retry count, dead-letter/failure, duplicate detection, idempotency outcome, message age.

Trace/correlation context propagates through messages where safe (no secrets in headers/payload logs).

## X. Background Jobs

Every job should expose: JobName, JobRunId, TenantId where applicable, StartedAt, Duration, Outcome, Retry, ProcessedCount, FailureCount, Trace/Correlation.

No ambient tenant guessing. Explicit tenant/scope on the job payload/context.

## Y. Database Observability

Need: query latency, connection failures, pool saturation, transaction duration, deadlocks/conflicts, migration status.

Do not log raw SQL parameters containing sensitive values indiscriminately.

Cross-module direct DB access remains **forbidden** regardless of observability.

## Z. Cache Observability

Cache abstraction should expose: hit/miss, latency, eviction/invalidation, backend failures, key-space/category.

Do not log full sensitive cache keys if they contain private data. No Redis required initially.

## AA. Search Observability

Preserve: query latency, zero-result rate, engine errors, index lag, document count, rebuild status.

Search analytics (queries, CTR) remains separate behavioral data (T017). Search owns index/query execution.

## AB. Payment Observability

Safe signals: provider latency, attempt status, decline category, callback lag, signature failure count, reconciliation mismatches, refund failures.

Never log card secrets/provider secrets. Payment remains source of payment truth.

## AC. Inventory Observability

Need: reservation conflict rate, reservation expiry, oversell-prevention rejection, release failures, stock reconciliation anomalies, external sync lag.

Inventory remains source of stock/reservation truth.

## AD. Media Observability

Need: upload failure, processing latency, transform failure, CDN failure, variant generation, storage growth, broken references.

## AE. AI Observability

Safe signals: provider/model, latency, token usage, retrieval latency, citation coverage, tool-call outcomes, cost signals, fallbacks/errors, evaluation score.

Do not indiscriminately log prompt/conversation content. Sensitive AI contexts require redaction/limited capture. AI telemetry ≠ Analytics ≠ audit of payments/authn. See `17-ai-assistant-rag.md`.

## AF. SEO / Web Observability

Signals: render failures, 404/5xx, redirect loops, canonical/hreflang validation failures, sitemap generation failures, CWV.

Some belong to scheduled diagnostics rather than runtime metrics.

## AG. Frontend Observability

Browser-side: JS errors, route failures, Core Web Vitals, failed API calls, critical interaction errors.

Do not capture sensitive form fields. Frontend telemetry is tenant/site scoped. Prefer correlating to backend TraceId where safe.

## AH. UI / UX Operational Signals

UI quality is product-critical. Support future measurement of: frontend errors, slow interactions, failed images, checkout step failures, search latency, mobile performance, CWV regressions.

Do **not** equate analytics engagement metrics with technical UX health.

Future Admin/Operations UI is workflow-oriented (health, incident drill-down, filters, timeline, RTL/LTR, accessible tables/charts, loading/empty/error). Backend/module boundary ≠ UI boundary.

## AI. Health Checks

Layered concepts: liveness, readiness, dependency health, degraded capability.

Avoid marking the entire application down because an optional AI provider is unavailable.

Examples:

```text
AI unavailable → core commerce still ready
Search unavailable → storefront degraded
Primary DB unavailable → critical readiness failure
```

Exact health policy: `NEEDS_LATER_P00_DETAIL`.

## AJ. Alerting Readiness

Expose actionable signals. Avoid alerting on every error log.

Candidates: sustained error rate, latency threshold, queue backlog, payment failure spike, database saturation, cross-tenant security anomaly, indexing lag, job failures.

Exact thresholds later (`NEEDS_LATER_P00_DETAIL`).

## AK. Sampling

High-volume traces/logs may require sampling.

Must **not** be accidentally lost due to generic telemetry sampling:

```text
security audit
business audit
payment reconciliation facts
```

Sampling policy applies differently to traces, logs, and audit.

## AL. Environment / Deployment Context

Telemetry identifies: service/application, version, environment, deployment/edition, instance, region where applicable.

Single-Store additionally supports TenantId on relevant spans/logs. Do not leak secrets or unnecessary build paths.

## AM. Release / Version Correlation

Correlate incidents with: application version, commit/build, deployment time, feature/config version where relevant.

No implementation.

## AN. Feature Flag Observability

If flags are later used, traces/logs may carry **active flag keys/versions** for debugging. Do not copy entire flag payload into every log.

Exact feature-flag architecture: deferred / later.

## AO. Audit Query UX

Professional Admin/Security UX: filter by actor, target/resource, action, date range, tenant/seller, success/failure, correlation, before/after summary.

Audit UI is **not** a raw log-file viewer. Access controlled by SpiceDB. Cohesive workflow, not a CRUD dump of storage tables.

## AP. Operational Dashboard UX

Show: health, error trends, latency, queue/job state, payment issues, search/index freshness, media failures, AI degradation, tenant-specific incidents.

Backend module boundaries must not dictate fragmented UI. Cohesive operational workflows. Support severity/status, filters, timeline, mobile/responsive where appropriate, RTL/LTR, accessible charts/tables, loading/empty/error states.

## AQ. Trace-to-Business Navigation

Operations staff should move conceptually between: OrderId, PaymentId, TraceId, Audit event, Job/message correlation — without exposing private customer data broadly.

## AR. Incident Diagnostics

Architecture should enable questions such as:

```text
Why did this order fail?
Why was payment captured but order remained pending?
Why did this tenant see no search results?
Why is this product image broken?
Why did AI cite stale content?
```

Observability carries enough correlation/provenance to answer these without direct database archaeology as the **normal** workflow. Cross-module joins remain forbidden; use ids, contracts, and audit/telemetry correlation.

## AS. Data Export / SIEM Readiness

Security audit and logs may later export to: SIEM, log platform, object archive, security analytics.

Use adapters/exporters. Do not make SIEM vendor part of domain architecture.

## AT. Failure of Observability Backend

```text
Telemetry backend failure should not normally break core commerce.
```

Technical logs/traces/metrics degrade safely. Business/Security Audit durability may require stronger local buffering/outbox semantics. Exact buffering: `NEEDS_LATER_P00_DETAIL`.

## AU. Audit Delivery Reliability

Audit events are higher reliability than normal telemetry. Future: local durable write, outbox, retry, reconciliation.

Do not permit a critical audited business change to be considered fully recorded only because a remote log collector accepted a best-effort event.

## AV. Audit Before/After Data

Avoid blindly serializing entire entity snapshots. Prefer: changed fields, business-relevant before/after, opaque references, reason — while protecting sensitive data.

Exact diff representation: `NEEDS_LATER_P00_DETAIL`.

## AW. Security Event Severity

May classify: Informational, Suspicious, High Risk, Critical (or equivalent). Do not lock final taxonomy.

Need hooks for future anomaly detection/security operations.

## AX. Data Ownership Matrix

Marks: `OWNER` | `SOURCE` | `CONSUMER` | `CORRELATION` | `NOT_OWNER`

| Fact | Observability | Business Audit | Security Audit | Analytics | Identity | Authorization | Order | Payment | Inventory | Search | Media | AI |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| trace | OWNER | CORRELATION | CORRELATION | CORRELATION | CONSUMER | CONSUMER | CORRELATION | CORRELATION | CORRELATION | CONSUMER | CONSUMER | CONSUMER |
| metric | OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | CONSUMER | CONSUMER | CONSUMER | CONSUMER | CONSUMER | CONSUMER | CONSUMER | CONSUMER |
| technical log | OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | SOURCE | SOURCE | SOURCE | SOURCE | SOURCE | SOURCE | SOURCE | SOURCE |
| business action audit | CONSUMER | OWNER (envelope) | NOT_OWNER | NOT_OWNER | SOURCE | SOURCE | SOURCE | SOURCE | SOURCE | NOT_OWNER | SOURCE | NOT_OWNER |
| security event | CONSUMER | NOT_OWNER | OWNER (envelope) | NOT_OWNER | SOURCE | SOURCE | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER |
| behavioral event | NOT_OWNER | NOT_OWNER | NOT_OWNER | OWNER | NOT_OWNER | NOT_OWNER | SOURCE | SOURCE | NOT_OWNER | SOURCE | NOT_OWNER | CONSUMER |
| order truth | NOT_OWNER | CONSUMER | NOT_OWNER | CONSUMER | NOT_OWNER | NOT_OWNER | OWNER | CORRELATION | NOT_OWNER | NOT_OWNER | NOT_OWNER | CONSUMER |
| payment truth | NOT_OWNER | CONSUMER | NOT_OWNER | CONSUMER | NOT_OWNER | NOT_OWNER | CORRELATION | OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER |
| authorization decision | CONSUMER | CONSUMER | CONSUMER | NOT_OWNER | CORRELATION | OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | CONSUMER |
| AI request metadata | CONSUMER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | OWNER |

Modules emit technical logs and audit facts; Observability owns the telemetry pipeline. Shared audit infrastructure persists envelopes; emitting modules remain SOURCE of semantic meaning. SpiceDB remains OWNER of authorization decisions.

## AY. Failure Matrix

Exact per-operation policy: `NEEDS_LATER_P00_DETAIL`.

Normal telemetry failure usually must **not** block commerce. Critical audit failure may require stricter behavior depending on the operation.

| Case | Block request? | Buffer? | Retry? | Fail closed? | Drop? | Alert? | Reconcile? |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Telemetry Exporter Down | No (normal) | Optional local | Yes (exporter) | No | Last resort traces/logs | Yes | No |
| Collector Down | No | Optional | Yes | No | Last resort | Yes | No |
| Log Backend Down | No | Buffer logs | Yes | No | After retention of buffer | Yes | No |
| Metric Backend Down | No | Optional | Yes | No | Aggregates gap OK | Yes | No |
| Trace Sampling | No | N/A | N/A | No | Yes (by policy) | No | No |
| Audit Store Down | Per-operation later | Required for critical audit | Yes | Maybe for critical audited writes | No for critical audit | Yes | Yes |
| Audit Delivery Retry | No (after local durable write) | Outbox | Yes | After local write succeeds | No | Yes if exhausted | Yes |
| Redaction Failure | Prefer fail closed on that field/event | Quarantine event | After fix | For sensitive emit | Drop unredacted payload | Yes | No |
| PII Detected In Log | N/A (post-detect) | N/A | N/A | N/A | Purge/redact pipeline | Yes | Review |
| Missing Tenant Context | Do not emit cross-tenant | No | After producer fix | Reject emit | Drop unsafe emit | Yes | No |
| Broken Trace Propagation | No | N/A | N/A | No | Continue with CorrelationId | Medium | No |
| Clock Skew | No | N/A | N/A | No | Keep OccurredAt vs received | Low | Possible |
| Duplicate Audit Event | No | N/A | N/A | No | Deduped (idempotent id) | Low | Yes |
| Frontend Telemetry Blocked | No | Optional client | Optional | No | Yes (best-effort) | Low | No |

## AZ. Testing Strategy — Architecture Level

Future implementation must test: trace propagation, message correlation, tenant context, secret redaction, PII redaction, duplicate exception logging avoidance, audit durability, audit authorization, cross-tenant telemetry separation, payment secret exclusion, AI prompt-content exclusion policy, job correlation, frontend/backend trace correlation, telemetry backend outage degradation, audit backend failure policy.

No tests now.

## BA. Decision Summary

### RECOMMENDED_FOR_ADR

1. OpenTelemetry is the vendor-neutral observability foundation.
2. Technical Logging, Tracing, Metrics, Business Audit, Security Audit and Analytics are separate concerns.
3. Structured logging is required.
4. Trace/Correlation/Causation/business identifiers remain distinct concepts.
5. Context propagates across requests, jobs and messages.
6. Sensitive data uses systematic redaction/minimization.
7. TenantId is included in relevant Single-Store telemetry without exposing connection secrets.
8. Business/Security Audit is durable and not replaced by technical logs.
9. Audit records are append-oriented/tamper-resistant in design.
10. Audit retention is independent from technical-log retention.
11. Vendor-specific APM/log/SIEM SDKs stay behind infrastructure/export boundaries.
12. Optional telemetry backend failure does not normally break commerce.
13. Critical audit delivery has stronger reliability/reconciliation than normal telemetry.
14. Frontend observability and Core Web Vitals are first-class because UI quality is product-critical.
15. Health distinguishes liveness/readiness/degraded optional dependencies.
16. SLO/alerting thresholds are operational policy, not hardcoded domain constants.
17. Operational dashboards are workflow-oriented, not raw log viewers.
18. Audit access is authorization-controlled through SpiceDB.
19. Release/build/version correlation is required for production diagnosis.
20. Observability must support future microservice extraction without redesign.

### NEEDS_LATER_P00_DETAIL

- Exact structured log / audit envelope schema
- Trace/log sampling policy and rates
- SLO/SLI numeric thresholds and alerting thresholds
- Exact health-check policy per dependency
- Exact per-operation audit-failure policy (block vs continue after local write)
- Audit buffering/outbox/reconciliation design
- Cryptographic tamper-evidence design
- Retention periods (logs vs business audit vs security audit)
- Audit before/after diff representation
- Security event severity taxonomy lock
- Feature-flag payload rules (if flags exist)

### DEFERRED

- Package install, collector/exporter configuration, vendor APM/log/SIEM choice
- Implementation of logging, tracing, metrics, dashboards, alerts
- Audit database/schema, SIEM, Shopeiva
- Final ADR lock
- Next pipeline task (not invented here)
