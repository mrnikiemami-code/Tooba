# TB-TMAR-ORDER-AMC-001-W1 — Physical tree after

## Domain (`Tooba.Order.Domain`)

Root god-file `OrderDomain.cs` (~888 LOC, enums+aggregates+events+notes/acks) removed and split:

- `GlobalUsings.cs` (namespace bridges for Domain consumers)
- `Aggregates/CheckoutAdminViewAck.cs`, `Aggregates/CheckoutGroup.cs`, `Aggregates/CheckoutOperationalNote.cs`,
  `Aggregates/OrderLine.cs`, `Aggregates/SellerOrder.cs`
- `Checkout/CartShippingDraft.cs`, `Checkout/CheckoutProcess.cs`, `Checkout/CheckoutReservationCommit.cs`
- `Checkout/Abuse/CheckoutAbuseBlockEvent.cs`, `Checkout/Abuse/CheckoutAbuseCustomerLock.cs`
- `Enums/OrderMode.cs`, `Enums/SellerOrderStatus.cs`
- `Events/CheckoutSubmittedDomainEvent.cs`, `Events/SellerOrderCreatedDomainEvent.cs`
- `PendingPayment/PendingPaymentCardHide.cs`
- `Reservation/ReservationCycle.cs`
- `Rules/InvoiceHeaderSemantics.cs`, `Rules/OpenUnpaidOrderPredicate.cs`

Path ↔ namespace is exact (`Tooba.Order.Domain.Aggregates|Checkout|Checkout.Abuse|Enums|Events|PendingPayment|Reservation|Rules`).
Root allowlist = `GlobalUsings.cs` only. Root-Allowlist-State: `ENFORCED`.

## Application (`Tooba.Order.Application`)

Seven single-file request leaf folders flattened (capability-first, shallow default):

| Before | After |
|---|---|
| `Admin/Customers/Queries/ListAdminCustomers/ListAdminCustomersQuery.cs` | `Admin/Customers/Queries/ListAdminCustomersQuery.cs` |
| `Admin/Dashboard/Queries/GetAdminOrderDashboardMetrics/GetAdminOrderDashboardMetricsQuery.cs` | `Admin/Dashboard/Queries/GetAdminOrderDashboardMetricsQuery.cs` |
| `Admin/LegacyList/Queries/ListAdminOrders/ListAdminOrdersQuery.cs` | `Admin/LegacyList/Queries/ListAdminOrdersQuery.cs` |
| `Admin/Sellers/Queries/GetSellerOrderCounts/GetSellerOrderCountsQuery.cs` | `Admin/Sellers/Queries/GetSellerOrderCountsQuery.cs` |
| `Customer/Queries/GetCustomerOrderDashboardSummary/GetCustomerOrderDashboardSummaryQuery.cs` | `Customer/Queries/GetCustomerOrderDashboardSummaryQuery.cs` |
| `Seller/Queries/GetSellerOrderDashboardSummary/GetSellerOrderDashboardSummaryQuery.cs` | `Seller/Queries/GetSellerOrderDashboardSummaryQuery.cs` |
| `Storefront/PendingPayment/Queries/ListStorefrontPendingPayments/ListStorefrontPendingPaymentsQuery.cs` | `Storefront/PendingPayment/Queries/ListStorefrontPendingPaymentsQuery.cs` |

Multi-file use-case leaves (`QueryAdminCustomersGrid/`, `GetAdminOrderDetail/`, `SubmitStorefrontCheckout/`, …) stay
because they own multiple cohesive production files. `GlobalUsings.cs` added at Application root (Catalog precedent).
Folder-Granularity-State: `PROFESSIONAL_SHALLOW` for the touched request leaves.

## Infrastructure (`Tooba.Order.Infrastructure`)

- `GlobalUsings.cs` added at root; `AllowedRootCsFiles` = `GlobalUsings.cs`, `OrderModule.cs`.

## Cross-module edge (W0 correction)

`Tooba.Order.Domain → Tooba.Offer.Contracts` is **live**, not stale: `CheckoutGroup.Channel` is typed
`Tooba.Offer.Contracts.Dtos.SalesChannel` and `CheckoutSnapshots.Channel` / `CheckoutDirectory` /
`StorefrontCheckoutService` all consume it. It is a Contracts-only edge with an existing characterization
test (`Host.Tests/OrderOfferContractsCharacterizationTests.cs`) and is therefore retained, not removed.
W0's "stale reference" claim is corrected here.
