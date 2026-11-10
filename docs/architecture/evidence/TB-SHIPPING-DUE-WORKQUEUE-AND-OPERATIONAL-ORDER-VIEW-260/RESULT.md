# TB-SHIPPING-DUE-WORKQUEUE-AND-OPERATIONAL-ORDER-VIEW-260 — RESULT

Task: `docs/ai/tasks/TB-SHIPPING-DUE-WORKQUEUE-AND-OPERATIONAL-ORDER-VIEW-260/TASK.md` (verbatim, unchanged)
Channel: `tooba-main` · Worker: `tooba-worker-01` · Mode: `DURABLE_DISPATCH_DUE_QUEUE_EXACT_TARGET_AND_SHARED_FINANCE_FREE_OPERATIONS`

Status: **PASS within the issued issue-5 scope**
Decision: **DURABLE_ACCEPTED_DISPATCH_TIMING_QUEUE_WITH_FULL_SET_COUNTS_EXACT_LOCATOR_AND_FINANCE_FREE_SHARED_OPERATIONAL_VIEW**
Base: `bb18f62d294e81ce86b8a09ae3eb8e2b5c84c873` · Code: `f3c15173c` · Evidence: this commit · Head: `main` == `origin/main` after the ledger commit
Stop: `STOP_FOR_ARCHITECT` · `automaticNextImplementationTask: NONE`

NOT Architect ACCEPT. NOT user VISUAL/FUNCTIONAL ACCEPT. NOT a claim about issues 1–4, which are already accepted
in their own bounded scopes. This task closed ONLY issue 5 (due work queue + shared operational order view).

---

## 1. What the operator gets

**`/admin/fulfillments` is now a dispatch work queue, not an order archive.**

* Five **server-authoritative** timing tabs, each with a count of the WHOLE search/column-filtered set (never a
  page tally): «سررسیدشده» / «ارسال امروز» / «عقب‌افتاده» / «آینده» / «موعد ارسال مشخص نشده», next to the
  existing nine operational-status tabs. The selected tab travels in the URL (`?filter=…`) and is restored on
  return, so the operator never loses their place.
* New grid columns: «بُعد زمانی ارسال» (bucket + truthful window hint), «موعد ارسال» (accepted carrier handoff
  only), «تحویل مشتری» (kept visually separate — this is the CUSTOMER promise, never a dispatch deadline),
  «اقدام بعدی» (the first operation the owning commands actually permit, with its precise blocker when the
  operation is not yet allowed). Each row keeps its physical order/seller/method/status/tracking context.
* Every row carries exactly ONE «عملیات ارسال» entry that navigates to the exact operational target:
  `/admin/orders/{checkoutId}/operations?fulfillmentId=…&shippingGroupId=…&from=queue&filter=…`. No bulk dispatch,
  no checkbox clutter, no duplicated child actions, no seller-aside duplicates.
* **A legacy row with no accepted plan is never invented:** it shows «موعد ارسال مشخص نشده» in the bucket column,
  «مشخص نشده» in the plan column, and is reachable through its own explicit filter.

**`/admin/orders/[checkoutId]/operations` is the shared, finance-free operational view.**

* It renders the **same** `AdminOrderItemsShippingPanel` / `AdminOrderOperationalDeliverySection` the full order
  page renders: one component, one set of server capabilities, one action owner, one retained envelope, one set of
  dialogs. The full order page is still one link away and keeps all of its financial sections; nothing was removed
  globally.
* Above the panel it shows only the exact target group(s) the locator resolved to: method, «موعد ارسال» (or
  «موعد ارسال مشخص نشده»), «تحویل مشتری», parcel summary and readiness — plus recipient/destination. A locator that
  no longer resolves is reported explicitly instead of silently showing a different group.
* No payment, settlement, refund, wallet or seller-share surface is present (`financeHits: []` measured).

## 2. Timing authority — one server rule, never the customer promise

`AdminFulfillmentDispatchTimingAuthority.Classify(pending, inTransit, plans, nowUtc)` is pure: no IO, explicit
`nowUtc`, the accepted carrier zone comes from the durable Order plan. Input is **only**
`IOrder.DispatchPlanFacts.ListPlannedForSellerOrdersAsync` over the accepted-plan read model written by TB-256
(`OrderDispatchPlanFactsBridge` — Contracts-only). Current Shipping settings are never consulted and the browser
never computes a date, a "today" or a bucket.

Pinned boundaries (exactly TB-256 semantics, half-open `[open, close)`):

