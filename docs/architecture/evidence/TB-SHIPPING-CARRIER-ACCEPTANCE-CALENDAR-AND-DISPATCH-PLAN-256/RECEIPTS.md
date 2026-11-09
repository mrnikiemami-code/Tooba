# RECEIPTS — TB-SHIPPING-CARRIER-ACCEPTANCE-CALENDAR-AND-DISPATCH-PLAN-256

Task claim: `2026-11-09T04:08:11.8998526Z` (Bridge, channel `tooba-main`, task row `c59c8d0d-5b6e-4303-a1fe-15709a1334a7`).
Base: `de492437322c887c20d640cddbbf07e957c214d4` (= HEAD = `origin/main` when claimed; 256 issue/docs commit on top of the issued expected base `e758169bd`).
Code: `921d3fbb6571bc05bfcef0d9625c688d2a5a27f3` (authoring + migrations + authority + durable plan), `fe5eb2f9b7235dae0d0d98687954128d985f2c05` (read-seam projection).
Evidence: this commit (appended after Code, docs-only).
Measured elapsed: ≈2.8 h (claim → evidence commit), single worker session, no parallel task.

## 1. Repository state at claim

```
git rev-parse HEAD            -> de492437322c887c20d640cddbbf07e957c214d4
git rev-parse origin/main     -> de492437322c887c20d640cddbbf07e957c214d4
git status --porcelain        -> (clean)
```

## 2. Private backup before migration (mandatory pre-step)

```
Copy-Item ... -> D:\temp\tb256-tooba_alpha.dump   2786765 bytes   2026-11-09 09:28:18 (+03:30)
```

## 3. Migrations (repository MigrationRunner, canonical target)

Applied by the published Host's startup MigrationRunner (no manual business SQL):

```
{"Category":"Tooba.MigrationRunner.MigrationOrchestrator","Message":"Migration Apply module=Shipping edition=SingleStore tenantId=store-alpha database=tooba_alpha connectionRef=tenant-alpha schema=shipping current=20261109113000_AddCarrierAcceptanceSchedule pendingCount=0 result=ok"}
{"Category":"Tooba.MigrationRunner.MigrationOrchestrator","Message":"Migration Apply module=Order edition=SingleStore tenantId=store-alpha database=tooba_alpha connectionRef=tenant-alpha schema=order current=20261109120000_AddPlannedDispatchEvidence pendingCount=0 result=ok"}
```

Read-only verification on `tooba_alpha` (docker `postgres-db`, psql as `admin`):

```
shipping.__ef_migrations_history (top 2):  20261109113000_AddCarrierAcceptanceSchedule
                                           20261103120000_CarrierShipmentDispatchClaim
"order".__ef_migrations_history (top 2):   20261109120000_AddPlannedDispatchEvidence
                                           20261108153000_AddOperationalGroupOccurrences
shipping.authoring_carrier_schedules / shipping.authoring_carrier_schedule_days  -> both present
order.cart_shipping_groups  accepted_dispatch_* columns -> 8
order.order_shipping_groups accepted_dispatch_* columns -> 8
ck_cart_shipping_groups_accepted_dispatch_plan, ck_order_shipping_groups_accepted_dispatch_plan -> present
draft 01a17d0d-e033-7000-af43-49a1daa28822 (USER) -> revision 2   (untouched)
draft 01a1beed-d7df-7000-bfc8-0185ea61afe3 (TASK) -> revision 3   (carrier 7 day rows)
"order".cart_shipping_groups   rows 19, with accepted_dispatch_handoff_date 0, format 3 rows 0
"order".order_shipping_groups  rows 14, with accepted_dispatch_handoff_date 0, format 3 rows 0
```

Historical accepted groups therefore read as explicit "unspecified" dispatch plan; nothing was backfilled.

## 4. Authoring save/readback proof (task-owned draft only)

`D:\temp\tb256-api-proof.json` (captured via the supported admin authoring API with the task session):

```
before  : revision 1, weeklySchedule null, carrierSchedule null
save #1 : 200, revision 2  (both sections written in the SAME revision)
          carrierSchedule deliberately different from the customer week:
            weekday3 (Wed) 09:00–12:00, weekday5 (Fri) closed, weekday6/7 (Sat/Sun) accepting
          weeklySchedule: Mon–Fri 09:00–17:00, Sat/Sun closed, slotDurationMinutes 60
save #2 : 200, revision 3  (carrier changed to Wed 10:00–13:00; customer week unchanged)
after #2: weeklySchedule unchanged (Mon–Fri 09:00–17:00, slot 60)   <- independence in the other direction
checks  : firstSaveHasBothSchedules=true, revisionAdvanced=true, customerWeekPreserved=true,
          carrierChanged=true, carrierWednesday1013=10:00:00-13:00:00,
          customerWednesday0917=09:00:00-17:00:00, carrierFridayClosed=false
```

## 5. Render proof (task-owned sessionless Chrome via CDP)

