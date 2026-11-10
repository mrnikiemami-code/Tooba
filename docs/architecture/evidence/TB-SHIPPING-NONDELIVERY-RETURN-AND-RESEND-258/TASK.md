PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-NONDELIVERY-RETURN-AND-RESEND-258
Parent-Task: TB-SHIPPING-TRACKING-UNIQUENESS-AND-QUANTITY-AUTHORITY-257
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: Tooba Shipping — record non-delivery, physical return and safe resend
Mode: COMPLETE_POST_DISPATCH_NONDELIVERY_RETURN_RECEIPT_AND_QUANTITY_SAFE_RESEND
Expected-Base-SHA: 4a3a241d2f75e0081a8ed8a375a061a4f987694b
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

## 1. Position and task boundary
Read CURRENT TOPs, closure plan256–260,257/ARCHITECT-REVIEW.md and mandatory backend/frontend feature skills, Shipping/Order/Fulfillment contracts/rules.
Safe current tree + actual origin parity; task/review/handoff doc descendants are legitimate, preserve unrelated changes.
Save prerequisite accepted; issues1 tracking and2 remaining quantity accepted within257. This task ONLY issue3: a dispatched parcel returned by courier cannot currently be recorded, because only Deliver is offered and Cancel is pre-dispatch.
User authorizes sequential continuation after Architect reviews each result, final handoff after five issues. Execute ONLY258;259/260 NOT ISSUED. No automatic next implementation.

## 2. Required domain semantics — do not turn physical return into cancel
Current flow: Ready -> Processing -> Packed -> Created parcel -> tracking -> Dispatch -> Deliver.
Keep it. Add explicit POST-DISPATCH exception path:
- «ثبت عدم تحویل»: valid for dispatched/in-transit, records reason, operator and time; does NOT pretend delivered/cancelled/physically returned.
- «برگشت به مبدأ»: record return-in-progress when carrier is bringing it back; no freed resend quantity yet.
- «تأیید دریافت در مبدأ»: requires actual receipt, explicit confirmation and received quantities; records actor/time/condition (usable/damaged or owning existing condition convention), makes only eligible physically received quantity available for controlled resend.
- «ارسال مجدد»: a NEW parcel occurrence linked to failed/returned original and exact eligible lines/quantities, preserves accepted shipping group/method; new tracking then existing dispatch/delivery path.
Non-delivery before return can resolve by a later genuine delivery or retry-in-transit if legal; reject inconsistent transitions. Never require a fake Delivered before returning.

Use existing owning Fulfillment/Order domains/Contracts/events/operations and real actor authorization. Inventory/Returns integrations only through their owned Contracts. Do not create a disconnected status table or frontend-only lifecycle.
This is NOT customer RMA after successful delivery. Keep existing delivered-customer-return path separate.
Do not add a general cancel-after-dispatch loophole or erase DispatchedAt/history.
An accidental dispatch correction (package never left) is NOT this physical return scenario; leave unsupported unless an existing audited correction seam already covers it. Do not broaden to a new universal undo framework.
No automatic order cancellation/refund/payout/reservation release. No price/accepted fees changes. No fabricated provider cancellation or carrier success.

## 3. Quantity, state and inventory invariants (mandatory)
Inspect actual meanings of QuantityOrdered/Processing/Packed/Allocated/Shipped, reservations, dispatch events and downstream consumers before editing.
Lifetime dispatch/history cannot be erased just to regain remainder. Returned and resent movements need explicit durable accounting.
Exactly-once receipt and resend allocation:
- only actual usable quantity physically received can become resendable;
- never delivered quantity, still-in-transit quantity, missing/damaged quantity, another group's goods, or already reserved/resent quantity;
- repeated receipt/refused/double-click/retry does not grant quantity twice;
- two concurrent resend allocations cannot both consume the same returned units;
- partial receipt/resend supports exact per-line quantities (positive, no foreign/duplicates/excess);
- cancel NEW pre-dispatch resend parcel releases its own reservation once; original parcel history/tracking remains intact;
- existing fresh unshipped quantity from257 is separate from returned-resend allowance; no double-count.
Preserve original order quantities and money. Do not simply decrement lifetime shipped or raise ordered/packed to trick existing first-allocation guards. Use a minimal explicit returned/resend capacity seam with conservation proofs and update all affected shared quantity readers/projections accordingly.
Ensure successful resend does not double-consume initial inventory reservation, double-book sales/settlement, or emit an indistinguishable second original-shipment integration movement. Return receipt inventory/hold ownership and damaged-vs-usable treatment must be explicit and atomic/idempotent through existing contracts; no automatic saleable stock increase while the same goods remain reserved for this order.
If existing integration cannot safely represent these facts, disclose exact blocker and return PARTIAL instead of a UI-only workaround.

Aggregate status must reflect actual parcel delivery, not merely QuantityShipped>=ordered. Do not mark whole fulfillment Delivered if one parcel was only dispatched/failed/returning. Returning/received originals and new resend parcels have coherent per-parcel actions/statuses. A delivered resend counts once toward customer-delivered quantity.
Do not change original accepted groups or use method reselection for resend. Preserve249/253 prebound occurrence/envelope/replay before remaining gate. New resend occurrence distinct, retry same intent exact target; no blind remint on uncertain response.

