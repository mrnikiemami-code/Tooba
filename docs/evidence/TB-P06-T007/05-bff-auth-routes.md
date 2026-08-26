# 05 — BFF auth routes (TB-P06-T007)

## Routes

| BFF route | Method | Host upstream | Notes |
|---|---|---|---|
| `/api/auth/csrf` | GET | — | Issues readable `tooba_csrf` cookie |
| `/api/auth/login` | POST | `POST /v1/auth/login` | Sets HttpOnly session + refresh + CSRF cookies |
| `/api/auth/logout` | POST | `POST /v1/auth/logout` | Clears cookies; CSRF required |
| `/api/auth/refresh` | POST | `POST /v1/auth/refresh` | Reads cookies; rotates session |
| `/api/auth/me` | GET | `GET /v1/auth/me` | Bearer from `tooba_session` via host-client |

## Implementation paths

- `src/frontend/app/api/auth/csrf/route.ts`
- `src/frontend/app/api/auth/login/route.ts`
- `src/frontend/app/api/auth/logout/route.ts`
- `src/frontend/app/api/auth/refresh/route.ts`
- `src/frontend/app/api/auth/me/route.ts`

## Login response shape (browser)

Returns `{ userId, authenticated: true }` — no access/refresh tokens in JSON body.

## CSRF on mutating routes

`login`, `logout`, `refresh` validate `X-Tooba-Csrf` against `tooba_csrf` cookie before proceeding.
