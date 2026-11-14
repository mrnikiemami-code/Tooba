# CURRENT ARCHITECT RECOVERY — 265 PARTIAL; ONLY265-R1 ISSUED — 2026-11-14

Read docs/architecture/evidence/TB-SHIPPING-COURIER-ORIGIN-RETURNS-LINKAGE-AND-OBLIGATION-CLOSURE-265/ARCHITECT-REVIEW.md and docs/ai/tasks/TB-SHIPPING-COURIER-ORIGIN-RETURNS-LINKAGE-AND-OBLIGATION-CLOSURE-265-R1/TASK.md.
265 worker PASS is NOT accepted: source-confirmed owning/recovery gaps A–E below. One worker/task repair, no new product stage/delegation/broad test loop.

A OrderReturnObligationClosure only reads and returns Closed=true: ZERO durable Order write/cumulative closure. Must persist case/source/line identity and actual readers consume it.
B Fulfillment settlement matches all resolutions by line, not source parcel, cancels whole Created parcels, sums whole claim against FIRST hold. Must scope/map exact source/partial quantity/multiple holds; protect competing send while Requested; real shared branch concurrency and package guard. Paid validation after claim and case-before-link retry also need exact recoverable phase.
C Inventory ExecuteUpdate Reserved happens outside transaction encompassing reservation/inbox SaveChanges; crash releases availability without identity. Replay key alone doesn't validate requested reservation/quantity. Fix atomic owning partial release/exact result.
D Reject can release claim despite case already approval/refund processing; successful refund then disposition error may become refund failure/new retry key; item marked disposed BEFORE stock call. Separate authoritative money outcome from physical work, serialize approval/reject, preserve exact unknown attempt, completed replay, disposition-only recovery. Both public entry routes enforce.
E Current UI Create exists but real View-case/read detail origin/phase/condition wiring and localization incomplete. Need one actual task-owned create->existing Returns case->approval controlled-finance handoff, not unpaid/no-action GET.

Retain useful265 origin/claim/Contracts structure, accepted timing264 and mobile264-R3/notifications263; no fresh scheduler/mobile perfection task. Financial/user acceptance NOT claimed. New tests justified ONLY by these concrete introduced seams; no full-module/base parity/green repetition.
Three paths binding: healthy same-unit resend / newly reserved healthy replacement / linked existing Returns. Requested claims selected qty but no stock release/refund; approved owning obligation cancellation releases ONLY relevant unconsumed hold once before existing refund. Consumed original sale untouched, damaged never saleable, siblings/unselected quantities retained. Unknown money stays exact attempt; stock error never causes another refund.
One canonical tooba_alpha5432 FE3000/Host5088 SingleStore, minimal independent TB265 fixtures only. User/screenshot/method01a17d0d... READ-ONLY, no extra DB/reset/reseed/businessSQL/global reclaimer; additive runner migrations+backup only. Preserve keys/CapturedPreview;255 parked/Tipax deferred. Parent-package workflow unsupported but locked-child command guard required.
Result must report real owning facts/phase boundaries/public UI and exact review URLs with controlled-finance limit. One result then Architect review. No new task until result.
STOP_FOR_ARCHITECT; automaticNextImplementationTask:NONE. Earlier CURRENT banners below superseded; do not recover by treating original265 PASS as acceptance.

---

﻿# CURRENT — 265 WORKER RESULT PASS: courier-origin Returns case, one source claim, phased approval closure, condition-safe stock — 2026-11-13

Read docs/ai/tasks/TB-SHIPPING-COURIER-ORIGIN-RETURNS-LINKAGE-AND-OBLIGATION-CLOSURE-265/TASK.md and
docs/architecture/evidence/TB-SHIPPING-COURIER-ORIGIN-RETURNS-LINKAGE-AND-OBLIGATION-CLOSURE-265/{TASK.md,RESULT.md,RECEIPTS.md,result-contract.txt,ownership-design.md,tb265-relational.txt}.
Status **PASS** within the issued scope, decision **COURIER_ORIGIN_CASE_AS_THIRD_LAWFUL_OUTCOME_WITH_ONE_FULFILLMENT_SOURCE_CLAIM_PHASED_APPROVAL_CLOSURE_AND_CONDITION_AWARE_DISPOSITION** — NOT Architect ACCEPT, NOT user VISUAL/FUNCTIONAL ACCEPT, NOT a live paid/provider refund. 264/R1/R2 scheduling and 264-R3 mobile stay accepted within scope and were not reopened.

