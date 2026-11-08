PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RECEIPTS
Task-ID: TB-SHIPPING-MARKETPLACE-OPERATIONAL-DELIVERY-GROUPS-253-R3
Channel: tooba-main
Worker-Id: tooba-worker-01
Bridge-Task-Row: f924010d-e600-40a2-9734-f8b63961614a
Claimed-At-UTC: 2026-11-08T11:20:27.9987396Z
Base-SHA: 2ac19888686b668530b146c7f80702c8edbeb415
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
Measured-Elapsed: ~42 minutes (claim 11:20Z → envelope ≈12:02Z), no clock reset

## 1. Claim and base

- `git rev-parse HEAD` == `git rev-parse origin/main` == `2ac19888686b668530b146c7f80702c8edbeb415` at claim.
- Bridge `GET /api/tasks?channelId=tooba-main` row `f924010d-e600-40a2-9734-f8b63961614a` = `Claimed`.
- No other Pending task on `tooba-main`; no historical subagent launched/resumed; no other channel used.

## 2. Source changes (working tree)

Backend:
- `src/backend/Modules/Order/Tooba.Order.Contracts/Shipping/MarketplaceOperationalDeliveryGroupAuthority.cs`
  (Marketplace origin fallback removed; `BuildMemberParcelDispatchKey(checkout, occurrence, group)`;
  `BuildWorkflowOperationLocator(occurrence)`; `WorkflowOccurrenceRequired`, `WorkflowIntentConflict`).
- `src/backend/Modules/Order/Tooba.Order.Application/Admin/Operations/Models/AdminOrderOperationsModels.cs`
  (`OperationalGroupOccurrenceId`).
- `src/backend/Modules/Order/Tooba.Order.Application/Admin/Operations/Services/MarketplaceOperationalGroupWorkflow.cs`
  (occurrence prebound/required; facts read before parcels; complete preflight `BuildPlan`; replay by occurrence key;
  binder receives `member.ShippingGroupId`).
- `src/backend/Modules/Order/Tooba.Order.Application/Admin/Operations/Models/...` +
  `Storefront/Ports/IOrderShippingGroupHistoryReader.cs`,
  `Infrastructure/Integrations/Storefront/OrderShippingGroupHistoryReader.cs`,
  `Admin/Operations/Policies/OperationalDeliveryGroupFactMapper.cs` (origin context seam).
- `src/backend/Modules/Fulfillment/...` `ShipmentSnapshot.ShippingDispatchKey`, contracts/adapter/read-models wiring.

Tests:
- `src/backend/Modules/Order/Tooba.Order.Tests/Admin/MarketplaceOperationalGroupWorkflowRelationalTests.cs`
  (real PostgreSQL matrix, cases A/B/C/D/E + refusals + incompatible slot).
- `src/backend/Modules/Order/Tooba.Order.Tests/Admin/MarketplaceOperationalDeliveryGroupAuthorityTests.cs`
  (restored missing-origin/differentiated-origin/occurrence-key facts).

Frontend:
- `src/frontend/app/admin/operational-group-occurrence-store.ts` (new; mint/read/write/clear per checkout+group).
- `src/frontend/app/admin/admin-order-operational-delivery-section.tsx` (prebound occurrence per run; renew only
  after success; async outcome).
- `src/frontend/app/admin/admin-order-items-shipping-panel.tsx` (forwards occurrence; returns success boolean).
- `src/frontend/app/admin/admin-order-operations.ts` (request field + JSON body).
- `src/frontend/app/admin/admin-order-operational-delivery.mount.test.ts` (10 facts).

Migration ledger: **none** — no schema change was required; no migration applied, no DDL, no history rewrite.

## 3. Validation

### 3.1 Disposable PostgreSQL matrix (real Order evidence)

```text
docker run -d --name tb253r3-pg -e POSTGRES_USER=tb253r3 -e POSTGRES_PASSWORD=*** \
  -e POSTGRES_DB=order_tb253r3 -p 127.0.0.1:15433:5432 postgres:16-alpine
$env:TOOBA_TB253R3_ORDER_POSTGRES="Host=127.0.0.1;Port=15433;Database=order_tb253r3;Username=tb253r3;Password=***"
dotnet test src\backend\Modules\Order\Tooba.Order.Tests\Tooba.Order.Tests.csproj --filter "Category=TB253R3"

Passed! - Failed: 0, Passed: 9, Skipped: 0, Total: 9   (repeated 6× consecutively, all green)
```

