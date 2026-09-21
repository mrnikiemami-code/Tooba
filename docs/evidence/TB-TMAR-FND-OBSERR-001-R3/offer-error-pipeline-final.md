# Offer error pipeline final — TB-TMAR-FND-OBSERR-001-R3

## Shape

Endpoint → `ISender` / gateway → exception bubbles → `ToobaExceptionHandler` → `IExceptionPresentationService` → ProblemDetails

## Removed from Offer seller endpoints

- Local try/catch for SemanticException / PlatformHttpException
- `ApiResponseFactory` / Accept-Language parameter passing
- Local language switch / hardcoded error titles
- Local `Results.Json(new ProblemDetails…)`

## Auth transport

`IOfferSellerAuthorizer` may still throw `PlatformHttpException`; handled by global pipeline.
