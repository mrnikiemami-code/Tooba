# TB-277 — approval vs. refund proof

The user's complaint was behavioural, not cosmetic: pressing «تأیید مرجوعی» executed the refund. That is now a backend
boundary, and the proof below is source + tests, not a screenshot.

## 1. `ApproveAsync` records only the decision

`src/backend/Modules/Returns/Tooba.Returns.Infrastructure/Directories/ReturnDirectory.cs`

```
ApproveAsync
  EnsureCanMutateAsync
  LoadMutableAsync
  EnsureOwningSellerScope                 (owning seller scope, TB-274-R1)
  read the durable decision
      · a stored REJECTION  → return.already_rejected, zero writes
      · a stored APPROVAL   → MapSnapshotAsync, ZERO effect (even after money moved/failed/completed)
  EnsureOrdinaryReceivingCompleteAsync    (complete + coherent recorded customer receipt)
  ResolvePaymentAsync                     (fixed payment binding; must be Succeeded)
  RecordReturnDecisionAsync(Approve)       (durable decision row, under the case lock, once)
  request.Approve(paymentId, now, destination)   ← ONLY the case moves to Approved
  SaveChanges / telemetry / MapSnapshotAsync
```

There is **no** `request.MarkRefundProcessing(...)`, **no** `request.BeginRefundAttempt(...)`, **no**
`ExecuteRefundAsync`, **no** wallet credit, **no** `RestockConsumedReservationAsync`, **no** courier closure call inside
approval anymore. `ApproveAsync` ends at `Approved`.

Audited callers: `ReturnAdminOperationsAdapter.ApproveAsync` → `IReturnAdminOperations.ApproveAsync` (Order operations
route) — decision semantics only; `ReturnAdminEndpoints` publishes receipt/retry/recover/start-refund separately; no event
handler or legacy adapter starts money on approval (a `return.approved.v1` fact without `refund.succeeded.v1` is a lawful
stored state now).

## 2. The FIRST refund is its own command, route and identity

| Layer | Addition |
| --- | --- |
| Application | `StartReturnRefundCommand(ReturnRequestId, ActorUserId)`; `StartReturnRefundCommandValidator` (case id only) |
| Port | `IReturnDirectory.StartFirstRefundAsync` |
| Directory | `ReturnDirectory.FirstRefund.cs` → `StartFirstRefundAsync` |
| Contracts | `IReturnAdminOperations.StartFirstRefundAsync` |
| Adapter | `ReturnAdminOperationsAdapter.StartFirstRefundAsync` |
| HTTP | `POST /v1/admin/returns/{returnRequestId:guid}/start-refund` (existing admin authorizer) |

`ReturnFirstRefundIdentity`:

```
attemptId      = SHA256("tb277-first-refund:{caseId}")   (deterministic GUID)
idempotencyKey = "refund-{caseId:N}"
```

`StartFirstRefundAsync` order of work:

```
guard / RefuseConstrainedTransaction          (no caller/ambient/enlisted/own transaction)
load case → MapSnapshotAsync → ReturnRecoveryPolicy.Describe
exact identity lookup (attemptId OR key)
    · found → answer the STORED row with zero effect (Pending / Failed / Succeeded; never re-sent)
description.Phase == Blocked                  → typed blocker, zero effect
description.Phase != AwaitingFirstRefund      → return.refund.first_not_lawful, zero effect
re-read recorded APPROVE decision             → typed refusal if missing/opposite
status must be Approved; fixed paymentId required
courier origin → EnsureCourierOriginClosureAsync (claim/obligation/branch BEFORE money)
ordinary       → recorded receipt must be coherent + complete
payment status must be Succeeded
MarkRefundProcessing + BeginRefundAttempt + insert  (durable BEFORE the external call)
23505 on pk_refund_attempts / ix_refund_attempts_IdempotencyKey → detach this entry, answer the stored row, no second send
transition 0        (stored pending) / unavailable (unconfigured PSP: typed 503, attempt kept)
                 / final  (existing ExecuteRefundAsync coordination)
courier completed + fully disposed → existing MarkRefundReleasedWhenFullyDisposedAsync
```

Key properties the task demanded:

1. **Authorization re-checked every call** (`EnsureCanMutateAsync` + actor from the authorizer).
2. **Recorded approval required** — the decision row and `Status == Approved` are both re-read.
3. **One durable attempt per case** — derived identity, persisted before the call; a duplicate click, a lost response or a
   different client key resolves to the same row; a terminal attempt is never re-sent.
4. **No external call inside a transaction or lock** — the refusal runs before any effect and the existing
   `ExecuteRefundAsync` boundary keeps its own guard.
5. **First start ≠ resume ≠ retry** — `AwaitingFirstRefund` only; a pending attempt reports `PendingMoney` with
   `resume_pending_refund`, a definite failure reports `FailedMoney` with `retry_refund`.

