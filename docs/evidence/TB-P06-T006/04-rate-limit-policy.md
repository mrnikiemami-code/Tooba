# 04 — Rate limit policy (TB-P06-T006)

## Implementation

- Class: `AuthenticationRateLimitThrottleSeam` (`src/backend/Host/Tooba.Host/AuthenticationRateLimitThrottleSeam.cs`)
- Key: `{RemoteIpAddress}:{operation}` (in-memory sliding window per Host instance)
- Config: `AuthRateLimitPermitLimit`, `AuthRateLimitWindowSeconds`

## Throttled operations

| Operation key | Endpoint |
|---|---|
| `login` | `POST /v1/auth/login` |
| `refresh` | `POST /v1/auth/refresh` |
| `password_reset_request` | `POST /v1/auth/password-reset/request` |
| `password_reset_complete` | `POST /v1/auth/password-reset/complete` |
| `identifier_verification_request` | `POST /v1/auth/identifier-verification/request` |
| `identifier_verification_complete` | `POST /v1/auth/identifier-verification/complete` |

## Not throttled (by design)

Register, logout, logout-all, password-change, `/me` — lower abuse surface or require authenticated session.

## Response on exceed

HTTP **429 Too Many Requests**

```json
{
  "status": 429,
  "title": "Too Many Requests",
  "errorCode": "identity.rate_limited",
  "traceId": "..."
}
```

Enumeration-safe: same error code regardless of identifier validity.

## Telemetry

`AuthenticationInstrumentation.RecordThrottled(operation)` → `tooba.authentication.event` with `outcome=throttled`.

## Test proof

`AuthSecurityHttpTests.Login_rate_limit_returns_429_with_stable_error_code` — limit=2, third attempt returns 429 + `identity.rate_limited`.
