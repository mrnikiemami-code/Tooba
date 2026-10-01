# tenant-resolution-parity — TB-TMAR-HOST-ERRORS-AMC-001-W1

## Seam change

`TenantResolutionMiddleware` no longer injects `IProblemDetailsService` or calls `PlatformExceptionMapper`.

On expected resolution failure:

1. structured operational warning (TraceId + ErrorCode + Path)
2. `IExceptionPresentationService.WriteAsync(httpContext, SemanticException, …)`
3. short-circuit (no `_next`)

## Code-based failures

| Condition | Code | HTTP |
|---|---|---|
| Edition Unset | `platform.edition.unconfigured` | 503 |
| Marketplace connection missing | `platform.connection.unconfigured` | 503 |
| Unknown / inactive host (fail-closed) | `platform.resolution.failed` | 404 |

All thrown as `SemanticException(SemanticError(FoundationErrorCodes.*))`.

## Hard-coded runtime user-facing text

ZERO on the touched MultiTenancy failure path (`"Not Found"` / `"Service Unavailable"` removed).

## Fail-closed anti-enumeration

Unknown host, Disabled, and Suspended still share identical 404 `platform.resolution.failed` (no existence leakage).

## Logging

Single operational warning retained in middleware for unique commerce-resolution context; presentation service also logs canonically. No new logger/ActivitySource/Meter.

## MultiTenancy structure certification

NOT_OPENED / DEFERRED — only the presentation seam + Foundation codes were touched.
