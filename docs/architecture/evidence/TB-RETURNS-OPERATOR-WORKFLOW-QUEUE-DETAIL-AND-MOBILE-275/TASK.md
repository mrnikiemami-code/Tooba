PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-RETURNS-OPERATOR-WORKFLOW-QUEUE-DETAIL-AND-MOBILE-275
Parent-Task: TB-RETURNS-CUSTOMER-RECEIPT-INSPECTION-AND-REFUND-GATES-274-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 79258250c87c313cf778a73ab94e8eb4be45d19e
Mode: ONE_OPERATOR_RETURNS_WORKFLOW_WITH_AUTHORITATIVE_QUEUE_DETAIL_REVIEW_AND_MOBILE
STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

USER INTENT / CURRENT CHECKPOINT
Finish the EXISTING /admin/returns operator experience, now over the272 creation,273 recovery and274 receipt/inspection foundations. A working backend alone is not the requested screen.
274-R1 ACCEPT within its bounded repair scope and declared limits; read its ARCHITECT-REVIEW.md. Original PARTIAL reviews/deviations remain history.
ONLY275 is issued.276 NOT ISSUED. One worker/task, no delegation, screenshot request/capture/artifact or auto-next implementation loop.
This task authorizes UI + the minimal read/capability and reviewed-retry/partial-receipt handoff changes below, NOT another lifecycle/provider/schema project.

BASE / ARCHITECTURE
Fetch normally; expected base exists and is ancestor of HEAD/origin/main; record actual base/incoming delta.
Expected base is the Architect docs-only acceptance/issue commit after5aaff422fb83095c885afaf372e610a3da8f0a2d.
Read TOP AGENTS/START-HERE/RECOVERY, both .cursor/skills/tooba-frontend-feature/SKILL.md and tooba-backend-feature/SKILL.md, docs/architecture/FRONTEND-ARCHITECTURE-STANDARD.md and relevant feature/route/Contracts/tests.
Reuse existing route/page composition, typed clients, feature/state/UI boundaries, AppDataGrid/Dialog/Toast/MessageBox and tokens. No new design system/store/network/formatting framework, mass moves or oversized mixed screen. Don't expand shared storefront dependencies/visual locks.
Unsafe overlap/divergence -> RECOVERY_CONFLICT; no reset/stash/rebase/amend/force or unrelated loss.

SOURCE-CONFIRMED UI GAP
admin-returns-work-queue-screen.tsx is still the old status-only grid + competing generic financial menu; no receipt/recovery phase, amount/destination or reliable counts.
AdminReturnDetailScreen in admin-screens.tsx renders ReturnDetailCard plus an unguarded status==RefundFailed retry button.
return-api.ts ignores Recovery/Receiving/DecisionReason/SaleableRestockedQuantity; absent destination/condition/amount can normalize to OriginalPayment/Usable/0.
AdminReturnGridQueryEngine uses first item's label/unit plus summed mixed quantities, RET-orderNumber repeats per case, and "refund_needed" means Approved WITHOUT receipt evidence.
Do not place new behavior into the old status-only generic path and call the experience complete.

A — ONE RETURNS-OWNED WORK DESCRIPTION FOR QUEUE AND DETAIL
Add/reuse a cohesive pure Returns read/capability description over current STORED status/decision/receipts/items/attempts and the accepted summary/recovery authorities.
Classify work, not a new lifecycle: awaiting receiving, decision/review, pending/unknown money, definite failure, unfinished physical resolution, settled/rejected-with-no-open-work history. Inconsistent/blocked ACTIVE work stays visible with the exact blocker; never hide it because no executable action exists.
Reuse ReturnReceivingSummary/ReturnRecoveryPolicy and owning preconditions; don't duplicate a second permissive business engine in FE or SQL helpers. Mutation always revalidates actor/scope and live facts.
Expose server next step, work category, blocked reason and explicit review/action capabilities. History requires no remaining physical/financial obligation: money Completed with missing stock binding/unfinished disposition is ACTIVE; rejected goods still held remain unresolved physical work.
No customer promise/warehouse deadline/today/filter/time calculation or provider read is invented.

