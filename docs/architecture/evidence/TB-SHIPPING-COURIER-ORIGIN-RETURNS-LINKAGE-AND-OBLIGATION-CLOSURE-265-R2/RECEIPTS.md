# TB-265-R2 receipts (commands actually run, once each after the last relevant change)

Sanitized: no cookie, token, password or connection secret is stored here; the local dev reference is the same one the
repository already documents. One canonical install only: `tooba_alpha` @127.0.0.1:5432 (SingleStore, tenant
`store-alpha`), Host 5088, Next 3000.

## 1. Migrations (additive only, through the repository runner)

```powershell
# private pre-migration backup
# D:\temp\tb265r2-backup\tooba_alpha-pre-tb265-r2.dump   3,570,765 bytes

dotnet run --project src\backend\Host\Tooba.MigrationRunner -c Release -- plan --tenant store-alpha
# Inventory    current=20261114150000_AddHeldObligationReleases            pendingCount=0
# Fulfillment  current=20261114160000_AddPartialCourierReturnSettlement    pendingCount=0
# Returns      current=20261114170000_AddReturnRequestDecisions            pendingCount=0
# Order        current=20261114120000_AddOrderLineObligationClosures       pendingCount=0
# every other module: pendingMigrations []
# full output: tb265r2-plan.txt
```

New migration files:

```
src/backend/Modules/Inventory/Tooba.Inventory.Infrastructure/Persistence/Migrations/20261114150000_AddHeldObligationReleases.cs
src/backend/Modules/Fulfillment/Tooba.Fulfillment.Infrastructure/Persistence/Migrations/20261114160000_AddPartialCourierReturnSettlement.cs
src/backend/Modules/Returns/Tooba.Returns.Infrastructure/Persistence/Migrations/20261114170000_AddReturnRequestDecisions.cs
```

No manual DDL, no business SQL, no reset/reseed/backfill/delete, no global reclaimer.

## 2. Backend focused tests (real `tooba_alpha` for the relational matrix)

```powershell
$env:TB265_CONNECTION='Host=127.0.0.1;Port=5432;Database=tooba_alpha;Username=admin;Password=123456'
$env:TB265_EVIDENCE_DIR='D:\Users\User\source\repos\SarvNewVer\docs\architecture\evidence\TB-SHIPPING-COURIER-ORIGIN-RETURNS-LINKAGE-AND-OBLIGATION-CLOSURE-265-R2'

dotnet test Modules\Fulfillment\Tooba.Fulfillment.Tests -c Release --filter "FullyQualifiedName~CourierReturnClaim265Tests"
#  Total tests: 8  Passed: 8                      (tb265r2-fulfillment.txt)

dotnet test Modules\Inventory\Tooba.Inventory.Tests -c Release --filter "FullyQualifiedName~ReturnObligationRelease265"
#  Total tests: 5  Passed: 5                      (tb265r2-inventory.txt)

dotnet test Modules\Returns\Tooba.Returns.Tests -c Release --filter "FullyQualifiedName~CourierOriginReturns265"
#  Total tests: 5  Passed: 5                      (tb265r2-returns.txt)

dotnet test Host\Tooba.Host.Tests -c Release --filter "FullyQualifiedName~CourierOriginReturns265RelationalTests"
#  Total tests: 17 Passed: 17                     (tb265r2-relational-log.txt; facts appended to tb265-relational.txt)
#  A–K retained; L exact release identity/payload, M rollback window, N approve-vs-reject barrier,
#  O completed-approval replay, P partial source replacement conservation, Q concurrent approvals share one key.

dotnet test Modules\Order\Tooba.Order.Tests -c Release --filter "FullyQualifiedName~AdminOrderPostDispatchReplayTests"
#  Total tests: 16 Passed: 16                     (tb265r2-order-replay.txt)  — the public-command boundary
```

## 3. Frontend

```powershell
cd src\frontend
npm run test:returns                                   # 7/7
node --experimental-strip-types --test app/admin/admin-error-map.test.ts   # 7/7
npx tsc --noEmit                                       # 0 diagnostics
npm run build                                          # exit 0
Get-Content .next\BUILD_ID                             # PtxAhyJO9UhnwD8B3_ln4
```

