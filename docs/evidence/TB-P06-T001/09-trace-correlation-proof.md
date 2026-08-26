# 09 — Trace correlation proof (TB-P06-T001)

| Path | Mechanism |
|---|---|
| HTTP ingress | W3C trace context via OpenTelemetry / ASP.NET Core |
| CommerceContext | `Activity.Current?.TraceId` → `CommerceContext.TraceId` |
| ProblemDetails | `traceId` extension on errors |
| Outbox | `correlation_id` from commerce trace or domain metadata |
| Auth boundary | **Aligned** to W3C TraceId (was Activity.Id — fixed) |

No custom correlation header invented; relies on standard trace propagation.

Tests: `ErrorContractTests` verifies traceId present on 500/409 responses.
