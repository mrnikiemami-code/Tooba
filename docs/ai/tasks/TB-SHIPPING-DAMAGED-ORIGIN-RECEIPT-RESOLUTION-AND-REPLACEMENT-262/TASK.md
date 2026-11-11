PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-DAMAGED-ORIGIN-RECEIPT-RESOLUTION-AND-REPLACEMENT-262
Parent-Task: TB-SHIPPING-USER-ACCEPTANCE-NOTIFICATIONS-AND-RETURNED-RESEND-261
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Mode: OWNED_DAMAGED_RECEIPT_DECISION_AND_HEALTHY_STOCK_REPLACEMENT_WITH_QUANTITY_CONSERVATION
Expected-Base-SHA: 4cf6bfbe084f7c4834205f6033064202bae2cf17
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

## 1. Read first / bounded outcome
Read CURRENT TOPs,261/ARCHITECT-REVIEW.md,261/damage-resolution-262-handoff.md, SHIPPING-USER-ACCEPTANCE-CORRECTIONS-261-262.md and mandatory backend/frontend feature skills, Inventory/Order/Fulfillment/Returns Contracts and existing design system.
261 accepted within correction scope, not user acceptance. Screenshot missing healthy resend was lawful: source receipt1 Damaged,0 Usable. This task closes the damaged-goods operational dead end. Do not redo261/256–260.
User wants working explicit resolution: damaged returned goods remain non-saleable; authorized operator can replace the damaged SAME ordered item from healthy stock, or leave/referral to existing customer-service/financial decision if unavailable. No automatic refund, product substitution, extra charge or wholesale warehouse system.
One worker/task. Verify safe main/origin; issue/docs descendants legitimate. No force/reset/amend/stash/rebase/unrelated overwrite.
NO loops: focused cases once, affected tests/build/publish only; no whole-module/base parity/screenshot matrix/repeated successful runs.

## 2. Existing ownership constraints — implementation authority
Fulfillment owns final physical receipt per source parcel/line/condition; Damaged capacity is never usable resend. Preserve all258 occurrence/source conservation and immediate lineage.
Inventory owns saleable stock and reservations. Existing RestockFromReturnAsync is after successful refund and has NO condition; NEVER invoke it for damaged units. Existing first-sale reservation is already consumed and must not be consumed/released again.
Handoff identifies missing quarantine/replacement-reservation contract: add the MINIMAL owned Inventory capability needed for a linked paid-order replacement, reusing its stock/reservation concurrency primitives. Do not force this through a fake new checkout/payment or direct DbContext call from another module.
Order owns paid-order/customer obligation + orchestration/actor authority. Returns/Settlement/Payment own financial decisions. Keep module boundaries Contracts-only.
Before coding, record short ownership/quantity design in task evidence, then implement; no separate design-only round unless an actual blocking product policy is found.

## 3. Durable damage resolution decision
For an actual Damaged receipt item expose one purposeful «تعیین تکلیف کالای آسیب‌دیده» entry in SAME operational parcel card/dialog.
Durable source-scoped resolution links checkout/orderLine/sourceShipment/receipt item, actual damaged quantity, actor/time, exact intent/occurrence and chosen outcome.
- Replace SAME SKU/variant, real seller and owning stock context from committed evidence; no client-supplied product/seller/location identity trust.
- Explicit operator quantity <= own outstanding damaged quantity AND undelivered/unresolved customer obligation. Do not borrow another source/group.
- Other outcome is referral/pending customer-service or existing financial handling, not a made-up successful refund. If money action exists, use its existing permission and real eligibility; no implicit financial write from a shipping button.
- A referral remains pending until actual owning resolution confirms; don't call it 'resolved' just because operator clicked a label.
- One damaged quantity cannot simultaneously fund replacement and refund/another replacement. Serializing/idempotent durable decision guards mandatory; current actor and scope rechecked server-side.
- Historical receipts remain unchanged/readable; no backfill or relabel. If one-time receipt1 Damaged was mistaken, do not edit it silently; audited receipt correction is NOT this scope.

## 4. Healthy replacement stock and parcel lifecycle
Replacement consumes NEW healthy stock funded by its OWN purpose-bound reservation, never invented Usable capacity or original consumed sale reservation.
Flow: review damage quantity -> durable authorized decision/intent -> acquire exact healthy stock reservation via Inventory-owned command -> prepare/pack explicitly via existing lawful operations (or explicit reviewed replacement preparation step) -> NEW linked replacement parcel -> existing tracking/dispatch/delivery.
Preserve accepted shipping group/method/recipient and original money; don't silently recalculate accepted delivery date/fees. No fabricated replacement deadline from old first-dispatch plan (unspecified unless authoritative commitment exists).
Link replacement to immediate damaged source AND exact resolution/reservation; keep distinction from healthy returned-goods resend. Do not merely treat every IsResend as 'skip inventory consume': replacement dispatch must consume its NEW reservation exactly once, while healthy resend consumes no fresh sale stock.
Replacement cancellation before dispatch releases only its NEW reservation once; damaged source stays non-saleable, still owns unresolved replacement obligation if appropriate. A cancelled occurrence retry doesn't silently recreate it.
No stock available => typed precise blocker, zero parcel and no leaked reservation; retry same unresolved intent safely, no silent substitute/order cancellation/refund.
Record physical damaged custody/non-saleable disposition through minimal owning Inventory fact if needed; because original already consumed, recording damage must NOT decrement available stock a second time or raise saleable stock. Do not invent full bin/location/quarantine transfer UI.
Multi-module failure windows need durable recorded links and exact recovery: decision-before-reserve, reserve-before-link, parcel-before-link, uncertain dispatch/cancel. Recover exact original reservation/parcel by occurrence, not scan/guess. Don't add generic saga framework; reuse owning operation patterns.
Same occurrence+same request replays exact ids; changed quantity/source/outcome conflicts before added allocations. Concurrent contexts cannot over-reserve/over-resolve. Public command exact replay reachable after fresh readiness disappears, with current actor authority.
Customer-delivered quantity counts physical terminal deliveries ONCE across original/resend/replacement chain, not ordered/packed inflation. Original dispatch/history untouched; no double sale/settlement/invoice. Correct target/card/line summaries and queue state reflect replacement being prepared/in transit/delivered and unresolved damage, not perpetual Mixed after all customer units truly delivered.
Mixed usable2+damaged1: healthy resend2 leaves healthy remaining0; replacement may fund ONLY outstanding damaged1 using NEW healthy stock. No second branch from old usable receipt.

