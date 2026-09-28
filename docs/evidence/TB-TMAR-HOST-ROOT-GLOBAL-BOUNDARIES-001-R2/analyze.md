# TB-TMAR-HOST-ROOT-GLOBAL-BOUNDARIES-001-R2 — Analyze

Skill: `tooba-architecture-analyze`

## 1. Reason for R2

R1 behavior was ACCEPTED but R1 certification was **REJECTED**.

`Tooba.Order.Infrastructure` is a *touched destination project* because R1 added source files into it:

- `ReservationCycle/ReservationCycleRegistration.cs`
- `ReservationCycle/UnpaidOrderExpiryWorker.cs`
- `ReservationCycle/UnpaidOrderExpiryWorkerOptions.cs`
- `Integrations/Payment/CheckoutReservationHoldPolicyAdapter.cs`

For a touched destination project the destination **project graph** is part of certification.
`Tooba.Order.Infrastructure.csproj` still carried direct foreign `*.Application` ProjectReferences. R1 wrongly excluded the dirty `.csproj`/`OrderModule.cs` from certification because they were byte-identical to pre-task `main`.

Additionally, `CheckoutReservationHoldPolicyAdapter` consumes `Tooba.Payment.Contracts.Hold` while `Order.Infrastructure` had **no explicit direct** `Tooba.Payment.Contracts` ProjectReference — it resolved transitively through the illegal `Payment.Application` reference. That is a non-canonical dependency declaration.

## 2. Baseline (HEAD 87a22d7c1439809e89938cc80bb89020eab2ffbf, clean tree before R2)

`Tooba.Order.Infrastructure.csproj` direct foreign Application ProjectReferences:

| Foreign Application reference | Consumer seam in Order.Infrastructure |
| --- | --- |
| `Tooba.Cart.Application` | `CheckoutDirectory` (`ICartDirectory`, `CartConversionAdapter`) |
| `Tooba.Catalog.Application` | `CheckoutDirectory`, `SellerOrderAuthBridge` (`ICatalogLookupGateway`) |
| `Tooba.Payment.Application` | `OrderPaymentBridge`, `OrderPaymentSucceededHandler`, `PaymentAdminOrderEnrichmentBridge`, `OrderUnpaidRetrySupplyBridge`, `CheckoutReservationHoldPolicyAdapter` |
| `Tooba.Fulfillment.Application` | `AdminOrderFulfillmentOperations` (`IFulfillmentDirectory`, `IFulfillmentShippedQuantityReader`) |
| `Tooba.AccessControl.Application` | `AdminOrderFulfillmentPermissionGate` (`IAccessControlEffectiveAccessReader`) |

Foreign Infrastructure / Domain direct ProjectReferences: none.

## 3. Legal target boundary

`Tooba.Order.Infrastructure` may depend on:

- `Tooba.Order.Application`
- `Tooba.Order.Contracts`
- neutral BuildingBlocks (`Tooba.Persistence`) and `Tooba.ModuleContracts`
- foreign `*.Contracts` only

Hard rules: foreign Application = ZERO, foreign Infrastructure = ZERO, foreign Domain = ZERO, cross-module persistence = ZERO.

## 4. Seam ownership decisions (true owner of each contract)

| Edge | Natural owning module | Decision |
| --- | --- | --- |
| Order ← Payment payable/checkout projection | Payment | Move the consumed models/ports into `Tooba.Payment.Contracts` (Payment owns payment projections) |
| Order ← Cart conversion | Cart | Order already had `ICartConversionPort` in `Cart.Contracts`; drop the `ICartDirectory`/`CartConversionAdapter` path |
| Order ← Catalog checkout lookup | Catalog | Add a narrow `ICatalogCheckoutLookup` to `Catalog.Contracts`; implement it directly on `CatalogDirectory` |
| Order ← Fulfillment admin operations | Fulfillment | Consume the existing `IFulfillmentAdminOperations` in `Fulfillment.Contracts.Operations` |
| Order ← AccessControl effective access | AccessControl | Create `Tooba.AccessControl.Contracts` with the narrow effective-access port + value types |

No application-internal type is moved into Contracts merely to silence a guard; each moved type is a genuine cross-module boundary payload/port that Order legitimately consumes.

## 5. Out-of-scope / not reopened

- Authorization cleanup (verify preservation only).
- Any other Host folder.
- Schema / migrations / frontend.
