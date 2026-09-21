# Recovery SoT — TB-TMAR-FND-OBSERR-001-R1

## Module-Recovery-State

FOUNDATION_PHASE1_COMPLETE

## Offer-State

REOPENED_WAITING_CENTRAL_FOUNDATION

## What landed

- Central CorrelationId AsyncLocal SSOT + provider/context + Host middleware
- IProblemDetailsContextProvider + immutable ToobaProblemDetailsContext
- SafeErrorMapper (Semantic/Validation/Platform/unknown)
- IRequestLocaleResolver (no FA-first, no AcceptLanguage.Contains ad-hoc)
- ApiResponseFactory / ToobaProblemDetailsFactory
- ObservabilityLogScope + ToobaTracingPolicy/Enricher (API/policy only)
- AddToobaObservabilityFoundation DI
- Offer seller endpoints use ApiResponseFactory; OfferEndpointLocalizer retained as contributor residual
- ToobaExceptionHandler delegated to central factory

## Next

TB-TMAR-FND-OBSERR-001-R2 — wire request/messaging correlation + full log scope + MediatR/module-path tracing topology validation.
