PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-COURIER-ORIGIN-RETURNS-LINKAGE-AND-OBLIGATION-CLOSURE-265
Parent-Task: TB-SHIPPING-AUTHORING-MOBILE-RESPONSIVE-UI-264-R3
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Mode: LINK_COURIER_ORIGIN_RECEIPT_TO_EXISTING_RETURNS_WITH_EXCLUSIVE_QUANTITY_ORDER_OBLIGATION_CANCELLATION_AND_CONDITION_SAFE_STOCK
Expected-Base-SHA: 7c32cdc2955d9c0b0c1dd4eef8b961b9a4acb904
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

## 1. ONE business feature, complete operator flow
264/R1/R2 timing and264-R3 mobile are accepted within scope; don't reopen them. Implement the third path from a courier-return receipt: "ایجاد پروندهٔ مرجوعی", linked to EXISTING Returns review/refund/disposition, beside existing healthy same-goods resend and newly-reserved healthy replacement.
This is implementation, not discovery-only. Architect source findings/design below govern ownership; resolve routine file/interface details autonomously. One worker/task, no delegation, no generic framework or parallel refund service. Read CURRENT recovery + module/Contracts/AMSC/design-system rules. Legitimate issue/docs descendants allowed; no force/reset/amend/stash/rebase/unrelated overwrite.

## 2. Source findings and required boundaries
Inspected at issued base:
- ReturnEligibilityEvaluator: Paid + LastDeliveredAt + per-line delivered-window snapshots. This is customer-delivered RMA eligibility; a never-delivered courier receipt is currently not_delivered.
- Order RequestReturnCoreAsync uses that evaluator and existing IReturnAdminOperations. OrderReturnContracts exposes read context only; no courier-origin obligation cancellation command exists.
- ReturnDirectory.CreateInternalAsync returns any existing IdempotencyKey early; new courier path MUST validate current authority/ownership + exact source/quantity intent, not inherit that broad replay shortcut.
- ReturnDirectory.ApproveAsync verifies payment, approves and immediately starts existing Payment/Wallet refund. ExecuteRefundAsync unconditionally restocks reservation-bearing items on refund success.
- ReturnItem has quantity/reservation but no source receipt/condition. InventoryReturnGateway.RestockFromReturnAsync lacks condition. Blind reuse would restock damaged goods.
- IOrderInventoryLifecyclePort releases a Held reservation by id, no quantity argument. Partial-line/partial-hold cancellation needs an owning quantity-safe seam, never release a whole sibling hold.
- Fulfillment owns origin receipt, source-safe resend capacity and damaged resolution/replacement holds. Returns owns case/approval/refund, Order owns remaining customer delivery obligation, Inventory owns release/restock/condition. Extend narrow Contracts seams, never cross-module DbContext/entities.
Preserve delivered-customer eligibility and refund behavior. No fake Delivered flag, no global NotDelivered bypass, no money/stock SQL or frontend-invented eligibility.

## 3. Source-aware Returns case creation
Add explicit origin discriminator/linkage on existing Returns case/items for CourierOrigin vs existing CustomerDelivered. Persist checkout/seller order/fulfillment/source parcel/receipt/line/condition/quantity, requesting operator audit and actual customer identity. Existing rows keep their original source semantics; minimal additive schema as needed.
For courier-origin path require paid owning order AND durable physically received origin quantities on that actual Returned parcel. Use receipt provenance, not DeliveredAt/customer RMA clock; don't impose a seven-day-after-fake-delivery window. Genuine seller/actor/package guards remain.
Support both healthy and damaged received quantities, including mixed usable2/damaged1 of SAME line, with explicit selected quantities and real remaining source capacity. Receipt is reused; no second physical receive.
Case starts Requested, visible in EXISTING Returns list/detail + linked from parcel. Creating case moves no money, releases no stock, doesn't cancel whole order. One clear reviewed command entry, exact durable occurrence before POST, same-key exact replay through public command even after action/capacity disappears. Changed source/quantity/condition under same key -> typed conflict, zero added case/claim.
Current actor/checkout/seller/fulfillment/parcel authority always rechecked. No blank auto-reminted key. Lost response/timeout preserves exact intent using existing263 outcome semantics; close/reload safely resumes from owning recorded facts/case link, not a browser-only promise.

