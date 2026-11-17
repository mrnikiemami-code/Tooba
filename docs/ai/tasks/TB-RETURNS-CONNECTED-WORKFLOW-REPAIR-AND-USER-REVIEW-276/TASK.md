PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-RETURNS-CONNECTED-WORKFLOW-REPAIR-AND-USER-REVIEW-276
Parent-Task: TB-RETURNS-OPERATOR-WORKFLOW-QUEUE-DETAIL-AND-MOBILE-275
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 5fbdea3394d0636ae62e5ae06bab95e432563da2
Mode: CONNECTED_RETURNS_REPAIR_AND_ONE_LAWFUL_USER_REVIEW_CASE
STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

USER INTENT / ONE TASK
Complete the connected operator work left by275 AND leave ONE real, lawful return case visible so the user can inspect the populated queue/detail on desktop/mobile at the end. This is NOT a case-only fixture task. One worker, one task, no delegation, no screenshots, no background retry/continuation loop. Finish this task and wait for the user's test and Architect review; do not issue/start another task.

ARCHITECT CHECKPOINT
Read:

docs/architecture/evidence/TB-RETURNS-OPERATOR-WORKFLOW-QUEUE-DETAIL-AND-MOBILE-275/ARCHITECT-REVIEW.md
the275 TASK and evidence, existing backend/frontend architecture instructions, current recovery files and Returns272–276 plan.
275 = PARTIAL / NOT ACCEPTED as a connected workflow. Preserve its original worker report. Retain accepted272-R1/273-R2/274-R1 and shipping history, backups, migration/audit limits. No broad re-audit or reopening of settled modules.
Delivered275 source abc0bad78e1b9a5c98cde8887c62d7dbc6078f28, posting head a780ee38ac56711ee0d3b6fc66cec0d9c8a46091; expected base above adds Architect docs only.
Recover actual main/origin with normal fetch/fast-forward when applicable; inspect incoming files, preserve unrelated dirty/untracked work, declare actual base and conflicts. No reset/stash/rebase/amend/force. Never silently substitute a missing base.

SOURCE-CONFIRMED ROOTS
1 admin-return-workspace.tsx finish152–174 clears the stored intent BEFORE testing answered.state; run205–206 invokes it for every AdminResult. null is treated as success. “نادیده گرفتن”292–300 deletes possibly-sent intent without reconciliation.
2 return-review-intent.ts persists only setItem(JSON.stringify(intent)), no verified read-back; clear compares key only; reader accepts any action string/missing body. dispatch uses current snapshot.checkoutId, not frozen reviewed transport context.
3 epoch fences initial GET116–126 only; run/finish/load/finally110–212 are unfenced. Retarget/unmount + late mutation/refetch can overwrite a newer surface/lock; unresolved intent can be abandoned/overwritten.
4 RetryRefundAsync394–406 enforces RefundFailed + CURRENT un-retried leaf BEFORE finding deterministic child412. Exact old intent therefore refuses after Pending/Completed, and its parent is excluded after a failed child. ExpectedFailedAttemptId null selects the current leaf. The “duplicate success” test explicitly expects RefundRetryInvalidState instead of replay.
5 The21 FE node tests check helpers/source regex, not mounted workspace lifecycle/action behavior. They are not mounted proof. No live case exists; empty/not-found pages do not prove the populated workflow.

A — EXACT DURABLE REVIEW THROUGH THE REAL WORKSPACE
Use the existing Returns helper/workspace; no new generic transaction/UI/storage framework.

Capture immutable target case + owning checkout/route + reviewed action + occurrence + EXACT serialized transport body before FIRST possible send. The actual request must use those bytes/identities; don't rebuild from a newer snapshot/form. Persist AND verify exact read-back before invoking the executor. Storage getter/set/read-back failure => clear Persian explanation, ZERO request; no render crash from sessionStorage getter.
Treat every possibly-sent stored envelope as unresolved until authoritative reconciliation. Restore on remount even if fresh detail/preview fails or action capability changes. Validate known action/body/target/version; corrupt/unsupported stored data must be visible as unreadable pending evidence and must not silently permit replacement/sending arbitrary action.
Unknown/transport/5xx/conflict/denied is NOT proof of no write. Keep frozen intent, visible status, manual exact retry/reconcile. Remove the unconditional finish-clear and unsafe ignore path. No new financial/physical intent may replace it simply because UI reloads or a request times out.
A successful authoritative command answer resolves that exact reviewed command; it need not mean money/physical completion. Refresh from the owning read and show its real state. null/malformed/no checkout/no executor is never success.
Where the existing contract proves refusal before all writes/effects, offer a deliberate new review with the previous outcome preserved; use a small explicit auditable rule, not all4xx or message matching. Otherwise keep unresolved. No auto fresh occurrence, no arbitrary “dismiss uncertainty”.
Compare-and-clear/update must match case/route/action/occurrence AND exact body; a late answer may never erase a newer envelope.
Capture presentation epoch for every mutation and follow-up read, fence all setState/notice/toast/pending/close/finally and cleanup on unmount/retarget. Fence stale detail reads too. Keep same-tick serialization; newer surface's pending lock cannot be released by old finally. Storage reconciliation of an old exact command must not mutate newer UI.
Keep server capability ownership; no browser-made permissions, optimistic money/restock facts or automatic polling. Audit remaining Order return menus: navigation to this owner may remain, duplicate blind approve/retry forms must not survive. Change only a confirmed remaining duplicate entry.

