# 12 — Multi-instance safety (TB-P06-T004)

## Assumption

Multiple `Tooba.Host` instances may run behind a load balancer sharing the same PostgreSQL databases and messaging DB.

## Singleton / exclusive work — DB-backed

| Work | Mechanism | Duplicate execution prevented? |
|---|---|---|
| Outbox row dispatch | `FOR UPDATE SKIP LOCKED` + `locked_until` | Yes — one claim per row per lease |
| Cart expiry batch | `FOR UPDATE SKIP LOCKED` on due carts | Yes — concurrent workers split batches |
| Inventory hold release | `FOR UPDATE SKIP LOCKED` on reservations | Yes |
| EF migrations (CLI) | PostgreSQL advisory lock per database | Yes |

## Parallel-safe horizontal scaling

- HTTP request handling — stateless with tenant-scoped DbContext per scope.
- MassTransit SQL Transport — competing consumers on `tooba-integration` endpoint (transport coordinates delivery).

## Not safe to duplicate without coordination

- `MigrationRunner apply` without advisory lock success (second instance waits or fails).
- Manual dead-letter replay without idempotent handlers.

## Local timer only — avoided

Cart expiry and outbox **do not** use in-process-only leader election. All instances poll; PostgreSQL locking deduplicates claims.

## Proof tests

- `OutboxPostgresTests.Concurrent_claim_does_not_deliver_the_same_row_twice`
- `CartExpiryPostgresTests.Concurrent_claim_processes_each_due_cart_once`

## Per-tenant isolation on failure

Outbox dispatcher catches per-target exceptions; one tenant/module failure does not abort other targets in the same poll cycle.
