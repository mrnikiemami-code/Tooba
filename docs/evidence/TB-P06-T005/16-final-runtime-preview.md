# 16 — Final runtime preview (TB-P06-T005)

Recorded after validation; backend/frontend restarted post-build.

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

Dev default authorization mode remains `Disabled`; readiness passes with `authorization=disabled` in checks JSON.

## User preview URLs

| Surface | URL |
|---|---|
| Home | http://127.0.0.1:3001/ |
| PDP (demo seed) | http://127.0.0.1:3001/products/demo-mobile-1 |
| Backend health | http://127.0.0.1:5088/health/ready |

Production SpiceDB: set `Mode=SpiceDb`, TLS, token, and `ReadinessProbeEnabled=true` for live probe on readiness.
