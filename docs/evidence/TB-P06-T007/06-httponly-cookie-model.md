# 06 — HttpOnly cookie model (TB-P06-T007)

## Cookies (`src/frontend/lib/auth/constants.ts`)

| Cookie | Name | HttpOnly | Path | Purpose |
|---|---|---|---|---|
| Session | `tooba_session` | yes | `/` | Opaque SessionId (Host Bearer value) |
| Refresh | `tooba_refresh` | yes | `/api/auth` | Opaque refresh secret |
| CSRF | `tooba_csrf` | no | `/` | Double-submit token (readable by JS) |

## Options (`src/frontend/lib/server/session-cookies.ts`)

- `sameSite: lax`
- `secure: true` in Production
- `maxAge`: session/refresh 14 days; CSRF 24 hours

## Host boundary unchanged

Host still accepts `Authorization: Bearer {SessionId}` only. BFF reads HttpOnly cookies and injects Bearer server-side — tokens never exposed to browser JavaScript.

## Dev actor fallback

Non-production BFF may forward `X-Tooba-Dev-Actor-User-Id` when no session cookie (`lib/server/host-client.ts`).
