# RECEIPTS — TB-SHIPPING-EMPLOYEE-WORK-QUEUE-JOB-STAGES-AND-TIME-SCOPES-271

Point-in-time observations of one worker on the canonical workstation
(`D:\Users\User\source\repos\SarvNewVer`, Windows PowerShell). Times are worker-local (UTC+03:30) unless marked `Z`.

## 0. Envelope

| Field | Value |
| --- | --- |
| Bridge-Task-Row | `a4ddd1a9-8d79-44da-95c5-b8a8e778452f` (`Claimed`, channel `tooba-main`) |
| Expected-Base-SHA | `c501fe9bcdb2c42a26d05e23d44af288931612ed` |
| Base-SHA (verified) | `c501fe9bcdb2c42a26d05e23d44af288931612ed` |
| Code-SHA | `114c48ce6a65b8c57423ae5673325c199e24a1c8` |
| Evidence-SHA | the commit adding this document set (`result-contract.txt` records it) |

```
git rev-parse HEAD        -> c501fe9bcdb2c42a26d05e23d44af288931612ed   (== Expected-Base-SHA)
git rev-parse origin/main -> c501fe9bcdb2c42a26d05e23d44af288931612ed
git merge-base --is-ancestor c501fe9bcd HEAD -> exit 0
```

## 1. Changed paths (code commit)

Added:

```text
src/backend/Modules/Fulfillment/Tooba.Fulfillment.Application/WorkQueue/Models/AdminFulfillmentJobStage.cs
src/backend/Modules/Fulfillment/Tooba.Fulfillment.Application/WorkQueue/Models/AdminFulfillmentLegacyQueueSelection.cs
src/backend/Modules/Fulfillment/Tooba.Fulfillment.Application/WorkQueue/Models/AdminFulfillmentWorkQueueCountsBuilder.cs
src/backend/Modules/Fulfillment/Tooba.Fulfillment.Tests/Behavior/AdminFulfillmentJobStage271Tests.cs
src/frontend/app/admin/admin-fulfillment-work-queue-columns.tsx
src/frontend/app/admin/admin-fulfillment-work-queue-271.mount.test.ts
```

Modified:

```text
src/backend/Modules/Fulfillment/Tooba.Fulfillment.Application/WorkQueue/Models/AdminFulfillmentWorkQueueModels.cs
src/backend/Modules/Fulfillment/Tooba.Fulfillment.Application/WorkQueue/Queries/QueryAdminFulfillmentWorkQueueQuery.cs
src/backend/Modules/Fulfillment/Tooba.Fulfillment.Infrastructure/Queries/AdminFulfillmentWorkQueueQueryEngine.cs
src/backend/Modules/Fulfillment/Tooba.Fulfillment.Infrastructure/Queries/AdminFulfillmentWorkUnitProjection.cs
src/frontend/app/admin/admin-fulfillment-work-queue-screen.tsx
src/frontend/app/admin/admin-fulfillment-work-queue.test.ts
src/frontend/app/admin/orders/[checkoutId]/operations/admin-order-operations-route-screen.tsx
src/frontend/app/admin/orders/[checkoutId]/operations/page.tsx
src/frontend/app/fulfillment/fulfillment-api.ts
src/frontend/app/fulfillment/fulfillment-work-queue-260.test.ts
src/frontend/app/fulfillment/fulfillment-work-queue-dispatch.ts
```

`git diff --stat` before the code commit: 11 files changed, 687 insertions(+), 610 deletions(-) —
`admin-fulfillment-work-queue-screen.tsx` −702/+183 (moved into the extracted columns module, 537 lines).
No `Migrations` path, no schema, no settings/appsettings, no `tooba_alpha` write, no fixture.

## 2. Focused checks (each once after the final code)

```text
dotnet test src\backend\Modules\Fulfillment\Tooba.Fulfillment.Tests\Tooba.Fulfillment.Tests.csproj -c Release ^
  --filter "FullyQualifiedName~AdminFulfillmentJobStage271"
-> Passed!  - Failed: 0, Passed: 15, Skipped: 0, Total: 15, Duration: 30 ms - Tooba.Fulfillment.Tests.dll (net8.0)

npx tsx --tsconfig tsconfig.mount-tests.json --test app/admin/admin-fulfillment-work-queue-271.mount.test.ts
-> tests 10 | pass 10 | fail 0 | duration_ms 5350

node --experimental-strip-types --test app/admin/admin-fulfillment-work-queue.test.ts ^
  app/fulfillment/fulfillment-work-queue-260.test.ts ^
  app/fulfillment/fulfillment-work-queue-persian-dates-269.test.ts ^
  app/fulfillment/fulfillment-api.test.ts
-> tests 28 | pass 28 | fail 0 | duration_ms 312

npm run build        -> 0 errors, BUILD_ID 3jjJwEQe78ttzvxLwFBNg (40.7 s)
dotnet publish src/backend/Host/Tooba.Host/Tooba.Host.csproj -c Release -o artifacts\publish-primary
                     -> 0 errors (Tooba.Host -> artifacts\publish-primary\)
```

Architecture guards (run once, read-only, **not** part of the shipped behaviour):