| Situation | Bucket | Window state |
| --- | --- | --- |
| Pending, `now >= close`, or handoff day already passed | `Overdue` | `Closed` |
| Pending, handoff day == today (carrier zone), `open <= now < close` | `Today` | `Open` |
| Pending, handoff day == today, `now < open` | `Today` | `TodayNotOpen` |
| Pending, handoff day strictly future | `Upcoming` | `Future` |
| Pending, accepted evidence carries no plan | `Unspecified` | `None` |
| Nothing left to prepare, a parcel is dispatched/in-transit/non-delivered | `InTransit` | `None` |
| Delivered/cancelled with no open parcel | `NotQueued` | `None` |

Tabs overlap deliberately and truthfully: «سررسیدشده» = today-open + overdue; «ارسال امروز» = the whole today
bucket including a window that has not opened yet; «عقب‌افتاده» = overdue only. `InTransit` never contributes to
pending counts. Priority: overdue (earliest deadline first) → today → upcoming → unspecified → in-transit, with
handoff instant and row id as deterministic ties. Counts are computed by the same engine over the same filtered
set as the page, so a tab can never show 3 while the page is a different 3.

## 3. Focused proof (one canonical database)

| Case | Suite | Result |
| --- | --- | --- |
| A — authority boundaries | `AdminFulfillmentDispatchTiming260Tests` | **15/15 pass** (27 ms) |
| B — real query/pagination/filters/counts | `AdminFulfillmentWorkQueueDispatch260RelationalTests` | **1/1 pass** on real `tooba_alpha` |
| B′ — client contract mapping | `fulfillment-work-queue-260.test.ts` | **4/4 pass** |
| C — shared finance-free view | `admin-order-operations-view.mount.test.ts` | **4/4 pass** |
| D — read-only canonical browser | CDP DOM evidence in `dom/` | measured, see §4 |

Case A pins before-open / exact open / before-close / exact close, previous and next carrier-local day,
cross-zone midnight, a missing plan, cancelled/delivered, and due-but-not-ready. Case B receipt (real DB, fresh
run):

```
TB260-174243 planGroup=33c1324e-55da-4ea1-8c28-ea0cc8ec8c7c handoff=2026-11-10 window=00:00-23:59 zone=Asia/Tehran
  bucketA=Today/open rowB=Unspecified rowC=NotQueued
  counts=active2/today1/overdue0/upcoming0/unspecified1/transit0/due1
  filters=dueA/todayA/overdue0/upcoming0/unspecifiedB pages=3x1 distinct=3 writes=0
```

That is: a real durable plan classifies as today-open and is due; a plan-less row is `Unspecified` and is returned
by its own filter; a cancelled/delivered row is `NotQueued`; three tab filters return the truthful sets; paging is
stable across three distinct rows with no duplicates; **`writes=0`** (no user row touched).

Case C mounts the real `OrderOperationsView` + real `AdminOrderItemsShippingPanel`: the exact target card and the
accepted plan/customer commitment are separated, the same operational section is present, no financial marker is
present, an unauthorised viewer gets a readable card with **zero** mutation entries, and a retained unresolved
attempt keeps its **reconcile** entry (prepare entry absent) inside the same view.

## 4. Case D — real read-only browser on the canonical instance

Task-owned sessionless Chrome profile, `--headless=new`, **GET only** (no click, no screenshot, no mutation),
`dom/tb260-dom.json`, `dom/tb260-exact.json`, `dom/tb260-rows.json`, `dom/tb260-cell-dump.json`,
`dom/tb260-small.json`.

Queue (`/fa/admin/fulfillments`) at 1440 / 1024 / 390:

* 10 rows on the first page; tabs 44px each; `dir=rtl`; no horizontal document overflow
  (`scrollWidth 1425/1440`, `1009/1024`, `375/390`).
* tab counts measured `سررسیدشده=0`, `ارسال امروز=4`, `عقب‌افتاده=4`, `آینده=0`, `موعد ارسال مشخص نشده=496`;
  selecting `?filter=dispatch_today` narrows the page to exactly 4 rows and preserves the filter in every row link.
* Badges and cells are Persian with the server tone: `ارسال امروز · بازهٔ تحویل باز است` / `موعد ارسال مشخص نشده`;
  plan cells `2026-11-10 · 00:00–23:59 (Asia/Tehran)` and `مشخص نشده`; next step `شروع پردازش`. A regex probe for
  raw server enum words (`Today|Overdue|Upcoming|Unspecified|InTransit|TodayNotOpen|Closed|None|NotQueued`) inside
  the queue returns **false**.
