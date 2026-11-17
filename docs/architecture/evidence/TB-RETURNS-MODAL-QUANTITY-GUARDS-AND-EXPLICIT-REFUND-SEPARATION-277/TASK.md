PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-RETURNS-MODAL-QUANTITY-GUARDS-AND-EXPLICIT-REFUND-SEPARATION-277
Parent-Task: TB-RETURNS-CONNECTED-WORKFLOW-REPAIR-AND-USER-REVIEW-276
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 04aa77a5df260b3e76a115d607080a8e6c8ed004
Mode: SHARED_ACTION_MODALS_UNIT_BOUNDED_RECEIVING_AND_DECISION_ONLY_APPROVAL_WITH_EXPLICIT_FIRST_REFUND
STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

ONE TASK / USER INTENT
User tested276. They reject action forms appearing below the page, a free quantity field that accepts5 against a remaining ceiling1 and only fails after POST, inconsistent application notifications, and approval immediately executing refund.
Implement together:
1 professional existing-DS modals for every Returns action review;
2 per-product/per-unit quantities bounded by authoritative remaining quantity, including fractional weight units;
3 application-standard Toast/MessageBox;
4 REAL backend separation: approve return != start money; a separate deliberate financial action starts FIRST refund.
One worker/task/result, no delegation, screenshots or automatic next task. No scope limited to wording or opening a modal around the old combined approval.

READ / RECOVER
Read AGENTS standing user rules dated2026-11-17,276/ARCHITECT-REVIEW.md, existing Returns plan/recovery files, backend/frontend feature and architecture instructions, and affected owning source.
Parent276 was not final user acceptance. Retain all original worker/audit reports and prior bounded acceptances; new approval/refund separation is an explicit user behavior requirement. No rewriting old reports to claim it previously existed.
Expected base adds Architect docs to be47ee81ae0bf2b7e2678752741837ca60af0b70. Verify actual HEAD/origin/base; integrate normal incoming docs without discarding work, declare conflicts/deltas. No force/amend/reset/stash/rebase.
Existing case01a1e6b0-f33a-7000-80ee-beffb6344490 WAS EXERCISED BY THE USER after276. User screenshot shows received1 usable1, recorded approval and successful refund attempt, caseCompleted. Read latest owning facts;276's Requested/no-receipts/no-attempt report is OBSOLETE for this row. Keep history intact; no reset/reversal/clone/new fixture to recreate the earlier stage.

SOURCE-CONFIRMED ROOT

admin-return-workspace.tsx opens pending==receipt/approve/reject as inline sections. Receipt input is type-free text with inputMode=decimal; row label “قلم1”; remaining is only a small hint, no pre-send ceiling. UI states “در حال ثبت اقدام” even while only editing.
Workspace DOES import shared MessageBox/ToastRegion (and ToastRegion currently renders a small fixed dark message). User rejects the actual outcome presentation; don't falsely report no import. Find/reuse the application's intended shared command-feedback pattern; unify success/error/validation behavior instead of adding another notification layer.
Shared Dialog exists in design-system/primitives/overlays.tsx and is used by admin-shipment-plan-modal.tsx.
ReturnDirectory.ApproveAsync171–281 records approval, calls request.MarkRefundProcessing, BeginRefundAttempt and ExecuteRefundAsync; courier replay can call ResumeCourierApprovalAsync. Thus merely renaming the button cannot stop money.
Domain already has Approved distinct from RefundProcessing. ReturnRecoveryPolicy currently groups Approved with RefundProcessing and no attempt becomes attempt_unbound; ReturnWorkPolicy has no first-refund capability.
AdminReturnDetailQueryEngine currently enriches actual Order UnitCodeSnapshot via IOrderGridEnrichmentReader and Catalog titles; ReturnItemSnapshot exposes only label, not quantity precision. A label is not unit precision authority.
Preserve273 same-Pending-attempt recovery and276 deterministic failed-child retry; FIRST execution is neither recovery nor retry.

