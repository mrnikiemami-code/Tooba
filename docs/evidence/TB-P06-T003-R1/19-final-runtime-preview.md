# 19 — Final runtime preview (TB-P06-T003-R1)

Recorded after validation, before commit/push.

| Runtime | Port | Status |
|---|---|---|
| PostgreSQL | 5432 | available (local dev) |
| Backend (Tooba.Host) | 5088 | running |
| Frontend (Next.js) | 3000 | running (3001 also bound; use 3000 for PDP) |

## Health

| Endpoint | Status | Notes |
|---|---|---|
| `GET http://127.0.0.1:5088/health/live` | 200 | liveness OK |
| `GET http://127.0.0.1:5088/health/ready` | 200 | `messaging-transport=postgresql-sql`, `messaging-schema=transport`, `messaging=Healthy` |

## User preview URLs

| Surface | URL | Status |
|---|---|---|
| Home | http://127.0.0.1:3000/ | 200 |
| PDP (demo seed) | http://127.0.0.1:3000/products/demo-mobile-1 | 200 |
| Backend health | http://127.0.0.1:5088/health/ready | 200 |

Backend + Frontend left running after task completion per protocol.