QUEUE:
One primary strip: همهٔ کارهای باز + دریافت کالا + بررسی و تصمیم + پیگیری بازپرداخت + بازپرداخت ناموفق + تعیین تکلیف کالا, with quiet سابقه (or equally clear existing tab styling). These are mutually assigned work selections over the authoritative description; no overlapping hidden legacy bucket. No new time-scope strip.
Counts/page/total/history must come from ONE common constrained set BEFORE paging, after common search/column/advanced filters. No page-derived totals, stage-dependent archive count or second classification read. Missing counts show unknown, not invented0.
The page contains exactly the selection counted. History badge is stable across active selections with common constraints. Old queueFilter links normalize ONCE to the new selection; no silent old+new filter combination. Unsupported computed-field advanced/sort selections must be refused/disabled rather than silently ignored.
Maintain existing supported explicit sorts/saved views/search and URL/back-forward state; returning from detail restores selection/search/page as appropriate.
Query in batches over owning facts; no per-row Directory/Get/Payment/provider call, no full Order orchestrator dependency cycle. Foreign product/party/order enrichment via existing Contracts. Do not truncate candidates at a fetched page or an arbitrary500-row limit to make a count look plausible; disclose performance limits, not false scalability.

DISPLAY FACTS:

distinct stable human case locator within its parent order, independent of current grid page/sort/filter (no raw GUID and no two cases with the same RET-orderNumber label). Derive from existing immutable facts; no new durable numbering registry/schema.
order/customer/seller and product names/units from actual existing lookup/snapshots; no GUID suffix masquerading as a product.
multiple products display "N قلم" with per-line names/quantities/units in detail; NEVER sum incompatible units under the first item's unit.
recorded amount + real currency + actual destination, origin (customer vs courier), receiving progress, separate money/physical phase, next step/blocker.
no raw enum/error/IANA/ISO code in normal operator copy; no guessed currency/destination/condition/0 amount. Unknown is explicit and cannot authorize a financial review.
remove misleading first-line eligibility countdown; captured per-line policy may be read without recomputing the already accepted request's eligibility from current settings.
Keep API wire facts/query/date keys unchanged. Additive read DTO/enrichment only.

B — ONE ACTION OWNER: THE EXISTING RETURN DETAIL
/admin/returns/{returnRequestId} becomes the sole admin receiving/decision/recovery workspace; extract cohesive admin feature components/hooks from admin-screens.tsx, which can keep a compatibility re-export.
Queue is read-only with ONE clear "بررسی مرجوعی" link per row (and existing order navigation if useful); retire its financial kebab mutations.
Other admin Order/Returns entry points route the user to THIS case workspace instead of offering a second blind approval/rejection/refund dialog. Preserve unrelated shipping/order operations.

DETAIL CONTENT:
Compact human case/order header; origin and actual receiving source; money amount/currency/destination; receiving/financial/physical facts in a clear order.
Per product show requested, received, remaining, inspected usable/damaged and SaleableRestockedQuantity distinctly. Legacy default Usable does not prove inspection; a mixed item never says its entire requested amount is saleable.
Receipt history shows real occurrences, recorded time/actor label when available, portions/conditions and remaining quantity. Keep technical ids/keys internal; don't invent an actor name.
Customer's request reason and winning operator decision reason are distinct. Refund-attempt history is quiet, with recorded outcome and truthful absent PSP reference. No payment success merely from a status chip.
Show exact blockers in plain Persian and only real remedies. No invented force-approve/restock/return-to-customer button. Clear loading/empty/denied/error/retry and history-only states.

