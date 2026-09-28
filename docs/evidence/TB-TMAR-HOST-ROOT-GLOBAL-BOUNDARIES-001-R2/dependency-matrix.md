# TB-TMAR-HOST-ROOT-GLOBAL-BOUNDARIES-001-R2 — Dependency Matrix

## 1. Touched destination project: `Tooba.Order.Infrastructure`

### 1.1 Project graph — BEFORE (HEAD 87a22d7c…)

```text
Tooba.Order.Infrastructure
├── Tooba.Order.Application
├── Tooba.Cart.Application              <-- FOREIGN APPLICATION (illegal)
├── Tooba.Catalog.Application           <-- FOREIGN APPLICATION (illegal)
├── Tooba.Payment.Application           <-- FOREIGN APPLICATION (illegal)
├── Tooba.Fulfillment.Application       <-- FOREIGN APPLICATION (illegal)
├── Tooba.AccessControl.Application     <-- FOREIGN APPLICATION (illegal)
├── Tooba.Cart.Contracts
├── Tooba.Catalog.Contracts
├── Tooba.Inventory.Contracts
├── Tooba.Promotion.Contracts
├── Tooba.Fulfillment.Contracts
├── Tooba.ModuleContracts
└── BuildingBlocks/Tooba.Persistence
```

`Tooba.Payment.Contracts` reached only **transitively** through `Tooba.Payment.Application` (non-canonical).

### 1.2 Project graph — AFTER (R2)

```text
Tooba.Order.Infrastructure
├── Tooba.Order.Application
├── Tooba.Order.Contracts
├── Tooba.Cart.Contracts
├── Tooba.Catalog.Contracts
├── Tooba.Inventory.Contracts
├── Tooba.Promotion.Contracts
├── Tooba.Payment.Contracts              <-- EXPLICIT DIRECT (added R2)
├── Tooba.Fulfillment.Contracts
├── Tooba.AccessControl.Contracts        <-- NEW module (added R2)
├── Tooba.ModuleContracts
└── BuildingBlocks/Tooba.Persistence
```

foreign Application = ZERO · foreign Infrastructure = ZERO · foreign Domain = ZERO

## 2. Edge-by-edge matrix

| # | Foreign edge | Before | After | Owning module | Contract artifact |
| - | ------------ | ------ | ----- | ------------- | ----------------- |
| 1 | Order ← Cart conversion | `Cart.Application.Ports.ICartDirectory` + `Cart.Application.Conversion.CartConversionAdapter` | `Cart.Contracts.ICartConversionPort` / `ICartQueryGateway` | Cart | existing `Cart.Contracts` (no new type needed) |
| 2 | Order ← Catalog checkout lookup | `Catalog.Application.ICatalogLookupGateway` | `Catalog.Contracts.Checkout.ICatalogCheckoutLookup` | Catalog | NEW `CatalogCheckoutLookupContracts.cs` |
| 3 | Order ← Payment projections | `Payment.Application.Models.*` + `Payment.Application.Ports.*` | `Payment.Contracts.Checkout.*` | Payment | NEW `OrderPaymentProjectionContracts.cs` |
| 4 | Order ← Fulfillment admin ops | `Fulfillment.Application.IFulfillmentDirectory` + `IFulfillmentShippedQuantityReader` | `Fulfillment.Contracts.Operations.IFulfillmentAdminOperations` | Fulfillment | existing `Fulfillment.Contracts.Operations` |
| 5 | Order ← AccessControl effective access | `AccessControl.Application.IAccessControlDirectory` + `Application.Models.EffectiveAccessDto` + `Domain` | `AccessControl.Contracts.IAccessControlEffectiveAccessReader` + `EffectiveAccess` | AccessControl | NEW `Tooba.AccessControl.Contracts` project |

## 3. New / moved contract types

### `Tooba.Payment.Contracts/Checkout/OrderPaymentProjectionContracts.cs` — namespace `Tooba.Payment.Contracts.Checkout`

| Type | Kind | Origin |
| ---- | ---- | ------ |
| `OrderPaymentMode` | enum | moved from `Payment.Application.Models` |
| `PayableSellerOrderSnapshot` | record | moved from `Payment.Application.Models` |
| `PayableCheckoutSnapshot` | record | moved from `Payment.Application.Models` |
| `IPayableCheckoutReader` | port | moved from `Payment.Application.Ports` |
| `IOrderPaymentProjection` | port | moved from `Payment.Application.Ports` |

### `Tooba.Catalog.Contracts/Checkout/CatalogCheckoutLookupContracts.cs` — namespace `Tooba.Catalog.Contracts.Checkout`

| Type | Kind |
| ---- | ---- |
| `ICatalogCheckoutLookup` | port (narrow checkout lookup, implemented directly by `CatalogDirectory`) |

### `Tooba.AccessControl.Contracts` (new project) — namespace `Tooba.AccessControl.Contracts`

| Type | Kind |
| ---- | ---- |
| `AccessOwnerScopeKind` | enum |
| `AccessOwnerScope` | record |
| `EffectivePermission` | record |
| `EffectiveAccess` | record |
| `IAccessControlEffectiveAccessReader` | port (narrow effective-access read) |

Adapter: `Tooba.AccessControl.Infrastructure/Adapters/AccessControlEffectiveAccessReader.cs`.

## 4. Cross-module persistence

| Check | State |
| ----- | ----- |
| Foreign `DbContext` reference inside `Order.Infrastructure` source | ZERO |
| Cross-module LINQ `Join` between module tables | ZERO (`.Join(` + `string.Join` matches are in-module/local) |

## 5. Preserved R1 invariants (independent re-verification)

| Invariant | State |
| --------- | ----- |
| Six Host root files absent | CONFIRMED (all `False`) |
| Settlement global-usings absent | CONFIRMED (no match) |
| Commerce hold owner | Payment |
| Payment → Catalog | `Catalog.Contracts` only |
| Unpaid expiry worker owner | `Order.Infrastructure` |
