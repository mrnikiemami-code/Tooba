# 04 — Stuck work recovery (TB-P06-T004)

## Outbox dispatcher — passive lease expiry

Claim SQL (`NpgsqlOutboxDispatcherStore.ClaimAsync`):

- Selects rows where `processed_at IS NULL`, `dead_lettered_at IS NULL`, `next_attempt_at <= now()`, and `(locked_until IS NULL OR locked_until <= now())`.
- Claims with `FOR UPDATE SKIP LOCKED`, sets `locked_until = now() + lock_seconds`, increments `attempt_count`.

**Recovery:** If a worker crashes mid-dispatch, the row remains claimed until `locked_until` elapses. No active heartbeat is required; the next poll (any instance) re-claims the row when the lease expires.

On success: `processed_at` set, `locked_until` cleared.  
On retry: `next_attempt_at` set, `locked_until` cleared.  
On dead-letter: `dead_lettered_at` set, `locked_until` cleared.

**Proof:** `OutboxPostgresTests.Expired_lock_is_reclaimed_after_lease_elapses`.

## Cart expiry — SKIP LOCKED batch claims

`CartDirectory.ExpireDueBatchAsync`:

- Selects active carts with `expires_at <= utcNow`, `ORDER BY expires_at LIMIT batch`, `FOR UPDATE SKIP LOCKED`.
- Expires and releases holds inside a DB transaction.

`InventoryDirectory.ReleaseExpiredBatchAsync` uses the same pattern for held reservations past `expires_at`.

**Recovery:** Uncommitted or crashed mid-transaction rolls back the claim. Completed rows change status (`Expired` / `Released`) and are not re-selected.

## MigrationRunner — advisory lock

`PostgresMigrationAdvisoryLock` prevents concurrent `apply` on the same database. Lock released on dispose; operator may retry after timeout.

## Not used for durable work

- In-memory locks across requests or instances.
- Host-header–scoped claim keys.