B — EXACT FAILED-REFUND RETRY REPLAYS ITS CHILD
Retain deterministic ReturnRetryIdentity and273 outcome/evidence/transaction authorities; no schema or new engine.

Require explicit nonempty ExpectedFailedAttemptId on every reachable retry boundary, including legacy adapter/caller. Missing/empty/foreign/incoherent => existing typed refusal and zero new attempt/effect. Never select current/latest failure for an unreviewed caller.
After authorization + owning case/intent binding, locate the exact reviewed parent and its deterministic child BEFORE gates for a NEW attempt. Stored terminal child answers from storage, including Succeeded and Failed, without gateway/Wallet/Inventory re-execution or a grandchild. Stored Pending child reconciles that same child/key through the existing lawful pending recovery authority; no new key/attempt, no default destination, no re-approval. Enforce present effectful gates when actually making an effect.
If NO child exists, only the one coherent positively bound failed leaf may begin a NEW retry after existing receiving/approval/payment/evidence gates. A stale/foreign/ambiguous parent cannot mint a sibling; a pending/unknown parent is never a definite-failure retry.
After child Failed, replay(parent) still returns that child; deliberate NEW review(child) alone may create its deterministic child. Two intents must stay distinguishable.
Keep provider key/payment/amount/currency/destination binding exact, no external effect inside caller/ambient/enlisted/own transaction. Narrow unique collision handling to real owning PK schema/table/constraint and re-read winner through the same rule; no broad catch/global tracker clear. Do not claim one provider HTTP call or true two-writer proof from sequential tests.
Update misleading duplicate-success test to prove replay, not refusal. Preserve existing physical-only recovery route as its separate intent; don't use retry_refund as an unreviewed physical bypass.

C — CONNECTED PROOF, NOT SOURCE-REGEX SUBSTITUTES
Add focused tests with the repository's ACTUAL mount runner and real AdminReturnWorkspace component. Mock only HTTP/storage/owning external ports as appropriate; don't write another implementation as the test subject.
Backend cases through real ReturnDirectory: explicit identity required; fresh child; duplicate after Pending/Succeeded/Failed; parent replay vs deliberate child review; foreign/stale/ambiguous refusal; zero extra attempts and correct effects/key. Retained affected275 cases once after final change.
Mounted FE cases: first-send durable read-back precedes executor; throwing/silent-discarding storage zero requests; same-tick double click one send; unknown/409/503 keeps exact bytes and prevents replacement; remount+failed GET still recovers; success exact clears only matching body/target; null/malformed response not success; late mutation/detail after retarget/unmount can't overwrite/clear newer UI/intent/lock. Exercise receipt explicit quantity+condition and owning approve/reject/recovery/retry route envelopes with controlled responses only. Do not perform actual money/stock actions.
Retain affected API/queue mapping checks as needed. Do not call helper/string tests mounted proof. Run only named affected suites once after final implementation; if test/code needs fixing, record failed attempt and rerun affected checks, not an artificial “once” claim.
Typecheck + FE production build once; backend build/publish once after final source change. No unfiltered Returns/Host/Shipping/Order/AMSC/parity/baseline/stress/detached-worktree suites. No new relational harness unless essential to changed persistence; any such proof rollback-only, canonical target validated, no committed test fixtures/delete/cleanup. No schema change.

