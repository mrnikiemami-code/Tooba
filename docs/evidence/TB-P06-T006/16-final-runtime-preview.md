# 16 — Final runtime preview (TB-P06-T006)

Recorded after validation; backend/frontend running post-build.

| Runtime | Port | Status |
|---|---|---|
| PostgreSQL | 5432 | available (local dev) |
| Backend (Tooba.Host) | 5088 | running |
| Frontend (Next.js) | 3001 | running |

## Health

| Endpoint | Status | Notes |
|---|---|---|
| `GET http://127.0.0.1:5088/health/live` | 200 | Security headers present |
| `GET http://127.0.0.1:5088/health/ready` | 200 | Readiness checks pass |

## Auth security smoke

| Check | Expected |
|---|---|
| `POST /v1/auth/login` (bad creds) | 401 `identity.authentication.failed` |
| Repeated login beyond limit | 429 `identity.rate_limited` |
| CORS from dev origin (`localhost:3001`) | Allowed when in Development config |
| Bearer `/v1/auth/me` without token | 401 `identity.session.invalid` |

## User preview URLs

| Surface | URL |
|---|---|
| Home | http://127.0.0.1:3001/ |
| PDP (demo seed) | http://127.0.0.1:3001/products/demo-mobile-1 |
| Backend health | http://127.0.0.1:5088/health/ready |

Production checklist: set `CorsAllowedOrigins`, wire external OTP provider, configure `TrustedProxies` behind load balancer.
