# TB-272 — creation-integrity proof (A–D)

Task: `TB-RETURNS-CREATE-OWNERSHIP-EXACT-REPLAY-AND-QUANTITY-INTEGRITY-272`
Base: `9d1a64e49102dd4e4429caad987beb7219f070ae` (expected == actual == `origin/main` at claim)
Code commit: `4d0ba4e169bff5c529ff387509c4fa920a31930f`

Every claim below points at the source that owns it and at the check that exercised it. Nothing here is an Architect
acceptance, a user acceptance, or a financial/provider claim.

## A — authorized, exact replay

**Ordering (the defect).** The previous `CreateInternalAsync` (`ReturnDirectory.cs`, removed by this commit) answered
`_db.ReturnRequests.SingleOrDefaultAsync(x => x.IdempotencyKey == …)` **before** loading the order, before the
ownership check and without comparing what the stored row was created for. Any key therefore returned whatever case
held it.

**New ordering** — `src/backend/Modules/Returns/Tooba.Returns.Infrastructure/Directories/ReturnDirectory.Creation.cs`:

| Step | Line | What it establishes |
| --- | --- | --- |
| mutation gate | 39 | `_guard.EnsureCanMutateAsync` runs first (unchanged authority) |
| key presence | 41–44 | empty key is a typed refusal, never a silent replacement |
| order binding | 48–49 | the submitted seller order must exist (`return.missing`) |
| customer ownership | 51–54 | `requireOwner` path: `PlacedByUserId != ActorUserId` ⇒ `return.not_owner` — **before** any replay |
| duplicate shape | 57–60 | see B |
| intent capture | 63 | one normalized intent (below) |
| critical section | 66–79 | one transaction + advisory lock (see D) |
| stored replay | 81–89 | only now is the stored row read |
| replay decision | 157–168 | `AnswerStoredCreationAsync`: snapshot mapped, then compared; mismatch ⇒ `return.idempotency_conflict` |

The `requireOwner:false` (admin) entry point is **not** a weaker authorization: it is the same method with the customer
ownership predicate skipped, and it still requires the order binding, the actor's mutation gate (route authority) and the
exact-intent comparison. It cannot answer another order's or another customer's case, because the compared intent
contains `SellerOrderId` and `RequestedByUserId` (`OrdinaryCreationIntent`, lines 183–228) — proven by
`Admin_and_customer_creation_share_the_same_exact_intent_rule` and
`A_non_owner_is_refused_before_any_stored_snapshot_is_answered` (the admin replay of the owning customer's intent
returns the owning customer's case, and a foreign order is a conflict).

**Compared intent fields and normalization** (`OrdinaryCreationIntent.Capture` / `Matches`, lines 183–228):

| Field | Normalization |
| --- | --- |
| `SellerOrderId` | exact |
| `RequestedByUserId` | exact (the owning customer of the order, never the actor) |
| `Origin` | must be `CustomerDelivered` — a courier-origin case can never answer an ordinary request |
| `RefundDestination` | exact enum |
| `Reason` | `Trim()` on both sides, ordinal compare (`Whitespace_only_reason_difference_is_the_same_intent`) |
| lines | `(OrderLineId, Quantity)` set, ordered by `OrderLineId`, quantity rounded to the persisted `numeric(18,6)` scale (`MidpointRounding.AwayFromZero`) — a different item ORDER is the same intent (`Reordered_lines_are_the_same_intent_and_replay_the_same_case`) |

**Exact replay survives elapsed eligibility** because the replay branch is entered before the eligibility read: the test
`Exact_replay_after_the_window_elapsed_answers_the_same_case_once` replays at `DeliveredAt + 90d` against a 7-day
window with the whole quantity already occupied by that very case and asserts the same `ReturnRequestId`, the same
`RefundAmount`, one item and one row.

**Unique-index arbitration** — `ReturnCreationSerialization.IsIdempotencyConflict` (lines 78–86) matches only
`SQLSTATE 23505` **and** `ConstraintName == ix_return_requests_idempotency_key` **and**
`TableName == return_requests` **and** `SchemaName ∈ {returns, null}`. The live index was verified read-only:

```
CREATE UNIQUE INDEX ix_return_requests_idempotency_key
  ON returns.return_requests USING btree (idempotency_key)
```

A genuine concurrent same-key collision re-reads the winner and runs the SAME authorization + exact-intent comparison
(lines 134–148). Any other provider failure is rethrown untouched — proven on the real database by
`Only_the_idempotency_index_is_classified_as_a_key_collision` (a real `pk_return_items` violation is **not**
classified) and by the committed two-writer case in `The_same_key_on_two_orders_collides_on_the_real_unique_index_and_conflicts`
(the loser is refused with `return.idempotency_conflict`, not a raw provider exception).

## B — one ordinary request line per order line

Two layers, one rule, both ordinary paths:

1. **Endpoint-reachable validation** — `CreateReturnCommandValidator`
   (`Tooba.Returns.Application/Validation/ReturnsRequestValidators.cs` line 38–42) refuses a repeated `OrderLineId`
   with `returns.validation.duplicate_order_line`; it fires for customer and admin because both dispatch the same
   `CreateReturnCommand`.
2. **Owning invariant** — `ReturnRequest.FindRepeatedOrderLine` (lines 111–122) and the guard inside
   `ReturnRequest.Create` (lines 156–159) throw `returns.duplicate_line`, so no future caller can bypass the Directory.
   The Directory itself throws the typed `return.duplicate_line` fault first (lines 57–60).

`ReturnRequest.CreateCourierOrigin` deliberately passes `enforceOneLinePerOrderLine: false` (line 269): its durable
claim identity is `(order line, condition)`, so the same line legitimately appears once per received condition. The
test proves both the refusal (same and differing quantities) and the courier exemption, plus that quantity `<= 0` and
`> remaining` are still refused (`A_partial_return_preserves_the_remaining_quantity_and_over_remaining_is_refused`).

## C — refund failure keeps return capacity

`ReturnEligibilityEvaluator.GetAlreadyReturnedQuantitiesAsync`
(`Tooba.Returns.Infrastructure/Evaluators/ReturnEligibilityEvaluator.cs` lines 138–160) now counts
`Requested`, `Approved`, `RefundProcessing`, **`RefundFailed`**, `Completed`. `Rejected`/`Cancelled` stay absent on
purpose, which is what releases capacity. Same evaluator, no second counter, delivered slices and the captured
per-line window are untouched.

Proven by `Refund_failed_keeps_its_capacity_while_a_rejected_case_releases_it`: after
`Approve → MarkRefundProcessing → MarkRefundFailed` the line reports `RemainingReturnableQuantity = 0` and a new request
is refused with `return.quantity_exceeded`, while a `Rejected` case on another order restores `2` units and accepts a
new request.

## D — one serialized check-and-insert per seller order

`ReturnDirectory.Creation.cs` lines 66–133, `ReturnCreationSerialization.cs`:

- The decision opens **Returns' own** transaction (`ownsTransaction` is false when a caller already owns one, so the
  module participates instead of nesting).
