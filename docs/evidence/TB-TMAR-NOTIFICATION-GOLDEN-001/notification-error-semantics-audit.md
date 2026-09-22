# Notification Error Semantics Audit

## Adoption
- Expected failures → `Result` / `Result<T>` + `SemanticError`
- HTTP mapping → `ApiResponseFactory` + `NotificationErrorCatalogContributor`
- Stable codes:
  - `customer.session.required` (401)
  - `notification.missing` (404)

## Forbidden patterns absent
- Manual `{ title, errorCode }` in Endpoints
- `ex.Message` / prose classification
- `Contains`/`StartsWith` message heuristics
- `PlatformHttpException` as Notification business outcome
- Silent catch / unknown `InvalidOperationException` swallow

## Unexpected
Unknown exceptions propagate (no swallow in handlers).

## Verdict
- Notification-Result-Adoption: HTTP_USE_CASES_ADOPTED
- Notification-Error-Classification: STABLE_CODES_ONLY
- Notification-Prose-Mapping: NONE
- Notification-Unexpected-Exception-Swallow: NONE