* The exact locator from a real row (`64677682-…`) resolves to **exactly one** target card with
  «موعد ارسال مشخص نشده», the panel and the operational section, `financeHits: []`, `dir=rtl`, no overflow at all
  three widths, and `backHref=/admin/fulfillments?filter=dispatch_unspecified`.

Legacy canonical order `01a1b92d-e97e-7000-999a-143350d0edaf` (`/operations?from=queue`) at 1440 / 1024 / 390:
3 target cards, **all three** truthfully «موعد ارسال مشخص نشده» (historical null plan, nothing inferred),
customer receiving shown separately, operational + items panels present, `financeHits: []`, `dir=rtl`, every frame
control ≥ 44px, no overflow (`1425/1440`, `1009/1024`, `375/390`).

## 5. Deploy / runtime

* Source `D:\Users\User\source\repos\SarvNewVer`; **no migration added and none applied** (`git status` shows no
  `Migrations` change); nothing re-applied.
* Host republished to `artifacts\publish-primary` and restarted: PID **27312**, `/health` 200, `/health/ready` 200.
* FE rebuilt and restarted: PID **11948**, `BUILD_ID` **`OknVRb0IHssj09ZCN_TtH`**, `/fa` 200; the previous
  `BUILD_ID` `Zllw_pSMz566dDykRpaBe` is 404; the served chunk `7825-19f75933ca8b9455.js` (200) carries «مشخص نشده»
  and «موعد ارسال مشخص نشده».
* ONE database `tooba_alpha` @ `127.0.0.1:5432`; Edition `SingleStore`, deployment `store-alpha-canonical`,
  tenant `store-alpha`. No extra/disposable/test/parallel/Docker database, no 15433, no edition/connection/port
  change.
* DataProtection key file `App_Data\dataprotection-keys\key-d82e31a6-0269-4818-8766-c8267198bde9.xml` unchanged
  (2026-11-02). CapturedPreview local ON / checked-in OFF — untouched. No manual DDL, business SQL, reset, reseed
  or backfill; user order/cart/parcel/payment/service rows read-only.

## 6. Honest limits and disclosures

1. **Pre-existing legacy-fixture data defect, not introduced here.** 4 of the 5 sampled old fixture orders
   (`1857e08c…`, `2029bc02…`, `be2df021…`, `83597e4c…`) answer `500` on `GET /v1/admin/orders/{id}`:
   `Cannot convert string value 'Web' … mapped 'SalesChannel' enum` (Offer defines
   `Direct/Marketplace/Agency/Corporate/Affiliate/Api`). Those rows were written by earlier task fixtures outside
   the domain path; TB-260 does not touch `SalesChannel`, the checkout model or that mapping. For such a row the
   operational view degrades honestly — «سفارش خوانده نشد», retry, preserved back link, **no** mutation entry.
   Real storefront orders (`01a1b92d…`, `64677682…`) load normally.
2. **Plan-bearing proof is fixture-derived.** Existing canonical orders carry no accepted plan; TB-256/259 forbid
   faking dates or backfilling. Plan-bearing classification is proven by case A (deterministic authority) and case
   B (real committed-domain plan fixture `TB260-*`). No live plan-bearing real checkout is claimed.
3. **Timing is filtered by the server tabs, not by the column.** «بُعد زمانی ارسال» / «موعد ارسال» are sortable
   server-side; the query policy deliberately REJECTS a column filter on them (it would otherwise be accepted and
   silently ignored, and the grid would look filtered while it is not).
4. **Touch targets.** The queue quick tabs are 44px and every control added by this task in the operational frame is
   ≥ 44px (measured `below44: 0` on the legacy order). Two pre-existing surfaces are unchanged and smaller: the
   admin grid's compact row icon buttons (32px, the design-system pattern of every admin grid) and the items
   panel's per-line kebab (28px, identical on the full order page). Neither was introduced or restyled here.
5. No screenshots (not required by the task), no live Tipax/provider call, no fake waybill; 255 stays parked.
6. Concurrency/lost-response safety is inherited from 249/253/257/258/259 (retained envelope + reconcile entry,
   proven reachable in case C) — not re-proven as a stress test here.

## 7. Changed paths (code commit `f3c15173c`)

Backend

