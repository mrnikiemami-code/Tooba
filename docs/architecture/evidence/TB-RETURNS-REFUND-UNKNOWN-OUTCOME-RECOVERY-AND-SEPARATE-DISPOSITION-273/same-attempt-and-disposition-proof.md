# TB-273 — same-attempt money recovery and separate physical disposition proof

All facts live in `src/backend/Modules/Returns/Tooba.Returns.Tests/Behavior/ReturnsRecovery273Tests.cs` and run through
the real owning boundary (`ReturnDirectory.RecoverAsync` → gateway/Wallet/Inventory ports → `returns.*` state), with
controlled task-owned ports. **15/15 pass.**

Fixtures are built from stored approved facts (`ReturnRequest.Create` + `Approve` + `MarkRefundProcessing` +
`BeginRefundAttempt`), never from a live refund. Identities used by every fact:

| Identity | Value |
| --- | --- |
| attempt idempotency key | `refund-{returnRequestId:N}` (stored, never regenerated) |
| Wallet ledger key | `wallet-refund-credit:{returnRequestId:D}` (case key, once-only) |
| per-item restock key | `return-restock-{returnRequestId:N}-{returnItemId:N}` (existing identity, reused per retry) |
| request occurrence key | a fresh `Guid` per command call (the intent does not dedupe on the request side) |

## 1. Required refusal matrix — no effect

| Fact | Seeded shape | Intent | Refusal | Effect |
| --- | --- | --- | --- | --- |
| `A_wrong_or_missing_attempt_identity_is_refused_before_any_money_call` | unknown money | resume, wrong id / no id | `return.recovery.attempt_mismatch` | 0 gateway calls, attempt still `Pending`, case still `RefundProcessing` |
| `A_requested_case_can_never_gain_a_recovery_action` | `Requested` | resume / disposition | `attempt_mismatch` / `state_mismatch` | 0 attempts, phase `None`, 0 gateway, 0 restock |
| `A_rejected_case_can_never_gain_a_recovery_action` | `Rejected` | resume / disposition | `attempt_mismatch` / `state_mismatch` | phase `None`, 0 gateway, 0 restock |
| `A_closed_use_case_guard_refuses_recovery_before_any_effect` | unknown money + closed guard | resume / disposition | `platform.use_case.closed` | 0 gateway, 0 Wallet, 0 restock, attempt still `Pending` |
| `The_read_policy_separates_money_from_physical_completion` | two pending attempts | read | phase `Blocked` + `return.recovery.attempt_unbound` | pure read, no action offered |

A `Cancelled` case is covered by the same policy row as `Requested`/`Rejected` (`Phase=None`, no kind) and is refused by
the same two guards (`recovery.attempt_mismatch` for a money intent, `recovery.state_mismatch` for a physical intent),
because no approved attempt can exist for it.

## 2. Same-attempt replay (unknown money)

`An_unknown_answer_is_recovered_with_the_same_attempt_and_key`
- gateway succeeds once → exactly **one** durable attempt remains, same `RefundAttemptId`, same `IdempotencyKey`, same
  `PaymentId`, same `Amount`; status `Succeeded`, provider reference recorded; case `Completed`.
- the provider observed exactly **one** call carrying the recorded key/payment/amount; the Wallet was never touched.
- physical completion is **false** and the item is still `Disposition=None` — money success is not stock settlement.
- repeating the same intent afterwards answers from the row: still one gateway call, one attempt, same reference.

`A_repeated_transport_failure_stays_pending_without_minting_a_second_attempt`
- two timeouts → still one `Pending` attempt, `ProviderReference` null, `CompletedAt` null, case `RefundProcessing`,
  phase `PendingMoney` bound to the same attempt; two gateway calls, **one** distinct key, one attempt row.

`A_definite_failure_is_not_resumable_and_never_mints_the_new_attempt_retry`
- `RefundFailed` + `Failed` attempt → phase `FailedMoney`; a resume answers the stored row with **zero** money calls and
  keeps the original attempt/key/failure code; a disposition intent is refused with `state_mismatch` (stock may not
  repair a failed refund).

`Nothing_is_held_or_written_while_the_money_answer_is_outstanding`
- an independent `DbContext` probes the durable attempt at the exact instant of the gateway call: no transaction is
  open on the owning context and the durable row is still `Pending` (nothing is written before the answer).
