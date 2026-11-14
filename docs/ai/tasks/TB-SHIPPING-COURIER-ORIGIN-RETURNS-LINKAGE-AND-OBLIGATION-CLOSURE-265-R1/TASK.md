PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-COURIER-ORIGIN-RETURNS-LINKAGE-AND-OBLIGATION-CLOSURE-265-R1
Parent-Task: TB-SHIPPING-COURIER-ORIGIN-RETURNS-LINKAGE-AND-OBLIGATION-CLOSURE-265
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Mode: REPAIR_DURABLE_ORDER_CLOSURE_SOURCE_EXACT_SETTLEMENT_ATOMIC_STOCK_RELEASE_AND_MONEY_DISPOSITION_RECOVERY_WITH_REAL_CASE_HANDOFF
Expected-Base-SHA: d6bdc88c99502e115ebd3e3d4e7f64087dadc0f2
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

## 1. Bounded repair of265, not a new stage
Read265/ARCHITECT-REVIEW.md, original265 task and CURRENT recovery, repository architecture/Contracts/AMSC/UI rules.265 is PARTIAL. Preserve origin model/three paths/Contracts boundaries, accepted scheduling/mobile. Fix concrete A–E below in ONE execution, then STOP.
One worker/task, no delegation, no broad whole-module/solution-test/base parity/green repeats/screenshots. Focused failing cases then affected checks; repeat only after actual failure/new change. Legitimate issue/docs descendants permitted; no force/reset/amend/stash/rebase/unrelated overwrite.

## 2. A — real Order-owned durable closure, not a read-only calculation
OrderReturnObligationClosure currently has only IOrderReturnReader and returns Closed=true / line.Quantity-request.Quantity with ZERO writes.
Implement an Order-owned durable case+claim+source+line quantity closure fact through the existing owning persistence/application seam and narrow Contracts. Exact approval identity replays same closure, changed source/quantity conflicts, cumulative approved closures bounded by still-owed obligation (including already delivered/cancelled parts), no closure of sibling portions.
Normal relevant Order/Fulfillment capability/obligation readers must consume it; no status-only record ignored by actual send/supply/recovery path. Don't decrement historical ordered/packed/shipped, don't weaken whole-order already_dispatched rule.
Persist/verify from an independent context. A call returning a calculated remaining number is NOT proof. Minimal additive migration allowed; old rows readable no backfill.
Only current authorized Returns approval closes quantity; a caller/client boolean isn't authority.

## 3. B — source/quantity-exact Fulfillment settlement and claim lifecycle
Current SettleApprovedAsync loops every DamagedResolution with matching line in the UNIT (not source parcel), cancels whole linked Created parcel, remembers only FIRST hold, and releases TOTAL case quantity from it.
Bind settlement to claim's exact source parcel/receipt, canonical per-line/condition quantities and owning damaged resolution(s). Persist exact allocation map/phase linking each cancelled quantity to its OWN hold.
- two source parcels of SAME line: caseA cannot cancel/releaseB.
- multiple lines/holds: each receives its OWN amount, no first-hold shortcut.
- partial Return: preserve unselected units/holds and their lawful delivery obligations. If current replacement parcel cannot be partially cancelled, use proper existing allocation cancellation/re-preparation with exact remaining stock ownership; never cancel extra customer obligation, free extra stock or mark entire resolution settled for a subset. No invented stock split outside Inventory.
- Created replacement branch claimed for Returns must not remain dispatchable for that claimed portion while case Requested; freeze/transfer its selected capacity through owning state. Reject/cancel Requested restores only lawful source claim, keeping stock Held; approved/failed-refund never resurrects send.
- serialize/reconcile shared claim-vs-resend-vs-replacement/dispatch transitions, not only two case claims. Same common owning lock/concurrency protocol observed by ALL competing writers; aggregate attach list and UI alone aren't race safety.
- enforce actual package lock/paid/ownership at command boundary. Current RunCourierReturnAsync bypasses ordinary projected gate and ClaimAsync only checks Returned; no direct-API locked-child shortcut.
- perform paid/line validation before a NEW durable claim, or record explicit recoverable creation intent. Failed preflight must not strand source capacity. If claim and case exist but LinkCase failed, exact retry must repair link, not merely return existingByKey snapshot forever. No blind release/remint.
- ActiveKey is built from whole line set, not independent per-line uniqueness; don't claim it alone proves overlap safety. Preserve lawful non-overlapping partial capacity and prove real common authority.
Record actual operator and source transition identity; no broad exception swallowing as generic conflict.

