# Infrastructure organization — TB-TMAR-ORDER-POSTCLOSURE-STRUCTURE-002

## Before

Root production `.cs` files:

- CheckoutDirectory.cs, CheckoutProcessTracker.cs, CheckoutSubmitHost.cs
- OpenOrderUseCaseGuard.cs
- OrderFulfillmentBridge.cs, OrderGridEnrichmentBridge.cs, OrderNotificationBridge.cs
- OrderOutboxRegistration.cs, OrderPaymentBridge.cs, OrderPaymentSucceededHandler.cs
- OrderPurchaseVerificationGateway.cs, OrderReturnBridge.cs
- ReservationCycleCheckoutLineSource.cs, ReservationCycleDirectory.cs
- SellerOrderAuthBridge.cs (also contained CustomerCheckoutOwnershipBridge)
- UnpaidOrderExpiryReconciler.cs
- OrderModule.cs

Existing top-level folders with overlap risk:

- CheckoutAbuse/, Fulfillment/, Payments/, Admin/, Customer/, Seller/, Storefront/, Events/, Persistence/

## Ownership classification

| Old path | Responsibility | New path | Reason |
| --- | --- | --- | --- |
| CheckoutAbuse/CheckoutAbuseGate.cs | Checkout abuse gate | Checkout/Abuse/ | Merge CheckoutAbuse under Checkout capability |
| CheckoutDirectory.cs | EF checkout orchestration | Checkout/Persistence/ | Persistence-centric directory |
| CheckoutSubmitHost.cs | Partial of CheckoutDirectory (ICheckoutSubmitHost) | Checkout/Persistence/ | Same namespace required for C# partial class; not split to Services |
| CheckoutProcessTracker.cs | EF checkout process tracker | Checkout/Persistence/ | Persistence |
| ReservationCycleDirectory.cs | Reservation cycle store | ReservationCycle/ | Capability grouping |
| ReservationCycleCheckoutLineSource.cs | Cycle line source | ReservationCycle/ | Capability grouping |
| UnpaidOrderExpiryReconciler.cs | Unpaid expiry reconciler | ReservationCycle/ | Capability grouping |
| SellerOrderAuthBridge.cs | Seller order auth reader | Seller/ | Seller capability |
| (same file) CustomerCheckoutOwnershipBridge | Customer checkout ownership | Customer/CustomerCheckoutOwnershipBridge.cs | Split to Customer capability |
| OrderPurchaseVerificationGateway.cs | Purchase verification | PurchaseVerification/ | Capability grouping |
| OrderFulfillmentBridge.cs | Contracts fulfillment reader | Integrations/Fulfillment/ | Foreign Contracts bridge |
| Fulfillment/AdminOrderFulfillment* | Order-owned admin fulfillment ops | Admin/Fulfillment/ | Admin capability; not Integrations |
| OrderPaymentBridge.cs + Payments/* | Payment Contracts ports | Integrations/Payment/ | Single Payment integration home |
| OrderReturnBridge.cs | Returns Contracts reader | Integrations/Returns/ | Integration |
| OrderNotificationBridge.cs | Notification Contracts reader | Integrations/Notifications/ | Integration |
| OrderGridEnrichmentBridge.cs | Cross-module grid enrichment (Returns/Fulfillment consumers) | Integrations/GridEnrichment/ | Not Admin-only |
| OrderPaymentSucceededHandler.cs | payment.succeeded consumer + inbox record | Events/Payment/ | Integration-event consumer |
| OpenOrderUseCaseGuard.cs | Use-case guard | Guards/ | Shared guard |
| OrderOutboxRegistration.cs | Outbox module registration | Messaging/ | Messaging |
| Persistence/* | DbContext + Migrations | Persistence/ | Unchanged |

## After

```text
Tooba.Order.Infrastructure
├─ Admin/ (+ Fulfillment/)
├─ Checkout/{Abuse,Persistence}
├─ Customer/
├─ Seller/
├─ Storefront/
├─ ReservationCycle/
├─ PurchaseVerification/
├─ Integrations/{Fulfillment,Payment,Returns,Notifications,GridEnrichment}
├─ Events/{,Payment/}
├─ Guards/
├─ Messaging/
├─ Persistence/{OrderDbContext.cs,Migrations/}
└─ OrderModule.cs
```

## Namespace alignment

Path ↔ namespace for all moved files (see OrderInfrastructureOrganizationGuardTests.RequiredAlignments).

Domain entity clash: inside `Infrastructure.ReservationCycle` and Persistence DbSet, Domain type `ReservationCycle` is referenced via `using ReservationCycleEntity = Tooba.Order.Domain.ReservationCycle` (type alias for Domain entity; not an Infrastructure namespace-hide workaround).

## Root allowlist

| File | Reason |
| --- | --- |
| OrderModule.cs | Module composition / DI entry |

## Integration consolidation

| Concern | Final location |
| --- | --- |
| Payment bridges | Integrations/Payment/ only (Payments/ removed) |
| Fulfillment Contracts bridge | Integrations/Fulfillment/OrderFulfillmentBridge.cs |
| Admin fulfillment ops | Admin/Fulfillment/ (root Fulfillment/ removed) |
| Returns | Integrations/Returns/ |
| Notifications | Integrations/Notifications/ |
| Grid enrichment | Integrations/GridEnrichment/ |

## Behavior

DI registrations/lifetimes unchanged; Persistence/Migrations path unchanged; no frontend changes; Checkout remains paused.
