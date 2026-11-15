# RECEIPTS — TB-SHIPPING-QUEUE-PERSIAN-DATES-AND-CONSISTENT-TIME-DISPLAY-269

## Bridge / protocol

```text
GET  http://127.0.0.1:17321/api/tasks?channelId=tooba-main                200
     -> first non-completed row: id=58d11af5-9ccd-4a52-b7df-c1b2e3e0934a
        taskId=TB-SHIPPING-QUEUE-PERSIAN-DATES-AND-CONSISTENT-TIME-DISPLAY-269  status=Pending
GET  http://127.0.0.1:17321/api/tasks/next?channelId=tooba-main           200  (4585-byte TASK payload)
POST http://127.0.0.1:17321/api/workers/heartbeat                         200  workerId=tooba-worker-01 status=Working
POST http://127.0.0.1:17321/api/results                                   200
POST http://127.0.0.1:17321/api/tasks/58d11af5-9ccd-4a52-b7df-c1b2e3e0934a/complete   200
POST http://127.0.0.1:17321/api/workers/heartbeat                         200  status=Idle
```

## Base / ancestry (declared honestly)

```text
Expected-Base-SHA (envelope) : d4fb3b8e3505a445b123efc8d63badea52df1172
git cat-file -t d4fb3b8e…    : fatal: could not get object info  -> NOT PRESENT in this clone
git rev-parse HEAD           : f62bd7564d5394dc8e36df4193b065d17be38682  (local main == origin/main at task start)
```

Work therefore started from the actual local `main` head, which already contains TB-268. The envelope SHA could not be
verified as an ancestor; this is recorded as a limit, not silently substituted.

## Changed files

```text
M  src/frontend/app/fulfillment/fulfillment-work-queue-dispatch.ts
   + formatters now delegate to lib/operational-format.ts (day / instant / zone / window)
   + date-only anchor at local noon, carrier-zone instants, LTR-isolated ranges, ONE zone label per cell
   + truthful markers: تاریخ نامعتبر / منطقهٔ زمانی نامعتبر / منطقهٔ زمانی مشخص نشده
M  src/frontend/app/fulfillment/fulfillment-work-queue-260.test.ts
   one expectation updated from the old raw string to the new Persian string
A  src/frontend/app/fulfillment/fulfillment-work-queue-persian-dates-269.test.ts   (7 cases)
```

No server, Contracts, endpoint, migration, schema, appsettings, dictionary or locale file was touched.

## Commands (each run once)

```text
node --experimental-strip-types --test app/fulfillment/*269.test.ts app/fulfillment/*260.test.ts   13/13 pass
npm run test:fulfillment                                                                          6/6 pass
npx tsc --noEmit                                                                                  exit 0
npm run build                                                                                     exit 0  BUILD_ID UmAnnlHYHCf-Sa5-dTMkm
Stop-Process -Id <port-3000 owner> ; npx next start -p 3000                                       PID 38800, /fa 200
```

## Runtime

```text
FE  : Next production 127.0.0.1:3000  PID 38800  BUILD_ID UmAnnlHYHCf-Sa5-dTMkm  /fa 200
Host: 127.0.0.1:5088  untouched  /health 200  /health/ready 200
DB  : one canonical tooba_alpha @ 127.0.0.1:5432  SingleStore  store-alpha  (no connection/setting change)
```

No migration, no DDL, no business SQL, no reset/reseed, no fixture, no provider/waybill call. User orders, the shipping
draft and the existing example group/parcel were read-only (GET only). DataProtection and CapturedPreview unchanged.

## Served-chunk proof

```text
/_next/static/chunks/967-076c4bd18e2dcc76.js   200   contains «منطقهٔ زمانی نامعتبر» (new marker)
/_next/static/chunks/app/admin/fulfillments/page-d1ac35abce2c2c2.js   200   (page entry, links the screen chunks)
```

The screens' column definitions resolve `formatPlannedHandoff`/`formatPreparationDeadlines`/`formatCustomerReceiving` to
the new chunk symbols (`eq.WP` / `eq.YP` here), i.e. the served bundle is this build and not a stale one.

## Scratch files (not part of this change)

```text
?? scripts/tb266r1-*.mjs   (8 files from the 266-R1 round, previously reported untracked; untouched here)
```
