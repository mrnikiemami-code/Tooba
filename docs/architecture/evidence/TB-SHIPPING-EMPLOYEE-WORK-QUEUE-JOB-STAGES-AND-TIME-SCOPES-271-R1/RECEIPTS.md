# TB-271-R1 — RECEIPTS

Environment: ONE canonical `tooba_alpha` @127.0.0.1:5432, SingleStore `store-alpha` (no second/disposable/Docker
database, no 15432/15433, no edition/connection/port/appsettings change). FE 3000 → Host 5088.

## Builds / deploys

| Step | Receipt |
| --- | --- |
| `dotnet build src/backend/Tooba.slnx -c Release` | **0 errors** (623 warnings, pre-existing analyzer noise) |
| `dotnet publish src/backend/Host/Tooba.Host/Tooba.Host.csproj -c Release -o artifacts/publish-primary` | **0 errors**; publish completed after the running Host (PID 40692) was gracefully stopped |
| Host start | `dotnet Tooba.Host.dll` → PID **24008**, `http://127.0.0.1:5088` |
| `GET /health` | 200 |
| `GET /health/ready` | 200 `{"status":"ready","checks":{"edition":"SingleStore","postgresql":"configured","authorization":"inmemory","messaging-transport":"postgresql-sql","messaging":"Healthy"}}` |
| `npm run build` (src/frontend) | 0, `BUILD_ID mry3IgeR--YNQDX_gvEMt` |
| Next start | `npm run start` → PID **41084** on 3000 |
| `GET /fa` | 200 (length 375909) |
| `GET /_next/static/mry3IgeR--YNQDX_gvEMt/_buildManifest.js` | 200 |
| `GET /fa/admin/fulfillments` | 200 (18744) |
| `GET /admin/fulfillments` | 200, renders the same queue |

## Checks (run once after the final code change)

| Command | Result |
| --- | --- |
| `dotnet test --filter "FullyQualifiedName~AdminFulfillmentWorkQueue271R1Tests"` | **17/17** |
| `dotnet test --filter "FullyQualifiedName~AdminFulfillmentJobStage271Tests"` | **15/15** |
| combined 271R1 + 271 + `FulfillmentErrorAndGridTests` | **37/37** |
| whole `Tooba.Fulfillment.Tests` (disclosed) | 489 passed / 20 failed — all pre-existing Postgres-gated + 2 module architecture guards (see RESULT limits) |
| `npx tsx --tsconfig tsconfig.mount-tests.json --test app/admin/admin-fulfillment-work-queue-271.mount.test.ts` | **12/12** |
| `node --experimental-strip-types --test app/admin/admin-fulfillment-work-queue.test.ts` | **9/9** |
| `npm run test:fulfillment` | **6/6** |
| `npm run test:grid` | 6 passed, **1 pre-existing failure** `column-manager.test.ts` (`PAGE_SIZES` expectation predates this change; `AppDataGrid.tsx` at `HEAD` already contains `10, 20, 25, 50, 100, 1000`) |
| `npm run typecheck` | 0 |

## Live read-only query receipts (`POST /v1/admin/fulfillments/work-queue/query`)

Actor from `GET /v1/admin/dev-context` = `01a036c2-970e-7000-8eb7-94bf5cc2d8db` (`admin-actor@tooba.local`,
tenant `store-alpha`).

```
no sort/no filters : 200 totalCount=4 selectedTotal=4 stageAll=4 stageHistory=1 items=4 time 4/0/4 undated=4
                     items: prep, prep, ready_to_send, delivery  (0 history rows)
history + today    : 200 totalCount=1 selectedTotal=1 stages=history time 0/0/0 stageHistory=1
history + all      : 200 totalCount=1 same key parcel:01a1dc15-b0b7-7000-9118-953a3f6e92d2
all + all          : 200 totalCount=4 stageAll=4 stageHistory=1
preparation + all  : 200 totalCount=2 selectedTotal=2 time 2/0/2
handoff + future   : 200 totalCount=0 stageAll=0 stageHistory=1 time 0/0/0
advanced jobStage  : 400 errorCode=grid.advancedFilter.field.invalid
multi-value jobStage: 400 errorCode=grid.filter.operator.invalid
updatedAt asc      : 200 rows=4 (explicit sort preserved)
advanced cityName  : 200 rows=0 total=0 (accepted and evaluated)
```

