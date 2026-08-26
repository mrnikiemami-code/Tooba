# Authentication & API Security Operations

## Canonical decision

```text
Host API = Bearer opaque SessionId (Guid), server-side session store
Browser clients = Next.js BFF with HttpOnly cookies (tooba_session, tooba_refresh)
BFF injects Authorization: Bearer {SessionId} server-side to Host
CSRF = double-submit (tooba_csrf cookie + X-Tooba-Csrf header) on mutating BFF routes
AUTH_SESSION_STORAGE = HTTPONLY_COOKIE_SERVER_SIDE_PROPAGATION
PRODUCTION_OTP = PROVIDER_BACKED_FAIL_CLOSED
Authorization header on Host = Bearer {SessionId} — NOT JWT
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

Related: `Tooba:TrustedProxies` — reverse-proxy IPs for forwarded headers (rate-limit client IP accuracy).

## OTP delivery (`Identity:OtpDelivery`)

| Key | Purpose | Default (dev) | Production |
|---|---|---|---|
| `Mode` | `Capturing`, `Disabled`, `Webhook` | `Capturing` | `Disabled` |
| `WebhookUrl` | Provider webhook endpoint | `""` | env-injected when Mode=Webhook |
| `WebhookApiKey` | Bearer token for webhook | `""` | env-injected |
| `TimeoutSeconds` | HTTP timeout for webhook | `10` | `10` |

| Mode | Provider | Behavior |
|---|---|---|
| `Capturing` | `CapturingOtpDeliveryProvider` | Dev/test in-memory capture |
| `Disabled` | `FailClosedOtpDeliveryProvider` | `identity.otp.delivery.unconfigured` |
| `Webhook` | `WebhookOtpDeliveryProvider` | POST `{ purpose, destination, code }` to webhook |

Implementation: `IOtpDeliveryProvider` → `OtpDeliveryProviderSender` (`IOtpSender`). Metric: `tooba.identity.otp.delivery` (outcome tag only).

## Host session model

- **Access**: `Authorization: Bearer {SessionId}` where SessionId is the persisted session Guid string.
- **Refresh**: `POST /v1/auth/refresh` with `{ sessionId, refreshToken }` JSON body (called by BFF, not browser directly).
- **Host does not set auth cookies** — cookie issuance is BFF responsibility.
- **Tenant/Edition** from resolved session; client tenant spoof rejected (`identity.tenant.untrusted`).

Implementation: `SessionAuthenticationMiddleware`, `AuthenticationHttpBoundary` (`src/backend/Host/Tooba.Host/AuthenticationHttpBoundary.cs`).

## BFF cookie model (browser clients)

| Cookie | HttpOnly | Path | Purpose |
|---|---|---|---|
| `tooba_session` | yes | `/` | SessionId for server-side Bearer injection |
| `tooba_refresh` | yes | `/api/auth` | Refresh secret for BFF refresh route |
| `tooba_csrf` | no | `/` | CSRF double-submit token |

Libraries: `src/frontend/lib/server/session-cookies.ts`, `src/frontend/lib/server/host-client.ts`, `src/frontend/lib/auth/browser-session.ts`.

Host origin env: `TOOBA_HOST_ORIGIN` (default `http://127.0.0.1:5088`).

## BFF auth routes

| Route | Method | Purpose |
|---|---|---|
| `/api/auth/csrf` | GET | Issue CSRF cookie |
| `/api/auth/login` | POST | Login; set HttpOnly cookies |
| `/api/auth/logout` | POST | Logout Host + clear cookies |
| `/api/auth/refresh` | POST | Rotate session from cookies |
| `/api/auth/me` | GET | Current user via Bearer injection |

## Customer API proxy

`/api/customer/[...path]` → Host `/v1/customer/{path}` with server-side Bearer injection.

- GET: no CSRF required.
- POST/PUT/PATCH/DELETE: CSRF + same-origin check.
- Browser clients use `credentials: "include"`; no localStorage session tokens.

## CSRF (BFF mutating routes)

1. `GET /api/auth/csrf` sets `tooba_csrf`.
2. Mutating requests send `X-Tooba-Csrf` header matching cookie.
3. Failure: HTTP 403 + `auth.csrf.invalid`.

Protected: `/api/auth/login|logout|refresh`, mutating `/api/customer/*`.

## CORS

- Policy name: `ToobaCors` on Host `/v1/auth/*` and health endpoints.
- BFF routes are same-origin to the Next.js app — browser calls BFF, BFF calls Host server-side.

## Rate limits (Host)

Sliding window per client IP + operation. Exceeded: HTTP 429 + `identity.rate_limited`.

Configure `Tooba:TrustedProxies` behind reverse proxy for accurate IP keys.

## Security headers

Applied by `SecurityHeadersMiddleware` when enabled (see TB-P06-T006 evidence).

## Observability

- Auth metric: `tooba.authentication.event` — tags `outcome`, `operation`.
- OTP metric: `tooba.identity.otp.delivery` — tag `outcome` only.
- **Never log** passwords, refresh tokens, OTP codes, or raw `Authorization` headers.

## Error secrecy

| Scenario | Status | errorCode |
|---|---|---|
| Bad login credentials | 401 | `identity.authentication.failed` |
| Rate limited | 429 | `identity.rate_limited` |
| Invalid session | 401 | `identity.session.invalid` |
| CSRF failure (BFF) | 403 | `auth.csrf.invalid` |
| OTP unconfigured (Production) | — | `identity.otp.delivery.unconfigured` |
| Password reset request (any identifier) | 200 | `{ accepted: true }` |

## Troubleshooting

| Symptom | Check |
|---|---|
| 403 on customer mutating calls | CSRF cookie/header; call `ensureCsrfCookie()` first |
| 401 on `/api/customer/*` | Session cookie expired; login via `/api/auth/login` |
| OTP flows fail in Production | Expected until `Mode=Webhook` configured |
| BFF cannot reach Host | `TOOBA_HOST_ORIGIN` env |
| Cookies not set in Production | HTTPS required (`secure: true`) |

## Integration tests

```powershell
dotnet test src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj --filter "FullyQualifiedName~OtpDeliveryProviderTests|FullyQualifiedName~AuthSecurityHttpTests"
cd src/frontend
npm run test -- lib/auth/csrf.test.ts
```

## Evidence

- TB-P06-T006 (Host auth hardening): `docs/evidence/TB-P06-T006/`
- TB-P06-T007 (OTP + BFF cookies): `docs/evidence/TB-P06-T007/`
