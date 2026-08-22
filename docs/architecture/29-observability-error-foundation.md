# Tooba — Observability & Error Handling Foundation

Status:

```text
P01 foundation — candidate layout; not an ADR; not a P01 Gate
```

Task:

```text
TB-P01-T002
```

```text
Technical Log != Business Audit != Security Audit != Analytics
```

This task implements technical observability and API/frontend error foundations only. Technical logs are not durable business or security audit records. Later business actions must emit dedicated audit through a separate capability. PageView/Purchase and similar product analytics must not travel through this technical OpenTelemetry/logging path as analytics truth.

## What was implemented

- Vendor-neutral OpenTelemetry on `Tooba.Host` (ASP.NET Core, HttpClient, runtime metrics).
- Minimal Tooba instrumentation seam in BuildingBlocks: `ToobaTelemetry` (`ActivitySource` / `Meter` named `Tooba`).
- Structured JSON console logging with W3C `TraceId` / `SpanId` activity tracking.
- Centralized exception handling to `application/problem+json` (`ProblemDetails`).
- Existing `GET /health` (liveness) and `GET /ready` (host foundation ready). No fake database readiness.
- Next.js App Router `error.tsx`, `global-error.tsx`, and `not-found.tsx` as safe non-commercial fallbacks.
- Lightweight Host integration tests for the error contract.

## OpenTelemetry instrumentation

Configured on the Host composition root only:

- Traces: ASP.NET Core requests (health/ready filtered), HttpClient, `Tooba` `ActivitySource`.
- Metrics: ASP.NET Core, HttpClient, runtime, `Tooba` `Meter`.
- Resource attributes: `service.name`, `service.version`, `deployment.environment`.
- No tenant/user attributes on the resource (those foundations do not exist yet).
- W3C trace context is the ASP.NET Core / OpenTelemetry default; incoming valid traceparent is continued. `HttpContext.TraceIdentifier` is a fallback only when `Activity.Current` is absent; `ProblemDetails.traceId` prefers the W3C TraceId.

## Exporter / configuration

Host configuration (also bindable from environment variables via ASP.NET configuration, e.g. `Tooba__Observability__OtlpEndpoint`):

```text
Tooba:Observability:ServiceName
Tooba:Observability:OtlpEndpoint
Tooba:Observability:EnableTracing
Tooba:Observability:EnableMetrics
```

OTLP exporter is registered only when `OtlpEndpoint` is non-empty. An empty endpoint is the repository default. The process must start without a collector. No collector URL is hard-coded. No vendor APM package is referenced.

## Technical logging policy

- JSON console logs. Microsoft.AspNetCore default level remains Warning in non-Development to reduce noise.
- Activity tracking includes TraceId, SpanId, ParentId.
- Safe fields may include method, path, status, TraceId, environment, service.
- Do not log secrets, `Authorization` values, cookies, passwords, OTP, connection strings, payment payloads, or full query strings (query strings may later carry sensitive tokens).
- Exception handler logs unexpected failures with TraceId, method, and path — not headers or query strings.
- Technical logs are rotatable diagnostics. They are not a substitute for Business Audit, Security Audit, or Analytics.

## ProblemDetails contract

API errors use RFC 9110-style `ProblemDetails` with content type `application/problem+json`.

Stable extensions:

```text
traceId   (always)
errorCode (optional; omitted for generic unexpected errors)
```

Conceptual mapping (not a business taxonomy):

| Outcome | Typical status | Current seam |
| --- | --- | --- |
| Client/validation | 400 | `BadHttpRequestException`; later module validation |
| Unauthorized | 401 | `PlatformHttpException` |
| Forbidden | 403 | `PlatformHttpException` |
| Not found | 404 | `PlatformHttpException` |
| Conflict | 409 | `PlatformHttpException` |
| Unexpected | 500 | any unmapped exception |

`PlatformHttpException` in BuildingBlocks is a Host mapping seam only. Modules must not treat it as domain language.

Production payloads must not include stack traces, file paths, SQL, connection strings, or secrets. Development 500 responses may include the exception type name only.

## Health / readiness

```text
GET /health  → process/application alive
GET /ready   → host foundation ready (not database, cache, or bus)
```

Responses are non-secret JSON status only. Future health checks can attach here later.

## Frontend error boundary

- `app/error.tsx` — route-level client boundary (required by Next.js).
- `app/global-error.tsx` — root client boundary replacing the root layout.
- `app/not-found.tsx` — RSC not-found fallback.

These are safe placeholders, not commercial branded UX, not Shopeiva copies, and not visually accepted product UI.

## Frontend telemetry seam (deferred)

Do not install a heavy RUM/APM vendor in this phase.

Later, when authorized:

- Web Vitals (Next.js `useReportWebVitals` or equivalent lightweight hook).
- Frontend error reporting correlated with backend TraceId where feasible.
- Navigation/performance telemetry.

None of that is product analytics (see `16-first-party-analytics.md`).

## Deferred

Production collector, Grafana, Jaeger, Tempo, Prometheus, vendor APM, audit stores, analytics ingestion, tenant/user enrichment, database/cache/bus health, alerting, commercial error UX, Design System, Data Grid, Shopeiva study.

## Tests

`src/backend/Host/Tooba.Host.Tests` uses `WebApplicationFactory<Program>`:

- unexpected exception → 500 ProblemDetails with `traceId`, no stack/secrets in body;
- mapped `PlatformHttpException` → 409 with optional `errorCode`;
- `/health` and `/ready` remain OK.
