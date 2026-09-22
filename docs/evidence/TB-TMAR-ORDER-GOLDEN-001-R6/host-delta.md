# Host delta — TB-TMAR-ORDER-GOLDEN-001-R6

## Removed from Host (business authority)

| Surface | Role |
|---------|------|
| `AdminPanelEndpoints` `GET /orders/{checkoutId}` | Admin order detail route |
| `AdminPanelComposer.GetOrderAsync` + detail-only helpers | Detail composition + AdminViewAck write |
| Detail DTOs in `AdminPanelModels` | Moved to Order.Application.Admin.Detail.Models |

## Moved to Order

| Surface | Location |
|---------|----------|
| Query | `GetAdminOrderDetailQuery` + `GetAdminOrderDetailHandler` |
| Composer | `AdminOrderDetailComposer` (+ financials / fulfillment status / reservation audit mapper) |
| Models | `Order.Application/Admin/Detail/Models` |
| Port/Store | `IAdminOrderDetailCheckoutStore` / `AdminOrderDetailCheckoutStore` (AdminViewAck + IClock) |
| HTTP | `Order.Endpoints/AdminOrderDetailEndpoints` via `ISender` → `ApiResponseFactory` |
| Settlement contract | `ISettlementAdminOrderDetailReader` (Settlement.Contracts + Infrastructure adapter) |

Foreign boundaries in Order.Application Detail: Party / Catalog / Fulfillment / Payment / Returns / Settlement **Contracts** (+ Order ports/services). No foreign Application/Infrastructure/Domain/DbContext.

## Still remaining in Host for Order

- Admin dashboard / sellers / customers list (+ OrderDbContext for those list paths only)
- CustomerPanel / SellerPanel
- ReservationCycleCoordinator (Host orchestration; detail audit mapping is Order-owned)
- Geography read-only route

## Preserved R4 / R5 / R5-R1 Host removals

- No restore of CheckoutAbuseGate (Host), storefront composers, recovery/supply composers

## SoT (evidence only — R7 not started)

- Order: `INCOMPLETE_REFERENCE_REPAIR`
- Checkout: `PAUSED_AT_SAFE_W5_CHECKPOINT`
- Slice: `ADMIN_ORDER_DETAIL_CQRS_R6`
- nextTask: `TB-TMAR-ORDER-GOLDEN-001-R7`
