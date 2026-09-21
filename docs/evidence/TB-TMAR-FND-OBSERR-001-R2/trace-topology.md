# Trace topology evidence — TB-TMAR-FND-OBSERR-001-R2

## Layers proven

1. HTTP correlation middleware + enrichment
2. MediatR `mediatr.{module}.{request}` spans (`TracingBehavior`)
3. Module-call spans via `IModuleCallTracer` on Offer golden gateways
4. Messaging publish/consume correlation scopes (`MessagingCorrelation`)
5. Outbox dispatcher correlation restore

## Tests

- `Tooba.BuildingBlocks.Tests` Observability suite (TracingBehavior, ModuleCallTracer, MessagingCorrelation, log scope, single registration)
- `Tooba.Offer.Tests` OfferTraceTopologyTests + architecture guards
- `Tooba.Host.Tests` CorrelationRuntimeTests

## OpenTelemetry sources

Unchanged single pipeline: `Tooba` + `MassTransit` + ASP.NET Core + HttpClient. No second provider.