## 4. C — atomic Inventory partial release and exact replay
InventoryReturnObligationRelease currently decrements Positions via ExecuteUpdate BEFORE SaveChanges(reservation+inbox), with no transaction spanning both. Fault may free availability without durable record; replay lookup matches key only.
Use owning Inventory atomic transaction/concurrency primitives for reservation partial state + Reserved decrement + durable release identity/event. First durable commit contains ALL or NONE. Same key/reservation/quantity replays exact result; changed reservation/quantity conflicts before writes; concurrent keys cannot release same remaining held portion twice.
Release ONLY selected unconsumed held quantity, never whole sibling hold, no onHand increase from release. Preserve real Consumed/Released/Held status in response, not report every no-op as Released. Original consumed sale remains consumed.
Use distinct correct purpose identity if reusing existing inbox would alias restock semantics. No new generic idempotency framework.
Injected failure after position update/before commit + independent-context read -> no half-change; exact retry -> one decrement once. Partial hold amount/count/replay read from real storage, not mock counter only.

## 5. D — approval/rejection/refund/disposition recovery must not cross money boundaries
Current ReturnDirectory:
- Reject skips domain Reject for status!=Requested but ALWAYS ReleaseCourierOriginClaim; can release still-Requested claim of a case already RefundProcessing/partly closed.
- ExecuteRefund marks money succeeded, then marks item disposition BEFORE restock call inside same catch. Restock failure is then treated as refund failure or causes invalid Completed->RefundFailed.
- RetryRefund mints a NEW key without courier closure/unknown-money reconciliation; completed Approve replay isn't handled first.
Repair narrow courier path and introduced shared regression:
1 durable approval intent/serialized status+phase before side effects. Reject/cancel only legitimate Requested+no approval intent; otherwise typed refusal with zero claim/stock/money mutation. Exact terminal replay returns its own fact, not new approval/refund. Public Returns AND Order command paths must reach these rules and exact reconcile even after fresh projection disappears.
2 approval closure idempotent through each independent owner phase, no refund until confirmed source/Order/stock closure. Concurrent approve/approve/approve-vs-reject cannot interleave contradictory effects.
3 money outcome tracked independently of physical disposition. A known successful refund remains successful if goods disposition fails. Record pending/retryable physical task; retry must repeat ONLY unfinished disposition using SAME identity, never make another refund. Mark SaleableRestock confirmed only AFTER owning restock confirms; failure cannot persist false completed restock.
4 unknown network/provider outcome retains SAME durable financial attempt/idempotency identity and reconciles through existing owning gateway; never label unknown as ordinary failed and mint another refund key. A proven terminal failure may use established authorized retry policy after closure. No parallel refund engine/real provider test.
5 failed refund keeps obligation closed, holds already released stay released, no send re-enabled. Money success + stock failure must recover without invalid domain transition/double credit. Failed claim release after confirmed reject/cancel resumes that pending phase, no stuck hidden claim.
Preserve delivered-customer RMA guard/flow; shared disposition correction must not regress existing refunds/restock. No full finance rewrite; fix only these relevant owning boundaries. Expose precise pending states/actions rather than false success.

