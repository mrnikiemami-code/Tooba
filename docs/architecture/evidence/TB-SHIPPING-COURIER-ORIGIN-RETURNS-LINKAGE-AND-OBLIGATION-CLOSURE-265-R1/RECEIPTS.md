# TB-265-R1 receipts (commands actually run, one pass each after the last change)

Sanitized: no cookie, token, password or connection secret beyond the local dev reference is kept here.
One canonical install only: `tooba_alpha` @127.0.0.1:5432 (SingleStore, tenant `store-alpha`), Host 5088, Next 3000.

## 1. Migration: private backup, apply through the repository runner, plan

```powershell
# private pre-migration backup (D:\temp\tb265-backup\)
pg_dump -Fc ... -> tooba_alpha-pre-tb265-r1.dump        3,457,390 bytes  07:45 local
# first pre-change snapshot for the earlier attempt
                     tooba_alpha-pre-tb265.dump          3,381,173 bytes  11-13 20:35

dotnet run --project src\backend\Host\Tooba.MigrationRunner -c Release -- apply --tenant store-alpha
# 07:48 local -> order 20261114120000_AddOrderLineObligationClosures,
#                fulfillment 20261114130000_AddCourierReturnClaimLinesAndAllocations

dotnet run --project src\backend\Host\Tooba.MigrationRunner -c Release -- plan --tenant store-alpha
```

Latest plan (excerpt, full structured output in `tb265-plan.txt`):

```
module=Order         current=20261114120000_AddOrderLineObligationClosures        pendingCount=0 result=ok
module=Fulfillment   current=20261114130000_AddCourierReturnClaimLinesAndAllocations pendingCount=0 result=ok
module=Inventory     current=20261012110133_AddReservationConsumptions             pendingCount=0
module=Returns       current=20261113174500_NormalizeCourierOriginColumnNames      pendingCount=0
module=Shipping      current=20261112120000_AddProcessingHoursAndTimingPolicy      pendingCount=0
... every other module: pendingMigrations []
```

Two additive migrations only:
`src/backend/Modules/Order/Tooba.Order.Infrastructure/Persistence/Migrations/20261114120000_AddOrderLineObligationClosures.cs`
and
`src/backend/Modules/Fulfillment/Tooba.Fulfillment.Infrastructure/Persistence/Migrations/20261114130000_AddCourierReturnClaimLinesAndAllocations.cs`.
No manual DDL, no business SQL, no reset/reseed/backfill/delete, no global reclaimer run.

## 2. Backend focused tests

```powershell
cd src\backend
$env:TB265_CONNECTION='Host=127.0.0.1;Port=5432;Database=tooba_alpha;Username=admin;Password=123456'
$env:TB265_EVIDENCE_DIR='D:\Users\User\source\repos\SarvNewVer\docs\architecture\evidence\TB-SHIPPING-COURIER-ORIGIN-RETURNS-LINKAGE-AND-OBLIGATION-CLOSURE-265-R1'

dotnet test Host\Tooba.Host.Tests\Tooba.Host.Tests.csproj -c Release `
  --filter "FullyQualifiedName~CourierOriginReturns265RelationalTests"
```

```
Passed  A_receipt_capacity_is_claimed_exactly_once_and_across_both_conditions
Passed  B_two_independent_contexts_cannot_claim_the_same_quantity_twice
Passed  C_rejecting_a_requested_case_returns_only_its_branch_claim
Passed  D_approval_closes_the_line_obligation_releases_only_unconsumed_held_and_restocks_once
Passed  E_a_recorded_created_replacement_is_redirected_into_the_case_and_released_once
Passed  F_an_already_dispatched_replacement_can_never_be_redirected_or_refunded_as_unused
Passed  G_the_order_owned_closure_is_a_durable_row_readable_from_an_independent_context
Passed  H_a_requested_case_freezes_dispatch_of_its_claimed_replacement_parcel
Passed  I_an_exact_retry_repairs_a_case_whose_claim_link_was_lost
Passed  J_two_contexts_can_never_release_more_than_the_held_quantity
Passed  K_a_successful_refund_with_a_failed_restock_retries_only_the_disposition
Test Run Successful.  Total tests: 11  Passed: 11
```

Recorded fixture ids: `tb265-relational.txt` (three recorded runs; the last block is the final code).

```powershell
dotnet test Modules\Returns\Tooba.Returns.Tests\Tooba.Returns.Tests.csproj -c Release `
  --filter "FullyQualifiedName~CourierOriginReturns265Tests"
Test Run Successful.  Total tests: 5  Passed: 5        (log: tb265-behavior.txt)

dotnet test Modules\Fulfillment\Tooba.Fulfillment.Tests\Tooba.Fulfillment.Tests.csproj -c Release `
  --filter "FullyQualifiedName~CourierReturnClaim265Tests"
