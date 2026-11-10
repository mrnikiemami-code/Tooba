# TB-260 — RECEIPTS

Point-in-time observations on the canonical instance. One database, one runtime, no parallel deployment.

## Environment

| Item | Value |
| --- | --- |
| Base SHA | `bb18f62d294e81ce86b8a09ae3eb8e2b5c84c873` |
| Code SHA | `f3c15173c` |
| Database | `tooba_alpha` @ `127.0.0.1:5432` |
| Edition / deployment / tenant | `SingleStore` / `store-alpha-canonical` / `store-alpha` |
| Host | PID `27312`, `artifacts\publish-primary\Tooba.Host.exe`, `/health` 200, `/health/ready` 200 |
| Frontend | PID `11948`, `next start -p 3000 -H 127.0.0.1`, `BUILD_ID` `OknVRb0IHssj09ZCN_TtH`, `/fa` 200 |
| Previous FE build | `Zllw_pSMz566dDykRpaBe` → 404 |
| Served chunk | `/_next/static/chunks/7825-19f75933ca8b9455.js` → 200 (contains «مشخص نشده», «موعد ارسال مشخص نشده») |
| Migration in this task | NONE (no `Migrations` change in `git status`) |
| DataProtection | `App_Data\dataprotection-keys\key-d82e31a6-0269-4818-8766-c8267198bde9.xml`, 2026-11-02, untouched |
| CapturedPreview | local ON / checked-in OFF, untouched |

## Case A — timing authority (pure)

```
dotnet test src\backend\Modules\Fulfillment\Tooba.Fulfillment.Tests\Tooba.Fulfillment.Tests.csproj -c Release \
  --filter "FullyQualifiedName~AdminFulfillmentDispatchTiming260Tests"

Passed!  - Failed: 0, Passed: 15, Skipped: 0, Total: 15, Duration: 27 ms
```

## Case B — owning query on real storage

```
$env:TB260_CONNECTION = "Host=127.0.0.1;Port=5432;Username=admin;Password=123456;Database=tooba_alpha"
dotnet test src\backend\Modules\Order\Tooba.Order.Tests\Tooba.Order.Tests.csproj -c Release \
  --filter "FullyQualifiedName~AdminFulfillmentWorkQueueDispatch260RelationalTests"

Passed Tooba.Order.Tests.Admin.AdminFulfillmentWorkQueueDispatch260RelationalTests.
  Dispatch_buckets_counts_filters_and_paging_come_from_the_durable_plan_on_real_storage [1 s]
TB260-174243 planGroup=33c1324e-55da-4ea1-8c28-ea0cc8ec8c7c handoff=2026-11-10 window=00:00-23:59 zone=Asia/Tehran
  bucketA=Today/open rowB=Unspecified rowC=NotQueued
  counts=active2/today1/overdue0/upcoming0/unspecified1/transit0/due1
  filters=dueA/todayA/overdue0/upcoming0/unspecifiedB pages=3x1 distinct=3 writes=0
Passed: 1
```

## Case B′ / C — frontend contract + shared view

```
npx tsx --test app/fulfillment/fulfillment-work-queue-260.test.ts
  tests 4 / pass 4 / fail 0

npx tsx --test app/admin/admin-order-operations-view.mount.test.ts
  ✔ queue locator is explicit, and the frame contains no financial surface
  ✔ a locator that no longer resolves is reported instead of silently showing another group
  ✔ a group with no accepted plan never invents a dispatch date
  ✔ mounted: authority decides the mutation entry; retained attempt stays reconcilable
  tests 4 / pass 4 / fail 0
```

## Build / typecheck / publish

```
cd src\frontend ; npx tsc --noEmit                     → exit 0
cd src\frontend ; npm run build                        → exit 0, BUILD_ID OknVRb0IHssj09ZCN_TtH
dotnet publish src\backend\Host\Tooba.Host\Tooba.Host.csproj -c Release -o artifacts\publish-primary
                                                       → exit 0
```

