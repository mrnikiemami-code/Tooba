# Recovery SoT — TB-TMAR-FND-OBSERR-001-R2

## Module-Recovery-State

FOUNDATION_RUNTIME_TRACING_COMPLETE

## Offer-State

REOPENED_WAITING_CENTRAL_FOUNDATION

## What landed

- Runtime pipeline reorder: ForwardedHeaders → Correlation → ExceptionHandler → … → Tenant → Auth → RequestObservabilityEnrichment
- CorrelationIdMiddleware scope restore + response header + Activity enrich
- Nested request log scope (Tenant/Store/Actor/IP when available)
- TracingBehavior + normalized LoggingBehavior
- IModuleCallTracer / ModuleCallTrace
- Offer golden-path gateway decorators (Catalog/Party/Pricing/Inventory)
- MassTransit + in-process + outbox CorrelationId propagation/restore
- MessagingCorrelation helper; W3C headers as backup
- Tests + architecture guards + evidence pack

## Next

TB-TMAR-FND-OBSERR-001-R3 — localization catalog / SafeErrorMapper normalize / ToobaExceptionHandler full factory / repo-wide guards

## Residuals (R3/R4)

- OfferEndpointLocalizer bilingual contributor
- SafeErrorMapper heuristic classification cleanup
- ToobaExceptionHandler / endpoint presentation full centralization
- Optional Outbox TraceParent additive columns (deferred)
