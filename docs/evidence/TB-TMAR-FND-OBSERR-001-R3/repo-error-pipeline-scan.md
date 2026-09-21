# Repo error pipeline scan — TB-TMAR-FND-OBSERR-001-R3

## FOUNDATION_FIXED

- `ClassifySemanticCode` / naming heuristics removed
- Explicit ErrorDescriptor catalog
- Resource-based localizer; no FA-first in foundation
- Thin `ToobaExceptionHandler` + `IExceptionPresentationService`
- Context-based `ApiResponseFactory` (test-only Accept-Language override)

## OFFER_FIXED

- `OfferEndpointLocalizer` deleted
- Offer .resx + catalog contributor
- Seller endpoints thin; no Accept-Language / local ProblemDetails mapping
- Guards: no PlatformHttpException in Domain/Application

## LEGACY_OTHER_MODULE / FUTURE_MIGRATION

- Host/Catalog/Content/Notification/Fulfillment/Promotion `StartsWith("fa"|"en")` for **catalog locale preference** (not error presentation)
- Widespread Host `PlatformHttpException` + local catch/`ToError` patterns
- `PlatformExceptionMapper` residual in Host tenant middleware / tests

## BLOCKS_GOLDEN_OFFER

- None for seller Offer golden path after R3

## Do not migrate entire repo in R3

Confirmed.
