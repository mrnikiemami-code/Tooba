TB-SHIPPING-USER-ACCEPTANCE-NOTIFICATIONS-AND-RETURNED-RESEND-261 — WORKER RESULT
====================================================================================
Status: **PASS** within the issued correction scope.
Decision: **ONE_EXISTING_TRANSIENT_TOAST_FOR_COMPLETED_ACTIONS_CONTEXTUAL_CARRIER_RETURN_VOCABULARY_PARCEL_TRUTHFUL_LINE_STATUS_AND_TRUTHFUL_DAMAGED_CAPACITY_EXPLANATION_WITH_DAMAGED_RESOLUTION_HANDED_TO_262**
NOT Architect ACCEPT. NOT user VISUAL/FUNCTIONAL ACCEPT. NOT "all shipping problems fixed".
This task closes the notification/word/refresh/explanation defects; damaged-goods *resolution* is
explicitly NOT implemented and is handed to TB-262 (not issued).

Base / SHA
----------
Expected-Base-SHA     66242d2dd4c8b3a5d8ecb9e87083e34f914bfbbd (ancestor of HEAD, verified)
Work base HEAD        0d1481e4d5463951a88efa9d107ae5877d553e4d  (docs issue commit for this task)
Code+evidence SHA     __CODE_SHA__
Evidence/merge SHA    __MERGE_SHA__
Branch                main
Single worker, no parallel task, no other worker resumed.

1. Transient notification consistency (user override honoured)
--------------------------------------------------------------
- The EXISTING common mechanism is used (`react-toastify`, already configured once by the app). No new
  library, provider or parallel notification system was added.
- `features/admin-shipping-authoring/ui/authoring-publication-panel.tsx`: a confirmed publish/withdraw now
  calls `toast.success(...)` exactly once on the `ok` outcome. The persistent
  "published/saved successfully" success strip is gone from the DOM, so a GET, reload or re-render cannot
  replay it.
- `features/admin-shipping-authoring/ui/authoring-screen.tsx`: a confirmed save uses the same single
  `toast.success` and no persistent success strip.
- Shipping parcel/order actions in `app/admin/admin-order-items-shipping-panel.tsx` already used the same
  transient behaviour (`toast.success` / `toast.error`); unchanged, so the two surfaces now behave
  identically.
- Deliberately KEPT in contextual UI (they are not temporary success notifications):
  current publication status, permission/payment/inventory blockers, field validation, and the retained
  uncertain-request/reconcile row. No duplicate toast+inline copy of the same outcome.

2. Persian operational language and carrier-vs-customer meaning
--------------------------------------------------------------
- New `formatAdminShipmentStatus` in `app/admin/admin-api.ts` maps carrier exception states in operator
  Persian: `NonDelivered` «تحویل ناموفق», `Returning` «در حال برگشت به مبدأ», `Returned` «دریافت‌شده در
  مبدأ»; every other state falls through to the existing `formatAdminStatus` (no raw English enum, no
  hardcoded language list — the same Persian resource convention as the rest of admin).
- The customer RMA vocabulary («مرجوعی») is untouched and stays in the Returns domain; `formatAdminStatus`
  was not globally renamed.
- `app/fulfillment/fulfillment-api.ts` follows the same operator terms.
- `app/admin/admin-fulfillment-work-queue-screen.tsx` state labels corrected for the three carrier states.
- Used by the operational section/parcel cards, the seller shipping panel badges and order lines.

3. Owning-detail refresh after a physical return (the stale-badge defect)
-----------------------------------------------------------------------
- `Tooba.Order.Application/Admin/Detail/AdminOrderDetailFulfillmentStatus.cs`: line status is now derived
  from the physical parcels that actually carry the line (`ParcelsOfLine`).
  - all of a line's parcels `Returned` → line `Returned`;
  - mixed siblings → `Mixed` (one parcel never overwrites the whole line);
  - a `Created` resend → the line reports preparation, not delivery;
  - `PartialDispatched` when only part travelled.
- `AdminOrderDetailComposer.cs` passes the line's own parcels into that evaluation.
- No lifecycle change, no new state, no fake `Delivered`/`Cancelled`; historic shipped/packed quantities are
  untouched and nothing implies lifetime shipped was undone.