```text
npm run test:architecture
-> pass 8 / fail 3
   FE-FOLDER-001 novel flat admin files: 17 (15 already novel at HEAD; this task adds
     admin-fulfillment-work-queue-columns.tsx + admin-fulfillment-work-queue-271.mount.test.ts)
   FE-FOLDER-002 admin-api exports: unchanged file (admin-api.ts untouched by this task), already failing at HEAD
   FE-SIZE 11 already-oversized paths, identical set at HEAD:
     admin-order-operational-delivery-section.tsx 1854, admin-order-operational-delivery.mount.test.ts 4276,
     admin-shipment-plan-270r2.mount.test.ts 1010, fulfillment-api.ts 845@HEAD -> 874@worktree,
     storefront-shipping-api.ts 1064, storefront-shipping-group-cards.tsx 870,
     authoring-messages.ts 1030, authoring-screen.tsx 1211, order-group-carrier-execution.mount.test.ts 961,
     order-group-carrier-execution.tsx 1024, post-dispatch-exception-dialog.tsx 1170
   (HEAD LOC verified via `git show HEAD:<path>`; no baseline entry was rewritten; the screen itself is 537 lines)
```

## 3. Deploy (one canonical environment)

| Item | Value |
| --- | --- |
| Database | `tooba_alpha` @ `127.0.0.1:5432`, SingleStore, tenant `store-alpha` (dev-context `tenantId=store-alpha`) |
| Migration in this task | **NONE** |
| Host | `5088`, PID `40692`, `artifacts\publish-primary\Tooba.Host.dll`, `/health` 200, `/health/ready` 200 |
| Frontend | `3000`, PID `24868` (`next start`), `BUILD_ID 3jjJwEQe78ttzvxLwFBNg`, `/fa` 200 |
| Served manifest | `/_next/static/3jjJwEQe78ttzvxLwFBNg/_buildManifest.js` 200 |
| Served chunk | `/_next/static/chunks/967-fdda0cb21881b957.js` 200, 32 005 bytes, contains «آماده‌سازی و بسته‌بندی» |
| Previous build | `cz4aReIm9Nx33hKljkmfr` stopped before publish (graceful, then no listener on 5088) |
| Queue routes | `/fa/admin/fulfillments` 200, `/admin/fulfillments` 200 |

Commands actually used for the restart (Host stopped first so the publish path was not locked):

```text
Select-Process 48388 -> CloseMainWindow -> Stop-Process   (old Host, STOPPED)
dotnet publish ... -o artifacts\publish-primary           (0 errors)
Start-Process dotnet Tooba.Host.dll                        -> PID 40692, /health 200, /health/ready 200
Stop-Process 31184 (old Next) ; npm run start              -> node PID 24868, /fa 200
```

No migration/DDL/business SQL, no reset/reseed/backfill/cleanup, no provider/waybill call, no settings or shipping
draft write. All four live queue rows are pre-existing user records read read-only.

## 4. Read-only live observation (DOM/text/network only, no screenshots)

`/fa/admin/fulfillments`, clean entry, 1440×900 then 390×844:

```text
stage tablist      همه ۴ · آماده‌سازی و بسته‌بندی ۲ · آمادهٔ ارسال ۱ · تحویل به حامل ۰ · پیگیری تحویل ۱ · برگشتی‌ها ۰
time scope         امروز ۴ · آینده ۰ · همه ۴        history tab: سابقهٔ ارسال ۰
summary            ۴ ردیف در این نمایش · ۴ ردیف بدون موعد مشخص
rows (stage)       پیگیری تحویل / آمادهٔ ارسال / آماده‌سازی و بسته‌بندی / آماده‌سازی و بسته‌بندی
row 1 (grid)       مرحلهٔ کار پیگیری تحویل · زمان کار «پیگیری جاری؛ مهلت مشخصی ثبت نشده» ·
                   بُعد زمانی «در مسیر» · موعد ارسال «۲۴ آبان ۱۴۰۵ · ⁦۰۸:۰۰–۲۰:۰۰⁩ · به وقت تهران» ·
                   مهلت‌ها «… ۱۳:۰۰ · … ۱۸:۴۰ · … ۲۰:۰۰ · به وقت تهران» · تحویل مشتری «۲۶ آبان ۱۴۰۵ · ⁦۰۹:۰۰-۱۰:۰۰⁩» ·
                   اقدام بعدی «ثبت تحویل»
row 2 (grid)       مرحلهٔ کار آمادهٔ ارسال · زمان کار «موعد ارسال مشخص نشده» · اقدام بعدی «ایجاد مرسوله»
row 3 (grid)       مرحلهٔ کار آماده‌سازی و بسته‌بندی · زمان کار «موعد ارسال مشخص نشده» · اقدام بعدی «بسته‌بندی»
stage select       click «آمادهٔ ارسال»  -> stage badges unchanged, time badges امروز ۱ / آینده ۰ / همه ۱, 1 row
scope select       click «آینده»         -> «۰ ردیف در این نمایش» (honest empty), URL ?stage=ready_to_send&scope=future
legacy URL         ?queueFilter=needs_action -> renders همه + امروز (همه ۴ · امروز ۴), no hidden second filter
390 geometry       document overflow 0 · all 10 stage/time tabs 44px high · grid scoped scroll 3282 > 340
                   stage strip 324/324 at 390 (no extra strip scroll needed)
1440 geometry      document overflow 0 · 8 columns rendered before the virtualized right-hand columns
                   (columns are unmounted outside the design-system grid viewport; scrolled right to read them)
/admin/fulfillments same strip, same counts, document overflow 0 at 390
```

Pre-existing toolbar buttons (`فیلتر پیشرفته`, `ستون‌ها`, `خروجی CSV`, `خروجی Excel`, `پیش‌فرض`, `ذخیره نما`) measure
36px high at 390 — unchanged by this task and not part of the new stage/time controls (all of which are 44px).

## 5. No-write statement

Nothing in this task wrote business data: no shipment/plan/hold/stock/decision/return/user row, no checkout, no
settings, no shipping draft (`01a17d0d-…` untouched and unread for write), no fixture, no demo order. The only writes
are the worker's own files (source, tests, evidence) and the standard commit/push.
