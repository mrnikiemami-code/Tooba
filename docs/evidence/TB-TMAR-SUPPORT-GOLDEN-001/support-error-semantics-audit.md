# Support Error Semantics Audit

## Adoption
- Expected failures → `Result` / `Result<T>` + `SemanticError`
- HTTP mapping → `ApiResponseFactory` + `SupportErrorCatalogContributor`
- Directory/domain `InvalidOperationException` → `SupportExceptionMapper` (exact known codes only)

## Stable public codes
| Code | HTTP |
|------|------|
| `customer.session.required` | 401 |
| `support.missing` | 404 |
| `support.rejected` | 400 |
| `support.reply.rejected` | 400 |
| `support.action.rejected` | 400 |
| `support.patch.rejected` | 400 |
| `seller.authorization.denied` | 403 |
| `admin.authorization.denied` | 403 |
| `support.demo.not_ready` | 503 |

## Forbidden patterns absent
- Manual `{ title, errorCode }` in Endpoints
- `ex.Message` / prose classification
- `Contains`/`StartsWith` heuristics in mapper
- Generic `catch InvalidOperationException => rejected` in Endpoints
- `PlatformHttpException` for Support **business** outcomes (auth denials still PlatformHttpException from Host adapters — transport)

## Unexpected
Unknown exceptions propagate (mapper rethrows; Endpoints do not swallow).

## Verdict
- Support-Result-Adoption: HTTP_USE_CASES_ADOPTED
- Support-Error-Classification: STABLE_CODES_ONLY
- Support-Prose-Mapping: NONE
- Support-Unexpected-Exception-Swallow: NONE
