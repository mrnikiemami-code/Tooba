PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-RESEND-REPLACEMENT-OWN-DISPATCH-PLAN-270-R4
Parent-Task: TB-SHIPPING-RESEND-REPLACEMENT-OWN-DISPATCH-PLAN-270-R3
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 1ff02ab8a18c20999b92791f2b51520726bfa2d1
Mode: REPAIR_PRE_SEND_DURABILITY_NEW_CONFIRM_GATES_AND_EXACT_HANDOFF
Track: SHIPPING_OPERATIONAL_TIMING
Title: Close four remaining planning recovery and command-boundary defects
STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

ONE SMALL REPAIR — NOT A NEW PHASE
Fix ONLY the concrete four defects below on the existing R2/R3 implementation. No new architecture, schedule engine, schema, job-stage filters, screenshot work, live examples or broad refactor.
ONE worker, ONE active task. No delegation/subagents/concurrent task or historical-agent resume. 271 NOT ISSUED. No automatic next implementation, idle heartbeat, polling/wait/retry loops.
No screenshot request/capture/output. No unfiltered suites, baseline/stash comparisons, cleanup or speculative hardening. Keep validation directly tied to these defects.

ARCHITECT REVIEW OF R3
Decision PARTIAL, not Architect ACCEPT. Reviewed actual main 1ff02ab8a18c20999b92791f2b51520726bfa2d1 / implementation 96c5a8e19c10ba246b799202a804187ca1957a18. Improved: captured AreaGroupId, exact binding comparison, Inventory hold read, own-plan guard/projection/bulk preflight, selected context fingerprint and create-to-plan plumbing. Preserve these repairs.
Remaining source-confirmed defects:
A. admin-shipment-plan-modal.confirm writes the body BEFORE POST with uncertain:false. load freezes ONLY rec.uncertain && rec.requestBody and treats every other retained body as history. If navigation/reload/unmount occurs while the first POST is in flight, no catch has set uncertain:true: on remount the original body/key can be overwritten by a fresh intent. A request is unresolved as soon as it may be sent, not only after the UI catches an exception.
B. PlanShipmentDispatchCoreAsync, after stored-plan replay, calls synchronous RequirePlannable(locatedAny). It NEVER calls RequirePlannableAsync and therefore bypasses its active-package membership gate for a direct new confirmation POST. The earlier new-command EnsureCancelledDoesNotBlock was removed; RequirePlannable does not check IsCheckoutCancelled either. Preview denying a cancelled checkout is not a guard for the mutation. Exact authorized historical replay must remain possible, NEW confirmation must enforce cancellation/package gates.
C. features/order-operational-groups/created-parcel.ts finally returns the only Created resend candidate even when a nonempty requested occurrence key did NOT match it. That is a fallback to another attempt, explicitly forbidden. Replacement decision fallback scans every source and accepts any one link with matching key without binding the requested source parcel. "Only one candidate" is not exact operation identity.
D. Modal's epoch effect returns immediately when !open and has no cleanup. Thus closing/unmounting alone does not invalidate an in-flight load/send callback; send.finally also calls setPending(false) unconditionally. Existing per-component inFlightRef is not durable across remount; an old answer can clear a newer same-scope retained attempt if clearing is scope-only.
R3 green tests did not cover the first-POST interruption (before any exception), unmatched-key single-candidate fallback or direct package-locked confirmation. No need to rerun the entire earlier feature proof.
Relational evidence remains sequential immutable/index proof; do not repeat the contradictory "two independent writers" claim. R2 missing pre-apply backup remains an irreversible audit deviation; no retroactive fix/reapply/restore.
Architect has not executed local runtime/tests; mounted/owning-app test evidence is worker evidence, no live/user acceptance.

RECOVERY / GOVERNANCE
Fetch origin normally; main + safe worktree; issued Expected-Base-SHA must exist and be ancestor of actual origin/main. Declare compatible docs advancement, unsafe code divergence/missing base -> RECOVERY_CONFLICT. No silent base substitute, force/amend/reset/rebase/stash or unrelated scratch edits.
Read current AGENTS/START-HERE and canonical BRIDGE-WAKE-V1 protocol/controller, required backend/frontend feature skills and applicable standards. Follow existing Contracts/CQRS/Result/localization rules without new audit/certification/findings scan.
Review only targeted source plus retained planning/post-dispatch patterns needed for these repairs. Preserve all current quantity/payment/hold/receipt/package and date/zone semantics.

