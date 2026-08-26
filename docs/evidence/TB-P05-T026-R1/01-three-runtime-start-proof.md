# 01 — Three runtime start proof (TB-P05-T026-R1)

**Mandatory:** all three runtimes started **before** any tracked file change.

| Runtime | Command | CWD | PID | Port | URL | Health | Started |
|---|---|---|---:|---:|---|---|---|
| Tooba Backend (Host) | `dotnet run --project src/backend/Host/Tooba.Host/Tooba.Host.csproj --no-build --urls http://127.0.0.1:5088` | repo root | 21852 → restarted 30104 | 5088 | http://127.0.0.1:5088 | `GET /health` → `{"status":"ok"}` | before gate work |
| Tooba Frontend (Next dev) | `npm run dev -- --hostname 127.0.0.1 --port 3000` | `src/frontend` | 29596 | 3000 | http://127.0.0.1:3000 | `GET /` → HTTP 200 | before gate work |
| Original Shopeiva (purchased) | `npm run dev -- --hostname 127.0.0.1 --port 3017` | `SarvNewVerRequirment/reference/shopeiva` | 2560 | 3017 | http://127.0.0.1:3017 | `GET /` → HTTP 200 | before gate work |

## Infrastructure

| Dependency | Status |
|---|---|
| Docker `postgres-db` | Up (continuity from prior gate) |
| Docker `rabbitmq` | Up (continuity) |

## Simultaneous reachability (initial)

All three HTTP 200 at task open: Tooba Home, Tooba health, Shopeiva Home, Shopeiva PDP.

**Three-runtime-start: PASS**