- same probe for the Wallet path.

## 3. Unconfigured adapter is a truthful unavailable

`An_explicitly_unconfigured_adapter_is_a_typed_unavailable_that_keeps_the_pending_attempt`
- the port throws `ContractOperationException` with the exact owning code `payment.refund.gateway.unconfigured`
  → `return.recovery.gateway_unavailable`; the attempt stays `Pending` with no provider reference and no failure code,
  the case stays `RefundProcessing`, the phase stays `PendingMoney` on the same attempt.
- once the adapter answers, the **same** attempt/key settles exactly once (one attempt row).

Classification is by typed code only. Any other exception is `Unresolved` (see §2) and is never promoted to a terminal
refusal, and no `result == false` is invented.

## 4. Wallet once-only

`Wallet_destination_credits_once_with_the_case_key_and_never_calls_the_psp`
- an unknown Wallet answer keeps the attempt `Pending` and the case `RefundProcessing`.
- the successful recovery calls the Wallet with the **case** key `wallet-refund-credit:{caseId}` and
  `ReturnRequestId = caseId`; two calls, **one** distinct key, **zero** gateway calls, one attempt, case `Completed`.
- `OriginalPayment` cases never reach the Wallet (the destination is read from the stored case, not chosen by the
  caller or by a fallback).

## 5. Physical disposition is a separate intent

`Confirmed_money_with_a_failed_restock_recovers_stock_only_with_the_same_item_key`
- a courier-origin case with confirmed money; the owning restock refuses once → the usable line stays
  `Disposition=None`, phase stays `PendingDisposition`, nothing is marked before Inventory confirms.
- the retry is disposition-only: **0** gateway calls, **0** Wallet calls, **1** attempt (unchanged), and exactly one
  restock carrying `return-restock-{caseId:N}-{itemId:N}`.
- the damaged line ends `DamagedNonSaleable` and the usable line is disposed; the case reaches `Settled` and the
  existing closure phase `RefundReleased`.
- a second identical intent is answered from the stored completion: no extra restock, no money, still one attempt.

`An_ordinary_case_without_receiving_evidence_is_blocked_with_zero_restock_and_money_kept`
- `CustomerDelivered` + confirmed money + unfinished disposition → read blocker `return.recovery.receiving_required`
  with `FinancialComplete=true`, `PhysicalComplete=false`.
- the intent is refused with the same code; **0** restocks, **0** gateway calls, **1** attempt, the item stays
  `Disposition=None`, the case stays `Completed` with the money confirmed. No receipt, inspection or default-`Usable`
  saleable restock is invented.

`Confirmed_money_with_unsettled_receipts_is_described_by_origin_not_by_status` (read policy) proves the same stored
shape maps to `PendingDisposition` for a courier receipt and to `Blocked/receiving_required` for an ordinary RMA.

## 6. Late / concurrent / lost answers

`A_local_save_failure_after_an_external_success_is_never_answered_as_durable`
- the gateway succeeds but the local `SaveChanges` throws (EF `SaveChangesInterceptor`): the caller receives the
  failure, **not** a durable success.
- an independent context reads the durable row: still `Pending`, no provider reference — the external answer did not
  become money.
- the next call (fresh request scope) re-reads the **durable** attempt and reconciles it: still exactly one attempt,
  the same key, two gateway calls with **one** distinct key, and the provider reference recorded once.

Monotonic application (`RecordRefundOutcomeAsync`) is what makes a late answer safe: it reloads the attempt and the
case under a short owning transaction, applies only from `Pending`, and returns `false` for a stale answer, so a stale
success/failure can neither downgrade a terminal row nor raise a second outcome event.

## 7. What is explicitly NOT proven here

- No real PSP/Wallet/Inventory operation, no real money, no real stock movement.
- No "exactly one HTTP provider call" claim: two same-key concurrent recoveries may each call the owning gateway; the
  Claim's same-key idempotency is what must fold them, and this suite proves only the durable-outcome and identity
  properties.
- Concurrency is a two-context/re-read proof, not a stress harness, and no committed two-writer race exists.
- The `SELECT … FOR UPDATE` branch runs only on a relational provider; these facts run on the in-memory provider (the
  running Host exercises the relational path, without an automated rollback-only fact).
- No live case exists in the canonical database, so there is no served walkthrough of a real recovery.
