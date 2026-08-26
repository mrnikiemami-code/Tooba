# 01 — Runtime start before gate (TB-P05-T026)

Predecessor verified: `6a41ebfeb430d0d3988480e271bdf47e6730a61a` on `main`.

Gate context: P05 Completion Gate / live sellability acceptance. Runtime started **before** gate work and kept available for Architect review.

## Backend (Host)

| Field | Value |
|---|---|
| Process | `Tooba.Host` PID **21760** |
| URL | `http://127.0.0.1:5088` |
| Health | `GET /health` → `{"status":"ok"}` (HTTP 200) |
| Storefront API smoke | `GET /v1/storefront/home` → HTTP 200 |

## Frontend (Next.js)

| Field | Value |
|---|---|
| Process | `node` / `next dev` PID **11612** |
| Command (supported) | `npm run dev -- --hostname 127.0.0.1 --port 3000` (cwd `src/frontend`) |
| URL | `http://127.0.0.1:3000` |
| Home smoke | `GET /` → HTTP 200 |

## Dependencies

| Dependency | Status |
|---|---|
| Docker `postgres-db` | Up (listening `127.0.0.1:5432`) |
| Docker `rabbitmq` | Up |
| Host origin / FE rewrite | Host `5088`; Next rewrites `/v1/*` → Host |
| Ports | No new ports invented |

## Gate note

Runtime was already healthy at gate open (same Host/FE PIDs as final T025 preview continuity). No redesign or architecture expansion at runtime start.

**Runtime-before gate: PASS**
