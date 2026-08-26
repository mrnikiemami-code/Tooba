# Authentication & API Security Operations

## Canonical decision

```text
Authentication = Bearer opaque SessionId (Guid), server-side session store
Refresh token = opaque secret in JSON body (not a cookie)
Authorization header = Bearer {SessionId} — NOT JWT
CSRF = N/A (Bearer-only API; no auth cookies)
Production OTP = fail-closed until external provider wired
```

## Configuration (`Tooba:AuthSecurity`)

| Key | Purpose | Default |
|---|---|---|
| `CorsAllowedOrigins` | Explicit allowed origins; empty = deny cross-origin | `[]` |
| `EnableSecurityHeaders` | X-Content-Type-Options, Referrer-Policy, X-Frame-Options, Permissions-Policy | `true` |
| `EnableHsts` | Strict-Transport-Security in Production | `true` |
| `EnableCspReportOnly` | CSP-Report-Only (Shopeiva-safe; not enforced) | `true` |
| `AuthRateLimitPermitLimit` | Max auth-sensitive requests per IP+operation per window | `30` (Production: `20`) |
| `AuthRateLimitWindowSeconds` | Rate-limit window length | `60` |
| `MaxRequestBodyBytes` | Kestrel max request body | `10485760` (10 MiB) |

Related: `Tooba:TrustedProxies` — list of reverse-proxy IP addresses for forwarded headers (rate-limit client IP accuracy).

## Session model

- **Access**: `Authorization: Bearer {SessionId}` where SessionId is the persisted session Guid string.
- **Refresh**: `POST /v1/auth/refresh` with `{ sessionId, refreshToken }` JSON body.
- **No auth cookies** are set by Host; do not enable cookie-based sessions without Architect envelope.
- **Tenant/Edition** come from resolved session; client `TenantId` headers/body/query/cookies are rejected (`identity.tenant.untrusted`).

Implementation: `SessionAuthenticationMiddleware`, `AuthenticationHttpBoundary` (`src/backend/Host/Tooba.Host/AuthenticationHttpBoundary.cs`).

## CORS

- Policy name: `ToobaCors`.
- Registered in `Program.cs`; applied to `/v1/auth/*` and `/health/live`, `/health` when enabled.
- **Production**: set explicit frontend origins in `CorsAllowedOrigins`. Wildcard `*` is forbidden at startup validation.
- **Empty array**: cross-origin denied (same-origin or reverse-proxy path only).

Development example (`appsettings.Development.json`): `http://localhost:3001`, `http://127.0.0.1:3001`.

## Rate limits

Sliding window per **client IP + operation** via `AuthenticationRateLimitThrottleSeam`.

| Operation | Endpoint |
|---|---|
| `login` | `POST /v1/auth/login` |
| `refresh` | `POST /v1/auth/refresh` |
| `password_reset_request` | `POST /v1/auth/password-reset/request` |
| `password_reset_complete` | `POST /v1/auth/password-reset/complete` |
| `identifier_verification_request` | `POST /v1/auth/identifier-verification/request` |
| `identifier_verification_complete` | `POST /v1/auth/identifier-verification/complete` |

Exceeded limit: HTTP **429** with `errorCode: identity.rate_limited` (enumeration-safe).

Behind a reverse proxy: configure `Tooba:TrustedProxies` so `X-Forwarded-For` resolves to the real client IP.

## Security headers

Applied by `SecurityHeadersMiddleware` on all responses when enabled:

| Header | Value |
|---|---|
| `X-Content-Type-Options` | `nosniff` |
| `Referrer-Policy` | `strict-origin-when-cross-origin` |
| `X-Frame-Options` | `SAMEORIGIN` |
| `Permissions-Policy` | `camera=(), microphone=(), geolocation=()` |
| `Content-Security-Policy-Report-Only` | Shopeiva-tolerant report-only policy |
| `Strict-Transport-Security` | Production only when HSTS enabled |

CSP is **report-only** to avoid breaking Shopeiva inline scripts; enforce only after visual review.

## Request body limits

Kestrel `MaxRequestBodySize` bound from `MaxRequestBodyBytes`. Oversized bodies rejected at server level before handler execution.

## Production OTP

- **Production**: `ProductionOtpSender` — throws `identity.otp.delivery.unconfigured`. Password-reset and verification challenges cannot deliver OTP until an external SMS/email provider is registered.
- **Non-Production**: `CapturingOtpSender` captures codes in memory for dev/test.

Wire a real `IOtpSender` implementation before enabling password-reset or identifier-verification flows in Production.

## Observability

- Metric: `tooba.authentication.event` — tags `outcome`, `operation`.
- Throttled requests recorded as `outcome=throttled`.
- **Never log** passwords, refresh tokens, OTP codes, or raw `Authorization` headers.

## Error secrecy

| Scenario | Status | errorCode |
|---|---|---|
| Bad login credentials | 401 | `identity.authentication.failed` |
| Rate limited | 429 | `identity.rate_limited` |
| Invalid session | 401 | `identity.session.invalid` |
| Password reset request (any identifier) | 200 | `{ accepted: true }` |
| Tenant spoof attempt | 400 | `identity.tenant.untrusted` |

## Trusted proxies

When `Tooba:TrustedProxies` is non-empty:

1. `UseForwardedHeaders` enabled for X-Forwarded-For, X-Forwarded-Proto, X-Forwarded-Host.
2. Only listed IP addresses accepted as known proxies.
3. Required for correct rate limiting and HSTS behind TLS-terminating load balancers.

## Troubleshooting

| Symptom | Check |
|---|---|
| 429 on login | Rate limit; shared NAT IP; lower `AuthRateLimitPermitLimit` or widen window |
| CORS blocked in browser | Origin in `CorsAllowedOrigins`; preflight from allowed origin |
| OTP flows fail in Production | Expected until provider wired; check `identity.otp.delivery.unconfigured` |
| Wrong client IP in rate limit | `TrustedProxies` not configured behind reverse proxy |
| Missing security headers | `EnableSecurityHeaders=false` in config |

## Integration tests

```powershell
dotnet test src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj --filter AuthSecurityHttpTests
```

Requires Docker for Testcontainers PostgreSQL.

## Evidence

Task evidence: `docs/evidence/TB-P06-T006/`