| Area | State | What this means |
| --- | --- | --- |
| Calendar256/save256-R2 | closed within scope | customer/carrier weeks distinct; max transport, cutoff, own save capability |
| Tracking/quantity257 | closed within scope | server unique tracking, packed minus shipped minus open allocation |
| Non-delivery/return/resend258-R3 | accepted internal-operator scope | receipt condition/source-safe resend; retained exact occurrence |
| Common/separate packing259 | accepted within scope | deliberate immutable packing mode; parent package exception flow still unsupported |
| Queue260-R1 | accepted within scope | exact physical Group/Parcel/Package identities and own-group plan/locator |
| Feedback/damage261–263 | accepted within scope | existing Toast/MessageBox; Persian states; healthy-stock replacement and retained pending decision; referral is NOT Returns |
| Scheduling264/R1/R2 | ACCEPTED within scope | source fixes + real authoring Save/reload/Publish + public positive projection/selection/commit + stored policy3->queue |
| Mobile264-R3 | PASS pending Architect/user visual review | identity block owns a full mobile row, title 1 line, method in its own bounded row, header static below lg; desktop compact row and one Save owner unchanged |
| Returns265 | PASS pending Architect/user review | courier-origin case as third lawful outcome, one Fulfillment source claim, phased approval closure, condition-aware disposition |

## Delivered Returns linkage (265) — pending review
`create_courier_return` is an ordinary Order post-dispatch command (`return.manage` decided before any evidence read) that claims the exact reviewed quantity in Fulfillment (`fulfillment.courier_return_claims` + lines, filtered unique `ActiveKey`, per-line remainder = receipt − resend − replacement − case claims), then creates the case in the EXISTING Returns workflow with `Origin`, source fulfillment/parcel/receipt/claim/occurrence linkage and `ClosurePhase`. Usable and damaged quantity are separate pools; `usable 2 + damaged 1` of one line works, the same pool cannot fund two branches (`fulfillment.returns.claim_quantity_exceeded`), and damaged units committed to an already dispatched replacement are refused (`fulfillment.returns.replacement_already_dispatched`). Approval is phased and idempotent `None → ObligationClosed → StockSettled → RefundReleased`: Order closes only the exact line obligation (no historical ordered/packed/dispatched decrement, no whole-order `already_dispatched` weakening), Inventory releases only the unconsumed held quantity of that closed obligation once, disposition is `SaleableRestock` or `DamagedNonSaleable` (damaged never becomes saleable), and only then does the existing refund path run. Creating `Requested` moves no money, releases no stock, cancels no order; reject/cancel releases only the branch claim. FE: one contextual «ایجاد پروندهٔ مرجوعی» / «مشاهدهٔ پروندهٔ مرجوعی» in the existing shared dialog with retained durable envelope, one Toast, existing MessageBox for approval, no finance panel on the finance-free view.
Proven once each: Fulfillment 7/7, Returns 5/5, Inventory 5/5, Order post-dispatch replay 16/16 (5 new), Host relational A–F 6/6 on real `tooba_alpha`, FE mount 62/62 (2 new), `tsc` 0, `next build` 0, Host build/publish 0. Runtime Host 5088 (PID 37424, `/health`+`/health/ready` 200), Next 3000 (PID 20232, BUILD_ID `wqPY0iLLazeSDXeSeyTtu`). Additive migrations applied by the repository MigrationRunner after private backup; all 33 modules pending 0; user/screenshot rows read-only; task rows `TB265-…`. Limits: no real money movement, no worker browser click-through (user trial pending), parent package still unsupported, dispatched replacement refuses, concurrency proven by two contexts + DB guards.

STOP_FOR_ARCHITECT; automaticNextImplementationTask: NONE. The blocks below are superseded historical records.

---

# SUPERSEDED — 265 ISSUE / ARCHITECT RECOVERY CHECKPOINT — 2026-11-13 (historical)