## 4. ONE source-quantity authority across all three branches
Fulfillment owning transaction/aggregate is the common authority for source claims; UI hiding/Returns-only query is not concurrency safety.
Create case claims its selected quantity durably before it can fund another live resend/replacement. Same source quantity cannot simultaneously finance resend, replacement and refund; mixed/unselected quantities remain independently available. Two contexts must not overdraw.
Cross-module create/link failure requires recorded exact-intent continuation: no orphan claim permanently hidden, no duplicate case, no blind compensation/remint. Use existing durable occurrence/outbox/transaction patterns, no distributed transaction/new generic saga platform.
Requested -> Rejected/Cancelled may release only its branch claim after owning durable decision (making lawful capacity available); NOT the order stock hold. Approved/RefundProcessing/RefundFailed/Completed keep delivery closure and never re-enable resend just because refund failed.
Existing pending Referral or Replacement decisions already consume damaged outstanding capacity. A lawful deliberate switch to Returns must transfer/close that recorded branch using explicit owning linkage, never mutate its immutable occurrence identity or mark damaged Usable:
- Created replacement parcel may be cancelled through existing pre-dispatch rules; keep hold while case Requested, no duplicate hold or parcel;
- recorded/reserved pending decision can redirect exact remaining obligation to a linked case with audited history and deterministic retry;
- already dispatched/in-transit/delivered replacement cannot be treated as unused held stock or silently cancelled/refunded as unfulfilled. Refuse with precise next step until actual owning return/receipt/eligible path, no new recall/provider integration here.
Old source replay must not resurrect a branch after approved Return; identical historical request answers resolved/redirected facts.
Parent consolidated-package exception workflow remains unsupported: do not unlock child or duplicate physical action. Capability refusal explicit; no invented package refund path.

## 5. Approval closes delivery obligation, releases only unconsumed held quantity
Latest user rule: at APPROVED return with owning cancellation of corresponding ORDER-LINE delivery obligation, release relevant unused stock reservation ONCE. Creating Requested case alone is not release; parcel cancel/retry/return/timeout still is not release.
Integrate in Returns OWN approval entry so direct admin Returns routes and Order route cannot bypass it. Do not weaken whole-order fulfillment.cancel.already_dispatched rule.
A narrowly authorized Order obligation-closure contract plus Fulfillment source/branch closure and Inventory quantity-safe held release is required. Store durable case/source/line/quantity identity + phase so retry after ANY boundary resumes exact work:
1 validate payment/actor/source/claim, prepare durable approval intent;
2 authoritative Order/Fulfillment decision closes only corresponding obligation/cancels eligible Created replacement allocation; no shipped-history decrement or ordered/packed inflation;
3 Inventory releases only unconsumed held quantity assigned to that cancelled obligation (partial holds/sibling portions preserved); mark exact completion once;
4 only when required owning closure is confirmed may existing refund approval/execution proceed.
Never refund first and leave live replacement dispatchable. Failure/uncertainty is pending/reconcilable, not completed success; no broad cross-module transaction or compensating release of active-order stock. Existing retry/approval endpoints must be able to resume recorded phase, not reject because projection changed.
Consume != release: original consumed sale reservation is never released/recreated/consumed twice. Other active order quantities/holds untouched. Shortages/no replacement hold produce no fake release.
Customer money amounts/currency/refund beneficiary/destination use EXISTING authoritative order/payment/Returns snapshots, never client total or operator wallet. Creation cannot start refund. Preserve existing Payment/Wallet/Settlement integration and their idempotency; no new money engine.
Use existing refund success/failure/retry workflow after closure. A lost PSP response is not permission to double refund; same recorded provider attempt must reconcile/reuse authoritative identity before any new attempt. Scope fixes to introduced courier flow/approval boundaries; no unrelated whole finance rewrite.
No real provider call/wallet credit/customer refund in this task's verification; use controlled task-owned ports and distinguish that evidence from live API proof.

## 6. Physical goods disposition is independent and condition-aware
Healthy received units may enter saleable stock only ONCE via owning Inventory return policy when the established workflow permits it; damaged units NEVER call unconditional saleable RestockFromReturnAsync. Persist explicit condition/disposition or pending-disposition fact rather than pretending they vanished.
Prevent duplicate stock addition across physical origin receipt, refund success/retry/events, cancelled replacement release and an actual returned replacement. Only the physically received original source quantity is eligible for its disposition; release of a never-dispatched replacement hold is separate healthy stock.
Extend existing Returns item/Inventory port narrowly for condition-aware handling, or branch explicitly to a durable damaged disposition that does not increase saleable availability. Do not invent an entire quarantine/location subsystem. Never pass damaged quantity to existing unconditional restock and claim resolved.
Case detail shows money phase and physical disposition separately with short Persian labels. Refund failure doesn't undo confirmed physical/order closure; unpaid/no real payment -> exact blocker, no fake success.

