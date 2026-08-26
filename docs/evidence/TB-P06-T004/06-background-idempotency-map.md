# 06 — Background idempotency map (TB-P06-T004)

| Job / path | Side effect | Idempotency mechanism | Duplicate trigger safe? |
|---|---|---|---|
| **Outbox dispatch** | Publish integration event | `processed_at` set only after successful publish | Yes — reclaim after lease still checks processed/dead state |
| **Outbox dead-letter** | Terminal state | `dead_lettered_at` exclusive | Yes — excluded from claim query |
| **Cart expiry** | Mark cart `Expired`; release line holds | Only `Active` + due carts selected; `cart.Expire()` state transition | Yes — second run returns 0 (`CartExpiryPostgresTests.Duplicate_expiry_trigger_is_idempotent`) |
| **Inventory release (expiry path)** | Decrement reserved; reservation → `Released` | `ReleaseAsync` returns early if already `Released` | Yes — no double decrement |
| **Inventory release (cart path)** | Same | Called from cart expiry batch per line hold | Yes |
| **Order payment consumer** | Apply paid projection | `order.payment_inbox` keyed by integration `EventId` | Yes — duplicate delivery skipped |
| **MassTransit publish** | Enqueue transport message | Outbox row remains unprocessed until publish succeeds | At-least-once to transport; handlers must dedupe |

## Restart / multi-instance

- Process restart: pending outbox rows (`processed_at IS NULL`) resume on next poll.
- Duplicate claim: `SKIP LOCKED` ensures one worker owns a row per claim window.
- Duplicate cart expiry: status filter prevents re-processing expired carts.

## Gaps (documented, not blockers)

- Party projection handler relies on SpiceDB tuple semantics; no durable inbox in current handler set.
- Processed outbox rows: **no automated retention/purge** in this task (see 07).
