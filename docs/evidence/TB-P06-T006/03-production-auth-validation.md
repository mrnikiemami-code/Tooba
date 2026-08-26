# 03 — Production auth validation (TB-P06-T006)

## Startup validation (`AuthSecurityOptionsValidator`)

| Rule | Failure message |
|---|---|
| `AuthRateLimitPermitLimit <= 0` | Auth rate limit permit limit must be positive. |
| `AuthRateLimitWindowSeconds <= 0` | Auth rate limit window must be positive. |
| `MaxRequestBodyBytes <= 0` | Max request body bytes must be positive. |
| Any `CorsAllowedOrigins` entry is `*` | CORS wildcard origin is forbidden. |

Registered via `.ValidateOnStart()` in `Program.cs`.

## Production OTP fail-closed

| Environment | `IOtpSender` | Behavior |
|---|---|---|
| Production | `ProductionOtpSender` | Throws `identity.otp.delivery.unconfigured` |
| Non-Production | `CapturingOtpSender` | In-memory capture for dev/test |

Wiring: `IdentityModule.cs` — `environment.IsProduction()` branch.

No silent fallback to CapturingOtpSender in Production.

## Production config snapshot

`appsettings.Production.json`:

- `AuthRateLimitPermitLimit`: 20
- `CorsAllowedOrigins`: `[]` (ops must set explicit frontend origins)
- `EnableSecurityHeaders`, `EnableHsts`, `EnableCspReportOnly`: true

## Not in scope

- JWT validation (architecture uses opaque SessionId, not JWT).
- Cookie-based session or antiforgery token validation.
