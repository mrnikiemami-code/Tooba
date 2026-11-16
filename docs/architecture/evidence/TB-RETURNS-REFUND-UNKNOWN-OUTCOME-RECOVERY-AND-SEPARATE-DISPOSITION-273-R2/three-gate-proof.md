# THREE-GATE PROOF — TB-RETURNS-REFUND-UNKNOWN-OUTCOME-RECOVERY-AND-SEPARATE-DISPOSITION-273-R2

Date: 2026-11-16
Expected base: `02af2261032adf3aeb299fa9c369ee3b5154718d` (Architect docs-only review following `a2ffbf60d`)
Actual base at claim: `02af22610` (fast-forwarded from local `a2ffbf60d`; no divergence, no dirty work)
Code: `2481d40b98182bd17cf72614ff5c45173ef310ad`

This document maps each of the three issued gate omissions to the exact code path that now closes it, to the named
case that proves it, and to what was actually OBSERVED. It separates:

- **controlled unit proof** — in-memory `ReturnsDbContext` with controlled gateway/Wallet/Inventory ports;
- **real relational refusal** — one rollback-only fact on the canonical PostgreSQL `tooba_alpha`;
- **served build / wire** — the republished Host 5088 read-only probes;
- **not proven** — no live return case exists, so there is no served operator walkthrough of a real recovery.

The policy remains ONE function, `ReturnRecoveryPolicy.Describe(ReturnSnapshot)`
(`src/backend/Modules/Returns/Tooba.Returns.Application/ReturnRequests/Models/ReturnRecoveryDescription.cs`), used by
`MapSnapshotAsync` for every read and by `RecoverAsync` for both intents. No predicate was duplicated in the command.

---

## A — courier source coherence BEFORE pending money

### Source defect (accepted)

`ReadMoney` was evaluated first and `PendingMoney` returned at lines 113–130 without any source check;
`HasCoherentCourierSource` was reached only through `Physical` (line ~294), i.e. after a succeeded attempt. A stored
approved courier case with a malformed receipt/source/condition could be advertised as resumable and reach
`RecoverPendingRefundAsync → EnsureCourierOriginClosureAsync` (whose completed `StockSettled` marker made it a no-op)
and then the money port.

### Repair (single rule, before the phase is offered)

`ReturnRecoveryDescription.cs` lines 127–139: inside the `Approved | RefundProcessing` branch, the `money.IsPending`
case now evaluates the EXISTING `HasCoherentCourierSource(snapshot)` before constructing `PendingMoney`. A courier case
that fails the rule is `Blocked` with `ReturnsErrorCodes.RecoveryEvidenceConflict`, `RecoveryKind = null`,
`FinancialComplete = false`, `PhysicalComplete = false` — no executable intent, no attempt id offered.

The rule itself is unchanged: non-empty `SourceFulfillmentId`, `SourceShipmentId`, `SourceReceiptId`, `SourceClaimId`
and `SourceOccurrenceKey`; every item pointing at that SAME receipt+parcel; and the recorded condition/disposition
pair legal (`Damaged → None | DamagedNonSaleable`, otherwise `None | SaleableRestock`).

Because `RecoverAsync` decides on the SAME description (`RecoverPendingRefundAsync`, `description.Phase == Blocked`
branch), the read blocker and the action refusal are the same typed code by construction.

### Observed proof

