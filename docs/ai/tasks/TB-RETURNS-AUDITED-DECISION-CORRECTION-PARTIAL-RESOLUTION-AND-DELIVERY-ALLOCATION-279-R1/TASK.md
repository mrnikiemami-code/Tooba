PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-RETURNS-AUDITED-DECISION-CORRECTION-PARTIAL-RESOLUTION-AND-DELIVERY-ALLOCATION-279-R1
Parent-Task: TB-RETURNS-AUDITED-DECISION-CORRECTION-PARTIAL-RESOLUTION-AND-DELIVERY-ALLOCATION-279
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA
Repository: mrnikiemami-code/Tooba
Branch: main
Mode: ONE_CONSOLIDATED_CRITICAL_AND_SMALL_RETURNS_CLOSURE
Track: RETURNS_MARKETPLACE_CLOSURE
Title: Close all six source-confirmed 279 defects and the recorded small issues in one bounded repair
Expected-Base-SHA: 0ef5ae21a048dc6e466d50d8c72bcbe7b9e27e32
Reviewed-Implementation-SHA: 9c0885a5943e7f1a4e1a09a17fac417d23037aa6
automaticNextImplementationTask: NONE

## 0. Authorization, pace and exact envelope

The user explicitly says: "تسک بده جمع بشه دیگه". This is ONE consolidated repair of 279, not six micro-tasks, a new roadmap, a rewrite or permission to suppress failures. Complete A–G in this task and stop for Architect/user review. Small items S1–S5 are INCLUDED after the critical paths; do not silently defer them.

The complete exact envelope is persisted BEFORE execution in:
- docs/ai/tasks/TB-RETURNS-AUDITED-DECISION-CORRECTION-PARTIAL-RESOLUTION-AND-DELIVERY-ALLOCATION-279-R1/TASK.md
- docs/architecture/evidence/TB-RETURNS-AUDITED-DECISION-CORRECTION-PARTIAL-RESOLUTION-AND-DELIVERY-ALLOCATION-279-R1/TASK.md
- docs/architecture/evidence/TB-RETURNS-AUDITED-DECISION-CORRECTION-PARTIAL-RESOLUTION-AND-DELIVERY-ALLOCATION-279-R1/issuance-receipt.json (SHA256 and byte length)
Use those identical bytes if Bridge content is empty. Never reconstruct this task from a summary. A later docs-only issuance commit on top of Expected-Base-SHA is explicitly permitted: verify the base exists/is an ancestor, record actual claim SHA, and verify the incoming delta contains NO source/config/schema changes. Stop RECOVERY_CONFLICT for divergence or incoming source drift; no reset/stash/rebase/amend/force or discarded user work.

Single worker, one active task, no delegation/subagents/historical agents, no screenshots, no polling loop. Read selectively and implement directly; do not spend the task on repeated reports or broad test loops.

## 1. Mandatory targeted source audit

Read root AGENTS.md CURRENT banner and applicable governance; read the repository backend/frontend feature skills before changing their files. Read:
- docs/architecture/evidence/TB-RETURNS-AUDITED-DECISION-CORRECTION-PARTIAL-RESOLUTION-AND-DELIVERY-ALLOCATION-279/ARCHITECT-REVIEW.md
- docs/planning/returns/RETURNS-279-CONSOLIDATED-CLOSURE-CHECKLIST.md
- docs/planning/returns/RETURNS-279-EXECUTION-DECISIONS.md
- original 279 TASK.md and its source-linked proof documents, with prior 278 review only where referenced.
Inspect actual owners, not every historical task:
ReturnDirectory.cs, .FirstRefund.cs, .Corrections.cs, .Resolutions.cs, .Allocations.cs, .CourierOrigin.cs, .Recovery.cs, ReturnBudgetSerialization, ReturnEligibilityEvaluator, ReturnCorrectionModels/ReturnAllocationPolicy/ReturnRecoveryPolicy/ReturnWorkPolicy, owning command/route/adapters, persistence configuration; customer return-request-modal/return-submission, operator workspace/review-intent, owning Order payment/unit read contracts. Reuse the existing shared Dialog and installed react-toastify AppProviders.

Retain 277/R1/R2 bounded acceptances, decision-only approval and distinct first-start/resume/retry intents. Parent 279 and 278 remain PARTIAL until this repair is independently reviewed. Do not rewrite previous evidence or acceptance.

## 2. Architecture decision