## 7. Same professional operator surfaces
Existing shared parcel review + Returns screens; one contextual "ایجاد پروندهٔ مرجوعی"/"مشاهدهٔ پروندهٔ مرجوعی", no parallel page/framework/finance panel on finance-free shipping view.
Review selected source/quantities/conditions, consequences ("پس از تأیید، ارسال این تعداد متوقف و بازپرداخت طبق پرونده انجام می‌شود"). Explicit confirmation for approval/irreversible decision through existing MessageBox, success one react-toastify, local field validation, pending/refused/unknown distinguished; no raw enums/server diagnostics/sticky success.
Existing capabilities own actions, one action owner per parcel/group, full order and finance-free view use SAME components. Actual Returns link navigates case detail and preserves safe return path.
Preserve264-R3 mobile responsive header; verify touched dialog/list at390 and desktop once, no screenshot deliverable.
Tailwind content currently omits features/**/* (source-confirmed backlog). Do not rely on new feature-only classes that production doesn't emit. Use established emitted utilities/primitives and verify served computed styles; no unreviewed global Tailwind/style expansion under this task.

## 8. Focused acceptance: ONE connected scenario plus targeted integrity cases
Only canonical tooba_alpha5432 FE3000->Host5088 SingleStore/store-alpha; minimal genuinely labelled independent TB265 production-path fixtures allowed. Existing user/screenshot records and user method01a17d0d-... READ-ONLY. No new/disposable/Docker/15433 DB, edition/connection switch/reset/reseed/raw businessSQL/manualDDL/deletion/global reclaimer.
A paid fixture with receipt usable2+damaged1 -> case for selected qty; same request exact replay same case; changed intent conflict; no double case/source claim. Source-sibling isolation and current actor/owner/package refusal.
B creation leaves healthy replacement hold retained, no refund; concurrent resend/replacement/create cannot fund same qty twice (two independent contexts, no stress).
C decline/cancel Requested -> releases ONLY source branch claim, hold retained; own active quantities unaffected.
D approve partial courier Return -> exact corresponding obligation closed, unconsumed held portion released once, sibling hold untouched; original consumed sale remains consumed. Replays before/after each boundary no second release/refund/case.
E Existing pending replacement with retained hold/Created parcel -> deliberate audited Returns switch; already dispatched child refuses unsafe shortcut; no resurrection by old occurrence. Existing Referral can link to actual case without borrowing new capacity.
F healthy disposition counted once; damaged0 saleable increase; mixed quantities exact; refund success/failure/unknown/retry preserve closure and no duplicate funds. Provider/Wallet controlled fixture mocks with durable attempts; no real external money movement.
G Actual public admin command -> case visible on existing Returns GET/list/detail -> approval owning orchestration -> status/parcel caps refreshed. Real browser create/review on task fixture, safe close/reload of pending attempt, one notification, no duplicate action; backend phase proof may use controlled financial seam, disclose boundary and provide case/operation URLs.
Retain focused delivered-customer RMA eligibility/refund/healthy-restock regression so new origin doesn't weaken existing guard. Relevant258/262 source-capacity and263 outcomes only. No full-module/solution-test/base parity/green repeats; typecheck/build/publish relevant apps once after final change. New changes/failures justify related rerun only.
Minimal additive migrations allowed with private backup + repository MigrationRunner; old source/records readable, no historical reclassification/backfill. Preserve DataProtection/CapturedPreview localON7dates checked-inOFF;255 parked/Tipax deferred.
If a truly missing product decision prevents the defined policy, STOP with one precise decision after completing independent work; don't manufacture refund/stock truth. Don't split routine implementation into many discovery tasks.

## 9. Handoff
Persist TASK verbatim docs/ai/tasks/TB-SHIPPING-COURIER-ORIGIN-RETURNS-LINKAGE-AND-OBLIGATION-CLOSURE-265/TASK.md; evidence TASK/RESULT/RECEIPTS/result-contract/ownership-design with precise pending-phase/replay proof under docs/architecture/evidence/TB-SHIPPING-COURIER-ORIGIN-RETURNS-LINKAGE-AND-OBLIGATION-CLOSURE-265/.
Update AGENTS/START-HERE/recovery/263–265 plan:mobile264-R3 accepted within scope,265 result pending Architect/user review, no all-complete claim.
Commit AND push; report Base/Code/Merge/Evidence SHA, HEAD==origin/main, changed paths/Contracts ownership, exact creation-vs-approval release rule, phase boundaries/stock/financial proof and limits, minimal fixtureIDs/runtime/build/migration/actual review URLs and short user test. Do not call mocked refund live proof or claim provider integration.
One worker result then STOP_FOR_ARCHITECT; no next task automatically.
automaticNextImplementationTask: NONE
END_TOOBA_TASK
