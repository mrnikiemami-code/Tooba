# A — Binding and finalization proof (fail-closed stored evidence)

One pure policy: `Tooba.Returns.Application/ReturnRequests/Models/ReturnRecoveryDescription.cs`
(`ReturnRecoveryPolicy`). Callers: `ReturnDirectory.MapSnapshotAsync` (every read) and
`ReturnDirectory.RecoverAsync` (the action). `RecoverAsync` keeps no second, permissive decision read.

## 1. Financial evidence (`ReadMoney`)

| Stored fact | Phase / result | Effect |
| --- | --- | --- |
| `RecordedDecision == "Reject"` | `Blocked` + `return.already_rejected` | none; the read reports the same code |
| `RecordedDecision` missing/blank/other | `Blocked` + `return.recovery.evidence_conflict` | none; an approval is never inferred from status, caller or default |
| any live (`Pending`/`Succeeded`) attempt whose payment, amount, currency or id/key does not match this case | `Blocked` + `return.recovery.attempt_unbound` | none |
| ≥2 live bound attempts, or 1 succeeded **and** 1 pending | `Blocked` + `return.recovery.attempt_unbound` | none |
| exactly 1 bound `Succeeded` | money confirmed (`FinancialComplete: true`) | no resend |
| exactly 1 bound `Pending` | `PendingMoney` + `ResumePendingRefund` + that id | resumable |
| no live bound row, exactly 1 bound `Failed` | `FailedMoney` (older failed history is preserved but is not the pending attempt) | the new-attempt retry is a different intent |
| no live bound row, no single bound failure | `Blocked` + `return.recovery.attempt_unbound` | none |

`IsBoundToCase` requires all of: non-empty attempt id, non-blank idempotency key, non-zero case `PaymentId`,
`attempt.PaymentId == case.PaymentId`, `attempt.Amount == case.RefundAmount`, trimmed currency equality.

## 2. Physical phase (`Physical`)

| Stored fact | Result |
| --- | --- |
| no received item | `Blocked` + `return.recovery.evidence_conflict`, `FinancialComplete: true` |
| ordinary RMA, every line disposed | `Settled` (no invented receipt/inspection step) |
| ordinary RMA, any line `None` | `Blocked` + `return.recovery.receiving_required`; the bound money is kept, nothing is restocked |
| courier, incoherent source/claim/receipt/line/condition facts | `Blocked` + `return.recovery.evidence_conflict`, money kept, no stock call |
| courier, `ClosurePhase == RefundReleased` but a line is still `None` | `Blocked` + `return.recovery.evidence_conflict` (the marker cannot be truthful) |
| courier, every line disposed **and** `ClosurePhase == RefundReleased` | `Settled`, `PhysicalComplete: true` |
| courier, everything else | `PendingDisposition` + `SettlePhysicalDisposition` (financial complete, physical not) |

`HasCoherentCourierSource` requires non-empty `SourceFulfillmentId`, `SourceShipmentId`, `SourceReceiptId`,
`SourceClaimId` and occurrence key; every item's `SourceReceiptId`/`SourceShipmentId` equal to the case's; and
`Damaged ⇒ None|DamagedNonSaleable`, other ⇒ `None|SaleableRestock`. It reads only facts the case already stores; no
foreign SQL and no second source owner.

## 3. Command alignment

`RecoverAsync` → `Describe` once, then dispatch:

- `ResumePendingRefund`: the expected id must exist on the case; a non-`Pending` attempt is answered as stored **only**
  when `description.RefundAttemptId == expected`, else `return.recovery.attempt_mismatch`; a `Blocked` description
  refuses with **its own** blocker before the pending check; then `Phase must be PendingMoney` with the same id.
- `SettlePhysicalDisposition`: `Settled` ⇒ stored answer with zero effect; `Blocked` ⇒ its blocker; then
  `PendingDisposition && FinancialComplete` is required. `EnsureCourierOriginClosureAsync` is a no-op when the closure
  markers are already durable and `SettlePhysicalDispositionAsync` skips disposed lines, so a marker-only recovery
  performs **no** gateway/Wallet/Inventory call and creates **no** attempt.

## 4. Final marker (the last closure step)

`MarkRefundReleasedWhenFullyDisposedAsync` (replaces the in-memory `MarkRefundReleasedWhenFullyDisposed`):

1. ordinary origin → nothing to finalize;
2. durable re-read of `return_items.disposition` — any `None` (or no line) → return;
3. durable re-read of `ClosurePhase/ObligationClosedAt/StockSettledAt` — already `RefundReleased`, or an earlier marker
   still missing → return;
4. mark `RefundReleased`, `SaveChangesAsync`; on failure `RestoreTrackedAsync` (scoped `ReloadAsync`, detach only if the
   store cannot be re-read) and rethrow — never a global `ChangeTracker.Clear()`.

Proof: `Every_item_disposed_with_only_the_final_marker_pending_is_recoverable_with_zero_money` (marker-only completion,
0 external calls, 1 attempt, settled replay zero-effect) and
`A_lost_final_marker_save_never_reports_settled_and_the_same_scope_retry_completes_it` (injected failure on the marker
save; a **separate** `DbContext` confirms `ClosurePhase` stayed `StockSettled` while the lines stayed disposed; the same
scope then completes from reloaded truth with 0 external calls and exactly one attempt).

Item-save failures (`ReturnDirectory.CourierOrigin.cs`) now reload only the affected line, proven by
`A_lost_item_save_never_promotes_an_unsaved_disposition_or_double_restocks`: durable `None` + `StockSettled` after the
failure, then one restock with the unchanged per-item identity `return-restock-{case:N}-{item:N}`.

## 5. What this proof does NOT claim

- No committed real case exists, so nothing here demonstrates a served operator recovery.
- No relational coverage of the marker/`FOR UPDATE` branch (it needs a committed row); those facts are controlled-port
  facts with an injected save failure.
- `Settled` is not claimed for any live row: the canonical `returns.return_requests` table is empty.
