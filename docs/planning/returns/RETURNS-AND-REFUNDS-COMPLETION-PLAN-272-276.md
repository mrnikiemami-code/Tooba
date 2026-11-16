# Returns and refunds — Architect execution plan — 2026-11-16

State: APPROVED_FOR_SEQUENTIAL_IMPLEMENTATION_BY_USER.
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

## Ordered work packages — only 272 is issued next
| Candidate task | Coherent outcome | Review boundary |
| --- | --- | --- |
| TB-RETURNS-CREATE-OWNERSHIP-EXACT-REPLAY-AND-QUANTITY-INTEGRITY-272 | Authorized exact replay, duplicate-line refusal, failed-refund quantities retained, atomic ordinary creation quantity enforcement | No money/physical lifecycle/FE change; command and relational evidence |
| 273 — candidate, NOT ISSUED | Explicit same-attempt unknown-outcome reconciliation; definite failure distinguished from unknown; independent retry of physical disposition without refund again | No invented PSP outcome, no blind new refund key; preserve approve/reject arbitration and Wallet once-only credit |
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
