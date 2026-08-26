# 06 — Health / readiness proof (TB-P06-T001)

| Endpoint | Purpose | Behavior |
|---|---|---|
| `/health/live` | Liveness | `{ "status": "ok" }` — process alive |
| `/health` | Legacy liveness | Same as `/health/live` (compatibility) |
| `/health/ready` | Readiness | Config + optional messaging checks; **no DB open** |
| `/ready` | Legacy readiness | Alias of `/health/ready` |

Readiness checks (`HostReadinessEvaluator`):
- edition configured (not Unset)
- PostgreSQL connection references present for edition/messaging refs
- authorization config complete (SpiceDb token when Mode=SpiceDb)
- MassTransit bus health when messaging enabled

503 response shape: `{ "status": "not-ready", "checks": { ... } }` — no connection strings.

Tenant resolution and auth middleware skip `/health*` and `/ready`.

Tests: `HostHealthEndpointTests`, extended `ErrorContractTests`.
