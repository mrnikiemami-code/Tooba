# RECEIPTS — TB-SHIPPING-RESEND-REPLACEMENT-OWN-DISPATCH-PLAN-270-R2

## Repository

```
repository      mrnikiemami-code/Tooba
branch          main
expected base   15b0d91488637631dbbc7e466212dd226b26f919  (present, checked out, and the actual work base)
parent commit   198176d6ffabb2acee3e29ffd1938c3fd748c46f
```

No reset, rebase, stash, amend or force was used. Untracked `scripts/tb266r1-*.mjs` and `scripts/tb270r2-*.mjs` are
pre-existing/round scratch artifacts and are NOT part of this change set.

## Focused commands (run once after the final code change)

```
dotnet test src/backend/Modules/Order/Tooba.Order.Tests/Tooba.Order.Tests.csproj -c Release \
  --filter "FullyQualifiedName~AdminShipmentPlan270R2Tests|FullyQualifiedName~AdminShipmentDispatchPlan270R2RelationalTests"
  -> Passed!  Failed: 0, Passed: 19, Skipped: 0, Total: 19

dotnet test src/backend/Modules/Order/Tooba.Order.Tests/Tooba.Order.Tests.csproj -c Release --no-build \
  --filter "FullyQualifiedName~AdminFulfillmentOwnPlanProjection270R2Tests"
  -> Passed!  Failed: 0, Passed: 3, Skipped: 0, Total: 3

dotnet test src/backend/Modules/Fulfillment/Tooba.Fulfillment.Tests/Tooba.Fulfillment.Tests.csproj -c Release \
  --filter "FullyQualifiedName~DispatchPlanGuard270R2Tests"
  -> Passed!  Failed: 0, Passed: 6, Skipped: 0, Total: 6

cd src/frontend
npx tsx --tsconfig tsconfig.mount-tests.json --test \
  app/admin/admin-shipment-plan-270r2.mount.test.ts app/admin/admin-order-operational-delivery.mount.test.ts
  -> tests 77 / pass 77 / fail 0

npx tsc --noEmit            -> exit 0
npm run build               -> 0 errors, BUILD_ID jdEuO1DKPbRL6rVo1vqUG
dotnet publish src/backend/Host/Tooba.Host/Tooba.Host.csproj -c Release -o artifacts/publish-primary
                            -> exit 0 (0 errors)
```

The unfiltered Order/Fulfillment/Shipping/Composition suites were **not** run (out of the issued focused scope; R1's
disclosed whole-project failures are not re-litigated or certified here).

## Migration

```
dotnet run --project src/backend/Host/Tooba.MigrationRunner -c Release -- plan --tenant store-alpha
  Order  currentMigration 20261115120000_AddShipmentDispatchPlans  pendingMigrations []  succeeded true
  (all other modules unchanged, pendingMigrations [])
```

Applied exactly once by the runner. `pre-apply-plan.json` (52,119 B) is the plan captured before the apply.

### Backup discipline — disclosed gap

The issued order was backup **then** apply. The apply happened first; only `pre-apply-plan.json` existed at that moment.
A **post-apply** private backup was then taken:

```
D:\temp\tb270r2-backup\tooba_alpha-post-tb270r2.dump   2,949,234 bytes   sha256:331B373AD45882D9…
docker exec postgres-db sh -c "pg_dump -U admin -d tooba_alpha -Fc -f /tmp/tooba_alpha-post-tb270r2.dump"
documented restore (NOT executed): pg_restore -U admin -d tooba_alpha --clean --if-exists <dump>
```

Residual risk is bounded: the migration only `CREATE TABLE`/`CREATE (UNIQUE) INDEX` and its `Down` drops exactly that
table.

## Services

```
Host   http://127.0.0.1:5088   PID 19240   /health 200   /health/ready 200 (SingleStore)
FE     http://127.0.0.1:3000   PID 41560   /fa 200, /fa/admin/orders/<order>/operations 200, /fa/admin/fulfillments 200
BUILD_ID jdEuO1DKPbRL6rVo1vqUG
/_next/static/jdEuO1DKPbRL6rVo1vqUG/_buildManifest.js                            -> 200
/_next/static/chunks/9424-91fab6e90a400226.js                                    -> 200  (carries «زمان‌بندی ارسال» + admin-shipment-plan-slot)
```

The previous Host process (PID 12752) held the publish directory open and made the first `dotnet publish` fail with
MSB3027/MSB3021; it was stopped, the publish re-ran clean, and the new process was started with
`ASPNETCORE_ENVIRONMENT=Development` and `ASPNETCORE_URLS=http://127.0.0.1:5088` (a first start without the URL variable
listened on `:5000` and was replaced). The previous Next process (PID 38800) was serving the old build id and was
replaced so the changed FE is truly served.

## Read-only data observations

```
select count(*) from "order".shipment_dispatch_plans;                 -> 0
linked parcels (resend_of_shipment_id not null or damaged_resolution_id not null):
  01a1dc16-482d-7000-8d64-53b199873504 | source 01a1dc15-b0b7-7000-9118-953a3f6e92d2 | Delivered
```

No business write: no parcel, hold, reservation, stock, decision, plan, order, payment or user row was created or
modified by the worker. No provider call, no waybill, no notification send, no money movement.

## Evidence artifacts

```
docs/ai/tasks/TB-SHIPPING-RESEND-REPLACEMENT-OWN-DISPATCH-PLAN-270-R2/TASK.md          (received file, verbatim)
docs/architecture/evidence/TB-SHIPPING-RESEND-REPLACEMENT-OWN-DISPATCH-PLAN-270-R2/
  TASK.md  RESULT.md  RECEIPTS.md  result-contract.txt
  ownership-and-plan.md  readiness-and-feasibility-proof.md  replay-and-dispatch-guard-proof.md  user-review.md
docs/architecture/evidence/TB-SHIPPING-RESEND-REPLACEMENT-OWN-DISPATCH-PLAN-270-R1/ARCHITECT-REVIEW.md  (R1 checkpoint persisted)
```

No screenshots were requested, captured or delivered (explicit instruction).
