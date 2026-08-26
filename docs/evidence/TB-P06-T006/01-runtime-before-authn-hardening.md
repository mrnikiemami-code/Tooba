# 01 — Runtime before authentication hardening (TB-P06-T006)

## Predecessor

| Field | Value |
|---|---|
| Task | TB-P06-T005 (ACCEPTED) |
| Commit | `cb5b81e07d78e6a99e6ef0153f24ba48066c63bd` |
| Branch | `main` |
| Bridge UUID | `2299809c-fe31-4c0a-9cfc-7d4432997434` |

## Services started

| Service | Port / URL | Status |
|---|---|---|
| PostgreSQL | local dev instance | running |
| Backend (`Tooba.Host`) | `http://localhost:5088` | running |
| Frontend (Next.js) | `http://localhost:3001` | running |

## Smoke checks (before TB-P06-T006 changes)

| Check | URL | Expected | Result |
|---|---|---|---|
| Liveness | `GET /health/live` | 200 `{ status: "ok" }` | 200 |
| Legacy health | `GET /health` | 200 | 200 |
| Readiness | `GET /health/ready` | 200 when edition + PostgreSQL refs configured | 200 |
| Storefront Home | `GET /` (frontend) | 200 | 200 |

## Notes

- Bearer SessionId auth already live via `SessionAuthenticationMiddleware`; no auth cookies.
- No `Tooba:AuthSecurity` section or dedicated rate-limit/security-header middleware at task start.
- CORS not applied to `/v1/auth` or health endpoints before this task.