- `AdminOrderOperationsRouteScreen` readiness for a focused `Returned` parcel now reads that parcel's own
  receipt: usable capacity → «آمادهٔ ارسال مجدد»; an existing linked resend → «ارسال مجدد ثبت شده — پیگیری
  مرسولهٔ جدید»; damaged only → «دریافت‌شده در مبدأ — اقلام آسیب‌دیده نیازمند تعیین تکلیف»; nothing usable
  → «ظرفیت سالمی برای ارسال مجدد نمانده». It can never fall through to «کار فیزیکی باقی نمانده».
- Group summary wording separates travelling parcels from returned ones instead of calling a returned
  parcel «در مسیر».

4. The reported missing «ارسال مجدد» — measured root cause, not a guess
---------------------------------------------------------------------
Read-only inspection of the exact screenshot fixture (see `tb261-live-fixture.txt`):
checkout `13feece0-…`, returned parcel `a44aa20e-ecf3-4964-b828-bf1fc4a72eac`, shipping group
`9fc0c94d-…`, receipt `[{ orderLineId: 8e0a897b-…, quantity: 1.000000, condition: "Damaged" }]`.
`returnedUsableQuantity = 0` and `resendableQuantity = 0`.

So the absent resend button was **correct behaviour**: `ReturnedUsableQuantity` counts `Usable` only and a
damaged unit funds nothing. The actual defects were (a) the raw/generic badge and a stale «در مسیر تحویل»
line state and (b) silence — no reason and no next step. Both are repaired:
- the parcel card names the exception and states «۱ قلم آسیب‌دیده — نیازمند تعیین تکلیف»
  (`admin-order-post-dispatch-damaged-{shipmentId}`);
- consumed capacity names the EXISTING child resend parcel
  (`admin-order-post-dispatch-capacity-consumed-{shipmentId}`), so the operator is never sent to a dead end
  and never offered a duplicate;
- the line follows its parcel (live after deploy: line `8e0a897b` reads `Returned`; sibling `e4f00cd4` on
  another parcel still reads `InTransit`).
No row of this fixture was mutated; no receipt was created, changed or deleted.

Positive healthy path (unchanged authority, now visible and explained)
---------------------------------------------------------------------
- With own usable capacity > 0 the same parcel exposes exactly ONE deliberate «ارسال مجدد»
  (`admin-order-post-dispatch-resend-{shipmentId}`) at 44px, inside the existing reviewed dialog, bounded by
  `ResendableQuantity` of THAT source parcel.
- Submission still goes through the prebound durable occurrence envelope (TB-258-R3), single-flight, exact
  replay of the same ids on a lost response, and creates a `Created` parcel with
  `resendOfShipmentId = <immediate source>` on the existing tracking/dispatch path.
- No payment/reservation/actor/package lock was bypassed to display the button.

5. Changed paths
----------------
Backend
  src/backend/Modules/Order/Tooba.Order.Application/Admin/Detail/AdminOrderDetailFulfillmentStatus.cs
  src/backend/Modules/Order/Tooba.Order.Application/Admin/Detail/AdminOrderDetailComposer.cs
  src/backend/Host/Tooba.Host.Tests/AdminFulfillmentScopeSequenceTests.cs
Frontend
  src/frontend/app/admin/admin-api.ts
  src/frontend/app/admin/admin-fulfillment-work-queue-screen.tsx
  src/frontend/app/admin/admin-order-items-shipping-panel.tsx
  src/frontend/app/admin/admin-order-operational-delivery-section.tsx
  src/frontend/app/admin/admin-order-operational-delivery.mount.test.ts
  src/frontend/app/admin/orders/[checkoutId]/operations/admin-order-operations-route-screen.tsx
  src/frontend/app/admin/orders/[checkoutId]/operations/admin-order-operations-route.mount.test.ts
  src/frontend/app/fulfillment/fulfillment-api.ts
  src/frontend/features/admin-shipping-authoring/ui/authoring-publication-panel.tsx
  src/frontend/features/admin-shipping-authoring/ui/authoring-screen.tsx
  src/frontend/features/admin-shipping-authoring/tb261-notification.mount.test.ts (new)
