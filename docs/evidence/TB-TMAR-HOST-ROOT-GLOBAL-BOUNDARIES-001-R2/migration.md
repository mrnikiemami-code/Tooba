# TB-TMAR-HOST-ROOT-GLOBAL-BOUNDARIES-001-R2 — Migration

Skill: `tooba-architecture-migrate`

All changes are behavior-preserving. No schema, migration, route, or frontend change.

## 1. `Tooba.Order.Infrastructure.csproj` — boundary repair

Removed:

```text
..\..\Cart\Tooba.Cart.Application\Tooba.Cart.Application.csproj
..\..\Catalog\Tooba.Catalog.Application\Tooba.Catalog.Application.csproj
..\..\Payment\Tooba.Payment.Application\Tooba.Payment.Application.csproj
..\..\Fulfillment\Tooba.Fulfillment.Application\Tooba.Fulfillment.Application.csproj
..\..\AccessControl\Tooba.AccessControl.Application\Tooba.AccessControl.Application.csproj
```

Added:

```text
..\..\Payment\Tooba.Payment.Contracts\Tooba.Payment.Contracts.csproj
..\..\AccessControl\Tooba.AccessControl.Contracts\Tooba.AccessControl.Contracts.csproj
```

## 2. Payment seam

- Moved the consumed checkout projection models/ports from `Tooba.Payment.Application` into
  `Tooba.Payment.Contracts/Checkout/OrderPaymentProjectionContracts.cs`
  (`Tooba.Payment.Contracts.Checkout`).
- `Payment.Application` keeps its own internal usings; `PaymentDirectory` / `PaymentExpiryDirectory`
  / `PaymentActorAccess` / `StorefrontPaymentOrchestrator` / `PaymentModule` updated to the new namespace.
- `Order.Infrastructure` consumers (`OrderPaymentBridge`, `OrderPaymentSucceededHandler`,
  `PaymentAdminOrderEnrichmentBridge`, `OrderUnpaidRetrySupplyBridge`, `OrderModule`,
  `CheckoutReservationHoldPolicyAdapter`) now import `Tooba.Payment.Contracts.Checkout`
  / `Tooba.Payment.Contracts.Hold`.
- `Order.Infrastructure` now has an **explicit direct** `Payment.Contracts` ProjectReference.

## 3. Cart seam

- `CheckoutDirectory` no longer takes `ICartDirectory` (`Cart.Application`).
- It depends only on `ICartConversionPort` (Cart.Contracts) and `ICartQueryGateway` (Cart.Contracts).
- The inline `new CartConversionAdapter(...)` fallback was removed in favor of the injected
  `ICartConversionPort`; the reconcile path now calls `_cartConversion.ConvertForCheckoutAsync(...)`.
- `CartConversionAdapter` remains Cart-owned in `Cart.Application` and is registered by Cart;
  no application-internal Cart type is moved into Contracts.

## 4. Catalog seam

- Added `Tooba.Catalog.Contracts.Checkout.ICatalogCheckoutLookup`.
- `CatalogDirectory` (Catalog.Infrastructure) implements it directly; `CatalogModule` registers it.
- `CheckoutDirectory` and `SellerOrderAuthBridge` consume `ICatalogCheckoutLookup` instead of
  `Catalog.Application.ICatalogLookupGateway`.

## 5. Fulfillment seam

- `AdminOrderFulfillmentOperations`, `OrderUnpaidRetrySupplyBridge`, and
  `PaymentAdminOrderEnrichmentBridge` consume `Fulfillment.Contracts.Operations.IFulfillmentAdminOperations`
  instead of `Fulfillment.Application.IFulfillmentDirectory` / `IFulfillmentShippedQuantityReader`.

## 6. AccessControl seam

- Created `Tooba.AccessControl.Contracts` project (narrow effective-access port + value types) and
  added it to `src/backend/Tooba.slnx`.
- Created `AccessControlEffectiveAccessReader` adapter in `AccessControl.Infrastructure`
  (maps `AccessControl.Application` effective access to the Contracts shape);
  registered in `AccessControlModule`.
- `AdminOrderFulfillmentPermissionGate` consumes `IAccessControlEffectiveAccessReader`.
  Explicit namespace qualification avoids ambiguity with `AccessControl.Application.Models.AccessOwnerScope`.

## 7. Preserved behavior fixes during repair

- Restored the accidentally-removed `IStorefrontShippingDraftStore` DI registration in
  `OrderModule` (no other registration exists for it) so storefront shipping-draft behavior is
  unchanged vs `main`.

## 8. Test/guard alignment (no architecture guard weakened)

- `PaymentArchitectureGuardTests.AllowedContractsFolders` gains `Checkout` (new lawful folder).
- `PaymentFoundationTests` payment/order boundary assertions updated from the illegal
  `Tooba.Payment.Application` expectation to the canonical `Tooba.Payment.Contracts` +
  `DoesNotContain Payment.Application`.
- Stale `AccessControl.Application.Models/Permissions` usings removed from
  `OrderSellerPanelArchitectureGuardTests`.
- `Tooba.Host.Tests.csproj` gains explicit Payment Contracts/Application/Infrastructure references.

## 9. Durable guard

New `OrderInfrastructureForeignLayerBoundaryGuardTests` proves, with **no whitelist**:

1. `Tooba.Order.Infrastructure.csproj` has ZERO foreign Application/Infrastructure/Domain ProjectReference.
2. Every `.cs` under `Order.Infrastructure` has ZERO foreign
   Application/Infrastructure/Domain imports/FQNs.
3. `Tooba.Payment.Contracts` reference is explicit and direct, and `Payment.Application` is absent.
4. All foreign `*.Contracts` boundaries used are present.
