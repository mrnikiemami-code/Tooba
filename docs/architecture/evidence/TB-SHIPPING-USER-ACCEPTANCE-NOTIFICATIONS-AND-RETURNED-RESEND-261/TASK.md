PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-USER-ACCEPTANCE-NOTIFICATIONS-AND-RETURNED-RESEND-261
Parent-Task: TB-SHIPPING-DUE-WORKQUEUE-AND-OPERATIONAL-ORDER-VIEW-260-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Mode: UNIFY_TOAST_AND_PERSIAN_CARRIER_RETURN_STATES_AND_REPAIR_HEALTHY_RESEND_USER_PATH
Expected-Base-SHA: 66242d2dd4c8b3a5d8ecb9e87083e34f914bfbbd
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

## 1. Context / user rejection
Read CURRENT TOPs and docs/planning/shipping/SHIPPING-USER-ACCEPTANCE-CORRECTIONS-261-262.md, mandatory backend/frontend feature skills and design-system conventions.
Earlier5/5 was bounded code/evidence acceptance, NOT user ACCEPT. User now rejects actual experience. This is concrete correction, no broad refactor or test loop.
Only261 now; damaged-stock replacement262 PLANNED NOT ISSUED. Persist its exact owning seams/gaps in evidence for next task, do not implement replacement speculatively here.
Verify safe HEAD/origin; review/issue doc descendants legitimate. No force/reset/amend/stash/rebase/unrelated overwrite.

## 2. Transient notification consistency — explicit latest user override
Use the EXISTING common Toast mechanism/configuration, not a new library/provider/parallel system.
Authoring save/publish/unpublish and shipping action success/failure notifications in touched flows use the same transient behavior; remove persistent 'published/saved successfully' success strips after completion. Toast shown once, timeout consistent with app, not replayed on GET/reload/render.
Keep permanent current status, actionable permission/payment/inventory blocker, field validation, and retained uncertain request/reconcile state in the appropriate contextual UI; they are not temporary success notifications. No duplicate Toast+inline copy of the SAME outcome.
Latest user Toast directive supersedes old task bans on Toast for create/outcomes. Do not sweep storefront or entire application; codify rule in repo recovery/conventions for future work.

## 3. Persian operational language, correct meaning and refresh
Source confirmed generic admin-api.ts Returned='مرجوعی', and user reports raw Returning/etc.
Carrier exception states need context-specific mappings on queue/order/operations/cards/dialogs/target summaries:
NonDelivered «تحویل ناموفق»; Returning «در حال برگشت به مبدأ»; Returned «دریافت‌شده در مبدأ». Preserve separate delivered-customer RMA vocabulary. Do not globally rename every Returned enum across Returns/financial domains.
All known shipment/package/group/line states in Persian locale localized, no raw English enum fallback for operator. Other languages from existing registry/resource conventions; no hardcoded language list.
After mutation refresh SAME owning detail/operations/queue truth: target summary, parcel card, line status and readiness must agree about that source. A returned parcel must not keep a stale 'در مسیر تحویل' badge; mixed siblings get honest mixed summaries, not overwrite every item with one parcel status. Preserve historic shipped quantities; UI must not imply lifetime shipped was undone.
Use state+capabilities, not custom frontend lifecycle. No fake Delivered/Cancelled to make labels agreeable.
Dates use existing Jalali formatter, time ranges isolate LTR so start/end do not reverse; hide raw Asia/Tehran in routine summary unless useful, retain authoritative zone internally.