ACTIONS (only server-described review capabilities):
1 "ثبت دریافت و بررسی": choose actual quantities from remaining amounts; explicit usable/damaged selections, no preselected "all arrived/all usable". Allow legitimate mixed/partial portions and localized decimal input; preserve exactly6-decimal semantics without binary-float rounding into different goods. Final review lists exact product portions; submit actual /receipt command.
2 approve: deliberate review of THIS case, complete receiving, exact stored amount/currency/destination and effect. Use existing owning approve path (Order operations -> Returns Contracts); no new duplicate financial authority.
3 reject: meaningful reason required, review then existing rejection command with exact reason; warning about already-received goods is factual, no invented stock action.
4 pending/unknown money: "پیگیری نتیجه بازپرداخت" uses /recover kind resume_pending_refund + exact STORED expectedRefundAttemptId. No new attempt/key or destination substitution.
5 physical-only: /recover kind settle_physical_disposition, labelled as goods work,0money/new attempt; missing stock binding/received-but-rejected remains a readable blocker.
6 definite failure: a NEW reviewed retry, clearly different from unknown reconciliation; safe server identity requirement in C.
After a command, refresh authoritative return/work reads. HTTP200 while pending/blocked is not "وجه برگشت داده شد"; communicate returned actual phase. No optimistic dates/money/stock/completion.

C — TWO MINIMAL SERVER HANDOFF FIXES REQUIRED BY THIS UI
C1 LEGACY PARTIAL RECEIPT CONTINUATION:
RecordReceiptAsync currently blocks a further NEW occurrence on a financially progressed ordinary case solely because any receipt header already exists. This can strand its first partial receiving.
Align one receipt-recording precondition/capability with the owning write: while no requested item has physical disposition and cumulative receiving is incomplete/coherent, a further partial occurrence is lawful even on that legacy progressed case. Keep all origin/state/owner/line/cumulative caps and exact replay protections.
Once receiving is complete or ANY physical settlement/history exists, don't allow a new receipt to change the usable amount behind the stable stock key. Rejected/cancelled history never receives NEW goods through this command.
Prove a progressed case can record partial1 then partial2 to exact coverage with0external effects, over-receive refused, settled case remains locked. Do not manufacture legacy live rows.

C2 REVIEWED DEFINITE-FAILURE RETRY, NOT BLIND NEW MONEY:
The existing retry-refund endpoint takes no reviewed failed-attempt id and uses count-based new keys. Don't expose that as a safe remount/retry operation.
Add the smallest server-bound reviewed identity (expectedFailedAttemptId) to the existing owning retry request/endpoint/adapter as needed. Server derives/validates actual case, permission, failed-attempt binding, amount/currency/payment/destination and receiving gate; client supplies none of those authorities.
Exactly ONE child retry intent may derive from that reviewed failed attempt: use a deterministic key from case+failedAttempt identity with existing attempt storage/unique key (no schema), re-read/replay its recorded child on the same reviewed request. A lost response or duplicate POST must not mint another counter-based retry.
Unknown/Pending or Succeeded is NEVER a new-retry source. A fresh retry after a NEW child definitively fails requires a NEW deliberate review bound to THAT child. Ambiguous legacy history is a typed review/blocker, not arbitrary first/latest fallback.
Use the same current-failure/lineage rule in read capability and submit; known derived child identities can prove which current failure is reviewable. Preserve old attempt history. No new financial engine/table/provider API.
Any older admin/menu caller lacking the reviewed identity must route to review or fail closed; never silently generate one or keep a bypass.
Prove same reviewed failure+duplicate/lost answer replays ONE child; changed/stale/incoherent source refuses or returns its own recorded child with0fresh money; next definite failed child needs explicit review; Pending recovery keeps its existing key. Database fault handling stays exact/scoped; no global tracker clear or external call inside a transaction.
This is a bounded retry-command identity fix needed for real UI behavior, not a provider/financial-formula audit.