| Case | Kind | Observed |
| --- | --- | --- |
| `Incoherent_courier_source_blocks_pending_money_in_the_same_policy_the_command_uses` | controlled policy | coherent pending courier = `PendingMoney` + `ResumePendingRefund`; each of 8 broken shapes (missing claim, empty fulfillment id, missing receipt, blank occurrence key, wrong item receipt, wrong item parcel, illegal usable→non-saleable disposition, illegal damaged→restock) = `Blocked` / `return.recovery.evidence_conflict` / `RecoveryKind = null` / `FinancialComplete = false`; a bound SUCCEEDED case with a broken source stays `FinancialComplete = true` and `Blocked` (money preserved, never re-sent) |
| `Pending_courier_money_with_a_broken_source_is_refused_before_any_money_or_owner_call` | real `ReturnDirectory`, controlled ports | three separately corrupted stored shapes (missing `SourceClaimId`; item receipt+parcel pointing elsewhere; usable unit marked `DamagedNonSaleable`): read = `Blocked` / `return.recovery.evidence_conflict` / no kind; `RecoverAsync(ResumePendingRefund)` = SAME `return.recovery.evidence_conflict`; gateway 0, Wallet 0, Inventory 0 calls; attempt still `Pending` with the SAME id and key and no provider reference; case still `RefundProcessing`, closure still `StockSettled`, attempt count 1 |
| `A_coherent_pending_courier_case_resumes_the_same_bound_attempt_and_key` | real `ReturnDirectory`, controlled ports | a lawful case (both closure timestamps + `StockSettled`) resumes the SAME attempt id/key/payment/amount once, gateway called once with that key, Wallet never called, no new attempt, one recorded decision; the post-success physical phase is the existing path |

Not proven: no live courier case exists (live `totalCount=0`), so no served operator walkthrough.

---

## B — `Settled` requires the prior durable closure evidence

### Source defect (accepted)

`Physical` reported `Settled` from `allItemsDisposed && ClosurePhase == RefundReleased`; `HasCoherentCourierSource`
never inspected `ObligationClosedAt`/`StockSettledAt`, while the finalization writer
(`MarkRefundReleasedWhenFullyDisposedAsync`) requires both. A forged/incomplete final marker was therefore reported
as completion and the physical command returned early.

### Repair

`ReturnRecoveryDescription.cs` lines 319–333: a `RefundReleased` marker whose `ObligationClosedAt` OR `StockSettledAt`
is null is now `Blocked` with `return.recovery.evidence_conflict`, `FinancialComplete = true`,
`PhysicalComplete = false`, `RecoveryKind = null`. Nothing is backfilled, repaired or re-stamped; no new chronology,
SLA or lifecycle stage was introduced. The existing `RefundReleased + not all disposed` guard is unchanged, and the
coherent `StockSettled` marker-only state still maps to `PendingDisposition` → `SettlePhysicalDisposition`.

### Observed proof

| Case | Kind | Observed |
| --- | --- | --- |
| `A_recorded_final_marker_without_its_prior_closure_timestamps_is_inconsistent_not_settled` | controlled policy | coherent `RefundReleased` = `Settled` (financial+physical complete); each of missing obligation / missing stock / both = `Blocked` / `return.recovery.evidence_conflict` / `RecoveryKind = null` / `FinancialComplete = true` / `PhysicalComplete = false`; coherent `StockSettled` = `PendingDisposition` + `SettlePhysicalDisposition` |
| `A_final_marker_missing_its_prior_evidence_refuses_physical_recovery_and_repairs_nothing` | real `ReturnDirectory`, controlled ports, 3 variants | obligation-only, stock-only and both missing: read `Blocked` / evidence conflict; `RecoverAsync(SettlePhysicalDisposition)` refuses with the SAME code; gateway 0, Wallet 0, Inventory 0 calls; attempt count 1 and the succeeded reference unchanged (`ref-273-courier`); stored closure stays `RefundReleased`; the missing timestamp stays null (no backfill) and the present one keeps its value |
| retained `Every_item_disposed_with_only_the_final_marker_pending_is_recoverable_with_zero_money` | real `ReturnDirectory` | coherent `StockSettled` + both timestamps + all items disposed still finishes only the final marker: gateway/Wallet/Inventory 0 calls, 1 attempt, replay stays `Settled` and zero-effect |
| retained `A_lost_final_marker_save…` / `A_lost_item_save…` | real `ReturnDirectory` | interrupted saves still never report `Settled`; the same-scope retry completes from durable facts |