## Exact current position
Active task: docs/ai/tasks/TB-SHIPPING-COURIER-ORIGIN-RETURNS-LINKAGE-AND-OBLIGATION-CLOSURE-265/TASK.md.
Parent264-R3 mobile accepted by independent source/evidence review in docs/architecture/evidence/TB-SHIPPING-AUTHORING-MOBILE-RESPONSIVE-UI-264-R3/ARCHITECT-REVIEW.md. User visual acceptance still pending, not claimed.
264/R1/R2 timing/positive public selection->durable policy3->queue accepted; do not reopen.263 notifications/pending recovery accepted.257 quantity/tracking,258 source-safe return/resend and262 healthy-stock replacement are retained.
ONLY265 now implements remaining courier-origin -> existing Returns business path. One worker/task, result then review; no delegation or automatic next. Don't create another mobile polish/scheduling/test task without concrete defect.

##265 owning design — read TASK, don't improvise shortcuts
Architect inspected current ReturnEligibilityEvaluator (Paid+Delivered), ReturnDirectory (Approve immediately starts existing refund, refund success unconditionally restocks), ReturnItem (no receipt/condition), OrderReturnContracts (read-only), Order Returns orchestrator, Inventory lifecycle/return Contracts, and Fulfillment damaged directory.
Add explicit CourierOrigin source on existing Returns and narrow Contracts; preserve customer-delivered RMA rules. No fake Delivered/global eligibility bypass.
Three routes: same usable goods resend; separate healthy-stock replacement hold; linked Returns case reusing actual origin receipt. Fulfillment source authority must prevent same quantity funding competing branches. Requested claims quantity but does NOT release stock/start refund.
Approved courier case + owning cancellation of corresponding Order-line obligation closes sending, releases ONLY relevant unconsumed held quantity once, THEN existing Returns refund can proceed. Partial sibling holds untouched, consumed original sale never released/recreated. Failed/unknown refund doesn't reopen delivery or remint attempts. Durable per-phase identity/reconcile mandatory; direct Returns approval routes cannot bypass closure.
Condition-aware goods disposition: healthy may restock once under existing policy; damaged NEVER unconditional saleable restock. No new quarantine platform. Actual customer/payment beneficiary, never operator wallet/client total.
Existing pending Referral/Replacement can deliberately redirect through audited source branch linkage; Created replacement may be cancelled retaining hold until approval; already-dispatched replacement refuses unsafe refund/release shortcut. Parent-package child locks stay, no silent unlock.
Actual public create/case link/Returns screens, existing Toast/MessageBox, retained exact envelope; one action owner; mobile390 and desktop usability retained. Financial verification uses controlled task ports, not real user/provider money.
No all-shipping-complete/user ACCEPT until review and user trial.

## Environment/audit/backlog
One tooba_alpha5432, FE3000->Host5088 SingleStore; minimal genuine TB265 fixture writes through owning paths allowed, user/screenshot rows read-only. User draft01a17d0d-... remains user-owned; R1 writes are audited, no blind restore. No extra DB/reset/reseed/businessSQL/manualDDL/global reclaimer. Additive migrations only runner+backup. Preserve keys/CapturedPreview,255 parked/Tipax deferred.
Tailwind content features glob missing is verified existing build-surface backlog; mobile fix reused emitted utilities.265 must verify production CSS, no silent global style expansion/extra test loop.
Existing limitations: parent common-package exceptions/provider not covered.264 queue fixture unpaid with disclosed Fulfillment factory layer. No fresh scheduler tests required by265.
STOP_FOR_ARCHITECT; automaticNextImplementationTask:NONE. Prior CURRENT blocks below are superseded historical records; this is the authoritative continuation point.

---

# SUPERSEDED — prior worker/architect checkpoint

---

# SUPERSEDED — 264-R3 WORKER RESULT PASS: mobile authoring header readable, timing form usable — 2026-11-13

