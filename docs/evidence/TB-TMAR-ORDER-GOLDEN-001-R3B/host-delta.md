# TB-TMAR-ORDER-GOLDEN-001-R3B — Host delta

## Removed from Host

- `Host/Tooba.Host/Admin/AdminOrderOperationsEndpoints.cs` (ops + return-eligibility routes)
- `Host/Tooba.Host/Admin/AdminOrderOperationsComposer.cs` (~2479 LOC business authority)
- `Host/Tooba.Host/Admin/AdminOrderOperationsModels.cs` (wire DTOs)
- `Host/Tooba.Host/Admin/AdminFulfillmentCapabilityProjector.cs` (moved into Order.Application)
- `Program.cs` registrations: `AddScoped<AdminOrderOperationsComposer>`, `MapAdminOrderOperationsEndpoints()`

## Still remaining in Host for Order

- `OrderInventoryRecoveryComposer` (+ audit/assess/recover implementation)
- `OrderSupplyComposer` (+ supply status / ensure)
- `AdminOrderInventoryRecoverySupplyEndpoints.cs` (thin routes only):
  - `GET /v1/admin/orders/inventory-recovery/audit`
  - `GET /v1/admin/orders/{checkoutId}/inventory-recovery`
  - `GET /v1/admin/orders/{checkoutId}/supply-status`
- Storefront checkout / pending payment / shipping composers (not migrated)
- Order detail Host surfaces (not in R3B)
- Checkout W6 remains paused

## Allowed Host adapters

- `HostOrderAdminAuthorizer` — `IOrderAdminAuthorizer` (panel auth)
- `HostOrderAdminEffectiveAccessReader` — `IOrderAdminEffectiveAccessReader` (AccessControl grants → Order port)
- `HostAdminOrderOperationsInventoryRecoveryAdapter` — wraps `OrderInventoryRecoveryComposer` assess/recover (no Order business logic)
- `HostAdminOrderOperationsSupplyAdapter` — wraps `OrderSupplyComposer` status/ensure (no Order business logic)
- `HostAdminOrderSupplyStatusReader` — grid supply status bridge (R3)

## Order ownership after R3B

Routes in `Order.Endpoints.AdminOrderOperationsEndpoints`:

- `GET /v1/admin/orders/{checkoutId}/operations`
- `POST /v1/admin/orders/{checkoutId}/operations`
- `GET /v1/admin/orders/{checkoutId}/return-eligibility`

Flow: Endpoints → `ISender` → Queries/Commands → `AdminOrderOperationsOrchestrator` → Order ports + foreign Contracts only.
