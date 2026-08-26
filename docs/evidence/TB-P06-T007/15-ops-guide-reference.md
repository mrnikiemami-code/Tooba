# 15 — Ops guide reference (TB-P06-T007)

## Updated guide

`docs/operations/authentication-security.md`

## Additions

| Section | Content |
|---|---|
| Canonical decision | `AUTH_SESSION_STORAGE = HTTPONLY_COOKIE_SERVER_SIDE_PROPAGATION`; `PRODUCTION_OTP = PROVIDER_BACKED_FAIL_CLOSED` |
| BFF cookie model | `tooba_session`, `tooba_refresh`, `tooba_csrf` |
| CSRF | Double-submit on mutating BFF routes |
| Customer proxy | `/api/customer/[...path]` Bearer injection |
| OTP delivery | `Identity:OtpDelivery` modes: Capturing / Disabled / Webhook |
| Host vs BFF | Host remains Bearer-only; browser uses BFF |

## Evidence cross-reference

Prior auth hardening (rate limits, CORS, security headers): `docs/evidence/TB-P06-T006/`

Task evidence: `docs/evidence/TB-P06-T007/`
