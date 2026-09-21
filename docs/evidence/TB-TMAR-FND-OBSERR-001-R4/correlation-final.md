# Correlation final — TB-TMAR-FND-OBSERR-001-R4

SSOT: `CorrelationIdContext` (`AsyncLocal`). `ICorrelationIdProvider` and `CorrelationContextAdapter` read that value. `HttpContext.Items["Tooba.CorrelationId"]` is an optional mirror, not a second source.

## HTTP

`CorrelationIdMiddleware`:

- Valid `X-Correlation-ID` is normalized to Guid format `N` and kept.
- Invalid or missing values generate one new id for the request.
- The same id is written on the response header immediately and in `OnStarting`.
- `BeginScope` restores the previous AsyncLocal value, including nested scopes.
- ProblemDetails reads the provider, so the body id matches the header.

Host tests (after PostgresSerial isolation): valid preserve, invalid replace, conflict join. See `CorrelationRuntimeTests`.

## Messaging

`MessagingCorrelation.ResolveForPublish` prefers metadata, then ambient context, then the event id. It does not mint a second random id.

`BeginConsumeScope` restores AsyncLocal on dispose. `ToobaIntegrationTransportConsumer` uses it.

`BeginConsumeActivity` enriches a current MassTransit (or other) activity and starts `tooba.messaging.consume` only when `Activity.Current` is null.

`MassTransitIntegrationEventPublisher` sets `CorrelationId` and the `X-Correlation-ID` header, and copies `traceparent` only from the current W3C activity. It starts `tooba.messaging.publish` only when no current activity exists.

`InProcessIntegrationEventPublisher` is the Testing double. It opens a consume-side child span after restoring correlation. It is not the production bus.

## Outbox

`OutboxSaveChangesInterceptor` persists `CorrelationId` from metadata or ambient context.

`OutboxDispatcher` restores that id with `BeginScope` before delayed publish. Null legacy rows fall through `ResolveForPublish` to the event id. Dispatch starts a new `tooba.outbox.dispatch` activity. It does not invent a parent trace from a stored trace id.

## Competing SSOT

None. Middleware, provider, outbox, and MassTransit all normalize through `CorrelationIdContext`.

Verdict: PASS.
