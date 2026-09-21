# Tracing final — TB-TMAR-FND-OBSERR-001-R4

Exactly one `AddOpenTelemetry()` call, in `Tooba.Host/Program.cs`.

Ownership:

- ASP.NET Core instrumentation owns server spans. `/health` and `/ready` are filtered out.
- HttpClient instrumentation owns client spans.
- MassTransit source name `MassTransit` is added to the same provider. Tooba does not start a second transport span when a MassTransit activity is current.
- `ToobaTelemetry` activities are internal children or fallbacks only when no current activity exists. HTTP enrich never creates a fallback server span (`EnrichHttpRequest` returns on `CreateFallback`).

`TracingBehavior` is registered once in `ToobaCqrsRegistration`.

Span names are `mediatr.{module}.{operation}` from the request type name. Tags are module, operation, request kind, correlation, and request type (the operation name, not a payload). On failure the only exception tag is `exception.type`. There is no `exception.Message` tag and no request-body tag.

Offer endpoints and handlers do not call `StartActivity`. Module calls go through `IModuleCallTracer`.

Verdict: PASS.