## Read first; exact current state
Read docs/ai/tasks/TB-SHIPPING-AUTHORING-MOBILE-RESPONSIVE-UI-264-R3/TASK.md and
docs/architecture/evidence/TB-SHIPPING-AUTHORING-MOBILE-RESPONSIVE-UI-264-R3/{TASK.md,RESULT.md,RECEIPTS.md,result-contract.txt}.
Status **PASS** within the issued responsive-presentation scope, decision **MOBILE_IDENTITY_ROW_WITH_UNCLIPPED_TITLE_AND_OWN_BOUNDED_METHOD_ROW_UNCHANGED_DESKTOP_HEADER_ONE_SAVE_OWNER_ZERO_DOCUMENT_OVERFLOW**, NOT Architect ACCEPT, NOT user VISUAL/FUNCTIONAL ACCEPT. 264/R1/R2 scheduling/connected delivery stays ACCEPTED and was not reopened. 265 remains PLANNED, NOT ISSUED.

| Area | State | What this means |
| --- | --- | --- |
| Calendar256/save256-R2 | closed within scope | customer/carrier weeks distinct; max transport, cutoff, own save capability |
| Tracking/quantity257 | closed within scope | server unique tracking, packed minus shipped minus open allocation |
| Non-delivery/return/resend258-R3 | accepted internal-operator scope | receipt condition/source-safe resend; retained exact occurrence |
| Common/separate packing259 | accepted within scope | deliberate immutable packing mode; parent package exception flow still unsupported |
| Queue260-R1 | accepted within scope | exact physical Group/Parcel/Package identities and own-group plan/locator |
| Feedback/damage261–263 | accepted within scope | existing Toast/MessageBox; Persian states; healthy-stock replacement and retained pending decision; referral is NOT Returns |
| Scheduling264/R1/R2 | ACCEPTED within scope | source fixes + real authoring Save/reload/Publish + public positive projection/selection/commit + stored policy3->queue |
| Mobile264-R3 | PASS pending Architect/user visual review | identity block owns a full mobile row, title 1 line, method in its own bounded row, header static below lg; desktop compact row and one Save owner unchanged |
| Returns265 | PLANNED, NOT ISSUED | next after mobile result; owning integration design below |

264 evidence/review: docs/architecture/evidence/TB-SHIPPING-READINESS-AWARE-CUSTOMER-CALENDAR-AND-DISPATCH-DEADLINES-264-R2/ARCHITECT-REVIEW.md.473cc23c62b29ebb67202ad2ecd8486c4d1594ae now PUSHED; transport issue CLOSED. Later always-commit-and-push rule retained. Worker Windows runs are evidence, not Architect independent runtime execution; user functional acceptance pending.
264 fixture checkout01a1d511-adbb-7000-8575-d3b957453d76 is PendingPayment. Factory Fulfillment fixture is disclosed; don't claim real paid/provider test. Source policy3 preserves customer window and preparation week. No reopening or new task merely to rerun green cases.

## Delivered mobile correction (264-R3) — pending review
Measured root cause on the served page: one `flex-wrap` row held back182px + toolbar178px beside a `min-w-0 flex-1` identity block, and the real authoring content is only281px wide at360 (311px at390), so the block collapsed to65/95px → title65/95px in3/2 vertical word lines, method clipped, header222/198px sticky. Now the identity block is `w-full min-w-0 grow md:w-auto` (own row below md), the title is `min-w-0 shrink-0 leading-6`, the method name is `w-full min-w-0 truncate md:w-auto` + title attribute (own bounded row below md), and the header is `lg:sticky` only. ONE Save owner/callback/dirty/disabled, tab strip, day+minute editors, single Toast, MessageBox for blocking decisions unchanged.
Measured read-only on the served build at360/390/1440: title151px/1line at both mobile widths, identity255/285px, method unclipped255/285px, header194px static on mobile and62px sticky at1440, 0 document overflow, 0 off-screen controls, 0 buttons/links under44px, all seven day rows in the week/carrier/processing editors intact with 0 overflow, 0 text inputs under90px and none under28px. Task-owned draft01a1cd43… read only (tab switches only, no save/publish/discard/data write); user service01a17d0d… untouched. FE-only: typecheck0, build0, BUILD_ID `Ex36QU4MPjSKfm8VUlqDC` chunk `6138-3bc417f144f96c81.js`, Next PID37012 on3000, Host5088 untouched.
Reported not fixed: `tailwind.config.ts` content globs are `./app`, `./design-system`, `./lib` only — a class used solely inside `features/**` is never emitted, so feature-level responsive utilities must be re-verified in the DOM.

