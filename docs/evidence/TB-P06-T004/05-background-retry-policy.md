# 05 — Background retry policy (TB-P06-T004)

## Outbox dispatcher

| Setting | Default | Behavior |
|---|---|---|
| `Tooba:Outbox:MaxAttempts` | 5 | After `attempt_count >= MaxAttempts`, row → dead-letter |
| `Tooba:Outbox:RetryBaseDelaySeconds` | 2 | Delay = `base * 2^(attempt-1)` capped at shift 8 |
| `Tooba:Outbox:LockSeconds` | 30 | Soft claim lease per attempt |

**Failure classes:**

- **Transient** (handler/publish throws): schedule `next_attempt_at`, clear lock.
- **Permanent / poison** (exhausted attempts): `dead_lettered_at` + sanitized `last_error`.
- **Business rejection**: same as transient until max attempts, then dead-letter.

No infinite retry loop; poll interval adds natural spacing between cycles.

## MassTransit consumer (SQL Transport)

`MessagingRetryConfigurator.ApplyConsumerRetry`:

- **2** immediate retries
- **3** delayed intervals: **5s**, **15s**, **30s**

Configured on the Postgres transport endpoint via `cfg.UseMessageRetry(...)`.

## Cart expiry

No per-cart retry table. Failures log + increment `tooba.cart_expiry.tenant_failures`; next poll cycle retries eligible rows. Idempotent expiry prevents duplicate side effects on re-run.

## MigrationRunner

Operator-driven: CLI exits non-zero; advisory lock timeout → retry manually. No automatic exponential backoff in-process.
