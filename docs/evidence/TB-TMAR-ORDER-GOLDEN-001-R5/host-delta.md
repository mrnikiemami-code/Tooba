# Host delta — TB-TMAR-ORDER-GOLDEN-001-R5

## Removed from Host (business authority)

| File | Role |
|------|------|
| `Host/Storefront/StorefrontCheckoutComposer.cs` | Checkout preview/submit/get orchestration |
| `Host/Storefront/StorefrontPendingPaymentComposer.cs` | Pending list/cancel/hide orchestration |
| `Host/Storefront/StorefrontPendingPaymentProjector.cs` | Pending payment projection |
| `Host/Storefront/StorefrontShippingComposer.cs` | Shipping project/select/commit orchestration |
| `Host/Storefront/StorefrontShippingCalculator.cs` | Shipping rate/window calculation |
| `Host/Storefront/StorefrontIranGeography.cs` | Moved into Order Application (shipping + geography read) |
| `Host/Storefront/StorefrontRecipientNames.cs` | Moved into Order Application; panels use Order type |

### Routes removed from `StorefrontEndpoints.cs`

- `POST /v1/storefront/checkout/preview`
- `POST /v1/storefront/checkout`
- `GET /v1/storefront/checkout/{checkoutId}`
- `POST /v1/storefront/pending-payments`
- `POST /v1/storefront/checkout/{checkoutId}/cancel`
- `POST /v1/storefront/checkout/{checkoutId}/hide-pending-card`
- `POST /v1/storefront/shipping/projection`
- `PUT /v1/storefront/shipping/selection`
- `POST /v1/storefront/shipping/commit`

Host message-text classifiers for those routes removed with the route handlers.

## Moved to Order

| Surface | Location |
|---------|----------|
| Queries | `PreviewStorefrontCheckout`, `GetStorefrontCheckout`, `ListStorefrontPendingPayments`, `ProjectStorefrontShipping` |
| Commands | `SubmitStorefrontCheckout`, `CancelPendingCheckout`, `HidePendingPaymentCard`, `SaveStorefrontShippingSelection`, `CommitStorefrontShipping` |
| Services | `StorefrontCheckoutService`, `StorefrontPendingPaymentService`, `StorefrontPendingPaymentProjector`, `StorefrontShippingService`, `StorefrontShippingCalculator` |
| HTTP | `Order.Endpoints/StorefrontOrderEndpoints` via `ISender` → `ApiResponseFactory` |
| Infra stores | `StorefrontShippingDraftStore`, `StorefrontPendingCheckoutStore` (Order.Infrastructure) |

Foreign boundaries: Cart / AddressBook / Catalog / Payment / Fulfillment / Localization **Contracts** (+ Order ports). No foreign Application/Infrastructure/Domain in Order.Application.

## Still remaining in Host for Order

- Admin order **detail** (`GET /orders/{id}` + AdminViewAck) — `AdminPanelComposer` / Host DbContext (deferred R6+)
- `CheckoutAbuseGate` (Host) — implements `ICheckoutAbuseGate`; still uses Order/Catalog DbContext for abuse counting
- `CheckoutIdentityGate` (Host) — catalog identity policy; wrapped by thin `HostOrderStorefrontCheckoutIdentityGate`
- Panel composers that **consume** Order data (`AdminPanelComposer`, `CustomerPanelComposer`, `SellerPanelComposer`, grids, reservation cycle)
- Geography read-only route `GET /geography/provinces` (static data from Order `StorefrontIranGeography`)

## Allowed Host adapters

| Adapter | Reason |
|---------|--------|
| `HostOrderStorefrontActor` | Session / Dev-Testing actor header / guest secret → `IOrderStorefrontActor` (no business decisions) |
| `HostOrderStorefrontCheckoutIdentityGate` | Thin wrap of existing `CheckoutIdentityGate` |
| `HostOrderAdminAuthorizer` / effective-access readers | Auth-only (prior slices) |
| R4 recovery/supply Host files | Remain **absent** (preserved) |

## SoT (evidence only — R6 not started)

- Order: `INCOMPLETE_REFERENCE_REPAIR`
- Checkout: `PAUSED_AT_SAFE_W5_CHECKPOINT`
- nextTask: `TB-TMAR-ORDER-GOLDEN-001-R6`