ONE_REVIEWED_PORTION_AUTHORITY_FOR_MONEY_STOCK_AND_CUSTODY_ONE_ATOMIC_VERSION_FENCED_DECISION_TRANSITION_ONE_SHARED_RETURNS_PAYMENT_RESERVATION_PROTOCOL_FOR_EVERY_NEW_ATTEMPT_AND_ONE_VIEWER_SCOPED_EXACT_DURABLE_CUSTOMER_INTENT_WITH_NO_GUESSED_LEGACY_ALLOCATION_ON_THE_EXISTING_RETURNS_MODULE

Returns owns its facts/transactions. Foreign module access is Contracts-only through owning ports, no foreign-schema SQL/FK. Read description and actions use the same pure authority. Frontend flags never grant permission. No alternate payment/stock engine, background retry, daemon/lease framework, global error suppression, magic timer, epsilon/clamp/round, first-item/seller fallback or blanket tracker clear.

## A. One reservation protocol for FIRST and RETRY money (F1)

Source defect: RetryRefundAsync persists/executes a new child without the new shared budget protocol. PersistFirstAttemptUnderLockAsync calls _payments.GetAsync inside locks/transaction and skips the ceiling when it returns null.

1. Both first attempt and NEW failed-attempt child use ONE short owning persistence/reservation authority. Exact positively bound stored identity replay occurs before mutable new-attempt gates and mints nothing; terminal attempts are never re-sent. Pending resume uses the same stored key/amount, not a new reservation.
2. Obtain owning payment facts BEFORE database transaction/locks. Positively bind payment, customer, checkout/seller order as available through the owning contract, currency, lawful paid amount and frozen approved amount. Missing/incoherent authority fails closed. Extend the owning read contract minimally if required; never accept client money facts or infer missing ownership.
3. Under a consistent documented seller-order -> case -> payment lock order, re-read current Returns decision/resolution/version/binding and all Pending+Succeeded obligations for that payment. Count all Returns cases, not only this seller/case. Persist case processing state + attempt + reservation atomically and COMMIT before any external port. No gateway/Wallet/Inventory/owning payment read call under a transaction/lock.
4. A definite failure releases only its own reservation by the durable attempt outcome; unknown/Pending keeps it; Succeeded remains counted. Two requests cannot independently pass the same capacity. Use database arithmetic/short owning coordination, not a process semaphore.
5. Regression: A failed 60, B now reserved 60 of payment 100, A retries 60 => typed budget refusal, no child/effect; duplicate same child => same stored identity; missing payment authority => refusal, never a skipped check. Currency/owner mismatch refuses before attempt.
6. Preserve caller/ambient/enlisted-transaction refusal before external effects, existing exact outcome coordination and stable provider identities. Scope is Returns-mediated obligations; explicitly retain the limitation that outside-Returns card refunds need owning Payment authority and are not universally covered.

## B. Real partial resolution drives money AND actual physical effects (F2)

1. Replace the usable-first inferred accepted split with an explicit reviewed per-item/per-condition authority. Each requested line must account for accepted received portions and rejected/withdrawn remainder; never infer accepted condition from Min(accepted, usable). Keep compatible stored version-0/legacy facts honest.
2. Validate identity, own accepted unit quantum, exact six-decimal representability, nonnegative portions, per-condition received ceiling, aggregate requested ceiling and full accounted remainder. Refuse, never clamp. No requested quantity, condition default or frontend draft is receiving evidence.
3. Allow a complete reviewed resolution with accepted 2 actually received + rejected 1 never received out of requested 3 to progress. Approval/start/retry/recovery gate accepted receiving coverage and resolution coherence, not whole-original-request IsComplete. Unreceived rejected remainder does not create handback/stock or a fictitious receipt.
4. Money = accepted quantity × STORED line money according to the existing currency policy; requested refund_amount immutable; approved amount frozen before first attempt. Zero accepted means zero owed, not a successful refund; no zero-money gateway attempt.
5. Actual SettlePhysicalDispositionAsync/Inventory call uses ONLY accepted usable quantity. Received 3 usable / accepted 2 / rejected 1 => Inventory receives exactly 2; 1 remains customer custody. Accepted damaged never enters saleable inventory.
6. Mixed accepted usable/damaged portions each need their own durable positive completion. A single coarse item Disposition must not prove both. Per-portion writes/replays use stable owning identities; local-save failure must leave durable pending facts and retry must not duplicate Inventory work. Missing stock binding remains explicit pending. Keep the historical per-item restock identity for already-started legacy work; do not re-key/re-execute recorded effects.
7. Read phases, capabilities, displayed saleable-restocked quantity and closure derive from these SAME stored portion facts. Do not label settled while money or handback or either accepted physical portion remains unresolved.

## C. Rejected received goods remain owned custody and occupy capacity (F3)

