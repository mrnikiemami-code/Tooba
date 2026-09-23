# Host delta — TB-TMAR-ORDER-GOLDEN-001-R9

## Removed from Host (Order authority)

| Surface | Change |
|---------|--------|
| `CustomerPanelEndpoints` `/orders`, `/orders/{id}`, `/orders/{id}/retry-unpaid` | Routes removed; owned by `Order.Endpoints.CustomerOrderEndpoints` |
| `CustomerPanelComposer` OrderDbContext / CatalogDbContext / Payment / Party / supply / cycles | Removed; no Order business methods |
| `CustomerOrder*` DTOs in Host models | Moved to `Order.Application/Customer/.../Models` |

## Host retained (thin composition)

| Surface | Role |
|---------|------|
| `GET /v1/customer/dashboard` | Host; Order counts/recent via `GetCustomerOrderDashboardSummaryQuery` + Wishlist/AddressBook/profile |
| `GET/PUT /v1/customer/profile` | Host; optional Order summary for last shipping/recipient/mobile |
| `CustomerPanelComposer` | Wishlist / AddressBook / Profile / Identity only |
| `HostOrderCustomerAuthorizer` | Thin Actor seam for Order customer routes (`IOrderCustomerAuthorizer`) |

## Moved / owned by Order

| Surface | Location |
|---------|----------|
| Routes | `Order.Endpoints/CustomerOrderEndpoints.cs` via `ISender` + `ApiResponseFactory` |
| List | `ListCustomerOrdersQuery` |
| Detail | `GetCustomerOrderDetailQuery` |
| Dashboard summary | `GetCustomerOrderDashboardSummaryQuery` |
| Retry unpaid | `RetryCustomerUnpaidOrderCommand` + `IReservationCycleCoordinator` |
| Store | `Order.Infrastructure/Customer/CustomerOrderCheckoutStore.cs` (`PlacedByUserId`) |
| Composer | `Order.Application/Customer/CustomerOrderComposer.cs` (Catalog/Party/Payment **Contracts** only) |

## SoT

- Order: `INCOMPLETE_REFERENCE_REPAIR`
- Checkout: `PAUSED_AT_SAFE_W5_CHECKPOINT`
- nextTask: `TB-TMAR-ORDER-GOLDEN-001-R10` (not started)
- Slice: `CUSTOMER_PANEL_ORDER_CQRS_R9`

## Still remaining in Host for Order

- SellerPanel Order authority (R10)
- Admin dashboard / list / grids (R11)
- Thin adapters (storefront/admin/customer authorizers, unpaid expiry shell)