REPAIR A — UNRESOLVED FROM THE FIRST POSSIBLE SEND
Before invoking the operation executor, durably store the exact body/key/context as UNRESOLVED/SENDING. A verified durable unresolved boolean is sufficient if used consistently; no generic state-machine framework.
After that boundary, unmount/reload/close/process interruption is an unknown outcome until an authoritative response proves otherwise. Remount must recover this exact body even when no catch/finally ran, even when its slot is no longer in the fresh preview, and even when preview fails.
Never generate a new occurrence or rebuild dates/context from UI while the previous send is unresolved. Retry explicitly with its exact serialized intent, not a fresh selection. No timers/polling.
Migrate already-retained v1 body records conservatively: body with no authoritative terminal result cannot be assumed never sent merely because uncertain is false/missing. Do not discard it automatically. Refuse send if durable write/read-back fails; memory-only record cannot authorize mutation.
Clear/transition only for that EXACT occurrence/body after authoritative success/replay or a proven no-write refusal. A generic 4xx (especially conflict/authorization loss) is not by itself proof no plan exists. Preserve unresolved evidence and expose bounded existing outcome read/reconcile where needed. Never clear a pending key then auto-create a new intent.
A genuinely ended no-write review may start a new review only through explicit user selection, with prior outcome preserved. Keep existing failure/copy behavior concise and one surface.

REPAIR B — REPLAY FIRST, NEW-WRITE GATES SECOND
After access and exact checkout/parcel association, read a recorded exact plan first and answer immutable history/replay without recalculating or re-enabling actions.
When no plan exists, require BOTH checkout not cancelled AND the actual async active-package lock gate before any new plan build/insert, along with existing Created/Paid/group/source/hold/context/feasibility checks.
Use the same authoritative gate composition for NEW preview/confirm; no UI-only safeguard or duplicate slightly different policy. Do not move live eligibility checks ahead of a known stored replay.
For preview/read recovery, expose an already recorded plan under authorization even when checkout was later cancelled; apply cancellation rejection to offering a NEW calendar. No mutation/dispatch action granted by history response.
Add focused command-level cases proving direct package-locked/cancelled requests cannot insert a row (regardless of hidden UI), while exact authorized committed replay remains readable after those state changes. Denied actor/cross-checkout remains refused.

REPAIR C — EXACT PARCEL HANDOFF, NO SINGLE-CANDIDATE SHORTCUT
A resend handoff requires exact sourceShipmentId AND exact nonempty occurrence recorded by the owning command/reconciliation, or an explicit canonical result identifier whose ownership is actually proven. If key is missing/mismatched, return no target rather than choose an existing child.
A replacement must bind requested source, reviewed decision/occurrence, actual replacement id and Created child coherently. Do not scan unrelated source decisions as substitutes. Ambiguous/missing identity returns no auto-open, existing cards remain truthful.
Remove the only-Created-candidate fallback; do not replace it with first/latest/status-only/time-based inference. Existing first/exact-key path remains valid when truly exact.
Prefer existing command/reconcile payload; a minimal owning result enrichment is allowed only if required for an otherwise missing exact identity. No changed create identity policy, duplicate parcel or new wizard.
Tests must include one valid Created child with WRONG occurrence key (must not open), a right key on WRONG source/decision (must not open), ambiguous children, and exact created/recovered positive path.

REPAIR D — CLOSE/UNMOUNT/NEW-TARGET RESPONSE FENCE
Invalidate the presentation epoch on close AND effect cleanup/unmount, not just when a later open/load effect happens. Guard all stale outcome callbacks including finally UI updates; old callback must not close/reopen a newer dialog, refresh wrong target or clear/overwrite newer storage.
Storage clear/update must compare the currently recorded occurrence/body with the request being resolved (compare-and-clear/update), not merely viewer/checkout/shipment scope. Protect against response arrival after a genuine remount or replacement review.
Do not rely on a React ref surviving unmount. Durable unresolved state from Repair A controls same-scope remount; epoch controls old component callbacks. No global timer/lock timeout or assumed request cancellation.
Continue to allow explicit exact replay of unresolved intent; prevent a second DIFFERENT intent while first may have committed. Don't turn lifecycle fencing into an endless disabled state with no recovery.
Retain existing same-tick guard and exact viewer scope, 268 tabs and single action/modal owner.