A — ONE CLEAN SHARED MODAL PER ACTION
Use existing DS Dialog and form/control primitives. No native browser prompt/confirm, custom portal/overlay framework or expandable footer cards.
Receipt, approve, reject, FIRST refund, pending reconciliation, failed retry and physical-only review each enter ONE purposeful modal (shared implementation with action-specific content is fine), never stacked. No new page or second action owner.

Clear title, actual product/unit/remaining or financial facts, concise explanation and deliberate confirm/cancel. Read state outside; edit fields inside modal. “در حال ثبت” only after send, not when a form opens.
Focus initial field, Escape/cancel/close/focus return per shared Dialog; constrained viewport/internal scroll, RTL, readable labels,44px controls, desktop and390px. No page jump or document overflow when opening. Don't globally restyle unrelated admin/design-system pages.
Closing before send writes nothing. Closing/remount during uncertain command preserves durable exact body/target/key; on reopen show actionable retained intent, not a fresh financial selection. Keep epoch/same-tick lock and compare-clear protections through modal close/unmount/retarget and follow-up read.
Correct the276 proven-no-effect list where it treats a later authorization loss/conflict/state refusal as proof an earlier send did not write. Keep possibly-sent intent until authoritative reconciliation. No “ignore” deletion of an unknown valid intent; unreadable legacy evidence cannot be silently deleted just to allow a new send.
After authoritative success, close correct modal, refresh owning state, publish ONE shared success notification. A failed field stays in modal with concise error and preserved values; command-level refusals use application-standard shared feedback once. No double Toast+MessageBox for one failure, raw codes, browser alert or custom ad-hoc toast. Repeated identical successes should still notify correctly; pending/refused is never called completed.
Audit existing approved admin Toast/MessageBox examples and cite the reused path in proof. If ToastRegion is the actual common primitive, use its established integration consistently; don't invent a global toast service just because its current style is simple. Correct this feature's use/presentation within shared patterns.

B — UNIT-AWARE BOUNDED RECEIVING

Show actual product title + localized unit beside every quantity control, clear “حداکثر قابل دریافت: …”, and min/step/precision from the owning unit policy. No “قلم1” as the only identity and no bare label “واحد” when unit is unknown.
Authoritative ceiling = requested minus all durable received portions for this RETURN ITEM, not order quantity/sibling capacity. Per modal usable+damaged totals share ONE remaining ceiling. Mixed portions and multiple partial occurrences must remain representable; never one condition funds another line.
Use an existing bounded quantity control if suitable, with local validity + disabled submit. For pieces disallow fractions when owning policy says whole units; for gram/kg/other permitted fractional units allow exact supported decimals up to persisted6, using real unit metadata. Do not hardcode all units step1 or permit/deny fractions by Persian label guessing.
Read canonical unit code/quantity rules through existing Contracts/quantity authority, prefer accepted Order snapshot when available; don't import Catalog/Order DbContexts or create a new UOM framework. If metadata is absent, expose honest unavailable/typed blocker or documented safe supported rule; no guessed precision and no silent fallback to whole pieces. Bounded remaining control must still work.
Persian/Arabic digits and decimal separator normalize exactly; no binary-floating ceil/epsilon, silent clamp/round or converting grams into pieces. Invalid empty/negative/non-numeric/excess/precision >allowed gets immediate inline explanation;5 with remaining1 never sends. Editing invalid transient text may remain visible for correction but must not become a valid request.
Keep initial arrival/inspection deliberate: displaying ceiling or placeholder is not evidence all goods arrived. Do not preselect Usable or auto-record maximum. User did not authorize fabricated inspection.
Server independently revalidates quantity/remaining/unit precision through owning rules after serialization/lock; client max is UX, not authority. Missing/stale evidence/over-receipt refuses with zero writes. Preserve exact occurrence replay before NEW receipt capacity checks; old exact replay answers durable fact after remaining becomes0.
Returned read model and controls reflect fresh partial receipts; don't use unit display enrichment as a new stock/payment authority.

