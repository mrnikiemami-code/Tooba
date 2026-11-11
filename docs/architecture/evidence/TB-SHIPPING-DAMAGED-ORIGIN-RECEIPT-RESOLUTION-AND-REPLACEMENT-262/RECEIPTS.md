# TB-262 — receipts

## 1. Base / work state

```
git rev-parse HEAD              4cf6bfbe084f7c4834205f6033064202bae2cf17   (== Expected-Base-SHA, == origin/main)
git status -sb                  ## main...origin/main
```

The issue/docs commits after `4cf6bfbe0` are legitimate descendants. No force/reset/rebase/stash, no unrelated
overwrite.

## 2. Private backup before the additive migration

```
D:\temp\tb262-backup\tooba_alpha-pre-tb262.dump   3,448,171 bytes   2026-11-11 16:20:40
header magic: PGDMP (PostgreSQL custom-format logical dump)
SHA256: 23B41AA87FEF0DA23D2D0592B53AFDA84B7672A0AAD57BC026B2B8EF599DF0C2
```

## 3. Migration (repository MigrationRunner, not manual DDL)

```
$env:ASPNETCORE_ENVIRONMENT='Development'; $env:DOTNET_ENVIRONMENT='Development'
Set-Location src\backend\Host\Tooba.MigrationRunner
dotnet run -c Release --no-launch-profile -- apply --tenant store-alpha
```

Plan after the apply (excerpt; every module reported `pendingCount=0`):

```
module=Fulfillment current=20261111123000_AddDamagedGoodsResolution pendingCount=0 schema=fulfillment result=ok
```

Schema added by the migration (additive only): nullable `fulfillment.shipments.damaged_resolution_id` and
`fulfillment.shipments.replacement_reservation_id`; table `fulfillment.damaged_goods_resolutions`; indexes
`ix_shipments_damaged_resolution_id`, `ix_damaged_goods_resolutions_{fulfillment_id,shipment_id,
replacement_reservation_id,replacement_shipment_id}` and unique
`ux_damaged_goods_resolutions_fulfillment_occurrence (fulfillment_id, occurrence_key)`.

## 3.1 RETENTION REVISION — files changed for the 2026-11-11 user override

```
src/backend/Modules/Fulfillment/Tooba.Fulfillment.Domain/Aggregates/DamagedGoodsResolution.cs
  ReservationKey                      damage-replacement:{resolutionId:N}          (stable across attempts)
  ReleaseBeforeDispatch               -> RetainStockBeforeDispatch  (parcel detached, hold kept, Attempt+1)
  new MarkOrderCancelled              only reachable from the authorized order cancellation
src/backend/Modules/Fulfillment/Tooba.Fulfillment.Domain/Aggregates/FulfillmentUnit.cs
  DetachReplacementReservationForCancel -> RetainReplacementStockForCancelledParcel (no release)
  new ReleaseReplacementStockForOrderCancel (returns the unconsumed hold ids, exactly once)
src/backend/Modules/Fulfillment/Tooba.Fulfillment.Infrastructure/Directories/FulfillmentDirectory.cs
  CancelShipmentAsync                 retains the hold, no inventory release
  AbortForCheckoutCancelAsync         releases the unconsumed replacement holds once, after the domain abort
src/backend/Modules/Fulfillment/Tooba.Fulfillment.Infrastructure/Directories/FulfillmentDirectory.Damaged.cs
  Reserve path                        CommitReservationForPaidOrderAsync -> ExpiresAt = NULL
  Retry path                          re-reads the retained hold and requires Held, else typed conflict
src/backend/Modules/Fulfillment/Tooba.Fulfillment.Application/.../IFulfillmentInventoryGateway.cs   doc only
src/backend/Modules/Inventory/Tooba.Inventory.Contracts/Replacement/DamagedReplacementInventoryContracts.cs  doc only
```
No schema/migration, no endpoint, no UI, no money change. `tb262-domain.txt` keeps the domain fact list.