1. Whole rejection and partial rejection must both expose a lawful actual-handback action for received goods, even without a prior partial plan. Keep original receiving/decision history, no automatic stock/refund/return shipment.
2. Do NOT release capacity for rejected received goods merely because case status becomes Rejected/Cancelled. Hold the outstanding physical portion under the seller-order serialization until its real handback is durably recorded. Release only the positively releasable remainder; partial handback releases only its portion. No second claim over goods still held.
3. Custody command validates own case/item/condition, exact unit quantum, condition-specific rejected-received minus condition-specific handed-back ceiling and server-derived recipient. Wrong-condition quantity, over-handback or foreign item writes nothing. Replay compares the whole immutable custody intent.
4. Proof semantics: explicit operator attestation of actual in-person handback to the owning customer (required reviewed proof/reference), OR an existing owning carrier's verified delivered-back fact if such an owner already exists. Do not build a carrier subsystem here; do not accept tracking creation/dispatch as delivery evidence. The UI must state what the operator attests and freeze that intent.
5. An owed handback stays active/readable; custody is zero money and cannot reopen a completed financial attempt. Reopen continues to refuse irreversible custody/effects.

## D. Freeze reviewed versions; persist one atomic decision transition (F4)

1. Require reviewed decision AND applicable resolution version in the frozen command from modal -> API -> CQRS -> owning Directory for a NEW decision/first refund. A POST-side fresh snapshot cannot stand in for the version the operator reviewed. Missing/stale versions fail closed for new effectful commands; exact recorded replay remains lawful before mutable gates. Route adapters and old callers must not silently choose current/latest.
2. One owning short transaction persists decision revision/baseline, case status, frozen approved amount and any capacity change together. RecordReviewedDecisionAsync must not commit a decision and let ApproveAsync/RejectAsync perform the corresponding state change later outside its lock.
3. Re-read tracked entities AFTER lock and compare expected versions; do not let pre-lock tracked values overwrite newer correction. Correction and every attempt reservation obey the same authority, so reopening/reapproving between modal review and send cannot spend under the obsolete review.
4. Replace string.GetHashCode durable occurrence keys with the exact frozen reviewed occurrence or a deterministic cryptographic identity of canonical immutable data. Exact replay requires full intent comparison, not merely same decision text or latest status.
5. Unique conflict arbitration requires exact SQLSTATE/schema/table/constraint identity and scoped savepoint/detachment; never Clear the caller tracker, swallow unrelated persistence failures or commit/rollback caller transactions. Preserve immutable baseline and append-only history.

## E. No guessed legacy delivery attribution (F5)

1. New cases allocate to their own positively delivered slices, oldest eligible deadline first, under seller serialization; same-instant merge remains bounded to owning line, with exact quantities/windows.
2. A legacy case without allocations must NOT be guessed onto oldest slices. Where positive facts prove a unique allocation, use that proof; otherwise publish a typed ambiguity blocker and refuse a new allocation that relies on choosing the origin. No backfill, old/new quantity mixing or invented date. Return window and quantity blockers stay distinct.
3. Reopen/reacquire uses the case's provable capacity and current competing reservations. It cannot reactivate old rows without checking the exact slices; a sibling-taken slice is a zero-write conflict even if a different slice has spare capacity.
4. Retain received rejected custody occupancy from C. No broad status-only shortcut.

## F. Finish the customer exact durable act, shared modal and small items (F6, S1–S5)

