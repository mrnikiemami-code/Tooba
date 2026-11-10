PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-DUE-WORKQUEUE-AND-OPERATIONAL-ORDER-VIEW-260
Parent-Task: TB-SHIPPING-OPERATIONAL-GROUP-PACKING-CHOICE-259
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: Tooba Shipping — due dispatch queue and shared operational order view
Mode: DURABLE_DISPATCH_DUE_QUEUE_EXACT_TARGET_AND_SHARED_FINANCE_FREE_OPERATIONS
Expected-Base-SHA: bb18f62d294e81ce86b8a09ae3eb8e2b5c84c873
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

## 1. Position / no loops
Read CURRENT TOPs, closure plan256–260,259/ARCHITECT-REVIEW.md,256 accepted dispatch-plan read seam,257 quantity authority,258 exception flow and259 packing outcomes; mandatory repository backend/frontend feature skills/design-system rules.
Save closed; issues1–4 accepted within bounded scope. ONLY remaining issue5 in this task. No automatically invented261/new feature. One worker, focused proof once, then STOP_FOR_ARCHITECT for final user handoff review.
User explicitly forbids loops. Do not rerun whole module/solution tests, base-worktree parity suites, screenshot matrices or repeated successful checks. After relevant tests/build pass, deploy and return. Fix a concrete introduced failure, rerun only its affected checks. Disclose unrelated reds without cleanup/rebaseline.
Verify actual HEAD/origin and safe worktree; issue/review doc descendants legitimate. No force/reset/amend/stash/rebase/unrelated overwrite.

## 2. User outcome
/admin/fulfillments becomes a useful work queue, not a dump of every historical seller order.
Operator sees what must be prepared/handed to carrier now/today, what's overdue, and what is in transit awaiting customer delivery. Opening an entry shows the EXISTING shipping/packing/parcel operations of that exact order/group/package, WITHOUT financial/payment/settlement panels.
Do not implement another lifecycle/form alongside the order page. Both entry points use the same feature-owned operational component/action wiring, server capabilities, dialogs and retained envelopes. Full order detail remains accessible as a separate link.

## 3. Authoritative dispatch timing — never confuse customer promise
Consume ONLY Order-owned durable accepted PlannedDispatch from256 history/read projection: handoff date/open/close/carrier timezone, schedule snapshot/source revision/policy and effective MAX. No current Shipping settings lookup or reverse arithmetic in browser.
Receiving date/hour is CUSTOMER delivery commitment, never dispatch deadline.
Use injected nowUTC and explicit carrier timezone, one server authority for classification/filter/count/sort. Define and pin boundaries:
- «سررسیدشده»: unfulfilled dispatch work whose handoff window has opened, including overdue;
- «ارسال امروز»: pending handoff with local accepted handoff date today, including not-yet-open windows, clearly labelled upcoming vs ready;
- «عقب‌افتاده»: pending work now >= accepted carrier close instant, or prior handoff date;
- «آینده»: pending windows not yet open (available as filter, not falsely actionable by timing alone);
- «در مسیر»: actually dispatched/in-transit/non-delivered states as appropriate, with permitted delivery/exception actions; not in pending-dispatch counts;
- historical null plan => «موعد ارسال مشخص نشده», separate visible filter/section; never infer from customer date/current service or silently omit it.
Today/date and open/close comparisons are in accepted carrier timezone converted to instants; [open,close) matches256. No browser clock guessing or server-local date shortcuts.
Timing urgency is independent from operational readiness: due but unpacked/untracked remains visible with truthful next step/blocker; due does NOT bypass packing/payment/owner/provider/permission rules. Future date alone is not a new domain dispatch prohibition unless one already exists.
Cancelled/delivered entries excluded from default active queue, accessible via explicit history/status filters. Zero remaining fresh quantity after257 must not create phantom pending-dispatch work; existing Created parcels still represent dispatch work.259 separate children and common parent must count physical work once.

## 4. Queue identity / correct target / pagination
Inspect existing queue API/projection/filters and reuse owning Contracts; no Host domain logic or cross-module DB reads.
Keep exact checkout/seller/accepted-group/operational-group/parcel/package locators as needed. One common package action owner, no duplicated child row actions/count inflation; separate parcels remain separately actionable within their one compatible group.
Where work has no parcel yet, target exact group/lines and existing next operation; never bulk-select every seller order merely because its date matches.
Show concise columns: order reference/customer, responsible party/seller where useful, method, «موعد ارسال», «تحویل مشتری» separately, physical parcel/package/status, next action or precise blocker. Hide technical tenant/enum/provenance. Essential titles wrap.
Provide search and useful limited filters (urgency/date/status/method/responsibility as supported), genuine SERVER pagination + total/counts consistent with full filtered set, stable ordering (overdue earliest deadline first then due then upcoming with deterministic tie-breaker). Do not filter one fetched page and pretend counts represent all records.
Avoid checkbox/bulk-action clutter unless an existing safe bulk operation is actually used. No new batch dispatch system.
Loading/empty/error states concise; stale data never authorizes mutation. Successful action refreshes detail and queue counts; a lost response keeps exact prior occurrence/envelope and reconcile entry.

