# 18 — Zero secret output proof (TB-P06-T007)

## OTP delivery

| Rule | Implementation |
|---|---|
| No OTP in logs | Providers never log `OneTimeCode`; instrumentation records outcome tag only |
| No OTP in metrics | `tooba.identity.otp.delivery` — `outcome` tag only |
| Capturing provider | `LastCode` property for test assertions only; not serialized to HTTP |

## Session tokens

| Rule | Implementation |
|---|---|
| No tokens in login JSON | BFF returns `{ userId, authenticated }` only |
| HttpOnly cookies | `tooba_session`, `tooba_refresh` not readable by JS |
| Server-side Bearer | `host-client.ts` injects Bearer; never echoed to browser response body |

## CSRF

Token is non-secret synchronizer; still not logged in production telemetry.

## Tests

- `OtpDeliveryProviderTests`: asserts error code string, not OTP payload in exception messages beyond controlled capture property.
- No test output dumps refresh tokens or session Guids to stdout.