1. Safe access to the sessionStorage PROPERTY as well as its methods. Unavailable/unreadable evidence is an explicit blocked state, never retained=null meaning free-to-submit.
2. Require resolved authenticated viewer scope for a new act. Do not send/store in a shared unscoped key while viewer is unknown. Preserve legacy unscoped evidence without silently adopting/deleting it under a different viewer. No cross-viewer scan/update/clear by submissionKey alone.
3. Before ANY new act write, compare the exact current scoped evidence. Pending/unknown/unreadable cannot be overwritten by a second click, reopen or changed selection. Read-back must verify exact bytes before the first send; write failure/refusal sends nothing.
4. Parse and validate each frozen body's seller/line/quantity/reason/destination/idempotency key against its envelope; enforce known version/state/action and one exact checkout/viewer scope. Update outcomes and clear with a full exact compare on the correct scoped act. Never clear a different body merely because the key matches. A recorded child requires an actual valid bound case response, not any ok-shaped JSON.
5. Epoch fence open/close/unmount/retarget/viewer changes, EVERY next child send, callback/toast/load/finally and busy lock. A stale completion may reconcile ONLY its exact originating stored child; it cannot send further children for a changed viewer, paint/release newer UI state, replace newer evidence or clear an unrelated act. Reopening cannot reset the executor lock while an old operation is still active.
6. Preserve already recorded child results and retry only unresolved frozen bodies. Same-line multi-reason portions share ONE exact remaining ceiling. Known no-effect tokens must be dictated solely by immutable bytes/identity, not changing metadata/state/auth/window/capacity. Keep the uncertainty card when a later refusal does not prove the first send was unwritten.
7. Replace the CUSTOMER custom fixed div role=dialog with the existing shared DS Dialog; operator dialogs stay shared. Focus/return, Escape/cancel, local scrolling, >=44px new controls and no document overflow at 390/1440. No native prompts or inline expanding action forms.
8. Actual template feedback = existing react-toastify through ONE AppProviders container. One toast per user command outcome, no toast on GET/render, inline field errors remain inline. No ToastRegion or duplicate provider.
9. Unknown accepted unit metadata must produce a CONSISTENT read/write policy and honest UI explanation. Preserve lawful whole-quantity behavior where the existing owning rule authorizes it, with explicit whole-only restriction; do not invent fractional precision or silently hide a request the server accepts. Exact millionths and known unit precision remain authoritative.
10. Update the stale 276 mounted fixture to the accepted current wire contract without weakening assertions; if superseded, map each former behavior to retained coverage and remove only truly obsolete duplication.
11. Human locator, own product/unit, requested versus approved money, accepted versus returned-to-stock and outstanding custody must be clear. No "all refunded" for rejected/zero-owed quantities, raw GUID as human number, or settled claim with owed portions. Recover source ownership, don't rewrite the whole workspace.
12. Finish checklist S1–S5 in this SAME pass. Update concise recovery pointers/current state; preserve original audit/worker reports, no repeated giant new history block. If a real item cannot close, explicitly report it with evidence and reason; no PASS claiming it closed.

## G. Schema, proof, deployment and handoff — bounded authority

Schema:
Prefer existing additive structures. If explicit per-condition resolution/physical completion/custody needs missing durable fields, this task authorizes ONE minimal additive Returns-owned migration and its model/config/snapshot/constraint updates. No foreign schema, drop, rename of shipped facts, editing applied 279 migrations, data backfill/repair/reset/seed. Give legacy nullable/versioned facts honest read semantics; do not rewrite completed rows.
Generate and inspect actual snake_case mapped keys/types/FKs/checks before apply; verify has-pending-model-changes consistency. A fresh private canonical pg_dump, SHA256 and pg_restore --list success are REQUIRED BEFORE the actual apply. Runner plan must contain only this task's intended migration. Apply through Tooba.MigrationRunner once; after any failed fully rolled-back apply, inspect source/plan and make a NEW verified backup before a corrected apply. Do not manually DDL/Down/reapply. Report every attempt honestly. If changes exceed this bounded additive schema, STOP with a concrete proposal, not a partial silent redesign.

Focused proof:
- Add behavioral regressions at the real owning Directory/ports for A–E, including actual Inventory argument on partial acceptance, accepted-unreceived-rejected progression, mixed condition durability/lost-save, rejected-held capacity, condition-specific handback, stale reviewed version and exact replay, budget retry after another case reserves, missing/mismatched authority, ambiguous legacy slice. Pure policy tests alone are not proof of the effectful Directory.
- Add narrowly scoped rollback-only real PostgreSQL facts for the newly shared seller/case/payment coordination and atomic decision transitions. Verify target client loopback:5432/tooba_alpha and server database/port before writes; caller-owned harness rollback only; separate-connection final committedRows=0. Allowed: two-connection lock contention with rollback, isolated protocol facts with controlled ports. Forbidden: committed fixtures/two-writer commits/delete cleanup/second DB. A caller-transaction external-effect refusal alone does NOT prove budget coordination. Do not claim a committed race or live provider effect.
- Mount the REAL customer modal and operator workspace with controlled HTTP/storage. Cover pending overwrite refusal, unreadable/throwing storage getter/read-back failure, two-seller partial success and byte-identical unresolved replay, frozen-body tampering, viewer/checkout retarget/unmount/late answer, same-line portions, shared Dialog lifecycle, single template toast, partial-resolution/custody/frozen-version UI.
- Run directly changed/retained focused classes after final code changes, tsc, production FE build, backend Release build and publish. Run relevant exact architecture pins only if contract/route/catalog changes require it; update actual typed/localized sets truthfully. Do NOT use a broad whole Returns/Host/AMSC project run as a substitute, and no repeated green-suite loops. Report any necessary failed-first test with its correction; don't hide failures.
- Refresh Host/Next only when their served source changed, gracefully from the correct publish working directory/current configuration; verify actual loaded DLL hash, BUILD_ID/manifests/chunks and health/ready/platform. No settings/key/port change.
- Live verification is GET/read-only DOM and SELECT on existing cases ONLY. No live POST refusal probes, no real receipt/decision/reopen/resolution/custody/start/retry/recovery, no provider/PSP/Wallet/Inventory/notification/waybill/money call. No new review case/order/hold/fixture and no reset of the three completed cases. At 390/1440 check read-only overflow/labels; active UI is proven mounted. If no active live case, explicitly say user can inspect history and the active flow lacks a live walkthrough.
- No screenshot, stress harness, automatic replay, polling or broad module recertification.

