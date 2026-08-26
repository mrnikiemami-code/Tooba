# 10 — Host client server-side (TB-P06-T007)

## host-client.ts

Path: `src/frontend/lib/server/session-cookies.ts` (cookies) + `src/frontend/lib/server/host-client.ts` (upstream)

| Function | Purpose |
|---|---|
| `hostBaseUrl()` | `TOOBA_HOST_ORIGIN` or `http://127.0.0.1:5088` |
| `readSessionId()` | Read `tooba_session` from Next.js cookie jar |
| `buildUpstreamAuthHeaders()` | Bearer or dev-actor headers |
| `forwardToHost(path, init)` | Authenticated fetch to Host |
| `readHostJson(path)` | Convenience JSON wrapper |

## session-cookies.ts

| Function | Purpose |
|---|---|
| `buildAuthCookies(sessionId, refreshToken)` | Set session, refresh, CSRF on login/refresh |
| `clearAuthCookieOptions(secure)` | Expire all auth cookies on logout/failed refresh |
| `sessionCookieOptions` / `refreshCookieOptions` | HttpOnly, SameSite=lax, secure in prod |

## Security boundary

Session and refresh values exist only in HttpOnly cookies and server-side fetch headers — never returned to client JSON after login.
