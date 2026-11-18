# TB-279-R1 — budget and decision proof (F1, F4)

## F1 — one shared reservation protocol for FIRST and RETRY money

`ReturnDirectory.Reservations.cs` is the only place a NEW money attempt is minted.

1. `RequirePaymentAuthorityAsync` runs **before** any transaction/lock. It binds the owning payment to the case:
   payment id, checkout id, trimmed currency and amount, status `Succeeded`; a missing payment is
   `return.refund.payment_missing` (never "budget unknown, continue"), a foreign checkout/currency/payment is
   `return.refund.budget_binding_invalid`, a non-successful payment is `return.refund.already_started`, and
   `amount < 0 || amount > payment.Amount` is `return.refund.budget_exceeded`.
2. `PersistNewAttemptUnderReservationAsync` then takes the documented order **seller order → case row → payment budget**
   (`LockOwningSellerOrderAsync`, `FOR UPDATE` on `returns.return_requests`, `ReturnBudgetSerialization.AcquireAsync`),
   re-reads the tracked case and its attempts, re-reads the effective decision state, and refuses a stale reviewed
   version (`return.correction.version_stale`) before anything is minted.
3. The budget counts **every** Returns obligation of that payment
   (`refund_attempts` where `payment_id = @payment` and `status in (Pending, Succeeded)`), so a sibling seller case of
   the same checkout can never independently pass the ceiling. `Pending` (unknown outcome) and `Succeeded` keep
   spending; a definite `Failed` stops counting — that is exactly "release only its own reservation".
4. The attempt row and the case processing state are saved and committed (when this directory owns the transaction)
   **before** the gateway/Wallet call, so two requests cannot both reserve the same capacity, and a lost local save is
   never answered as durable money.
5. First refund and the reviewed retry child share this method; `StartFirstRefundAsync` keeps its deterministic
   case-derived attempt id/key, and a stored attempt (Pending/Failed/Succeeded) is answered from storage with zero
   effect (`AttemptReservation.AlreadyAnswered`).

Ordering guarantee: no owning payment read, gateway, Wallet or Inventory call runs under the locks. `Strict`
refusal of a caller-owned/enlisted/ambient transaction before external effects is preserved
(`RefuseConstrainedTransaction`), and the failure path releases the transaction.

## F4 — one atomic, version-fenced decision transition

`ApproveAsync` and `RejectAsync` no longer "record a decision now, move the case later". `InLockedDecisionScopeAsync`
holds the seller-order serialization + the case row lock, reloads the tracked case, and then `AppendReviewedDecisionAsync`
appends the reviewed revision (or the version-0 baseline) **in the same transaction** as:

* the case status transition (`request.Approve(...)` / `request.Reject(...)`),
* the frozen `approved_amount` derived from the accepted quantities × the STORED unit price,
* the capacity release of the never-received remainder on rejection.

The operator's reviewed `ExpectedDecisionVersion` travels in the frozen command (UI → API → CQRS → Directory); a
missing version is `return.refund.review_version_required`, a stale one is `return.correction.version_stale`, and an
exact stored replay is answered before those gates. Occurrence identities are deterministic (`SHA256` of the canonical
immutable intent, never `string.GetHashCode`), the legacy decision row stays the immutable version-0 baseline, and the
append-only revision table is the effective authority for every later read.

## Relational facts (real PostgreSQL, rollback-only)

File: `src/backend/Host/Tooba.Host.Tests/ReturnsClosure279RelationalTests.cs` — `3/3`, `committedRows=0` for all three.

| Fact | What it proves | Observed |
| --- | --- | --- |
| `The_payment_budget_counts_every_case_of_that_payment_and_no_other_payment` | the EXACT budget read counts a sibling case's `Pending` 60 of payment A and excludes payment B's `Pending` 100; `60 + 60 > 100`, `60 + 40 <= 100` | `counted=60.0000` / `counted=100.0000` |
| `A_second_session_really_waits_for_the_payment_budget_lock` | a foreign session holding `ReturnBudgetSerialization.AdvisoryLockKey(payment)` blocks the owning reservation step (still incomplete after 1.2 s) and release resumes it; a DIFFERENT payment is independent under the same namespace | `waited=true`, independent key acquired within `lock_timeout=2000ms` |
| `A_stale_reviewed_version_writes_nothing_and_a_lawful_approval_moves_every_fact_together` | reviewed version 5 on a version-0 case refuses with `return.correction.version_stale` and writes NOTHING (status `Requested`, `payment_id` null, `approved_amount` null, 0 decision rows); the lawful version 0 moves decision + status + frozen amount in ONE caller transaction, with the requested amount unchanged and 0 attempts | `approved=1000` (1 accepted × stored 1000), `requested=2000`, `attempts=0` |

Command:

```
$env:TB279R1_CONNECTION="Host=127.0.0.1;Port=5432;Username=admin;Password=123456;Database=tooba_alpha"
dotnet test src\backend\Host\Tooba.Host.Tests\Tooba.Host.Tests.csproj -c Release --filter "FullyQualifiedName~ReturnsClosure279RelationalTests"
```

Declared limits: a committed winner-vs-loser race is not claimed (the boundary rolls back every row, and a competing
row is invisible to another session until commit); the effectful first-refund intent refuses a caller-owned
transaction, so its own commit path is covered by the controlled-port suites; no provider/PSP/Wallet/Inventory effect.
