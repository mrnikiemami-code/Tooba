# Decision collision proof — TB-274-R1 (D)

## 0. The discrepancy this repairs (explicit correction of the parent proof)

The parent 274 `RESULT.md` / `disposition-and-decision-proof.md` §3 stated that the global tracker clear was removed
from the decision collision path. **That claim was wrong**: `ReturnDirectory.Decisions.cs` still called
`_db.ChangeTracker.Clear()`, filtered only `ConstraintName`, and returned the collision winner without repeating the
changed-reason comparison. The parent report is left unchanged as history; this file is the corrected fact.

## 1. What the code does now

| Concern | Implementation |
| --- | --- |
| Savepoint | the decision insert runs inside `SAVEPOINT tb274_decision_intent` (relational + owning transaction only); `RELEASE SAVEPOINT` on success |
| Collision recovery | `ROLLBACK TO SAVEPOINT` + `RELEASE SAVEPOINT` — the caller's transaction is never committed, rolled back or otherwise taken over; when there is no owning transaction the pre-existing `ownsTransaction` rollback is the only fallback |
| Tracking scope | a snapshot of already-tracked entity instances is taken **before** `Add`; `DetachAttemptEntries` detaches only entries that are `Added` and were **not** in that snapshot. There is no `ChangeTracker.Clear()` |
| Exact identity | `IsDecisionKeyConflict` requires SQLSTATE `23505` **and** `ConstraintName == pk_return_request_decisions` **and** `TableName == return_request_decisions` **and** `SchemaName == returns` |
| One winner comparison | the collision winner goes through the same `AnswersDecision` + `NormalizeDecisionReason` (`Ordinal`) comparison the normal stored read uses: same kind + same normalized reason replays; a different reason is `return.decision.reason_conflict` with no write; a legacy `null` reason is never overwritten |
| Ordering | the owning seller scope refusal runs **before** the durable decision is recorded (ReturnDirectory.cs L335, before `RecordDecisionAsync`) |
| Not reopened | the separate legacy concurrent approval algorithm and the full financial retry design are untouched |

## 2. Controlled proof (`ReturnsCustomerReceipts274R1Tests`, in-memory EF + a collision interceptor that raises the
exact `PostgresException` shape)

| Fact | Assertion |
| --- | --- |
| `D_the_owning_decision_primary_key_arbitrates_the_collision_and_keeps_unrelated_tracked_work` | an armed winning `Reject` with the **same** reason answers the intent: `1` decision row, case `Rejected`, reason preserved, `0` gateway calls, `0` attempts. An **unrelated** caller-owned tracked entity keeps its *instance* (`Assert.Same`) and its value (`USD`, durable in the store) — a global clear would have broken both. No `Added` decision entry survives the refusal, so a later real save cannot replay the failed insert |
| `D_a_winner_with_a_different_reason_is_a_typed_conflict_with_no_mutation` | the stored winner keeps `دلیل برنده`, the caller's different reason raises `return.decision.reason_conflict`, still `1` decision row, `0` gateway calls |
| `D_a_failure_on_another_constraint_is_never_read_as_a_decision_collision` | three lookalikes each propagate the raw `DbUpdateException`/`23505`: a foreign constraint (`ix_return_requests_idempotency_key`), the right constraint name on the **wrong table** (`return_requests`), and the right table/constraint in the **wrong schema** (`public`). In every case: `0` decisions stored, `0` gateway calls, `0` attempts, status still `Requested`, no reason, no claim release |

The injection is **controlled and labelled as such**: it raises the exact provider error object; it is not a real
two-writer race.

## 3. Real store

| Fact | Assertion |
| --- | --- |
| `ReturnsCustomerReceipts274R1RelationalTests.B_a_foreign_seller_scope_cannot_write_a_decision_on_the_real_store` | on real `tooba_alpha` the owning-directory scope refusal precedes the decision write: a foreign seller scope writes no decision row (caller-owned transaction, rolled back, `committedRows = 0`) |
| Real constraint identities | the CHECK/composite-FK names used by the storage integrity work exist on `returns.customer_return_receipt_lines` (verified read-only by `\d` and by the refusals in `B_the_stored_checks_refuse_a_nonpositive_quantity_and_an_undefined_inspection`) |

## 4. Limits (declared, not claimed)

- The **savepoint branch itself** (`relational && transaction is not null`) is not exercised by any test fact: the unit
  suite runs on the in-memory provider, and a real duplicate-key state cannot be produced inside a rollback-only
  caller transaction without a second writer (the only lawful second-writer state is a committed row, which this task
  forbids). The arbitration *decision* logic, the scoped detach and the winner comparison are proven; the SQL
  `SAVEPOINT`/`ROLLBACK TO` execution is production-only.
- No committed race, no fixture row, no cleanup/restore.
- The `Reject` winner path is the audited one; the legacy concurrent-approval algorithm is out of scope by instruction.
