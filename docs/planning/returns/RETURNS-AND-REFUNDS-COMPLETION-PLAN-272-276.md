# CURRENT — 277-R2 ACCEPTED; REQUESTED277 REPAIR CHAIN CLOSED WITH PROOF LIMITS — 2026-11-17

Read docs/architecture/evidence/TB-RETURNS-MODAL-QUANTITY-GUARDS-AND-EXPLICIT-REFUND-SEPARATION-277-R2/ARCHITECT-REVIEW.md.
Source938ab6488ebdeec6cf2c97d3023b0bb085dd4b9d removes the last unsafe token; mounted regression proves unknown->metadata-refusal retains exact intent and authoritative replay clears it. Prior277-R1 A/B acceptance retained;277 modal/approval-vs-first-refund work retained. This requested repair chain is closed within reviewed scope, not universal Returns/provider/user acceptance.
No next implementation issued. activeTask: NONE; automaticNextImplementationTask: NONE. Await user's next feedback. Existing case is Settled read-only history; no new case/live mutation/reset/provider/money action authorized. Preserve standing user rules, all partial/audit history and proof limits.

# SUPERSEDED — 277-R2 WORKER PASS (one unsafe no-effect token removed; mounted unknown→lookup-refusal→exact-replay retention); AWAIT ARCHITECT REVIEW — 2026-11-17
Read docs/ai/tasks/TB-RETURNS-MODAL-QUANTITY-GUARDS-AND-EXPLICIT-REFUND-SEPARATION-277-R2/TASK.md and
docs/architecture/evidence/TB-RETURNS-MODAL-QUANTITY-GUARDS-AND-EXPLICIT-REFUND-SEPARATION-277-R2/{TASK.md,RESULT.md,RECEIPTS.md,result-contract.txt,retention-proof.md,user-review.md}.
**One tiny repair only**, base `eaab66e86`. `return.receipt.unit_quantity_invalid` is no longer a proven no-effect refusal
(the owning route also answers it when a separate unit-metadata lookup could not answer, so a refuted retry may not erase a
possibly-sent receipt); the set is 9 body-dictated tokens and its doc comment now says so. Mounted real workspace proves
transport-loss retention → retry refused with that code keeps the byte-identical envelope and the retained card → only the
200 replay of that same occurrence clears it; all sends byte-identical. Negative control: re-adding the token makes that
fact fail (`storedIntent()` null). 277-R1 A/B, the receipt write gate and all 277 modal/approval/first-refund work are
untouched; no backend edit, no schema, no fixture, no live POST, no new case. Mounted 9/9, queue 26/26, combined FE 48/48,
`tsc` 0, `next build` 0 (`BUILD_ID GptasB03cazPtYoJp7aiE`, Next PID 23760); Host unchanged (`5088` PID `21060`, dll
`8AA71DD8…E7948`, `/health` + `/health/ready` 200). Settled case `01a1e6b0-…` read-only.
STOP_FOR_ARCHITECT; automaticNextImplementationTask: NONE.

# SUPERSEDED — 277-R1 UNIT/DECIMAL REPAIR ACCEPTED; ONE UNSAFE REFUSAL TOKEN REMAINS; ONLY277-R2 — 2026-11-17
Read docs/architecture/evidence/TB-RETURNS-MODAL-QUANTITY-GUARDS-AND-EXPLICIT-REFUND-SEPARATION-277-R1/ARCHITECT-REVIEW.md.
A/B accepted within bounded source-review scope. C still PARTIAL: unit_quantity_invalid can mean unavailable owning metadata and must not erase an earlier possibly-sent receipt.
Only TB-RETURNS-MODAL-QUANTITY-GUARDS-AND-EXPLICIT-REFUND-SEPARATION-277-R2 next, FE classifier + one mounted regression. Preserve standing user rules, source/data/history,277 approval split. No live POST/new case/backend/schema change/screenshots/delegation/loop.
STOP_FOR_ARCHITECT; automaticNextImplementationTask: NONE.

# SUPERSEDED — 277-R1 WORKER PASS (owning unit enforced at the receipt write, exact millionth client quantities, body-only no-effect refusals); AWAIT ARCHITECT REVIEW AND USER LOOK — 2026-11-17

