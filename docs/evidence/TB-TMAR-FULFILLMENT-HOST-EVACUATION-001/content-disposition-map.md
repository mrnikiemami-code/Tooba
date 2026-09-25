# TB-TMAR-FULFILLMENT-HOST-EVACUATION-001 — Content Disposition Map

Task: TB-TMAR-FULFILLMENT-HOST-EVACUATION-001
Parent: TB-TMAR-FULFILLMENT-ARCH-COMPLETE-002-PRECERT-REPAIR-001
Parent commit: 16062d45bde71476da35f9e20622f1b6b5637fa8
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE

Produced BEFORE any Host deletion. Every type/member/constant/behavior classified.

## 1. HostFulfillmentAdminAuthorizer.cs

Path: `src/backend/Host/Tooba.Host/Admin/HostFulfillmentAdminAuthorizer.cs`

| Item | Classification | Disposition |
| --- | --- | --- |
| `HostFulfillmentAdminAuthorizer` type | Fulfillment-specific policy (safe) | Rehomed → `Tooba.Fulfillment.Endpoints.Admin.FulfillmentAdminAuthorizer` |
| `MarketplacePlatformTenantId = "marketplace-platform"` | Generic platform constant (duplicated with `HostSettlementAdminAuthorizer`) | Rehomed → `Tooba.Host.Admin.HostAdminPanelAccess.MarketplacePlatformTenantId` (preserved value) |
| `CurrentAuthenticatedSession` lookup | Generic platform actor seam | Stays Host-only, consumed inside `HostAdminPanelAccess` |
| `ICurrentTenant` lookup | Generic tenant mechanics | Stays Host-only, consumed inside `HostAdminPanelAccess` |
| `ControlPlaneRegistry` lookup (Edition) | Generic edition detection | Stays Host-only, consumed inside `HostAdminPanelAccess` |
| `IAuthorizationGuard` lookup | Generic authorization guard | Stays Host-only, consumed inside `HostAdminPanelAccess` |
| `IHostEnvironment` lookup | Generic environment detection | Stays Host-only, consumed inside `HostAdminPanelAccess` |
| tenant-present admin authorization delegation | Generic platform mechanics | `AdminPanelAccess.RequireAuthorizedAsync` (unchanged, Host) |
| Marketplace Development synthetic tenant fallback | **Generic platform** admin semantics (identical to Settlement) | Moved verbatim into `HostAdminPanelAccess` (generic, zero Fulfillment references) |
| actor resolution (`ResolveActorUserId`) | Generic platform | `AdminPanelAccess.ResolveActorUserId` (unchanged, Host) |
| `AuthorizationCheck` construction (tenant#view) | Generic platform | Preserved verbatim in `HostAdminPanelAccess` |
| Allow → return actor / Unavailable → 503 `admin.authorization.unavailable` / Deny → 403 `admin.authorization.denied` | Generic platform HTTP mapping | Preserved verbatim |
| 503 `admin.tenant.missing` | Generic platform error code | Preserved verbatim |

## 2. HostFulfillmentCustomerAuthorizer.cs

Path: `src/backend/Host/Tooba.Host/Customer/HostFulfillmentCustomerAuthorizer.cs`

| Item | Classification | Disposition |
| --- | --- | --- |
| `HostFulfillmentCustomerAuthorizer` type | Fulfillment-specific policy | Rehomed → `Tooba.Fulfillment.Endpoints.Customer.FulfillmentCustomerAuthorizer` |
| authenticated actor resolution | Generic platform | `ICurrentAuthenticatedUser` (BuildingBlocks.Security) |
| Dev/Testing actor header `X-Tooba-Dev-Actor-User-Id` | Generic platform transport | Preserved in module-owned authorizer (environment-gated) |
| guest fallback actor | Shared cross-module authority | `Tooba.Order.Contracts.Fulfillment.StorefrontGuestActor.ActorId` (single copy) |
| `X-Tooba-Guest-Secret` header read | Generic HTTP header access | Preserved in module-owned authorizer |
| checkout ownership lookup (`ICustomerCheckoutOwnershipReader`) | Order Contracts (already approved) | Unchanged; module consumes Order.Contracts |
| `CartAccess(null, guestSecret)` construction | Cart Contracts | Unchanged; module consumes Cart.Contracts |
| guest cart ownership verification (`ICartQueryGateway.GetCartAsync`) | Cart Contracts | Unchanged semantics |
| `InvalidOperationException` → `ownedByGuest = false` | Fulfillment-specific authorization semantics | Preserved verbatim in module authorizer |
| guest actor `PlacedByUserId` override (`ownedByActor = false`) | Fulfillment-specific semantics | Preserved verbatim |
| missing actor → `customer.actor.missing` | Fulfillment error mapping | Preserved (`FulfillmentErrorCodes.CustomerActorMissing`) |
| missing/not-owned → `customer.order.missing` | Fulfillment error mapping | Preserved (`FulfillmentErrorCodes.CustomerOrderMissing`) |
| dependency on `StorefrontCheckoutService.StorefrontGuestActorId` | Illegal Fulfillment → Order.Application edge | REMOVED — replaced by shared Contracts constant |

## 3. HostFulfillmentSellerAuthorizer.cs

Path: `src/backend/Host/Tooba.Host/Seller/HostFulfillmentSellerAuthorizer.cs`

| Item | Classification | Disposition |
| --- | --- | --- |
| `HostFulfillmentSellerAuthorizer` type | Fulfillment-specific policy | Rehomed → `Tooba.Fulfillment.Endpoints.Seller.FulfillmentSellerAuthorizer` |
| `SellerPanelAccess.RequireAuthorizedAsync` | Generic platform seller-panel primitive | Behind generic `ISellerPanelAccess`; Host impl `HostSellerPanelAccess` |
| session/authorization/environment dependencies | Generic platform | Stay Host-only |
| actor + seller resolution (`X-Tooba-Seller-Party-Id`) | Generic platform transport | Unchanged inside `SellerPanelAccess` |
| `IAccessControlDirectory` usage | Foreign module Application (illegal for Fulfillment) | Behind generic `IPlatformEffectiveAccessReader`; Host impl `HostPlatformEffectiveAccessReader` |
| effective access lookup (`GetEffectiveAccessAsync`) | Generic access mechanics | Unchanged; Host adapter maps to neutral grants |
| `AccessOwnerScope(Seller, sellerPartyId)` | Generic scope mechanics | Mapped in Host adapter |
| `order.handle` filter | Fulfillment-specific permission projection | Preserved verbatim in module authorizer |
| `!DeniedByCeiling` exclusion | Fulfillment-specific semantics | Preserved verbatim |
| `GlobalWithinOwner` projection | Fulfillment-specific semantics | Preserved verbatim |
| `Category` scope + non-null resource projection | Fulfillment-specific semantics | Preserved verbatim |
| distinct category ids | Fulfillment-specific semantics | Preserved verbatim |
| `SellerHandlePermissionInput` construction | Fulfillment Application contract | Preserved verbatim |

## 4. Generic platform seams introduced (Host-implemented, module-neutral)

| Contract (BuildingBlocks.Security) | Host implementation | Contains Fulfillment? |
| --- | --- | --- |
| `IAdminPanelAccess` | `Tooba.Host.Admin.HostAdminPanelAccess` | No |
| `ISellerPanelAccess` | `Tooba.Host.Seller.HostSellerPanelAccess` | No |
| `IPlatformEffectiveAccessReader` (+ `PlatformPermissionGrant`, `PlatformAccessOwnerKind`, `PlatformAccessScopeKind`) | `Tooba.Host.AccessControl.HostPlatformEffectiveAccessReader` | No |

## 5. Deletion precondition

No Host production file was deleted before this map was complete and every live responsibility
above was rehomed. Zero-consumer syntax artifacts: none found (all three files held live policy).
