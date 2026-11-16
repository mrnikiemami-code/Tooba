# TB-274 — receiving, inspection and approval-gate proof

Owning code: `Tooba.Returns.Domain/{Aggregates/ReturnReceipt.cs,Aggregates/ReturnReceiptLine.cs,ValueObjects/ReturnReceivingSummary.cs}`,
`Tooba.Returns.Infrastructure/Directories/ReturnDirectory.{Receipts.cs,Recovery.cs,CourierOrigin.cs}`, command
`RecordReturnReceiptCommand`, port `IReturnDirectory.RecordReceiptAsync`, endpoint `POST /v1/admin/returns/{id}/receipt`.
Focused suite: `Tooba.Returns.Tests/Behavior/ReturnsCustomerReceipts274Tests.cs` (**25/25**).

## 1. Recording is a pure physical statement

`A_recorded_receipt_is_the_physical_fact_and_moves_no_money_or_stock` — one occurrence (2 usable) creates exactly one
header + one line, `Received = 2`, `Usable = 2`, `Damaged = 0`, `IsComplete = true`, while gateway calls, Wallet calls,
Inventory restocks and the refund-attempt count all stay `0` and the status stays `Requested`.

## 2. Partial occurrences accumulate; over-receiving is refused

`Partial_occurrences_accumulate_without_over_receiving` — `1` then `1` of a requested `2` gives `Received = 2` in two
distinct occurrences; the second occurrence alone is not complete, the pair is. A third occurrence asking for `1` more is
`return.receipt.over_received` with the receipt/line counts unchanged.

Relational equivalent: `ReturnsCustomerReceipts274RelationalTests.A_receipt_rows_accumulate_cumulatively_and_never_exceed_the_requested_quantity`
(real schema, caller-owned transaction rolled back, `committedRows=0`).

## 3. A mixed item stays one requested line

`Mixed_usable_and_damaged_in_one_occurrence_stays_one_requested_line` — one submission with `2 usable` + `1 damaged` for
the same `ReturnItemId` produces two inspected lines of one receipt, `Received = 3`, `Usable = 2`, `Damaged = 1`, and the
case still has exactly one return item (TB-272's one-line rule untouched).
Relational: `A_mixed_conditions_are_stored_on_distinct_lines_of_the_same_requested_line`.

## 4. Exact replay and typed conflict

`An_exact_replay_answers_the_stored_receipt_and_a_changed_intent_is_a_conflict` — the same occurrence key with the same
(reordered) line set answers the stored receipt and writes nothing; the same key with a different quantity is
`return.receipt.conflict` and no second header/line is written.

## 5. Origin and state refusals

| Refusal | Test |
| --- | --- |
| courier-origin case never records a second receipt here (`return.receipt.origin_invalid`) | `A_courier_origin_case_never_records_a_second_receipt_here` |
| a case that already carries a physical disposition refuses a new receipt (`return.receipt.state_invalid`) | `A_decided_case_never_accepts_a_new_physical_receipt`, `A_settled_physical_history_never_accepts_a_new_receipt` |
| blank key / zero / negative / over-precision quantity / undefined condition / foreign item / duplicate (item, condition) / empty input are refused before any write | `Invalid_receipt_shapes_are_refused_before_any_write` |
| a stored incoherent receipt never authorizes approval or restock | `An_incoherent_stored_receipt_never_authorizes_approval_or_restock` |

## 6. Every ordinary money entry is gated by the same authority

| Path | Proof |
| --- | --- |
| approval with no receiving | `Approval_without_receiving_is_refused_with_zero_money_and_zero_attempt` — `returns.receiving.incomplete`, 0 gateway calls, 0 attempts, 0 decisions |
| partial receiving | `A_partial_receipt_does_not_authorize_approval` — refused until the coverage is exactly complete |
| complete receiving | `Approval_over_a_complete_receipt_restocks_only_the_inspected_usable_quantity` — unchanged amount/currency/destination, once-only attempt, restock == `Usable` only (2 of 3) |
| fully damaged | `A_fully_damaged_receipt_never_calls_saleable_inventory` — zero saleable calls, line becomes `DamagedNonSaleable` |
| definite-failure new attempt (`retry`) | `A_new_attempt_retry_is_gated_by_the_same_receiving_authority` |
| unknown ordinary money (`recover.resume_pending_refund`) | `Unknown_ordinary_money_is_never_resumed_without_a_complete_receipt` — read says `Blocked` + `return.recovery.receiving_required` with no kind; the action refuses with the same code and the SAME attempt id/key stays pending, 0 gateway calls; a real receipt then unblocks the SAME attempt (money succeeded → inspected usable restocked) |
| confirmed money, unsettled physical | `Real_receiving_may_unblock_a_money_complete_case_without_ever_repeating_its_money` — receipt recorded after a legacy money-only completion, physical intent restocks `2` usable with the gateway untouched and still one attempt |
| confirmed money, no receipt | `Physical_recovery_after_confirmed_money_restocks_only_usable_units` |

Relational: `B_approval_over_a_partial_receipt_is_refused_before_any_decision_attempt_or_money`.

## 7. Retained 273 invariants with real evidence

`ReturnsRecovery273Tests` keeps every invariant and its ordinary positive fixtures now carry a **real** recorded receipt
(`SeedOrdinaryCaseAsync` inserts the receipt rows the gates read, not a bypass flag):

- unknown money resumes the SAME attempt/key once; a repeated transport failure stays pending with one key;
- a definite failure is `FailedMoney`, not resumable, and never mints the retry attempt;
- a completed case answers its stored outcome; money success is never downgraded or duplicated;
- the read policy now describes ordinary pending money as `Blocked / receiving_required` when the receipt is absent, and
  as `PendingMoney` when it is complete;
- a money success over a complete receipt settles the inspected usable quantity (still no second money).

`ReturnsRecovery273RelationalTests` re-ran green (**3/3**) with `TB273R1_CONNECTION` set to the canonical target after this
change, confirming the transaction-refusal guard still precedes every ordinary gate.
