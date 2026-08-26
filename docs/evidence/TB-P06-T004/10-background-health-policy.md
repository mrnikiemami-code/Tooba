# 10 — Background health policy (TB-P06-T004)

## Liveness — independent of background job health

| Endpoint | Behavior |
|---|---|
| `GET /health/live` | Always `{ status: "ok" }` — no DB, bus, or worker checks |
| `GET /health` | Same (legacy alias) |

A failing outbox handler, cart expiry error loop, or transport consumer retry **does not** fail liveness.

## Readiness — critical dependencies only

`HostReadinessEvaluator` checks:

1. Edition configured (not `Unset`)
2. Required PostgreSQL connection references present (edition tenants + messaging ref if enabled)
3. Authorization config valid when SpiceDB mode
4. **Messaging only when enabled:** `IBusControl.CheckHealth()` must not be `Unhealthy`

Readiness **does not** check:

- Outbox queue depth or dispatcher last success
- Cart expiry last run
- BackgroundWorkerRegistry state
- TCP probe to tenant databases

## Messaging disabled (typical dev)

```json
"Tooba:Messaging:Enabled": false
```

Readiness returns `messaging=disabled`; bus health skipped. Outbox may still dispatch to `MessagingDisabledPublisher` (publish no-op / disabled seam per registration).

## Tenant resolution bypass

`TenantResolutionMiddleware` skips DB resolution for `/health`, `/health/live`, `/health/ready`, `/ready`.

## Rationale

Scheduled job transient failure must not take the serving process out of rotation; readiness fails only when the process cannot honor requests due to missing config or definitively unavailable messaging when messaging is required.
