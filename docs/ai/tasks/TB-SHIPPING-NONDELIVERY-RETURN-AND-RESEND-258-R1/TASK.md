PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-NONDELIVERY-RETURN-AND-RESEND-258-R1
Parent-Task: TB-SHIPPING-NONDELIVERY-RETURN-AND-RESEND-258
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: Tooba Shipping — complete safe physical return and resend
Mode: REPAIR_RESEND_INTENT_REPLAY_SOURCE_CAPACITY_AND_MIXED_CONDITION_RECEIPT
Expected-Base-SHA: 00917021d0d63c8e6f5f5c8992eb1a40e15a1601
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

## 1. Position / preflight
Read current TOPs, SHIPPING-OPERATIONAL-CLOSURE-PLAN-256-260.md, 258/ARCHITECT-REVIEW.md and mandatory repository backend/frontend feature skills, Contracts and design-system conventions.
Worker258 PASS is PARTIAL after independent source review. Preserve its lawful transitions and existing proofs. Only repair the four concrete gaps below, not the full module.
Safe current tree and origin parity; issue/review/recovery documentation descendants of Expected-Base are legitimate. Preserve unrelated work; no force/amend/reset/stash/rebase.
Save256-R2 CLOSED; issues1/2 accepted257; issue3 remains open here;259/260 NOT ISSUED. One worker/task, then STOP_FOR_ARCHITECT. User wants sequential continuation and final handoff after five defects, not an acceptance prompt now.

## 2. A — exact prebound resend intent, owning replay before capacity gate
Source defect: FulfillmentDirectory.Returns.cs CreateResendShipmentAsync returns any existing resend with the dispatch key before comparing the requested source and line quantities. Later aggregate validation cannot protect this return. Blank keys are permitted.
Require a trustworthy non-empty occurrence and a complete immutable reviewed intent BEFORE allocation. Bind the owning fulfillment/order scope, exact source parcel, accepted group/method and canonical ordered line quantities; actor/owner authorization remains per request and existing durable audit must identify the operator. Use existing owning durable records/patterns, not a new generic idempotency framework.
- Same occurrence + exact same intent returns its exact recorded parcel with zero new allocation, including after quantities are consumed.
- Same occurrence + changed source, group/method or any selection/quantity is typed localized intent conflict before new writes. Do not silently return the old result for a new request.
- A missing key is a typed refusal, not a non-idempotent fallback.
- A cancelled recorded resend is not invisible: keep occurrence history; retry cannot silently recreate or loop into a unique-index failure. A deliberate replacement uses a NEW occurrence only after authoritative resolution and consumes only legitimately released source capacity.
- Reconcile crash windows using exact durable occurrence/parcel links and exact intent; never scan arbitrary open parcels or guess from matching total quantities.
- Authorization, actual method/group and package locks remain enforced; replay must not bypass caller authority.
A minimal owning additive migration is authorized only if existing durable storage cannot represent the required binding. Explain why; no disconnected status service.

## 3. B — retained full client request through uncertain outcome
Source defect: post-dispatch-exception-dialog.tsx effect [open,mode,parcelKey,lines] resets drafts and mints a fresh key on reopen/reference refresh. Uncertain requests can become a second operation.
Prebind and retain the COMPLETE reviewed resend envelope before first POST, scoped to viewer + checkout/fulfillment + source parcel. Follow the existing253 retained-attempt safety pattern and existing feature boundaries.
- A harmless lines rerender/refresh does not reset an open review, overwrite edits or mint another occurrence.
- Same-tick submit sends one mutation (synchronous single-flight + finally).
- Timeout/network/5xx/undefined and typed refusal keep the exact envelope/key; no bare error-code release.
- Close/reopen/remount restores the exact submitted request; explicit reconcile submits it verbatim, even if fresh capacity/action readiness is now false due to partial progress.
- Changed review cannot replace or remint an unresolved attempt. Only authoritative completion/resolution permits a deliberate new occurrence.
- Viewer switch never inherits another viewer's attempt. Storage unavailable must not lead to blind remint; expose a safe, bounded recovery behavior using owning state.
Keep one concise inline localized error and purposeful existing dialog. No raw diagnostics/technical paragraphs, no extra page, no inputs outside the review dialog.

## 4. C — source-parcel capacity and precise physical lineage
Source defect: FulfillmentUnit ReturnedUsableQuantity/ResendCommittedQuantity pool by order line across all receipts; CreateResendShipment replaces a resend source with its ancestor. This permits borrowing another source parcel's units and loses immediate return lineage.
Make allocation capacity specific to the actual physically received source parcel and its own usable receipt quantities.
Example: A and B each carried one of the SAME order line, both physically returned usable1. Resend from A qty2 MUST refuse although total returned is2. A1 and B1 may each allocate once.
A returned resend must reference its IMMEDIATE returned source for its next resend, keeping the full root/ancestor chain readable. Root identity, if useful, is separate; never replace the source argument with an ancestor to pass capacity checks.
Repeated cycle: A returned usable1 -> R1 dispatch -> R1 returned usable1 -> R2 is lawful once, linked R1, with no double customer delivery or capacity borrowing; the first A receipt cannot fund another live branch while R1/R2 represents those units.
Cancellation of a Created resend releases only its own source allocation once. Concurrent independent requests cannot overdraw that source. Damaged/missing/in-transit goods grant zero.
Update ALL affected source capacity readers/projections/dialog lines from the same authority. Keep fresh257 packed-minus-shipped-minus-open quantity separate; resend dispatch never consumes the original inventory reservation twice. Do not inflate packed/ordered or decrement lifetime shipped, rewrite tracking/history, trigger settlement/refund or saleable-stock increases.

