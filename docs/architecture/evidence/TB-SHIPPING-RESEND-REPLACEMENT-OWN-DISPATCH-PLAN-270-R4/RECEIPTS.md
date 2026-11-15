# RECEIPTS — TB-SHIPPING-RESEND-REPLACEMENT-OWN-DISPATCH-PLAN-270-R4

All commands ran on the ONE canonical environment: `tooba_alpha` @ 127.0.0.1:5432 (SingleStore `store-alpha`),
Host 5088, Next 3000. Session window: 2026-11-15 18:02 → 18:45 (+03:30).

## 0. Recovery / base

```
git rev-parse HEAD                                         1ff02ab8a18c20999b92791f2b51520726bfa2d1
git fetch origin --quiet ; git rev-parse origin/main       1ff02ab8a18c20999b92791f2b51520726bfa2d1
git merge-base --is-ancestor 1ff02ab8a… origin/main        True (expected base IS the actual head)
git status -sb                                             ## main...origin/main
```

Issued `Expected-Base-SHA` exists and is an ancestor of `origin/main`; no docs-only advancement, no divergence, no
RECOVERY_CONFLICT. No stash / reset / rebase / amend / force was used.

Pre-existing untracked scratch (not part of this change set, untouched):
`scripts/tb266r1-*.mjs`, `scripts/tb270r2-*.mjs`.

## 1. Targeted checks (each run ONCE, after the final code change)

```
dotnet test src/backend/Modules/Order/Tooba.Order.Tests/Tooba.Order.Tests.csproj -c Release \
  --filter "FullyQualifiedName~AdminShipmentPlan270R2Tests"
  → Passed!  - Failed: 0, Passed: 28, Skipped: 0, Total: 28, Duration: 649 ms

cd src/frontend; npx tsx --tsconfig tsconfig.mount-tests.json --test features/order-operational-groups/created-parcel.test.ts
  → tests 13 / pass 13 / fail 0

cd src/frontend; npx tsx --tsconfig tsconfig.mount-tests.json --test app/admin/admin-shipment-plan-270r2.mount.test.ts
  → tests 14 / pass 14 / fail 0

cd src/frontend; npx tsx --tsconfig tsconfig.mount-tests.json --test app/admin/admin-order-operational-delivery.mount.test.ts
  → tests 75 / pass 75 / fail 0

cd src/frontend; npx tsc --noEmit
  → exit 0 (no output)
```

The first pass of `admin-shipment-plan-270r2.mount.test.ts` reported `tests 14 / pass 13 / fail 1`
(`TypeError: Cannot read properties of undefined (reading 'checkoutId')`) from a mis-shaped `writeShipmentPlanAttempt`
call in the NEW test only; the call was corrected to `{ scope: SCOPE, … }` and the suite then reported 14/14. No product
code was changed to make a test pass.

Not rerun (untouched by these repairs): `AdminShipmentDispatchPlan270R2RelationalTests` (store/unique-index only),
`AdminFulfillmentOwnPlanProjection270R2Tests`, `OrderShipmentDispatchPlanStoreFilter270R3Tests`,
`DispatchPlanGuard270R2Tests`, `ReplacementReadiness270R3Tests`, `AdminFulfillmentBulkTests`,
`ShippingAttemptSchedulingLookupTests`. No unfiltered suite, no baseline/stash comparison, no disposable Postgres matrix.

## 2. Builds

```
dotnet build src/backend/Tooba.slnx -c Release
  → Build succeeded. 332 Warning(s) 0 Error(s)

dotnet publish src/backend/Host/Tooba.Host/Tooba.Host.csproj -c Release -o artifacts/publish-primary
  → 0 errors (only offline-feed NU1900 warnings)

cd src/frontend; npm run build
  → ✓ Compiled successfully in 9.0s
  → .next/BUILD_ID = cz4aReIm9Nx33hKljkmfr
```

## 3. Deploy / restart

```
Stop-Process -Id 43312            # previous Host listener (graceful stop BEFORE publishing over locked output)
dotnet publish …                  # succeeded, no MSB3027/MSB3021 file-lock failure this round
Start-Process dotnet Tooba.Host.dll … (ASPNETCORE_ENVIRONMENT=Development, ASPNETCORE_URLS=http://127.0.0.1:5088)
  → hostPid 48388
GET /health       200 {"status":"ok"}
GET /health/ready 200 {"status":"ready","checks":{"edition":"SingleStore","postgresql":"configured",
                        "authorization":"inmemory","messaging-transport":"postgresql-sql","messaging":"Healthy"}}
Get-NetTCPConnection -LocalPort 5088 -State Listen → OwningProcess 48388

Stop-Process -Id 620              # previous Next server
npx next start -p 3000            # launcher 47940 → listener PID 31184
GET /fa                                                          200
GET /_next/static/cz4aReIm9Nx33hKljkmfr/_buildManifest.js        200 (6,612 B)
GET /fa/admin/orders/01a1db2f-e36e-…/operations                   200 (read-only history page; 31 chunks)
GET /_next/static/chunks/9424-c99e9070dc020045.js                200 (191,506 B) — carries
                                                                  «تلاش دوباره با همین درخواست»
```

## 4. Data / schema boundaries (zero business writes)

```
migration added/applied       NONE (this repair needs code/contracts/client state only)
DDL / reset / reseed          NONE
business SQL / cleanup        NONE
fixture order/parcel/plan     NONE
restore / backup change       NONE (R2 post-apply dump preserved; missing pre-apply backup still declared)
provider / waybill / money    NONE
notification                  NONE
```

The only HTTP calls made were GETs against `/health`, `/health/ready`, `/fa`, the build manifest/chunks and the existing
completed-history operations page. No `plan_shipment_dispatch`, no create/dispatch/deliver/return/refund was sent.