Read docs/ai/tasks/TB-RETURNS-MODAL-QUANTITY-GUARDS-AND-EXPLICIT-REFUND-SEPARATION-277-R1/TASK.md and
docs/architecture/evidence/TB-RETURNS-MODAL-QUANTITY-GUARDS-AND-EXPLICIT-REFUND-SEPARATION-277-R1/{RESULT.md,RECEIPTS.md,result-contract.txt,precision-and-uncertainty-proof.md,user-review.md}.
277-R1 PASS within the issued A–C repair scope; 277 stays reviewed-PARTIAL with its DS modals, decision-only approval and
explicit first-refund route retained. Base `42e2df1f328e16b1bd855cc57edde168071a600f`. The receipt write now validates a
NEW quantity against the owning unit (`return.receipt.unit_quantity_invalid`, zero writes, exact replay first), the dialog
uses exact integer millionths for the field bound, the shared usable+damaged ceiling and the confirm gate, and the
proven-no-effect refusal set keeps only body-dictated codes. No schema change, no new case, no live business write. The
exercised case `01a1e6b0-f33a-7000-80ee-beffb6344490` is READ-ONLY. One worker/task, no screenshots/delegation/loop.
STOP_FOR_ARCHITECT; automaticNextImplementationTask: NONE.

# SUPERSEDED — 277 WORKER PASS (shared action modals + unit-bounded receiving + decision-only approval with an explicit first refund); AWAIT USER LOOK AND ARCHITECT REVIEW — 2026-11-17

Read docs/architecture/evidence/TB-RETURNS-MODAL-QUANTITY-GUARDS-AND-EXPLICIT-REFUND-SEPARATION-277/{RESULT.md,RECEIPTS.md,result-contract.txt,modal-and-quantity-proof.md,approval-vs-refund-proof.md,user-review.md}.
277 PASS within the issued A–F scope, awaiting Architect review (NOT Architect ACCEPT, NOT user acceptance). 276/275/274/273/272-R1
and shipping acceptances preserved. Expected-Base-SHA == Base `04aa77a5df260b3e76a115d607080a8e6c8ed004`.

Behavior now: every Returns action review opens ONE shared design-system `Dialog` (no inline forms); receipt quantity fields
show product + localized unit + «حداکثر قابل دریافت», are bounded by the owning unit precision (`QuantityDecimalPlacesSnapshot`,
capped 6, null when unanswered) and one `requested − received` ceiling shared by usable+damaged; success uses the shared
`ToastRegion`, command refusals the shared `MessageBox`, field problems in-dialog; `ApproveAsync` is DECISION ONLY (no attempt,
no gateway/Wallet, no restock) and the FIRST refund is its own `start_refund` command/route with a deterministic case-derived
attempt id/key persisted before the external call. `AwaitingFirstRefund` is appended as phase 6; money-complete with undisposed
goods is still `Disposition`, never settled. No migration.

Proof: backend 61/61 focused; mounted real workspace 8/8; `tsc` 0; `next build` 0 (`BUILD_ID VAx5DNtd8dMTFmyI5e2OR`);
backend build 0; Host publish 0. Host 5088 PID 49764 (dll SHA256 `64C74398…2F9EB0`, healthy), Next 3000 PID 7020.
The exercised case `01a1e6b0-f33a-7000-80ee-beffb6344490` is untouched (`Completed`, `Approve`, 1 receipt, 1 Succeeded attempt at
`00:31:26Z`, disposition `SaleableRestock`, `Settled`); counts/`updated_at` unchanged and no returns event after that instant.
Primary review URL: http://127.0.0.1:3000/fa/admin/returns/01a1e6b0-f33a-7000-80ee-beffb6344490 (settled history → no action is
offered; the modal workflow is proven by the mounted real component, not by posting on the case).
Limits: controlled ports only (no real PSP/Wallet/Inventory), no committed two-writer race, jsdom mount + DOM/text live check,
no served modal opened on the settled case, other units inherit owning metadata without individual exercise.
STOP_FOR_ARCHITECT; automaticNextImplementationTask: NONE.

# SUPERSEDED — USER REVIEW REQUIRES MODALS, BOUNDED UNIT QUANTITIES AND SEPARATE REFUND; ONLY277 — 2026-11-17