## 4. Shared-package and provider boundaries
A locked child inside active consolidated package cannot independently record return/delivery/cancel to contradict its parent.
Reuse existing single action-owner rule: expose coherent parent package exception/receipt workflow for ALL members as appropriate; propagate exact quantities/events once and no duplicated child buttons. Partial receipt must not falsely return every child.
If scope cannot safely cover a reachable package case, refuse it with a localized precise blocker and report limit, do not silently unlock children.
CarrierApi continues dedicated provider path. Local operator non-delivery/physical receipt is an audited internal fact, NOT a provider cancellation/label/HTTP call. Respect existing provider capability/authority gates; no fake returned carrier state, no live Tipax or credentials request.

## 5. Professional existing UI and API wiring
Same existing operational order section and action projection, no parallel page/state machine:
- in-transit parcel offers concise contextual «عدم تحویل» alongside lawful «ثبت تحویل»;
- dialog short: reason, useful note, actual event time if owning model supports it; actor assigned server-side;
- return receipt dialog exact items/quantity/condition and clear action, no finance/technical paragraphs;
- received usable goods offer «ارسال مجدد», with same accepted method/customer details shown read-only; retained input, single-flight submit, one inline error, explicit uncertain reconciliation.
No raw enum/code/tenant fields, no duplicate toast+inline, no checkbox clutter outside purposeful dialog.
Server defines and enforces transition/actor/owner/group/readiness/quantity validity, stale UI is not authorization.
Wire commands/queries/validators/error localization/operation lists/admin and existing seller equivalents under correct responsibility scope. No raw body trust or bypass of projected authority.
Use same configured languages, existing design-system dialogs and public feature boundaries, no flat-folder debt/baseline changes. Preserve approved authoring tabs/save.

## 6. Schema, proof, same canonical database
ONE DB tooba_alpha ONLY. NO extra/disposable/test DB, Docker PostgreSQL or15433.
Additive migrations necessary here authorized after private backup, exact target/pending inventory and existing MigrationRunner. No manual DDL/business SQL/reset/reseed/backfill rewriting accepted or dispatched history. Existing parcels remain readable with null/new defaults; do not falsely mark old parcels returned.
User orders/parcels/payment remain read-only. For storage workflow proof use task-labelled minimal production-path fixture in this SAME DB, verified ownership/IDs; no fake paid-history/customer copy or raw business SQL. Supported domain/application paths only, no real provider. Log exact created records, no sweeping delete.
Meaningful focused cases:
A dispatched -> non-delivery -> return progress -> actual receipt -> new resend -> track -> dispatch -> customer delivery, preserved original.
B delivered cannot be marked undelivered, illegal transition/refused actor/cross-owner/group.
C non-delivery alone grants zero resend; partial receipt, damaged/missing, receipt retry, resend cancellation, existing unshipped quantity distinct.
D concurrent independent contexts same receipt/resend race -> no duplicate allowance/allocation; exact-occurrence retry same new parcel, uncertain outcome reconcile.
E original+resend conserve per-line inventory/customer-delivered quantity; no double reservation consume/financial movement; one other in-transit parcel keeps overall notDelivered.
F shared-package lock/action-owner safety.
G mounted dialog real commands/payload/validation/pending/inline error, exact parcel targeting.
Real persistence proof for durable invariants in SAME DB, pure/mounted complements, never substitute InMemory for concurrency. If a safe fixture path is unavailable, exact PARTIAL rather than another database.
Run affected focused tests + typecheck/build; no full unrelated suites or repeated broad proof loop, no screenshots. Existing unrelated reds disclosed without cleanup/rebaseline.

## 7. Canonical deployment and handoff
Source D:\Users\User\source\repos\SarvNewVer.
FE3000 -> Host5088 SingleStore/store-alpha/tooba_alpha only.
Preserve DataProtection and local CapturedPreview ON/7dates, checked-in OFF;255 parked, Tipax deferred.
Deploy changed components + required additive migrations from canonical; verify runtime/API/served build.
Authorized task-owned read-only DOM on existing review order
http://127.0.0.1:3000/admin/orders/01a1b92d-e97e-7000-999a-143350d0edaf
Dispatched parcels should expose the correct NEW exception action without mutating them.
Full write workflow only on clearly labelled task fixture; provide fixture review locator and exact proof.
Do not tell user five issues complete; only third covered here. Worker stops after result; Architect reviews then issues259 if acceptable.

## 8. Recovery/result
Persist exact TASK/RESULT/RECEIPTS under task/evidence dirs.
Update closure plan + AGENTS.md + docs/prompts/START-HERE-IF-CHATGPT-IS-LOST.md + docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-22.md.
Record transition chart, counters/inventory semantics, real provider vs internal facts, schema/runtime identity, exact proof limits and next task not issued.
Ledger save accepted;1/2 accepted257;3 implemented/pending-review or PARTIAL;4pending259;5pending260.
Commit/push normally verify safe local==origin/main, no force/amend/reset/stash/rebase.
Return complete BEGIN_TOOBA_WORKER_RESULT with actual Base/Code/Evidence SHA, PASS/PARTIAL/BLOCKED, real migration/proof/runtime/locator and limits. Missing conservation/concurrency/command wiring -> PARTIAL, not full PASS.
STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
END_TOOBA_TASK
