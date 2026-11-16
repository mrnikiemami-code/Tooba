# B/C/D — Transaction boundary, truthful reference, actual coverage labels

## B. No caller/ambient transaction may span an external effect

Implementation: `ReturnDirectory.RefuseConstrainedTransaction()` (TB-273-R1) throws
`ContractOperationException(ReturnsErrorCodes.RecoveryTransactionOwned)` when

```text
_db.Database.CurrentTransaction is not null      // caller-owned / enlisted EF transaction
|| System.Transactions.Transaction.Current is not null   // ambient TransactionScope
```

Enforced at three boundaries, each **before** any effect:

| Boundary | File / member | Why it is there |
| --- | --- | --- |
| Command entry | `ReturnDirectory.Recovery.cs` → `RecoverAsync` | refuses before the mutable case read, before the closure steps, before any gateway/Wallet/Inventory call and before any recovery write |
| Shared external-call boundary | `ReturnDirectory.cs` → `ExecuteRefundAsync` | so a **future direct caller** of the modified execution helper cannot silently bypass the precondition (the existing `retry_refund` path included) |
| Owning outcome coordination | `ReturnDirectory.cs` → `RecordRefundOutcomeAsync` | checked **before** that method opens its own `FOR UPDATE` transaction, so "durable before the physical phase" can never be false |

It never commits, rolls back or disposes the caller's transaction; nothing is mutated before the refusal, so tracked
entities keep their instance, state and values. No second connection, no distributed transaction, no lease table, no
daemon, no generic UoW.

Proof — `ReturnsRecovery273RelationalTests` (real `tooba_alpha`, rollback only, **2/2**):

| Fact | Asserted |
| --- | --- |
| `A_caller_owned_transaction_refuses_effectful_recovery_before_any_effect` | `return.recovery.transaction_owned`; 0 gateway / 0 Wallet / 0 Inventory calls; `db.Database.CurrentTransaction` still non-null; stored attempt still `Pending`; case still `RefundProcessing`; tracked attempt `Unchanged` + `Pending`; caller rolls back; `committedRows=0` |
| `An_ambient_transaction_scope_refuses_effectful_recovery_before_any_effect` | same code and zero-call assertions inside a `TransactionScope` (deliberately not completed); `committedRows=0` |

Target validation (before any write): client string must be loopback + port 5432 + database `tooba_alpha`; the server
must confirm `current_database()`/`inet_server_port()` and answer on a **loopback or private** address. Observed and
recorded: `tooba_alpha:5432/172.18.0.2`. A missing/invalid target produces a visible framework SKIP, never a silent pass.

Also asserted by the controlled-port suite: `gateway.TransactionProbe` / `wallet.TransactionProbe` check
`CurrentTransaction is null` **at the instant of the call**, so "no transaction spans the external call" is a positive
fact on the successful branches too.

Declared limit: the `FOR UPDATE` + `Reload` + apply-only-from-`Pending` branch is **not** exercised by the relational
facts (it requires a committed case row, forbidden here). The empty/missing-case wire probes never reach it either.

## C. Truthful provider reference

- `RefundAttempt.MarkSucceeded(string? providerReference, …)` — `null`/blank input stores `null`; a non-empty value is
  trimmed and preserved. The synthetic `refund-{attemptId:N}` fallback is removed from
  `ReturnDirectory.ExecuteRefundAsync`.
- `ApplyRefundSuccessAsync(…, string? providerReference, …)` passes the adapter's value through unchanged, so a `true`
  `GatewayRefundResult.Succeeded` with no reference is still recorded as a success with a truthfully absent reference —
  never an invented PSP id and never a fabricated provider rejection.
- Wallet keeps its own documented `wallet-refund:{caseId}` ledger identity; it is never presented as a PSP reference and
  never substitutes the destination.
- No migration: the column was already nullable.

Proof — `A_blank_provider_reference_is_stored_truthfully_and_never_invented`: adapter success with `reference: null` ⇒
attempt `Succeeded`, `ProviderReference` **null**, case `Completed`, bound money `FinancialComplete: true`, physical
phase explicitly `Blocked(return.recovery.receiving_required)`; a repeat answers the stored outcome with exactly **1**
gateway call and **1** attempt.

Durable physical flags after a failure: see `binding-and-finalization-proof.md` §4 (scoped `ReloadAsync` only).

## D. Coverage labels

| Required fact | Where | Label |
| --- | --- | --- |
| missing/opposite approval blocked, positive fixtures carry a real decision row | `A_case_without_a_recorded_approval_is_blocked_in_read_and_refused_with_zero_effect`, `An_opposite_recorded_decision_is_blocked_and_never_refunded` | controlled ports, real `ReturnDirectory`, EF-InMemory |
| payment/amount/currency/key mismatch & ambiguity ⇒ same read blocker + zero effect | `A_succeeded_attempt_that_does_not_bind_to_the_case_never_authorizes_money_or_stock`, `A_pending_attempt_with_a_mismatched_amount_is_blocked_in_read_and_action`, retained ambiguity fact | controlled ports |
| all items disposed + `StockSettled` + no `RefundReleased` ⇒ pending finalization, zero money | `Every_item_disposed_with_only_the_final_marker_pending_is_recoverable_with_zero_money` | controlled ports |
| last item-save and final marker-save failure stay honest; same-scope retry does not trust unsaved flags | `A_lost_item_save_never_promotes_an_unsaved_disposition_or_double_restocks`, `A_lost_final_marker_save_never_reports_settled_and_the_same_scope_retry_completes_it` | controlled ports + injected local save failure + a **separate** `DbContext` read |
| caller-owned / ambient transaction refused, caller state untouched | `ReturnsRecovery273RelationalTests` (2 facts) | **real PostgreSQL, rollback only**, canonical target validated |
| null/blank PSP reference truthful, repeat without fresh money | `A_blank_provider_reference_is_stored_truthfully_and_never_invented` | controlled ports |
| retained same-key unknown/timeout/unconfigured/Wallet/physical-only | 15 retained TB-273 facts | controlled ports |

No real PSP, Wallet, Inventory, waybill, notification or money operation was performed in any fact. No test row was
committed, deleted, seeded or restored; relational rows lived only inside rolled-back caller transactions.
