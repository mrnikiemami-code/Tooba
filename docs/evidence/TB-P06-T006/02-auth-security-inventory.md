# 02 — Auth security inventory (TB-P06-T006)

## Authentication boundary (pre-hardening baseline)

| Component | Path | Role |
|---|---|---|
| Session middleware | `AuthenticationHttpBoundary.cs` (`SessionAuthenticationMiddleware`) | Resolve `Bearer {SessionId}` to `CurrentAuthenticatedSession` |
| HTTP routes | `AuthenticationEndpointMapper.MapAuthenticationBoundary` | `/v1/auth/*` register/login/refresh/logout/reset/verification/me |
| Throttle seam interface | `IAuthenticationThrottleSeam` | Hook for auth-sensitive rate limits |

## New / updated in TB-P06-T006

| Component | Path | Role |
|---|---|---|
| Options | `AuthSecurityHostOptions.cs` | `Tooba:AuthSecurity` — CORS, headers, rate limits, body size |
| Options validator | `AuthSecurityOptionsValidator` | Startup validation; forbids CORS `*` |
| Rate limit | `AuthenticationRateLimitThrottleSeam.cs` | IP+operation sliding window; 429 |
| Security headers | `SecurityHeadersMiddleware.cs` | nosniff, Referrer-Policy, X-Frame-Options, Permissions-Policy, CSP-Report-Only, HSTS |
| Telemetry | `AuthenticationInstrumentation.cs` | `tooba.authentication.event` counter |
| Production OTP | `ProductionOtpSender.cs` | Fail-closed OTP delivery |
| Identity wiring | `IdentityModule.cs` | `ProductionOtpSender` (Production) / `CapturingOtpSender` (non-Production) |
| Host pipeline | `Program.cs` | CORS `ToobaCors`, Kestrel body limit, forwarded headers, middleware order |
| Health CORS | `HostHealthEndpoints.cs` | `RequireCors("ToobaCors")` on live + legacy health |
| Tests | `AuthSecurityHttpTests.cs` | Rate limit, headers, CORS, Production OTP |

## Configuration keys (`Tooba:AuthSecurity`)

| Key | Default (`appsettings.json`) |
|---|---|
| `CorsAllowedOrigins` | `[]` |
| `EnableSecurityHeaders` | `true` |
| `EnableHsts` | `true` |
| `EnableCspReportOnly` | `true` |
| `AuthRateLimitPermitLimit` | `30` (Production: `20`) |
| `AuthRateLimitWindowSeconds` | `60` |
| `MaxRequestBodyBytes` | `10485760` |

Related: `Tooba:TrustedProxies` in `appsettings.json` (empty by default).

## Session model (locked)

- Access token = SessionId Guid string in `Authorization: Bearer` header.
- Refresh token = opaque string in JSON body only.
- **No auth cookies** issued by Host.