C — APPROVAL IS DECISION ONLY, FIRST MONEY IS SEPARATE
This is a production backend boundary, not only frontend sequencing.
1 ApproveAsync and every reachable approve adapter/Order operation/replay must validate existing actor/seller/receipt/decision rules and persist ONLY the lawful approval decision + case Approved/fixed payment/destination binding as appropriate. No refund attempt creation, MarkRefundProcessing, gateway/Wallet call, Inventory restock, physical settlement or courier closure execution from approve. An exact recorded approval replays its recorded state with zero effect, including if money later progressed. Reject/reason semantics stay intact.
2 Audit direct callers and ReturnApproved event consumers/automatic hooks: no alternate route, event handler, legacy adapter or page may execute money merely because approval occurred. Don't retain a hidden “old combined” path.
3 Add a narrow explicit Returns CQRS command/typed validator/admin route + Contracts adapter for FIRST refund (e.g. StartReturnRefundCommand/start_refund). Existing admin authority/Result/catalog/localization conventions; no client-supplied amount/payment/customer/destination authority. The modal shows actual case, approved decision, server amount/currency, fixed destination and payment availability, and clearly says this step executes money.
4 Use the EXISTING Approved state for approved/no financial attempt. One shared stored-fact recovery/work description must classify it honestly as awaiting first refund, FinancialComplete=false, no pending/failed attempt id, active refund queue, positive CanStartRefund only when lawful. No-attempt Approved is not unknown money/attempt_unbound merely because approval is now separate. Missing decision/payment/receiving/coherent binding still blocks with typed reason; no new enum renumbering or invented completion.
5 First start checks authorization every call, recorded Approve, correct ownership, complete coherent ordinary receipt, owning payment readiness and fixed amount/currency/destination. Courier-origin must pass its existing source/claim/obligation/branch closure gates BEFORE any money effect; move required effectful prerequisites to this explicit route while preserving exclusivity/conservation. Approval cannot release claims/stock early. No external call while caller/ambient/enlisted/own transaction/lock is open.
6 First financial execution has ONE durable attempt identity per case using existing deterministic first-refund key/uniqueness. Persist attempt BEFORE external call. Duplicate click/lost response/another start key must never create a second first attempt. Existing recorded attempt is reconciled on its own stored identity, including terminal failed/succeeded; don't infer a fresh attempt from current status/count and don't send terminal attempts again.
7 Keep FIRST start vs resume SAME Pending vs deliberate NEW retry of a specific Failed child separate in Contracts/capabilities/UI. Pending unknown cannot be retried under a new key. Preserve273 outcome classification/no fabricated provider reference and276 parent-child replay.
8 Explicit refund success may continue the existing condition-safe physical disposition flow under its lawful gates; do not redesign that lifecycle. Approving alone leaves items undisposed and money not started. No live provider/Wallet/Inventory exercise for proof.
9 Status Completed must not be equated to financial+physical completion when stored dispositions are still None. User screenshot shows Completed/refund success and “باقی‌مانده” disposition; inspect actual persisted facts and shared recovery/work classification read-only. If coherent money success but stock unfinished, show active physical work/blocker, never “همه آثار ... بسته شده”/settled/history. Do not patch historical rows/backfill/infer SaleableRestock from usable receipt or label.
10 Existing shipped numeric enums/wire meaning/historical attempts stay truthful. Expect no migration: use current approval/status/attempt tables. If a concrete unavoidable schema gap is found, document it rather than applying unreviewed DDL. No automatic start on page load, approval success, refresh or queue selection.

D — FOCUSED CONNECTED VERIFICATION
Controlled test ports only for money/Inventory. No real financial test.
Backend named affected tests:

approve+replay across ordinary/courier => durable decision/Approved and ZERO new attempt/gateway/Wallet/Inventory/closure effects;
Approved/no attempt => active awaiting-first-refund read and exact positive/blocked capability matches command;
explicit start => one durable first identity; duplicate/lost response replay after Pending/Succeeded/Failed; unknown retains same key, missing/rejected/unreceived/foreign/unauthorized =>zero effects;
courier source/closure guards still before money; no event/caller hidden combined path;
confirmed money + missing disposition is NOT settled;
receipt whole/fractional supported units, partial/mixed shared ceiling, stale remaining, exact replay, precision and persistence unchanged.
Mounted REAL workspace/Dialogs with repository mount runner:
click action => dialog, no inline action form/no send/no fake loading; close returns focus, no overlapping modal;
actual product/unit, max1 entered5 blocks before request; permitted fractional gram amount accepted, disallowed fraction pieces refused, Persian decimal normalized, usable+damaged sum bounded;
approve confirm sends decision ONLY, refreshed awaiting-first-refund then distinct financial modal; request money occurs only on its explicit confirm;
shared single notification outcomes, validation/refusal retains field state;
storage verified before first send; double-click, uncertain close/remount, late answer/retarget, denied/conflict retain exact intent (affected276 proof).
Don't substitute regex tests. Run only named affected suites after final code, typecheck/build once and backend build/publish once; record real failed attempts/fixes if needed. No whole Returns/Host/AMSC/Shipping/Order/parity/baseline/stress suite. Any essential relational proof is canonical-target validated and rollback-only; no committed fixture/cleanup. No tests that assume approval still refunds.
Use existing accepted architecture guards for changed routes/catalog only if necessary; don't repair unrelated baseline failures.

E — ENVIRONMENT / USER CASE / SERVED REVIEW
ONE tooba_alpha127.0.0.1:5432 SingleStore/store-alpha, Host5088/FE3000; no new DB/Docker/15432/15433, edition/connection/ports/appsettings/key/CapturedPreview changes.
User case01a1e6b0-f33a-7000-80ee-beffb6344490 and source checkout01a1db2f-e36e-7000-b7d3-afb2fe86e81a are READ-ONLY. No new case/order/payment/receipt/decision/hold/stock/history, no refund/retry/recovery/notification/provider/waybill/money call, no reset/delete/restore/seed/backfill. No reversal of user's recorded successful refund and no second case solely to restore earlier demo state.
Graceful Host stop before publish; rebuild/restart changed Host andFE, loaded DLL/hash/health/ready + served BUILD_ID/manifest/relevant chunk. No unrelated deployment change.
Read real served queue + existing exercised case at1440/390 GET/DOM only. Show correct historical financial/physical state, readable product/unit, no ISO/IANA or raw provider-code spill, no document overflow. Open a real action modal only if genuinely offered; close without posting. If current settled/blocked history offers no edit action, use controlled mounted proof for modal variants; never advertise a live active stage or invent data. All native Dialog measurements must reflect actual shared component behavior.
No screenshots asked/taken. Preserve exercised case and return one primary real case review URL for user to recheck display. Clearly distinguish controlled modal workflow proof vs live history-only look; user must not repeat an already succeeded refund to test layout.

F — EVIDENCE / EXACT HANDOFF
TASK verbatim:
docs/ai/tasks/TB-RETURNS-MODAL-QUANTITY-GUARDS-AND-EXPLICIT-REFUND-SEPARATION-277/TASK.md
Evidence:
docs/architecture/evidence/TB-RETURNS-MODAL-QUANTITY-GUARDS-AND-EXPLICIT-REFUND-SEPARATION-277/
TASK.md, RESULT.md, RECEIPTS.md, result-contract.txt,
modal-and-quantity-proof.md, approval-vs-refund-proof.md, user-review.md.
AGENTS standing user rules already registered; preserve them. Update CURRENT AGENTS/START-HERE/RECOVERY/Returns plan with this actual behavior and remaining limits; original276 report/history unchanged.
Normal code/evidence commit+push, exact finalHEAD/origin parity with fresh fetch, served DLL/FE build identity. No self-referential final hash loop, force/amend/reset/stash/rebase, credential artifacts or scratch scripts.
Result must state:

approval no longer executes money (backend evidence, not screenshot);
real first-refund action vs unknown recovery vs failed retry;
reused shared modal/notification path and unit quantity authority;
existing live case was untouched; user test history reflects money already recorded;
no real PSP/Wallet/Inventory test, no user/financial/provider/full-Returns acceptance.
One review URL. No next task automatically, no loop; wait for result review and user look.

STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
END_TOOBA_TASK