Total tests: 8  Passed: 8                              (log: tb265-fulfillment.txt)
```

Solution build and Host publish:

```powershell
dotnet build Tooba.slnx -c Release        # 8 Warning(s)  0 Error(s)   (log: D:\temp\tb265-build.txt)
dotnet publish src\backend\Host\Tooba.Host\Tooba.Host.csproj -c Release -o artifacts\publish-primary
                                          # 0 Error(s)     (log: D:\temp\tb265-publish.txt)
```

## 3. Frontend

```powershell
cd src\frontend
npm run test:returns      # 7/7
npm run typecheck         # tsc --noEmit, 0 diagnostics
npm run build             # exit 0
Get-Content .next\BUILD_ID  # JD0cZnOk5evPmE-HmLmNl
```

Served-build proof over HTTP:

```
GET /_next/static/JD0cZnOk5evPmE-HmLmNl/_buildManifest.js  -> 200
GET /_next/static/y1ZgMVMtBdg9PSdNndMYa/_buildManifest.js  -> 404   (previous build id)
GET /_next/static/chunks/3852-a745543e9279487e.js          -> 200   carries «مشاهدهٔ پروندهٔ مرجوعی»
GET /_next/static/chunks/7825-e370aae29c6ec503.js          -> 200   carries «مرحلهٔ بستن پرونده» + testid
```

## 4. Runtime restart

```powershell
# Host: published 08:00, running 08:00:45
Get-NetTCPConnection -LocalPort 5088 -State Listen   # 28852
GET http://127.0.0.1:5088/health        -> 200
GET http://127.0.0.1:5088/health/ready  -> 200 (SingleStore)

# Frontend: previous PID 39780 stopped, new production server
node node_modules/next/dist/bin/next start -p 3000    # PID 11200
GET http://127.0.0.1:3000/fa            -> 200
```

No edition/connection/port/appsettings change, no DataProtection or CapturedPreview write
(local ON 7 dates / checked-in OFF), no user order/cart/parcel/payment/return row written by this round.

## 5. Change set (git)

```powershell
git merge-base --is-ancestor d6bdc88c99502e115ebd3e3d4e7f64087dadc0f2 HEAD   # exit 0
git diff --stat   # 24 files changed, 1684 insertions(+), 152 deletions(-) + 7 new files
git commit -am "..."                          # code commit (see result-contract.txt)
git push origin HEAD:main
```

New files:

```
src/backend/Modules/Fulfillment/Tooba.Fulfillment.Application/Fulfillments/Models/CourierReturnCaseSnapshot.cs
src/backend/Modules/Fulfillment/Tooba.Fulfillment.Domain/Aggregates/CourierReturnClaimAllocation.cs
src/backend/Modules/Fulfillment/Tooba.Fulfillment.Domain/Aggregates/CourierReturnClaimLineRecord.cs
src/backend/Modules/Fulfillment/Tooba.Fulfillment.Domain/ValueObjects/CourierReturnAllocationState.cs
src/backend/Modules/Fulfillment/Tooba.Fulfillment.Domain/ValueObjects/CourierReturnClaimBinding.cs
src/backend/Modules/Fulfillment/Tooba.Fulfillment.Infrastructure/Persistence/Migrations/20261114130000_AddCourierReturnClaimLinesAndAllocations.cs
src/backend/Modules/Order/Tooba.Order.Domain/Aggregates/OrderLineObligationClosureRecord.cs
src/backend/Modules/Order/Tooba.Order.Infrastructure/Persistence/Migrations/20261114120000_AddOrderLineObligationClosures.cs
```
