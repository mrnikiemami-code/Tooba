# Application organization — TB-TMAR-ORDER-POSTCLOSURE-QUALITY-001

## Before (Application root capability clutter)

- CheckoutAbuseContracts.cs
- CheckoutCommitBarrier.cs
- CheckoutPayableInvariant.cs
- CheckoutProcessContracts.cs
- CheckoutProcessManager.cs
- IReservationCycleCoordinator.cs
- IUnpaidOrderExpiryReconciler.cs
- OrderContracts.cs (monolithic dump)
- ReservationCycleContracts.cs
- ReservationCycleCoordinator.cs
- ReservationCyclePolicyResolver.cs
- SellerOrderCancellationPolicy.cs

## After (capability folders)

```
Tooba.Order.Application/
├─ Admin/ …
├─ Customer/ …
├─ Seller/
│  └─ Policies/SellerOrderCancellationPolicy.cs
├─ Storefront/ …
├─ Checkout/
│  ├─ Abuse/CheckoutAbuseContracts.cs
│  ├─ Process/{CheckoutProcessContracts,CheckoutProcessManager,CheckoutCommitBarrier}.cs
│  ├─ Policies/CheckoutPayableInvariant.cs
│  └─ Contracts/{CheckoutSnapshots,CheckoutDirectoryPorts}.cs
├─ ReservationCycle/
│  ├─ Contracts/{IReservationCycleCoordinator,IUnpaidOrderExpiryReconciler,ReservationCycleContracts}.cs
│  ├─ Services/ReservationCycleCoordinator.cs
│  └─ Policies/ReservationCyclePolicyResolver.cs
├─ PurchaseVerification/OrderPurchaseVerificationContracts.cs
└─ Validation/OrderValidationCodes.cs
```

## OrderContracts split

| Capability | Types |
|---|---|
| Checkout/Contracts/CheckoutSnapshots.cs | OrderLineSnapshot, SellerOrderSnapshot, CheckoutSnapshot, SubmitCheckoutCommand |
| Checkout/Contracts/CheckoutDirectoryPorts.cs | OrderAccess, ICheckoutReservationHoldPolicy, IOrderUseCaseGuard, ICheckoutDirectory, note snapshot/outcomes |
| PurchaseVerification/OrderPurchaseVerificationContracts.cs | OrderPurchaseVerification, IOrderPurchaseVerificationGateway |

## Remaining root `.cs` files

**none** — Application project root has zero `.cs` files.

`Validation/OrderValidationCodes.cs` lives under Validation/ (shared validation codes, not a relocated capability dump).

## Namespace alignment

Path ↔ namespace aligned for every moved file. No `using X = Tooba.Order.Application` alias workaround.

Domain entity `Tooba.Order.Domain.ReservationCycle` is referenced as `global::Tooba.Order.Domain.ReservationCycle` inside `Application.ReservationCycle.*` to avoid parent-namespace clash.
