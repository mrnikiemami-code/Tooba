# 01 — Runtime before hardening (TB-P06-T001)

Recorded before tracked code changes.

| Runtime | Command | PID (sample) | Port | URL | Health |
|---|---|---|---|---|---|
| Tooba Host | `dotnet run --project src/backend/Host/Tooba.Host` | 8044 | 5088 | http://127.0.0.1:5088 | 200 `/health` |
| Tooba Frontend | `npm run dev` (src/frontend) | node | 3000 | http://127.0.0.1:3000/ | 200 |
| PostgreSQL | local/manual (dev) | — | 5432 | — | assumed for Development appsettings |
| SpiceDB | not required (Authorization Mode=InMemory in Development) | — | — | — | N/A |

Predecessor commit: `1d59ae57aa346398c2eb0b1322943164bdd36210`

Shopeiva reference runtime: **not required** (backend/ops task; no UI edits).