FOCUSED CHECKS ONCE
Use existing task-specific tests and deterministic deferred promises; no sleeps or broad loop:
1 Start first POST, assert unresolved exact envelope DURABLE BEFORE executor is invoked, then unmount before promise resolves or catch runs. Mount again: same body/key recovered, different slot cannot overwrite, explicit retry is exact. Exercise preview failed/old slot absent and old uncertain:false records.
2 Late success/refusal after close/unmount/different target/new valid occurrence cannot clear newer storage or fire onClose/onCompleted for the wrong surface. Compare-and-clear/current-request proof, not just epoch counter unit assertion.
3 Actual planning command with no existing plan: active package and cancelled checkout each refuse with zero inserts. Same recorded exact plan after lifecycle/cancellation answers stored history; denial/cross-checkout still refuses; no fresh calendar/action.
4 Actual created-parcel resolver AND connected existing panel handoff: wrong key even with one child, wrong source replacement and ambiguous source -> no modal; exact source/key/decision -> correct child; no duplicate create. Run this changed helper's test file explicitly, don't only list it in changed files.
5 Retained body conflict/authorization response preserves recoverability rather than assuming absent server outcome; proven no-write expiry supports deliberate fresh review with prior result.
Run only changed helper tests, named relevant shipment-plan mounted cases and affected Order command test class; retained feature suite only where directly affected. Typecheck/affected build + required targeted checks once after final code; no unrelated Shipping/Inventory/Fulfillment/relational reruns if untouched.
Use actual configured mount runner tsx --tsconfig tsconfig.mount-tests.json --test. No wrong-runner probe, unfiltered Composition or disposable Postgres matrix.
Persist actual counts and whether each is helper/command/mounted-panel evidence. Keep true two-writer concurrency UNPROVEN unless actually tested; no new concurrency task in R4.

DATA / RUNTIME
No new schema/migration/DDL expected or authorized here. No new live order/parcel/plan/stock/hold/settings fixture, no business SQL, cleanup, restore, reset/reseed/backfill, provider/waybill/money/notification. User rows/draft and completed linked history read-only.
ONE canonical tooba_alpha5432 SingleStore/store-alpha FE3000->Host5088; no new DB/Docker/15432/15433 or edition/connection/port/appsettings/keys/CapturedPreview change.
Preserve R2 post-apply backup and missing pre-apply audit. Do not drop/reapply existing plan table. If a schema change appears necessary report exact reason; this repair's four defects should be fixable with code/contracts/state only.
Publish/restart changed services once after checks. Stop running Host before publishing over locked output; don't repeat known file-lock publish failure. Verify actual served BUILD_ID/chunk + health/ready once.
Read-only DOM/text/network current history page390/1440 if UI changed; no screenshots, no invented actionable example. No new live plan needed for proof.

HANDOFF / RECOVERY SOURCE OF TRUTH
Persist exact received TASK docs/ai/tasks/TB-SHIPPING-RESEND-REPLACEMENT-OWN-DISPATCH-PLAN-270-R4/TASK.md.
Evidence docs/architecture/evidence/TB-SHIPPING-RESEND-REPLACEMENT-OWN-DISPATCH-PLAN-270-R4/: TASK.md, RESULT.md, RECEIPTS.md, result-contract.txt, first-send-and-lifecycle-proof.md, command-gates-and-exact-handoff-proof.md, user-review.md.
Persist this R3 PARTIAL review at docs/architecture/evidence/TB-SHIPPING-RESEND-REPLACEMENT-OWN-DISPATCH-PLAN-270-R3/ARCHITECT-REVIEW.md. Preserve prior reports and nested R2 review as historical; no rewrite/relabel of failed proof.
Update top AGENTS/START-HERE/plan pointer: latest accepted R1; R2/R3 PARTIAL; R4 actual result awaiting review; 271 NOT ISSUED; next implementation NONE. No huge docs rewrite.
Record root/fix for the FOUR listed defects, exact named checks/counts, unchanged business/schema state, deploy/code/evidence/final SHA/parity and limits. One existing review URL explicitly history-only if no eligible parcel exists.
Normal commit/push/fetch parity, no force/amend/reset/rebase/stash, secrets or unrelated scripts.

CANONICAL RESULT / STOP
ONE complete final Result; no placeholder/probe/progress POST.
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Actual Task-ID, Parent-Task, Channel, Worker-Id, Agent-Type, Program, Mode, Bridge-Task-Row.
Status: PASS | PARTIAL | BLOCKED | RECOVERY_CONFLICT (actual value, no suffix)
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: 1ff02ab8a18c20999b92791f2b51520726bfa2d1
Base-SHA: actual verified work base
Code-SHA: actual implementation commit or NONE
Evidence-SHA: actual evidence commit or NONE
automaticNextImplementationTask: NONE
Body: repaired first-send ambiguity; new-confirm gates vs history; exact source/key handoff; lifecycle/CAS proof; targeted checks/deploy; data boundaries; changed paths/elapsed/finalHEAD/origin parity; review URL/limits.
Use real values, no placeholders. END_TOOBA_WORKER_RESULT then END_TOOBA_TASK, nothing follows.
PASS only when the listed repair cases are implemented/proven; Worker PASS != Architect ACCEPT, no user/live/provider/financial/universal shipping acceptance.
Complete lifecycle, return IDLE and STOP. No follow-up task/271/poll/wait loop.
STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
END_TOOBA_TASK