Base `c60458a95`; code `b7cd52819`; evidence/merge SHA in result-contract.txt; single worker, one task, no delegation, no historical agent resumed. One DB tooba_alpha5432 SingleStore store-alpha; Next3000 (PID37012) / Host5088; no migration, schema, backend, API or locale change; no save/publish/discard/data mutation; user draft01a17d0d read-only; keys+CapturedPreview preserved; 255 parked, Tipax deferred. 264 scheduling stays ACCEPTED; this mobile correction needs Architect review and the user's own visual check. STOP_FOR_ARCHITECT; automaticNextImplementationTask:NONE.

## Next265 — prior inspection findings, not implemented
Source independently read at18c291d7:
- ReturnEligibilityEvaluator requires IsPaid and LastDeliveredAt and uses delivered quantities/customer return-window snapshot. Existing path therefore does NOT admit never-delivered courier-origin receipt; do not fake Delivered or disable the global customer eligibility rule.
- ReturnDirectory.ApproveAsync starts refund via existing Payment/Wallet paths. ExecuteRefundAsync then calls RestockConsumedReservationAsync for each reservation-bearing item on success.
- ReturnItemSnapshot has quantity/reservation but no physical condition/source receipt. ReturnInventoryGateway delegates to Inventory.RestockFromReturnAsync, no condition parameter. Blind reuse could turn damaged goods into saleable stock.
- ReturnAdminOperations existing creation/approval/refund/history is the owning workflow. Need narrow typed courier-origin reason/source linkage via Contracts and quantity authority, not a parallel refund service.
When issuing265, inspect remaining Order/Fulfillment/Inventory ownership and exact approval/cancellation seams FIRST; this is not a worker discovery-only task. Design complete connected path and user trial.

## Three business paths and reservation rule (latest user decision)
1 Healthy SAME returned units -> resend using source Usable capacity; no fresh sale reservation.
2 New healthy-stock replacement -> own purpose-bound reservation + immediate source/resolution link, no damaged-as-usable conversion.
3 Create linked Returns case -> existing Returns approval/refund/disposition workflow, own receipt reused (no second physical receipt). Opening case alone never releases stock.
Source quantity may fund only ONE active branch. Requested case must claim/block competing send quantity durably; rejection/cancel releases only that claim if owning policy permits, not a stock release. Approval with owning cancellation of corresponding order-line obligation releases ONLY relevant unconsumed held reservation once; consumed original sale stock is not "released". Don't bypass whole-order already-dispatched cancellation rule; explicit line-obligation integration needed. Already-dispatched replacement cannot be treated as held unused stock; return/recall policy required.
Financial amounts/destination/customer identity belong to existing Returns/Order/Payment/Wallet snapshots (never operator-as-refund-beneficiary); do not auto-refund on button Create. Approval/refund retries/lost responses need durable identity/reconciliation, no double refund or two branches. Failed refund remains approved obligation, not permission to resend. Healthy restock vs damaged disposition explicit; never saleable restock of damaged units or double count receipt/reservation.
No financial integration is claimed today. Parent-package returns/damage remain deferred, no silent child unlock.265 not issued yet.

## Audit/environment/stop
R1 incorrectly wrote USER draft01a17d0d-e033-7000-af43-49a1daa28822 and labelled it fixture; audit corrected in264/R1-RESULT and RECEIPTS. Do NOT write/restore/re-publish it. It remains user's service, not task data. Mobile read-only proof uses separate labelled draft; future fixtures must be genuinely independent.
One canonical tooba_alpha5432, FE3000 Host5088 SingleStore/store-alpha. No new DB/disposable/Docker/15433/edition switch/reset/reseed/raw business SQL/global reclaimer. Preserve DataProtection/CapturedPreview localON7dates checked-inOFF.255 parked/Tipax deferred. No provider/money/user-data mutation authorized by mobile task.
STOP_FOR_ARCHITECT; automaticNextImplementationTask:NONE. All earlier CURRENT blocks below are superseded historical records; this block governs current order/scope.

---

# SUPERSEDED — prior checkpoint

