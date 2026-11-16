# TB-272-R1 (C) — the relational proof is rollback-only, and what it can and cannot show

Class: `src/backend/Host/Tooba.Host.Tests/ReturnsCreateIntegrity272RelationalTests.cs`
([Collection("PostgresSerial")], `SkippableFact` per fact).

## 1. What was wrong and what replaced it

The TB-272 class committed task-labelled cases on the canonical database and then ran `ExecuteDeleteAsync` cleanup,
silently returned when `TB272_CONNECTION` was absent, and enforced the "canonical target only" rule in a comment. That
is a task-boundary deviation, and the previous worker result is now historical evidence only — it is **not** repaired
retroactively and this class does not repeat it.

The repaired class:

- opens **one caller-owned transaction per fact** and rolls it back (`await caller.RollbackAsync()` in the body plus
  `await using` disposal);
- contains **no** `CommitAsync`, `ExecuteDeleteAsync`, `ExecuteUpdateAsync`, `TRUNCATE`, `DELETE FROM`, restore or
  reseed — asserted by inspecting the file before the run (`rg` over those tokens returns nothing);
- validates the target **before** opening a connection for writing:

| Check | Rule |
| --- | --- |
| `TB272_CONNECTION` present | absent ⇒ visible `SkippableFact` SKIP, never a pass |
| connection string parses | invalid ⇒ SKIP |
| host | `127.0.0.1` / `localhost` / `::1` only |
| port + database | exactly `5432` and `tooba_alpha` |
| server self-identity | `current_database()` = `tooba_alpha` and `inet_server_port()` = 5432, read-only |

Secrets are never printed; only the non-secret server identity is logged
(`TB272R1 target database=tooba_alpha serverPort=5432 serverAddress=172.18.0.2` — the container's own interface
address observed through the published loopback port).

- tags every fixture row with a per-run prefix (`tb272r1-rel-<guid>-`) and ends each fact by counting that prefix on a
  **separate** connection and asserting `0` committed rows:

```
TB272R1 rollback-only tag=tb272r1-rel-960b636ff8d845f1ab0502ea65ad7441- committedRows=0
TB272R1 rollback-only tag=tb272r1-rel-f9d5712d869348fb9cec35f22e88e2da- committedRows=0
TB272R1 rollback-only tag=tb272r1-rel-88be7d34b55e467396648ab235b08c37- committedRows=0
TB272R1 rollback-only tag=tb272r1-rel-24e05dd485a647b5bb2668f6a8b8db44- committedRows=0
TB272R1 rollback-only tag=tb272r1-rel-0e9600b2c957466b8ebe648a68812056- committedRows=0
TB272R1 rollback-only tag=tb272r1-rel-eb62c1861c94491f937cd634623f40ed- committedRows=0
```

Result: **6/6 passed** on the canonical `tooba_alpha`.

## 2. The five retained real-relational facts

| Fact | Real relational fact proven |
| --- | --- |
| `The_creation_decision_waits_for_the_seller_order_advisory_lock` | A foreign connection holds `pg_advisory_xact_lock(AdvisoryLockKey(order))`; the creation is still incomplete after 1.2 s (`Assert.False(pending.IsCompleted)`), and completes only after the holder's transaction is **rolled back**. The contender never commits. |
| `A_real_unique_violation_inside_a_caller_transaction_is_exactly_classified_and_savepoint_recoverable` | The same key inserted twice inside one caller transaction raises a real `PostgresException` with `SqlState=23505`, `ConstraintName=ix_return_requests_idempotency_key`, `TableName=return_requests`; `IsIdempotencyConflict` is true; restoring `tb272_creation_intent` makes the caller transaction usable again (a new insert succeeds) while the key count stays 1. |
| `A_key_collision_removes_only_this_attempt_and_keeps_the_caller_transaction_usable` | With a caller's own **dirty** (`Modified`) entity tracked, the recovery branch runs, restores the savepoint, detaches only this attempt's rows, the caller's entity keeps instance/state/values, the caller transaction still works (a subsequent creation succeeds and the pending change is saveable), and the honest answer inside an uncommitted transaction is `return.stale` — not a borrowed snapshot, not a silent success. |
| `Only_the_idempotency_index_is_classified_as_a_key_collision` | A real `pk_return_items` violation is surfaced with its own constraint name and **not** reclassified as the idempotency conflict. |
| `Persisted_quantity_precision_and_the_full_binding_hold_inside_one_transaction` | A stored six-decimal quantity (`0.000001`) replays as itself and equals both the fresh read and the tracked answer; `0.0000011` is refused with `return.quantity_precision` before comparison; wrong-checkout and wrong-seller rows refuse with `return.idempotency_conflict`; only the injected rows exist. |
| `A_refused_creation_leaves_no_row_keeps_the_caller_transaction_and_replays_exactly` | An over-remaining request is refused inside the critical section, the caller transaction stays usable, exactly one request exists, and the identical key replays the same case. |

No live case was created, approved, rejected, refunded or restocked. The Order/Fulfillment/Payment/Wallet seams are
controlled in-memory readers; no provider, money, stock or notification effect is reachable.

## 3. What this boundary deliberately cannot show

- **Committed winner-vs-loser two-writer race: NOT proven and NOT authorized.** A competing row is only visible to
  another connection after a commit, and committing task rows on the canonical database is forbidden. The previous
  272 race/cleanup observation stays historical only.
- The collision-recovery branch is therefore reached with a controlled `SaveChangesInterceptor`
  (`ExactConflictInjector`) that raises a `DbUpdateException` carrying a `PostgresException` with the exact
  `23505` + constraint + table + schema identity that the *same run* proves against real PostgreSQL. That is a
  controlled failure injection for an unreachable-under-rollback branch, not a fabricated business row.
- The **owned-transaction** creation path is not executed here (a successful owned creation commits by definition).
  Only the caller-owned branch is exercised end-to-end.
- No stress harness, no second database, no Docker, no ports 15432/15433, no detached baseline worktree re-run.

## 4. Data boundary actually observed

- One canonical `tooba_alpha` on `127.0.0.1:5432` (SingleStore / `store-alpha`).
- Rolled-back task writes only: the rows existed inside each caller transaction and were verified absent afterwards
  (`committedRows=0` per fact).
- No migration, DDL, seed, backfill, cleanup, delete, restore, reset or reseed. No user order/draft/history mutation.
- Startup transport effect on `tooba_messaging` (MassTransit schema ensure) is a pre-existing host behaviour and is
  distinct from "no committed business writes".