D — DURABLE REVIEW BEFORE SEND; ONE INTENT UNTIL AUTHORITATIVE RESOLUTION
Use existing local patterns, with a small Returns-owned intent helper/hook, not a generic app-wide framework.
Before the FIRST possible request, persist AND verify the exact serialized body/target/action/review identity in session storage (or the existing approved session mechanism). Storage failure -> no send.
Same-tick double click -> one executor. Unknown timeout/5xx/unmount/reload/route switch retains unresolved exact intent; edits/new receipt occurrence/new retry identity must not replace a possibly-sent intent.
A success/replayed owning result or a genuinely proven no-write refusal may resolve it. Conflict/authorization loss/arbitrary4xx alone is not proof no prior effect occurred.
Manual reconciliation/refetch and EXACT-body retry remain available; no background polling/auto-retry loop. Receipt retries keep the same occurrence key and portions; money recovery keeps expected attempt; failed-money retry keeps the reviewed failed parent; reject keeps the exact reason.
For uncertain approval, reconcile owning decision/attempt facts rather than automatically creating a fresh financial intent; the existing approval/refund identity must never be rekeyed.
Closing/remounting/changing target increments a presentation epoch. Late responses/finally cannot close a newer dialog, clear/update another pending intent or refresh the wrong case. Use compare-and-clear/update on exact scope+body/key.
After authoritative result, clear only its own intent and load server truth. A newer pending review cannot be erased by an older response. Never reset shipping's own pending intent store.

E — MOBILE / READABILITY ON CURRENT DESIGN SYSTEM
Design this as an operator page, not a tall technical form:
-1440: clear single work strip and readable shared AppDataGrid;64px rows/unclipped badges, focused columns and separated money/physical meanings.
-390: header/actions wrap cleanly, work strip scrolls locally if needed, grid has scoped horizontal scroll only; detail uses one column, concise summary then line/receipt sections. No wall of repeated nested cards or micro-labels.
-New actions/form controls min44px; normal body text around14px, secondary text still readable; design tokens/contrast/focus styles, RTL alignment and LTR-isolated references/numeric ranges. No global toolbar restyle merely to repair historical36px controls.
-Dialog fits narrow viewport, header/footer and content scroll appropriately; keyboard/focus/escape semantics from existing Dialog, no overlapping modals and no browser confirm/prompt.
-Persian/Jalali calendar and digits using existing operational/date/money/quantity helpers. Receipt/request/decision instants use a deliberate known display zone: configured admin zone if available, else explicit labelled UTC; never an implicit browser/carrier-zone guess or raw ISO fallback. Keep wire/query dates and currencies unlocalized.
-No global CSS/storefront redesign or new locale registry/hardcoded language list. Preserve customer/seller creation flows/shared components outside admin scope; don't make admin strict decoding silently alter another consumer's write default.

F — FOCUSED PROOF AND ACTUAL SERVED ARTIFACTS
Backend named focused work-query/capability/guard tests:

