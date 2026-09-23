# Endpoints organization — TB-TMAR-ORDER-POSTCLOSURE-STRUCTURE-001

## Before

Capability-specific endpoint files lived at `Tooba.Order.Endpoints` project root:

- `AdminCustomersEndpoints.cs`
- `AdminOrderCompletenessEndpoints.cs`
- `AdminOrderDetailEndpoints.cs`
- `AdminOrderInventoryRecoverySupplyEndpoints.cs`
- `AdminOrderOperationsEndpoints.cs`
- `AdminOrdersGridEndpoints.cs`
- `CustomerOrderEndpoints.cs`
- `SellerOrderEndpoints.cs`
- `StorefrontOrderEndpoints.cs`

Root also held composition/shared items (kept):

- `OrderEndpointModule.cs`
- `Errors/`
- `Resources/`

## After

```text
Tooba.Order.Endpoints
├─ Admin
│  ├─ Completeness/AdminOrderCompletenessEndpoints.cs
│  ├─ Customers/AdminCustomersEndpoints.cs
│  ├─ Detail/AdminOrderDetailEndpoints.cs
│  ├─ InventoryRecovery/AdminOrderInventoryRecoverySupplyEndpoints.cs
│  ├─ Operations/AdminOrderOperationsEndpoints.cs
│  └─ OrdersGrid/AdminOrdersGridEndpoints.cs
├─ Customer/CustomerOrderEndpoints.cs
├─ Seller/SellerOrderEndpoints.cs
├─ Storefront/StorefrontOrderEndpoints.cs
├─ Errors/
├─ Resources/
└─ OrderEndpointModule.cs
```

Folder names match Application capability naming (Admin.*, Customer, Seller, Storefront). No deviations.

## Namespace alignment

| Path | Namespace |
| --- | --- |
| Admin/Customers/AdminCustomersEndpoints.cs | Tooba.Order.Endpoints.Admin.Customers |
| Admin/Completeness/AdminOrderCompletenessEndpoints.cs | Tooba.Order.Endpoints.Admin.Completeness |
| Admin/Detail/AdminOrderDetailEndpoints.cs | Tooba.Order.Endpoints.Admin.Detail |
| Admin/InventoryRecovery/AdminOrderInventoryRecoverySupplyEndpoints.cs | Tooba.Order.Endpoints.Admin.InventoryRecovery |
| Admin/Operations/AdminOrderOperationsEndpoints.cs | Tooba.Order.Endpoints.Admin.Operations |
| Admin/OrdersGrid/AdminOrdersGridEndpoints.cs | Tooba.Order.Endpoints.Admin.OrdersGrid |
| Customer/CustomerOrderEndpoints.cs | Tooba.Order.Endpoints.Customer |
| Seller/SellerOrderEndpoints.cs | Tooba.Order.Endpoints.Seller |
| Storefront/StorefrontOrderEndpoints.cs | Tooba.Order.Endpoints.Storefront |
| OrderEndpointModule.cs | Tooba.Order.Endpoints |
| Errors/OrderErrorCatalogContributor.cs | Tooba.Order.Endpoints.Errors |
| Resources/OrderErrorResources.cs | Tooba.Order.Endpoints.Resources |

Authorizer interfaces moved with their capability files:

- `IOrderCustomerAuthorizer` → `Tooba.Order.Endpoints.Customer`
- `IOrderSellerAuthorizer` → `Tooba.Order.Endpoints.Seller`
- `IOrderAdminAuthorizer` remains on root `OrderEndpointModule.cs` (`Tooba.Order.Endpoints`)

Host adapters updated to match (compile-only using/FQN). No Host Order route ownership regained.

## Allowed root files

| File | Reason |
| --- | --- |
| `OrderEndpointModule.cs` | Composition entry: presentation DI + `MapOrderEndpoints()` |

No other `*.cs` at Endpoints project root.

## Route preservation

`OrderEndpointModule.MapOrderEndpoints` still registers exactly once:

1. AdminOrderCompletenessEndpoints
2. AdminOrdersGridEndpoints
3. AdminCustomersEndpoints
4. AdminOrderDetailEndpoints
5. AdminOrderOperationsEndpoints
6. AdminOrderInventoryRecoverySupplyEndpoints
7. StorefrontOrderEndpoints
8. CustomerOrderEndpoints
9. SellerOrderEndpoints

No route paths, HTTP methods, authorizer usage, ISender usage, or ApiResponseFactory behavior changed.

## Guard

`OrderEndpointOrganizationGuardTests` enforces root allowlist, path↔namespace alignment, single Map registration per capability, shared Errors/Resources, no namespace-alias workarounds, and Host non-ownership of Order routes.

## Out of scope (preserved)

- Infrastructure foldering untouched
- Application foldering untouched
- Checkout remains PAUSED_AT_SAFE_W5_CHECKPOINT
- Frontend unchanged
- Order remains COMPLETE_REFERENCE_PATTERN