## 5. D — mixed usable/damaged receipt for one line
Source defect: ShipmentReturnReceipt HashSet<OrderLineId> forbids a single parcel line split between Usable and Damaged.
Support an exact physical receipt such as parcel line quantity3 = usable2 + damaged1. Validate positive quantities, owning line, legal condition, no duplicate same line+condition, and SUM of all conditions <= what THIS parcel carried. Usable capacity is2 only.
Use one coherent receipt representation, validator, read projection and purposeful UI. Replay compares the complete canonical receipt; changed quantities/conditions conflict without adding capacity.
Keep durable actor/time. Missing quantities do not become usable by default. If receipt remains a one-time final receipt, make finalization explicit in concise operator wording; do not claim incremental receipt support. No broad warehouse/RMA redesign.

## 6. Focused proof — one canonical database only
ONE database tooba_alpha on5432; same FE3000 -> Host5088 SingleStore/store-alpha. NO disposable/test/parallel DB, Docker PostgreSQL,15433 or edition/connection switch.
User order/parcels remain read-only. Minimal clearly task-labelled production-domain/application fixtures in SAME DB authorized, exact IDs recorded, no raw business SQL/DDL/reset/reseed/backfill or sweeping delete.
Run meaningful focused cases:
A same occurrence same intent replay; changed source and changed quantity under same key conflict with zero new rows; missing key refusal; cancelled recorded occurrence handled explicitly.
B real independent contexts same key -> one parcel; different keys over same source capacity -> no over-allocation; storage-level enforcement, not InMemory.
C two sources same line cannot borrow; source and sibling-group isolation; two successive physical return/resend cycles preserve immediate links and inventory/customer-delivered conservation; pre-dispatch cancellation releases only own allocation.
D usable2 + damaged1 on one3-unit line persists/readbacks, grants2; aggregate excess/duplicate condition/foreign line refused, exact replay vs changed receipt.
E real mounted dialog/panel: lines rerender preserves edits/key; one same-tick submit; network loss + close/remount + consumed fresh capacity still reconciles EXACT saved payload; changed review blocked; viewer isolation; completion then deliberate next occurrence distinct.
Retain258 lifecycle/package-lock safety and257 quantity/tracking focused regressions. Verify durable operator audit for new facts through the existing owning path; do not treat an ignored actor argument as audit proof.
Do not repeat broad module suites or new stress loops. Affected tests, typecheck/build/publish only; disclose unrelated reds without rebaseline. No screenshots required.

## 7. Deploy / invariants / handoff
D:\Users\User\source\repos\SarvNewVer; canonical FE3000 -> Host5088, SingleStore/store-alpha/tenant-alpha/tooba_alpha only.
Deploy changed components, verify served build and runtime identity. Necessary additive migrations only after private backup and existing MigrationRunner; old rows readable, no fabricated historical receipt/intent or rewritten accepted money/history.
Preserve DataProtection, local CapturedPreview ON/7dates and checked-in OFF.255 stays parked; live Tipax deferred, no provider credential/call/fake waybill.
Existing active consolidated-package children remain locked with a precise refusal; parent workflow belongs later259, do not silently unlock or broaden this repair.
Preserve approved timing tabs/save and256 scheduling rules. No published service or real user-order mutation.
Authorized read-only DOM on existing order01a1b92d-e97e-7000-999a-143350d0edaf; verify contextual actions/one action owner and no overflow. No click performing real business mutation.

## 8. Evidence / recovery / required result
Persist TASK.md verbatim under docs/ai/tasks/TB-SHIPPING-NONDELIVERY-RETURN-AND-RESEND-258-R1/ and TASK/RESULT/RECEIPTS + exact focused evidence under docs/architecture/evidence/TB-SHIPPING-NONDELIVERY-RETURN-AND-RESEND-258-R1/.
Update TOPs (AGENTS.md, START-HERE-IF-CHATGPT-IS-LOST.md, SHIPPING-ARCHITECT-RECOVERY-2026-10-22.md) and closure plan: issue3 repaired pending Architect review, NOT five-defect completion;259/260 still NOT ISSUED.
Return BEGIN_TOOBA_WORKER_RESULT with full Base/Code/Merge/Evidence SHA, HEAD==origin/main, exact changed paths, migration/backup if any, runtime/build, case results, fixture IDs and limits. Worker PASS is not Architect/user acceptance.
STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
END_TOOBA_TASK