## 6. E — finish the actual operator path and localization
Current FE diff adds Create button only. No persisted View-case link after refresh; no Return screen mapping/render of new origin/closure/condition/disposition in supplied changes. New typed reasons need established error resources/admin mapping; no raw error or generic unknown when refusal is known.
Project actual linked Returns cases/remaining claims through existing owned read contracts -> shared parcel card: ONE "مشاهدهٔ پروندهٔ مرجوعی" per case and correct Create for remaining quantity, source/phase/condition/disposition on EXISTING Returns detail. No finance panel on finance-free shipping view.
Use existing reviewed modal, durable envelope before send, same identity retained on uncertain/pending create, exact server case recovery after reload, one Toast on confirmed create; MessageBox for irreversible approval/blocking acknowledgement, local validation; no stacked/duplicate surfaces.
One task-owned paid-fixture case created through ACTUAL public admin command from existing operational UI -> real existing Returns list/detail GET/navigation -> approval orchestration with controlled financial ports -> reload shows exact case and phase. UI proof cannot be only a mounted form plus live unpaid/no-action projection.
Provide exact fixture/review URLs. Minimal browser read/interaction no screenshots;390 and desktop touched screens only. Don't save user's records. Production CSS classes actually emitted (features glob backlog retained, no silent global style sweep).

## 7. Targeted proof — verify actual failure windows, not mocks of the missing authority
ONE canonical tooba_alpha5432, FE3000->Host5088 SingleStore/store-alpha only.
Reuse minimal labelled TB265 fixtures where safe; new minimal independent labelled owning-path fixtures permitted. No user/screenshot/user method writes; no real refund/wallet credit/provider. Task-owned controlled financial ports + real durable rows, clearly disclose seam.
A exact Order closure persisted/cumulative/partial replay via independent context, actual relevant reader observes closed remainder.
B same-line sourceA/sourceB + case for partialA -> noB parcel/hold change; multi-line case releases own quantities; selected Created replacement can't dispatch; remaining units/holds stay usable for proper obligation. Claim-vs-resend/replacement/dispatch two-context race, current authority/package guard.
C partial release fault at update-before-commit + replay/changed key intent, no half-written Reserved/reservation/inbox, no double release.
D fault after claim/before case and case/before link -> exact continuation, no orphan capacity. Approve-vs-reject and completed replay via PUBLIC commands; phase failures resume.
E refund SUCCESS then restock failure -> money once, no false SaleableRestock, disposition-only recovery; UNKNOWN refund -> same attempt/key, no second funds; known failure retains closed obligation. Reject Requested no hold release; late reject refuses.
F one actual positive operator/case detail link proof, localized errors and mobile, retained exact request.
Focused relevant tests only, affected typecheck/build/publish once; no broad/full-suite/base parity/stress. New fixes justify only related rerun. Update claims/evidence truthfully, don't label mocked closure as owning durable proof.
Minimal additive schema changes via repository MigrationRunner with private backup; no manualDDL/businessSQL/reset/reseed/backfill/delete/global reclaimer. Old data readable.
Preserve keys/CapturedPreview localON7dates checked-inOFF.255 parked/Tipax deferred; parent common-package return orchestration still unsupported with REAL child guards. Accepted scheduling/mobile untouched.

## 8. Handoff
Persist TASK verbatim docs/ai/tasks/TB-SHIPPING-COURIER-ORIGIN-RETURNS-LINKAGE-AND-OBLIGATION-CLOSURE-265-R1/TASK.md and evidence TASK/RESULT/RECEIPTS/result-contract/owning-phase proof under docs/architecture/evidence/TB-SHIPPING-COURIER-ORIGIN-RETURNS-LINKAGE-AND-OBLIGATION-CLOSURE-265-R1/.
Update AGENTS/START-HERE/recovery/plan:265 PARTIAL until265-R1 reviewed; no user/all-shipping acceptance. Preserve exact remaining phase/limits for next chat, no ambiguous PASS banners.
Commit AND push; full SHAs/HEAD==origin/main, changed owners, real source/quantity/fault proof, minimal IDs/runtime/build/migrations, real review URLs and user trial. No further task proposed or automatic execution.
STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
END_TOOBA_TASK
