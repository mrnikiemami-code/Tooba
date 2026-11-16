# Returns and refunds — Architect execution plan — 2026-11-16

State: APPROVED_FOR_SEQUENTIAL_IMPLEMENTATION_BY_USER.

## CURRENT checkpoint — 2026-11-16 (274 worker pass)
274 worker result: PASS within the issued A–E backend scope, awaiting Architect review (NOT Architect/user acceptance).
Read docs/architecture/evidence/TB-RETURNS-CUSTOMER-RECEIPT-INSPECTION-AND-REFUND-GATES-274/RESULT.md.
Returns now owns the ordinary customer RECEIVING/INSPECTION facts (`POST /v1/admin/returns/{id}/receipt`, additive
`customer_return_receipts` + `customer_return_receipt_lines`) and `ReturnReceivingSummary` is the single authority that
gates approval, refund retry, pending-money recovery and physical-disposition recovery; saleable restock is only the
inspected `Usable` quantity after confirmed money, `Damaged` never touches saleable stock, and a new rejection persists a
meaningful reason with its winning decision (`return.decision.reason_conflict` on a changed replay). One additive migration
applied once by the repository runner after a verified pre-apply `pg_dump`. `ReturnsCustomerReceipts274Tests` 25/25, whole
Returns suite 93/93, relational 6/6 with `committedRows=0`, build/publish 0 errors, FE untouched.
**275 (operator receiving/inspection UI + mobile) and 276 (connected acceptance) remain candidates, NOT ISSUED.** No live
return case exists, so no served operator walkthrough; no demo case/order/hold was created; money/stock proven with
controlled ports and rollback-only relational facts.

## Superseded 273-R2-ACCEPT checkpoint — 2026-11-16
273-R2 ACCEPT within three-gate source/evidence scope;273 backend recovery foundation closed through repairs. Read273-R2/ARCHITECT-REVIEW.md. Original273/R1 PARTIAL results/reviews and all deviations remain history.
ONLY TB-RETURNS-CUSTOMER-RECEIPT-INSPECTION-AND-REFUND-GATES-274 is next: Returns-owned ordinary customer receiving/inspection facts, exact received quantities/conditions, all ordinary refund/restock entry paths gated, persisted rejection reason. Backend-only; UI275/connected276 NOT ISSUED. Additive migration requires actual pre-apply backup and repository runner. No live business fixtures/money/stock actions.

## Superseded273-R2 worker checkpoint — 2026-11-16
273-R2 worker result: PASS within the issued three-gate repair scope, awaiting Architect review (NOT Architect/user
acceptance). Read docs/architecture/evidence/TB-RETURNS-REFUND-UNKNOWN-OUTCOME-RECOVERY-AND-SEPARATE-DISPOSITION-273-R2/RESULT.md.
The parent **273 and 273-R1 remain PARTIAL**. 273-R2 closed the three accepted omissions: the SAME courier
source/condition coherence rule runs before `PendingMoney` is offered (typed evidence conflict, no kind, zero calls);
`RefundReleased` without `ObligationClosedAt`/`StockSettledAt` is an evidence conflict, never `Settled`; the single
transaction guard also reads EF's `RelationalConnection.EnlistedTransaction`. `ReturnsRecovery273Tests` 29/29 and ONE
new rollback-only explicit-enlistment fact 1/1 on real `tooba_alpha` (`committedRows=0`); schema/FE untouched.
**274–276 remain candidates, NOT ISSUED** (274 = customer receipt/inspection evidence, 275 = operator UI, 276 =
connected acceptance). The live Returns table is empty, so no served recovery walkthrough exists.

## Superseded273-R1 checkpoint — 2026-11-16
273-R1 Architect decision: PARTIAL, only273-R2 three-gate correction is next. Read273-R1/ARCHITECT-REVIEW.md. Source coherence before pending money, prior closure timestamps before settled, explicit enlisted transaction detection.274–276 NOT ISSUED. No scope expansion or proof loop. Preserve272-R1 acceptance/audit.

