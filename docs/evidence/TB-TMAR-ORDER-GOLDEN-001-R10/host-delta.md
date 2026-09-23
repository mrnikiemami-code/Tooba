# Host delta — TB-TMAR-ORDER-GOLDEN-001-R10

## Removed from Host (Order authority)

| Surface | Change |
|---------|--------|
| `SellerPanelEndpoints` `/orders`, `/orders/{sellerOrderId}` | Routes removed; owned by `Order.Endpoints.SellerOrderEndpoints` |
| `SellerPanelComposer` OrderDbContext / AccessControl order.view / Catalog category fallback / SellerOrder list-detail-dashboard counting | Removed; no Order business methods |
| `SellerOrderListItem`, `SellerOrderLineView`, `SellerOrderDetailPage` in Host models | Moved to `Order.Application/Seller/.../Models` |

## Host retained (thin composition)

| Surface | Role |
|---------|------|
| `GET /v1/seller/dashboard` | Host; seller display via Party + Order open/paid via `GetSellerOrderDashboardSummaryQuery`; ActiveOffers stays 0 |
| `GET /v1/seller/catalog-variants` | Host; Catalog only |
| Catalog attribute / variant-axis mutation routes | Host |
| `SellerPanelComposer` | Catalog variants + seller display shell only |
| `HostOrderSellerAuthorizer` | Thin Actor/SellerParty seam for Order seller routes (`IOrderSellerAuthorizer`) |
| `HostSellerOrderViewAccessReader` | Thin AccessControl → `ISellerOrderViewAccessReader` adapter |

## Moved / owned by Order

| Surface | Location |
|---------|----------|
| Routes | `Order.Endpoints/SellerOrderEndpoints.cs` via `ISender` + `ApiResponseFactory` |
| List | `ListSellerOrdersQuery` |
| Detail | `GetSellerOrderDetailQuery` |
| Dashboard summary | `GetSellerOrderDashboardSummaryQuery` |
| Store | `Order.Infrastructure/Seller/SellerOrderStore.cs` (`SellerPartyId`) |
| Composer | `Order.Application/Seller/SellerOrderComposer.cs` (Catalog/Party **Contracts** + AccessControl port only) |

## SoT

- Order: `INCOMPLETE_REFERENCE_REPAIR`
- Checkout: `PAUSED_AT_SAFE_W5_CHECKPOINT`
- nextTask: `TB-TMAR-ORDER-GOLDEN-001-R11` (not started)
- Slice: `SELLER_PANEL_ORDER_CQRS_R10`

## Still remaining in Host for Order

- Admin dashboard / list / grids (R11)
- Thin adapters (storefront/admin/customer/seller authorizers, unpaid expiry shell, AccessControl effective-access adapter)
