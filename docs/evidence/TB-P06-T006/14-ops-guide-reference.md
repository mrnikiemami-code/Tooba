# 14 — Ops guide reference (TB-P06-T006)

## Operations guide

Created: `docs/operations/authentication-security.md`

Mirrors style of `docs/operations/authorization-spicedb.md`.

## Contents

| Section | Topics |
|---|---|
| Canonical decision | Bearer SessionId, no cookies, CSRF N/A, Production OTP fail-closed |
| Configuration | Full `Tooba:AuthSecurity` key table |
| Session model | Bearer header, refresh JSON, tenant from session |
| CORS | ToobaCors policy, Production origin requirements |
| Rate limits | Throttled operations table, 429 behavior |
| Security headers | Header values, CSP report-only note |
| Request body limits | Kestrel MaxRequestBodySize |
| Production OTP | ProductionOtpSender vs CapturingOtpSender |
| Observability | `tooba.authentication.event` metric |
| Error secrecy | errorCode reference table |
| Trusted proxies | Forwarded headers setup |
| Troubleshooting | Common symptoms |
| Integration tests | Filter command |

## Related architecture docs

- `docs/architecture/41-authentication-http-boundary.md`
- `docs/architecture/37-identity-authentication-foundation.md`
- `docs/architecture/04-identity-authentication.md`