## 5. Shared professional operational view
Opening queue row/eye should directly show relevant «عملیات ارسال سفارش» with target group/parcel/package visible/focused, not force scrolling through finance and order history. A dedicated existing-style route or shared operational panel is acceptable; reuse same component/commands as full order.
Include only useful recipient/address, group items/readiness, receiving+dispatch rows, parcels/packages and lawful contextual actions. No payment tables, seller settlement, refunds/deposits or technical diagnostic timeline. Operational payment lock may show concise blocker only.
Existing full order page still shows its financial sections; no global removal.
Preserve one action owner, approved purposeful dialogs, 44px controls, RTL, full wrapped titles, no horizontal document overflow at1440/1024/390. Clean navigation/back to queue preserves filters/page. No new global CSS or app/admin flat-folder debt; use public feature index and current design system.
Support257 tracking,258 non-delivery/final receipt/resend and259 common/separate shapes through shared existing capabilities. No fake delivery or post-dispatch cancel. Parent package return remains explicitly unsupported/locked; do not create it here.

## 6. Focused verification — ONE canonical database
Only tooba_alpha5432 + FE3000/Host5088 SingleStore/store-alpha. NO additional/disposable/test/Docker DB,15433 or edition/connection switch.
Real user order/cart/payment/parcel/service rows read-only. Minimal clearly task-labelled production-domain/application fixtures on SAME DB authorized for timing/shape proof; record IDs. No raw business SQL/manual DDL/reset/reseed/backfill/sweeping delete, no computer clock change.
Required cases:
A deterministic authority tests before open/exact open/before close/exact close, previous/next local day, cross-zone midnight, null plan, cancelled/delivered, due but not ready.
B owning real query + pagination: due/overdue/upcoming/unspecified filters/counts across more than one page; old customer date does NOT fabricate dispatch due; stable tie order. Counts and physical common/separate rows not duplicated.
C mounted shared operational view from queue locator: exact target, finance absent, same component/actions as order, correct next step; unauthorized/no readiness no mutation button. Retained uncertain attempt/reconcile remains reachable.
D real read-only browser navigation on canonical queue and known existing order01a1b92d-e97e-7000-999a-143350d0edaf: legacy null-plan truthful, exact target link, finance-free operations; RTL/44px/no overflow at three widths. No business click on user records. No screenshots needed.
Existing canonical orders may all have null plans. Do NOT fake dates/backfill or claim live plan-bearing checkout. Controlled fixture proof supplies plan-bearing cases; disclose layers and runtime limits.
Run affected focused tests, typecheck/build/publish only. No whole-suite parity or repeated green loops.

## 7. Deploy / constraints
Source D:\Users\User\source\repos\SarvNewVer; deploy changed components on canonical ports. Verify served BUILD_ID/API/health and same DB/edition.
No schema change expected for read queue. If essential, minimal additive owning migration after private backup via repository MigrationRunner only; explain need, old data readable.
Preserve DataProtection, local CapturedPreview ON/7dates and checked-in OFF,256 MAX/cutoff/two calendars/save,257 quantity/tracking,258 returned capacity/audit and259 durable packing mode.
255 remains parked; live Tipax deferred. No provider calls/credentials/fake waybills. Do not restart a parallel runtime.

## 8. Evidence / final milestone
Persist TASK verbatim docs/ai/tasks/TB-SHIPPING-DUE-WORKQUEUE-AND-OPERATIONAL-ORDER-VIEW-260/TASK.md and TASK/RESULT/RECEIPTS/result-contract with focused proof under docs/architecture/evidence/TB-SHIPPING-DUE-WORKQUEUE-AND-OPERATIONAL-ORDER-VIEW-260/.
Update AGENTS.md, START-HERE-IF-CHATGPT-IS-LOST.md, SHIPPING-ARCHITECT-RECOVERY-2026-10-22.md and closure plan: issues1–4 accepted, issue5 pending Architect review; worker PASS not final acceptance. Clear final user trial route/checklist covering five repaired defects with honest fixture-vs-live limits; no new task proposed.
Return BEGIN_TOOBA_WORKER_RESULT with full Base/Code/Merge/Evidence SHAs, HEAD==origin/main, changed paths, exact filter/time semantics, query/pagination proof, fixture IDs, shared view reuse proof, runtime/build, brief user review URLs and limits. No broad-suite/visual/provider claims beyond evidence.
STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
END_TOOBA_TASK