## Case D — read-only canonical browser

Task-owned Chrome profile `D:\temp\tb260-chrome`, `--headless=new`, DevTools protocol, **GET only**.
Harnesses: `dom/harness/cdp-harness.mjs`, `cdp-exact.mjs`, `cdp-rows.mjs`, `cdp-cells.mjs`, `cdp-grid.mjs`.

Queue at 1440 / 1024 / 390 (`dom/tb260-dom.json`):

| width | rows | tabs | tab min height | dir | scrollWidth/innerWidth | latin enum leak |
| --- | --- | --- | --- | --- | --- | --- |
| 1440 | 10 | 0 / 4 / 4 / 0 / 496 | 44 | rtl | 1425 / 1440 | false |
| 1024 | 10 | 0 / 4 / 4 / 0 / 496 | 44 | rtl | 1009 / 1024 | false |
| 390 | 10 | 0 / 4 / 4 / 0 / 496 | 44 | rtl | 375 / 390 | false |

(tab counts are Persian-rendered `۰ / ۴ / ۴ / ۰ / ۴۹۶`; the nine pre-existing status tabs are still present.)

`?filter=dispatch_today` → 4 rows, tab `ارسال امروز` selected, every row link carries `filter=dispatch_today`.
Grid cell dump (`dom/tb260-cell-dump.json`): bucket `ارسال امروز · بازهٔ تحویل باز است`;
plan `2026-11-10 · 00:00–23:59 (Asia/Tehran)`; receiving `2026-11-10 · 09:00-17:00`; next step `شروع پردازش`;
a plan-less row renders `موعد ارسال مشخص نشده` / `مشخص نشده`.
Cell sampling across horizontal scroll (`dom/tb260-dom-grid.json`) confirms all three timing columns render real
server values, including the null-plan row.

Exact locator from a real queue row `64677682-ccd0-43ce-b3c8-20b804a2e273`
(`?fulfillmentId=01a1c5c2-ee27-7000-9809-a00147d07c82&from=queue&filter=dispatch_unspecified`,
`dom/tb260-exact.json`):

| width | state | targets | null plan | finance hits | overflow | back href |
| --- | --- | --- | --- | --- | --- | --- |
| 1440 | ready | 1 | 1 | [] | 1425 / 1440 | `/admin/fulfillments?filter=dispatch_unspecified` |
| 1024 | ready | 1 | 1 | [] | 1009 / 1024 | same |
| 390 | ready | 1 | 1 | [] | 375 / 390 | same |

Measured target text: `پیشتاز · آمادهٔ اقدام · موعد ارسال: موعد ارسال مشخص نشده · تحویل مشتری 2026-11-12 · 09:00–10:00 ·
مرسوله‌ها: بدون مرسوله`; `hasShippingPanel: true`, `hasOperationalPanel: true`.

An INTENTIONALLY unresolvable locator (`shippingGroupId=01a1b92db7e1700097a193f3bc3bcf0c` — no such member on
that order) returns `targetCards: 0` at all three widths with `minAction: 44`, `below44: 0`, `financeHits: []`:
the view reports that the locator no longer resolves instead of silently showing a different group.

The three 28px controls on the resolved row page (`dom/tb260-small.json`, `below44: 3`) are the pre-existing
per-line kebab buttons `admin-order-line-kebab-*` of the items panel — identical on the full order page, not
introduced here.

Legacy canonical order `01a1b92d-e97e-7000-999a-143350d0edaf` (no locator, `dom/tb260-dom.json` pages
`legacy-operations-all`): 3 target cards, 3 × «موعد ارسال مشخص نشده», operational + items panels present,
`financeHits: []`, `below44: 0`, `minAction: 44`, no overflow at 1440 / 1024 / 390.

## Read-only boundary

No click, no POST, no business mutation during case D; the only writes in the whole task are the task-labelled
`TB260-*` fixtures created by case B through production domain paths (`writes=0` relative to user data).