Read264-R2/ARCHITECT-REVIEW.md under docs/architecture/evidence/TB-SHIPPING-READINESS-AWARE-CUSTOMER-CALENDAR-AND-DISPATCH-DEADLINES-264-R2/.
Commit473cc23c62b29ebb67202ad2ecd8486c4d1594ae is pushed/retrievable; former transport blocker CLOSED. Source/evidence review accepts bounded timing and connected delivery corrections; user visual/functional acceptance pending. No scheduler repair/test loop needed.
Worker-proven real form save/reload/publish -> format4 quote with processing policy -> public selection/commit -> durable policy3 -> existing queue. Checkout01a1d511-adbb-7000-8575-d3b957453d76 is PendingPayment; Fulfillment factory fixture is disclosed, not live paid/provider proof.
User mobile screenshot remains OPEN: authoring title/action header squeezes vertically, responsive form/tabs/actions need attention. User said no next task before R2 result; do not mark mobile fixed from desktop proof.
265 Returns linkage PLANNED/NOT ISSUED: inspect owning existing Returns before issue. Three paths, per-source quantity exclusivity, approved return+owning line-obligation cancellation release only unconsumed hold once; case creation alone not release. No financial automation accepted.
R1 user-service mutation audit retained; no blind restore/further user writes. Same oneDB/ports, preserve keys/CapturedPreview;255 parked/Tipax deferred. No new task issued by this review.
STOP_FOR_ARCHITECT; automaticNextImplementationTask:NONE. Prior CURRENT blocks below superseded.

---

# SUPERSEDED — prior delivery status

264-R2 closed the delivery gap: changed FE rebuilt/served (`BUILD_ID ILzh0yMqPr3RDrjGp3wYE`), real authoring
Save→reload→Publish on the operator form (revision 8 / selection 5 / snapshot `01a1d50f-…`), a positive
storefront calendar from that same capture, the public selection writer saving durable policy-3 deadlines, a real
public-commit checkout `01a1d511-adbb-7000-8575-d3b957453d76`, and the existing admin queue answering exactly
those instants (`prep 2026-11-14T09:30Z / transfer 15:10Z / handoff 16:30Z`, carrier 2026-11-14 08:00–20:00
Asia/Tehran, customer 2026-11-16 09:00-10:00).
Evidence: docs/architecture/evidence/TB-SHIPPING-READINESS-AWARE-CUSTOMER-CALENDAR-AND-DISPATCH-DEADLINES-264-R2/.
NOT Architect ACCEPT, NOT user acceptance, NOT a paid checkout, NOT "shipping complete". No migration this round.
The USER review draft `01a17d0d-…` was read-only this round; its R1 mutations are disclosed in an appended audit
note, no rollback. `265` (courier-origin return ↔ existing Returns linkage) remains **NOT ISSUED**.
STOP_FOR_ARCHITECT; automaticNextImplementationTask:NONE.

---

# SUPERSEDED — 264-R2 ISSUED, delivery closure only — 2026-11-12

Read264-R1/ARCHITECT-REVIEW.md under docs/architecture/evidence and docs/ai/tasks/TB-SHIPPING-READINESS-AWARE-CUSTOMER-CALENDAR-AND-DISPATCH-DEADLINES-264-R2/TASK.md.
Retain264-R1 source corrections (zero/window/explicit processing policy). User handoff incomplete: changed FE not rebuilt/served; positive storefront selection->accepted evidence->queue not proven. Only narrow delivery closure, no scheduler rewrite/broad green loop.
IMPORTANT audit: draft01a17d0d-e033-7000-af43-49a1daa28822 is USER-owned. R1 PUT/publish on it was not a task fixture and violated read-only scope. No further writes/blind rollback; document exact mutations honestly. Fresh positive proof uses truly labelled independent minimal task fixtures through owning production paths; creating required task-only compatible offer/stock/method/cart IS explicitly authorized on same DB, not user rows.
One worker/task, actual authoring Save/reload/publication + new served FE build + positive dates/selection + exact queue/operations review URL.265 remains planned NOT ISSUED. No all-complete/user ACCEPT claim.
Same canonical tooba_alpha5432/3000/5088 SingleStore; no additional DB/edition switch/reset/reseed/global reclaimer. Preserve keys/CapturedPreview;255 parked/Tipax deferred. Existing Toast/MessageBox and retained reservation/occurrence rules unchanged.
STOP_FOR_ARCHITECT; automaticNextImplementationTask:NONE. Prior CURRENTs below superseded.

---

