# 01 — Runtime before background work (TB-P06-T004)

## Predecessor

| Field | Value |
|---|---|
| Task | TB-P06-T003-R1 (ACCEPTED) |
| Commit | `e1f0edf580f199d1fa227a136315e2aa167dc3b3` |
| Branch | `main` (HEAD == origin/main) |

## Services started

| Service | Port / URL | Status |
|---|---|---|
| PostgreSQL | local dev instance | running |
| Backend (`Tooba.Host`) | `http://localhost:5088` | running |
| Frontend (Next.js) | `http://localhost:3001` | running |

## Smoke checks (before TB-P06-T004 changes)

| Check | URL | Expected | Result |
|---|---|---|---|
| Liveness | `GET /health/live` | 200 `{ status: "ok" }` | 200 |
| Legacy health | `GET /health` | 200 | 200 |
| Readiness | `GET /health/ready` | 200 when edition + PostgreSQL refs configured | 200 |
| Storefront Home | `GET /` (frontend proxy) | 200 | 200 |

## Notes

- Background workers (`OutboxDispatcherHostedService`, `CartExpiryHostedService`) start with Host; no separate worker process.
- `/health/live` does not probe DB, outbox backlog, or worker last-run state.
- Messaging may be disabled in dev (`Tooba:Messaging:Enabled=false`); outbox dispatcher still polls when `Tooba:Outbox:Enabled=true`.