## 5. Professional UI / integration
SAME full-order and finance-free operational component and queue, no parallel page/lifecycle. Server projects decision/replacement capabilities and precise blockers.
Dialog concise: damaged item/quantity, resolution choice, healthy availability and explicit confirm where available. No raw enums/tenant/source ids/architecture prose. All operational statuses localized contextually; no 'مرجوعی مشتری' label for carrier return.
Confirmed actions use ONE existing transient Toast. Field validation, genuine blockers and unresolved reconcile context stay relevant; no persistent success banner/duplicate same-outcome Toast+inline.
Retain full reviewed envelope durably before first POST, current viewer scope, single-flight, exact retry/remount/reconcile, no bare error-code release or storage-failure new POST. Preserve healthy resend actions separately; no damaged-to-usable conversion.
A linked created replacement shows its own tracking/dispatch action and source relationship; consumed capacity points at that existing parcel instead of silent missing button. Unknown result never permits blind new decision.

## 6. Focused proof — one canonical database
ONLY tooba_alpha5432 + FE3000 -> Host5088 SingleStore/store-alpha; no disposable/test/parallel/Docker DB/15433/edition switch.
Screenshot/reference user fixtures READ-ONLY. New minimal task-labelled production-domain/application fixtures in SAME DB authorized, exact IDs recorded. No raw business SQL/manual DDL/reset/reseed/backfill/deletion. Required additive owning migrations authorized after private backup via repository MigrationRunner; no business history rewrite.
Prove:
A damaged1 => healthy resend0, non-saleable custody fact no saleable increase/double stock deduction; own decision outstanding1.
B usable2+damaged1 => healthy resend2, remaining usable0; new replacement1 reserves healthy stock separately; over/foreign/duplicate resolution refuses.
C real owning end-to-end decision -> reserve -> explicit preparation -> replacement parcel -> tracking/dispatch/delivery; NEW reservation consumed once, original consumed unchanged; terminal customer obligation correct and original source history retained.
D cancellation releases only new reservation; no-stock has zero parcel/leak; crash between reserve/link and parcel/link + uncertain response recovers exact ids; changed intent conflict; two independent contexts cannot double-resolve1.
E unauthorized/payment/seller/group/package locks and refund-vs-replacement mutual exclusion; no delivered-customer RMA confusion. If no existing lawful financial resolution seam, expose pending referral honestly and report deferred financial automation, don't fake completed refund.
F mounted REAL panel/dialog+public API payload and owning command: durable-before-send, one same-tick request, retained retry, localized states, one Toast, no persistent success; source screenshot remains untouched.
Focused affected tests/typecheck/build/publish once. No broad suite/solution test sweep/base parity/repeated greens. No live provider calls/credentials/fake waybills. Stage255 stays parked; live Tipax deferred.

## 7. Deploy / recovery / required result
Canonical source D:\Users\User\source\repos\SarvNewVer, same runtime ports/edition/connection. Preserve DataProtection and CapturedPreview locally ON/7dates, checked-in OFF. Deploy changed components/migrations with private backup and exact pending inventory; verify health/API/served BUILD_ID.
Keep common-package child lock; parent-package damage/return orchestration remains unsupported unless existing safe parent seam already covers it. No silent child unlock or scope expansion to parent redesign.
Persist TASK verbatim docs/ai/tasks/TB-SHIPPING-DAMAGED-ORIGIN-RECEIPT-RESOLUTION-AND-REPLACEMENT-262/TASK.md and TASK/RESULT/RECEIPTS/result-contract + ownership design/fixture ids/proof docs/architecture/evidence/TB-SHIPPING-DAMAGED-ORIGIN-RECEIPT-RESOLUTION-AND-REPLACEMENT-262/.
Update TOPs (AGENTS.md, START-HERE-IF-CHATGPT-IS-LOST.md, SHIPPING-ARCHITECT-RECOVERY-2026-10-22.md) and261–262 correction plan; original5-defect closure is history, user acceptance remains pending. No automatic new task/all-complete claim.
Return BEGIN_TOOBA_WORKER_RESULT full Base/Code/Merge/Evidence SHA, HEAD==origin/main, exact ownership/contracts/migration, inventory ledger before/after, decision/parcel linkage, retry/concurrency/public-path tests, runtime/build/limits and real user trial entry. If safe replacement cannot be represented, exact PARTIAL with owning blocker; no status-only completion.
STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
END_TOOBA_TASK
