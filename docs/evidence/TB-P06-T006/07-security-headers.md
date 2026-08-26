# 07 — Security headers (TB-P06-T006)

## Middleware

`SecurityHeadersMiddleware` (`src/backend/Host/Tooba.Host/SecurityHeadersMiddleware.cs`)

Applied globally when `EnableSecurityHeaders=true` (default).

## Headers set

| Header | Value | Notes |
|---|---|---|
| `X-Content-Type-Options` | `nosniff` | Always when enabled |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | Always when enabled |
| `X-Frame-Options` | `SAMEORIGIN` | Always when enabled |
| `Permissions-Policy` | `camera=(), microphone=(), geolocation=()` | Always when enabled |
| `Content-Security-Policy-Report-Only` | Shopeiva-tolerant policy | When `EnableCspReportOnly=true` |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` | Production + `EnableHsts=true` only |

## CSP policy (report-only)

```text
default-src 'self'; img-src 'self' data: https:; script-src 'self' 'unsafe-inline' 'unsafe-eval'; style-src 'self' 'unsafe-inline'; frame-ancestors 'self'
```

**Report-only** — does not block Shopeiva inline scripts/styles. Enforce only after visual review.

## Pipeline order

`Program.cs`: `UseCors` → `SecurityHeadersMiddleware` → `TenantResolutionMiddleware` → `SessionAuthenticationMiddleware`

## Test proof

`AuthSecurityHttpTests.Health_live_includes_security_headers` asserts nosniff, Referrer-Policy, X-Frame-Options on `/health/live`.