# SUPERSEDED — previous result status

Read docs/architecture/evidence/TB-SHIPPING-READINESS-AWARE-CUSTOMER-CALENDAR-AND-DISPATCH-DEADLINES-264/ARCHITECT-REVIEW.md and docs/ai/tasks/TB-SHIPPING-READINESS-AWARE-CUSTOMER-CALENDAR-AND-DISPATCH-DEADLINES-264-R1/TASK.md.
Concrete review gaps: zero preparation allowed forward but rejected backward/DB; selected customer window/zone missing from backward deadlines (zero-transit10–11 vs carrier17 close); silent24/7 preparation and instant-ready new-commitment fallback; missing real authoring save/publish/selection handoff.
Only264-R1 fixes these original-scope seams A–D, retaining existing timing owner/evidence/queue. No265 implementation until result reviewed.263 remains accepted within scope, user acceptance pending.
One worker/task, no delegation/full-suite/base-parity/repeated-green/screenshots. Same canonical tooba_alpha5432/FE3000/Host5088 SingleStore. Minimal labelled owning-path fixtures only, user/screenshot rows read-only, no extra DB/reset/reseed/global reclaimer. Additive correction migration only via runner+backup; old accepted plans not recomputed.
Messaging existing Toast/MessageBox; active-order hold retained through parcel cancel/return/retry/timeout, dispatch consumes once. Future approved Returns+owning line-obligation cancellation release belongs265; opening case alone not release.
Plan SHIPPING-USER-FLOW-CLOSURE-263-265.md. Preserve DataProtection/CapturedPreview;255 parked/Tipax deferred.
STOP_FOR_ARCHITECT; automaticNextImplementationTask:NONE. Prior CURRENTs below superseded.

---

# Shipping user-flow closure 263–265 — 2026-11-11
Latest user instruction: fix the defects, issue sequential tasks, review each result before the next. Smooth operator flow using existing Toast and MessageBox; no repeated repair/test loops.
262-R1 accepted ONLY its three repair seams; new user trial reopens functional acceptance.

## Sequence
1. **263 ACCEPTED within issued scope — user functional acceptance pending** — TB-SHIPPING-REPLACEMENT-RECOVERY-AND-OPERATOR-FEEDBACK-263.
   Reproduced on the read-only screenshot record: the durable decision existed (`Recorded`) but the admin projection
   dropped its `occurrenceKey`, so the resume posted a blank key and the owner answered the deterministic 409
   `fulfillment.damage.resolution_occurrence_required`; every non-ok answer was then presented as an unknown response.
   Repaired at the owning seams (projection forwards the key; ONE completed/pending/refused/unknown classification;
   existing Toast for confirmed success; existing MessageBox for a blocking refusal; all `fulfillment.damage.*`
   localized). See docs/architecture/evidence/TB-SHIPPING-REPLACEMENT-RECOVERY-AND-OPERATOR-FEEDBACK-263/RESULT.md.
   No financial or calendar feature was added.
