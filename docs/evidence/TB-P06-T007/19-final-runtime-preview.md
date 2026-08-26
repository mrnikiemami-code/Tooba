# 19 — Final runtime preview (TB-P06-T007)

Recorded after validation; backend/frontend running post-build.

| Runtime | Port | Status |
|---|---|---|
| PostgreSQL | 5432 | available (local dev) |
| Backend (Tooba.Host) | 5088 | running |
| Frontend (Next.js) | 3001 | running |

## Health

| Endpoint | Status |
|---|---|
| `GET http://127.0.0.1:5088/health/live` | 200 |
| `GET http://127.0.0.1:5088/health/ready` | 200 |

## BFF auth smoke

| Check | Expected |
|---|---|
| `GET /api/auth/csrf` | 200; sets `tooba_csrf` |
| `POST /api/auth/login` (valid creds + CSRF) | 200; sets `tooba_session`, `tooba_refresh` |
| `GET /api/auth/me` (with session cookie) | 200 user envelope |
| `POST /api/auth/login` (missing CSRF) | 403 `auth.csrf.invalid` |
| `GET /api/customer/dashboard` (with session) | Proxied 200/401 from Host |

## User preview URLs

| Surface | URL |
|---|---|
| Home | http://127.0.0.1:3001/ |
| BFF CSRF | http://127.0.0.1:3001/api/auth/csrf |
| Backend health | http://127.0.0.1:5088/health/ready |

Production checklist: set `Identity:OtpDelivery:Mode=Webhook` + webhook URL/key; ensure `TOOBA_HOST_ORIGIN` points to Host; cookies require HTTPS (`secure` in production).