Only TB-RETURNS-MODAL-QUANTITY-GUARDS-AND-EXPLICIT-REFUND-SEPARATION-277 is authorized next. Read276/ARCHITECT-REVIEW.md and standing user rules in AGENTS.
Source base be47ee81ae0bf2b7e2678752741837ca60af0b70. User exercised the review case: receipt and approval, with a recorded refund success. Its latest live state MUST be read; the earlier Requested/empty-history report is obsolete. Do not reset/clone/reverse the case or perform further live receipt/decision/money/stock writes.
276 is not final user acceptance. Existing foundations retained; original worker/audit reports preserved.277 owns shared modal/notification UX, unit-aware remaining quantity constraints and a real decision-vs-first-refund command split. One worker/task, no screenshots/delegation/loops, no automatic next task.

# SUPERSEDED — 276 WORKER PASS (connected durable review + reviewed retry replay + ONE live review case); AWAIT USER TEST AND ARCHITECT REVIEW — 2026-11-17

Read docs/ai/tasks/TB-RETURNS-CONNECTED-WORKFLOW-REPAIR-AND-USER-REVIEW-276/TASK.md and
docs/architecture/evidence/TB-RETURNS-CONNECTED-WORKFLOW-REPAIR-AND-USER-REVIEW-276/{RESULT.md,RECEIPTS.md,result-contract.txt,connected-intent-and-retry-proof.md,user-review-case-proof.md,user-review.md}.
276 PASS within the issued A–F connected-repair + one-user-review-case scope, awaiting Architect review (NOT Architect
ACCEPT, NOT user acceptance). 275 stays PARTIAL as the parent; 272-R1/273-R2/274-R1 and shipping acceptances preserved.
Connected repair: the reviewed envelope now freezes the target case + owning checkout/route + action + occurrence + the
EXACT transport body, is written and READ BACK before the first possible send, refuses to send on storage unavailability,
treats unknown/5xx/conflict as unresolved, releases only on an explicit `PROVEN_NO_EFFECT_REFUSAL_CODES` token, clears
compare-and-clear on the exact bytes, and fences every mutation/read/`finally` with a presentation epoch. The reviewed
failed-refund retry requires a nonempty `ExpectedFailedAttemptId`, locates its deterministic child BEFORE the
`RefundFailed`/current-leaf gates, replays a stored child (Succeeded/Failed) from storage, reconciles a stored Pending child
on the same key, and only then may mint after the effectful gates.
Proof: mounted `AdminReturnWorkspace` 10/10; `ReturnsOperatorWorkflow275Tests` 15/15 through real `ReturnDirectory`;
queue/mapping/labels 26/26; error map 8/8; `tsc` 0; `next build` 0 (`BUILD_ID gC-lsQo8rzo30IeWT6MwF`); backend build 0;
Host publish 0; AMSC guard 34/34. Host 5088 PID 8572 (dll SHA256 `D998ECBC…32838E`, healthy), Next 3000 PID 21544.
ONE lawful live review case exists: `01a1e6b0-f33a-7000-80ee-beffb6344490` / `RET-TB-20261114175151-01-7d5f59-1`,
`CustomerDelivered`, `Requested`, 1 × `pcs`, 2,407,839 IRR, zero receipts/decisions/attempts/dispositions, source order
still `Paid`, one normal `return.requested.v1` event (two older leftover rows disclosed). Primary review URL:
http://127.0.0.1:3000/fa/admin/returns/01a1e6b0-f33a-7000-80ee-beffb6344490
No schema change, no provider/money/stock/notification effect, no demo order or fake receipt. User has NOT accepted yet.
STOP_FOR_ARCHITECT; automaticNextImplementationTask: NONE.

# SUPERSEDED — 275 PARTIAL AFTER SOURCE REVIEW; ONLY276 CONNECTED REPAIR + ONE USER REVIEW CASE — 2026-11-16

