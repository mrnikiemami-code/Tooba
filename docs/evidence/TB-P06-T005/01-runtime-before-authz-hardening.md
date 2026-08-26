# 01 — Runtime before authorization hardening (TB-P06-T005)

## Predecessor

| Field | Value |
|---|---|
| Task | TB-P06-T004 (ACCEPTED) |
| Commit | `3c80f142c308fe3b8b71f961fee142bc7581c683` |
| Branch | `main` |

## Services started

| Service | Port / URL | Status |
|---|---|---|
| PostgreSQL | local dev instance | running |
| Backend (`Tooba.Host`) | `http://localhost:5088` | running |
| Frontend (Next.js) | `http://localhost:3001` | running |

## Smoke checks (before TB-P06-T005 changes)

| Check | URL | Expected | Result |
|---|---|---|---|
| Liveness | `GET /health/live` | 200 `{ status: "ok" }` | 200 |
| Legacy health | `GET /health` | 200 | 200 |
| Readiness | `GET /health/ready` | 200 when edition + PostgreSQL refs configured | 200 |
| Storefront Home | `GET /` (frontend proxy) | 200 | 200 |

## Notes

- Dev default: `Tooba:Authorization:Mode=Disabled` (fail-closed adapter).
- SpiceDB readiness probe not yet wired into `/health/ready` at task start.
- Background workers (outbox-dispatcher, cart-expiry) unaffected by authorization mode.