D — ONE LAWFUL USER REVIEW CASE, AFTER REPAIR/DEPLOY
NEW EXPLICIT USER AUTHORIZATION: “یک پرونده ایجاد کن من ببینم ظاهر ... این کار کنار جلو بردن باشد ... پایان تسک بعدی من تست کنم”.
Earlier blanket “no live return case” instructions are overridden ONLY for this one ordinary CustomerDelivered Requested return and its owning items/normal event records. Do not stop merely because previous tasks forbade fixtures. Do NOT turn this into a standalone seed/demo task.
1 Read actual owning eligibility/order facts and existing returns. Prefer an existing eligible delivered trial order; candidate checkout history reference is01a1db2f-e36e-7000-b7d3-afb2fe86e81a, but eligibility is NOT asserted and never inferred from a URL or a sibling parcel. Use current owning reads for delivery, customer/seller binding, window, refundable amount/currency/destination, available quantity and unit precision. Pick a small lawful quantity (no fractional piece trick).
2 Create ONE ordinary case through the EXISTING authorized owning command/API, server-derived actor/ownership/amount/eligibility. Stable task occurrence/idempotency key, clear reason “پروندهٔ بررسی رابط کاربری مرجوعی — TB-276”; retain exact envelope and replay if the creation response is lost. First look up/reconcile any already-created same task case before another send. Never create a second case to improve appearance.
3 This authorization permits occupying that case's requested return capacity. It does NOT permit new orders/payments/Delivered transitions, fabricated receipts/inspection, approval/rejection/refund/retry/recovery execution, stock/hold/reservation changes, fake provider success, customer notifications or date/window changes. No raw SQL/seed/DbContext bypass, no disabling guards. No customer details/secrets in public output.
4 Leave case Requested, zero receipt occurrences/decisions/refund attempts/dispositions. Do not pre-fill “arrived/usable” or manufacture rich history. The real request and product facts populate the page; absent physical/money history must read honestly. Show receiving form without saving; the user can inspect fields and layout.
5 If current data has no lawful eligible candidate, complete the connected repairs and report the exact owning eligibility blocker with safe read evidence. Do not fake readiness or create a paid/delivered order to get around it. Result must then be PARTIAL / USER_REVIEW_CASE_BLOCKED, not “ready for user test”. No scope expansion/another task automatically.
6 Record before/after targeted counts and exact created case ID/human locator/checkout association. Assert ONE new/reconciled task case + its exact items only, receipt/decision/attempt counts0, source order/payment/shipment facts unchanged. Expected normal Returns events/outbox if any must be disclosed; don't claim “zero live business write”. Preserve the case for the user's test; no cleanup/delete/reject/reset.

E — ACTUALLY SERVED POPULATED MOBILE/DESKTOP REVIEW
ONE tooba_alpha127.0.0.1:5432 SingleStore/store-alpha, Host5088, Next3000. No Docker/secondDB/15432/15433, edition/connection/ports/appsettings/keys/CapturedPreview change.
Graceful Host stop before overwriting locked output, publish/restart once, health/ready and loaded artifact identity. FE rebuild/restart and verify served BUILD_ID + manifest/relevant chunk matches final code.
Read-only real served /fa/admin/returns AND /admin/returns, then real created case detail at390 and1440. Query/counters must include the case in its truthful stage with correct page/strip parity and stable locator; dates Persian/Jalali via shared accepted-zone helpers, per-line product/quantity/unit, money vs physical status separate. No raw GUID as main reference, raw ISO/IANA spill, invented success/stock/promise.
Inspect receipt form open/close and responsive geometry WITHOUT saving. Fix local clipping/unreadable hierarchy/overflow exposed by the populated case (existing design system; one-column mobile action/form layout,44px new controls, scoped table scroll). Empty/not-found check is supplementary and cannot substitute for populated proof. No screenshots requested/taken or screenshot artifacts.
No PSP/Wallet/Inventory/provider/waybill/notification/money operation. Controlled mount tests are not a live operational trial; user visual/functional acceptance remains pending.

F — HANDOFF / STOP
Persist THIS TASK VERBATIM:
docs/ai/tasks/TB-RETURNS-CONNECTED-WORKFLOW-REPAIR-AND-USER-REVIEW-276/TASK.md
Evidence under docs/architecture/evidence/TB-RETURNS-CONNECTED-WORKFLOW-REPAIR-AND-USER-REVIEW-276/:
TASK.md, RESULT.md, RECEIPTS.md, result-contract.txt,
connected-intent-and-retry-proof.md, user-review-case-proof.md, user-review.md.
Keep275 ARCHITECT-REVIEW and all prior reports/history unchanged. Update AGENTS/START-HERE/RECOVERY/Returns plan with accurate repair status + created case locator + waiting for USER review, not user acceptance.
Result must distinguish: code repair proof; actual case creation/replay; read-only served DOM/form checks; controlled money/stock tests; remaining provider/concurrency limits. Include exact Code/Evidence/Final HEAD, normal push/fresh origin parity and served build/dll identity. Don't pin a self-referential commit hash inside its own file; report final real SHA in final Bridge body, no endless poster-only commits.
No force/amend/reset/stash/rebase, credential artifacts or unrelated scratch scripts.
Give ONE primary review URL: the REAL created case detail (if blocked, explicitly no populated review URL). Mention queue navigation within user-review.md. Brief Persian guidance: inspect queue/detail on phone/desktop and open receipt form; no money/physical action has been executed by worker. Leave this case intact.
Worker PASS != Architect ACCEPT != user VISUAL/FUNCTIONAL ACCEPT. No provider/financial/full-Returns/shipping completeness claim. No next task/loop; finish and wait.

STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
END_TOOBA_TASK