## Superseded273-R1 worker checkpoint — 2026-11-16
273-R1 worker result: PASS within the issued A–D repair scope, awaiting Architect review (NOT Architect/user acceptance).
Parent **273 remains PARTIAL** until the Architect reviews this repair; its evidence and `ARCHITECT-REVIEW.md` are unchanged.
Read docs/architecture/evidence/TB-RETURNS-REFUND-UNKNOWN-OUTCOME-RECOVERY-AND-SEPARATE-DISPOSITION-273-R1/RESULT.md.
273-R1 repaired: one fail-closed stored-evidence policy (recorded APPROVE required, fully bound attempt, coherent courier
source) shared by read and action; all-items-disposed + `StockSettled` is explicitly `PendingDisposition` whose zero-money
intent writes only the final marker; `return.recovery.transaction_owned` refuses a caller/ambient transaction before any
effect; a null/blank PSP reference is stored truthfully. `ReturnsRecovery273Tests` 24/24,
`ReturnsRecovery273RelationalTests` 2/2 rollback-only on real `tooba_alpha` (`committedRows=0`). Schema untouched.
**274–276 remain candidates, NOT ISSUED** (274 = customer receipt/inspection evidence, 275 = operator UI, 276 = connected
acceptance). The live Returns table is empty, so no served walkthrough of a real recovery exists.
272-R1 ACCEPT within A–C source/evidence repair scope; parent272 creation work closed through repair.

## Superseded273 checkpoint — 2026-11-16
273 Architect decision: PARTIAL, NOT ACCEPTED. Read273/ARCHITECT-REVIEW.md. Only273-R1 bounded evidence/binding/closure, transaction boundary and truthful reference repair is next.274–276 NOT ISSUED. Preserve272-R1 acceptance and prior audit.

## Superseded273 worker checkpoint — 2026-11-16
273 = PASS within the issued backend A–D scope, awaiting Architect review (NOT Architect/user acceptance).
Read docs/architecture/evidence/TB-RETURNS-REFUND-UNKNOWN-OUTCOME-RECOVERY-AND-SEPARATE-DISPOSITION-273/RESULT.md.
273 delivered: one pure stored-fact recovery description reused by read and action; `POST /v1/admin/returns/{id}/recover`
with `resume_pending_refund` repeating the EXISTING pending attempt (same key/payment/amount/currency, fixed destination)
and `settle_physical_disposition` as a separate zero-money intent; truthful `return.recovery.*` blockers including
`gateway_unavailable` and `receiving_required`; no transaction/row lock across an external call. Schema untouched.
274–276 remain candidates, **NOT ISSUED** (274 = customer receipt/inspection evidence, 275 = operator UI, 276 = connected
acceptance). The live Returns table is empty, so no served walkthrough of a real recovery exists.
272-R1 ACCEPT within A–C source/evidence repair scope; parent272 creation work closed through repair.
Read 272-R1/ARCHITECT-REVIEW.md. The original272 PARTIAL review and unauthorized committed fixture/delete deviation remain history. Limits: no committed two-writer/owned-success relational proof, no live return walkthrough or Architect runtime execution.

## Superseded worker pointers (retain as history)

272-R1 repair result: PASS within its issued A–C scope (2026-11-16), awaiting Architect review; evidence in
`docs/architecture/evidence/TB-RETURNS-CREATE-OWNERSHIP-EXACT-REPLAY-AND-QUANTITY-INTEGRITY-272-R1/`. 272 remains
**PARTIAL** as the parent until that review; only the precision/full-binding, caller-transaction/tracker and
rollback-only-harness seams changed. 273–276 remain candidates, NOT ISSUED.
272 Architect review: PARTIAL; only272-R1 bounded repair is next. Read 272/ARCHITECT-REVIEW.md. The original worker PASS remains history, not acceptance. Committed relational fixtures/deletes were outside the rollback-only authorization and must not recur. Source baseline: c492443179b86f3efc065121b3ae2a140105937d.
272 worker result: PASS within its issued A–D scope (2026-11-16) — awaiting Architect review; evidence in
`docs/architecture/evidence/TB-RETURNS-CREATE-OWNERSHIP-EXACT-REPLAY-AND-QUANTITY-INTEGRITY-272/`. 273–276 remain candidates, NOT ISSUED.
User instruction: plan, record in Architect context, then start. One downloadable task at a time; review result and source before issuing the next. No screenshot deliverables, delegation, idle polling or automatic implementation loop.

## Goal and completion boundary
Complete the existing /admin/returns workflow with truthful physical, financial and operator states. Reuse Returns ownership, existing endpoints/CQRS and Contracts to Order, Fulfillment, Inventory, Payment and Wallet. A successful refund and settled physical disposition are distinct facts. Customer-return and courier-origin paths retain their different physical evidence. No blanket shipping completion or user acceptance follows from worker PASS.