## 4. Diagnose and fix healthy return/resend end to end
User repro: courier motorcycle breaks -> package comes back physically intact -> operator receives it as سالم -> needs ارسال مجدد, not customer 'مرجوعی' dead end.
Read-only inspect exact screenshot fixture:
checkout13feece0-4890-4e45-b92f-4d4b1a5e0c97,
fulfillment3ce14fb8-6453-44c0-b364-0f919c8bf2e0,
shippingGroup2c550347-8c06-4a2b-b4f6-c7c77ab6e058.
Trace actual source parcel, final receipt conditions/quantities, own resend committed/capacity, existing child resend, current actor/payment/inventory/package locks and projected operations -> actual UI.
DO NOT assume absence is bug if receipt has zero usable quantity or a resend already consumed it. Show precise useful reason and existing linked resend when allocated; do not create a duplicate.
For real positive own usable capacity with lawful current authority: one visible deliberate ارسال مجدد -> reviewed exact quantity -> new linked Created parcel -> existing tracking/dispatch. Preserve immediate source lineage and full prebound occurrence durable-before-POST, exact replay/uncertain retained recovery/single-flight.
Legitimate blockers remain enforced; expose concise reason and EXISTING authorized recovery action if applicable. Never bypass payment/reservation/actor/package locks merely to display a button. Verify whether resend of SAME reserved returned goods wrongly depends on a fresh original reservation; any correction must follow owning Inventory/Order contracts and no second reservation consume.
Trace blocker from actual API, not screenshot guess. No mutation of this fixture or real user rows without separate authorization; use new minimal task-labelled production-path fixtures for proof.
Mixed receipt usable2/damaged1: healthy resend only2, damaged1 explicitly 'آسیب‌دیده — نیازمند تعیین تکلیف', no fabricated resendable quantity. No false whole-order completed/delivered while unresolved customer fulfillment remains.

## 5. Damage handoff — record honest remaining requirement
Inspect existing Inventory/Returns/Order resolution contracts READ-ONLY for planned262. Evidence must list owning seams for quarantine, replacement healthy-stock reservation, quantity-safe linked replacement and customer-service/financial handling; no disconnected status-only button.
Current damage receipt records damage and grants0 usable capacity; do not claim this completes damaged-goods resolution.
Do NOT silently convert damaged to usable, raise packed/ordered, decrement lifetime shipped, auto refund, release original sale reservation twice, add saleable damaged stock, swap customer-selected product or charge customer again.
262 remains not implemented in this slice. Put exact gaps and planned acceptance cases in recovery so new chat cannot lose them.

## 6. Focused proof / no loop
Same canonical tooba_alpha5432 / FE3000 -> Host5088 SingleStore/store-alpha ONLY. No extra/disposable/test/Docker DB/15433/edition switch.
Minimal clearly task-labelled fixtures via production paths permitted, exact IDs recorded. No user order/service save/publication, parcel/receipt/dispatch/payment changes on screenshot/reference rows. No raw business SQL/manual DDL/reset/reseed/backfill/deletion.
Prove once:
A mounted authoring success Toast once, no persistent success strip, GET/reload no repeat; field/blocker UI still correct.
B known operational statuses localized and correct carrier-vs-customer-return meaning; actual route target+parcel+lines refresh after source return with unrelated sibling preserved.
C production command flow courier failed -> Returning -> physical سالم receipt -> exact own capacity -> visible resend -> new linked parcel, repeat/lost response same ids; mixed سالم/آسیب‌دیده capacity2 only; capacity consumed points to existing resend, no dead end/duplicate.
D unauthorized, valid payment/inventory lock, package lock and zero capacity show truthful blocker, no bypass; actual screenshot root-cause evidence.
Affected tests + typecheck/build/publish only. No whole-module/base parity/full solution test sweep/repeated successful checks. Browser authorized read-only task profile, no screenshots required; no full matrix loop. If a concrete test fails, repair and rerun only affected check.
Deploy changed components canonical; preserve keys/CapturedPreview ON locally/7dates, checked-in OFF;255 parked, live Tipax deferred. No provider calls/credentials/fake waybill.

## 7. Result / recovery / stop
Persist TASK verbatim docs/ai/tasks/TB-SHIPPING-USER-ACCEPTANCE-NOTIFICATIONS-AND-RETURNED-RESEND-261/TASK.md and TASK/RESULT/RECEIPTS/result-contract + focused proof docs/architecture/evidence/TB-SHIPPING-USER-ACCEPTANCE-NOTIFICATIONS-AND-RETURNED-RESEND-261/.
Update AGENTS.md, START-HERE-IF-CHATGPT-IS-LOST.md, SHIPPING-ARCHITECT-RECOVERY-2026-10-22.md, closure plan and261–262 correction plan. Mark USER acceptance reopened;261 pending review;262 damaged resolution still planned NOT ISSUED. No 'all complete' claim.
Return BEGIN_TOOBA_WORKER_RESULT full Base/Code/Merge/Evidence SHA, HEAD==origin/main, screenshot missing-action cause, changed paths, notification semantics, positive healthy resend proof, exact remaining damaged ownership seams, runtime/build/limits.
STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
END_TOOBA_TASK