## 3. One shared stored-fact description, extended additively

`ReturnRecoveryPhase.AwaitingFirstRefund = 6` (appended; every shipped numeric value keeps its meaning) and
`ReturnRecoveryPolicy`:

- `MoneyEvidence` gains `Absent`; **zero attempts is a truthful stored fact**, not `attempt_unbound`.
- `Absent` + `Approved` → `AwaitingFirstRefund`, `FinancialComplete=false`, no attempt id, no recovery kind; every
  precondition the command checks is mirrored as a typed blocker instead of a guess (`RecoveryEvidenceConflict`,
  `RecoveryReceivingRequired`, `RefundPaymentMissing`), and a `RefundProcessing` case with zero attempts is
  contradictory → `RecoveryAttemptUnbound` (never "ready to start").
- `ReturnWorkPolicy` maps `AwaitingFirstRefund` → category `refund`, `CanStartRefund = true`, `NeedsOperator = true`,
  `ActionCodes == ["start_refund"]`, no pending/failed id.
- `AdminReturnQueueFilters.ComposeRefundStatus` no longer reports `Approved` with no attempt as «pending» (that was the
  old combined semantics); a pending refund now requires an actual pending attempt or `RefundProcessing`.

Contracts are additive and trailing: `ReturnWorkSnapshot.CanStartRefund`, `ReturnItemSnapshot.UnitDecimalPlaces`,
`ReturnRecoveryPhaseOption.AwaitingFirstRefund`, one error code `return.refund.first_not_lawful`. No numeric renumbering,
no migration.

## 4. Tests that pin the boundary

`ReturnsExplicitRefund277Tests` (6/6):

```
Approval_records_the_decision_only_and_sends_no_money_and_no_stock
    Approved + recorded decision; RefundAttempts empty; attempt count 0;
    gateway calls empty; Inventory.RestockCalls 0; Wallet calls empty
Approved_without_an_attempt_describes_the_first_refund_as_active_operator_work
    category refund; CanStartRefund true; CanResumeRefund/CanRetryRefund/CanSettlePhysical false;
    NeedsOperator true; IsOpen true; ActionCodes == [start_refund]; no pending/failed id
The_first_refund_is_one_deterministic_attempt_and_a_second_call_replays_it_with_zero_effect
    attempt id/key == derived; exactly ONE gateway call with that key and the case amount;
    second call: still 1 attempt, still 1 call; afterwards CanStartRefund false
An_unapproved_case_can_never_start_its_first_refund
    return.refund.first_not_lawful; 0 attempts; 0 gateway calls
A_pending_first_attempt_is_answered_from_storage_and_never_re_sent
    stored Pending attempt answered; 0 gateway calls; 1 attempt; CanResumeRefund true
The_admin_detail_publishes_the_units_own_precision_and_never_invents_one
    7 from the owning line → 6; unit g; product title; unanswered owning line → null/null/null
```

Retained suites were adapted, not weakened: `ReturnsCustomerReceipts274Tests` / `-R1Tests` now call the explicit
`StartRefundAsync` where the financial execution is under test (approval alone no longer completes the case), and
`ReturnsOperatorWorkflow275Tests` keeps the classification facts — including `Completed` + `disposition None` →
`Disposition` + `CanSettlePhysical` (money success alone is **not** settled) and `Completed` + `SaleableRestock` →
`Settled`.

## 5. Live state (read-only)

`GET /v1/admin/returns/01a1e6b0-f33a-7000-80ee-beffb6344490` returns

```
status Completed · recordedDecision Approve
receipts 1 (00:31:06Z) · refundAttempts 1 Succeeded (00:31:26Z, key refund-01a1e6b0f33a700080eebeffb6344490)
item disposition SaleableRestock / saleableRestockedQuantity 1
recovery phase Settled, financialComplete true, physicalComplete true
work category settled, canStartRefund false, actionCodes []
```

i.e. the user's own earlier exercise (receipt → approve → refund → physical settlement) is intact; nothing in this task
wrote to it (counts and `updated_at` unchanged; no returns outbox event after `00:31:26Z`). The only POST issued was the
disclosed `start-refund` replay that returned the stored Succeeded attempt with zero effect.

## 6. Limits

- No real PSP/Wallet/Inventory call is made anywhere in this proof: the directory uses controlled ports, and the live Host
  uses its configured (non-production) adapters only for reads.
- No committed two-writer race; the uniqueness/replay property is proven by the deterministic identity and the stored-row
  answer, plus the narrowed `23505` arbitration.
- Courier-origin first-refund closure gating is covered by the retained 273/274 suites and the existing guard code; it is
  not re-exercised with a live courier case here.
