# Tooba Background Processing — Operations Guide

Operational reference for Host background workers, recovery, and multi-instance deployment. Complements [messaging.md](./messaging.md) (MassTransit SQL Transport layer).

## Worker inventory

| Worker | Config section | Default | Disable |
|---|---|---|---|
| Outbox dispatcher | `Tooba:Outbox` | Enabled, poll 2s | `Enabled: false` |
| Cart expiry | `Tooba:CartExpiry` | Enabled, poll 5s | `Enabled: false` |
| MassTransit consumer | `Tooba:Messaging` | Disabled in template | `Enabled: false` |
| Auth schema bootstrap | `Tooba:Authorization:ApplySchemaOnStartup` | `false` | leave false in prod |
| MigrationRunner | CLI | manual | N/A |

Poll targets: marketplace = one DB; single-store = active tenants from control plane only.

## Schedules

No Quartz. Workers use `BackgroundService` loops:

- Outbox: `PollIntervalSeconds` (min 1s enforced)
- Cart expiry: `PollIntervalSeconds` (min 5s enforced)

MassTransit consumes continuously from SQL transport when enabled.

## Configuration reference

### Outbox (`Tooba:Outbox`)

| Key | Default | Notes |
|---|---|---|
| `Enabled` | `true` | |
| `PollIntervalSeconds` | `2` | |
| `BatchSize` | `20` | Per module per tenant per poll |
| `LockSeconds` | `30` | Soft claim lease |
| `RetryBaseDelaySeconds` | `2` | Exponential base |
| `MaxAttempts` | `5` | Then dead-letter |

### Cart expiry (`Tooba:CartExpiry`)

| Key | Default | Notes |
|---|---|---|
| `Enabled` | `true` | Independent from outbox |
| `PollIntervalSeconds` | `5` | |
| `BatchSize` | `20` | SKIP LOCKED batch |

## Retry and backoff

| Layer | Policy |
|---|---|
| Outbox dispatcher | Exponential: `base * 2^(attempt-1)`, max 5 attempts → `dead_lettered_at` |
| MassTransit consumer | 2 immediate + 5s / 15s / 30s |
| Cart expiry | Next poll; idempotent state transitions |

## Stuck work recovery

### Outbox

1. Find rows: `processed_at IS NULL`, `dead_lettered_at IS NULL`.
2. If `locked_until > now()` — wait for lease expiry or investigate stuck worker.
3. If `next_attempt_at > now()` — waiting for backoff.
4. After fix, dispatcher auto-resumes; no manual unlock usually needed unless debugging (clear `locked_until`).

### Cart / inventory holds

Due carts re-selected when `status = Active` and `expires_at <= now()`. Expired carts skipped. Orphaned held reservations cleaned by `ReleaseExpiredHoldsAsync`.

### Migrations

Re-run `Tooba.MigrationRunner apply` after advisory lock holder exits.

## Lease model

- **Outbox:** `locked_until` set on claim; cleared on success/retry/dead-letter; passive recovery when expired.
- **Cart/inventory:** transaction-scoped `FOR UPDATE SKIP LOCKED`; no long-lived lease column.

## Health and metrics

- **Liveness** (`/health/live`): always OK — background failures do not fail pod.
- **Readiness** (`/health/ready`): edition + PostgreSQL refs + messaging bus health **only when messaging enabled**.
- **Metrics:** `tooba.outbox.*`, `tooba.cart_expiry.*`, `tooba.messaging.*` via OTLP.
- **Registry:** in-process last success/failure per worker (`outbox-dispatcher`, `cart-expiry`) — logs/metrics only, not HTTP.

## Graceful shutdown

- Host passes cancellation to worker loops; no new polls after stop signal.
- MassTransit `StopTimeout` = 30s for in-flight consumers.
- Plan rolling deploys so at least one instance polls during drain.

## Multi-instance operation

- Safe to run N Host instances: PostgreSQL `SKIP LOCKED` deduplicates outbox and cart claims.
- Do not run concurrent `MigrationRunner apply` on same DB without advisory lock success.
- All instances share messaging DB; transport coordinates consumer competition.

## Troubleshooting

| Symptom | Likely cause | Action |
|---|---|---|
| Outbox backlog growing | Downstream publish/handler failures | Check `last_error`, metrics `.retries`, fix handler; watch `.dead_letters` |
| Rows stuck in-flight | Crashed worker mid-claim | Wait `LockSeconds` or inspect `locked_until` |
| Dead letters accumulating | Poison messages / permanent errors | Fix payload/handler; manual replay policy TBD |
| Cart holds not releasing | Cart expiry disabled or worker errors | Verify `Tooba:CartExpiry:Enabled`, logs, `tenant_failures` metric |
| Readiness 503 messaging | Bus unhealthy when messaging required | Check messaging DB connectivity, `IBusControl` health |
| Tenant A errors, B fine | Expected isolation | Fix tenant config/DB; other tenants unaffected |

## Safe replay / retry

- **Outbox pending:** fix root cause; automatic resume.
- **Outbox dead-letter:** operator must decide — no built-in replay API; re-insert only with idempotency review.
- **MassTransit redelivery:** at-least-once; handlers must use inbox/state checks (e.g. `order.payment_inbox`).
- **Cart expiry:** safe to re-run; idempotent by design.

## Retention

No automated outbox purge in current release. Plan operational cleanup:

- Archive/delete processed outbox rows by age.
- Review dead-letter table periodically.
- SQL transport tables: follow MassTransit operational guidance separately from module outbox.

## Related docs

- [messaging.md](./messaging.md) — transport topology, consumer retry
- [database-migrations.md](./database-migrations.md) — MigrationRunner usage
- Evidence: `docs/evidence/TB-P06-T004/`