Served-build proof over HTTP (after restarting the Next server):

```
GET /_next/static/PtxAhyJO9UhnwD8B3_ln4/_buildManifest.js   -> 200
GET /_next/static/chunks/1323-a6cb5f1ef95dfaee.js           -> 200, 76,975 bytes, carries "held_release"
GET /_next/static/chunks/264-000.js                         -> 404 (an old chunk name)
```

## 4. Build / publish / runtime

```powershell
dotnet build src\backend\Tooba.slnx -c Release          # Build succeeded, 0 Error(s), 213 Warning(s)   (tb265r2-build.txt)
dotnet publish src\backend\Host\Tooba.Host\Tooba.Host.csproj -c Release -o artifacts\publish-primary
                                                       # 0 errors                                  (tb265r2-publish.txt)

# Host (published build): PID 10004
GET http://127.0.0.1:5088/health        -> 200
GET http://127.0.0.1:5088/health/ready  -> 200  (SingleStore)

# Frontend production server: PID 17188, `node node_modules/next/dist/bin/next start -p 3000`
GET http://127.0.0.1:3000/fa            -> 200
```

## 5. Live operator proof (read + task-labelled public-command writes)

See `tb265r2-live-operator.txt` for the full request/response record. Summary:

```powershell
$h = @{ 'X-Tooba-Dev-Actor-User-Id'='01a036c2-970e-7000-8eb7-94bf5cc2d8db'; 'Accept'='application/json'; 'Accept-Language'='fa-IR' }
GET  /v1/admin/dev-context                                            -> 200 (admin-actor@tooba.local, store-alpha)
POST /v1/admin/orders/0dabee2e-…/operations  code=create_courier_return -> 200 case 01a1d8a1-b65e-… (Requested)
POST same body, same idempotencyKey                                  -> 200 same case and claim; claims for that parcel = 1
GET  /v1/admin/orders/0dabee2e-…                                     -> 200 courierReturnCases=[{claim, case, Requested}]
GET  /v1/admin/returns/01a1d8a1-b65e-…                               -> 200 CourierOrigin, closurePhase=None, no refundAttempts
POST /v1/admin/orders/d82b9539-…/operations  code=reject_return       -> 200 Rejected; one decision row; 0 reservations touched
POST same approve on the rejected case (before fix)                  -> 404 refund.payment_missing
POST same approve on the rejected case (after fix + republish)       -> 409 return.already_rejected (Persian title)
GET  /v1/admin/returns/01a1d89e-538a-…                               -> 200 partial case: caseSettled 1 of 3, remaining 2 on the order
```

## 6. Change set

```
git merge-base --is-ancestor a0e1f24e55f14f8bc366c9018da9f117e718aca8 HEAD   # exit 0
git diff --stat   # 22 tracked files, 1412 insertions(+), 143 deletions(-), plus 6 new files:
#   src/backend/Modules/Inventory/Tooba.Inventory.Infrastructure/Persistence/HeldObligationReleaseRecord.cs
#   src/backend/Modules/Inventory/…/Migrations/20261114150000_AddHeldObligationReleases.cs
#   src/backend/Modules/Fulfillment/…/Migrations/20261114160000_AddPartialCourierReturnSettlement.cs
#   src/backend/Modules/Returns/Tooba.Returns.Infrastructure/Persistence/ReturnDecisionRecord.cs
#   src/backend/Modules/Returns/…/Migrations/20261114170000_AddReturnRequestDecisions.cs
#   src/backend/Modules/Returns/Tooba.Returns.Infrastructure/Directories/ReturnDirectory.Decisions.cs
#   docs/architecture/evidence/TB-SHIPPING-…-265-R2/*  (this evidence)
```

## 7. Repository state (this round)

```
d25544749f9dbe77c0dd79aa9c4e472af895a02c   code + migrations + evidence + ledger (single task commit)
70f152abbb758b204570ac0cb80b839bc7117543   result contract + SHA recording
git rev-parse origin/main                  == the local HEAD after each push (parity verified)
```
