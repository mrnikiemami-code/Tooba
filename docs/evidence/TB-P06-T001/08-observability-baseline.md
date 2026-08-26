# 08 — Observability baseline (TB-P06-T001)

| Component | Status |
|---|---|
| OpenTelemetry | `AddOpenTelemetry` in `Program.cs` |
| ASP.NET Core instrumentation | Yes; health paths filtered from noise |
| HttpClient instrumentation | Yes |
| MassTransit activity source | When messaging enabled |
| Custom meter (Outbox) | `ToobaTelemetry.Meter` |
| Exporters | OTLP when `Tooba:Observability:OtlpEndpoint` set |
| Structured logging | JSON console + activity tracking (SpanId/TraceId/ParentId) |
| Service attributes | `service.name`, `service.version`, `deployment.environment` |
| Business Audit / Security Audit | **Not collapsed** — out of scope; technical log only |

DB-specific EF instrumentation: deferred (not required for this baseline).