## Served UI receipts (DOM/text only — no screenshots)

- `/fa/admin/fulfillments` at 1440 and 390: document overflow **0**; seven stage tabs and three scope tabs measure
  **44px**; stage badges همه ۴ · آماده‌سازی ۲ · آمادهٔ ارسال ۱ · تحویل به حامل ۰ · پیگیری ۱ · برگشتی ۰; time badges
  امروز ۴ / آینده ۰ / همه ۴; summary «۴ ردیف در این نمایش · ۴ ردیف بدون موعد مشخص».
- archive badge stayed «سابقهٔ ارسال ۱» across «همه» → «تحویل به حامل» → «همه»; the URL returned to clean `""` when the
  selection returned to the defaults.
- live request body captured in-page after switching to «آینده»:
  `{"calls":1,"sort":[],"filters":[{"field":"jobStage","operator":"equals","value":"all"},{"field":"timeScope","operator":"equals","value":"future"}],"page":1,"selected":"0"}`
  — no synthetic `updatedAt`, only the two visible selection tokens, and an honest empty future selection.
- archive tab: exactly **1** grid row, `time-scope-today` `disabled`, time badges 0/0/0, summary «۱ ردیف در این نمایش»,
  overflow 0.
- `/admin/fulfillments` at 390: same badges, 4 rows, overflow 0.
- 25 pre-existing 36px grid toolbar controls remain under 44px at 1440 (declared, not restyled).

## Data

No migration, no DDL, no reset/reseed/backfill/cleanup/restore, no business SQL, no new fixture order/parcel/plan/hold,
no settings/key/CapturedPreview change, no provider/waybill/payment/stock/Returns/settlement/notification action. The
only traffic was GET reads and read-only queue queries.

## Changed paths

Backend
- `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Application/Fulfillments/Ports/ISellerFulfillmentAuthorizer.cs`
- `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Application/WorkQueue/Models/AdminFulfillmentJobStage.cs`
- `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Application/WorkQueue/Models/AdminFulfillmentWorkQueueCountsBuilder.cs`
- `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Application/WorkQueue/Models/AdminFulfillmentWorkQueueModels.cs`
- `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Application/WorkQueue/Queries/QueryAdminFulfillmentWorkQueueQuery.cs`
- `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Infrastructure/Queries/AdminFulfillmentWorkQueueQueryEngine.cs`
- `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Infrastructure/Queries/AdminFulfillmentWorkUnitProjection.cs`
- `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Tests/Behavior/AdminFulfillmentWorkQueue271R1Tests.cs` (new)
- `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Tests/Behavior/AdminFulfillmentJobStage271Tests.cs`
- `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Tests/Behavior/FulfillmentErrorAndGridTests.cs`

Frontend
- `src/frontend/app/admin/admin-fulfillment-work-queue-screen.tsx`
- `src/frontend/app/admin/admin-fulfillment-work-queue-271.mount.test.ts`
- `src/frontend/design-system/app-data-grid/AppDataGrid.tsx`

Docs
- `docs/ai/tasks/TB-SHIPPING-EMPLOYEE-WORK-QUEUE-JOB-STAGES-AND-TIME-SCOPES-271-R1/TASK.md`
- `docs/architecture/evidence/TB-SHIPPING-EMPLOYEE-WORK-QUEUE-JOB-STAGES-AND-TIME-SCOPES-271/ARCHITECT-REVIEW.md`
- `docs/architecture/evidence/TB-SHIPPING-EMPLOYEE-WORK-QUEUE-JOB-STAGES-AND-TIME-SCOPES-271-R1/*`
- `AGENTS.md` pointer banner

Untracked `scripts/tb266r1-*.mjs` and `scripts/tb270r2-*.mjs` scratch files are NOT part of this change set.