Case ledger is in RESULT §E (A missing/different origin, B replay + barrier concurrency, C identical next
occurrence, D preflight/typed conflicts with zero partial writes, E injected persistence failure + exact-id
recovery, refusals, incompatible slot).

Correction history inside this slice: the first run failed 5/9 and exposed three real defects in the R3 rewrite —
(a) `AcceptedGroupShipmentCreateBinder.Resolve` was called without the member `ShippingGroupId`
(`order.shipment.accepted_group_required`), (b) replay was refused because remaining packed quantity had already
been consumed by the same occurrence, (c) facts-then-parcels read order allowed a concurrent twin to look like an
over-selection. All three were fixed in owning code before the recorded green runs; no test was skipped/re-baselined.

### 3.2 Authority + workflow

```text
dotnet test ... --filter "Category=TB253R3|FullyQualifiedName~MarketplaceOperationalDeliveryGroupAuthorityTests"
Passed! - Failed: 0, Passed: 26, Skipped: 0, Total: 26
```

(17 authority facts + 9 relational facts.)

### 3.3 Mounted admin section

```text
npx tsx --tsconfig tsconfig.mount-tests.json --test app/admin/admin-order-operational-delivery.mount.test.ts
tests 10 / pass 10 / fail 0
```

### 3.4 Frontend typecheck / production build / Host publish

```text
npx tsc --noEmit -p tsconfig.json        → exit 0
npm run build                            → exit 0, Middleware 34.9 kB, BUILD_ID b-jSPMIvyhKxNpaZ1CDZL
dotnet publish Tooba.Host -c Release -o artifacts\publish-primary → exit 0 (warnings only, pre-existing)
```

## 4. Canonical runtime (actual, after deploy)

- Host PID **14932** `:5088` `/health` 200; `GET /__platform-commerce` →
  `{"edition":"SingleStore","deploymentId":"store-alpha-canonical-tb253r3","tenantId":"store-alpha",
  "connectionReference":"tenant-alpha","database":"tooba_alpha"}`.
- FE PID **40052** `:3000` `/fa` 200; Build ID `b-jSPMIvyhKxNpaZ1CDZL`.
- DataProtection `KEYS_BEFORE=True`, `KEYS_AFTER=True`.
- CapturedPreview local ON (Enabled/SelectionEnabled/CheckoutEnabled/AvailableDateCount=7); checked-in OFF.

## 5. Authorized read-only review

- Host `GET /v1/admin/orders/01a1b92d-e97e-7000-999a-143350d0edaf` with
  `X-Tooba-Dev-Actor-User-Id: 01a036c2-970e-7000-8eb7-94bf5cc2d8db` → **200**; `shippingGroups=3`
  ownerKinds `Store,Store,Store`; `operationalDeliveryGroups=3`, all `isCrossSellerMarketplace=false`,
  `consolidateEligible=false`.
- FE `/admin/orders/01a1b92d-…` → 200 `admin-shell`.
- Fresh sessionless task-owned Chrome (`D:\temp\tb253r3-chrome`, CDP 9339, PID 34532) with the dev-actor seam:
  denial banner absent, operational section rendered, **3** group cards, `scrollWidth` not exceeding viewport at
  1440/1024/390, hit-test inside the section at each width, cards stacked, no group action button.
- No mutation: no pack/create/dispatch/cancel/payment or merchant edit.

Shared-profile disclosure: the agent's default browser profile carries a non-admin HttpOnly session that the Host
prefers over the dev-actor header, so the authorized render was measured on the fresh profile above. No user
profile/tab was touched, closed or automated.

## 6. Cleanup (owned resources)

- `tb253r3-pg` container stopped/removed after the matrix and evidence readback.
- Task Chrome profile `D:\temp\tb253r3-chrome` removed; task Chrome process stopped.
- No global Docker restart, no port change, no other task runtime touched.

## 7. Repository

- Code commit + evidence commit appended below by the final envelope; `git push origin main`, then `git fetch`
  and `HEAD == origin/main` verified before Bridge delivery.
- No amend/force/reset/stash/rebase.

END_TOOBA_WORKER_RECEIPTS