2. **264 WORKER PASS — pending Architect review** — TB-SHIPPING-READINESS-AWARE-CUSTOMER-CALENDAR-AND-DISPATCH-DEADLINES-264 — readiness-aware shipping calendar and operational start deadlines. Current 256 policy assumes nowUTC is goods readiness; user explicitly rejects that assumption. Inspect actual existing per-item/offer/seller preparation inputs and operational working calendar before adding settings; don't invent duplicate lead-time fields. Model readiness (processing/packing), transfer to carrier, actual acceptance window/pickup cutoff, MAX transport working days and independent customer receiving window. If window is opening hours it is not an invented fixed pickup appointment. Preparation cannot run through closed warehouse hours unless explicit policy allows it. Before ordering, offer customer dates ONLY from first feasible handoff after readiness+transfer; max2–3=>3. At carrier close exactly => next acceptance; 16:59 can also miss today's 17:00 when preparation/transfer takes time. No double counting: existing authoritative preparation time plus only distinct missing transfer/buffer.
   For chosen customer date, derive latest feasible carrier handoff AND latest preparation start using same authority, save accepted evidence and queue from durable facts (no current-config recomputation of old orders). Customer promise stays separate. Multi-item common group readiness is constrained by all required members, not summed parallel preparation blindly. Separate parcels retain own work scope. Revalidate availability at acceptance; historical plans remain explicit legacy/unknown, not fabricated/backfilled.
   Acceptance examples: 14:00+2h feasible before17,16:00+2h misses17; weekday noon-close, closed warehouse/carrier/customer day, cross-zone, chosen later date; queue shows when preparation must start and handoff deadline.
   Worker result: the physical preparation/transfer pair is now an explicit owned authoring policy carried through the captured
   quote; ONE forward/backward authority turns the trusted eligibility instant into feasible carrier handoffs and
   customer dates and derives latest preparation-start / transfer-departure / handoff-deadline instants; the
   policy-v2 triple persists in accepted evidence behind an additive all-or-nothing constraint (v1 stays readable);
   the existing finance-free queue gains its own preparation state/due filter and the shared operations timeline
   shows the deadlines beside the customer promise. No second queue/settings app, no money change.
   See docs/architecture/evidence/TB-SHIPPING-READINESS-AWARE-CUSTOMER-CALENDAR-AND-DISPATCH-DEADLINES-264/RESULT.md.
   Boundary: acceptance proven through the production resolver + real DB + actual queue query, NOT a live paid checkout.
   R1 correction (Architect gaps A–D): explicit `0/0`, `0/30`, `30/0` allowances are planned with a non-decreasing
   triple; the chosen customer date/window/zone now constrains the same backward authority so a same-day plan can
   never hand over a carrier close later than the window opens; the warehouse processing week is an explicit stored
   policy (`WorkingWeek` vs deliberate `Continuous`, format 4) and a timing pair without it is refused instead of
   being read as "ready now"; and the capture graph bug that dropped an existing processing week from the published
   body was found and fixed on the canonical install (pre-fix capture `01a1cd1e-…` = format 3/null, post-fix
   `01a1cd2d-dc41-…` = format 4 with Asia/Tehran 6-working-of-7). Live storefront projection on a task-labelled
   guest cart quotes the SELLER-owned capture and truthfully returns no dates for it. Positive customer-date
   creation has no Store-owned offer to run on, so it is proven by the authority/capture facts, not a live checkout.
   See .../264/R1-RESULT.md, R1-RECEIPTS.md, tb264r1-connected-proof.txt, tb264r1-storefront-projection.txt.
3. **265 PLANNED / NOT ISSUED** — connect courier-origin return to EXISTING Returns flow. Inspect Returns eligibility/finance/inventory APIs (delivered-customer RMA may currently refuse never-delivered source). Reuse Contracts and owning processes, no parallel refund module or fake Delivered. Three routes: resend same healthy goods; healthy-stock replacement with distinct reservation; create linked Returns case with existing origin receipt reused. Cover usable as well as damaged quantities. One source quantity cannot fund simultaneous resend/replacement/refund branches; concurrent/retry exact authority.
 Latest reservation rule: opening a case alone doesn't release. Approved return plus owning cancellation of relevant order-line obligation releases only its unconsumed held stock once, blocks further send for that quantity; consumed original reservation isn't released or resurrected. Existing whole-order post-dispatch cancellation restriction must not be bypassed; explicitly integrate line obligation policy instead. Already-dispatched replacement needs physical recall/receipt, cannot be treated as unused hold. Refund amount/approval/execution remains Returns/Settlement/Payment-owned; damaged disposition isn't saleable restock. Referral in262 is NOT this integration. Preserve case audit links and exact replay, no double receive/refund. Scope/issue after actual code review and263/264 results.

## Communication
Existing react-toastify for transient confirmed success; existing MessageBox for blocking acknowledgement/decision; adjacent field validation; compact persistent business status. No technical diagnostics, no duplicate success/error surfaces, no unsafe abandonment of uncertain commands.

## Runtime and stop rules
One worker/task at a time, review then next. No automatic worker continuation. Same tooba_alpha5432, FE3000/Host5088 SingleStore; no new DB/reset/reseed. Minimal labelled production-path fixtures only; user rows read-only. Never global mutating reclaimer fixture test. Preserve DataProtection/CapturedPreview.255 parked, Tipax deferred.
Do not claim all complete. Issue263 result -> architect review -> next bounded task264; after265 user milestone unless concrete blocker requires decision.


