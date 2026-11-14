# RESULT — TB-SHIPPING-EXECUTABLE-PARCEL-CREATION-AND-REAL-OPERATOR-HANDOFF-266-R1

| Field | Value |
| --- | --- |
| Status | **PASS** within the issued scope |
| Decision | `PACKED_ORDER_DEAD_END_DIAGNOSED_AND_TRUTHFULLY_EXPLAINED_REAL_BROWSER_CREATE_TRACK_DISPATCH_PROVEN_ON_ONE_COHERENT_CAPTURED_ORDER_AND_A_SECOND_COHERENT_ORDER_LEFT_ACTIONABLE` |
| Not claimed | NOT Architect ACCEPT, NOT user VISUAL/FUNCTIONAL ACCEPT, NOT a carrier/provider waybill, NOT shipping-complete |
| Base (expected) | `f4ca3cd4cc5b4f5c9e9cc0fb0eb818a09affcd9b` — verified ancestor of the work HEAD |
| Work HEAD before the evidence commit | `4d35ec1533e8b3304b3b819ee32ef6ac86ab1dec` |
| Worker | one worker, one task, no delegation, no historical agent resumed |
| `STOP_FOR_ARCHITECT` | yes · `automaticNextImplementationTask: NONE` |

---

## 1. What the dead end actually was

The reported page (`/fa/admin/orders/01a1daa2-519d-…/operations?fulfillmentId=01a1daa2-5929-…&from=queue`) belongs to
an order that has **zero captured shipping groups**: its checkout used the legacy default method
«ارسال پیش‌فرض فروشگاه» and no `order.order_shipping_groups` row was ever materialised.

The operations API nevertheless advertised `create_shipment`
(`shipmentCreationPossible: true`, `shipmentEligibleQuantity: 1`) because that projection is quantity/paid-based and
never asks whether an **accepted captured group** exists. The delivery section, however, can only open
`AdminCreateShipmentModal` from an `AdminAcceptedShippingGroup`; with none it rendered the generic amber strip
«…وضعیت سفارش را دوباره بارگذاری کنید.» and the unpack row action. That is the screenshot the user sent.

The reload instruction was the real defect: the group was never captured, so no reload can ever produce it. And no
lawful legacy route exists — `SellerMutateFulfillmentCommandValidator` refuses `CreateShipment` without a non-empty
accepted group id (`fulfillment.validation.shipping_group_id_required`).

Full evidence: `root-cause.md`.

## 2. What was repaired

`src/frontend/app/admin/admin-order-operational-delivery-section.tsx` — inside the existing empty-projection
branch, a permanent historical fact is now separated from a transient projection gap:

| Condition | Render | Test id |
| --- | --- | --- |
| `shippingGroups` empty **and** at least one packed line | «این سفارش گروه ارسال ثبت‌شده ندارد؛ زمان ایجاد آن، انتخاب روش و بازهٔ تحویل مشتری ثبت نشده است. برای همین ایجاد مرسوله برای این سفارش ممکن نیست و بارگذاری دوباره هم آن را تغییر نمی‌دهد. آزمایش کامل ایجاد مرسوله را روی سفارشی انجام دهید که گروه ارسال ثبت‌شده دارد.» | `admin-order-operational-delivery-no-captured-group` |
| genuinely transient empty projection | unchanged retry copy | `admin-order-operational-delivery-empty` |

No accepted group / snapshot / timezone / commit date is invented, no unknown group is routed to a seller
fallback, no new page or framework, no backend contract change, no migration.

Verified in a browser on the served build: the new strip renders on the exact rejected URL, the generic strip is
absent, and there is no create/forward parcel action there (`browser-live-verification.txt` §1).

## 3. Coherent trial data (two orders, total stays ≤ 6)

Both created through the **public storefront pipeline only** — projection → captured selection → commit →
sanctioned Development payment confirmation → `mark_processing` → `mark_packed`. No `UPDATE status='Paid'`, no
factory bypass, no fake payment reader in runtime.

