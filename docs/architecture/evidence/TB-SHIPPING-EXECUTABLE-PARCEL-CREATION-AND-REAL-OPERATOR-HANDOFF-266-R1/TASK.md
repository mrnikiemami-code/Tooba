PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-EXECUTABLE-PARCEL-CREATION-AND-REAL-OPERATOR-HANDOFF-266-R1
Parent-Task: TB-SHIPPING-PAYMENT-GATED-DISPATCH-READABLE-QUEUE-AND-CLEAN-TRIAL-DATA-266
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: f4ca3cd4cc5b4f5c9e9cc0fb0eb818a09affcd9b
Mode: FIX_PACKED_ORDER_DEAD_END_AND_PROVE_REAL_BROWSER_CREATE_TRACK_DISPATCH_WITH_COHERENT_CAPTURE
STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

User rejection, exact target

User: «ببین من نمی فهمم مشکل چیه این خطا ها چیه کار درست تحویل بده الان فقط دکمه بازگشت از بسته بندی وجود داره من باید دقیقا چی رو تست کنم».
Screenshot: /admin/orders/01a1daa2-519d-7000-bb9c-c4d0150a1971/operations?fulfillmentId=01a1daa2-5929-7000-98a4-32f0a306553e&from=queue
Packed target, no shipment, unknown dispatch date, amber group-unavailable/reload strip, only unpack row action. This is NOT a valid user acceptance handoff. Stop asking user to discover missing setup/data. One worker, one bounded task, no delegation/historical agent, no broad suites/loops.
Read AGENTS, TB266 ARCHITECT-REVIEW and prior task. The last worker left zero Order shipping groups and claimed readiness; this correction must close actual displayed flow. Preserve accepted shipping/Returns model, no new page/framework.

1 Diagnose actual displayed dead end before changing it

Read real admin order detail + operations + group/carrier endpoints/network on this exact page, authenticated as same effective actor; capture structured reason/status without saving credentials. Distinguish an endpoint error, zero lawful accepted groups, projection eligibility, source/quantity mismatch, and client action mapping. Backend create_shipment projection alone is NOT proof button works: current shared panel opens creation with an AdminAcceptedShippingGroup and otherwise shows no forward create.
Trace UI through AdminOrderItemsShippingPanel -> shared operational/group/carrier component -> existing create modal -> public command -> owning Fulfillment. Record exact root and repair responsible seam. No generic "reload" for permanent missing historical facts.
For lawful captured orders: correct group identity, remaining quantity, method and permissions from owning Contracts/readers. Render exactly one clear «ایجاد مرسوله» at its proper existing action owner for Packed eligible target; no duplicate seller/row/package controls.
For truly uncaptured historical orders: if existing backend explicitly supports an executable legacy method/group route, expose that same lawful route without faking customer commitments; otherwise show concise truthful reason. Do not invent accepted groups/snapshot/timezone/dates or route every unknown group to seller fallback.
Keep stale precise locator unresolved with ZERO unintended actions. A fallback target card saying Packed is not a repair if actual creation remains impossible.

2 Coherent minimal user trial data, no new cleanup

The four existing TB266 orders remain; do NOT delete/reset/restore/reseed them again or silently retrofit their accepted shipping history. User may have interacted since last run. Add at most TWO task-labelled orders via existing public checkout projection/selection/commit and safe Development payment confirmation; total remains at most6. Reuse existing coherent task-owned capture if available. User shipping draft01a17d0d-e033-7000-af43-49a1daa28822 MUST NOT be saved/published/withdrawn/restored. Task-owned draft changes only if demonstrably necessary.
New trial MUST contain durable captured Order shipping group with configured accepted method and customer window, carrier week + preparation policy, actual owner/rate/destination. Queue reads ITS stored dispatch deadlines, never recomputes current settings. No direct SQL Paid/captured evidence, no fake payment reader in runtime, no direct factory bypass for positive handoff.
No live carrier/Tipax/payment/refund/wallet calls. Distinguish INTERNAL automatic tracking (TRK whole parcel identity from257) from a provider-issued waybill. INTERNAL tracking is explicitly authorized for task-owned manual/internal shipment demonstration. Do not use "no fake waybill" as reason to omit all internal create/dispatch/return workflow; never claim internal identifier is a post/Tipax booking.
If current configuration cannot lawfully create a captured order, resolve task-owned configuration/offer via existing owning commands, not permission weakening or rewriting user method. If truly external dependency blocks it, report PARTIAL with exact missing prerequisite, not READY/PASS.