Read docs/architecture/evidence/TB-RETURNS-OPERATOR-WORKFLOW-QUEUE-DETAIL-AND-MOBILE-275/ARCHITECT-REVIEW.md.
275 source review found unresolved intent cleared on failure/null, missing verified durable body and exact compare-clear, mutation lifecycle fencing gaps, and retry child replay ordered after live-state/current-leaf gates. The 21 FE node tests do not mount the workspace.
Only TB-RETURNS-CONNECTED-WORKFLOW-REPAIR-AND-USER-REVIEW-276 is authorized next; no parallel task. It repairs the connected workflow and prepares ONE lawful ordinary Requested return via the owning API for user review. User explicitly authorized this one case alongside progress; prior no-fixture blanket does not forbid that case. No approval/rejection/receipt/refund/recovery/stock/provider effect or fabricated Paid/Delivered fact. No schema change.
No user/financial/full-Returns acceptance. Preserve prior acceptance/audit history. ONE tooba_alpha5432/store-alpha, Host5088/FE3000; no screenshots, delegation or automatic continuation.
STOP_FOR_ARCHITECT; automaticNextImplementationTask: NONE.

# Returns and refunds — Architect execution plan — 2026-11-16

State: APPROVED_FOR_SEQUENTIAL_IMPLEMENTATION_BY_USER.

## Superseded checkpoint — 2026-11-16
274-R1 ACCEPT within inspection/receipt-integrity/stock/decision repair scope; read274-R1/ARCHITECT-REVIEW.md for exact evidence and retained limits. Only TB-RETURNS-OPERATOR-WORKFLOW-QUEUE-DETAIL-AND-MOBILE-275 next;276 NOT ISSUED.
275 carries operator integration, one server work/read description + queue/count/detail, durable reviewed UI actions, legacy partial-receipt continuation and reviewed failed-attempt retry guard. No migration/provider/live business fixtures. Original partial/audit/validation history remains unchanged.

## Superseded274-R1 worker checkpoint — 2026-11-16
274-R1 worker result: PASS within the issued A–E repair scope, awaiting Architect review (NOT Architect/user acceptance). Read docs/architecture/evidence/TB-RETURNS-CUSTOMER-RECEIPT-INSPECTION-AND-REFUND-GATES-274-R1/RESULT.md. The parent **274 stays PARTIAL** until that review; its evidence and ARCHITECT-REVIEW.md are unchanged.
Closed: (A) inspection is explicit at every reachable entry — null/blank/unknown refuses with `return.receipt.line_invalid` before any case read, and the optional `Usable` default is gone from wire/typed/Contracts shapes; (B) the one `ReturnReceivingSummary` rejects non-positive/over-precise/undefined/duplicate/borrowed/over-covering stored facts and the write refuses a NEW occurrence on incoherent evidence with `return.recovery.evidence_conflict` + 0 rows, owning seller scope is enforced inside the directory, and additive migration `20261116140705_BoundedReceiptIntegrityConstraints` adds the `quantity > 0` / `condition IN ('Usable','Damaged')` CHECKs plus local composite ownership FKs; (C) an inspected usable quantity with no owning stock binding is a typed PENDING blocker (`return.recovery.stock_binding_required`, `Disposition = None`, 0 Inventory calls, money untouched) instead of a false completion, a mixed line publishes its exact restocked usable portion, and a rejected/cancelled case with undisposed received goods reports `return.recovery.physical_resolution_required`; (D) the decision collision is arbitrated only by `23505` on `pk_return_request_decisions` in `returns.return_request_decisions` with a savepoint + scoped detach (no global `ChangeTracker.Clear`) and one shared winner/reason comparison.
Fresh pre-apply `pg_dump` (2,979,578 B, SHA256 `31D47A05…527A52`, verified readable before apply) and ONE runner apply (`pendingMigrations=[]`, post-apply `pendingCount=0`); both receipt tables and the decision table are still empty. `ReturnsCustomerReceipts274R1Tests` 14/14, parent `ReturnsCustomerReceipts274Tests` 25/25, R1 relational 4/4 rollback-only, build/publish 0 errors, Host-only refresh healthy, FE untouched. **No live return case exists**, so no served receiving walkthrough; money/stock are controlled-port proof and the relational facts are rollback-only.
**275 (operator receiving/inspection UI + mobile) and 276 (connected acceptance) remain candidates, NOT ISSUED.**

## Superseded 274-PARTIAL checkpoint — 2026-11-16
274 Architect decision PARTIAL; only274-R1 receipt-integrity/stock/decision repair next. Read274/ARCHITECT-REVIEW.md.275/276 NOT ISSUED. Retain applied274 migration, require fresh pre-backup for new additive constraints. Preserve272-R1/273-R2 acceptance and audit history.

## Superseded274 worker checkpoint — 2026-11-16
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