- `pg_advisory_xact_lock(key)` (lines 62–70) with the deterministic key
  `AdvisoryLockNamespace ^ BitConverter.ToInt64(sellerOrderId bytes)` (lines 32, 51–55). The namespace is a durable
  constant (`0x544F4F4241_000272`), documented as never to be reused.
- The lock is **transaction-scoped**: PostgreSQL releases it on commit, rollback, cancellation or connection loss, so
  there is no manual release path to get wrong. A refusal throws from inside the transaction and the `await using`
  rolls it back.
- The eligibility read happens **after** the lock (line 92), so it observes every creation that committed while this
  request waited; no stale pre-lock quantity is reused.
- Only Returns' own rows are touched inside the transaction: no foreign Order/Fulfillment table lock, no foreign-schema
  SQL, no money/stock/provider call (creation performs none).

**Real contention, independent connections, canonical `tooba_alpha`:**

| Test | What it actually proves |
| --- | --- |
| `The_creation_decision_waits_for_the_seller_order_advisory_lock` | a foreign session holds `pg_advisory_xact_lock` for that order; the creation task is **not** completed after 1200 ms and completes only after the holder's rollback ⇒ the writer takes the same lock |
| `Concurrent_creations_never_over_allocate_the_last_returnable_unit` | 3 rounds; 3 delivered units, 2 already committed, two **different** keys created simultaneously on own contexts/connections ⇒ exactly 1 accepted, 1 refused `return.quantity_exceeded`, 2 rows, requested units exactly 3 |
| `A_refused_creation_leaves_no_row_and_releases_its_lock` | the refusal rolls back (1 row total) and the next writer on the same order is not blocked; exact replay still answers the committed case |

**Committed two-writer result: PROVEN (declared deviation).** A rolled-back transaction is invisible to a competing
connection, so a true race cannot be demonstrated without a commit. The race tests therefore COMMIT task-labelled rows
(`reason = "TB272 relational"`, idempotency keys prefixed `tb272-rel-<runTag>-…`) and delete exactly those keys
afterwards; after the final run `select count(*) … like 'tb272-rel-%'` = `0` in both `return_requests` and
`return_items`. No user row was created, updated or deleted, and no business SQL/DDL/backfill/reset was executed.

## Cross-origin audit (required by the task)

The courier-origin path keeps Fulfillment receipt/claim ownership and its own ordering; this slice does not force its
undelivered physical source into ordinary delivered-line eligibility. The key-collision audit is explicit: a stored
courier-origin case carries `Origin = CourierOrigin`, so an ordinary replay of that key is a typed
`return.idempotency_conflict` and never borrows the courier snapshot
(`A_stored_courier_origin_key_is_not_replayed_as_an_ordinary_case`). A shared physical-capacity redesign across the two
origins is **not** part of 272 and is reported here as out of slice.