* `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Application/WorkQueue/Models/AdminFulfillmentDispatchTiming.cs` (new)
* `…/WorkQueue/Models/AdminFulfillmentWorkQueueModels.cs` · `…/WorkQueue/Queries/QueryAdminFulfillmentWorkQueueQuery.cs`
* `…/Fulfillments/Ports/ISellerFulfillmentAuthorizer.cs` (adds `CountAsync` to the owning query port)
* `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Infrastructure/Queries/AdminFulfillmentWorkQueueQueryEngine.cs`
* `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Tests/Behavior/AdminFulfillmentDispatchTiming260Tests.cs` (new)
* `src/backend/Modules/Order/Tooba.Order.Contracts/Shipping/OrderDispatchPlanFacts.cs` (new)
* `src/backend/Modules/Order/Tooba.Order.Infrastructure/Integrations/DispatchPlan/OrderDispatchPlanFactsBridge.cs` (new)
* `src/backend/Modules/Order/Tooba.Order.Infrastructure/OrderModule.cs`
* `src/backend/Modules/Order/Tooba.Order.Tests/Admin/AdminFulfillmentWorkQueueDispatch260RelationalTests.cs` (new)

Frontend

* `src/frontend/app/admin/admin-fulfillment-work-queue-screen.tsx`
* `src/frontend/app/fulfillment/fulfillment-api.ts` · `…/fulfillment-work-queue-dispatch.ts` (new) · `…/fulfillment-work-queue-260.test.ts` (new)
* `src/frontend/app/admin/orders/[checkoutId]/operations/page.tsx` (new) · `…/admin-order-operations-route-screen.tsx` (new)
* `src/frontend/features/order-operational-groups/ui/order-operations-view.tsx` (new) · `…/index.ts`
* `src/frontend/design-system/app-data-grid/admin-grid-query-client.ts` (optional `mapMeta`, existing callers unchanged)
* `src/frontend/app/admin/admin-order-operations-view.mount.test.ts` (new)

No schema, no migration, no Host/global CSS redesign, no `app/admin` flat-folder file, no new page framework.

## 8. Final user handoff — trial route and checklist (five repaired defects)

**Start here:** `http://127.0.0.1:3000/fa/admin/fulfillments` (queue) → «عملیات ارسال» on any row →
`…/operations` (shared finance-free view) → «بازگشت به صف کار» keeps the filter.

| # | Defect | Where to look | What to expect | Honest limit |
| --- | --- | --- | --- | --- |
| 1 | Tracking generator duplicates | queue row → order operations → parcel card → «تولید خودکار» → «ثبت کد رهگیری» | one whole-identity `TRK-<32 hex>` code per parcel; a second parcel never receives the same code; a taken code retries itself instead of repeating | no live Tipax / no provider waybill; historical short codes are not backfilled |
| 2 | Wrong remaining quantity | order operations → group card and «ایجاد مرسوله» | `packed − shipped − open` only; a wholly shipped group shows no executable line and **no** dead-end create entry | — |
| 3 | No non-delivery / return / resend | dispatched parcel card → «ثبت عدم تحویل» → «برگشت به مبدأ» → «تأیید دریافت در مبدأ» → «ارسال مجدد» | history/tracking preserved; only physically received usable quantity is resendable, once; resend is a NEW linked parcel | origin receipt is a one-time final receipt; damaged grants no saleable stock; a parent PACKAGE return stays explicitly locked/unsupported |
| 4 | Shared group forced one common package | compatible group → «آماده‌سازی ارسال گروه» | two choices «یک بستهٔ مشترک» / «مرسوله‌های جدا»; separate = member parcels, zero package rows, still complete | no arbitrary bin-packing/optimizer; one reviewed parcel per selected member |
| 5 | No due queue / no shared operational view | `/fa/admin/fulfillments` (tabs + counts, «موعد ارسال», «اقدام بعدی») and the operations route | server buckets from the durable accepted plan only; customer promise shown separately; one action entry per row; same operational components, no finance panels | existing canonical orders carry no accepted plan → they truthfully show «موعد ارسال مشخص نشده»; plan-bearing classification is proven by the tag tests/fixtures, not by a live plan-bearing checkout |

Widths to try: 1440 / 1024 / 390 (RTL, no horizontal document overflow, 44px targets in the queue and the
operational frame). Legacy read-only reference order: `01a1b92d-e97e-7000-999a-143350d0edaf`.

Not claimed by this handoff: Architect ACCEPT, user visual/functional acceptance, live provider behaviour, or a
live Marketplace multi-seller checkout.