Not proven: no live case; no live completion/backfill was executed.

---

## C — explicit provider enlistment refused without an ambient scope

### Source defect (accepted)

`RefuseConstrainedTransaction` checked only `_db.Database.CurrentTransaction` and
`System.Transactions.Transaction.Current`. EF's provider-enlisted shape has BOTH null.

### Repair

`src/backend/Modules/Returns/Tooba.Returns.Infrastructure/Directories/ReturnDirectory.cs` lines 685–709:

```csharp
private void RefuseConstrainedTransaction()
{
    if (_db.Database.CurrentTransaction is not null
        || System.Transactions.Transaction.Current is not null
        || HasEnlistedProviderTransaction())
    {
        throw new ContractOperationException(ReturnsErrorCodes.RecoveryTransactionOwned);
    }
}

private bool HasEnlistedProviderTransaction() =>
    _db.Database.IsRelational()
    && _db.Database.GetService<IRelationalConnection>() is RelationalConnection { EnlistedTransaction: not null };
```

`RelationalConnection.EnlistedTransaction` is the installed provider's own public transaction-manager state
(EF Core `Microsoft.EntityFrameworkCore.Relational` 8.0.11, reflected and compile-verified: `get_EnlistedTransaction`
is public; `IRelationalConnection` does not expose it, `RelationalConnection` does). No reflection, string matching or
second transaction framework is used. The SAME guard remains the only precondition and is still called at all three
existing sites (command entry, the shared `ExecuteRefundAsync` boundary, and before the outcome coordination opens its
own transaction). A non-relational controlled provider owns no enlistment, so unit tests keep working without claiming
provider proof.

### Observed proof (real PostgreSQL, rollback-only)

`ReturnsRecovery273RelationalTests.An_explicitly_enlisted_caller_transaction_refuses_effectful_recovery_before_any_effect`
— run ALONE with an exact method filter, target `tooba_alpha:5432/172.18.0.2`, `committedRows=0`:

1. the caller stages the approved case inside its own EF transaction and rolls that transaction back (nothing commits);
2. `await db.Database.OpenConnectionAsync()`; `((RelationalConnection)db.Database.GetService<IRelationalConnection>())
   .EnlistTransaction(new CommittableTransaction())` — explicit enlistment, outside any `TransactionScope`;
3. BEFORE the action it asserts `db.Database.CurrentTransaction is null`, `Transaction.Current is null` and
   `connection.EnlistedTransaction` is the caller's transaction;
4. `RecoverAsync` throws `return.recovery.transaction_owned` — note the staged case row does NOT exist (its transaction
   was rolled back), so `return.missing` would have been the answer if any lookup ran first; the guard therefore
   demonstrably fired before the read and before any effect;
5. gateway 0, Wallet 0, Inventory 0 calls; the caller's enlistment is still present; the tracked `RefundAttempt` is
   still `Unchanged`/`Pending`;
6. the caller rolls its own transaction back and a separate context confirms `committedRows = 0`.

Existing caller-owned and ambient facts are unchanged and were NOT re-run in this round.

Provider limitation declared: `NpgsqlConnection`'s own `EnlistedTransaction` is internal, so a caller that bypasses the
owning context and enlists the raw `DbConnection` directly is not separately observable through supported APIs. The
supported owning-context path (the one this guard exists for) is proven above.

---

## Boundaries actually observed

- One canonical `tooba_alpha` on `127.0.0.1:5432` (SingleStore/store-alpha); Host 5088 / FE 3000.
- No migration, DDL, seed, backfill, cleanup, delete or restore; no live approval/reject/refund/recovery mutation; no
  provider/PSP/Wallet/Inventory/waybill/notification/money operation; no committed task row.
- FE untouched (no build, test, restart or screenshot).
- No new error code, no catalog/route/validator count change, no architecture-guard run.
