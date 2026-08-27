# 01 — Runtime before visual proof (TB-P06-T011-R2)

Task: `TB-P06-T011-R2`
Date: 2026-08-27
Predecessor: `92fa21e6068c89a889f1115b9859ffc65fb09eed`

## Bridge

| Check | Value |
| --- | --- |
| Health | `http://127.0.0.1:17321/health` → ok |
| Claimed task | `TB-P06-T011-R2` / UUID `666135e3-4137-4806-8191-3261ae7cf472` |
| Worker | `tooba-worker-01` / Working |

## Three runtimes (before tracked changes)

| Runtime | Command | PID | Port | URL | HTTP |
| --- | --- | --- | --- | --- | --- |
| Tooba Backend | `dotnet run --no-build` (Tooba.Host) | 35300 → restarted after migrations | 5088 | http://127.0.0.1:5088 | `/health` 200, `/health/live` 200, `/health/ready` 200 |
| Tooba Frontend | `set PORT=3000&& npm run dev` | 28928 | 3000 | http://127.0.0.1:3000 | `/` 200 |
| Original Shopeiva | `set PORT=3001&& npm run dev` | 8420 | 3001 | http://127.0.0.1:3001 | `/user-panel/orders` 200 |

Shopeiva root: `../SarvNewVerRequirment/reference/shopeiva`

## DB note

Dev DB required `Tooba.MigrationRunner apply --all-tenants` before Returns/Fulfillment APIs responded (schemas `returns`, `fulfillment` were missing). Operational step only — no product code change.
