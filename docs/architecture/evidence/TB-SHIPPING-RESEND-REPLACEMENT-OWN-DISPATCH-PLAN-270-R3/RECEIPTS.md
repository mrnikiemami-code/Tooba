# RECEIPTS — TB-270-R3

All commands executed on the worker machine (Windows, PowerShell), repository
`D:\Users\User\source\repos\SarvNewVer`, one canonical database, no screenshots.

## 0. Bridge / protocol

```
GET  http://127.0.0.1:17321/health                                     200 {"status":"ok"}
GET  http://127.0.0.1:17321/api/tasks?channelId=tooba-main             200
        row ec5d133d-f518-4be3-9f98-894af2d43ad3
        taskId TB-SHIPPING-RESEND-REPLACEMENT-OWN-DISPATCH-PLAN-270-R3  status Claimed
POST http://127.0.0.1:17321/api/results                                200  (this result)
POST http://127.0.0.1:17321/api/tasks/{row}/complete                   200
POST http://127.0.0.1:17321/api/workers/heartbeat                      200  status=Idle
```

Earlier rows for R1/R2/270 are `Completed`; nothing historical was resumed, interrupted or re-launched.

## 1. Base / ancestry

```
git rev-parse HEAD^            febeb27e36829cdceb1a8c6a3dcf4a9b0f22f659
git rev-parse origin/main      febeb27e36829cdceb1a8c6a3dcf4a9b0f22f659
merge-base --is-ancestor febeb27e3 origin/main      True
```

Issued `Expected-Base febeb27e3…` was present locally, was `HEAD`, and equals `origin/main`.

## 2. Backend build

```
dotnet build Tooba.slnx -c Release            exit 0
  Build succeeded.  243 Warning(s)  0 Error(s)
```
(Warnings are pre-existing: NU1900 offline vulnerability feed + one xUnit analyzer note.)

## 3. Focused test runs (each once, after the final code change)

```
dotnet test …/Tooba.Order.Tests --filter FullyQualifiedName~AdminShipmentPlan270R2Tests
  Passed! Failed 0, Passed 25, Total 25
dotnet test …/Tooba.Order.Tests --filter FullyQualifiedName~AdminShipmentDispatchPlan270R2RelationalTests
  Passed! Failed 0, Passed 2, Total 2        (real tooba_alpha, rolled back)
dotnet test …/Tooba.Order.Tests --filter FullyQualifiedName~AdminFulfillmentOwnPlanProjection270R2Tests
  Passed! Failed 0, Passed 3, Total 3
dotnet test …/Tooba.Order.Tests --filter FullyQualifiedName~OrderShipmentDispatchPlanStoreFilter270R3Tests
  Passed! Failed 0, Passed 3, Total 3
dotnet test …/Tooba.Fulfillment.Tests --filter FullyQualifiedName~DispatchPlanGuard270R2Tests
  Passed! Failed 0, Passed 7, Total 7
dotnet test …/Tooba.Fulfillment.Tests --filter FullyQualifiedName~ReplacementReadiness270R3Tests
  Passed! Failed 0, Passed 4, Total 4
dotnet test …/Tooba.Fulfillment.Tests --filter FullyQualifiedName~AdminFulfillmentBulkTests
  Passed! Failed 0, Passed 12, Total 12
dotnet test …/Tooba.Shipping.Tests --filter FullyQualifiedName~ShippingAttemptSchedulingLookupTests
  Passed! Failed 0, Passed 8, Total 8
```

## 4. Frontend

```
npx tsc --noEmit                                             exit 0
npx tsx --tsconfig tsconfig.mount-tests.json --test app/admin/admin-shipment-plan-270r2.mount.test.ts
  ℹ tests 10  ℹ pass 10  ℹ fail 0
npx tsx --tsconfig tsconfig.mount-tests.json --test app/admin/admin-order-operational-delivery.mount.test.ts
  ℹ tests 74  ℹ pass 74  ℹ fail 0
npm run build                                                exit 0
  ✓ Compiled successfully
  .next/BUILD_ID = iXZVso1qBFzB4wDa0huHP
```

## 5. Deploy (Host + FE), including one honest failure

```
dotnet publish Host/Tooba.Host -c Release -o artifacts\publish-primary
  exit 1  MSB3021/MSB3027: Tooba.Wishlist.*.dll locked by ".NET Host (19240)"      <-- publish attempted while the
                                                                                       running Host held the output
Stop-Process -Id 19240                                                              <-- graceful stop of the running Host
dotnet publish Host/Tooba.Host -c Release -o artifacts\publish-primary
  exit 0  Tooba.Host -> …\artifacts\publish-primary\
```

The failed publish is recorded because it happened; it wrote nothing usable and the retry after the stop is the publish
that is now served.

```
Host  start (detached): launcher 21100 → listener PID 43312
  GET /health            200 {"status":"ok"}
  GET /health/ready      200 {"status":"ready","checks":{"edition":"SingleStore","postgresql":"configured",
                             "authorization":"inmemory","messaging-transport":"postgresql-sql","messaging":"Healthy"}}
  GET /__platform-commerce 200 {"edition":"SingleStore","deploymentId":"dev-singlestore","tenantId":"store-alpha",
                             "connectionReference":"tenant-alpha", …}
```

A pre-stop provenance read of the OLD Host returned the same tuple, so the restart did not change edition/tenant/connection.

```
FE  Stop-Process -Id 41560 (previous Next server)
    start (detached): launcher 5868 → listener PID 620
    GET /fa                                                          200
    GET /_next/static/iXZVso1qBFzB4wDa0huHP/_buildManifest.js        200  (6612 B)
    GET /_next/static/chunks/9424-518a110b21e84091.js                200  (189,738 B)
        frozen=true  retrySame=true      <-- the new review/recovery copy is really served
```

## 6. Schema / data

```
No migration file added or modified in this increment.
Applied schema remains order 20261115120000_AddShipmentDispatchPlans (R2, applied once by Tooba.MigrationRunner).
No DDL, no reset/reseed/backfill, no manual business SQL, no fixture order/parcel/hold/reservation created.
No live shipment_dispatch_plans row was created by this increment (relational writes rolled back).
No provider call, no waybill, no notification, no money movement, no user-data write.
```

## 7. Change set

```
Code-SHA    96c5a8e19c10ba246b799202a804187ca1957a18
            "TB-270-R3: exact captured binding, inventory-owned hold read, frozen review recovery and
             create-to-plan handoff"
Evidence    the commit that adds this evidence set (documented in the Bridge Result body)
Untracked, NOT part of this change set: scripts/tb266r1-*.mjs, scripts/tb270r2-*.mjs
```
