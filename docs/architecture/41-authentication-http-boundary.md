# Tooba — Authentication HTTP Boundary Foundation

Status:

```text
COMPLETE — Architect accepted TB-P02-T005
```

Task:

```text
TB-P02-T005
```

```text
HTTP/API boundary != Domain/Application
Authentication != Authorization
Bearer session handle != JWT product
Refresh secret != access/session handle
Trusted TenantContext != client tenant claim
```

This document locks the P02 authentication HTTP boundary. Identity accounts remain `37-identity-authentication-foundation.md`. Sessions/credentials remain `40-session-token-credential-lifecycle.md`. SpiceDB remains `38-spicedb-authorization-foundation.md`. Party remains `39-party-organization-membership-foundation.md`.

## Routes and contracts

Versioned JSON under `/v1/auth`:

| Method | Path | Auth | Notes |
| --- | --- | --- | --- |
| POST | `/register` | public | Creates user. Duplicate normalized identifier → 409 `identity.identifier.conflict`. |
| POST | `/login` | public | Identifier + password. Success returns opaque `accessToken` (session id) and raw `refreshToken` once. Failures collapse to 401 `identity.authentication.failed`. |
| POST | `/refresh` | public | Session id + refresh secret. Rotates secret. Replay/revoked/expired → 401 `identity.session.invalid` without reuse details. |
| POST | `/logout` | bearer preferred | Revokes current session. Idempotent when the bearer still parses as a session id. |
| POST | `/logout-all` | bearer | Revokes all sessions for the authenticated user. |
| POST | `/password-reset/request` | public | Always `{ "accepted": true }`. Never returns `challengeId`. |
| POST | `/password-reset/complete` | public | Consumes durable challenge, applies password policy, revokes sessions. Invalid/used → 400 `identity.challenge.invalid`. |
| POST | `/identifier-verification/request` | bearer | Issues durable challenge. Issuing does not mark verified. |
| POST | `/identifier-verification/complete` | public | Consumes challenge. Invalid → 400 `identity.challenge.invalid`. |
| POST | `/password-change` | bearer | Requires current password. Unauthenticated callers cannot use this as reset. |
| GET | `/me` | bearer | Returns `userId`, `sessionId`, edition, tenant id. No hashes, stamps, or secrets. |

DTOs are Host-owned. EF entities are not serialized. No custom JWT is minted.

## Principal / session resolution

`SessionAuthenticationMiddleware` reads `Authorization: Bearer {guid}` after tenant resolution. The guid is the Identity session handle. `IIdentitySessionResolver` loads the session from the tenant database already selected by trusted commerce context.

`CurrentAuthenticatedSession` exposes `UserId`, `SessionId`, edition, and tenant id for this request only. It does not parse SpiceDB relations and does not call SpiceDB.

Disabled, locked, expired, revoked, or stamp-mismatched sessions resolve to unauthenticated (no principal).

## Tenant behavior

Single-Store authentication uses Host → existing `TenantResolutionMiddleware` / `CommerceContext`. Marketplace remains a distinct edition connection.

Client tenant authority is rejected (`identity.tenant.untrusted`) when presented as:

- `X-Tenant-Id` / `TenantId` headers
- `tenantId` query
- `tenantId` cookie
- JSON `tenantId` or extension-data aliases

Identity application code does not parse Host.

A session issued in Tenant A cannot be used as Tenant B: the bearer is resolved against Tenant B’s identity schema, where that session row does not exist.

## Login / refresh / logout

Login creates an Identity-owned session and returns the opaque access handle plus the raw refresh secret. Hashes stay in PostgreSQL.

Refresh validates the opaque secret, rotates it, and returns a new raw secret. The previous secret is unusable. Public errors do not distinguish reuse detection from other invalid-session cases.

Logout revokes one session. Logout-all revokes every session for the user (security stamp / lifecycle policy from T004 still applies on password change and disable).

## Reset / verification / password-change

Password-reset request is enumeration-safe: unknown and known identifiers share the public accepted payload. Complete is single-use and then follows T004 revocation policy.

Identifier verification requires a valid durable challenge. Requesting a code does not mark the identifier verified.

Password change requires an authenticated session and the current password.

## ProblemDetails

Mapped statuses:

- invalid credentials / disabled / locked → 401 `identity.authentication.failed`
- expired/revoked/unknown session or refresh → 401 `identity.session.invalid`
- invalid/expired/consumed challenge → 400 `identity.challenge.invalid`
- validation → 400 `identity.validation.failed`
- duplicate identifier → 409 `identity.identifier.conflict`
- untrusted tenant input → 400 `identity.tenant.untrusted`
- unexpected → existing Host exception handler 500 without stack traces

`traceId` is present. Account existence and secrets are not.

## Security logging

Host logs only safe technical events (`identity.login.succeeded`, `identity.login.failed`, …). Passwords, refresh secrets, OTP/reset secrets, Authorization headers, and cookies are not logged. Identity security events still use the T004 sink without secrets.

## Rate-limit seam

`IAuthenticationThrottleSeam` / `NoOpAuthenticationThrottleSeam` records operation names (`login`, `refresh`, reset, verification). It is not an anti-abuse product and is not IP-only identity.

## Future cookie / BFF / JWT / IdP

This boundary is bearer + opaque session handle. Browser cookies, CSRF/SameSite, BFF, product JWT, Keycloak/OIDC, and WebAuthn are deferred. Do not ship insecure default cookies on this seam.

## Deferred

Commercial login UI, OpenAPI-only Swagger stack, Keycloak, social login, WebAuthn UI, customer profile, Seller/Agency portals, Catalog, Shopeiva, Data Grid, Design System.
