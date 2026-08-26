# 08 — Cart expiry hardening (TB-P06-T004)

## Independent configuration

Section `Tooba:CartExpiry` (separate from `Tooba:Outbox`):

| Key | Default | Purpose |
|---|---|---|
| `Enabled` | `true` | Disable worker without affecting outbox |
| `PollIntervalSeconds` | `5` | Loop delay (minimum 5 enforced in code) |
| `BatchSize` | `20` | Max carts per SKIP LOCKED claim batch |

## Time source

- Worker passes `DateTimeOffset.UtcNow` to `ICartDirectory.ExpireDueCartsAsync`.
- Cart hold TTL and expiry timestamps set in UTC at mutation time.

## Batch processing + locking

1. `ExpireDueBatchAsync`: SQL `FOR UPDATE SKIP LOCKED` on due active carts.
2. Loop until batch smaller than limit (drain backlog per tenant per cycle).
3. `ReleaseExpiredHoldsAsync` on inventory for orphaned held reservations (same SKIP LOCKED pattern).

## Idempotency + inventory safety

- Expired carts excluded from subsequent claims (`status = Active` filter).
- `InventoryDirectory.ReleaseAsync`: no-op when reservation already `Released`.
- Cart expiry releases line holds before marking expired inside one transaction.

## Process restart

Hosted service re-enters poll loop; unexpired due carts picked up on next cycle. No in-memory state.

## Metrics

- `tooba.cart_expiry.expired` — count per successful cycle with expirations
- `tooba.cart_expiry.tenant_failures` — per-tenant or loop failures

## Tests

- `CartExpiryPostgresTests.Duplicate_expiry_trigger_is_idempotent`
- `CartExpiryPostgresTests.Concurrent_claim_processes_each_due_cart_once`