| | Order A (worker proof) | Order B (left for the user) |
| --- | --- | --- |
| Checkout | `01a1db20-ae31-7000-91d1-0b63649ac5c3` | `01a1db2f-e36e-7000-b7d3-afb2fe86e81a` |
| Reference | `TB-20261114173514-01-73cd8a` | `TB-20261114175151-01-7d5f59` |
| Seller order | `01a1db20-ae44-7000-b652-c57ccb82dc94` | `01a1db2f-e375-7000-8197-9657a380a505` |
| Fulfillment | `01a1db25-7931-7000-9811-b473e938af48` | `01a1db2f-eb79-7000-b084-3d6b4add0fbb` |
| Order line | `01a1db20-ae48-7000-9d9b-97c8ff926390` | `01a1db2f-e379-7000-80f5-91d1401c225c` |
| Captured group | `01a1db20-ad98-7000-aa76-261f77230a71` | `01a1db2f-e351-7000-a516-71ab5b815d1c` |
| Method / window | `tipax·express`, 2026-11-17 09:00–10:00 | same |
| Carrier handoff | 2026-11-15 08:00–20:00 Asia/Tehran | same |
| Policy | v3 — prep 300 min, handoff 80 min, `preparationStartUtc 09:30Z`, `transferDepartureUtc 15:10Z`, `handoffDeadlineUtc 16:30Z` | identical |
| Final state | **Dispatched**, one parcel, tracking retained | **Packed**, 0 shipments, forward create live |

The queue/operations reads use the order's **stored** dispatch deadlines; nothing is recomputed from current
settings. The user's shipping draft `01a17d0d-e033-7000-af43-49a1daa28822` was never saved, published, withdrawn
or restored.

## 4. Real browser create → track → dispatch (not API-only)

Executed through the real controls on the served production build, authenticated as the Development admin actor:

1. «ایجاد مرسوله» (`admin-order-operational-create-01a1db20-ad98-…`, 44px) → `AdminCreateShipmentModal` with
   read-only method/window/quantity (1) → confirm inside the modal → **one** `Created` parcel.
2. «ثبت کد رهگیری» (`admin-order-shipment-assign-tracking-…`) → existing modal generated the internal identifier
   `TRK-01A1DB2C5F9F70008149360BCBC27C3A` → submitted through the modal's own action → stored.
3. «ارسال» (`admin-order-shipment-dispatch-…`) → confirmation → parcel `Dispatched`.

End state re-read and re-verified in the browser **after** the final production build:

```
GET /v1/admin/orders/01a1db20-ae31-…  -> fulfillmentStatus "Dispatched",
  line operationalStatus "Dispatched" (packed 1, allocated 1, shipped 1),
  shipment 01a1db2c-5f9f-7000-8149-360bcbc27c3a status "Dispatched"
  trackingReference "TRK-01A1DB2C5F9F70008149360BCBC27C3A" itemCount 1
GET …/operations -> supplyStatus "Fulfilled"; actions deliver_shipment + record_non_delivery only
served page -> «در مسیر تحویل — در انتظار ثبت تحویل», «کد رهگیری: TRK-…», 0 create buttons
```

`TRK-…` is the internal whole-parcel identity from TB-257 for a task-owned manual/internal shipment. It is **not**
a Tipax/post booking; no provider was called and no waybill is claimed.

Full sequence, controls and rendered text: `browser-control-handoff.md`, `browser-live-verification.txt`.

## 5. Second order proven still actionable in the browser

On `01a1db2f-e36e-…` the served page renders exactly **one** «ایجاد مرسوله» at **102 × 44 px**, one unpack action,
no duplicate seller/parcel/package control, and `scrollWidth − clientWidth = 0`. Owning API for the same order:
`create_shipment` present, `shipmentCreationPossible: true`, `shipmentEligibleQuantity: 1`,
`remainingPackedQuantity: 1`, `shipments: []`. So the user receives an actionable case, not a completed one.

## 6. Payment-authority proof on an EXISTING parcel

`src/backend/Modules/Fulfillment/Tooba.Fulfillment.Tests/Behavior/PaymentAuthorizedSupplyGate266Tests.cs` —
**7/7 passed**. Three new cases close the gap the task named:

- `Dispatch_after_payment_authority_lost_is_refused_without_dispatch_or_consumption` — after a real
  processing → packing → parcel-creation sequence, flipping the handoff to unpaid makes dispatch answer
  `fulfillment.order.payment_not_authorized`; the parcel stays `Created`, `DispatchedAt` null, `ConsumeCalls = 0`.
- `Bulk_group_selections_after_payment_authority_lost_are_refused_without_writing` — both grouped bulk entries
  refused with zero `QuantityProcessing` / `QuantityPacked` and the unit still `ReadyToFulfill`.
- `Consolidated_package_with_unpaid_member_refuses_dispatch_and_writes_nothing` — one unauthorized member refuses
  the whole package before any member effect; both parcels stay `Created`, package stays `Created`,
  `ConsumeCalls = 0`.

