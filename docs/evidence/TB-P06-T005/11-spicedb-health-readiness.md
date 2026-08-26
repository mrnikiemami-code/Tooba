# 11 — Authorization health policy (TB-P06-T005)

## Liveness — independent of SpiceDB

| Endpoint | Behavior |
|---|---|
| `GET /health/live` | Always `{ status: "ok" }` |
| `GET /health` | Legacy alias — same |

SpiceDB outage does **not** fail liveness.

## Readiness — SpiceDB when required

`HostReadinessEvaluator` (async via `HostHealthEndpoints`):

| Step | Check |
|---|---|
| 1 | Edition configured |
| 2 | PostgreSQL connection refs present |
| 3 | Authorization: endpoint/token when `Mode=SpiceDb` |
| 4 | SpiceDB `ReadSchema` probe when probe enabled |
| 5 | MassTransit bus health when messaging enabled |

## Response labels (no secrets)

| Label | Meaning |
|---|---|
| `authorization=disabled` | Mode Disabled |
| `authorization=inmemory` | Mode InMemory |
| `authorization=spicedb` | Mode SpiceDb + probe pass |
| `authorization=spicedb-unreachable` | Probe failed |

## Tenant bypass

`TenantResolutionMiddleware` skips `/health`, `/health/live`, `/health/ready`, `/ready`.

## Rationale

Transient SpiceDB blip removes instance from rotation (readiness 503) while process remains alive for diagnostics. Fail-closed on protected operations via `Unavailable` decision.

## Unchanged from T004

Readiness does not check outbox depth, cart expiry last run, or background worker registry.
