PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RECEIPTS
Task-ID: TB-SHIPPING-MARKETPLACE-OPERATIONAL-DELIVERY-GROUPS-253-R2
Channel: tooba-main
Worker-Id: tooba-worker-01
Bridge-Task-Row: 0e96ceb7-3651-40cc-a0b6-cb6ea34889fe
Claimed-At-UTC: 2026-11-08T11:07:00.6274134Z
Base-SHA: 9532d6c87045313231f0a9da06d8627679dcfe58
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

## 1. Claim and base

- `git rev-parse HEAD` == `git rev-parse origin/main` == `9532d6c87045313231f0a9da06d8627679dcfe58` before work.
- Bridge `GET /api/tasks?channelId=tooba-main` showed this task `Claimed`, row `0e96ceb7-3651-40cc-a0b6-cb6ea34889fe`.
- No other Pending task on `tooba-main`; no other channel used; no historical subagent launched/resumed.

## 2. Source changes (working tree)

Backend:

- `src/backend/Modules/Order/Tooba.Order.Contracts/Shipping/MarketplaceOperationalDeliveryGroupAuthority.cs`
  (Marketplace owner identity is a proven origin; `WithGroupWorkflowReadiness`;
  `BuildMemberParcelDispatchKey`; `BuildWorkflowOperationLocator`; `WorkflowGroupForeign`,
  `WorkflowSelectionRequired`, `WorkflowShipmentForeign`).
- `src/backend/Modules/Order/Tooba.Order.Application/Admin/Operations/Services/MarketplaceOperationalGroupWorkflow.cs` (new).
- `src/backend/Modules/Order/Tooba.Order.Application/Admin/Operations/Services/AdminOrderOperationalFacts.cs` (new, shared mapper).
- `src/backend/Modules/Order/Tooba.Order.Application/Admin/Operations/Commands/PrepareOperationalGroup/PrepareOperationalGroupCommand.cs` (new).
- `src/backend/Modules/Order/Tooba.Order.Application/Admin/Operations/Services/AdminOrderOperationsOrchestrator{.cs,.Operations.cs,.Projection.cs,.Fulfillment.cs}`.
- `src/backend/Modules/Order/Tooba.Order.Application/Admin/Operations/Models/AdminOrderOperationsModels.cs`.
- `src/backend/Modules/Order/Tooba.Order.Application/Admin/Detail/{AdminOrderDetailComposer.cs,Models/AdminOrderDetailModels.cs,Queries/GetAdminOrderDetail/GetAdminOrderDetailQuery.cs}`.
- `src/backend/Modules/Order/Tooba.Order.Endpoints/Admin/Operations/AdminOrderOperationsEndpoints.cs`.
- `src/backend/Modules/Order/Tooba.Order.Tests/Checkout/OrderEvidenceRelationalTestTarget.cs` (parameterized reuse of the same 15433 guard).
- `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Infrastructure/Tooba.Fulfillment.Infrastructure.csproj`
  (test project internals to the production fulfillment admin adapter; no runtime behavior change).

Tests:

- `src/backend/Modules/Order/Tooba.Order.Tests/Admin/MarketplaceOperationalGroupWorkflowRelationalTests.cs` (new, 4 cases).
- `src/backend/Modules/Order/Tooba.Order.Tests/Admin/MarketplaceOperationalDeliveryGroupAuthorityTests.cs`
  (two assertions updated for the corrected R2 Marketplace origin rule; Store/Seller fail-closed retained).

Frontend:

- `src/frontend/app/admin/admin-order-operational-delivery-section.tsx` (group review of remaining packed items,
  synchronous single-flight `useRef`, `finally` release, inline refusal, retained quantities).
- `src/frontend/app/admin/admin-order-operational-delivery.mount.test.ts` (8 mounted/static facts).
- `src/frontend/app/admin/admin-api.ts`, `src/frontend/app/admin/admin-order-operations.ts`,
  `src/frontend/app/admin/admin-order-items-shipping-panel.tsx`.

## 3. Validation receipts

### 3.1 Disposable PostgreSQL relational matrix