## 3. Protected state

ONE canonical tooba_alpha @127.0.0.1:5432, SingleStore/store-alpha; Host5088, FE3000. No new DB, Docker environment,15432/15433, edition/tenant/port/connection/settings/key/CapturedPreview change.
Existing completed user cases (including 01a1e6b0-f33a-7000-80ee-beffb6344490 and 01a1e9c5-c729-7000-b614-79daf3d0e812) and the third completed case remain read-only with their original history/amount/updated_at. No business row mutation beyond rollback-only task transactions; authorized migration/history is the only committed store change.
Unrelated untracked scratch scripts and Host App_Data keys are preserved, uncommitted. No cleanup/delete/restore/reset. No provider readiness, financial acceptance, full Marketplace/Returns acceptance or user visual acceptance claim.

## 4. Evidence, recovery and exact success criteria

Persist RESULT.md, RECEIPTS.md, result-contract.txt, closure-matrix.md, budget-and-decision-proof.md, portion-and-custody-proof.md, customer-intent-proof.md, rollback-only-proof.md, user-review.md and migration-and-backup-proof.md only if schema applied under this exact authority.
Closure matrix maps EVERY F1–F6 and S1–S5 to exact source + meaningful passing fact + remaining limit; evidence should be concise, not duplicated prose. Record actual base/claim/code/evidence/final SHA, final fetched origin parity, changed paths, runtime identities and protected-data read-only comparisons.
Update AGENTS CURRENT and Recovery SoT/START-HERE/Returns plan pointers to THIS result and accurate active state. Do not mark Architect ACCEPT or retroactively alter any prior PARTIAL/acceptance.
PASS requires all A–F implemented and the focused proof above with no unresolved critical counterexample. If any critical item is unresolved, report PARTIAL or BLOCKED with the exact issue rather than a cosmetic PASS. Finish small items together; no automatic follow-on task.
Known unavailable real provider effect, committed race or active served case is a declared acceptance limit, not permission to manufacture data or claim readiness.

Canonical final result:
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-RETURNS-AUDITED-DECISION-CORRECTION-PARTIAL-RESOLUTION-AND-DELIVERY-ALLOCATION-279-R1
Parent-Task: TB-RETURNS-AUDITED-DECISION-CORRECTION-PARTIAL-RESOLUTION-AND-DELIVERY-ALLOCATION-279
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA
Status: PASS | PARTIAL | BLOCKED
Scope: A-G_CONSOLIDATED_279_F1_F6_AND_S1_S5
Expected-Base-SHA: 0ef5ae21a048dc6e466d50d8c72bcbe7b9e27e32
Base-SHA: <actual claim SHA; docs-only issuance delta declared>
Code-SHA: <sha>
Evidence-SHA: <sha or explicitly one combined commit>
Final-HEAD: <sha>
HEAD-Equals-Origin-Main: <verified fresh fetch>
Critical-Closure-State: <F1-F6 each truthful>
Small-Closure-State: <S1-S5 each truthful>
Schema-State: <none or exact bounded additive migration/backups/apply>
Relational-Proof-State: <actual facts + committedRows>
Validation-State: <exact commands/counts/failures>
Served-State: <DLL/BUILD_ID and read-only receipts>
Live-Business-Write-State: ZERO
Provider-Money-Stock-Effect-State: ZERO
User-Work-Preserved: YES
Architect-Accept-State: NO
User-Acceptance-State: NO
Evidence-Limits: <explicit>
Recovery-Next-Task: NONE
Next-Recommended-Task: NONE
automaticNextImplementationTask: NONE
Stop-State: STOP_FOR_ARCHITECT
END_TOOBA_WORKER_RESULT

Post exactly the complete real Result to Bridge after validation/commit/push; never probe /api/results with placeholder data. Complete the claimed task lifecycle and stop entirely. Do not start another task or issue a new plan automatically.
STOP_FOR_ARCHITECT
END_TOOBA_TASK