## 4. Focused proofs
```
dotnet build src/backend/Tooba.slnx -c Release            0 Error(s)   (53 NuGet-NU1900 offline warnings)

dotnet test src/backend/Modules/Fulfillment/Tooba.Fulfillment.Tests -c Release --no-build \
  --filter "FullyQualifiedName~DamagedResolution262"
  Passed!  Failed: 0, Passed: 7, Skipped: 0, Total: 7

dotnet test src/backend/Modules/Fulfillment/Tooba.Fulfillment.Tests -c Release --no-build \
  --filter "FullyQualifiedName~PostDispatchException"
  Passed!  Failed: 0, Passed: 36, Skipped: 0, Total: 36

$env:TB262_CONNECTION='Host=127.0.0.1;Port=5432;Database=tooba_alpha;Username=admin;Password=••••••'
$env:TB262_EVIDENCE_DIR='…\docs\architecture\evidence\TB-SHIPPING-DAMAGED-ORIGIN-RECEIPT-RESOLUTION-AND-REPLACEMENT-262'
dotnet test src/backend/Host/Tooba.Host.Tests -c Release --no-build \
  --filter "FullyQualifiedName~DamagedGoodsResolution262"
  Passed!  Failed: 0, Passed: 6, Skipped: 0, Total: 6      (fixture ids appended to tb262-relational.txt)

dotnet test src/backend/Modules/Order/Tooba.Order.Tests -c Release --no-build \
  --filter "FullyQualifiedName~AdminOrderPostDispatchReplay"
  Passed!  Failed: 0, Passed: 10, Skipped: 0, Total: 10

cd src/frontend; npx tsc --noEmit                       exit 0, no output
cd src/frontend; npx tsx --tsconfig tsconfig.mount-tests.json --test \
  app/admin/admin-order-operational-delivery.mount.test.ts
  tests 52 / pass 52 / fail 0
```

RETENTION REVISION (2026-11-11 user override) — the same affected checks were re-run once after the change:

```
Fulfillment  DamagedResolution262Tests + PostDispatchException258*     43/43  (7 pure + 36 retained)
Host         DamagedGoodsResolution262RelationalTests on tooba_alpha    6/6   (D rewritten for retention)
Order        AdminOrderPostDispatchReplayTests                         10/10  (fake gateway now asserts the
                                                                               commit-for-paid-order call)
FE           tsc --noEmit                                              0
FE           mount test                                                52/52  (unchanged, no FE source change)
```

TB-262 mounted cases (inside the 52):

```
TB-262 mounted: one reviewed damaged decision is durable before the POST and posts exactly once
TB-262 mounted: a quantity above this parcel's own damaged remainder is refused with zero POST
TB-262 mounted: an uncertain decision is retained and reconciles the EXACT reviewed payload
```

Note: running the Order filter also re-appended two `tb258r2/r3-orchestrator-relational.txt` lines under the
261 evidence directory (the tests write their relational fixture ids there). Those files are evidence logs; the
appended lines are new task-labelled fixture rows, no historical line was edited or removed.

## 5. Read-only database ledger (after the work)

```
fulfillment.damaged_goods_resolutions   47 rows, every occurrence_key starts with 'tb262-'
                                        Fulfilled 34 / Recorded 10 / Reserved 3 / Cancelled 0
inventory.reservations                  Consumed 65 / Held 42 / Released 44
inventory.reservations (Held)           18 with no TTL, 24 with TTL
                                        of the 28 'damage-replacement:' keys, 4 are live no-TTL holds
                                        damaged-resolution:01a1cae6dd7e… / …dc8f… / …db82… / …df52…
fulfillment.__ef_migrations_history     head 20261111123000_AddDamagedGoodsResolution (unchanged)
```

## 6. Deploy

```
dotnet publish src/backend/Host/Tooba.Host/Tooba.Host.csproj -c Release -o artifacts/publish-primary   exit 0
host restarted from artifacts/publish-primary (PID 30928, previous PID 45212 stopped first)
GET http://127.0.0.1:5088/health        200
GET http://127.0.0.1:5088/health/ready  200
  {"status":"ready","checks":{"edition":"SingleStore","postgresql":"configured",
   "authorization":"inmemory","messaging-transport":"postgresql-sql","messaging":"Healthy"}}
```

FE is unchanged by the retention revision (no frontend source change): the running next process (PID 13608) still
serves BUILD_ID `2t3xzbGoosGy7zATxS-Xh` and `/fa` answers 200.

The earlier deploy (first result) for the record:

```
cd src/frontend; npm run build            BUILD_ID 2t3xzbGoosGy7zATxS-Xh
next start -p 3000 -H 127.0.0.1           PID 13608
GET http://127.0.0.1:3000/fa              200
GET /_next/static/chunks/7825-33c5ed94cd44fb57.js   200  («آسیب‌دیده» occurrences: 22)
GET /_next/static/chunks/992-24accfc967f39328.js    404  (previous build, replaced)
```

## 7. Boundaries honoured

ONE database `tooba_alpha` @ 127.0.0.1:5432 (SingleStore / `store-alpha`, container `postgres-db`). No
disposable/test/parallel database, no 15433, no edition/connection/port/appsettings change. DataProtection keys and
CapturedPreview state untouched (local ON / checked-in OFF). No manual DDL, no business SQL, no reset/reseed/
backfill/deletion. User order/cart/parcel/payment rows and the 261 screenshot fixtures read-only. No live provider
call, no fake waybill. 255 parked, live Tipax deferred.