3 Mandatory real browser execution, not API-only/bundle-text proof

Using a task-owned authenticated profile on SERVED production build (no screenshots required), perform the actual existing controls:
queue -> exact target -> visible «ایجاد مرسوله» -> review correct line/quantity/method -> confirm -> ONE persisted parcel -> reload -> assign/generate INTERNAL tracking through existing modal -> confirm -> reload -> visible «ارسال» -> confirm -> stored Dispatched and truthful state on same target/line/queue.
No direct API shortcut substitutes for these UI actions. Record URLs, control selectors, public requests/responses and owning IDs. Check no silent zero-POST click, generic unknown error, group-unavailable banner, duplicate mutation or actor mismatch. Use existing Toast once after confirmed action, MessageBox for blocking/irreversible review, local input validation; no permanent success strips or technical instructions.
If this reveals a concrete defect, fix it within this same bounded execution; don't return a known dead-end with another PASS.
Leave a SECOND coherent order at Packed with full accepted group and visible forward Create, ready for the USER to execute the SAME demonstrated sequence. Do not consume the sole actionable test case in worker proof and hand user only a completed one. If only one new order can cover both safely via separate lawful lines, retain clear own quantities; no fake remaining capacity.
Inspect actual page at390 and1440: forward action >=44px, modal fields and menu accessible, no clipped rows/document horizontal overflow; scoped table horizontal scroll allowed. Don't rerun unrelated accepted mobile typography/authoring suites.

4 Close narrowly missing payment-gate proof, not only no-Fulfillment404

Prior4 tests use InMemory and do not invoke dispatch/package/bulk. Add focused owning/public boundary cases for an EXISTING Created/tracked parcel whose source handoff is unauthorized: individual dispatch refuses before consume/state/event; package with one unauthorized member refuses whole package before any member effect; public bulk cannot bypass. Authorized positive path is the actual coherent live Development trial above. Exact recorded dispatch replay is historical and safe.
Use canonical PostgreSQL for integration only where necessary, no second/disposable/Docker/15433 DB. Keep fixtures minimal and labelled, no rerun of classes that manufacture hundreds of rows. Source reader control in a focused negative test is disclosed, not claimed real provider payment. No weakening payment/package/quantity/retained occurrence guards.
Existing method/order data not part of task stays read-only except explicit task-owned test controls; no user payments/returns changed.

5 Cleanup audit from266, NO repeat cleanup

Preserve original backup files and missing private cleanup script/manifests. Append exact audit record: what actually deleted/reconciled stock, before/after quantities, backup validation pg_restore --list and restoration command. Do not infer owner-safe release from final Reserved sum/entity COUNTS alone, do not fabricate missing logs or rerun destructive work to get them. If historical proof unavailable state UNVERIFIED and keep it separate from current flow fix. No new database restore. Don't expose credentials in committed receipts.

Delivery / definition of done

One canonical tooba_alpha5432 SingleStore/store-alpha, FE3000->Host5088, no edition/connection/port switch, DataProtection/CapturedPreview localON7dates checked-inOFF unchanged.255 parked, Tipax deferred, no parent-package return redesign. No global reclaimer/manual business SQL/backfill/delete/reset/schema sweep. Minimal code, focused tests once after final relevant change, affected typecheck/build/publish once; retry only on real failure/new edit.
Persist TASK verbatim docs/ai/tasks/TB-SHIPPING-EXECUTABLE-PARCEL-CREATION-AND-REAL-OPERATOR-HANDOFF-266-R1/TASK.md; evidence TASK/RESULT/RECEIPTS/result-contract/root-cause/browser-control-handoff/payment-dispatch-proof/user-trial.md under docs/architecture/evidence/TB-SHIPPING-EXECUTABLE-PARCEL-CREATION-AND-REAL-OPERATOR-HANDOFF-266-R1/. Update AGENTS/START-HERE/current recovery with exact root, what was proven and pending; no contradictory PASS banner.
Commit AND push main normally, full SHAs/origin parity/runtime/build. PASS only with one actual browser-completed create→track→dispatch and one separate still-actionable Packed coherent order left for user, plus negative payment dispatch proof. PARTIAL if missing, not "user can try".
User handoff in plain Persian: ONE starting URL, exact3 controls and expected result, order title, internal tracking meaning. No UUID deciphering/setup instructions. No further task proposed, no auto execution.
STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
END_TOOBA_TASK