# 06 — CSRF and session model (TB-P06-T006)

## Locked architecture: Bearer-only, no auth cookies

| Aspect | Decision |
|---|---|
| Access credential | `Authorization: Bearer {SessionId}` — opaque Guid |
| Refresh credential | JSON body `{ sessionId, refreshToken }` |
| Cookies | Host does **not** set secure auth cookies |
| JWT | Not used; SessionId is not a signed JWT |

Source: `SessionAuthenticationMiddleware`, `AuthenticationEndpointMapper` comment: *"کوکی امن پیش‌فرض ساخته نمی‌شود"*.

## CSRF: N/A

Cross-Site Request Forgery protections (antiforgery tokens, SameSite cookies) apply when browsers automatically attach session cookies.

Tooba API clients send the Bearer header explicitly. **No CSRF middleware or tokens added** — consistent with Bearer-only architecture.

Document only; do not add cookies to "enable CSRF."

## Tenant spoof rejection

`RejectUntrustedTenant` in `AuthenticationHttpBoundary.cs` rejects:

- `TenantId` in JSON body
- Forbidden extension keys (`X-Tenant-Id`, `tenantId`, etc.)
- Matching request headers
- Query params `tenantId`, `tenant_id`, `TenantId`
- Cookie keys `tenantId`, `TenantId`, `tenant_id`

Returns 400 + `identity.tenant.untrusted`.

Edition and TenantId on authenticated requests come from **resolved session**, not client input.

## Session response shape

`SessionResponse`: `{ userId, sessionId, accessToken, refreshToken }` where `accessToken` equals SessionId string (`ToString("D")`).
