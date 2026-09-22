# Order remaining Host audit (R3)

Source: independent audit at claim time (R2C HEAD `a98aca6e`). Working tree may already partially migrate OrdersGrid.

## Runtime registration

- `Program.cs`: `AddScoped AdminOrderOperationsComposer`; `AddScoped AdminOrdersGridQueryEngine` (may be unused if composer `new`s); `MapAdminOrderOperationsEndpoints()`; `MapAdminPanelEndpoints()`; `MapOrderEndpoints()` (Completeness).
- Completeness already on `Order.Endpoints` → `ISender`.

## Scope A — AdminOrderOperations (IN SCOPE)

| File | ~LOC | Role |
|------|------|------|
| `Host/Admin/AdminOrderOperationsComposer.cs` | 2267 | List/Execute/ReturnEligibility orchestration |
| `Host/Admin/AdminOrderOperationsEndpoints.cs` | 119 | Routes under `/v1/admin/orders` |
| `Host/Admin/AdminOrderOperationsModels.cs` | 76 | DTOs |

Routes IN SCOPE:

- `GET /{checkoutId}/operations`
- `POST /{checkoutId}/operations`
- `GET /{checkoutId}/return-eligibility`

OrderDbContext: read-only load in composer; mutations via ports (no SaveChanges in Ops composer).

Foreign deps today: Fulfillment/Returns/Settlement **Application**, Payment Contracts (+ leaky Payment Infra usings), AccessControl Application, Host Recovery/Supply composers.

~27 operation codes across cancel/deposit/fulfillment/package/return/refund families.

## Scope B — OrdersGrid (IN SCOPE)

| File | ~LOC | Role |
|------|------|------|
| `Host/Grid/AdminOrdersGridQueryEngine.cs` | 573 | DB-native filter/sort/page |
| `AdminPanelComposer.QueryOrdersGridAsync` / `ListOrdersAsync` | — | Panel entry |
| `AdminPanelEndpoints` `POST /orders/query`, `GET /orders` | — | HTTP |

Foreign: `PartyDbContext` (must become Party.Contracts), `IReturnDirectory` (Returns.Application → Contracts), `OrderSupplyComposer`, `IReservationCycleDirectory`.

## Deferred (leave Host unless blocking)

- `OrderInventoryRecoveryComposer` routes: `inventory-recovery`, `inventory-recovery/audit`
- `OrderSupplyComposer` route: `supply-status`
- `GET /orders/{id}` detail + AdminViewAck write (adjacent remainder / likely R4)
- Storefront checkout / reservation cycle broadly
- Sellers/Customers grids using Order DbContext

## Already migrated (R2C)

Notes, operational-history, invoice.html, receipt.html on `Order.Endpoints` with Offer-style Completeness CQRS.

## Recommended CQRS split

- Queries: `GetAdminOrderOperationsPage`, `ListAdminOrderReturnEligibility`, `QueryAdminOrdersGrid` (+ optional list)
- Commands: one Offer-style folder per operation code (~25–30), not a mega Execute facade
- Endpoints: Fulfillment-style module ownership; Host authorizer adapter only
- Kill hardcoded FA prose → Order error catalog

## Auth note

Ops HTTP gate uses tenant `#view` then composer fine-grained perms; Completeness uses explicit `order.view` / `order.handle`. Preserve dual model or unify with evidence.
