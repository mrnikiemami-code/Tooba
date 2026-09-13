# TB-P10-T004-R21-R1 — Fault Injection

`ICheckoutCommitBarrier` points: after reserve, after order write, after cart converted write, before `Complete()`.

Convert-fail (`FailOnceCartDirectory`) rolls back the ambient `TransactionScope`: Cart stays Active, **zero** checkout rows; retry then commits.

Inventory unavailable throws `inventory.supply.unavailable` before Complete; no Order/cycle/payment.

Concurrent reserve unique (`ix_reservations_idempotency_key`) maps to `inventory.reservation.conflict` without querying an aborted PostgreSQL transaction; loser returns the winner checkout after scope rollback.

No test cleanup masking; assertions run on the failed real `SubmitAsync`.