`D:\temp\tb256-render\render.mjs` → `D:\temp\tb256-render\render-out.json`:

```
userDraftLoad=ready (draft 01a17d0d…, revision 2 untouched)
userDraftDesktop        : dir=rtl locale=fa overflow=false 1425/1425, 7 customer days, carrier days 0,
                          copy offered (44px), configure offered, smallTargets 0
userDraftCarrierTab     : same draft, carrier panel visible, carrierDayCount 0, copy present/disabled=false, smallTargets 0
taskDraftCarrierDesktop : carrierDayCount 7, Friday «بدون پذیرش», time zone Asia/Tehran selected, overflow false, smallTargets 0
taskDraftCustomerDesktop: carrier days 0 (customer tab), overflow false
taskDraftCarrierMobile  : 390/390, overflow false, carrierDayCount 7, smallTargets 0
taskDraftCarrierTablet  : 1009/1009, overflow false, carrierDayCount 7, smallTargets 0
network                 : total 119, nonGet [], v1AdminGets 9    -> no write during render
ok = true
```

## 6. Executed tests

```
dotnet test Tooba.Shipping.Tests  --filter FullyQualifiedName~ShippingDispatchSchedulingAuthority256Tests
  -> Passed! Failed 0, Passed 8, Skipped 0, Total 8

dotnet test Tooba.Order.Tests --filter "FullyQualifiedName~PlannedDispatchEvidence256Tests|FullyQualifiedName~ShippingGroupAcceptedEvidence|FullyQualifiedName~StorefrontCapturedGroupProjectionComposer|FullyQualifiedName~StorefrontCapturedSelectionWriter"
  -> Failed 1, Passed 56, Total 57
     failure: StorefrontCapturedSelectionWriterTests.Nonblank_coupon_reaches_the_quote_path_and_confirms_the_discounted_goods
     (pre-existing: reproduced unchanged on a clean worktree at base de4924373)

$env:TOOBA_TB231_ORDER_POSTGRES='Host=127.0.0.1;Port=15433;Username=tb231;Password=***;Database=order_tb231'
dotnet test Tooba.Order.Tests --filter FullyQualifiedName~ShippingGroupAcceptedEvidenceStorageTests
  -> Passed! Failed 0, Passed 11, Skipped 0, Total 11
     (disposable PostgreSQL 16 container tb256-pg249, task-owned; real OrderDbContext + production migrations;
      DB-side refusals for half-written/incoherent plans; read-seam plan vs legacy null)

node --experimental-strip-types --test features/admin-shipping-authoring/authoring-carrier-schedule-256.test.ts
  -> tests 10, pass 10, fail 0

npx tsx --tsconfig tsconfig.mount-tests.json --test features/admin-shipping-authoring/authoring-carrier-calendar.mount.test.ts
  -> tests 6, pass 6, fail 0

cd src/frontend; npm run typecheck        -> exit 0
cd src/frontend; npm run build            -> exit 0, BUILD_ID O_m0I-sF7M3RMbc_yjtQn
dotnet publish src/backend/Host/Tooba.Host/Tooba.Host.csproj -c Release -o D:\temp\tb256-publish2 -> exit 0
```

Known pre-existing RED (disclosed, not re-baselined): `ShippingPersistenceModelTests.Model_and_the_initial_migration_are_in_sync`
(model-snapshot drift from main-branch carrier-shipment models, verified by an independent diff probe listing the 7
pending operations; none belongs to the TB-256 tables).

## 7. Deployment receipts (canonical, same target)

```
FE   : 127.0.0.1:3000  next start  PID 27492  build O_m0I-sF7M3RMbc_yjtQn  /fa 200
       served chunk src/frontend/.next/static/chunks/6185-664164e21c3e24d4.js contains
       shipping-authoring-carrier-accepting-*/notaccepting-* (new editor shipped)
Host : 127.0.0.1:5088  artifacts/publish-primary/Tooba.Host.exe  PID 12220  /health 200
       ASPNETCORE_ENVIRONMENT=Development, Tooba__Edition=SingleStore,
       Tooba__DeploymentId=store-alpha-canonical-tb256, tenant store-alpha / tenant-alpha -> tooba_alpha
       startup log: "TenantId=store-alpha Edition=SingleStore DeploymentId=store-alpha-canonical-tb256"
       republish preserved artifacts/publish-primary/App_Data/dataprotection-keys
Previous Host PID 13432 stopped; port 5088 released before start; no second Host/port.
```

## 8. Scope guards actually honoured

- No new cart/checkout/order/reservation/payment; no user draft/published service mutation; no business SQL write, no reset/reseed.
- No edition/tenant/connection/deployment/port switch; no Marketplace work (255 stays parked).
- User draft `01a17d0d-…` verified still revision 2 after all operations.
- CapturedPreview local ON with 7 dates; checked-in gates OFF; DataProtection preserved.
- No screenshots/images; render used a task-owned sessionless Chrome, all GET.