Container: `tb253r2-pg` → `127.0.0.1:15433`, `POSTGRES_DB=order_tb253r2`,
`POSTGRES_USER=tb253r2`, `POSTGRES_PASSWORD=local` (task-owned; never 5432, never `tooba_alpha`).

```text
$env:TOOBA_TB253R2_ORDER_POSTGRES="Host=127.0.0.1;Port=15433;Database=order_tb253r2;Username=tb253r2;Password=local"
dotnet test src\backend\Modules\Order\Tooba.Order.Tests\Tooba.Order.Tests.csproj --filter "Category=TB253R2"
Passed!  - Failed: 0, Passed: 4, Skipped: 0, Total: 4
```

Cases: `group-workflow` (two compatible Marketplace sellers → two group-bound parcels + one common package,
real parcel ids/quantities/membership asserted), `retry-reuse` (retry/twin reuses recorded parcels, no second
allocation), `refusals` (foreign/partial/incompatible selections refused), `incompatible-slot`
(different receiving commitment not groupable).

### 3.2 Authority + workflow unit facts

```text
dotnet test ... --filter "FullyQualifiedName~OperationalDeliveryGroup|FullyQualifiedName~MarketplaceOperationalGroup"
Passed!  - Failed: 0, Passed: 17, Skipped: 0, Total: 17
```

Note: the first run of this filter was `Failed: 2, Passed: 15` because two assertions still encoded the
superseded R1 location-only origin rule. The Marketplace origin correction is part of R2 scope (a real
Marketplace Manual group carries no pickup location), so those two assertions were re-expressed against the
corrected rule (`Marketplace_owner_identity_is_a_proven_origin_without_a_location`,
`Store_missing_origin_still_fails_closed`); no fail-closed assertion was removed and no test was skipped.

### 3.3 Mounted admin section

```text
npx tsx --tsconfig tsconfig.mount-tests.json --test app/admin/admin-order-operational-delivery.mount.test.ts
tests 8 / pass 8 / fail 0
```

Covers: static group render with both sellers/items/parcel/action; reviewed quantities produce exactly one
group request; same-tick double click performs exactly one mutation; known refusal inline + quantities
retained; lost response inline + remount reconciles recorded parcels; actor without manage permission sees
no actionable create; seller-owned groups offer no group workflow; source uses synchronous single-flight
with try/finally and inline error.

### 3.4 Frontend typecheck and production build

```text
npx tsc --noEmit      → exit 0
npm run build         → success (route table emitted, middleware 34.9 kB, no errors)
```

### 3.5 Backend build

`dotnet test` compiles `Tooba.Order.Application`, `Tooba.Order.Endpoints`, `Tooba.Order.Infrastructure`,
`Tooba.Fulfillment.Infrastructure` and the test project with 0 errors before running the cases.

## 4. Not executed / not claimed

- No Live Tipax/provider call (deferred by the user); no carrier credentials requested.
- No schema, migration, seed, reset or business SQL on the canonical database.
- No canonical FE3000/Host5088 rebuild/redeploy in this slice (no shipped runtime asset was required by the
  issued scope; the changed backend/FE are part of the next deployment).
- No visual/viewport acceptance; user appearance review remains the user's.
- No change to financial seller orders or committed shipping-group history; no Store→Marketplace promotion.

## 5. Repository

- Source (`Code-SHA`): `8580097d696f5b1fd0f9c8ed9382dea2c912eccc`
  — `feat(shipping): one reviewed Marketplace operational group workflow + real relational proof`
- Evidence commit (`Evidence-SHA`): `be5f9c54db2efcba90f5e0b910cbeaa57fdd5a94`
  — `docs(shipping): 253-R2 evidence, task contract and TOP updates` (complete envelope; the trailing
  stamp commit only aligns this declared SHA).
- `git push origin main` then `git fetch` and `HEAD == origin/main` verified before Bridge delivery.

END_TOOBA_WORKER_RECEIPTS

## ARCHITECT CORRECTION (2026-11-08)

R2 receipts retained as-is; the PASS envelope above is superseded by the R3 correction recorded in
`RESULT.md` / `../TB-SHIPPING-MARKETPLACE-OPERATIONAL-DELIVERY-GROUPS-253-R3/`. No R2 artifact was rewritten.

