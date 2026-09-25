# TB-TMAR-FULFILLMENT-HOST-EVACUATION-001 — Fulfillment Host Evacuation

Task: TB-TMAR-FULFILLMENT-HOST-EVACUATION-001
Parent: TB-TMAR-FULFILLMENT-ARCH-COMPLETE-002-PRECERT-REPAIR-001 (accepted at `16062d45bde71476da35f9e20622f1b6b5637fa8`)

## Content Disposition Map

`docs/evidence/TB-TMAR-FULFILLMENT-HOST-EVACUATION-001/content-disposition-map.md` — produced
BEFORE any Host deletion; covers every type/member/constant/behavior of all three files.

## Old Host file → destination

| Removed Host file | Fulfillment-owned destination | Generic Host seam |
| --- | --- | --- |
| `src/backend/Host/Tooba.Host/Admin/HostFulfillmentAdminAuthorizer.cs` | `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Endpoints/Admin/FulfillmentAdminAuthorizer.cs` | `Tooba.Host.Admin.HostAdminPanelAccess` (`IAdminPanelAccess`) |
| `src/backend/Host/Tooba.Host/Customer/HostFulfillmentCustomerAuthorizer.cs` | `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Endpoints/Customer/FulfillmentCustomerAuthorizer.cs` | `ICurrentAuthenticatedUser` (BuildingBlocks.Security) |
| `src/backend/Host/Tooba.Host/Seller/HostFulfillmentSellerAuthorizer.cs` | `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Endpoints/Seller/FulfillmentSellerAuthorizer.cs` | `Tooba.Host.Seller.HostSellerPanelAccess` (`ISellerPanelAccess`), `Tooba.Host.AccessControl.HostPlatformEffectiveAccessReader` (`IPlatformEffectiveAccessReader`) |

## Generic platform seam changes

- New neutral contracts in `src/backend/BuildingBlocks/Tooba.BuildingBlocks/Security/IPlatformAccessSeams.cs`:
  `IAdminPanelAccess`, `ISellerPanelAccess`, `IPlatformEffectiveAccessReader`,
  `PlatformPermissionGrant`, `PlatformAccessOwnerKind`, `PlatformAccessScopeKind`.
  Zero Fulfillment references; reusable by any module.
- New generic Host implementations: `HostAdminPanelAccess`, `HostSellerPanelAccess`,
  `HostPlatformEffectiveAccessReader`. All generic/platform-named, no Fulfillment error codes/types.
- No Fulfillment-specific replacement file lives in Host.

## Guest actor authority

- New single shared constant: `Tooba.Order.Contracts.Fulfillment.StorefrontGuestActor.ActorId`
  (`aaaaaaaa-aaaa-4aaa-8aaa-000000000009`).
- `StorefrontCheckoutService.StorefrontGuestActorId` now references that constant; the Guid literal
  exists in exactly one production location (the Contracts constant).
- `Fulfillment` consumes `StorefrontGuestActor` from `Order.Contracts` — **no** `Order.Application` edge.

## Semantic parity proof (focused tests, mocks only, no DB, no web host)

`src/backend/Modules/Fulfillment/Tooba.Fulfillment.Tests/Behavior/CustomerFulfillmentAuthorizerParityTests.cs`
- authenticated owner allowed; wrong authenticated owner → `customer.order.missing`
- valid guest secret allowed; invalid guest secret → `customer.order.missing`
- non-dev environment with no actor/secret → `customer.actor.missing`
- missing checkout → `customer.order.missing`
- Dev/Testing actor header honored
- guest `PlacedByUserId` never counts as actor ownership

`src/backend/Modules/Fulfillment/Tooba.Fulfillment.Tests/Behavior/FulfillmentAdminSellerAuthorizerParityTests.cs`
- admin delegates to generic seam and propagates seam failures (503/`admin.tenant.missing`)
- seller actor/seller-party delegation
- `order.handle` global projection → `HasGlobalWithinOwner`
- `order.handle` category scopes → distinct ids
- denied-by-ceiling excluded
- unrelated permission ignored; category without resource ignored

## Files removed only after rehome

The three Host files above were deleted only after the map was complete and every live
responsibility was rehomed. No semantic/business/security content was discarded.

## Cross-module boundary proof

- `Fulfillment -> Host = ZERO` (guard: no `Tooba.Host` in Fulfillment production sources).
- `Fulfillment -> AccessControl = ZERO` (no `Tooba.AccessControl.*` in Fulfillment; effective access
  arrives through the neutral `IPlatformEffectiveAccessReader` seam).
- `Fulfillment -> Order.Application = ZERO` (guard) — only `Order.Contracts`.
- `Tooba.Fulfillment.Endpoints.csproj` references only Fulfillment.Application, BuildingBlocks,
  `Order.Contracts`, `Cart.Contracts` (no Infrastructure/Host/AccessControl/Order.Application).

## DI proof

- `FulfillmentEndpointModule.AddFulfillmentEndpointPresentation` registers the three module-owned
  authorizers; `Program.cs` calls it (`builder.Services.AddFulfillmentEndpointPresentation()`).
- The three Host authorizer registrations were removed from `Program.cs`.
- Host registers only generic seams: `IAdminPanelAccess`, `ISellerPanelAccess`,
  `IPlatformEffectiveAccessReader`.
- No service-locator expansion.

## Focused validation

- `dotnet build Tooba.Fulfillment.Tests` → succeeded.
- focused tests (`FulfillmentArchitectureGuardTests`, `FulfillmentValidatorCoverageGuardTests`,
  `CustomerFulfillmentAuthorizerParityTests`, `FulfillmentAdminSellerAuthorizerParityTests`,
  `FulfillmentEndpointOwnershipTests`) → 28/28 passed.
- `dotnet build Tooba.Host` → succeeded.

## Structure certification

Fulfillment structure certification remains **PENDING**
(next: `TB-TMAR-FULFILLMENT-ARCH-COMPLETE-002-STRUCTURE-001`). Not certified in this task.

## Protected state

Routes unchanged, error semantics preserved, MediatR 12.5 / ISender-only endpoints unchanged,
validator coverage 15 / 10 / 5 unchanged, no schema/migration change, Checkout
`PAUSED_AT_SAFE_W5_CHECKPOINT`, `frontendFrozen = true`.