Disclosed limit: `IOrderFulfillmentReader` is the controlled source reader in these unit cases — the owning gate's
behaviour on a recorded payment fact, not a real provider payment. No guard was weakened.
Details: `payment-dispatch-proof.md`.

## 7. Tests and builds (run once after the final relevant change)

| Check | Result |
| --- | --- |
| Fulfillment `PaymentAuthorizedSupplyGate266Tests` | **7/7** passed |
| FE `admin-order-operational-delivery.mount.test.ts` | **63/63** passed (1 new TB-266-R1 static case) |
| FE `npx tsc --noEmit` | 0 errors |
| FE `npm run build` | 0 errors — first attempt crashed the build worker at static generation while the old `next start` still held `.next`; the running server was stopped and the same source built cleanly (documented, not a source fix) |
| `BUILD_ID` | `a07UMunhlqua0X_aHt-f5`, chunk `7825-dc4d3435934a5ee0.js` carries the new copy, served 200 |

## 8. Runtime

| Item | Value |
| --- | --- |
| Database | ONE canonical `tooba_alpha` @ 127.0.0.1:5432, SingleStore, tenant `store-alpha` |
| Host | 5088, PID 12752, `/health` 200, `/health/ready` 200 |
| Frontend | `next start` 3000, PID 38224, `/fa` 200, `BUILD_ID a07UMunhlqua0X_aHt-f5` |
| Migrations | **none added or applied** (no schema change) |
| Read-only counts after this task | checkouts 6 · seller_orders 6 · fulfillments 5 · `order_shipping_groups` 2 · shipments 1 · payments 5 · reservations 9 (`Held` 4) · `settlement_entries` 0 · stock_positions 3577 · `total_reserved` 4 |

No second/disposable/Docker database, no 15433, no edition/connection/port/appsettings change, no manual business
SQL, no reset/reseed/backfill/restore, no global reclaimer, no schema sweep. `DataProtection` keys and the
`CapturedPreview` flag are unchanged (local ON, checked-in OFF). All new rows are task-labelled
`TB266R1-*` / `TB-2026111417…`.

## 9. Cleanup audit (no repeat)

No cleanup was repeated. `cleanup-audit-266.md` appends the exact audit: the four private backups are intact and
`pg_restore --list`-validated (1425 TOC lines each), the restoration command is documented but not executed, the
private `cleanup.sql` and all audit helpers are present, `on_hand` was never written by the cleanup, and the
manual `reserved = 0` write is disclosed as a counter write that was only coherent because the whole reservation
journal was emptied in the same transaction. Two things stay explicitly **UNVERIFIED**: the per-reservation owner
mapping before the wipe, and any owner-safe-release inference from the surviving `total_reserved` sum.

## 10. Known limits

- The worker's create → track → dispatch ran on the served production build that was live at that moment
  (`yuIokX6A6PV337VQbzU1k`). This task's code change touches only the empty-projection copy branch, not the parcel
  controls; the rebuilt `a07UMunhlqua0X_aHt-f5` was then verified in the browser to serve and render the new strip
  and the still-actionable create on Order B.
- `TRK-…` is an internal identifier; no live carrier/Tipax API, no provider waybill, no real money movement.
- The four original TB266 orders remain uncaptured; they are not retrofitted and their amber strip now states the
  permanent truth.
- Payment-gate negatives use a controlled `IOrderFulfillmentReader`; the authorized positive path is the live
  Development trial, not a provider-paid order.
- Parent consolidated-package return/damage workflow and live Tipax remain deferred; `255` parked.

## 12. Repository

| Field | SHA |
| --- | --- |
| expected base | `f4ca3cd4cc5b4f5c9e9cc0fb0eb818a09affcd9b` (verified ancestor) |
| work head before this task's commit | `4d35ec1533e8b3304b3b819ee32ef6ac86ab1dec` (== `origin/main` at round start) |
| code + evidence commit | `9d2ed5efa8ca623825ef416e24f651a9913b6c47` |
| metadata commit (`result-contract.txt` + this section) | `0e8a2063c5e426339ce0a5450afb789816656a51` (pushed `4d35ec153..0e8a2063c`) |

Branch `main`, remote `origin https://github.com/mrnikiemami-code/Tooba.git`, pushed normally.


## 11. User handoff

`user-trial.md` — one starting URL, the three controls in order, the expected result of each, the order title
`TB-20261114175151-01-7d5f59`, and the meaning of the internal `TRK-…` code. No UUID deciphering, no setup
instructions, no next task proposed.
