# 07 — CSRF double-submit (TB-P06-T007)

## Mechanism

1. Browser calls `GET /api/auth/csrf` → sets `tooba_csrf` (non-HttpOnly).
2. Mutating requests include header `X-Tooba-Csrf` matching cookie value.
3. BFF validates via `validateCsrf()` in `src/frontend/lib/auth/csrf.ts`.

## Protected routes

| Route | Methods |
|---|---|
| `/api/auth/login` | POST |
| `/api/auth/logout` | POST |
| `/api/auth/refresh` | POST |
| `/api/customer/[...path]` | POST, PUT, PATCH, DELETE |

## Failure response

HTTP **403** + `errorCode: auth.csrf.invalid`

## Client helper

`ensureCsrfCookie()` in `src/frontend/lib/auth/browser-session.ts` fetches CSRF cookie before first mutating call.

## Origin check (customer proxy)

Mutating `/api/customer/*` also rejects cross-origin requests where `Origin` host does not match request `Host`.
