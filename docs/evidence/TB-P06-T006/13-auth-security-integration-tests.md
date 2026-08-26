# 13 — Auth security integration tests (TB-P06-T006)

## Test class

`AuthSecurityHttpTests` — `src/backend/Host/Tooba.Host.Tests/AuthSecurityHttpTests.cs`

Collection: `PostgresSerial` (Testcontainers PostgreSQL).

## Tests

| Test | Proves |
|---|---|
| `Login_rate_limit_returns_429_with_stable_error_code` | After `AuthRateLimitPermitLimit=2`, third login attempt => 429 + `identity.rate_limited` |
| `Health_live_includes_security_headers` | `/health/live` returns X-Content-Type-Options, Referrer-Policy, X-Frame-Options |
| `Cors_allows_configured_origin_on_simple_request` | Configured origin receives `Access-Control-Allow-Origin` echo |
| `Production_otp_sender_is_fail_closed` | `ProductionOtpSender` throws `identity.otp.delivery.unconfigured` |

## Factory configuration

`AuthSecurityFactory` (nested in test file):

- Environment: `Testing`
- Single-Store tenants with Testcontainers PostgreSQL
- Overrides `Tooba:AuthSecurity:AuthRateLimitPermitLimit`, window, headers
- Optional `CorsAllowedOrigins:0` for CORS test

## Run

```powershell
dotnet test src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj --filter AuthSecurityHttpTests
```

Requires Docker for PostgreSQL Testcontainers (SkippableFact when unavailable).

## Full Host suite

221 tests passed as part of `dotnet test src/backend/Tooba.slnx` validation.
