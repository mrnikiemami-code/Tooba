# 10 — Error secrecy on auth endpoints (TB-P06-T006)

## ProblemDetails contract

`AuthProblem` in `AuthenticationHttpBoundary.cs`:

- `application/problem+json`
- Fields: `status`, `title`, `type: about:blank`
- Extensions: `traceId`, `errorCode`
- No password, token, OTP, or identifier echoed in response

## Enumeration-safe responses

| Endpoint | Condition | Status | errorCode | Notes |
|---|---|---|---|---|
| Login | Bad credentials or invalid kind | 401 | `identity.authentication.failed` | Same response for unknown user vs wrong password |
| Login | Rate limited | 429 | `identity.rate_limited` | |
| Refresh | Invalid session/refresh | 401 | `identity.session.invalid` | |
| Password reset request | Any identifier | 200 | `{ accepted: true }` | Always accepted envelope |
| Password reset complete | Bad challenge | 400 | `identity.challenge.invalid` | |
| Register | Duplicate identifier | 409 | `identity.identifier.conflict` | |
| Me / protected | No session | 401 | `identity.session.invalid` | |
| Tenant spoof | Header/body/query/cookie | 400 | `identity.tenant.untrusted` | |

## Logging boundary

Information logs: `identity.login.succeeded`, `identity.login.failed`, `identity.refresh.failed`, `identity.register.succeeded`.

**Not logged**: raw Authorization header, passwords, refresh tokens, OTP codes (per `AuthenticationInstrumentation` doc comment).

## Authorization header

`SessionAuthenticationMiddleware` parses Bearer token but does not log it.