## Source findings (read at baseline; no Architect runtime/test execution)
- ReturnDirectory.CreateInternalAsync returns a global key match before checking submitted order ownership, with no exact-intent comparison.
- Its item loop checks each duplicate line separately; the eligibility evaluator omits RefundFailed from occupied quantities.
- Customer approval starts money and eventual usable restock without an explicit customer receipt/inspection record.
- Unknown gateway exceptions retain RefundProcessing; ordinary admin recovery actions only cover RefundFailed. Completed can precede a failed physical disposition.
- RejectAsync discards Reason. Queue summarizes first product/unit with total quantities and repeats RET-orderNumber for multiple cases.
- PaymentModule registers FakePaymentRefundGateway outside Production, FailClosedPaymentRefundGateway in Production. No real PSP refund integration was established by this audit.

## Ordered work packages — current issue authority is the CURRENT checkpoint above
| Candidate task | Coherent outcome | Review boundary |
| --- | --- | --- |
| TB-RETURNS-CREATE-OWNERSHIP-EXACT-REPLAY-AND-QUANTITY-INTEGRITY-272 | Authorized exact replay, duplicate-line refusal, failed-refund quantities retained, atomic ordinary creation quantity enforcement | No money/physical lifecycle/FE change; command and relational evidence |
| 273 — backend foundation closed through273-R2 | Explicit same-attempt unknown-outcome reconciliation; definite failure distinguished from unknown; independent retry of physical disposition without refund again | No invented PSP outcome, no blind new refund key; preserve approve/reject arbitration and Wallet once-only credit |
| 274 — candidate, NOT ISSUED | Customer receipt/inspection evidence and lawful quantities/conditions before ordinary refund/restock; persisted decision reason | Keep courier receipt and claims authoritative, no duplicate receipt authority; concrete additive migration reviewed with pre-apply backup if needed |
| 275 — candidate, NOT ISSUED | Existing admin queue/detail/workflow: distinct case locator, accurate multiple items/units, amount/currency/destination/origin, actionable phase/blocker, counts and reason/recovery controls, Persian dates and mobile readability | One action owner, shared grid/components, 390/1440 DOM/text proof; durable reviewed intent and late-response fencing |
| 276 — candidate, NOT ISSUED | Connected focused acceptance of customer + courier + Wallet + original-payment outcomes and recovery, actual served artifacts and residual limits | Existing data GET only; controlled tests for missing live cases; no manufactured orders/refunds |

Candidate numbers describe sequence only and are not execution authorization. Reconcile dependencies and source after each result; do not create an R1/R2 loop speculatively.

## Product and financial decisions
- An ordinary physical return requires recorded actual receiving and inspected usable/damaged quantities before refund/restock. Any refund-without-return exception needs an explicit distinct policy, not inferred approval.
- Approval/rejection must record actor, time and reason as appropriate; approval is not a physical receipt.
- Unknown money outcome remains unknown and resumes the same durable attempt. Confirmed money is never sent again to repair stock.
- Existing configured refund destination must remain explicit; no silent OriginalPayment/Wallet substitution.
- Real original-payment integration is a separate completion dependency: inspect the configured provider's documented capabilities and owning Payment contracts before proposing implementation. Do not guess credentials, synthesize success or claim original-payment production readiness. Lack of integration does not block the bounded integrity repair.
- Final completion report must distinguish implemented workflow, controlled proofs, served read-only coverage, real-provider readiness and user acceptance.

## Task discipline and environment
One worker/task; STOP_FOR_ARCHITECT; automaticNextImplementationTask: NONE. Worker PASS != Architect ACCEPT.
ONE canonical tooba_alpha 127.0.0.1:5432 SingleStore store-alpha; Host5088/FE3000. No second database, Docker or 15432/15433, no reset/reseed/backfill/cleanup, no provider/waybill/notification/money calls or live user business writes. No shipping draft or completed history mutations.
Focused tests only; no broad backend/parity recertification. Do not claim concurrency from sequential uniqueness tests.
Schema changes require separately scoped reviewed migration, actual pre-apply backup, plan and repository MigrationRunner; none authorized for 272.
Preserve accepted shipping271-R1 and270-R4, historical PARTIAL reviews and R2 missing-pre-backup audit. No reopening shipping work.