classification keeps blocked active work vs quiet history, money-completed+physical-pending active, rejected received goods active;
same classification provides page+total+all badges before paging with common filters, including cases beyond page1, archive invariant, no legacy+new hidden filter;
per-case reference stable across sort/filter/page, multi-product mixed units honest;
-C1/C2 boundaries above and gates/capability parity; no new lifetime/DI cycle/N+1 foreign reads.
Frontend boundary/runner tests:
real DTO shape (numeric/string enums as actually supported), missing recovery/amount/currency/destination not treated as success/zero/OriginalPayment; real receiving/mixed/restocked facts mapped;
clean/URL/back-forward/return-to-list state, counters vs page and stale-response fencing;
mounted receiving partial/mixed+explicit condition review, reject reason, approve actual outcome, pending-money vs new-failed-retry vs physical-only;
before-first-send durable record, write failure0POST, same-tick double click1POST, unmount/remount exact-byte retry with changed or failed fresh read, conflict/denial keeps uncertainty, late response compare-and-clear fences; all use existing configured mount runner, not an arbitrary Vitest invocation;
no second financial action owner in queue/Order menu, unknown pending HTTP200 produces honest status instead of success.
Use controlled adapters/fixtures in automated tests, NOT live data writes. If relational proof is necessary, existing canonical rollback-only target/visible setup failure rules apply; no committed fixtures/cleanup/new DB.
Run only named new/affected suites ONCE after final changes, frontend typecheck+production build once, backend compile/publish once. No whole Returns/Host/AMSC/Order/Fulfillment/Shipping/parity suite, broad guards/baseline updates or detached baseline re-run. Exact changed pins only if genuinely required.
Preserve unrelated pre-existing architecture failures; do not alter baselines or run "all green" checks again.
Gracefully stop known locking Host before publish; serve Host5088 and Next3000 current artifacts using unchanged config, record host hash/health/readiness and FE BUILD_ID + manifest/chunk marker200. Inspect startup effects; no migrations/seeds/backfills.
Live READ-ONLY /admin/returns AND /fa/admin/returns at390/1440: text/DOM/geometry/network, document overflow0, locally scoped scroll, readable controls/empty/error state and truthful totals. No screenshots.
Current Returns table is empty. Do not create demo order/case/receipt/stock/refund to show actions. Mounted/controlled actionable proof must be labelled; empty live queue and missing detail are not a real operator financial walkthrough. One review URL, no "all Returns complete/user visual accepted" claim.

STRICT BOUNDARIES / HANDOFF
ONE canonical tooba_alpha127.0.0.1:5432 SingleStore/store-alpha, Host5088/FE3000; no connection/ports/edition/appsettings/keys/CapturedPreview change.
NO migration/DDL, new database/Docker/15432/15433, seed/backfill/reset/cleanup/delete/restore, committed live business fixture, valid live receive/approve/reject/retry/recovery mutation, provider/PSP/Wallet/Inventory/waybill/notification/money call or user order/draft/history/hold/stock change.
No provider integration/return-to-customer shipping/new lifecycle/financial formula or whole module certification.276 NOT ISSUED.
Task VERBATIM:
docs/ai/tasks/TB-RETURNS-OPERATOR-WORKFLOW-QUEUE-DETAIL-AND-MOBILE-275/TASK.md
Evidence:
docs/architecture/evidence/TB-RETURNS-OPERATOR-WORKFLOW-QUEUE-DETAIL-AND-MOBILE-275/{TASK.md,RESULT.md,RECEIPTS.md,result-contract.txt,operator-workflow-proof.md,query-count-and-display-proof.md,durable-intent-and-mobile-proof.md,user-review.md}
Document exact owner/files/capabilities, state+counter semantics, legacy handoff/retry guard, exact POST intent identities, actual tests/runtime/known limits. Original274/R1 evidence remains unchanged.
Update TOP AGENTS/START-HERE/RECOVERY/Returns plan coherently:275 worker result awaiting Architect,276 NOT ISSUED. Preserve272-R1/273-R2/274-R1 bounded acceptance and all audits.
Commit+push normally, expected/actual base/code/evidence/final HEAD + fresh origin parity. No force/amend/reset/stash/rebase, credentials/scratch/private dump artifacts or recursive self-hash pin loop.
ONE canonical complete Bridge result only, no placeholder/probe result POST:
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-RETURNS-OPERATOR-WORKFLOW-QUEUE-DETAIL-AND-MOBILE-275
Parent-Task: TB-RETURNS-CUSTOMER-RECEIPT-INSPECTION-AND-REFUND-GATES-274-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA
Status: PASS | PARTIAL | BLOCKED
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA / Base-SHA / Code-SHA / Evidence-SHA / Final-HEAD / origin parity
Decision; A–F implementation/evidence; exact checks/served artifacts; one review URL; actual actionable-vs-live proof limits.
Worker PASS != Architect ACCEPT; no user/provider/financial/full-Returns acceptance.
automaticNextImplementationTask: NONE
END_TOOBA_WORKER_RESULT

STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
END_TOOBA_TASK
