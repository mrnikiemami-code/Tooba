# Host delta — TB-TMAR-ORDER-GOLDEN-001-R11

## Removed from Host (Order authority)

| Surface | Change |
|---------|--------|
| `AdminPanelEndpoints` `GET /orders` | Route removed; owned by `Order.Endpoints.AdminOrdersGridEndpoints` |
| `AdminPanelEndpoints` `GET /customers`, `POST /customers/query` | Routes removed; owned by `Order.Endpoints.AdminCustomersEndpoints` |
| `AdminPanelComposer` `ListOrdersAsync` / `LoadOrderGroupsAsync` / `MapOrderListItemAsync` | Removed |
| `AdminPanelComposer` `ListCustomersAsync` / `QueryCustomersGridAsync` | Removed |
| `AdminPanelComposer` `OrderDbContext` + `SellerOrderStatus` classification | Removed |
| `Grid/AdminCustomersGridQueryEngine.cs` | Deleted; Order Infrastructure owns DB-native customers grid |
| `Admin/AdminReservationCycleMapper.cs` | Deleted — no production Host caller remained (tests retargeted to `AdminOrderReservationCycleMapper`) |
| Host `AdminCustomerListItem` / `AdminReservationCycleSummary` DTOs | Customer DTO moved to Order Application; reservation summary Host DTO removed |

## Cross-module Admin surfaces retained (allowed)

| Surface | Role | Why allowed |
|---------|------|-------------|
| `GET /v1/admin/dashboard` | Host | Combines Catalog published count + Offer active/seller counts + Order metrics via `GetAdminOrderDashboardMetricsQuery` |
| `GET /v1/admin/sellers` | Host | Party + Offer + Order count map via `ISellerOrderCountReader` |
| `POST /v1/admin/sellers/query` | Host | Cross-module sellers grid; Order metric from `ISellerOrderCountReader` (no `OrderDbContext`) |
| `AdminPanelComposer` | Host | Thin Catalog/Offer/Party composition only |
| `AdminSellersGridQueryEngine` | Host | Party/Offer SQL + Order counts port |

## Moved / owned by Order

| Surface | Location |
|---------|----------|
| `GET /v1/admin/orders` | `ListAdminOrdersQuery` + `AdminOrdersGridEndpoints` |
| `GET /v1/admin/customers` | `ListAdminCustomersQuery` + `AdminCustomersEndpoints` |
| `POST /v1/admin/customers/query` | `QueryAdminCustomersGridQuery` + `AdminCustomersGridPolicy` + `AdminCustomersGridReader` |
| Dashboard Order metrics | `GetAdminOrderDashboardMetricsQuery` |
| Seller order counts | `GetSellerOrderCountsQuery` / `ISellerOrderCountReader` |

## AdminReservationCycleMapper

- Production callers: **none** after audit
- Action: **deleted** Host facade; Order Detail mapper remains authoritative
- Evidence reason: dead Host facade not retained “just in case”

## SoT

- Order: `INCOMPLETE_REFERENCE_REPAIR`
- Checkout: `PAUSED_AT_SAFE_W5_CHECKPOINT`
- nextTask: `TB-TMAR-ORDER-GOLDEN-001-FINAL-CLOSURE` (not started)
- Slice: `ADMIN_PANEL_ORDER_RESIDUALS_CQRS_R11`

## Closure readiness

`READY_FOR_ORDER_FINAL_CLOSURE_AUDIT` (ILLEGAL_ORDER_AUTHORITY = 0 after post-R11 reverse audit; Architect still owns ACCEPT of FINAL-CLOSURE)
