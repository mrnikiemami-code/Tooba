# TB-263 — receipts (raw, one run per affected check)

Environment: Windows, `D:\Users\User\source\repos\SarvNewVer`, ONE canonical runtime
`tooba_alpha` @ 127.0.0.1:5432 (PostgreSQL, Edition `SingleStore`, tenant `store-alpha`), Host 5088, FE 3000.
All timestamps local (UTC+3:30). No disposable/second database was created.

## R1 — reproduction, read-only on the user's screenshot record (pre-repair Host)

```
> node scripts/tb263-probe.mjs
OPS 200
DAMAGED-ACTIONS [{"code":"resolve_damaged_goods","labelFa":"ادامهٔ تعیین تکلیف کالای آسیب‌دیده",
 "sellerOrderId":"f0ab4886-ff88-4ff2-b5c5-bb25b9d39612","fulfillmentId":"3ce14fb8-6453-44c0-b364-0f919c8bf2e0",
 "shipmentId":"a44aa20e-ecf3-4964-b828-bf1fc4a72eac","requiredPermission":"order.handle"}]
DETAIL 200
SHIP {"shipmentId":"a44aa20e-…","status":"Returned",
 "receipt":[{"orderLineId":"8e0a897b-…","quantity":1,"condition":"Damaged"}],
 "damaged":[{"damagedGoodsResolutionId":"01a1cbc2-b96d-7000-8c78-d12e172ff327","orderLineId":"8e0a897b-…",
   "quantity":1,"outcome":"Replacement","status":"Recorded","occurrenceKey":""}],
 "lines":[{"id":"8e0a897b-…","qty":1,"outstanding":0}]}
SHIP {"shipmentId":"01a1ca38-…","status":"Dispatched"}
LINE {"id":"8e0a897b-…","qty":1,"packed":1,"allocated":1,"shipped":1,"usable":0,"resendable":0,"op":"Returned"}
```

Owning seams that make the empty key a deterministic 409, not an unknown outcome:

```
src/backend/Modules/Fulfillment/Tooba.Fulfillment.Domain/Aggregates/FulfillmentUnit.cs:875
  throw new ContractOperationException("fulfillment.damage.resolution_occurrence_required");
src/backend/Modules/Fulfillment/Tooba.Fulfillment.Domain/Aggregates/FulfillmentUnit.cs:911
  throw new ContractOperationException("fulfillment.damage.exceeds_outstanding");
```

## R2 — projection after the repair (same read-only record, GET only, zero writes)

```
> node scripts/tb263-probe.mjs
OPS 200
DETAIL 200
SHIP {"shipmentId":"a44aa20e-…","status":"Returned",
 "damaged":[{"damagedGoodsResolutionId":"01a1cbc2-b96d-7000-8c78-d12e172ff327","quantity":1,
   "outcome":"Replacement","status":"Recorded","occurrenceKey":"a3618153-0e89-4205-bb30-3dcb90c9688f"}],
 "lines":[{"id":"8e0a897b-…","qty":1,"outstanding":0}]}
LINE 8e0a897b-… packed 1 allocated 1 shipped 1 usable 0 resendable 0 op Returned
```

## R3 — backend build

```
> dotnet build Tooba.slnx -c Release --nologo
    619 Warning(s)
    0 Error(s)
Time Elapsed 00:00:33.40
```

## R4 — focused backend tests (one run each, no loop)

```
> dotnet test Modules\Fulfillment\Tooba.Fulfillment.Tests\... --filter "FullyQualifiedName~DamagedResolution262"
Passed!  - Failed: 0, Passed: 8, Skipped: 0, Total: 8 - Tooba.Fulfillment.Tests.dll

> dotnet test Modules\Order\Tooba.Order.Tests\... --filter "FullyQualifiedName~AdminOrderPostDispatchReplay"
Passed!  - Failed: 0, Passed: 11, Skipped: 0, Total: 11 - Tooba.Order.Tests.dll

> dotnet test Host\Tooba.Host.Tests\... --filter "FullyQualifiedName~DamagedGoodsResolution262Relational"
Passed!  - Failed: 0, Passed: 8, Skipped: 0, Total: 8 - Tooba.Host.Tests.dll
```

The Order run appended its by-design receipt line to the retained 262-R1 evidence file:

```
G orchestration fulfillment=ac758945-093f-4285-9e70-00e1cad0c700 checkout=f8dfba61-831e-4cd8-bf83-ea99fc644e7f
  source=42d711a7-42c1-40b5-858c-9e159cb327a8 resend=3a928800-363e-4c73-9281-9950f75d1470
  receipt=bf549b4c-22a8-4613-895d-98af0248b286 lifecycle=public-command replay=exact(sameParcel)
  changed=conflict foreign=refused rows=shipments2/receipts1
```

## R5 — frontend checks (one run each)

```
> npx tsx --tsconfig tsconfig.mount-tests.json --test app/admin/admin-order-operational-delivery.mount.test.ts
✔ TB-263 mounted: an authoritative completion closes the review with exactly ONE toast and no uncertainty
✔ TB-263 mounted: a durably recorded decision awaiting healthy stock is PENDING, never an unknown response
✔ TB-263 mounted: a genuine transport loss keeps the exact envelope and says so concisely
✔ TB-263 mounted: a denied submission closes the review into the ONE existing MessageBox, with no duplicate surface
ℹ tests 58  ℹ pass 58  ℹ fail 0

> npx tsx --tsconfig tsconfig.mount-tests.json --test app/admin/admin-error-map.test.ts
ℹ tests 6  ℹ pass 6  ℹ fail 0

> npm run typecheck            # tsc --noEmit, exit 0, no output
> npm run build                # production build exit 0
```

## R6 — deploy / runtime receipts

```
Host: previous PID 7056 (artifacts\publish-primary\Tooba.Host.exe) stopped
      dotnet publish Host\Tooba.Host\Tooba.Host.csproj -c Release -o artifacts\publish-primary  → exit 0
      new PID 6216
      GET /health       200
      GET /health/ready 200 {"status":"ready","checks":{"edition":"SingleStore","postgresql":"configured",
                          "authorization":"inmemory","messaging-transport":"postgresql-sql","messaging":"Healthy"}}
FE:   previous PID 45964 (next start -p 3000) stopped
      node node_modules/next/dist/bin/next start -p 3000 → new PID 43700
      GET /fa 200; GET /fa/admin/orders/13feece0-…/operations?fulfillmentId=3ce14fb8-…&shippingGroupId=2c550347-… 200
      BUILD_ID iyES24s7U0oBnc8uyG4_S
      served chunks carrying the durable-pending copy: /_next/static/chunks/1323-b2ed9262639c7262.js,
      /_next/static/chunks/7825-0780d97d33f9949c.js
```

No migration file in the change set; nothing was applied. No DDL, reset, reseed, backfill or business SQL was run.
