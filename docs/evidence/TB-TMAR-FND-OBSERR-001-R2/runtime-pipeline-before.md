# Runtime pipeline before R2 edits

Source: `src/backend/Host/Tooba.Host/Program.cs` at `55fab5c4` (pre-R2 working tree).

## Middleware order (actual `app.Use*` sequence)

1. `app.UseExceptionHandler()` — `ToobaExceptionHandler`
2. `app.UseToobaCorrelationId()` — `CorrelationIdMiddleware` (R1)
3. `app.UseForwardedHeaders()` — **only when** `Tooba:TrustedProxies` is non-empty
4. `app.UseCors("ToobaCors")`
5. `app.UseMiddleware<SecurityHeadersMiddleware>()`
6. `app.UseMiddleware<TenantResolutionMiddleware>()` — builds `CommerceContext`, sets Activity tags + logger scope Edition/Deployment/Tenant
7. `app.UseMiddleware<SessionAuthenticationMiddleware>()` — Bearer/cookie session → `CurrentAuthenticatedSession`

Then endpoint mapping (`MapAuthenticationBoundary`, admin/storefront/seller/Offer/…) and health.

## Ordering observations (pre-R2)

| Concern | Pre-R2 fact |
| --- | --- |
| Forwarded headers vs correlation | Forwarded headers run **after** correlation (when enabled) |
| Exception vs correlation | Exception handler is **outside** correlation; AsyncLocal may be awkward on error path |
| Tenant/store/actor enrichment | Correlation log scope only has CorrelationId + RequestId; no Tenant/Store/Actor nested scope |
| Auth | After tenant resolution |

## MassTransit publisher

`MassTransitIntegrationEventPublisher`:

- Starts `tooba.messaging.publish` Activity on `ToobaTelemetry.ActivitySource`
- Publishes `ToobaIntegrationTransportMessage`
- Sets `context.CorrelationId = meta.EventId` (EventId, **not** business CorrelationId)
- Custom headers: `tooba.event-type`, `tooba.tenant-id`, `tooba.edition`, `tooba.deployment-id`, `tooba.event-id`
- No explicit W3C `traceparent` header; relies on MassTransit/OpenTelemetry

## MassTransit consumer

`ToobaIntegrationTransportConsumer`:

- Always starts new `tooba.messaging.consume` Activity (does not enrich MassTransit-owned Activity)
- Commerce context from envelope; `traceId = envelope.CorrelationId ?? envelope.EventId`
- Does **not** establish `CorrelationIdContext` scope / restore

## Outbox dispatcher

`OutboxDispatcher.DispatchTargetAsync`:

- Starts `tooba.outbox.dispatch` Activity
- Assigns commerce via `FromOutbox(message, message.CorrelationId ?? message.Id)`
- Publishes via `IIntegrationEventPublisher`
- Does **not** `CorrelationIdContext.BeginScope` from persisted CorrelationId

## Outbox persistence

`OutboxMessage.CorrelationId` already mapped (max 128). Written in `OutboxSaveChangesInterceptor` as `domain.Metadata.CorrelationId ?? commerce.TraceId` (commerce TraceId is Activity/HTTP trace, not necessarily CorrelationId SSOT). No TraceParent/TraceState columns.

## In-process test publisher

`InProcessIntegrationEventPublisher`: starts `tooba.outbox.publish`, invokes handlers in-process; no CorrelationIdContext restore/propagate beyond ambient AsyncLocal.

## OpenTelemetry sources (Host)

Already registered: `ToobaTelemetry.ActivitySourceName` (`Tooba`), `MassTransit`, ASP.NET Core + HttpClient instrumentation. Single `AddOpenTelemetry()` pipeline.

## Activity / traceparent behavior (pre-R2)

- HTTP: ASP.NET Core instrumentation owns server span; `ToobaTraceEnricher.EnrichHttpRequest` tags `tooba.correlation_id` when Activity present
- MediatR: `LoggingBehavior` only — **no** `TracingBehavior`
- Module calls: no `IModuleCallTracer` / module-call spans
- Messaging: custom Tooba Activities often duplicate rather than enrich MassTransit
