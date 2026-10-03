# TB-TMAR-ORDER-AMC-001-W0 — Physical tree before

## Solution Explorer

`Tooba.slnx` → `/Modules/Order/` contains Domain, Contracts, Application, Endpoints, Infrastructure, Tests.

## Domain (`Tooba.Order.Domain`)

- Root dump (all flat `Tooba.Order.Domain`):
  - `OrderDomain.cs` (~888 LOC) — `OrderMode`, `SellerOrderStatus`, `OrderLine`, `SellerOrder`, `CheckoutGroup`, domain events, notes/acks
  - `ReservationCycle.cs` (~230 LOC) — cycle enums + entity
  - `CartShippingDraft.cs`, `CheckoutProcess.cs`, `CheckoutReservationCommit.cs`
  - `CheckoutAbuseBlockEvent.cs`, `CheckoutAbuseCustomerLock.cs`
  - `InvoiceHeaderSemantics.cs`, `OpenUnpaidOrderPredicate.cs`, `PendingPaymentCardHide.cs`
- No `Aggregates/`, `Enums/`, `Events/`, capability folders
- Stale ProjectReference: `Offer.Contracts` (unused in Domain body)

## Application (`Tooba.Order.Application`)

- Capability / audience trees: Admin/*, Checkout, Customer, Seller, Storefront/*, ReservationCycle, PurchaseVerification, Validation
- 7 single-file request leaf folders under Commands/Queries (over-foldering)
- Error code statics under Application audiences (`StorefrontOrderErrors`, `CustomerOrderErrors`, `SellerOrderErrors`, Completeness/ReservationPolicy errors)
- God orchestrator: `Admin/Operations/Services/AdminOrderOperationsOrchestrator.cs` (~2446 LOC)
- 56 FluentValidation validators; `WithMessage` = 0

## Infrastructure (`Tooba.Order.Infrastructure`)

- Root allowlist: `OrderModule.cs`
- Capability trees: Admin, Checkout, Customer, Seller, Storefront, ReservationCycle, PurchaseVerification, Integrations/*, Persistence, Events, Messaging, Guards, Adapters
- No top-level `Directories/` or `DependencyInjection/` (Catalog COMPLETE_REFERENCE_PATTERN uses both)
- Large facades: `Checkout/Persistence/CheckoutDirectory.cs` (~863), `ReservationCycle/ReservationCycleDirectory.cs` (~382)

## Contracts (`Tooba.Order.Contracts`)

- Folders: Admin, Customer, Fulfillment, Notifications, Payments, Reservation, Returns, Storefront
- Missing: `Errors/`
- ProjectReference: Cart.Contracts only

## Endpoints (`Tooba.Order.Endpoints`)

- Audience folders: Admin, Customer, Seller, Storefront, Errors, Resources
- Root: `OrderEndpointModule.cs`
- Residual: `Results.Json` geography in Storefront endpoints