Docs
  docs/architecture/evidence/TB-SHIPPING-USER-ACCEPTANCE-NOTIFICATIONS-AND-RETURNED-RESEND-261/*
No migration, no schema change, no new endpoint, no new lifecycle.

6. Proof (focused, once)
------------------------
Fulfillment `PostDispatchException` (pure + R1 + relational) on REAL `tooba_alpha`      36/36
Host `AdminFulfillmentScopeSequenceTests` (line follows its parcels)                    14/14
Order `AdminOrderDetail|AdminOrderPostDispatchReplay`                                  15/15
FE `tb261-notification.mount.test.ts` (one toast, no success strip)                     1/1
FE `admin-order-operational-delivery.mount.test.ts` (TB-261 damaged / consumed /
   positive capacity / mixed line)                                                     49/49
FE mounted real route `admin-order-operations-route.mount.test.ts`                      4/4
Frontend typecheck                                                                       0 errors
Frontend production build                                                                0 errors
Host publish / solution build                                                            0 errors
Receipt ids: `tb258-relational.txt`. Live before/after facts: `tb261-live-fixture.txt`.

7. Runtime
----------
ONE database `tooba_alpha` @127.0.0.1:5432 — Edition `SingleStore`, tenant `store-alpha`. No additional,
disposable, test, Docker or 15433 database; no edition/connection/port/appsettings change; no manual DDL,
business SQL, reset, reseed or backfill; no user order/cart/parcel/payment row written; no provider call;
no migration added or applied (none was needed).
Host republished to `artifacts/publish-primary`, restarted — PID **32736**; `/health` 200 and
`/health/ready` 200.
FE rebuilt (`BUILD_ID Iyke7WwKot_a3oQOuWbjJ`) and restarted — PID **39388**; `/fa` 200; the previous
build's chunk `992-24accfc967f39328.js` is 404 and the served chunk `8318-63ce8a3990f03d4b.js` is 200 and
carries the new damaged copy.
DataProtection key file and CapturedPreview state unchanged (local ON / checked-in OFF). 255 parked; live
Tipax deferred.

8. Remaining damaged-goods requirement — owned by TB-262 (NOT issued)
---------------------------------------------------------------------
`damage-resolution-262-handoff.md` records the exact owning seams and gaps, read-only:
Fulfillment owns the condition (`ReturnReceiptCondition.Damaged`) and the capacity authority
(`ReturnedUsableQuantity`/`ResendCommittedQuantity`/`ResendableQuantity` in `FulfillmentUnit`);
Inventory owns stock (`IInventoryReturnGateway.RestockFromReturnAsync` must NOT be used for damaged goods,
`IFulfillmentInventoryLifecyclePort.ConsumeAsync` must not consume the original reservation twice,
`ICheckoutInventoryReservationPort` has no replacement-reservation command, and there is no quarantine
concept at all); Returns owns the customer/financial decision (`IReturnAdminOperations`,
`IReturnSettlementReader`, `RefundSucceededIntegrationEvent`) and today has no link from a damaged origin
receipt line to a refund or replacement decision.
Nothing in this task pretends that damage is resolved.

9. Honest limits
----------------
- No migration and no new capability: only operator-facing truth, wording and derived line status changed.
- The screenshot fixture is proven only by read-only GET + the mounted/relational cases; this task did not
  create a new positive resend parcel in `tooba_alpha` (the positive path is proven by the committed
  relational/occurrence cases and the mounted dialog, not by a new live checkout).
- No live Tipax/provider call, no fake waybill.
- Concurrency/recovery inherited from TB-258-R3 (retained envelope, single-flight, exact replay), not
  re-stressed here.
- No whole-module/base parity sweep, no repeated green runs, no screenshot matrix.
- Legacy pre-existing fixture rows with `SalesChannel='Web'` still answer 500 on admin order detail; not
  touched by this task.

STOP_FOR_ARCHITECT. automaticNextImplementationTask: NONE.
